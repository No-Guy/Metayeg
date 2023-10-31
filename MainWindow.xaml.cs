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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using System;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Drawing;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static Dictionary<int,(string,RectColor)> classes = new Dictionary<int,(string, RectColor)>();
        private DispatcherTimer timer;
        public MainWindow()
        {
            InitializeComponent();
            
            //classes[0] = ("person",new RectColor(150,0,0,200));
            LoadClasses();
            NewImage();
            Opened.MouseDown += Opened_MouseDown;
            Opened.MouseUp += Opened_MouseUp;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(10); // Adjust the interval as needed
            timer.Tick += UpdateMouseHeld;
            LoadLabel = true;
            LoadLablingCB.IsChecked = true;


        }
        public struct YOLORect
        {
            public double x, y;
            public double w, h;
            public int c;
            public YOLORect(double x0, double y0, double w0, double h0, int c0 = 0)
            {
                x = x0;
                y = y0;
                w = w0;
                h = h0;
                c = c0;
                    
            }
            
            public override string ToString()
            {
                return $"{(int)c} {Math.Round(x,4)} {Math.Round(y,4)} {Math.Round(w, 4)} {Math.Round(h, 4)}";
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
        }
        public static bool LoadLabel = true;
        public static string PATH = "";
        public static string ClassesFilePath = "";
        public static string labelsFolder = "";
        private static (double, double)[] SelectedLocations = new (double, double)[2];
        private static int CurrentID = 0;
        private static System.Windows.Controls.Image[] LocationImages = new System.Windows.Controls.Image[2];
        private System.Windows.Controls.Image? CurrentRect;
        public static List<System.Windows.Controls.Image> RectImages = new List<System.Windows.Controls.Image>();
        public static List<YOLORect> CreatedRectangles = new List<YOLORect>();
        public int RectCount = 0;
        private int SelectedID = 0;
        public static readonly int MARGINWIDTH = 5;
        public static int CurrentClass = 0;
        public void NewImage()
        {
            DontSaveRect();
            ResetLocations(true);
            LastRect.Content = "";
            PixelLocation.Content = "";
            CreatedRectangles = new List<YOLORect>();
            LocationImages = new System.Windows.Controls.Image[2];
            SelectedLocations = new (double, double)[2];
            CurrentID = 0;
            SelectedID = -1;
            foreach (var RI in RectImages)
            {
                ProjGrid.Children.Remove(RI);

            }
            RectImages = new List<System.Windows.Controls.Image>();
            CurrentClass = 0;
            if (classes.ContainsKey(CurrentClass))
            {
                Class_TextBox.Text = $"{CurrentClass}({classes[CurrentClass].Item1})";
                ChangeClassColor();
            }
            else
            {
                Class_TextBox.Text = $"{CurrentClass}(unknown)";
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
                            if(parts.Length >= 2 && parts.Length < 5)
                            {
                                int number;
                                if (int.TryParse(parts[1], out number))
                                {
                                    classes[number] = (parts[0],new RectColor((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256),200));

                                }
                            }
                            else if(parts.Length == 5)
                            {
                                if (int.TryParse(parts[1], out int number) && byte.TryParse(parts[2], out byte r) && byte.TryParse(parts[3], out byte g) && byte.TryParse(parts[4], out byte b))
                                {
                                    classes[number] = (parts[0], new RectColor(r, g, b, 200));
                                }
                            }
                        }
                    }
                }
                catch
                {
                    
                }
            }

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
                Opened.Source = new BitmapImage(new Uri(ImageObj.Images[0].PicturePath, UriKind.Absolute));
                ImageObj.Shown = ImageObj.Images[0];
                ImageObj.ShownInt = 0;
                PathLabel.Content = ImageObj.Shown.PicturePath;
            }
            await Task.Delay(100);
            if (ImageObj.Shown != null)
            {
                TryLoad();
            }
            //}
        }
        private void TryLoad()
        {
            if (LoadLabel)
            {
                var Path = System.IO.Path.Combine(labelsFolder, ImageObj.Shown.name + ".txt");
                if (File.Exists(Path))
                {
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

                                        x *= Opened.Source.Width;
                                        w *= Opened.Source.Width;
                                        y *= Opened.Source.Height;
                                        h *= Opened.Source.Height;

                                        var topLeft = Opened.PointToScreen(new System.Windows.Point(0, 0));
                                        var globaltopleft = ProjGrid.PointToScreen(new System.Windows.Point(0, 0));
                                        var corner1 = (x + w / 2, y + h / 2);
                                        var corner2 = (x - w / 2, y - h / 2);
                                        //System.Windows.MessageBox.Show($"{globaltopleft.X - topLeft.X},{globaltopleft.Y - topLeft.Y}");
                                        BuildRectEXT(offset(inApp(corner1),topLeft, globaltopleft), offset(inApp(corner2),topLeft, globaltopleft), cls);
                                    }
                                }

                            }
                        }
                    }
                    catch { }
                }
            }
        }
        public static (double,double) offset((double, double) a, System.Windows.Point b, System.Windows.Point c)
        {
            return (a.Item1 + Math.Abs(b.X - c.X), a.Item2 + Math.Abs(b.Y - c.Y));
        }
        private void DeleteLast(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (CreatedRectangles.Count > 0)
                {
                    CreatedRectangles.RemoveAt(CreatedRectangles.Count - 1);
                    ProjGrid.Children.Remove(RectImages[RectImages.Count - 1]);
                    RectImages.RemoveAt(RectImages.Count - 1);
                }
            }
            if(e.Key == Key.Return && Class_TextBox.IsFocused)
            {
                CBLF();
                //Keyboard.ClearFocus();

                // OR, set focus to the main window
                //System.Windows.Application.Current.MainWindow.Focus();
            }
        }
        public async void NextPrev(object sender, RoutedEventArgs e)
        {
            NewImage();
            if (ImageObj.Images.Count > 1)
            {
                if(sender == NextButton)
                {
                    ImageObj.ShownInt++;
                }
                else
                {
                    ImageObj.ShownInt--;
                }

                if(ImageObj.ShownInt == ImageObj.Images.Count)
                {
                    ImageObj.ShownInt = 0;
                }
                else if(ImageObj.ShownInt == -1)
                {
                    ImageObj.ShownInt = ImageObj.Images.Count - 1;
                }
                 ImageObj.Shown = ImageObj.Images[ImageObj.ShownInt];
                Opened.Source = new BitmapImage(new Uri(ImageObj.Shown.PicturePath, UriKind.Absolute));
                await Task.Delay(100);
                if (ImageObj.Shown != null)
                {
                    TryLoad();
                    PathLabel.Content = ImageObj.Shown.PicturePath;
                }
                
            }
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
        
        private void Opened_MouseDown(object sender, MouseButtonEventArgs e) {
            if (ImageObj.Shown != null && e.ChangedButton == MouseButton.Left)
            {
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
        }
        
        
        private void Opened_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (ImageObj.Shown != null && e.ChangedButton == MouseButton.Left && mousehold)
            {
                mousehold = false;
                timer.Stop();
                ResetLocations(false);
                DontSaveRect();
                GetLocation(sender, e);
            }
        }
        private void SelectX(object sender, RoutedEventArgs e)
        {
            int vid = -1;
            for (int i = 0; i < LocationImages.Length; i++)
            {
                if(sender == LocationImages[i])
                {
                    vid = i; break;
                }
            }
            if(vid != -1)
            {
                SelectedID = vid;
                CurrentID = 1;
                LocationImages[vid].IsHitTestVisible = false;
                mousehold = true;
                timer.Start();
            }
        }
        private void UpdateMouseHeld(object sender, EventArgs e)
        {
            if (mousehold)
            {
                
                //ResetLocations(SelectedID == 0 || SelectedID == 1 );
                ResetLocations(CurrentID == 2 ? true : false);
                DontSaveRect();
                System.Windows.Point mousePosition = new System.Windows.Point();
                mousePosition = Mouse.GetPosition(Opened);
                //System.Windows.MessageBox.Show($"{mousePosition}");
                double imageWidth = Opened.ActualWidth;
                double imageHeight = Opened.ActualHeight;

                int xInImage = round((mousePosition.X / imageWidth) * Opened.Source.Width);
                int yInImage = round((mousePosition.Y / imageHeight) * Opened.Source.Height);
                SelectedLocations[SelectedID] = (mousePosition.X, mousePosition.Y);
                UpdateLocations(CurrentID + 1);
            }
        }
        private bool mousehold = true;
        //MouseDown="GetLocation"
        private void GetLocation(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (CurrentID == 2)
                {
                    ResetLocations();
                    DontSaveRect();
                }
                if (ImageObj.Shown != null)
                {
                    System.Windows.Point mousePosition = e.GetPosition(Opened);

                    double imageWidth = Opened.ActualWidth;
                    double imageHeight = Opened.ActualHeight;

                    //int xInImage = round((mousePosition.X / imageWidth) * Opened.Source.Width);
                    //int yInImage = round((mousePosition.Y / imageHeight) * Opened.Source.Height);
                    SelectedLocations[SelectedID] = (mousePosition.X, mousePosition.Y);
                    //PixelLocation.Content = $"Selected Location: {inImage(SelectedLocations[CurrentID])}";
                    CurrentID++;
                    SelectedID = -1;
                    UpdateLocations();
                }
            }
            
        }
        public (double, double) inImage((double,double) mouse_position)
        {
            double imageWidth = Opened.ActualWidth;
            double imageHeight = Opened.ActualHeight;
            return ((mouse_position.Item1 / imageWidth) * Opened.Source.Width,(mouse_position.Item2 / imageHeight) * Opened.Source.Height);

        }
        public (double, double) inApp((double, double) image_location)
        {
            double imageWidth = Opened.ActualWidth;
            double imageHeight = Opened.ActualHeight;
            return ((image_location.Item1 * imageWidth) / Opened.Source.Width, (image_location.Item2 * imageHeight) / Opened.Source.Height);

        }
        private void UpdateLocations(int cid = -1)
        {
            if(cid == -1)
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

                // Update the image's position
                i.Margin = new Thickness(imageX, imageY, 0, 0);

                // Show the image
                i.Visibility = Visibility.Visible;
            }
            if(cid == 2)
            {
                BuildRect();
            }
        }
        private void ResetLocations(bool ResetCID = true)
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
        public void BuildRect()
        {
            var i = new System.Windows.Controls.Image();
            i.Width = Math.Abs((SelectedLocations[0].Item1 - SelectedLocations[1].Item1));
            i.Height = Math.Abs((SelectedLocations[0].Item2 - SelectedLocations[1].Item2));
            int width = (int)i.Width;
            int height = (int)i.Height;
            i.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            i.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            i.Margin = new Thickness(SelectedLocations[0].Item1 + LocationImages[0].Width/2 - i.Width * ((SelectedLocations[0].Item1 > SelectedLocations[1].Item1) ? 1 : 0), SelectedLocations[0].Item2 + LocationImages[0].Height / 2 - i.Height * ((SelectedLocations[0].Item2 > SelectedLocations[1].Item2) ? 1 : 0), 0, 0);
            i.Name = $"Rect_{RectCount}";
            i.IsHitTestVisible = false;
            CurrentRect = i;
            
            ProjGrid.Children.Add(i);
            System.Windows.Controls.Panel.SetZIndex(i, 1);
            setRectColor(i, 255, 0, 0,1);

            var corner1 = inImage(SelectedLocations[0]);
            var corner2 = inImage(SelectedLocations[1]);
            LastRect.Content = $"Current: ({corner1}, {corner2})";
        }
        public void BuildRectEXT((double, double) cor1, (double,double) cor2, int c)
        {
            
            var i = new System.Windows.Controls.Image();
            i.Width = Math.Abs(cor1.Item1 - cor2.Item1);
            i.Height = Math.Abs(cor1.Item2 - cor2.Item2);
            i.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            i.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            i.Margin = new Thickness(cor1.Item1 - i.Width * ((cor1.Item1 > cor2.Item1) ? 1 : 0), cor1.Item2 - i.Height * ((cor1.Item2 > cor2.Item2) ? 1 : 0), 0, 0);
            i.Name = $"Rect_{RectCount}";
            i.IsHitTestVisible = false;
            RectImages.Add(i);
            ProjGrid.Children.Add(i);
            System.Windows.Controls.Panel.SetZIndex(i, 1);
            if (!classes.ContainsKey(c))
            {
                AddClass(c);
            }
            setRectColor(i, classes[c].Item2);
        }
        public void setRectColor(System.Windows.Controls.Image i, RectColor C)
        {
            setRectColor(i, C.r, C.g, C.b, MARGINWIDTH);
        }
        private void setRectColor(System.Windows.Controls.Image i,byte r,byte g,byte b, int mw, byte alpha = 200)
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
                ResetLocations();
            }
        }
        public void CompleteRect()
        {
            setRectColor(CurrentRect, classes[CurrentClass].Item2.r, classes[CurrentClass].Item2.g, classes[CurrentClass].Item2.b, MARGINWIDTH, classes[CurrentClass].Item2.a);
            ResetLocations();
            var corner1 = inImage(SelectedLocations[0]);
            var corner2 = inImage(SelectedLocations[1]);
            double Width = ((double)Math.Abs(corner1.Item1- corner2.Item1)) / Opened.Source.Width;
            double Height = ((double)Math.Abs(corner1.Item2 - corner2.Item2)) / Opened.Source.Height;
            double x = (((double)Math.Abs(corner1.Item1 + corner2.Item1)) / 2d) / Opened.Source.Width;
            double y = (((double)Math.Abs(corner1.Item2 + corner2.Item2)) / 2d) / Opened.Source.Height;
            RectCount++;
            RectImages.Add(CurrentRect);
            CurrentRect = null;
            CreatedRectangles.Add(new YOLORect(x, y, Width, Height, CurrentClass));
            LastRect.Content = $"Last: <x: {Math.Round(x,2)},y: {Math.Round(y,2)},w: {Math.Round(Width,2)},h: {Math.Round(Height,2)}, c: {CurrentClass}>";
        }
        private int round(double x)
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
                if(split.Length == 2)
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
            foreach(string line in lines)
            {
                if(int.TryParse(line.Split(' ')[1], out int lineclass))
                {
                    if(lineclass == number)
                    {
                        SelectedLine = i;
                    }
                }
                i++;
            }
            System.Windows.MessageBox.Show($"{SelectedLine}");
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
        public void Export(object sender, RoutedEventArgs e)
        {
            if (PATH != "" && CreatedRectangles.Count > 0)
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
                using (StreamWriter writer = File.AppendText(newfilepath))
                {
                    foreach (YOLORect CurrentRect in CreatedRectangles)
                    {
                        writer.WriteLine(CurrentRect);

                    }

                }
            }

        }

        private void Load_Checked(object sender, RoutedEventArgs e)
        {
            LoadLabel = !LoadLabel;
        }
    }
}
    