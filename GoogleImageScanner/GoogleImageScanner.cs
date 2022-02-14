using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AnythingGalleryLoader;
using UnityEngine;
using UnityEngine.Networking;

namespace GoogleImageScanner
{
    public class GoogleImageScanner : IImageScanner, IInfoScanner, IRelatedScanner
    {
        private string searchQuery;
        private string currentShortQuery;
        private List<(string Url, string Title)> imageUrls = new List<(string Url, string Title)>();
        private List<(string Title, List<string> Paragraphs)> infoParagraphs = new List<(string Title, List<string> Paragraphs)>();
        private Queue<(string Title, string SourceUrl)> outstandingSourceUrls = new Queue<(string Title, string SourceUrl)>();
        private bool isLoadingSource;
        private int urlPtr;
        private int startOffset;
        private List<string> relatedQueries = new List<string>();
        private int relatedPtr;
        private static HttpWebRequest request;
        private TextInfo textInfo;

        #region Scanner
        public GoogleImageScanner()
        {
            textInfo = new CultureInfo("en-US", useUserOverride: false).TextInfo;
        }
        public void MUpdate()
        {
            if (!isLoadingSource && outstandingSourceUrls.Count > 0)
            {
                (string Title, string SourceUrl) tuple = outstandingSourceUrls.Dequeue();
                isLoadingSource = true;
                //MonoBehaviour.print("Loading");
                LoadInfo(tuple.Title, tuple.SourceUrl);
            }
        }
        public void ClearData()
        {
            imageUrls.Clear();
            relatedQueries.Clear();
            infoParagraphs.Clear();
            outstandingSourceUrls.Clear();
            searchQuery = string.Empty;
            currentShortQuery = string.Empty;
        }
        public void StartNewQuery(string query)
        {
            if (!(query == searchQuery))
            {
                string searchQueryBackup = searchQuery;
                searchQuery = query;
                CleanQuery();
                if (string.IsNullOrWhiteSpace(searchQuery))
                {
                    searchQuery = searchQueryBackup;
                    return;
                }

                currentShortQuery = query.ToLower();
                urlPtr = (startOffset = 0);

                LoadUrls(searchQuery);
            }
        }
        public void StartNewElaborateQuery(string query)
        {
            if (!(query == searchQuery))
            {
                if (!query.ToLower().Contains(currentShortQuery))
                {
                    searchQuery = currentShortQuery + query;
                }
                else
                {
                    searchQuery = query;
                }
                urlPtr = (startOffset = 0);
                CleanQuery();
                LoadUrls(searchQuery);
            }
        }
        #endregion Scanner

        #region ImageScanner
        public bool TryGetPainting(out string url, out string description)
        {
            if (imageUrls.Count == 0)
            {
                url = string.Empty;
                description = string.Empty;
                return false;
            }
            if (urlPtr >= imageUrls.Count)
            {
                urlPtr = 0;
            }
            url = imageUrls[urlPtr].Url;
            description = imageUrls[urlPtr].Title;
            urlPtr++;
            if (urlPtr >= imageUrls.Count)
            {
                urlPtr = 0;
                startOffset += imageUrls.Count;
                LoadUrls(searchQuery, startOffset);
            }
            return true;
        }
        #endregion ImageScanner

        #region InfoScanner
        public bool TryGetInfoText(out string description, out string info)
        {
            if (infoParagraphs.Count == 0)
            {
                description = string.Empty;
                info = string.Empty;
                return false;
            }
            (string Title, List<string> Paragraphs) tuple = infoParagraphs[UnityEngine.Random.Range(0, infoParagraphs.Count)];
            description = tuple.Title;
            info = tuple.Paragraphs[UnityEngine.Random.Range(0, tuple.Paragraphs.Count)];
            return true;
        }
        #endregion InfoScanner


        #region RelatedScanner
        public bool TryGetRelatedQuery(out string relatedSearch)
        {
            if (relatedQueries.Count == 0)
            {
                relatedSearch = string.Empty;
                return false;
            }
            if (relatedPtr >= relatedQueries.Count)
            {
                relatedPtr = 0;
            }
            relatedSearch = relatedQueries[relatedPtr];
            relatedPtr = (relatedPtr + 1) % relatedQueries.Count;
            return true;
        }
        #endregion RelatedScanner


        private void CleanQuery()
        {
            searchQuery = searchQuery.Replace(",", " ");
            searchQuery = searchQuery.Replace("-", " ");
            searchQuery = searchQuery.Replace("/", " ");
            searchQuery = searchQuery.Replace("(", " ");
            searchQuery = searchQuery.Replace(")", " ");
            searchQuery = searchQuery.Replace("\"", " ");
        }
        private void LoadUrls(string query, int startOffset = 0)
        {
            imageUrls.Clear();
            relatedQueries.Clear();
            infoParagraphs.Clear();
            outstandingSourceUrls.Clear();
            string arg = query.Replace(" ", "+").ToLowerInvariant();
            request = (HttpWebRequest)WebRequest.Create($"https://www.google.com/search?q={arg}&tbm=isch&source=lnms&start={startOffset}&safe=images");
            request.Accept = "text/html, application/xhtml+xml, */*";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";
            request.BeginGetResponse(FinishedResponse, null);
        }

        private void FinishedResponse(IAsyncResult result)
        {
            string empty = string.Empty;
            //imageUrls.Clear();
            //relatedQueries.Clear();
            //infoParagraphs.Clear();
            //outstandingSourceUrls.Clear();
            IHtmlDocument doc;
            using (HttpWebResponse httpWebResponse = (HttpWebResponse)request.EndGetResponse(result))
            using (Stream stream = httpWebResponse.GetResponseStream())
            //using (StreamReader streamReader = new StreamReader(stream))
            {
                //empty = streamReader.ReadToEnd();
                var parser = new HtmlParser();//System.ArgumentException
                doc = parser.ParseDocument(stream);
            }
            if (doc != null)
            {
                Dictionary<string, HashSet<IElement>> DataCache = new Dictionary<string, HashSet<IElement>>();
                foreach (IElement elem in doc.QuerySelectorAll("div,a"))
                {
                    foreach (IAttr attr in elem.Attributes)
                    {
                        //if(attr.Name.StartsWith("data-"))
                        {
                            string key = attr.Name + ":" + attr.Value;
                            if (!DataCache.ContainsKey(key))
                                DataCache[key] = new HashSet<IElement>();
                            DataCache[key].Add(elem);
                        }
                    }
                }
                List<HashSet<IElement>> Candidates = DataCache.OrderByDescending(dr => dr.Value.Count).Select(dr => dr.Value).ToList();
                //HashSet<IElement> MostPopularElemByData = DataCache.OrderByDescending(dr => dr.Value.Count).Select(dr => dr.Value).FirstOrDefault();
                //if (MostPopularElemByData != null)
                bool SearchResultsFound = false;
                bool RelatedSearchFound = false;
                HashSet<IElement> UsedElements = new HashSet<IElement>();
                foreach (HashSet<IElement> elemCollection in Candidates)
                {
                    bool iterationProcessed = false;
                    if (!SearchResultsFound && elemCollection.Any(dr => dr.QuerySelectorAll("[src]").Length > 0 && dr.QuerySelectorAll("[href]").Length > 0))
                    {
                        foreach (IElement item in elemCollection)
                        {
                            //string html = item.Html();
                            string url = item.QuerySelectorAll("[src]").Select(dr => dr.Attributes["src"].Value).Where(dr => Regex.IsMatch(dr, "https://encrypted-tbn0\\.gstatic\\.com/images(.*?)")).OrderByDescending(dr => dr.Length).FirstOrDefault();

                            string Title = null;
                            Queue<INode> TitleCandidates = new Queue<INode>();
                            TitleCandidates.Enqueue(item);
                            while (TitleCandidates.Count > 0)
                            {
                                INode e2 = TitleCandidates.Dequeue();
                                if (e2.NodeType == NodeType.Text)
                                {
                                    string localText = e2.Text()?.Trim();
                                    if (!string.IsNullOrWhiteSpace(localText))
                                    {
                                        Title = localText;
                                        break;
                                    }
                                }
                                else
                                {
                                    foreach (var e in e2.ChildNodes)
                                        TitleCandidates.Enqueue(e);
                                }
                            }

                            Title = textInfo.ToTitleCase(Title ?? item.TextContent ?? string.Empty);
                            //Match match2 = Regex.Match(html, ",\"https://(.*?)\",");
                            //string empty2 = string.Empty;
                            //if (match2.Value.Length > 0)
                            //{
                            //    empty2 = match2.Value;
                            //    empty2 = empty2.Replace("\"", "");
                            //    empty2 = empty2.Replace(",", "");
                            //    outstandingSourceUrls.Enqueue(new Tuple<string, string>(Title, empty2));
                            //}
                            string href = item.QuerySelectorAll("[href]").Select(dr => dr.Attributes["href"].Value).Where(dr => dr.Contains(@"&url=")).OrderByDescending(dr => dr.Length).FirstOrDefault()?.Trim();
                            if (!string.IsNullOrWhiteSpace(href))
                                try
                                {
                                    href = HttpUtility.ParseQueryString(new Uri("http://localhost" + href).Query)["url"]?.Trim();
                                }
                                catch { }
                            if (!string.IsNullOrWhiteSpace(href))
                                outstandingSourceUrls.Enqueue((Title, href));


                            var Qry = HttpUtility.ParseQueryString(request.RequestUri.Query);
                            string q_q = Qry["q"]?.Trim();
                            string q_start = Qry["start"]?.Trim();

                            if (string.IsNullOrWhiteSpace(Title))
                            {
                                imageUrls.Add((url, $"gogl://q={q_q}&start={q_start}"));
                            }
                            else
                            {
                                imageUrls.Add((url, $"{Title}\r\n\r\ngogl://q={q_q}&start={q_start}"));
                            }

                            SearchResultsFound = true;
                            iterationProcessed = true;
                            UsedElements.Add(item);
                        }
                    }
                    if (!iterationProcessed && !RelatedSearchFound && elemCollection.Any(dr => dr.TagName == "A"))// && !elemCollection.Any(dr => dr.QuerySelectorAll("[src]").Length > 0) && !elemCollection.Any(dr => dr.QuerySelectorAll("a[img]").Length > 0))
                    {
                        foreach (IElement item in elemCollection)
                        {
                            bool abortProc = false;
                            IElement parent = item.ParentElement;
                            while (parent != null)
                            {
                                if (UsedElements.Contains(parent))
                                {
                                    abortProc = true;
                                    break;
                                }
                                parent = parent.ParentElement;
                            }
                            if (abortProc)
                                break;

                            string related = item.TextContent.Trim();
                            if (!string.IsNullOrWhiteSpace(related))
                            {
                                relatedQueries.Add(related);
                                RelatedSearchFound = true;
                                iterationProcessed = true;
                            }
                        }
                    }
                    if (SearchResultsFound && RelatedSearchFound)
                        break;
                }
            }
        }

        private void LoadInfo(string title, string url)
        {
            AnythingGalleryLoader.ModInit.ImageScraper_StartCoroutine(InternalLoadInfo(title, url));
        }

        private IEnumerator InternalLoadInfo(string title, string url)
        {
            UnityWebRequest request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();
            if (!request.isNetworkError && !request.isHttpError)
            {
                MatchCollection matchCollection = Regex.Matches(request.downloadHandler.text, "<p>((.|\\n)*?)</p>");
                if (matchCollection.Count == 0)
                {
                    isLoadingSource = false;
                    yield break;
                }
                (string Title, List<string> Paragraphs) tuple = (title, new List<string>());
                foreach (Match item in matchCollection)
                {
                    string value = item.Value;
                    value = Regex.Replace(value, "<[a-z]+ (.*?)>|</[a-z]+>|<[a-z]+ />|<[a-z]+>", "");
                    value = value.Replace("<p>", "");
                    value = value.Replace("</p>", "");
                    if (value.Length >= 200)
                    {
                        tuple.Paragraphs.Add($"{value}\r\n\r\n{url}");
                    }
                }
                if (tuple.Paragraphs.Count > 0)
                {
                    infoParagraphs.Add(tuple);
                }
            }
            isLoadingSource = false;
        }
    }
}
