using Dell.Utilities;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace CollectorUI
{
    public class YouTubeCrawlerProgress
    {
        public int Total { get; set; }

        public int Current { get; set; }

        public VideoFrame Latest { get; set; }
    }

    public class VideoFrame
    {
        public string Title { get; set; }
        public DateTime Time { get; set; }
        public string Thumbnail { get; set; }
        public string Url { get; set; }
        public int FrameIndex { get; set; }

        public double? Score { get; set; }
    }

    public class YouTubeCrawler
    {
        public event EventHandler<YouTubeCrawlerProgress> ProgressChanged;

        public long CurrentSearchId { get; private set; }

        public List<VideoFrame> Results { get; private set; } = new List<VideoFrame>();

        private Classifier _classifier = null;

        private string _tempFolder;

        public YouTubeCrawler(Classifier classifier, string working)
        {
            _classifier = classifier;

            _tempFolder = Path.Combine(working, "YouTube");

            if (!Directory.Exists(_tempFolder))
            {
                Directory.CreateDirectory(_tempFolder);
                CommonTools.Log($"Creating directory [{_tempFolder}]");
            }
        }

        public List<VideoFrame> Search(string key, string group, double pass)
        {
            Results.Clear();
            CurrentSearchId = DateTime.Now.Ticks;

            var localCurrentSearchId = CurrentSearchId;

            try
            {
                CommonTools.Log($"Search [{key}]");

                var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    ApiKey = "AIzaSyA-mks7cOP71VwTwDmn7nfG1qlfQgGtTNU",
                    ApplicationName = "API key 5"
                });

                var searchListRequest = youtubeService.Search.List("snippet");
                searchListRequest.Q = key; // Replace with your search term.
                searchListRequest.MaxResults = 50;
                searchListRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
                searchListRequest.PublishedAfter = DateTime.Now - TimeSpan.FromDays(1);

                var searchListResponse = searchListRequest.ExecuteAsync().Result;

                List<string> videos = new List<string>();

                var searchResults = searchListResponse.Items.Where(s => s.Id.Kind == "youtube#video").ToArray();
                CommonTools.Log(searchResults.Count() + " videos.");

                // Add each result to the appropriate list, and then display the lists of
                // matching videos, channels, and playlists.
                for (int i = 0; i < searchResults.Count() && localCurrentSearchId == CurrentSearchId; i++)
                {
                    var s = searchResults[i];

                    CommonTools.Log($"Video [{i}]");
                    videos.Add(String.Format("{0} ({1})", s.Snippet.Title, s.Id.VideoId));

                    for (int j = 0; j < 3; j++)
                    {
                        try
                        {
                            var urlThumb = "http://" + $"img.youtube.com/vi/{s.Id.VideoId}/{j}.jpg";
                            var thumb = Path.Combine(_tempFolder, $"{s.Id.VideoId}_{j}.jpg");

                            HttpWebRequest httpRequest = (HttpWebRequest)
                            WebRequest.Create(urlThumb);
                            httpRequest.Method = WebRequestMethods.Http.Get;

                            using (Stream output = File.OpenWrite(thumb))
                            using (Stream input = httpRequest.GetResponse().GetResponseStream())
                            {
                                input.CopyTo(output);
                            }

                            var v = new VideoFrame()
                            {
                                Title = s.Snippet.Title,
                                Time = s.Snippet.PublishedAt.Value,
                                Url = $"https://" + $"www.youtube.com/watch?v={s.Id.VideoId}",
                                Thumbnail = thumb,
                                FrameIndex = j
                            };

                            if (localCurrentSearchId == CurrentSearchId)
                            {
                                ProgressChanged?.Invoke(this, new YouTubeCrawlerProgress()
                                {
                                    Latest = v,
                                    Total = searchResults.Count(),
                                    Current = i,
                                });
                            }

                            var result = _classifier.Test(thumb);

                            v.Score = result.Scores[0].Value;

                            if (localCurrentSearchId == CurrentSearchId)
                            {
                                ProgressChanged?.Invoke(this, new YouTubeCrawlerProgress()
                                {
                                    Latest = v,
                                    Total = searchResults.Count(),
                                    Current = i,
                                });
                            }

                            if (result.Scores[0].Key == group && result.Scores[0].Value > pass)
                            {
                                CommonTools.Log($"Found [{v.Title}]");
                                System.Diagnostics.Process.Start(v.Url);
                                Results.Add(v);
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            CommonTools.HandleException(ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CommonTools.HandleException(ex);
            }

            if (localCurrentSearchId == CurrentSearchId)
            {
                return Results;
            }
            else
            {
                throw new Exception("Cancelled.");
            }
        }

        public void Cancel()
        {

        }
    }
}
    

