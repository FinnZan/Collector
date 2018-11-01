using Dell.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace CollectorUI
{
    /// <summary>
    /// Interaction logic for YouTube.xaml
    /// </summary>
    public partial class YouTubePanel : UserControl
    {
        private ObservableCollection<VideoFrame> _youtubeResults = new ObservableCollection<VideoFrame>();                

        public YouTubePanel()
        {
            InitializeComponent();

            _youtubeResults = new ObservableCollection<VideoFrame>();
            lbxYouTubeResults.ItemsSource = _youtubeResults;

            EventManager.RegisterClassHandler(typeof(ListBoxItem), ListBoxItem.MouseLeftButtonDownEvent, new RoutedEventHandler((object o, RoutedEventArgs arg) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(((VideoFrame)lbxYouTubeResults.SelectedItem).Url);
                }
                catch (Exception ex)
                {
                    CommonTools.HandleException(ex);
                }
            }));
        }

        private void btStartYouTube_Click(object sender, RoutedEventArgs e)
        {
            var selectedClass = tbxGroup.Text;
            var pass = double.Parse(tbxPass.Text);
            var days = int.Parse(tbxDays.Text);

            var youTubeCrawler = new YouTubeCrawler(Global.Classifier, Global.WorkingFolder);                                  
            
            List<SearchHistory> toSearch = new List<SearchHistory>();
            var history = ReadSearchHistory();

            if (string.IsNullOrEmpty(tbxYouTubeKey.Text))
            {
                toSearch = history;
            }
            else
            {
                var k = new SearchHistory()
                {
                    Keyword = tbxYouTubeKey.Text.Trim(),
                    LastUpdated = DateTime.Now - TimeSpan.FromDays(days)
                };
                
                if (!history.Any(s => s.Keyword == k.Keyword))
                {
                    history.Add(k);
                }
                toSearch.Add(k);
            }

            youTubeCrawler.ProgressChanged += new EventHandler<YouTubeCrawlerProgress>((object o, YouTubeCrawlerProgress p) =>
            {
                lbxYouTubeResults.Dispatcher.Invoke((Action)(() =>
                {
                    SaveSearchHistory(history);

                    try
                    {
                        BitmapImage f = new BitmapImage();
                        f.BeginInit();
                        f.UriSource = new Uri(p.Latest.Thumbnail);
                        f.EndInit();
                        imgYouTubeCurrent.Source = f;
                    }
                    catch (Exception ex)
                    {
                        CommonTools.HandleException(ex);
                    }

                    lbYouTubeCurrent.Content = $"Video of [{p.Keyword}] {p.Current + 1}/{p.Total} Frame {p.Latest.FrameIndex}\n{p.Latest.Title}\n{p.Latest.Time}\n";

                    if (p.Latest.TestResult == null)
                    {
                        lbYouTubeCurrentScore.Content = $"Validating....";
                    }
                    else
                    {
                        var str = "";
                        foreach (var s in p.Latest.TestResult.Scores)
                        {
                            str = $"[{s.Key}] [{s.Value}]\n";
                        }
                        lbYouTubeCurrentScore.Content = str;

                        if (p.Latest.TestResult.Scores[0].Key == selectedClass && p.Latest.TestResult.Scores[0].Value > pass)
                        {
                            _youtubeResults.Add(p.Latest);
                        }
                    }

                    pbYouTube.Maximum = p.Total;
                    pbYouTube.Minimum = 0;
                    pbYouTube.Value = p.Current;
                }));
            });

            // UI ==========================
            _youtubeResults.Clear();
            lbYouTubeCurrent.Content = string.Empty;
            lbYouTubeCurrentScore.Content = string.Empty;
            pbYouTube.Visibility = Visibility.Visible;                                

            try
            {
                var results = youTubeCrawler.Search(toSearch).ContinueWith((r) =>
                {
                    CommonTools.Log($"Done. Saving historyt....");
                    SaveSearchHistory(history);

                    lbxYouTubeResults.Dispatcher.Invoke((Action)(() =>
                    {
                        pbYouTube.Visibility = Visibility.Collapsed;
                    }));
                });
            }
            catch (Exception ex)
            {
                CommonTools.HandleException(ex);
            }
        }

        private List<SearchHistory> ReadSearchHistory()
        {
            var ret = new List<SearchHistory>();

            var days = int.Parse(tbxDays.Text);

            try
            {
                var lines = File.ReadAllLines("keywords.txt");

                foreach (var l in lines)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(l))
                        {
                            int sep = l.IndexOf(' ');
                            var strDate = l.Substring(0, sep);
                            DateTime? date = null;
                            try
                            {
                                date = DateTime.ParseExact(strDate, "yyyyMMdd-HH:mm:ss", null);
                            }
                            catch (Exception ex)
                            {
                                CommonTools.HandleException(ex);
                            }
                            if (date == null)
                            {
                                date = DateTime.Now - TimeSpan.FromDays(days);
                            }
                            var strKey = l.Substring(sep + 1);
                            ret.Add(new SearchHistory()
                            {
                                LastUpdated = date.Value,
                                Keyword = strKey
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        CommonTools.HandleException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                CommonTools.HandleException(ex);
            }
            
            return ret;
        }

        private void SaveSearchHistory(List<SearchHistory> searchHistory)
        {
            try
            {
                var toSave = new List<string>();
                foreach (var s in searchHistory)
                {
                    toSave.Add(s.LastUpdated.ToString("yyyyMMdd-HH:mm:ss") + " " + s.Keyword);
                }
                CommonTools.Log($"Saving [{toSave.Count}] keys.");
                File.WriteAllLines("keywords.txt", toSave);
            }
            catch (Exception ex)
            {
                CommonTools.HandleException(ex);
            }
        }
    }
}
