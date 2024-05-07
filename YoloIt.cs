using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using WpfApp1;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.IO;
using System.Windows.Shapes;
using System.Diagnostics.Eventing.Reader;
using System.Net.Http.Headers;
using System.Windows.Threading;
namespace Metayeg
{
    internal class YoloIt
    {
        public async static void Yolo(Network N)
        {
            if (!float.TryParse(MainWindow.Singleton.ConfBox.Text, out float conf))
            {
                MainWindow.Singleton.ConfBox.Text = "0.1";
            }
            MainWindow.Singleton.LoadingPython.Content = "Creating Labels...";
            Thread T = new Thread(() => YOLO(N.name,conf));
            T.Start();
        }
        private static void YOLO(string name, float conf)
        {
            
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "python";
            startInfo.Arguments = $"\"{System.IO.Path.Combine(Directory.GetCurrentDirectory(), "src", "Yolo.py")}\" .\\models\\{name} \"{ImageObj.Shown.PicturePath}\" {conf}";
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;

            Process process = new Process();
            process.StartInfo = startInfo;
            process.Start();
            while (!process.StandardOutput.EndOfStream)
            {
                string? line = process.StandardOutput.ReadLine();

                if (line != null)

                {
                    var parts = line.Split(' ');
                    if (parts.Length > 0)
                    {
                        if (parts.Length == 5 && int.TryParse(parts[0], out int cls) && double.TryParse(parts[1], out double x) && double.TryParse(parts[2], out double y) && double.TryParse(parts[3], out double w) && double.TryParse(parts[4], out double h))
                        {
                            //Predicted.Add(new MainWindow.YOLORect(x, y, w, h, cls));
                            MainWindow.Singleton.Dispatcher.Invoke(() =>
                            {
                                MainWindow.AddRect(new MainWindow.YOLORect(x, y, w, h, cls));
                            });
                        }
                        
                       
                    }
                    
                }
            }
            while (!process.StandardError.EndOfStream)
            {
                string? line = process.StandardError.ReadToEnd();
                if (line != null)
                {
                    MainWindow.print(line);
                }
            }
                // Wait for the process to complete
            process.WaitForExit();

            // Close the command prompt
            process.Close();
            MainWindow.Singleton.Dispatcher.Invoke(() =>
            {
                MainWindow.Singleton.LoadingPython.Content = "";
            });
            //MainWindow.Singleton.RunYoloButton.IsEnabled = true;

        }
        
    }
}
