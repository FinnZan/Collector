using Dell.Utilities;
using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace CollectorUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FolderBrowserDialog _folderBrowserDialog = new FolderBrowserDialog();
        private OpenFileDialog _openFileDialog = new OpenFileDialog();
               
        private string _pythonDir;

        public MainWindow()
        {
            CommonTools.InitializeDebugger("Collector");
            CommonTools.Log("=====================================");
            CommonTools.ShowLogs(true);

            InitializeComponent();

            tbImage.Text = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "images");
            tbWorking.Text = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "working");
            
            ReadPreference();

            Global.WorkingFolder = tbWorking.Text;

            CommonTools.Log($"Python [{_pythonDir}]");

            _folderBrowserDialog.SelectedPath = Global.WorkingFolder;

            Global.Classifier = new Classifier(tbTaskName.Text, tbImage.Text, Global.WorkingFolder, _pythonDir);
        }

        private void btBrowseImage_Click(object sender, RoutedEventArgs e)
        {
            var ret = BrowseFolder();
            if (!string.IsNullOrEmpty(ret))
            {
                tbImage.Text = ret;
            }
        }

        private void btBrowseWorking_Click(object sender, RoutedEventArgs e)
        {
            var ret = BrowseFolder();
            if (!string.IsNullOrEmpty(ret))
            {
                tbWorking.Text = ret;
                Global.WorkingFolder = tbWorking.Text;
            }
        }

        private void btTrain_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(tbImage.Text))
            {
                System.Windows.MessageBox.Show("Image folder does not exist");
                return;
            }

            if (!Directory.Exists(Global.WorkingFolder))
            {
                System.Windows.MessageBox.Show("Working folder does not exist");
                return;
            }
            
            Global.Classifier.Train();
        }

        private void btTest_Click(object sender, RoutedEventArgs e)
        {
            var ret = Global.Classifier.Test(tbTestImage.Text);
            
            lbTestResult.Content = $"{ret.Scores[0].Key} ({ret.Scores[0].Value})";
        }

        private void btBrowseTestImage_Click(object sender, RoutedEventArgs e)
        {
            var ret = BrowseFile();
            if (!string.IsNullOrEmpty(ret))
            {
                tbTestImage.Text = ret;

                BitmapImage f = new BitmapImage();
                f.BeginInit();
                f.UriSource = new Uri(tbTestImage.Text);
                f.EndInit();
                imgTest.Source = f;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            SavePreference();
            Global.Classifier?.Stop();
            Environment.Exit(0);
        }

        private string BrowseFolder()
        {
            if (_folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                return _folderBrowserDialog.SelectedPath;
            }
            else
            {
                return string.Empty;
            }
        }

        private string BrowseFile()
        {
            if (_openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                return _openFileDialog.FileName;
            }
            else
            {
                return string.Empty;
            }
        }

        private void SavePreference()
        {
            var pref = $"IMAGES {tbImage.Text}\n";
            pref += $"WORKING {Global.WorkingFolder}\n";
            pref += $"TITLE {tbTaskName.Text}\n";
            pref += $"PYTHON {_pythonDir}\n";

            File.WriteAllText("pref.txt", pref);
        }

        private void ReadPreference()
        {
            if (File.Exists("pref.txt"))
            {
                var lines = File.ReadAllLines("pref.txt");
                foreach (var l in lines)
                {
                    if (!string.IsNullOrEmpty(l))
                    {
                        var index = l.IndexOf(' ');
                        var name = l.Substring(0, index);
                        var value = l.Substring(index + 1);

                        if (name.Equals("WORKING"))
                        {
                            tbWorking.Text = value.Trim();
                            Global.WorkingFolder = tbWorking.Text;
                        }

                        if (name.Equals("PYTHON"))
                        {
                            _pythonDir = value.Trim();
                        }

                        if (name.Equals("IMAGES"))
                        {
                            tbImage.Text = value.Trim();
                        }

                        if (name.Equals("TITLE"))
                        {
                            tbTaskName.Text = value.Trim();
                        }
                    }
                }
            }
        }             
    }
}
