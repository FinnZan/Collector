﻿using Dell.Utilities;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CollectorUI
{
    public class SearchHistory
    {
        public string Keyword { get; set; }

        public DateTime LastUpdated { get; set; }
    }

    public class YouTubeCrawlerProgress
    {
        public string Keyword { get; set; }

        public int Total { get; set; }

        public int Current { get; set; }

        public VideoFrame[] Frames { get; set; }
    }

    public class VideoFrame
    {
        public string Title { get; set; }
        public DateTime Time { get; set; }
        public string Thumbnail { get; set; }
        public string Url { get; set; }
        public int FrameIndex { get; set; }

        public TestResult TestResult { get; set; }

        public double Score
        {
            get
            {
                if (TestResult != null)
                {
                    return TestResult.Scores[0].Value;
                }
                else
                {
                    return -1;
                }
            }
        }
    }

    public class YouTubeCrawler
    {
        public event EventHandler<YouTubeCrawlerProgress> ProgressChanged;

        public long CurrentSearchId { get; private set; }
        
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

        public Task Search(List<SearchHistory> history)
        {
            CurrentSearchId = DateTime.Now.Ticks;
            
            return Task.Run(() =>
            {                
                var localCurrentSearchId = CurrentSearchId;

                foreach (var key in history)
                {
                    CommonTools.Log($"Search [{key.Keyword}]");

                    var searchTime = DateTime.Now;
                    var searchResults = FindVideo(key.Keyword, key.LastUpdated);

                    CommonTools.Log(searchResults.Count() + " videos.");

                    for (int i = searchResults.Count() - 1; i >= 0 && localCurrentSearchId == CurrentSearchId; i--)
                    {
                        try
                        {
                            var s = searchResults[i];
                            CommonTools.Log($"Video [{i}]");

                            VideoFrame[] frames = new VideoFrame[4];
                            for (int j = 0; j < 4; j++)
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

                                frames[j] = new VideoFrame()
                                {
                                    Title = s.Snippet.Title,
                                    Time = s.Snippet.PublishedAt.Value,
                                    Url = $"https://" + $"www.youtube.com/watch?v={s.Id.VideoId}",
                                    Thumbnail = thumb,
                                    FrameIndex = j
                                };
                            }

                            if (localCurrentSearchId == CurrentSearchId)
                            {
                                ProgressChanged?.Invoke(this, new YouTubeCrawlerProgress()
                                {
                                    Keyword = key.Keyword,
                                    Frames = frames,
                                    Total = searchResults.Count(),
                                    Current = searchResults.Count() - i - 1,
                                });
                            }

                            Parallel.ForEach(frames, (frame) =>
                            {
                                try
                                {
                                    frame.TestResult = _classifier.Test(frame.Thumbnail); ;
                                }
                                catch (Exception ex)
                                {
                                    CommonTools.HandleException(ex);
                                }
                            });

                            if (localCurrentSearchId == CurrentSearchId)
                            {
                                ProgressChanged?.Invoke(this, new YouTubeCrawlerProgress()
                                {
                                    Keyword = key.Keyword,
                                    Frames = frames,
                                    Total = searchResults.Count(),
                                    Current = searchResults.Count() - i - 1,
                                });
                            }

                            key.LastUpdated = s.Snippet.PublishedAt.Value;
                        }
                        catch (Exception ex)
                        {
                            CommonTools.HandleException(ex);
                        }
                    }

                    key.LastUpdated = searchTime;
                }
            });
        }

        private IList<SearchResult> FindVideo(string key, DateTime after)
        {
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = "AIzaSyA-mks7cOP71VwTwDmn7nfG1qlfQgGtTNU",
                ApplicationName = "API key 5"
            });

            List<SearchResult> all = new List<SearchResult>();
            var oldestFound = DateTime.Now;
            var num = 50;

            while (num >= 40) 
            {
                var searchListRequest = youtubeService.Search.List("snippet");
                searchListRequest.Q = key; // Replace with your search term.
                searchListRequest.MaxResults = 50;
                searchListRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
                searchListRequest.PublishedBefore = oldestFound;
                searchListRequest.PublishedAfter = after;

                var searchListResponse = searchListRequest.ExecuteAsync().Result;
                var items = searchListResponse.Items.Where(s => s.Id.Kind == "youtube#video").ToArray();
                num = items.Length;

                if (items.Length > 0)
                {
                    oldestFound = items[items.Length - 1].Snippet.PublishedAt.Value;
                    all.AddRange(items);
                }
            }            

            return all;
        }

        public void Cancel()
        {

        }
    }
}
    

