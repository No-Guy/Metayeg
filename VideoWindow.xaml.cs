using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;
using OpenCvSharp;
using System.Threading;
using OpenCvSharp.WpfExtensions;
using WpfApp1;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Windows.Ink;


namespace Metayeg
{
    /// <summary>
    /// Interaction logic for VideoWindow.xaml
    /// </summary> 
    public partial class VideoWindow : System.Windows.Window
    {
        public VideoWindow()
        {
            PlayPauseState = false;
            InitializeComponent();
            kill = false;
            Closing += WindowClosing;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(10); // Adjust the interval as needed
            timer.Tick += updateData;
            playtimer = new DispatcherTimer();
            playtimer.Tick += PlayTick;
            PlayPauseButton.Content = "Play";
            HasVideo = false;
        }
        private static DispatcherTimer timer;
        private static DispatcherTimer playtimer;
        public static int CurrentFrame;
        private static int EmptierFrame;
       // private static int ThreadFrame;
        private static SortedDictionary<int, Bitmap> ImageBuffer;
        public static readonly int BufferSize = 100;
        public static string PATH;
        public static object lockObject = new object();
        //public static Thread fillThread;
        //public static Thread emptierThread;
        public static bool kill = false;
        public static double FPS;
        public static int numWorkers = 5;
        public static bool HasVideo = false;
        private static Worker[] FillerThreads = new Worker[numWorkers];
        public class Worker
        {
            public Queue<int> Jobs;
            public Thread thread;
            public ManualResetEvent threadControlEvent = new ManualResetEvent(false);
            public Worker(int id)
            {
                thread = new Thread(() => FillerThread(id));
                Jobs = new Queue<int>();
                threadControlEvent.Set();
            }

        }
        public static int FrameCount = 0;
        private void SelectVideo(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select Video File",
                Filter = "Video Files|*.mp4;*.mkv;*.avi;*.wmv|All Files|*.*"
            };

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                PATH = openFileDialog.FileName;
                using (var capture = new VideoCapture(PATH))
                {
                    FrameCount = (int)capture.Get(VideoCaptureProperties.FrameCount);

                    FPS = capture.Fps;
                    int fps = (int)Math.Round(capture.Fps);
                    int width = (int)capture.FrameWidth;
                    int height = (int)capture.FrameHeight;
                    VideoData.Content = $"{width} x {height} @ {fps}";
                    HasVideo = true;
                }
                    //VideoFileReader videoReader = new VideoFileReader();
                    // Handle the selected video file path as needed (e.g., display it in a TextBox)
                for (int i = 0; i < numWorkers; i++)
                {
                    FillerThreads[i] = new Worker(i);
                    FillerThreads[i].thread.Start();
                }
                Seek(0);
                //Thread fillThread = new Thread(new ThreadStart(FillerThread));
                Thread emptierThread = new Thread(new ThreadStart(EmptierThread));
                
                // Start the thread
                
                emptierThread.Start();
                timer.Start();
                
            }
        }
        private void OneFrameForward(object sender, RoutedEventArgs e)
        {
            if (HasVideo)
            {
                Forward(1);
            }
        }
        public static bool PlayPauseState = false;
        private void PlayPause(object sender, RoutedEventArgs e)
        {
            if (!HasVideo)
            {
                return;
            }
            if (!PlayPauseState)
            {
                playtimer.Interval = TimeSpan.FromSeconds((1/FPS));
                playtimer.Start();
                PlayPauseButton.Content = "Pause";
            }
            else
            {
                PlayPauseButton.Content = "Play";
                playtimer.Stop();
            }
            PlayPauseState = !PlayPauseState;
        }
        public void PlayTick(object sender, EventArgs e)
        {
            Forward(1);
        }
        private void OneFrameBack(object sender, RoutedEventArgs e)
        {
            if (CurrentFrame > 0 && HasVideo)
            {
                CurrentFrame--;
                var frame = ExtractFrameSync(PATH, CurrentFrame);
                if (frame != null)
                {
                    Frame.Source = ConvertBitmapToBitmapImage(frame);
                    ImageBuffer[CurrentFrame] = frame;
                }
            }
            
        }
        static ManualResetEvent MainThreadStall = new ManualResetEvent(false);
        static bool RenderThreadSleep = false;

        Stopwatch ActualFPSSW = new Stopwatch();
        private static double Latency = 0;

        private static int idx = 0;
        Random random = new Random();
        public void Forward(int Stride = 1)
        {
            if (CurrentFrame >= FrameCount -1)
            {
                return;
            }
            ActualFPSSW.Start();
            CurrentFrame += Stride;
            //int idx = random.Next(numWorkers);
            if (CurrentFrame + BufferSize < FrameCount)
            {
                FillerThreads[idx].Jobs.Enqueue(CurrentFrame + BufferSize - 1);
            
                FillerThreads[idx].threadControlEvent.Set();
                //MainWindow.print($"tid: {idx}, job: {CurrentFrame + BufferSize}");
                idx = (idx + 1) % numWorkers;
            }
            if (!ImageBuffer.ContainsKey(CurrentFrame))
            {
                RenderThreadSleep = true;
                MainThreadStall.WaitOne();
                MainThreadStall.Reset();
                RenderThreadSleep = false;
            }
            Frame.Source = ConvertBitmapToBitmapImage(ImageBuffer[CurrentFrame]);
            ActualFPSSW.Stop();
            Latency = ActualFPSSW.Elapsed.TotalMilliseconds;
            playtimer.Interval = TimeSpan.FromMilliseconds(Math.Max(1,1/FPS * 1000 - Latency));
            ActualFPSSW.Reset();
        }
        public void Seek(int framenum)
        {
            ImageBuffer = new SortedDictionary<int, Bitmap>();
            CurrentFrame = framenum;
            for (int i = 0; i < BufferSize; i++)
            {
                int job = CurrentFrame + i;
                if (job < FrameCount)
                {
                    FillerThreads[i % numWorkers].Jobs.Enqueue(job);
                }
            }
            for (int i = 0; i < numWorkers; i++)
            {
                FillerThreads[i].threadControlEvent.Set();
            }
            EmptierFrame = CurrentFrame;
            var frame = ExtractFrameSync(PATH, CurrentFrame);
            if (frame != null)
            {
                Frame.Source = ConvertBitmapToBitmapImage(frame);
            }
        }
        
        public static void FillerThread(int i)
        {
            using (var capture = new VideoCapture(PATH))
            {
                while (!kill)
                {
                    if (FillerThreads[i].Jobs.Count > 0)
                    {
                        int job = FillerThreads[i].Jobs.Dequeue();
                        if (!ImageBuffer.ContainsKey(job))
                        {
                            var bmpi = ExtractFrameAsync(capture, job);
                            lock (lockObject)
                            {
                                ImageBuffer[job] = bmpi;
                            }
                            
                        }
                        if (job == CurrentFrame && RenderThreadSleep)
                        {
                            MainThreadStall.Set();
                        }

                    }
                    else
                    {
                        FillerThreads[i].threadControlEvent.WaitOne();
                        FillerThreads[i].threadControlEvent.Reset();
                    }
                }
            }
        }
        public void PrintKeys()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var key in ImageBuffer.Keys)
            {
                sb.Append(key.ToString() + ", ");
            }
            sb.Append($"\n{ImageBuffer.Count}");
            MainWindow.print(sb.ToString());
        }
        public void updateData(object sender, EventArgs e)
        {
            BufferData.Content = $"Frame {CurrentFrame}/{FrameCount}, Buffer: {ImageBuffer.Keys.Count}/{BufferSize}, Latency: {(int)Latency} ms";
        }
        private void EmptierThread()
        {
            while (!kill)
            {
                if(ImageBuffer.Count > BufferSize)
                {
                    var keysToRemove = new HashSet<int>();
                    lock (lockObject)
                    {
                        foreach (var key in ImageBuffer.Keys)
                        {
                            if (key < CurrentFrame || key >= CurrentFrame + BufferSize)
                            {
                                keysToRemove.Add(key);
                            }
                        }
                    }
                    foreach (var item in keysToRemove)
                    {
                        lock (lockObject)
                        {
                            ImageBuffer.Remove(item);
                        }
                    }
                }
            }
        }
        private static Bitmap? ExtractFrameAsync(VideoCapture capture, int frameIndex)
        {

            // Set the position of the video stream to the desired frame
            capture.Set(VideoCaptureProperties.PosFrames, frameIndex);

            // Read the frame at the specified index
            Mat frame = new Mat();
            capture.Read(frame);
          
            // Check if the frame is empty (invalid index or end of video)
            if (frame.Empty())
            {
                return null;
            }

            Bitmap bitmap = MatToBitmap(frame);
            //bitmap.Save("example.png", System.Drawing.Imaging.ImageFormat.Png);
            return bitmap;//ConvertBitmapToBitmapImage(bitmap);
        }
        public static Bitmap? ExtractFrameSync(string videoPath, int frameIndex)
        {

            // Open the video file
            using (var capture = new VideoCapture(videoPath))
            {
                // Check if the video file is opened successfully
                if (!capture.IsOpened())
                {
                    return null;
                }

                // Set the position of the video stream to the desired frame
                capture.Set(VideoCaptureProperties.PosFrames, frameIndex);

                // Read the frame at the specified index
                Mat frame = new Mat();
                capture.Read(frame);

                // Check if the frame is empty (invalid index or end of video)
                if (frame.Empty())
                {
                    return null;
                }

                return MatToBitmap(frame);
                //bitmap.Save("example.png", System.Drawing.Imaging.ImageFormat.Png);
                //return ConvertBitmapToBitmapImage(bitmap);
            }
        }
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            kill = true;
            timer.Stop();
            ImageBuffer = new SortedDictionary<int, Bitmap>();
        }


        private static Bitmap MatToBitmap(Mat mat)
        {
            using (var ms = mat.ToMemoryStream())
            {
                return (Bitmap)System.Drawing.Image.FromStream(ms);
            }
        }
        public static BitmapImage ConvertBitmapToBitmapImage(Bitmap bitmap)
        {
            BitmapImage bitmapImage = new BitmapImage();
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memory;
                bitmapImage.EndInit();
            }
            return bitmapImage;
        }
        private void Jump(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                int Destination = CurrentFrame;
                if (int.TryParse(JumpInputBox.Text, out Destination))
                {
                    if (Destination > 0 && Destination < FrameCount)
                    {
                        Seek(Destination);
                    }
                }
                JumpInputBox.Text = $"{Destination}";
            }
        }
    }
    
}
