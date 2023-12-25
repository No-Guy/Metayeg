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
namespace Metayeg
{
    internal class YoloIt
    {
        public static int Patches = 1;
        private static List<(string, int, int)> patches = new List<(string, int, int)>();
        private static int patchWidth = 0;
        private static int patchHeight = 0;
        private static int ImageWidth = 0;
        private static int ImageHeight = 0;

        public static async void CreatePatches()
        {
            GetPatchesCount();
            MainWindow.Singleton.RunYoloButton.IsEnabled = false;
            RectText.DestroyAll();
            var labels = System.IO.Path.Combine(MainWindow.labelsFolder, ImageObj.Shown.name + ".txt");
            if (File.Exists(labels))
            {
                File.Delete(labels);
            }
            //MainWindow.Singleton.
            await Task.Delay(50);
            Bitmap image = new Bitmap(ImageObj.Shown.PicturePath);
            ImageWidth = image.Width;
            ImageHeight = image.Height;
            patchWidth = image.Width / Patches;
            patchHeight = image.Height / Patches;
            int rows = Patches;
            int columns = Patches;
            GlobalRects = new List<MainWindow.YOLORect>();
            patches = new List<(string, int, int)>();
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    int x = col * patchWidth;
                    int y = row * patchHeight;

                    Rectangle patchRectangle = new Rectangle(x, y, patchWidth, patchHeight);
                    Bitmap patch = image.Clone(patchRectangle, image.PixelFormat);


                    string filename = $"patch_{row}_{col}.png";
                    patches.Add(($"patch_{row}_{col}", row, col));
                    patch.Save(filename, ImageFormat.Png);
                }
            }

            YOLOpatches();
        }
        public static void GetPatchesCount()
        {
            string val = MainWindow.Singleton.Patches_TextBox.Text;
            if(int.TryParse(val, out int count))
            {
                if(count > 0 && count < 11)
                {
                    Patches = count;
                    return;
                }
            }
            MainWindow.Singleton.Patches_TextBox.Text = Patches.ToString();
        }
        public async static void Yolo()
        {
            try
            {
                MainWindow.Singleton.RunYoloButton.IsEnabled = false;
                var workpath = Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(ImageObj.Shown.PicturePath));
                //MessageBox.Show(ImageObj.Shown.PicturePath + " " + Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(ImageObj.Shown.PicturePath)));
                File.Copy(ImageObj.Shown.PicturePath, workpath);
                YOLOpatches(false);
                MessageBox.Show(Path.Combine(MainWindow.labelsFolder, Path.ChangeExtension(Path.GetFileName(ImageObj.Shown.PicturePath), ".txt")));
                File.Copy(Path.ChangeExtension(workpath, ".txt"), Path.Combine(MainWindow.labelsFolder, Path.ChangeExtension(Path.GetFileName(ImageObj.Shown.PicturePath), ".txt")));
                File.Delete(workpath);
                await Task.Delay(10);
                MainWindow.Singleton.TryLoad();

                MainWindow.Singleton.RunYoloButton.IsEnabled = true;

            }
            catch
            {
                MessageBox.Show("fail");
            }

        }
        private static void YOLOpatches(bool rejoin = true)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",  // Use the Windows command prompt
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Create a new process and start it
            Process process = new Process
            {
                StartInfo = processStartInfo
            };

            process.Start();

            // Send the batch file path as a command to the command prompt
            process.StandardInput.WriteLine("run.bat");
            process.StandardInput.WriteLine("exit");

            // Wait for the process to complete
            process.WaitForExit();

            // Close the command prompt
            process.Close();
            if (rejoin)
            {
                Rejoin();
            }
            MainWindow.Singleton.RunYoloButton.IsEnabled = true;
            
        }
        private static List<MainWindow.YOLORect> GlobalRects = new List<MainWindow.YOLORect>();
        private static void Rejoin()
        {
            foreach (var result in patches)
            {
                var path = result.Item1 + ".txt";
                double vertical_patch = result.Item2;
                double horizontal_patch = result.Item3;
                if (File.Exists(path))
                {
                    try
                    {
                        using (StreamReader reader = new StreamReader(path))
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                var parts = line.Split(" ");
                                if (parts.Length == 5)
                                {
                                    if (int.TryParse(parts[0], out int cls) && double.TryParse(parts[1], out double local_x) && double.TryParse(parts[2], out double local_y) && double.TryParse(parts[3], out double local_w) && double.TryParse(parts[4], out double local_h))
                                    {
                                        double global_x = ((local_x * (double)patchWidth) + patchWidth * horizontal_patch) / ImageWidth;
                                        double global_y = ((local_y * (double)patchHeight) + patchHeight * vertical_patch) / ImageHeight;
                                        double global_w = local_w * ((double)patchWidth / (double)ImageWidth);
                                        double global_h = local_h * ((double)patchHeight / (double)ImageHeight);
                                        GlobalRects.Add(new MainWindow.YOLORect(global_x, global_y, global_w, global_h, cls));
                                        //MessageBox.Show($"{global_x}, {local_x}, {patchWidth}, {horizontal_patch}, {ImageWidth}");
                                        //MessageBox.Show($"{global_h} {local_h} {patchHeight} {ImageHeight}");
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            ExportandLoad();
        }

        private async static void ExportandLoad()
        {
            string folderPath = MainWindow.labelsFolder;

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
                foreach (var CurrentRect in GlobalRects)
                {
                    writer.WriteLine(CurrentRect);

                }

            }
            foreach (var file in patches)
            {
                var filePathA = file.Item1 + ".txt";
                var filePathB = file.Item1 + ".png";
                try
                {
                    if (File.Exists(filePathA))
                    {
                        File.Delete(filePathA);
                    }
                    if (File.Exists(filePathB))
                    {
                        File.Delete(filePathB);
                    }


                }
                catch { }

            }
            MainWindow.Singleton.RunYoloButton.IsEnabled = true;
            await Task.Delay(50);
            MainWindow.Singleton.TryLoad();
        }
        
    }
}
