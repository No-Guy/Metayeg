using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Windows.Media.Media3D;
using System.IO;
using static System.Windows.Forms.DataFormats;
using System.Security.Cryptography;
using System.Windows.Threading;
using Metayeg;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System;
using static System.Windows.Forms.AxHost;
using static System.Windows.Forms.LinkLabel;
using System.Windows.Forms.VisualStyles;
using static System.Net.Mime.MediaTypeNames;


namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static Dictionary<int, (string, RectColor)> classes = new Dictionary<int, (string, RectColor)>();
        private DispatcherTimer timer;
        private DispatcherTimer Dragtimer;
        public static MainWindow Singleton;
        public MainWindow()
        {
            InitializeComponent();
            Singleton = this;
            //classes[0] = ("person",new RectColor(150,0,0,200));
            LoadClasses();
            NewImage();
            Network.GenerateNetworks();
            Opened.MouseDown += Opened_MouseDown;
            Opened.MouseUp += Opened_MouseUp;
            timer = new DispatcherTimer();
            Dragtimer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(10); // Adjust the interval as needed
            timer.Tick += UpdateMouseHeld;
            Dragtimer.Interval = TimeSpan.FromMilliseconds(10);
            Dragtimer.Tick += UpdateDragRect;
            LoadLabel = true;
            //LoadLablingCB.IsChecked = true;
            OriginalImageSize = new Vector(Opened.Width, Opened.Height);
            OriginalWindowSize = new Vector(Width, Height);
            //OriginalOffset2 = offset2((0, 0));
            Tranforms.GetTransform();

        }
        public static Vector OriginalImageSize;

        public static (double, double) OriginalOffset2;

        public static Vector OriginalWindowSize;
        public struct YOLORect
        {
            public double x, y;
            public double w, h;
            public int c;

            public (double, double) Cor1;
            public (double, double) Cor2;
            public YOLORect(double x0, double y0, double w0, double h0, int c0)
            {
                x = x0;
                y = y0;
                w = w0;
                h = h0;
                c = c0;
                Cor1 = (-1, -1);
                Cor2 = (-1, -1);
            }
            public YOLORect(double x0, double y0, double w0, double h0, int c0, (double, double) S1, (double, double) S2)
            {
                x = x0;
                y = y0;
                w = w0;
                h = h0;
                c = c0;
                Cor1 = S1;
                Cor2 = S2;
            }
            public override string ToString()
            {
                return $"{(int)c} {Math.Round(x, 4)} {Math.Round(y, 4)} {Math.Round(w, 4)} {Math.Round(h, 4)}";
            }

        }
        public struct RectColor
        {
            public byte r;
            public byte g;
            public byte b;
            public byte a;
            public RectColor(byte r0, byte g0, byte b0, byte a0)
            {
                r = r0;
                g = g0; b = b0;
                a = a0;
            }
            public override string ToString()
            {
                return $"r: {r}, g: {g}, b: {b}, a: {a}";
            }
        }
        public static bool LoadLabel = true;
        public static string PATH = "";
        public static string ClassesFilePath = "";
        public static string labelsFolder = "";
        public static (double, double)[] SelectedLocations = new (double, double)[2];
        public static int CurrentID = 0;
        public static System.Windows.Controls.Image[] LocationImages = new System.Windows.Controls.Image[2];
        public System.Windows.Controls.Image? CurrentRect;
        //public static List<System.Windows.Controls.Image> RectImages = new List<System.Windows.Controls.Image>();
        //public static List<YOLORect> CreatedRectangles = new List<YOLORect>();
        public int RectCount = 0;
        public static int SelectedID = 0;
        public static readonly int MARGINWIDTH = 4;
        public static int CurrentClass = 0;
        public void NewImage()
        {
            DontSaveRect();
            ResetLocations(true);
            //RectText.DestroyAll();
            LastRect.Content = "";
            PixelLocation.Content = "";
            LocationImages = new System.Windows.Controls.Image[2];
            SelectedLocations = new (double, double)[2];
            CurrentID = 0;
            SelectedID = -1;
            SidebarTitle.Content = "";

        }
        public void ChangeClass(int c)
        {

            if (classes.ContainsKey(c))
            {
                CurrentClass = c;
                Class_TextBox.Text = $"{CurrentClass}({classes[CurrentClass].Item1})";
                ChangeClassColor();
                foreach (var KV in ClassesOptions)
                {
                    if (KV.Value == c)
                    {
                        ClassChooser.SelectedItem = KV.Key;
                    }
                }

            }
        }
        public void LoadClasses()
        {
            string executableDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Path to the text file
            string filePath = System.IO.Path.Combine(executableDirectory, "Classes.txt");
            ClassesFilePath = filePath;
            if (!File.Exists(filePath))
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("person 0");
                    writer.WriteLine("test -1");

                    // You can write more content if needed.
                }
            }
            // Read the contents of the text file
            if (File.Exists(filePath))
            {
                try
                {
                    // Open the file for reading
                    Random random = new Random();
                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            var parts = line.Split(" ");
                            if (parts.Length >= 2 && parts.Length < 5)
                            {
                                int number;
                                if (int.TryParse(parts[1], out number))
                                {
                                    classes[number] = (parts[0], new RectColor((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256), 200));

                                }
                            }
                            else if (parts.Length == 5)
                            {
                                if (int.TryParse(parts[1], out int number) && byte.TryParse(parts[2], out byte r) && byte.TryParse(parts[3], out byte g) && byte.TryParse(parts[4], out byte b))
                                {
                                    classes[number] = (parts[0], new RectColor(r, g, b, 200));
                                }
                            }
                        }
                    }
                    SetClassChooser();
                }
                catch
                {

                }
            }

        }
        public static Dictionary<string, int> ClassesOptions;
        public void SetClassChooser()
        {
            ClassesOptions = new Dictionary<string, int>();
            List<string> options = new List<string>();
            string? Class0 = null;
            int firstClass = 0;
            foreach (var KV in classes)
            {
                string name = $"{KV.Value.Item1}({KV.Key})";
                options.Add(name);
                if (Class0 == null)
                {
                    Class0 = name;
                    firstClass = KV.Key;
                }
                ClassesOptions[name] = KV.Key;
            }
            ClassChooser.ItemsSource = options;
            ClassChooser.SelectedItem = Class0;

            CurrentClass = firstClass;
            Class_TextBox.Text = $"{CurrentClass}({classes[CurrentClass].Item1})";
            ChangeClassColor();
        }
        private void ChangeClassDropDown(object sender, SelectionChangedEventArgs e)
        {
            // Get the selected item
            System.Windows.Controls.ComboBox comboBox = sender as System.Windows.Controls.ComboBox;
            string? selectedOption = comboBox.SelectedItem as string;
            if (selectedOption != null)
            {
                CurrentClass = ClassesOptions[selectedOption];
                Class_TextBox.Text = $"{CurrentClass}({classes[CurrentClass].Item1})";
                ChangeClassColor();
            }
        }
        public static VideoWindow? vw;
        private void OpenVideoWindow(object sender, RoutedEventArgs e)
        {
            vw = new VideoWindow();
            vw.Show();
        }
            private async void Button_Click(object sender, RoutedEventArgs e)
        {
            //PathLabel.Content = "aaa";
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "Select a folder:";
            DialogResult result = folderBrowserDialog.ShowDialog();

            //if (result == DialogResult.OK)
            //{
            string selectedFolderPath = folderBrowserDialog.SelectedPath;
            PathLabel.Content = selectedFolderPath;
            PATH = selectedFolderPath;
            labelsFolder = System.IO.Path.Combine(PATH, "labels");
            ImageObj.CreateImages();
            if (ImageObj.Images.Count > 0)
            {
                SetSource(ImageObj.Images[0].PicturePath);
                ImageObj.Shown = ImageObj.Images[0];
                RectText.location = 0;
                ImageObj.ShownInt = 0;
                PathLabel.Content = ImageObj.Shown.PicturePath;
            }
            await Task.Delay(100);
            if (ImageObj.Shown != null)
            {
                TryLoad();
            }
            if (ImageObj.Shown != null)
            {
                SidebarTitle.Content = ImageObj.Shown.name + ".txt";
                int charcount = ImageObj.Shown.name.Length;
                if (charcount < 22)
                {
                    SidebarTitle.FontSize = 14;
                }
                else if (charcount < 26)
                {
                    SidebarTitle.FontSize = 12;
                }
                else if (charcount < 31)
                {
                    SidebarTitle.FontSize = 10;
                }

            }
            else
            {
                SidebarTitle.Content = "";
            }
            UpdateImageCounter();
            //}
        }
        public void TryLoad()
        {
            Dictionary<int, int>? transform = null;
            /*
            if (File.Exists(System.IO.Path.Combine(PATH, "_transform.txt")))
            {
                using (StreamReader reader = new StreamReader(System.IO.Path.Combine(PATH, "_transform.txt")))
                {
                    string line;
                    transform = new Dictionary<int, int>();
                    while ((line = reader.ReadLine()) != null)
                    {
                        var parts = line.Split(" ");
                        if (parts.Length == 2)
                        {
                            if (int.TryParse(parts[0], out int from) && int.TryParse(parts[1], out int to))
                            {
                                transform[from] = to;
                            }
                        }
                    }
                }
            }
            */
            if (Tranforms.TransformEnabled)
            {
                transform = Tranforms.CreatedTransform;
            }
            if (LoadLabel)
            {
                var Path = System.IO.Path.Combine(labelsFolder, ImageObj.Shown.name + ".txt");
                if (File.Exists(Path))
                {
                    RectText.DestroyAll();
                    try
                    {
                        using (StreamReader reader = new StreamReader(Path))
                        {
                            string line;
                            
                            while ((line = reader.ReadLine()) != null)
                            {
                                var parts = line.Split(" ");
                                
                                if (parts.Length == 5)
                                {
                                    if (int.TryParse(parts[0], out int cls) && double.TryParse(parts[1], out double x) && double.TryParse(parts[2], out double y) && double.TryParse(parts[3], out double w) && double.TryParse(parts[4], out double h))
                                    {
                                        if (transform != null)
                                        {
                                            if (transform.ContainsKey(cls))
                                            {
                                                if (cls != -1)
                                                {
                                                    cls = transform[cls];
                                                }
                                                else
                                                {
                                                    continue;
                                                }
                                            }
                                        }
                                        var topLeft = Opened.PointToScreen(new System.Windows.Point(0, 0));
                                        var globaltopleft = ProjGrid.PointToScreen(new System.Windows.Point(0, 0));
                                        var Rect = new YOLORect(x, y, w, h, cls);
                                        x *= Opened.Source.Width;
                                        w *= Opened.Source.Width;
                                        y *= Opened.Source.Height;
                                        h *= Opened.Source.Height;


                                        var corner1 = (x + w / 2, y + h / 2);
                                        var corner2 = (x - w / 2, y - h / 2);
                                        //System.Windows.MessageBox.Show($"{globaltopleft.X - topLeft.X},{globaltopleft.Y - topLeft.Y}");
                                        BuildRectEXT(offset(inApp(corner1)), offset(inApp(corner2)), Rect);

                                    }
                                }

                            }
                        }
                    }
                    catch(Exception ex){
                        print(ex);
                    }
                }

            }
        }
        public ((double, double), (double, double)) YoloRectToCorners(YOLORect r)
        {
            var x = r.x; var y = r.y; var w = r.w; var h = r.h;
            x *= Opened.Source.Width;
            w *= Opened.Source.Width;
            y *= Opened.Source.Height;
            h *= Opened.Source.Height;
            x -= 11; y -= 11;   //test

            var topLeft = Opened.PointToScreen(new System.Windows.Point(0, 0));
            var globaltopleft = ProjGrid.PointToScreen(new System.Windows.Point(0, 0));
            var corner1 = (x + w / 2, y + h / 2);
            var corner2 = (x - w / 2, y - h / 2);
            return (offset(inApp(corner1)), offset(inApp(corner2)));

        }
        public (double, double) offset((double, double) a)
        {
            var topLeft = Opened.PointToScreen(new System.Windows.Point(0, 0));
            var globaltopleft = ProjGrid.PointToScreen(new System.Windows.Point(0, 0));
            return (a.Item1 + Math.Abs(topLeft.X - globaltopleft.X), a.Item2 + Math.Abs(topLeft.Y - globaltopleft.Y));
        }
        public (double, double) offset2((double, double) a, bool reverse = false)
        {
            var topLeft = Opened.PointToScreen(new System.Windows.Point(0, 0));
            var globaltopleft = ProjGrid.PointToScreen(new System.Windows.Point(0, 0));
            var d = topLeft - globaltopleft - new Vector(10, 10);
            return (a.Item1 + d.X * (reverse ? -1 : 1), a.Item2 + d.Y * (reverse ? -1 : 1));
        }
        private void DeleteLast(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (CurrentID > 0)
                {
                    ResetLocations();
                    DontSaveRect();
                }
                else if (RectText.Rectangles.Count > 0 && CurrentID == 0)
                {
                    RectText.Rectangles[RectText.Rectangles.Count - 1].Destroy();
                }

                ChangeCompletedCollision(true);
            }
            if (e.Key == Key.Delete)
            {
                DeleteImage(null, null);
            }

            if (e.Key == Key.D)
            {
                NextPrev(NextButton, null);
            }
            if (e.Key == Key.A)
            {
                NextPrev(PreviousButton, null);
            }
            if (e.Key == Key.Return && Class_TextBox.IsFocused)
            {
                CBLF();
                //Keyboard.ClearFocus();

                // OR, set focus to the main window
                //System.Windows.Application.Current.MainWindow.Focus();
            }
        }
        public async void NextPrev(object sender, RoutedEventArgs e)
        {
            NextPrev(sender, e, false);
        }

        public async void NextPrev(object sender, RoutedEventArgs e, bool DontSaveFlag = false)
        {
            NewImage();
            if (ImageObj.Images.Count > 1)
            {
                if (sender == NextButton)
                {

                    Export();
                    ImageObj.ShownInt++;
                    if (DestroyOnNext)
                    {
                        RectText.DestroyAll();
                    }
                }
                else
                {
                    Export();
                    RectText.DestroyAll();
                    ImageObj.ShownInt--;
                }

                if (ImageObj.ShownInt == ImageObj.Images.Count)
                {
                    ImageObj.ShownInt = 0;
                }
                else if (ImageObj.ShownInt == -1)
                {
                    ImageObj.ShownInt = ImageObj.Images.Count - 1;
                }
                RectText.location = 0;
                ImageObj.Shown = ImageObj.Images[ImageObj.ShownInt];
                SetSource(ImageObj.Shown.PicturePath);
                await Task.Delay(100);
                //RectText.DestroyAll();
                if (ImageObj.Shown != null)
                {
                    TryLoad();
                    PathLabel.Content = ImageObj.Shown.PicturePath;
                    SidebarTitle.Content = ImageObj.Shown.name + ".txt";
                }
                else
                {
                    SidebarTitle.Content = "";
                }
                UpdateImageCounter();
            }
        }
        private void SetSource(string path)
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path, UriKind.Absolute); // Replace with your image path
            bitmap.CacheOption = BitmapCacheOption.OnLoad; // Release file lock
            bitmap.EndInit();
            Opened.Source = bitmap;
            //File.Delete(path);
        }
        public void UpdateImageCounter()
        {
            if (ImageObj.Shown != null)
            {
                ImageCounter.Text = $"{ImageObj.ShownInt + 1}/{ImageObj.Images.Count}";
            }
            else
            {
                ImageCounter.Text = "";
            }
        }
        public void DeleteAllButtonFunction(object sender, RoutedEventArgs e)
        {
            RectText.DestroyAll();
            ResetLocations();
            DontSaveRect();
        }

        public void UpdateLocation(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (ImageObj.Shown != null)
            {
                System.Windows.Point mousePosition = e.GetPosition(Opened);

                double imageWidth = Opened.ActualWidth;
                double imageHeight = Opened.ActualHeight;
                int xInImage = round((mousePosition.X / imageWidth) * Opened.Source.Width);
                int yInImage = round((mousePosition.Y / imageHeight) * Opened.Source.Height);
                PixelLocation.Content = $"Selected Location: {(round(inImage((mousePosition.X, mousePosition.Y)).Item1), round(inImage((mousePosition.X, mousePosition.Y)).Item2))}";
            }
        }


        private void Opened_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ImageObj.Shown != null && e.ChangedButton == MouseButton.Left)
            {
                ChangeCompletedCollision(false);
                if (CurrentID == 2)
                {
                    ResetLocations();
                    SelectedID = 0;
                }
                mousehold = true;
                if (SelectedID == -1)
                {
                    SelectedID = CurrentID;
                }

                timer.Start();

            }
            else if (e.ChangedButton == MouseButton.Right && CurrentID == 2)
            {
                CompleteRect();
            }
            Xbuttons(e);
        }
        private void Xbuttons(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.XButton1) //M4
            {
                NextPrev(PreviousButton, null);
            }
            else if (e.ChangedButton == MouseButton.XButton2) //M5
            {
                NextPrev(NextButton, null);
            }
        }

        private void Opened_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (ImageObj.Shown != null && e.ChangedButton == MouseButton.Left && mousehold && !Dragtimer.IsEnabled)
            {
                mousehold = false;
                timer.Stop();
                ResetLocations(false);
                DontSaveRect();
                GetLocation();
            }

            if (CurrentRect != null)
            {
                CurrentRect.IsHitTestVisible = SelectedID == -1;
            }

        }
        public static void print(object message)
        {
            System.Windows.MessageBox.Show(message.ToString());
        }
        private void SelectX(object sender, RoutedEventArgs e)
        {
            ChangeCompletedCollision(false);
            int vid = -1;
            for (int i = 0; i < LocationImages.Length; i++)
            {
                if (sender == LocationImages[i])
                {
                    vid = i; break;
                }
            }
            if (vid != -1)
            {
                SelectedID = vid;
                CurrentID = 1;
                LocationImages[vid].IsHitTestVisible = false;
                mousehold = true;
                timer.Start();
                if (CurrentRect != null)
                {
                    CurrentRect.IsHitTestVisible = SelectedID == -1;
                }
            }
        }
        private void UpdateMouseHeld(object sender, EventArgs e)
        {
            if (mousehold)
            {
                if (Mouse.LeftButton == MouseButtonState.Pressed)
                {
                    //ResetLocations(SelectedID == 0 || SelectedID == 1 );
                    ResetLocations(CurrentID == 2 ? true : false);
                    DontSaveRect();
                    System.Windows.Point mousePosition = new System.Windows.Point();
                    mousePosition = Mouse.GetPosition(Opened);
                    //System.Windows.MessageBox.Show($"{mousePosition}");
                    double imageWidth = Opened.ActualWidth;
                    double imageHeight = Opened.ActualHeight;

                    //int xInImage = round((mousePosition.X / imageWidth) * Opened.Source.Width);
                    //int yInImage = round((mousePosition.Y / imageHeight) * Opened.Source.Height);

                    SelectedLocations[SelectedID] = ClampMousePosition((mousePosition.X, mousePosition.Y));
                    UpdateLocations(CurrentID + 1);
                }
                else
                {
                    mousehold = false;
                    timer.Stop();
                    ResetLocations(false);
                    DontSaveRect();
                    GetLocation();
                }
            }

        }

        private (double, double) ClampMousePosition((double, double) mousePosition)
        {
            mousePosition = offset2(mousePosition);
            var baseoffset2 = offset2((0, 0));
            var inapp = inApp((Opened.Source.Width, Opened.Source.Height));
            return (Math.Clamp(mousePosition.Item1, baseoffset2.Item1, inapp.Item1 + baseoffset2.Item1), Math.Clamp(mousePosition.Item2, baseoffset2.Item2, inapp.Item2 + baseoffset2.Item2));
        }
        private bool mousehold = true;
        public void OpenZoomWindow(object sender, RoutedEventArgs e)
        {
            var zoomwindow = new Zoomedwindow();
            zoomwindow.Show();
        }
        //MouseDown="GetLocation"
        private void GetLocation()
        {
            if (CurrentID == 2)
            {
                ResetLocations();
                DontSaveRect();
            }
            if (ImageObj.Shown != null && SelectedID != -1)
            {
                ChangeCompletedCollision(false);
                System.Windows.Point mousePosition = Mouse.GetPosition(Opened);

                double imageWidth = Opened.ActualWidth;
                double imageHeight = Opened.ActualHeight;

                //int xInImage = round((mousePosition.X / imageWidth) * Opened.Source.Width);
                //int yInImage = round((mousePosition.Y / imageHeight) * Opened.Source.Height);
                SelectedLocations[SelectedID] = ClampMousePosition((mousePosition.X, mousePosition.Y));
                //PixelLocation.Content = $"Selected Location: {inImage(SelectedLocations[CurrentID])}";
                CurrentID++;
                SelectedID = -1;
                UpdateLocations();
            }


        }
        private void GenericMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && timer.IsEnabled)
            {
                Opened_MouseUp(sender, e);
            }
        }
        private (double, double) StartingDragLocation;
        private void DragStart(object sender, MouseButtonEventArgs e)
        {
            //print(CollisionStatus);
            ///System.Windows.MessageBox.Show("Drag Satart");
            if (e.ChangedButton == MouseButton.Right && CurrentRect != null)
            {
                CompleteRect();
            }
            else if (e.ChangedButton == MouseButton.Left)
            {
                ResetLocations(false);
                StartingDragLocation = (Mouse.GetPosition(Opened).X, Mouse.GetPosition(Opened).Y);
                Dragtimer.Start();
                ChangeCompletedCollision(false);
            }
        }
        private void UpdateDragRect(object sender, EventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                var clampedmousepos = (Mouse.GetPosition(Opened).X, Mouse.GetPosition(Opened).Y);
                (double, double) DeltaLocation = (clampedmousepos.Item1 - StartingDragLocation.Item1, clampedmousepos.Item2 - StartingDragLocation.Item2);
                SelectedLocations[0] = (SelectedLocations[0].Item1 + DeltaLocation.Item1, SelectedLocations[0].Item2 + DeltaLocation.Item2);
                SelectedLocations[1] = (SelectedLocations[1].Item1 + DeltaLocation.Item1, SelectedLocations[1].Item2 + DeltaLocation.Item2);
                ProjGrid.Children.Remove(LocationImages[0]);
                ProjGrid.Children.Remove(LocationImages[1]);
                //ResetLocations();
                DontSaveRect();
                UpdateLocations();

                StartingDragLocation = (Mouse.GetPosition(Opened).X, Mouse.GetPosition(Opened).Y);
            }
            else
            {
                ChangeCompletedCollision(CurrentID == 0);
                Dragtimer.Stop();
            }

        }
        public (double, double) inImage((double, double) mouse_position)
        {
            mouse_position = offset2(mouse_position, true);
            double imageWidth = Opened.ActualWidth;
            double imageHeight = Opened.ActualHeight;
            return ((mouse_position.Item1 / imageWidth) * Opened.Source.Width, (mouse_position.Item2 / imageHeight) * Opened.Source.Height);

        }
        public (double, double) inApp((double, double) image_location)
        {
            double imageWidth = Opened.ActualWidth;
            double imageHeight = Opened.ActualHeight;
            return ((image_location.Item1 * imageWidth) / Opened.Source.Width, (image_location.Item2 * imageHeight) / Opened.Source.Height);

        }
        public void UpdateLocations(int cid = -1)
        {
            if (cid == -1)
            {
                cid = CurrentID;
            }

            for (global::System.Int32 j = 0; j < cid; j++)
            {
                var i = new System.Windows.Controls.Image();
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                if (j == SelectedID)
                {
                    bi.UriSource = new Uri(@"/xcen.png", UriKind.Relative);
                }
                else
                {
                    bi.UriSource = new Uri(@"/xcor.png", UriKind.Relative);
                }
                bi.EndInit();
                i.Source = bi;
                i.Name = "test_image";
                i.Width = 20;
                i.Height = 20;
                System.Windows.Controls.Panel.SetZIndex(i, 3);
                ProjGrid.Children.Add(i);
                LocationImages[j] = i;
                i.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                i.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                if (mousehold)
                {
                    i.IsHitTestVisible = false;
                }
                else
                {
                    i.MouseDown += SelectX;
                }
                // Center the image on the mouse position
                double imageX = (SelectedLocations[j].Item1);// * Opened.ActualWidth / Main_Window.ActualWidth);// + i.Width / 2;
                double imageY = (SelectedLocations[j].Item2);// + i.Height / 2;

                //var inapp = inApp((Opened.Source.Width, Opened.Source.Height));
                // Update the image's position
                i.Margin = new Thickness(imageX, imageY, 0, 0);

                // Show the image
                i.Visibility = Visibility.Visible;
            }
            if (cid == 2)
            {
                BuildRect();
            }
        }
        public void ResetLocations(bool ResetCID = true)
        {
            if (ResetCID)
            {
                CurrentID = 0;
            }
            ProjGrid.Children.Remove(LocationImages[0]);
            ProjGrid.Children.Remove(LocationImages[1]);
            LocationImages[0] = null;
            LocationImages[1] = null;
        }
        public void DontSaveRect()
        {
            if (CurrentRect != null)
            {
                ProjGrid.Children.Remove(CurrentRect);
            }
        }
        public void ClampCorners()
        {
            var inapp = inApp((Opened.Source.Width, Opened.Source.Height));
            var valoffset = offset2((0, 0));
            SelectedLocations[0] = (Math.Clamp(SelectedLocations[0].Item1, valoffset.Item1, inapp.Item1 + valoffset.Item1), Math.Clamp(SelectedLocations[0].Item2, valoffset.Item2, inapp.Item2 + valoffset.Item2));
            SelectedLocations[1] = (Math.Clamp(SelectedLocations[1].Item1, valoffset.Item1, inapp.Item1 + valoffset.Item1), Math.Clamp(SelectedLocations[1].Item2, valoffset.Item2, inapp.Item2 + valoffset.Item2));
        }
        public void BuildRect()
        {
            //clamp corners//
            ClampCorners();
            //print(SelectedLocations[1]);
            var i = new System.Windows.Controls.Image();
            i.Width = Math.Abs((SelectedLocations[0].Item1 - SelectedLocations[1].Item1));
            i.Height = Math.Abs((SelectedLocations[0].Item2 - SelectedLocations[1].Item2));
            int width = (int)i.Width;
            int height = (int)i.Height;
            i.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            i.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            i.Margin = new Thickness(SelectedLocations[0].Item1 + LocationImages[0].Width / 2 - i.Width * ((SelectedLocations[0].Item1 > SelectedLocations[1].Item1) ? 1 : 0), SelectedLocations[0].Item2 + LocationImages[0].Height / 2 - i.Height * ((SelectedLocations[0].Item2 > SelectedLocations[1].Item2) ? 1 : 0), 0, 0);
            i.Name = $"Rect_{RectCount}";
            i.IsHitTestVisible = false;
            CurrentRect = i;
            i.MouseDown += DragStart;
            ProjGrid.Children.Add(i);
            System.Windows.Controls.Panel.SetZIndex(i, 1);
            setRectColor(i, 255, 0, 0, 1);
            ChangeCompletedCollision(SelectedID == -1);
            var corner1 = inImage(SelectedLocations[0]);
            var corner2 = inImage(SelectedLocations[1]);
            LastRect.Content = $"Current: (({Math.Round(corner1.Item1)},{Math.Round(corner1.Item2)}), ({Math.Round(corner2.Item1)},{Math.Round(corner2.Item2)}))";
        }
        public void BuildRectEXT((double, double) cor1, (double, double) cor2, YOLORect r, RectText? Obj = null)
        {
            int c = r.c;
            var i = new System.Windows.Controls.Image();
            i.Width = Math.Abs(cor1.Item1 - cor2.Item1);
            i.Height = Math.Abs(cor1.Item2 - cor2.Item2);
            i.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            i.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            i.Margin = new Thickness(cor1.Item1 - i.Width * ((cor1.Item1 > cor2.Item1) ? 1 : 0), cor1.Item2 - i.Height * ((cor1.Item2 > cor2.Item2) ? 1 : 0), 0, 0);
            i.Name = $"Rect_{RectCount}";
            i.IsHitTestVisible = false;

            i.MouseDown += ResetRect;
            i.MouseEnter += RectMouseEnter;
            i.MouseLeave += RectMouseLeave;
            ProjGrid.Children.Add(i);
            System.Windows.Controls.Panel.SetZIndex(i, 1);
            i.IsHitTestVisible = SelectedID == -1 && EditModeFlag;
            if (!classes.ContainsKey(c))
            {
                AddClass(c);
            }
            setRectColor(i, classes[c].Item2);
            if (Obj == null)
            {
                new RectText(r, i);
            }
            else
            {
                Obj.image = i;
            }
        }
        public static void setRectColor(System.Windows.Controls.Image i, RectColor C, int mw = -1)
        {
            if (mw == -1)
            {
                setRectColor(i, C.r, C.g, C.b, MARGINWIDTH);
            }
            else
            {
                setRectColor(i, C.r, C.g, C.b, mw);
            }
        }
        private static void setRectColor(System.Windows.Controls.Image i, byte r, byte g, byte b, int mw, byte alpha = 200)
        {
            int width = (int)i.Width;
            int height = (int)i.Height;
            PixelFormat pixelFormat = PixelFormats.Bgra32;
            try
            {
                var writeablebi = new WriteableBitmap(
                    (int)i.Width,
                    (int)i.Height,
                    96,
                    96,
                    PixelFormats.Bgra32,
                    null);


                byte[] pixels = new byte[width * height * 4];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = (y * 4 * width) + (x * 4);
                        // Set the pixel color (B, G, R, A) - here, we set it to red
                        pixels[index + 0] = b; // Blue
                        pixels[index + 1] = g; // Green
                        pixels[index + 2] = r; // Red
                        pixels[index + 3] = (byte)(alpha * ((x < mw || y < mw || y > height - 1 - mw || x > width - 1 - mw) ? 1 : 0)); // Alpha 
                    }
                }
                writeablebi.WritePixels(new Int32Rect(0, 0, width, height), pixels, 4 * width, 0);
                i.Source = writeablebi;
            }
            catch
            {
                Singleton.ResetLocations();
            }
        }
        public void CompleteRect()
        {
            setRectColor(CurrentRect, classes[CurrentClass].Item2.r, classes[CurrentClass].Item2.g, classes[CurrentClass].Item2.b, MARGINWIDTH, classes[CurrentClass].Item2.a);
            ResetLocations();

            CurrentRect.MouseDown += ResetRect;
            CurrentRect.MouseEnter += RectMouseEnter;
            CurrentRect.MouseLeave += RectMouseLeave;
            CurrentRect.MouseDown -= DragStart;
            ChangeCompletedCollision(true);
            var corner1 = inImage(SelectedLocations[0]);
            var corner2 = inImage(SelectedLocations[1]);
            double Width = ((double)Math.Abs(corner1.Item1 - corner2.Item1)) / Opened.Source.Width;
            double Height = ((double)Math.Abs(corner1.Item2 - corner2.Item2)) / Opened.Source.Height;
            double x = (((double)Math.Abs(corner1.Item1 + corner2.Item1)) / 2d) / Opened.Source.Width;
            double y = (((double)Math.Abs(corner1.Item2 + corner2.Item2)) / 2d) / Opened.Source.Height;
            RectCount++;
            CurrentRect.IsHitTestVisible = EditModeFlag;
            new RectText(new YOLORect(x, y, Width, Height, CurrentClass, SelectedLocations[0], SelectedLocations[1]), CurrentRect);
            CurrentRect = null;
            LastRect.Content = $"Last: <x: {Math.Round(x, 2)},y: {Math.Round(y, 2)},w: {Math.Round(Width, 2)},h: {Math.Round(Height, 2)}, c: {CurrentClass}>";
        }
        public void RectMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (CurrentID == 0)
            {
                RectText.OnMouseEnter(((RectText)((System.Windows.Controls.Image)sender).Tag).label, e);
            }
        }
        public void RectMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            RectText.OnMouseLeave(((RectText)((System.Windows.Controls.Image)sender).Tag).label, e);
        }
        public static int round(double x)
        {
            if (x - (int)x > 0.5)
            {
                return (int)x + 1;
            }
            return (int)x;
        }
        private void ClassBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CBLF();


        }
        public static bool EditModeFlag = false;
        private void EnableEditMode(object sender, RoutedEventArgs e)
        {
            EditModeFlag = true;
            ChangeCompletedCollision(true);
        }
        private static bool CollisionStatus = false;
        public static void ChangeCompletedCollision(bool b)
        {
            b = b && EditModeFlag;
            if (b != CollisionStatus)
            {
                foreach (var Rect in RectText.Rectangles)
                {
                    Rect.image.IsHitTestVisible = b;
                    Rect.image.MouseDown += Singleton.ResetRect;
                }
                CollisionStatus = b;
            }

        }
        private void ResetRect(object sender, MouseButtonEventArgs e)
        {
            //print(e.ChangedButton);
            if (e.ChangedButton == MouseButton.Left)
            {
                ((RectText)((System.Windows.Controls.Image)sender).Tag).EditRect();
            }
            if (e.ChangedButton == MouseButton.Middle)
            {
                var master = ((RectText)((System.Windows.Controls.Image)sender).Tag);

                master.Data.c = CurrentClass;
                setRectColor(master.image, classes[CurrentClass].Item2);
                master.ChangeLabel();
            }
            Xbuttons(e);
        }
        private void DisableEditMode(object sender, RoutedEventArgs e)
        {
            EditModeFlag = false;
            ChangeCompletedCollision(false);
        }
        private void ChangeClassColor()
        {
            var i = ClassColor;
            int width = (int)i.Width;
            int height = (int)i.Height;
            PixelFormat pixelFormat = PixelFormats.Bgra32;
            try
            {
                var writeablebi = new WriteableBitmap(
                    (int)i.Width,
                    (int)i.Height,
                    20,
                    20,
                    PixelFormats.Bgra32,
                    null);


                byte[] pixels = new byte[width * height * 4];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = (y * 4 * width) + (x * 4);
                        // Set the pixel color (B, G, R, A) - here, we set it to red
                        pixels[index + 0] = classes[CurrentClass].Item2.b; // Blue
                        pixels[index + 1] = classes[CurrentClass].Item2.g; // Green
                        pixels[index + 2] = classes[CurrentClass].Item2.r; // Red
                        pixels[index + 3] = 255; // Alpha 
                    }
                }
                writeablebi.WritePixels(new Int32Rect(0, 0, width, height), pixels, 4 * width, 0);
                i.Source = writeablebi;
            }
            catch { }
        }
        private void CBLF()
        {
            string editedText = Class_TextBox.Text;
            var split = editedText.Split(' ');

            int number;
            if (split.Length == 1 && int.TryParse(editedText, out number) || split.Length == 2 && int.TryParse(split[0], out number))
            {
                CurrentClass = number;


            }
            else
            {
                Class_TextBox.Text = $"{CurrentClass}({classes[CurrentClass].Item1})";
                ChangeClassColor();
                return;
            }
            if (classes.ContainsKey(CurrentClass))
            {
                if (split.Length == 2)
                {
                    AddClass(number, split[1]);
                }
                Class_TextBox.Text = $"{CurrentClass}({classes[CurrentClass].Item1})";
                ChangeClassColor();
            }
            else
            {

                string name = split.Length == 1 ? "unknown" : split[1];
                AddClass(number, name);
            }
        }
        public void AddClass(int number, string name = "unknown")
        {
            var random = new Random();
            AddClass(number, new RectColor((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256), 200), name);
        }
        public void AddClass(int number, RectColor color, string name = "unknown")
        {
            //check if file has class
            string[] lines = File.ReadAllLines(ClassesFilePath);
            int SelectedLine = -1;
            int i = 0;
            string modname = $"{name}({number})";
            ClassesOptions[modname] = number;
            var e = ClassChooser.ItemsSource;
            var f = (List<string>)e;
            f.Add(modname);
            ClassChooser.ItemsSource = f;
            foreach (string line in lines)
            {
                if (int.TryParse(line.Split(' ')[1], out int lineclass))
                {
                    if (lineclass == number)
                    {
                        SelectedLine = i;
                    }
                }
                i++;
            }
            if (SelectedLine >= 0 && SelectedLine <= lines.Length)
            {
                // Create a new array without the line to delete
                string[] updatedLines = new string[lines.Length - 1];
                int updatedIndex = 0;

                for (int j = 0; j < lines.Length; j++)
                {
                    if (j != SelectedLine)
                    {
                        updatedLines[updatedIndex] = lines[j];
                        updatedIndex++;
                    }
                }
                File.WriteAllLines(ClassesFilePath, updatedLines);
            }
            classes[number] = (name, color);
            Class_TextBox.Text = $"{CurrentClass}({classes[CurrentClass].Item1})";
            try
            {
                using (StreamWriter writer = File.AppendText(ClassesFilePath))
                {
                    writer.WriteLine($"{classes[number].Item1} {number} {classes[number].Item2.r} {classes[number].Item2.g} {classes[number].Item2.b}");
                }
            }
            catch { }
            ChangeClassColor();
        }
        private void ClassBox_GotFocus(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.TextBox textBox = (System.Windows.Controls.TextBox)sender;
            textBox.Text = "";
        }
        public void SetVideo(object sender, RoutedEventArgs e)
        {
            Videos.GetVideoPath();
        }
        public void Export(object sender, RoutedEventArgs e)
        {
            Export();
        }
        public static string[] ImageExtensions = new string[] { ".png", ".jpeg", ".jpg" };
        private void CheckConsistency(object sender, RoutedEventArgs _)
        {
            try
            {
                string[] files = Directory.GetFiles(labelsFolder);
                HashSet<string> todelete = new HashSet<string>();
                int c = 0;
                foreach (var f in files)
                {
                    string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(f);
                    bool e = false;
                    foreach (string ext in ImageExtensions)
                    {
                        if (File.Exists(System.IO.Path.Join(PATH, fileNameWithoutExtension) + ext))
                        {

                            e = true;
                            break;
                        }
                    }
                    if (!e)
                    {
                        File.Delete(f);
                        c++;
                    }

                }
                if (c == 0)
                {
                    print("All Labels are paired");
                }
                else
                {
                    print($"Deleted {c} unpaired Labels");
                }
                int first = -1;
                c = 0;
                for (int i = 0; i < ImageObj.Images.Count; i++)
                {
                    var f = ImageObj.Images[i].PicturePath;
                    string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(f);
                    if (!File.Exists(System.IO.Path.Join(labelsFolder, fileNameWithoutExtension) + ".txt"))
                    {
                        if (first == -1)
                        {
                            first = i;
                        }
                        c++;
                    }
                }
                if (c > 0)
                {
                    print($"Missing {c} Labels, first in {first + 1}");
                }
                else
                {
                    print($"All Images are paired");
                }
            }
            catch
            {
                print($"Path Error");
            }

        }
        public void Export()
        {
            if (PATH != "" && ImageObj.Shown != null)
            {
                string folderPath = labelsFolder;

                try
                {
                    // Attempt to create the folder
                    Directory.CreateDirectory(folderPath);
                }
                catch
                {

                }
                string OriginalName = ImageObj.Shown.name;

                string newfilepath = System.IO.Path.Combine(folderPath, OriginalName + ".txt");
                using (StreamWriter writer = new StreamWriter(newfilepath))
                {
                    foreach (var CurrentRect in RectText.Rectangles)
                    {
                        writer.WriteLine(CurrentRect.Data);

                    }

                }
            }

        }
        public static bool DestroyOnNext = false;
        private void Load_Checked(object sender, RoutedEventArgs e)
        {
            LoadLabel = !LoadLabel;
        }
        private void DestroyOnNextToggle(object sender, RoutedEventArgs e)
        {
            DestroyOnNext = !DestroyOnNext;
        }
        //v2 changes//
        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RectText.UpdateRectTextsLocations();
            if (ImageObj.Shown == null)
            {
                var w = e.NewSize.Width;
                var h = e.NewSize.Height;
                var ratio = OriginalWindowSize.X / OriginalWindowSize.Y;
                if (w / h > ratio)
                {
                    //height is smaller, use it
                    var mult = h / OriginalWindowSize.Y;
                    Opened.Width = OriginalImageSize.X * mult;
                    Opened.Height = OriginalImageSize.Y * mult;
                }
                else
                {
                    var mult = w / OriginalWindowSize.X;
                    Opened.Width = OriginalImageSize.X * mult;
                    Opened.Height = OriginalImageSize.Y * mult;
                }
                //RectText.Regenerate();
            }
            // print(OriginalImageSize + "  " + OriginalWindowSize);
        }
        private void MainWindow_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            int delta = e.Delta; // This is the amount of mouse wheel movement
            RectText.location -= delta / 120;
            RectText.location = Math.Clamp(RectText.location, 0, Math.Max(RectText.Rectangles.Count - RectText.GetCount(), 0));
            RectText.UpdateRectTextsLocations();
        }
        public void CallToYoloIT(object sender, RoutedEventArgs e)
        {
            Network N = (Network)(((MenuItem)sender).Tag);
            if (N.isYolo)
            {
                YoloIt.Yolo(N);
            }
            /*
            if (YoloIt.Patches > 1)
            {
                YoloIt.CreatePatches();
            }
            else
            {
                YoloIt.Yolo();
            }
            */

        }

        private void ImageCounter_GotFocus(object sender, RoutedEventArgs e)
        {

        }
        private static bool WaitForEnter = false;
        private void ImageCounterWait(object sender, RoutedEventArgs e)
        {
            WaitForEnter = true;
        }
        private void ImageCounterStopWaitWait(object sender, RoutedEventArgs e)
        {
            WaitForEnter = false;
        }
        private void ImageCounterJump(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter && WaitForEnter)
            {
                if (int.TryParse(ImageCounter.Text, out int num))
                {
                    if (num >= 0 && num < ImageObj.Images.Count)
                    {
                        ImageObj.ShownInt = num;
                        NextPrev(PreviousButton, null);
                    }
                }
                UpdateImageCounter();
            }
        }
        public void DeleteImage(object sender, RoutedEventArgs e)
        {
            if (ImageObj.Shown != null)
            {
                var obj = ImageObj.Shown;
                var curpath = ImageObj.Shown.PicturePath;

                var bitmapImage = Opened.Source as BitmapImage;
                bitmapImage.StreamSource = null;
                bitmapImage.UriSource = null;
                Opened.Source = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (File.Exists(curpath))
                {
                    var name = System.IO.Path.GetFileNameWithoutExtension(curpath);
                    var label = System.IO.Path.Join(labelsFolder, name) + ".txt";



                    if (File.Exists(label))
                    {
                        File.Delete(label);
                    }
                    File.Delete(curpath);


                }
                ImageObj.Images.Remove(obj);
                ImageObj.ShownInt++;
                NextPrev(PreviousButton, null);
            }
        }
        public void AddTransform(object sender, RoutedEventArgs e)
        {
            //Tranforms.GetTransform();
        }
    }
}
    