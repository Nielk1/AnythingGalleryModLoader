using AnythingGalleryLoader;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace YouTubeScanner
{
    public class YouTubeScanner : IVideoScanner
    {
        private string searchQuery;
        private Queue<(string Url, string Title)> videoUrls = new Queue<(string Url, string Title)>();
        private List<(string VideoUrl, string Title)> directUrls = new List<(string VideoUrl, string Title)>();
        private int urlIdx;
        private bool hasLoadedVideoUrls;
        private bool isLoadingUrl;
        private string Title = string.Empty;
        private TextInfo textInfo;
        private static HttpWebRequest request;
        private YoutubeDlContext ytdl;
        private bool ytdl_updated;
        private bool ytdl_busy;

        #region Scanner
        public YouTubeScanner()
        {
            textInfo = new CultureInfo("en-US", useUserOverride: false).TextInfo;
            ytdl = YoutubeDlContext.Instance;
            ytdl_busy = false;
        }
        public void MUpdate()
        {
            if(!ytdl_busy && !ytdl_updated)
            {
                ytdl_busy = true;
                UpdateYtDl();
            }
            if (!ytdl_busy && hasLoadedVideoUrls && !isLoadingUrl && videoUrls.Count > 0)
            {
                ytdl_busy = true;
                (string Url, string Title) tuple = videoUrls.Dequeue();
                DirectUrlFromUrl(tuple.Url);
                Title = tuple.Title;
                isLoadingUrl = true;
            }
        }
        public void ClearData() { }

        public void StartNewQuery(string query)
        {
            if (!(query == searchQuery))
            {
                searchQuery = query;
                LoadUrls(searchQuery);
            }
        }
        public void StartNewElaborateQuery(string query) => StartNewQuery(query);
        #endregion Scanner

        #region VideoScanner
        public bool TryGetVideo(out string url, out string title)
        {
            lock (directUrls)
            {
                if (directUrls.Count == 0)
                {
                    url = string.Empty;
                    title = string.Empty;
                    return false;
                }
                if (urlIdx >= directUrls.Count)
                {
                    urlIdx = 0;
                }
                int index = urlIdx++;
                url = directUrls[index].VideoUrl;
                title = directUrls[index].Title;
                return true;
            }
        }
        #endregion VideoScanner

        private void UpdateYtDl()
        {
            //AnythingGalleryLoader.ModInit.VideoScraper_StartCoroutine(UpdateYtDlCo());
            new Thread(() => UpdateYtDlCo()).Start();
        }

        private /*IEnumerator*/ void UpdateYtDlCo()
        {
            ytdl.Update();
            ytdl_updated = true;
            ytdl_busy = false;
            //yield break;
        }

        private void LoadUrls(string query)
        {
            videoUrls.Clear();
            lock (directUrls)
                directUrls.Clear();
            urlIdx = 0;
            hasLoadedVideoUrls = false;
            string arg = query.Replace(" ", "+").ToLowerInvariant();
            request = (HttpWebRequest)WebRequest.Create($"https://www.youtube.com/results?search_query={arg}");
            request.Accept = "text/html, application/xhtml+xml, */*";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";
            request.BeginGetResponse(LoadUrls_FinishedResponse, null);
        }

        private void LoadUrls_FinishedResponse(IAsyncResult result)
        {
            _ = string.Empty;
            using (HttpWebResponse httpWebResponse = (HttpWebResponse)request.EndGetResponse(result))
            {
                using (Stream stream = httpWebResponse.GetResponseStream())
                using (StreamReader streamReader = new StreamReader(stream))
                {
                    foreach (Match item in Regex.Matches(streamReader.ReadToEnd(), "\"title\":{\"runs\":\\[{\"text\":(.*?)\"watchEndpoint\""))
                    {
                        string Url = Regex.Match(item.Value, "\"url\":\"/watch\\?v=(.*?)\"").Value;
                        Url = Url.Replace("\"url\":\"", string.Empty);
                        Url = Url.Replace("\"", string.Empty);
                        string Title = Regex.Match(item.Value, "\"title\":{\"runs\":\\[{\"text\":\"(.*?)\"}\\]").Value;
                        Title = Title.Replace("\"title\":{\"runs\":[{\"text\":\"", string.Empty);
                        Title = Title.Replace("\"}]", string.Empty);
                        videoUrls.Enqueue(("http://www.youtube.com" + Url, Title));
                    }
                }
            }
            hasLoadedVideoUrls = true;
        }

        private void DirectUrlFromUrl(string url)
        {
            new Thread((_url) => GetYtDlInfo((string)_url)).Start(url);
        }

        private /*IEnumerator*/void GetYtDlInfo(string url)
        {
            var data = ytdl.GetYoutubeDlVideoData(url);
            if (data != null)
            {
                //data.title
                //data.alt_title
                var formatData = data.formats.Where(dr => !string.IsNullOrWhiteSpace(dr.url) && dr.vcodec != "none" && dr.acodec != "none").OrderBy(dr => dr.filesize).FirstOrDefault();

                if (formatData != null)
                {
                    lock (directUrls)
                        directUrls.Add((formatData.url, data.title));
                }
            }

            ytdl_busy = false;
            isLoadingUrl = false;

            //yield break;
        }
    }
}
