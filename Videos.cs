using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfApp1;
namespace Metayeg
{
    internal class Videos
    {
        public static void GetVideoPath()
        {
            var openFileDialog = new OpenFileDialog();

            // Set the initial directory (optional)

            // Set the title of the dialog
            openFileDialog.Title = "Select a file";

            // Set the file filter (optional)
            openFileDialog.Filter = "Video files|*.mp4;*.avi;*.mkv;*.mov;*.wmv|All files (*.*)|*.*";

            // Show the dialog and get the result
            DialogResult result = openFileDialog.ShowDialog();

            // Check if the user clicked OK
            if (result == DialogResult.OK)
            {
                string selectedFileName = openFileDialog.FileName;
                if (selectedFileName != null)
                {
                    MainWindow.print(selectedFileName);
                    RectText.DestroyAll();
                    MainWindow.Singleton.NewImage();
                    ImageObj.Images = new List<ImageObj>();
                    ImageObj.ShownInt = 0;
                    ImageObj.Shown = null;
                    MainWindow.PATH = null;
                    MainWindow.Singleton.UpdateImageCounter();
                    MainWindow.Singleton.Opened.Source = null;
                    Loader(selectedFileName);
                }
            }
        }
        private static MediaElement mediaElement;
        private static DispatcherTimer timer;
        private static int frameCount;
        private static int desiredFrameCount = 10;

        static async Task Loader(string path)
        {
            var mediaElement = new MediaElement();
            mediaElement.LoadedBehavior = MediaState.Manual;
            mediaElement.MediaOpened += MediaElement_MediaOpened;
            mediaElement.Source = new Uri(path);
            timer = new DispatcherTimer();
            timer.Tick += Timer_Tick;
        }
        private static void MediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            // Set the timer interval based on video duration
            double videoDuration = mediaElement.NaturalDuration.TimeSpan.TotalMilliseconds;
            int interval = (int)(videoDuration / desiredFrameCount);
            timer.Interval = TimeSpan.FromMilliseconds(interval);

            // Start the timer
            timer.Start();
        }
        private static void Timer_Tick(object sender, EventArgs e)
        {
            // Capture a frame as BitmapImage
            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(
                (int)mediaElement.ActualWidth, (int)mediaElement.ActualHeight, 96, 96, PixelFormats.Pbgra32);

            renderTargetBitmap.Render(mediaElement);

            // Convert to BitmapImage
            BitmapImage bitmapImage = new BitmapImage();
            PngBitmapEncoder pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

            using (var stream = new System.IO.MemoryStream())
            {
                pngEncoder.Save(stream);
                stream.Seek(0, System.IO.SeekOrigin.Begin);

                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
            }

            // Process the BitmapImage as needed
            

            // Stop the process when desired number of frames are captured
            frameCount++;
            if (frameCount >= desiredFrameCount)
            {
                MainWindow.Singleton.Opened.Source = bitmapImage;
                timer.Stop();
            }
        }
    }
    


    }
