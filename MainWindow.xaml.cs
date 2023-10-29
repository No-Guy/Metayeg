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

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public enum cls
        {
            person = 0
        }
        private DispatcherTimer timer;
        public MainWindow()
        {
            InitializeComponent();
            NewImage();
            Opened.MouseDown += Opened_MouseDown;
            Opened.MouseUp += Opened_MouseUp;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(10); // Adjust the interval as needed
            timer.Tick += UpdateMouseHeld;
            
        }
        public struct YOLORect
        {
            public double x, y;
            public double w, h;
            public cls c;
            public YOLORect(double x0, double y0, double w0, double h0, cls c0 = cls.person)
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
        public static string PATH = "";
        private static (double, double)[] SelectedLocations = new (double, double)[2];
        private static int CurrentID = 0;
        private static System.Windows.Controls.Image[] LocationImages = new System.Windows.Controls.Image[2];
        private System.Windows.Controls.Image? CurrentRect;
        public static List<System.Windows.Controls.Image> RectImages = new List<System.Windows.Controls.Image>();
        public static List<YOLORect> CreatedRectangles = new List<YOLORect>();
        public int RectCount = 0;
        private int SelectedID = 0;
        public static readonly int MARGINWIDTH = 5;
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
            
        }
        private void Button_Click(object sender, RoutedEventArgs e)
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
            ImageObj.CreateImages();
            if (ImageObj.Images.Count > 0)
            {
                Opened.Source = new BitmapImage(new Uri(ImageObj.Images[0].PicturePath, UriKind.Absolute));
                ImageObj.Shown = ImageObj.Images[0];
                ImageObj.ShownInt = 0;
            }

            //}
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
        }
        public void NextPrev(object sender, RoutedEventArgs e)
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
                PixelLocation.Content = $"Selected Location: {inImage((mousePosition.X, mousePosition.Y))}";
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

                    int xInImage = round((mousePosition.X / imageWidth) * Opened.Source.Width);
                    int yInImage = round((mousePosition.Y / imageHeight) * Opened.Source.Height);
                    SelectedLocations[SelectedID] = (mousePosition.X, mousePosition.Y);
                    //PixelLocation.Content = $"Selected Location: {inImage(SelectedLocations[CurrentID])}";
                    CurrentID++;
                    SelectedID = -1;
                    UpdateLocations();
                }
            }
            
        }
        public (int,int) inImage((double,double) mouse_position)
        {
            double imageWidth = Opened.ActualWidth;
            double imageHeight = Opened.ActualHeight;
            return (round((mouse_position.Item1 / imageWidth) * Opened.Source.Width),round((mouse_position.Item2 / imageHeight) * Opened.Source.Height));

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
            setRectColor(CurrentRect, 120, 0, 0,MARGINWIDTH,180);
            ResetLocations();
            var corner1 = inImage(SelectedLocations[0]);
            var corner2 = inImage(SelectedLocations[1]);
            double Width = ((double)Math.Abs(corner1.Item1- corner2.Item1)) / Opened.Source.Width;
            double Height = ((double)Math.Abs(corner1.Item2 - corner2.Item2)) / Opened.Source.Height;
            double x = (((double)Math.Abs(corner1.Item1 + corner2.Item1)) / 2d) / Opened.Source.Width;
            double y = (((double)Math.Abs(corner1.Item2 + corner2.Item2)) / 2d) / Opened.Source.Height;
            cls c = cls.person;
            RectCount++;
            RectImages.Add(CurrentRect);
            CurrentRect = null;
            CreatedRectangles.Add(new YOLORect(x, y, Width, Height, c));
            LastRect.Content = $"Last: <x: {Math.Round(x,2)},y: {Math.Round(y,2)},w: {Math.Round(Width,2)},h: {Math.Round(Height,2)}, c: {c}>";
        }
        private int round(double x)
        {
            if (x - (int)x > 0.5)
            {
                return (int)x + 1;
            }
            return (int)x;
        }
        public void Export(object sender, RoutedEventArgs e)
        {
            if (PATH != "" && CreatedRectangles.Count > 0)
            {
                string folderPath = System.IO.Path.Combine(PATH, "labels");

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
                    foreach (YOLORect CurrentRect in CreatedRectangles)
                    {
                        writer.WriteLine(CurrentRect);

                    }

                }
            }

        }
    }
}
    