using System;
using System.Collections.Generic;

using System.Windows;
using System.Windows.Controls;
using WpfApp1;


namespace Metayeg
{
    internal class Tranforms
    {
        private static bool generatedflag = false;

        public static List<System.Windows.Controls.ComboBox>? Boxes;

        public static bool TransformEnabled;

        public static Dictionary<int, int>? CreatedTransform;
        public static void GetTransform()
        {
            if (generatedflag) return;
            generatedflag = true;
            Boxes = new List<System.Windows.Controls.ComboBox>();
            CreatedTransform = new Dictionary<int, int>();
            MenuItem menuItem0 = new MenuItem();
            TransformEnabled = false;
            // Create a Grid to hold label and checkbox
            Grid grid0 = new Grid();
            grid0.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid0.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Label on the left
            System.Windows.Controls.Label label0 = new System.Windows.Controls.Label();
            label0.Content = "Enabled:";
            Grid.SetColumn(label0, 0);
            grid0.Children.Add(label0);

            // Checkbox on the right
            System.Windows.Controls.CheckBox checkBox = new System.Windows.Controls.CheckBox();
            checkBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            checkBox.VerticalAlignment = VerticalAlignment.Center;
            checkBox.Checked += Check;
            checkBox.Unchecked += UnCheck;
            Grid.SetColumn(checkBox, 1);
            grid0.Children.Add(checkBox);
            
            // Set the Grid as the MenuItem's header
            menuItem0.Header = grid0;
            MainWindow.Singleton.TransformMenu.Items.Add(menuItem0);
            MainWindow.Singleton.TransformMenu.Items.Add(new Separator());


            var title = new MenuItem();
            title.Header = " From                              To";
            MainWindow.Singleton.TransformMenu.Items.Add(title);



            foreach (var KV0 in MainWindow.classes)
            {
                MenuItem menuItem = new MenuItem();
                var label = new System.Windows.Controls.Label
                {
                    Content = $"{KV0.Value.Item1}({KV0.Key})"
                };
                menuItem.Width = 340;

                var comboBox = new System.Windows.Controls.ComboBox();
                foreach (var KV in MainWindow.classes)
                {
                    int id = KV.Key;
                    string name = KV.Value.Item1;
                    comboBox.Items.Add($"{name}({id})");
                }
                CreatedTransform[KV0.Key] = KV0.Key;
                comboBox.SelectedItem = comboBox.Items[KV0.Key];
                comboBox.Items.Add($"Remove");
                comboBox.Width = 104;
                Boxes.Add(comboBox);
                comboBox.SelectionChanged += ChangeMenuClassDropDown;
                Grid grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition()); // Column for the Label
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) }); // Spacer column
                grid.ColumnDefinitions.Add(new ColumnDefinition()); // Column for the ComboBox
                double spacerWidth = 90; // Set your desired spacer width
                label.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                double labelWidth = label.DesiredSize.Width;

                // Set the width of the spacer column
                grid.ColumnDefinitions[1].Width = new GridLength(spacerWidth - labelWidth, GridUnitType.Pixel);
                Grid.SetColumn(label, 0);
                Grid.SetColumn(comboBox, 2);
                grid.Children.Add(label);
                grid.Children.Add(comboBox);

                menuItem.Header = grid;

                // Add the MenuItem to an existing Menu
                MainWindow.Singleton.TransformMenu.Items.Add(menuItem);
            }
        }
        private static void Check(object sender, RoutedEventArgs e)
        {
            TransformEnabled = true;
            RectText.Refresh();
        }
        private static void UnCheck(object sender, RoutedEventArgs e)
        {
            TransformEnabled = false;
        }
        private static void ChangeMenuClassDropDown(object sender, SelectionChangedEventArgs e)
        {
            var selint = -1;
            for (int i = 0; i < Boxes.Count; i++)
            {
                if (Boxes[i] == (System.Windows.Controls.ComboBox)sender)
                {
                    selint = i; break;
                }
            }
            if (selint >= 0)
            {
                CreatedTransform[selint] = Boxes[selint].SelectedIndex;
                if (Boxes[selint].SelectedIndex == Boxes[selint].Items.Count - 1)
                {
                    CreatedTransform[selint] = -1;
                }
            }
            e.Handled = true;
            //System.Windows.MessageBox.Show();
            RectText.Refresh();
        }
    }
    
}
