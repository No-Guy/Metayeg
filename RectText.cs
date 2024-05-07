using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Formats.Asn1;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using WpfApp1;
using static WpfApp1.MainWindow;

namespace Metayeg
{
    public class RectText
    {
        public YOLORect Data;
        public System.Windows.Controls.Label label;
        public System.Windows.Controls.Image image;
        public Window origin;
        public enum Window
        {
            MainWindow =0, VideoWindowLeft = 1, VideoWindowRight = 2
        }

        public static int location = 0;

        public static List<RectText> Rectangles = new List<RectText>();
        public RectText(YOLORect d, System.Windows.Controls.Image i, Window o = Window.MainWindow)
        {
            origin = o;
            label = new System.Windows.Controls.Label();
            Data = d;
            label.Tag = this;
            i.Tag = this;

            label.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            label.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            if (origin == Window.MainWindow)
            {
                label.MouseEnter += OnMouseEnter;
                label.MouseDown += OnMouseDown;
                label.MouseLeave += OnMouseLeave;
                label.Width = Singleton.SidebarTitle.Width;
                label.Height = Singleton.SidebarTitle.Height;
                label.Margin = new System.Windows.Thickness(0, Singleton.SidebarTitle.Margin.Top + 30 * (Rectangles.Count + 1), Singleton.SidebarTitle.Margin.Right, 0);
                Singleton.ProjGrid.Children.Add(label);
            }
            ChangeLabel();
            Rectangles.Add(this);
            image = i;
            UpdateRectTextsLocations();
            Inside(this);
            
        }
        public void ChangeLabel()
        {
            if (origin == Window.MainWindow)
            {
                label.Content = $"{classes[Data.c].Item1}: {round(Data.x * Singleton.Opened.Source.Width)},{round(Data.y * Singleton.Opened.Source.Height)},{round(Data.w * Singleton.Opened.Source.Width)},{round(Data.h * Singleton.Opened.Source.Height)}";
            }
            else if(origin == Window.VideoWindowRight || origin == Window.VideoWindowLeft)
            {
                label.Content = $"{classes[Data.c].Item1} ";
            }
        }
        private static string RoundTo1(double d)
        {

            return ((double)((int)(d * 10)) / 10).ToString();
        }
        public static void UpdateRectTextsLocations()
        {
            int i = 0;
            //System.Windows.MessageBox.Show(GetCount().ToString());
            foreach (var rect in Rectangles)
            {
                if (i >= location && i < GetCount() + location)
                {
                    if (rect.origin == Window.MainWindow)
                    {
                        rect.label.Margin = new System.Windows.Thickness(0, Singleton.SidebarTitle.Margin.Top + 30 * (i - location + 1), Singleton.SidebarTitle.Margin.Right, 0);
                    }
                    rect.label.IsEnabled = true;
                    rect.label.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    rect.label.IsEnabled = false;
                    rect.label.Visibility = System.Windows.Visibility.Collapsed;
                }
                i++;
            }
        }
        public static void Inside(RectText Rect1)
        {
            if (Window.MainWindow == Rect1.origin)
            {
                foreach (var Rect2 in Rectangles)
                {
                    if (Rect1 != Rect2)
                    {
                        if (Rect1.Data.x + Rect1.Data.w / 2 >= Rect2.Data.x + Rect2.Data.w / 2 && Rect1.Data.x - Rect1.Data.w / 2 <= Rect2.Data.x - Rect2.Data.w / 2 &&
                            Rect1.Data.y + Rect1.Data.h / 2 > Rect2.Data.y + Rect2.Data.h / 2 && Rect1.Data.y - Rect1.Data.h / 2 <= Rect2.Data.y - Rect2.Data.h / 2)  //Rect2 is inside Rect1
                        {
                            Singleton.ProjGrid.Children.Remove(Rect2.image);
                            Singleton.ProjGrid.Children.Add(Rect2.image);
                        }
                    }
                }
            }
            
        }
        public static void Refresh()
        {
            var set = new HashSet<RectText>();
            if (Rectangles != null)
            {
                foreach (var RT in Rectangles)
                {
                   
                    if (Tranforms.TransformEnabled)
                    {
                        RT.Data.c = Tranforms.CreatedTransform[RT.Data.c];
                    }
                    if (RT.Data.c != -1)
                    {
                        setRectColor(RT.image, classes[RT.Data.c].Item2);
                        RT.ChangeLabel();
                    }
                    else
                    {
                        set.Add(RT);
                        Singleton.ProjGrid.Children.Remove(RT.label);
                        Singleton.ProjGrid.Children.Remove(RT.image);
                    }
                    
                }
                foreach (var item in set)
                {
                    Rectangles.Remove(item);
                }

            }
            UpdateRectTextsLocations();
            Singleton.Export();
            //setRectColor(System.Windows.Controls.Image i, RectColor C, int mw = -1)
        }
        public static int GetCount()
        {
            if (Rectangles[0].origin == Window.MainWindow)
            {
                return (int)((System.Windows.Application.Current.MainWindow.Height - 65) / 30);
            }
            else if(Rectangles[0].origin == Window.VideoWindowRight || Rectangles[0].origin == Window.VideoWindowLeft)
            {
                return (int)((System.Windows.Application.Current.MainWindow.Height - 80) / 30);
            }
            return 100;
        }
        public static void DestroyAll()
        {
            foreach (var item in Rectangles)
            {
                if (item.origin == Window.MainWindow)
                {
                    Singleton.ProjGrid.Children.Remove(item.label);
                    Singleton.ProjGrid.Children.Remove(item.image);
                }
            }
            Rectangles = new List<RectText>();
        }
        public async static void Regenerate()
        {
            await Task.Delay(10);
            foreach (var Rect in Rectangles)
            {
                Singleton.ProjGrid.Children.Remove(Rect.image);
                AddRect(Rect.Data, Rect);
                /*
                var offset = (0, 0);
                var offset2 = (0,0);
                
                var corners = Singleton.YoloRectToCorners(Rect.Data);

                var cor1 = (corners.Item1.Item1 + offset.Item1 + offset2.Item1, corners.Item1.Item2 + offset.Item2 + offset2.Item2);
                var cor2 = (corners.Item2.Item1 + offset.Item1 + offset2.Item1, corners.Item2.Item2 + offset.Item2 + offset2.Item2);
                Singleton.BuildRectEXT(cor1, cor2, Rect.Data, Rect);
                Rect.Data.Cor1 = cor1;
                Rect.Data.Cor2 = cor2;
                */
            }
           // print(Rectangles.Count);
        }
        public void Destroy()
        {
            if (origin == Window.MainWindow)
            {
                Singleton.ProjGrid.Children.Remove(label);
                Singleton.ProjGrid.Children.Remove(image);
            }
            Rectangles.Remove(this);
            UpdateRectTextsLocations();
        }
        private static void OnMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)

        {
            var l = sender as System.Windows.Controls.Label;
            if (l != null && e.ChangedButton == MouseButton.Left && CurrentID == 0)
            {
                var ThisRect = ((RectText)l.Tag);
                ThisRect.EditRect();
            }
        }
        public void EditRect()
        {
            SetModifiedLabel(true);
            if (Singleton.CurrentRect != null)
            {
                Singleton.ResetLocations(false);
                Singleton.DontSaveRect();
            }
            Destroy();
            ((double, double), (double, double)) cors;
            if(Data.Cor1 != (-1,-1))
            {
                cors = (Data.Cor1, Data.Cor2);
                //cors = Singleton.YoloRectToCorners(Data);
            }
            else
            {
                cors = Singleton.YoloRectToCorners(Data);
            }
            SelectedLocations[0] = cors.Item1;
            SelectedLocations[1] = cors.Item2;
            Singleton.UpdateLocations(2);
            Singleton.ChangeClass(Data.c);
            CurrentID = 2;
            SelectedID = -1;
            Singleton.CurrentRect.IsHitTestVisible = true;
        }
        public static void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
            
        {
            var l = sender as System.Windows.Controls.Label;
            if (l != null) {
                l.FontWeight = FontWeights.Bold;
                var ThisRect = ((RectText)l.Tag);
                //System.Windows.MessageBox.Show(Lighten(classes[ThisRect.Data.c].Item2, 1.5d).ToString() +"\n" + classes[ThisRect.Data.c].Item2.ToString());
                setRectColor(ThisRect.image, Lighten(classes[ThisRect.Data.c].Item2,1.5d), MARGINWIDTH + 2);
                //MessageBox.Show(((RectText)l.Tag).Data.ToString());
            }
            
            
        }
        
        public static void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var l = sender as System.Windows.Controls.Label;
            if (l != null)
            {
                l.FontWeight = FontWeights.Thin;
                var ThisRect = ((RectText)l.Tag);
                setRectColor(ThisRect.image, classes[ThisRect.Data.c].Item2);
                //MessageBox.Show(((RectText)l.Tag).Data.ToString());
            }
        }
        public static RectColor Lighten(RectColor c, double by)
        {
            return new RectColor(ClampAsByte(c.r * by), ClampAsByte(c.g * by), ClampAsByte(c.b * by), c.a);
        }
        public static byte ClampAsByte(double d)
        {
            d = Math.Clamp(d, 0, 255);
            return (byte)d;
        }
    }
   
}
