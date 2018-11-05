using Dell.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CollectorUI
{
    /// <summary>
    /// Interaction logic for YouTubeVideoItem.xaml
    /// </summary>
    public partial class YouTubeVideoItem : UserControl
    {
        public YouTubeVideoItem()
        {
            InitializeComponent();

            ContextMenu cm = new ContextMenu();
            MenuItem mi = new MenuItem();
            mi.Header = "Add";
            cm.Items.Add(mi);            
            this.ContextMenu = cm;

            this.ContextMenu.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(mi_Click));
        }

        void mi_Click(object sender, RoutedEventArgs e)
        {
            var v = this.DataContext as VideoEntry;

            var dlg = new Window();
            ComboBox cbx = new ComboBox();
            cbx.Height = 50;
            cbx.FontSize = 20;
            cbx.ItemsSource = Global.Classifier.Classes;
            cbx.SelectedIndex = 0;
            var grid = new StackPanel();         
            var btOk = new Button();
            btOk.Content = "ADD";
            btOk.Height = 50;
            btOk.Click += new RoutedEventHandler((object o, RoutedEventArgs arg) => 
            {
                dlg.Close();
                foreach (var f in v.Frames)
                {
                    if (f.TestResult.IsMatch)
                    {
                        Global.Classifier.AddImage(f.Thumbnail, (string)cbx.SelectedItem);
                    }
                }
            });

            grid.Margin = new Thickness(5);
            grid.Children.Add(cbx);
            grid.Children.Add(btOk);
            dlg.Content = grid;
            dlg.Width = 220;
            dlg.Height = 150;

            var p = Mouse.GetPosition(Application.Current.MainWindow);
            dlg.Left = p.X + Application.Current.MainWindow.Left;
            dlg.Top = p.Y + Application.Current.MainWindow.Top;
            dlg.WindowStyle = WindowStyle.ToolWindow;            
            dlg.ShowDialog();         
        }
    }

    public class PassFailColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value)
            {
                return new SolidColorBrush(Colors.Green);
            }
            else
            {
                return new SolidColorBrush(Colors.Red);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Color)
            {
                if ((Color)value == Colors.Green)
                    return true;
                else
                    return false;
            }
            else
            {
                return false;
            }
        }
    }
}
