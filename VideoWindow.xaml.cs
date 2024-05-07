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
using static WpfApp1.MainWindow;
using System.Runtime.ExceptionServices;
using System.Drawing;
using System.Windows.Markup;


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
            Singleton = this;
            RectTextBaseLabelRight.Content = "";
            RectTextBaseLabelLeft.Content = "";
            JumpInputBox.Text = "";
            Network.GenerateNetworks(RectText.Window.VideoWindowLeft);
            Labels = new Dictionary<int, List<YOLORect>>();
            Frame.MouseDown += QLBL.AddQLBL;
            QLBL.setclasses();
            SavePLBLButton.IsEnabled = false;
            QLBLMenu_ResetButton.Click += QLBL.Reset;
            QLBLMenu_SaveButton.Click += QLBL.SaveQLBL;
            QLBLMenu_LoadButton.Click += QLBL.LoadQLBL;
            QLBLMenu_ScanButton.Click += QLBL.Scan;
        }
        public static VideoWindow Singleton;
        private static DispatcherTimer timer;
        private static DispatcherTimer playtimer;
        public static int CurrentFrame;
        private static int EmptierFrame;
       // private static int ThreadFrame;
        private static SortedDictionary<int, Bitmap> ImageBuffer;
        public static readonly int BufferSize = 100;
        public static string PATH;
        public static object lockObject = new object();
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
                JumpInputBox.Text = 0.ToString();
                // Start the thread

                emptierThread.Start();
                timer.Start();
                //RenderRectangle(new YOLORect(0.5, 0.5, 1, 1, 0));
            }
        }
        public static bool Waiting4Network = false;
        public void CallToTLBL(object sender, RoutedEventArgs e)
        {
            Network N = (Network)(((MenuItem)sender).Tag);
            if (N.isYolo)
            {
                string filePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Models", N.name);
                Thread T = new Thread(() => YoloVideoThread(filePath));
                T.Start();
            }
        }
        private static bool Reload = false;

        public static Dictionary<int, List<YOLORect>> Labels;
        public void YoloVideoThread(string yolopath, double conf = 0.1)
        {
            Labels = new Dictionary<int, List<YOLORect>>();
            Waiting4Network = true;
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "python";
            startInfo.Arguments = $"{System.IO.Path.Combine(Directory.GetCurrentDirectory(), "src", "tlbl_yolo.py")} \"{yolopath}\" \"{PATH}\" {conf}";
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = false;
            
            Process process = new Process();
            process.StartInfo = startInfo;
            /*
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    CompletedPercent = int.Parse(e.Data);
                }
            };
           
            */
            int frame_idx = 0;
            Labels[0] = new List<YOLORect>();
            process.Start();
            while (!process.StandardOutput.EndOfStream)
            {
                string? line = process.StandardOutput.ReadLine();
                
                if (line != null)
                {
                    string[] parts = line.Split();
                    if (line == "--")
                    {
                        frame_idx++;
                        Labels[frame_idx] = new List<YOLORect>();
                    }
                    else if (parts.Length == 5) { 

                        Labels[frame_idx].Add(new YOLORect(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]), float.Parse(parts[4]), int.Parse(parts[0])));
                    }
                }
            }
            process.WaitForExit();
            Waiting4Network = false;
            Reload = true;
            Dispatcher.Invoke(() =>
            {
                RenderLabels();
                SavePLBLButton.IsEnabled = true;
            });
            
        }
        private void RenderLabels()
        {
            RectText.DestroyAll();
            if (Labels.ContainsKey(CurrentFrame))
            {
                foreach (var rect in Labels[CurrentFrame])
                {
                    RenderRectangle(rect);
                }
            }
            RectTextBaseLabelLeft.Content = "Predicted Label";
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
                    if (Labels.ContainsKey(CurrentFrame))
                    {
                        RenderLabels();
                    }
                    QLBL.Refresh();
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
            if (Labels.ContainsKey(CurrentFrame))
            {
                RenderLabels();
            }
            QLBL.Refresh();
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
            QLBL.Refresh();
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
            if (Waiting4Network)
            {
                Pythonpercent.Content = $"Processing Video...";
            }
            else
            {
                Pythonpercent.Content = "";
            }
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
        public static BitmapImage ConvertBitmapToBitmapImageRGBA(Bitmap bitmap)
        {
            BitmapImage bitmapImage = new BitmapImage();
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png); // Use PNG format
                memory.Position = 0;
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memory;
                bitmapImage.EndInit();
            }
            return bitmapImage;
        }
        public static Bitmap ConvertBitmapImageToBitmapRGBA(BitmapImage bitmapImage)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                PngBitmapEncoder enc = new PngBitmapEncoder(); // Use PNG encoder
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }
        public static Bitmap ConvertBitmapImageToBitmap(BitmapImage bitmapImage)
        {
            // BitmapImage bitmapImage = new BitmapImage(new Uri("../Images/test.png", UriKind.Relative));

            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }
        private void Jump(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                int Destination = CurrentFrame;
                if (int.TryParse(JumpInputBox.Text, out Destination))
                {
                    if (Destination >= 0 && Destination < FrameCount)
                    {
                        Seek(Destination);
                    }
                }
                JumpInputBox.Text = $"{Destination}";
            }
        }
        public void RenderRectangle(YOLORect r)
        {
            var cor1 = ((r.x - r.w/2)*Frame.Width,(r.y - r.h/2) * Frame.Height);
            var cor2 = ((r.x + r.w / 2) * Frame.Width, (r.y + r.h / 2) * Frame.Height);
            int c = r.c;
            var i = new System.Windows.Controls.Image();
            i.Width = Math.Abs(cor1.Item1 - cor2.Item1);// (cor1.Item1 - cor2.Item1);
            i.Height = Math.Abs(cor1.Item2 - cor2.Item2);
            //i.Height = Math.Abs(cor1.Item2 - cor2.Item2);
            i.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            i.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            i.Margin = new Thickness(cor1.Item1 + Frame.Margin.Left, cor1.Item2+ Frame.Margin.Top, 0, 0);
            i.Name = $"Rect_{RectText.Rectangles.Count}";
            i.IsHitTestVisible = false;
            Grid.Children.Add(i);
            System.Windows.Controls.Panel.SetZIndex(i, 3);
            setRectColor(i, classes[c].Item2);
            new RectText(r, i, RectText.Window.VideoWindowLeft);

        }
        //models menu save load plbl
        private void SavePLBL(object sender, RoutedEventArgs e)
        {
            //PathLabel.Content = "aaa";
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "Select output folder";
            DialogResult result = folderBrowserDialog.ShowDialog();
            string selectedFilePath = System.IO.Path.Combine(folderBrowserDialog.SelectedPath, $"{System.IO.Path.GetFileNameWithoutExtension(VideoWindow.PATH)}.plbl");
            using (StreamWriter writer = new StreamWriter(selectedFilePath))
            {
                foreach (var framenum in Labels.Keys)
                {
                    writer.WriteLine(framenum);
                    foreach (var rect in Labels[framenum])
                    {
                        writer.WriteLine(rect);
                    }
                }

            }
        }
        private void LoadPLBL(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select Predicted Label File",
                Filter = "PLBL files (*.plbl)|*.plbl|All files (*.*)|*.*"
            };
            
         
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                int frame = 0;
                Labels = new Dictionary<int, List<YOLORect>>();
                var filepath = openFileDialog.FileName;
                using (StreamReader sr = new StreamReader(filepath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        var parts = line.Split(' ');
                        if(parts.Length == 1)
                        {
                            if(int.TryParse(parts[0], out int tmp))
                            {
                                frame = tmp;
                                Labels[frame] = new List<YOLORect>();
                            }
                        }
                        else if(parts.Length == 5)
                        {
                            if (int.TryParse(parts[0], out int c) && float.TryParse(parts[1], out float x) && float.TryParse(parts[2], out float y) && float.TryParse(parts[3], out float w) && float.TryParse(parts[4], out float h))
                            {
                                Labels[frame].Add(new YOLORect(x,y,w,h,c));
                            }
                        }
                        else
                        {
                            print($"{line} is invalid");
                            break;
                        }
                    }
                }
                RenderLabels();
                SavePLBLButton.IsEnabled = true;
            }
        }
    }



    

}
