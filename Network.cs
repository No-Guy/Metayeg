using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using WpfApp1;

namespace Metayeg
{
    internal class Network
    {
        public static List<Network>? networks;

        public string name;

        public bool isYolo;
        public static void GenerateNetworks(RectText.Window w = RectText.Window.MainWindow)
        {
            networks = new List<Network>();
            var NetworksFolder = Path.Combine(Directory.GetCurrentDirectory(), "Models");
            if (Directory.Exists(NetworksFolder))
            {
                string[] files = Directory.GetFiles(NetworksFolder);
                foreach (var file in files)
                {
                    string ext = Path.GetExtension(file);
                    string name = Path.GetFileName(file);
                    if (ext == ".pt") {
                        Network n = new Network();

                        n.name = name;
                        n.isYolo = (n.name[0] == 'Y' && n.name[1] == '_');
                        //MessageBox.Show(n.isYolo.ToString());
                        networks.Add(n);
                        MenuItem newItem = new MenuItem();
                        if (n.isYolo)
                        {
                            newItem.Header = $"{(name.Split('.')[0]).Split('_')[1]} (Yolo)";
                        }
                        else
                        {

                        }
                        newItem.FontSize = 12;
                        newItem.Tag = n;

                        if (w == RectText.Window.MainWindow)
                        {
                            newItem.Click += MainWindow.Singleton.CallToYoloIT;
                            MainWindow.Singleton.ModelsMenu.Items.Add(newItem);
                        }
                    }
                }
            }
            

        }
    }
}
