using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static WpfApp1.MainWindow;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApp1;
using System.IO;
using System.Windows.Shapes;

namespace Metayeg
{
    public struct Color32
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;
        public Color32(byte r, byte g, byte b) { 
            this.r = r;
            this.g = g;
            this.b = b;
            a = 255;
        }
    }
    internal class QLBL
    {
        public static Dictionary<int, HashSet<QLBL>> CurrentLabels = new Dictionary<int, HashSet<QLBL>>();
        public System.Windows.Controls.Image? I;
        public System.Windows.Point at;
        public int frame;
        public static int Class;
        public int label_class;
        public System.Windows.Controls.Label? label;
        public bool isActive = false;
        public static void AddQLBL(object sender, MouseButtonEventArgs e)
        {
            if (VideoWindow.Singleton.Frame.Source != null && e.ChangedButton == MouseButton.Left)
            {
                System.Windows.Point mousePosition = Mouse.GetPosition(VideoWindow.Singleton.Frame);
                if (RenderedQLBL == null)
                {
                    RenderedQLBL = new HashSet<QLBL>();
                }
                new QLBL(mousePosition);
                Refresh();
            }
        }
        public static HashSet<QLBL> RenderedQLBL;
        public void Render()
        {

            var i = new System.Windows.Controls.Image();
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri("pack://application:,,,/xcornc.png");
            bi.EndInit();
            i.Source = ChangeImageColor(bi, new RectColor(255, 255, 255), classes[label_class].Item2);
            i.Name = "test_image";
            i.Width = 15;
            i.Height = 15;
            System.Windows.Controls.Panel.SetZIndex(i, 3);
            VideoWindow.Singleton.Grid.Children.Add(i);
            i.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            i.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            i.Margin = new Thickness(at.X + VideoWindow.Singleton.Frame.Margin.Left - 0.5 * i.Width, at.Y + VideoWindow.Singleton.Frame.Margin.Top - 0.5 * i.Height, 0, 0);
            i.MouseDown += RemoveQLBL;
            // Show the image
            i.Visibility = Visibility.Visible;
            i.Tag = this;
            I = i;

            var lbl = new System.Windows.Controls.Label();
            label = lbl;
            label.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            label.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            VideoWindow.Singleton.Grid.Children.Add(label);
            label.Width = VideoWindow.Singleton.RectTextBaseLabelRight.Width;
            label.Height = VideoWindow.Singleton.RectTextBaseLabelRight.Height;
            label.Margin = new System.Windows.Thickness(0, VideoWindow.Singleton.RectTextBaseLabelRight.Margin.Top + 25 * (RenderedQLBL.Count + 1), VideoWindow.Singleton.RectTextBaseLabelRight.Margin.Right, 0);
            label.Content = classes[label_class].Item1;
            RenderedQLBL.Add(this);

        }
        public void DeRender()
        {
            VideoWindow.Singleton.Grid.Children.Remove(I);
            I = null;
            VideoWindow.Singleton.Grid.Children.Remove(label);
            label = null;
        }
        public static double Score;
        public static void Refresh()
        {
            Score = 0;
            double WrongClassMultiplier = 1.5d;
            if (RenderedQLBL != null)
            {
                foreach (var qlbl in RenderedQLBL)
                {
                    qlbl.DeRender();
                }

                RenderedQLBL.Clear();
            }
            else
            {
                RenderedQLBL = new HashSet<QLBL>();
            }
            if (!CurrentLabels.ContainsKey(VideoWindow.CurrentFrame))
            {
                CurrentLabels[VideoWindow.CurrentFrame] = new HashSet<QLBL>();
            }
            if (VideoWindow.Labels.ContainsKey(VideoWindow.CurrentFrame))
            {
                VideoWindow.Singleton.RectTextBaseLabelLeft.Content = "Predicted Label";
            }
            else
            {
                VideoWindow.Singleton.RectTextBaseLabelLeft.Content = "";
            }

            
            VideoWindow.Singleton.RectTextBaseLabelRight.Content = "True Label";
            if (VideoWindow.Labels.ContainsKey(VideoWindow.CurrentFrame))
            {

                foreach (var rect in VideoWindow.Labels[VideoWindow.CurrentFrame])
                {
                    QLBL? Match = null;
                    QLBL? BestWrongClass = null;
                    QLBL? BestCorretClass = null;
                    double shortestDistanceCorrectClass = 100000000;
                    double shortestDistanceWrongClass = 100000000;
                    double MatchDistance = 0;
                    foreach (var qlbl in CurrentLabels[VideoWindow.CurrentFrame])
                    {
                        if (!RenderedQLBL.Contains(qlbl))
                        {
                            var x = (double)qlbl.at.X / (double)VideoWindow.Singleton.Frame.ActualWidth;
                            var y = (double)qlbl.at.Y / (double)VideoWindow.Singleton.Frame.ActualHeight;
                            double distance = L2(x, y, rect.x, rect.y);
                            if (qlbl.label_class == rect.c)
                            {
                                if (BestCorretClass == null || shortestDistanceCorrectClass > distance)
                                {
                                    BestCorretClass = qlbl;
                                    shortestDistanceCorrectClass = distance;
                                }
                            }
                            else
                            {
                                if (BestWrongClass == null || shortestDistanceWrongClass > distance)
                                {
                                    BestWrongClass = qlbl;
                                    shortestDistanceWrongClass = distance;
                                }
                            }
                        }
                    }
                    //shortestDistanceWrongClass *= WrongClassMultiplier;
                    if (BestCorretClass != null && BestWrongClass == null)
                    {
                        Match = BestCorretClass;
                        MatchDistance = shortestDistanceCorrectClass;
                    }
                    else if (BestCorretClass == null && BestWrongClass != null)
                    {
                        Match = BestWrongClass;
                        MatchDistance = shortestDistanceWrongClass + 0.25;
                    }
                    else if (BestCorretClass != null && BestWrongClass != null)
                    {
                        if (shortestDistanceCorrectClass < shortestDistanceWrongClass * WrongClassMultiplier)
                        {
                            Match = BestCorretClass;
                            MatchDistance = shortestDistanceCorrectClass;
                        }
                        else
                        {
                            Match = BestWrongClass;
                            MatchDistance = shortestDistanceWrongClass + 0.25;
                        }
                    }
                    if (Match != null)
                    {
                        Match.Render();
                        bool contains = Contains(rect, (double)Match.at.X / (double)VideoWindow.Singleton.Frame.ActualWidth, (double)Match.at.Y / (double)VideoWindow.Singleton.Frame.ActualHeight);
                        bool CorretClass = rect.c == Match.label_class;
                        if (contains && CorretClass)
                        {
                            Match.label.Foreground = System.Windows.Media.Brushes.Green;
                        }
                        else if (contains && !CorretClass)
                        {
                            Match.label.Foreground = System.Windows.Media.Brushes.DarkGoldenrod;
                        }
                        else if (!contains && CorretClass)
                        {
                            Match.label.Foreground = System.Windows.Media.Brushes.Orange;
                        }
                        else
                        {
                            Match.label.Foreground = System.Windows.Media.Brushes.Red;
                        }
                        Score += MatchDistance;
                    }
                    else
                    {
                        Score += 1;
                    }
                }
            }
            foreach (var qlbl in CurrentLabels[VideoWindow.CurrentFrame])
            {
                if (!RenderedQLBL.Contains(qlbl))
                {
                    qlbl.Render();
                    qlbl.label.Foreground = System.Windows.Media.Brushes.DarkRed;
                    Score += 1;
                }
            }
            VideoWindow.Singleton.ScoreBox.Content = $"Score: {Score}";
            
            
        }
        public static double L2(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
        }
        public static bool Contains(YOLORect r, double x, double y)
        {
            return r.x + r.w / 2 > x && r.x - r.w / 2 < x && r.y + r.h / 2 > y && r.y - r.h / 2 < y;
        }
        public QLBL(System.Windows.Point at)
        {
            label = null;
            frame = VideoWindow.CurrentFrame;
            if (!CurrentLabels.ContainsKey(frame))
            {
                CurrentLabels[frame] = new HashSet<QLBL>();
            }
            CurrentLabels[frame].Add(this);
            this.at = at;
            label_class = Class;
        }
        public static void RemoveQLBL(object sender, MouseButtonEventArgs e)
        {
            if (VideoWindow.Singleton.Frame.Source != null && e.ChangedButton == MouseButton.Left)
            {
                var qlbl = (QLBL)(((System.Windows.Controls.Image)sender).Tag);
                CurrentLabels[VideoWindow.CurrentFrame].Remove(qlbl);
                Refresh();
            }
        }
        public static BitmapImage ChangeImageColor(BitmapImage originalImage, RectColor s, RectColor t)
        {

            Bitmap bitmap = VideoWindow.ConvertBitmapImageToBitmapRGBA(originalImage);

            // Iterate through each pixel in the bitmap
            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    // Get the color of the current pixel
                    var pixelColor = bitmap.GetPixel(x, y);

                    // Check if the pixel is white
                    if (pixelColor.R == s.r && pixelColor.G == s.g && pixelColor.B == s.b)
                    {
                        System.Drawing.Color newColor = System.Drawing.Color.FromArgb(pixelColor.A, t.r, t.g, t.b);
                        bitmap.SetPixel(x, y, newColor);
                    }
                }
            }
            return VideoWindow.ConvertBitmapToBitmapImageRGBA(bitmap);
        }
        public static void ChangeClass(object sender, RoutedEventArgs e)
        {
            ((MenuItem)VideoWindow.Singleton.QLBL_Classes.Items[Class]).Header = $"{MainWindow.classes[Class].Item1}";
            Class = (int)((MenuItem)sender).Tag;
            ((MenuItem)VideoWindow.Singleton.QLBL_Classes.Items[Class]).Header = $"{MainWindow.classes[Class].Item1}✔";
            VideoWindow.Singleton.QLBL_Classes.Header = $"QLBL Class({QLBL.Class})";
        }
        public static void setclasses()
        {
            Class = 0;
            VideoWindow.Singleton.QLBL_Classes.Items.Clear();
            foreach (var c in MainWindow.classes.Keys)
            {
                int classid = c;
                string class_name = MainWindow.classes[c].Item1;
                MenuItem newItem = new MenuItem();
                newItem.Header = class_name;
                newItem.Click += ChangeClass;
                newItem.Tag = c;
                VideoWindow.Singleton.QLBL_Classes.Items.Add(newItem);
            }
            ((MenuItem)VideoWindow.Singleton.QLBL_Classes.Items[Class]).Header = $"{MainWindow.classes[Class].Item1}✔";
            VideoWindow.Singleton.QLBL_Classes.Header = $"QLBL Class({QLBL.Class})";
        }
        //remove, save, load QLBL

        public static void Reset(object sender, RoutedEventArgs e)
        {
            var set = new HashSet<QLBL>();
            foreach (var qlbl in RenderedQLBL)
            {
                set.Add(qlbl);
            }
            foreach (var qlbl in set)
            {
                qlbl.DeRender();
            }
            CurrentLabels = new Dictionary<int, HashSet<QLBL>>();
            Refresh();
        }
        public static void SaveQLBL(object sender, RoutedEventArgs e)
        {
            //PathLabel.Content = "aaa";
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "Select output folder";
            DialogResult result = folderBrowserDialog.ShowDialog();
            string selectedFilePath = System.IO.Path.Combine(folderBrowserDialog.SelectedPath, $"{System.IO.Path.GetFileNameWithoutExtension(VideoWindow.PATH)}.qlbl");
            using (StreamWriter writer = new StreamWriter(selectedFilePath))
            {
                foreach (var framenum in CurrentLabels.Keys)
                {
                    writer.WriteLine(framenum);
                    foreach (var qlbl in CurrentLabels[framenum])
                    {
                        writer.WriteLine($"{qlbl.label_class} {qlbl.at.X} {qlbl.at.Y}");
                    }
                }

            }
        }
        public static void LoadQLBL(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select Quick Label File",
                Filter = "QLBL files (*.qlbl)|*.qlbl|All files (*.*)|*.*"
            };


            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                int frame = 0;
                var filepath = openFileDialog.FileName;
                CurrentLabels = new Dictionary<int, HashSet<QLBL>>();
                using (StreamReader sr = new StreamReader(filepath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        var parts = line.Split(' ');
                        if (parts.Length == 1)
                        {
                            if (int.TryParse(parts[0], out int tmp))
                            {
                                frame = tmp;
                                CurrentLabels[frame] = new HashSet<QLBL>();
                            }
                        }
                        else if (parts.Length == 3)
                        {
                            if (int.TryParse(parts[0], out int c) && int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int y))
                            {
                                CurrentLabels[frame].Add(new QLBL(new System.Windows.Point(x, y)));
                            }
                        }
                        else
                        {
                            print($"{line} is invalid");
                            break;
                        }
                    }
                }
                Refresh();
            }
        }
        public static void Scan(object sender, RoutedEventArgs e)
        {
            int missingcount = 0;
            int seeklocation = -1;

            for (int i = 0; i < VideoWindow.FrameCount; i++)
            {
                if (!CurrentLabels.ContainsKey(i))
                {
                    missingcount++;
                    if(seeklocation == -1)
                    {
                        seeklocation = i;
                    }
                }
            }
            if(missingcount == 0)
            {
                print($"all frames are labeled");
            }
            else
            {
                print($"{missingcount} frames are missing, starting at {seeklocation}");
                VideoWindow.Singleton.Seek(seeklocation );
            }
        }
    }
}
