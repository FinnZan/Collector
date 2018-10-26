using Dell.Utilities;
using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
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

        private Classifier _classifier = null;

        private YouTubeCrawler _youTubeCrawler = null;

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

            CommonTools.Log($"Python [{_pythonDir}]");

            _folderBrowserDialog.SelectedPath = tbWorking.Text;
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
            }
        }

        private void btTrain_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(tbImage.Text))
            {
                System.Windows.MessageBox.Show("Image folder does not exist");
                return;
            }

            if (!Directory.Exists(tbWorking.Text))
            {
                System.Windows.MessageBox.Show("Working folder does not exist");
                return;
            }

            _classifier = new Classifier(tbTaskName.Text, tbImage.Text, tbWorking.Text, _pythonDir);
            _classifier.Train();
        }

        private void btTest_Click(object sender, RoutedEventArgs e)
        {
            _classifier = new Classifier(tbTaskName.Text, tbImage.Text, tbWorking.Text, _pythonDir);
            var ret = _classifier.Test(tbTestImage.Text);
            
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
            _classifier?.Stop();
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
            pref += $"WORKING {tbWorking.Text}\n";
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

        private void btStartYouTube_Click(object sender, RoutedEventArgs e)
        {
            lbxYouTubeResults.ItemsSource = null;
            lbYouTubeCurrent.Content = string.Empty;
            lbYouTubeCurrentScore.Content = string.Empty;

            _classifier = new Classifier(tbTaskName.Text, tbImage.Text, tbWorking.Text, _pythonDir);

            if (_youTubeCrawler == null)
            {
                _youTubeCrawler = new YouTubeCrawler(_classifier, tbWorking.Text);
            }

            lbxYouTubeResults.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler((object o, SelectionChangedEventArgs args) =>
            {
                new Thread(()=> { System.Diagnostics.Process.Start(((VideoFrame)lbxYouTubeResults.SelectedItem).Url); }).Start();
            });

            _youTubeCrawler.ProgressChanged += new EventHandler<YouTubeCrawlerProgress>((object o, YouTubeCrawlerProgress p) =>
            {
                lbxYouTubeResults.Dispatcher.BeginInvoke((Action)(() =>
                {
                    BitmapImage f = new BitmapImage();
                    f.BeginInit();
                    f.UriSource = new Uri(p.Latest.Thumbnail);
                    f.EndInit();
                    imgYouTubeCurrent.Source = f;

                    lbYouTubeCurrent.Content = $"Video {p.Current+1}/{p.Total} Frame {p.Latest.FrameIndex}\n{p.Latest.Title}\n{p.Latest.Time}\n";

                    if (p.Latest.Score == null)
                    {
                        lbYouTubeCurrentScore.Content = $"Validating....";
                    }
                    else
                    {
                        lbYouTubeCurrentScore.Content = $"{p.Latest.Score}";
                    }
                    
                    pbYouTube.Maximum = p.Total;
                    pbYouTube.Minimum = 0;
                    pbYouTube.Value = p.Current;

                    lbxYouTubeResults.ItemsSource = null;
                    lbxYouTubeResults.ItemsSource = _youTubeCrawler.Results;
                }));
            });

            var key = tbxYouTubeKey.Text;
            var group = tbxGroup.Text;
            var pass = double.Parse(tbxPass.Text);

            new Thread(() =>
            {
                try
                {
                    var results = _youTubeCrawler.Search(key, group, pass);

                    CommonTools.Log("Done.");

                    lbxYouTubeResults.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        lbxYouTubeResults.ItemsSource = results;
                    }));
                }
                catch (Exception ex)
                {
                    CommonTools.HandleException(ex);
                }
            }).Start();
        }
    }
}
