using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using static WpfApp1.MainWindow;

namespace Metayeg
{
    internal class RectText
    {
        public YOLORect Data;
        public System.Windows.Controls.Label label;
        System.Windows.Controls.Image image;

        public static int location = 0;

        public static List<RectText> Rectangles = new List<RectText>();
        public RectText(YOLORect d, System.Windows.Controls.Image i)
        {
            label = new System.Windows.Controls.Label();
            Data = d;
            label.Tag = this;
            label.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            label.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            label.MouseEnter += OnMouseEnter;
            label.MouseDown += OnMouseDown;
            // Subscribe to the MouseLeave event
            label.MouseLeave += OnMouseLeave;
            label.Width = Singleton.SidebarTitle.Width;
            label.Height = Singleton.SidebarTitle.Height;
            label.Content = $"{classes[Data.c].Item1}: {round(Data.x * Singleton.Opened.Source.Width)},{round(Data.y * Singleton.Opened.Source.Height)},{round(Data.w * Singleton.Opened.Source.Width)},{round(Data.h * Singleton.Opened.Source.Height)}";
            label.Margin = new System.Windows.Thickness(0, Singleton.SidebarTitle.Margin.Top + 30 * (Rectangles.Count + 1), Singleton.SidebarTitle.Margin.Right, 0);
            Singleton.ProjGrid.Children.Add(label);
            Rectangles.Add(this);
            image = i;
            UpdateRectTextsLocations();
        }
        public static void UpdateRectTextsLocations()
        {
            int i = 0;
            //System.Windows.MessageBox.Show(GetCount().ToString());
            foreach (var rect in Rectangles)
            {
                if (i >= location && i < GetCount() + location)
                {
                    rect.label.Margin = new System.Windows.Thickness(0, Singleton.SidebarTitle.Margin.Top + 30 * (i - location + 1), Singleton.SidebarTitle.Margin.Right, 0);
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
        public static int GetCount()
        {
            
            return (int)((System.Windows.Application.Current.MainWindow.Height - 130) / 30);
        }
        public static void DestroyAll()
        {
            foreach (var item in Rectangles)
            {
                Singleton.ProjGrid.Children.Remove(item.label);
                Singleton.ProjGrid.Children.Remove(item.image);
            }
            Rectangles = new List<RectText>();
        }
        public void Destroy()
        {
            Singleton.ProjGrid.Children.Remove(label);
            Singleton.ProjGrid.Children.Remove(image);
            Rectangles.Remove(this);
            UpdateRectTextsLocations();
        }
        private static void OnMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)

        {
            var l = sender as System.Windows.Controls.Label;
            if (l != null && e.ChangedButton == MouseButton.Left && CurrentID == 0)
            {
                var ThisRect = ((RectText)l.Tag);
                ThisRect.Destroy();
                var cors = Singleton.YoloRectToCorners(ThisRect.Data);
                SelectedLocations[0] = cors.Item1;
                SelectedLocations[1] = cors.Item2;
                Singleton.UpdateLocations(2);
                CurrentID = 1;
                SelectedID = -1;
            }
        }
        private static void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
            
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
        
        private static void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
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
