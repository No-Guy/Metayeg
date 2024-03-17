using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Xml.Linq;

namespace WpfApp1
{
    internal class ImageObj
    {
        public static List<ImageObj> Images = new List<ImageObj>();
        public static ImageObj? Shown = null;
        public static int ShownInt = 0;
        public string? PicturePath;
        private BitmapImage? bmp;
        public string name;

        public int w;
        public int h;
        public static void DestroyAll()
        {
            Shown = null;
            Images = new List<ImageObj>();
        }
        public ImageObj(string p)
        {
            PicturePath = p;
            name = Path.GetFileNameWithoutExtension(PicturePath);
            Images.Add(this);
        }
        public ImageObj(BitmapImage b)
        {
            bmp = b;
            PicturePath = null;
            name = Images.Count.ToString();
            Images.Add(this);
        }
        public static void CreateImages()
        {
            Images = new List<ImageObj>();
            try
            {
                // Enumerate files in the directory
                foreach (string filePath in Directory.EnumerateFiles(MainWindow.PATH))
                {
                    string extension = Path.GetExtension(filePath).ToLower();
                    
                    if(extension == ".png" || extension == ".jpg" || extension == ".jpeg")
                    {
                        new ImageObj(filePath);
                    }   
                    
                }
            }
            catch (Exception e)
            {
                
            }
        }
    }
}
