using AnythingGalleryLoader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace ArchiveOrgScanner
{
    public class ArchiveOrgScanner : IImageScanner, IRelatedScanner, IInfoScanner, IVideoScanner
    {
        private int VideoListIndex;
        private uint page;
        private int relatedPtr;
        private int PaintingListIndex;
        private string searchQuery;
        private string currentShortQuery;
        private List<string> Related;
        private HashSet<string> RelatedUnique;
        private List<(string Url, string Desc)> Images;
        private List<(string Title, List<string> Paragraphs)> InfoParagraphs;
        private List<(string VideoUrl, string Title)> VideoItems = new List<(string VideoUrl, string Title)>();
        private Queue<string> outstandingSourceUrls;
        private static HttpWebRequest request;
        //private bool isLoadingSource;
        private int isLoadingSourceCount;
        const int LOADING_SOURCE_MAX = 10;
        public ArchiveOrgScanner()
        {
            page = 1;
            searchQuery = string.Empty;
            Related = new List<string>();
            RelatedUnique = new HashSet<string>();
            Images = new List<(string Url, string Desc)>();
            InfoParagraphs = new List<(string Title, List<string> Paragraphs)>();
            outstandingSourceUrls = new Queue<string>();
        }
        public void MUpdate()
        {
            //if (!isLoadingSource && outstandingSourceUrls.Count > 0)
            if (isLoadingSourceCount < LOADING_SOURCE_MAX && outstandingSourceUrls.Count > 0)
            {
                string id = outstandingSourceUrls.Dequeue();
                //isLoadingSource = true;
                isLoadingSourceCount++;
                //MonoBehaviour.print("Loading");
                LoadInfo(id);
            }
        }

        public void ClearData()
        {
            Related.Clear();
            RelatedUnique.Clear();
            Images.Clear();
            InfoParagraphs.Clear();
            searchQuery = string.Empty;
            currentShortQuery = string.Empty;
        }

        public void StartNewQuery(string query)
        {
            if (!(query == searchQuery))
            {
                string searchQueryBackup = searchQuery;
                searchQuery = query;
                searchQuery = CleanQuery(searchQuery);
                if (string.IsNullOrWhiteSpace(searchQuery))
                {
                    searchQuery = searchQueryBackup;
                    return;
                }

                currentShortQuery = query.ToLower();
                PaintingListIndex = 0;
                page = 1;


                Related.Clear();
                RelatedUnique.Clear();
                Images.Clear();
                InfoParagraphs.Clear();
                VideoItems.Clear();
                VideoListIndex = 0;

                LoadUrls(searchQuery, page);
            }
        }
        public void StartNewElaborateQuery(string query) => StartNewQuery(query);



        #region VideoScanner
        public bool TryGetVideo(out string url, out string title)
        {
            url = string.Empty;
            title = string.Empty;
            if (VideoItems.Count == 0)
                return false;
            bool retVal = false;
            if (VideoListIndex < VideoItems.Count)
            {
                url = VideoItems[VideoListIndex].VideoUrl;
                title = VideoItems[VideoListIndex].Title;
                retVal = true;
            }
            VideoListIndex++;
            if (VideoListIndex >= VideoItems.Count)
            {
                VideoListIndex = 0;
                page++;

                VideoItems.Clear();

                LoadUrls(searchQuery, page);
            }
            return retVal;
        }
        #endregion VideoScanner



        private string CleanQuery(string query)
        {
            query = query.Replace(",", " ");
            query = query.Replace("-", " ");
            query = query.Replace("/", " ");
            query = query.Replace("(", " ");
            query = query.Replace(")", " ");
            query = query.Replace("\"", " ");
            return query;
        }
        private void LoadUrls(string query, uint page)
        {
            string arg = query.Replace(" ", "+").ToLowerInvariant();
            request = (HttpWebRequest)WebRequest.Create($"https://archive.org/advancedsearch.php?q={query}&rows=250&page={page}&output=json");
            request.Accept = "text/html, application/xhtml+xml, */*";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";
            request.BeginGetResponse(FinishedResponse, null);
        }
        private void FinishedResponse(IAsyncResult result)
        {
            //RelatedList.Clear();
            //Related.Clear();
            //Images.Clear();
            //infoParagraphs.Clear();
            using (HttpWebResponse httpWebResponse = (HttpWebResponse)request.EndGetResponse(result))
            using (Stream stream = httpWebResponse.GetResponseStream())
            using (StreamReader streamReader = new StreamReader(stream))
            {
                string formattedResponse = streamReader.ReadToEnd();
                if (formattedResponse.StartsWith("callback("))
                {
                    formattedResponse = formattedResponse
                        .Substring("callback(".Length)
                        .TrimEnd(')');
                }

                var archiveResponse = JObject.Parse(formattedResponse);

                try
                {
                    foreach (JObject obj in (JArray)(archiveResponse["response"]["docs"]))
                    {
                        switch (obj["mediatype"].Value<string>())
                        {
                            case "image":
                            case "movies":
                                outstandingSourceUrls.Enqueue(obj["identifier"].Value<string>());
                                break;
                        }
                    }
                }
                catch { }
            }
        }

        private void LoadInfo(string id)
        {
            AnythingGalleryLoader.ModInit.ImageScraper_StartCoroutine(InternalLoadInfo(id));
        }


        /// <summary>
        /// we are assuming as these are Coroutines that they are not going to parallel access the collections, if they do we will need to add locks to the collections
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private IEnumerator InternalLoadInfo(string id)
        {
            UnityWebRequest request = UnityWebRequest.Get($"https://archive.org/details/{id}?output=json");
            yield return request.SendWebRequest();
            if (!request.isNetworkError && !request.isHttpError)
            {
                string rawDom = request.downloadHandler.text;
                JObject obj = JObject.Parse(rawDom);

                if (obj["metadata"]["mature_content"] == null || obj["metadata"]["mature_content"].First.Value<string>() == "False")
                {
                    if (obj["metadata"]["subject"] != null)
                    {
                        foreach (string subjectTags in (JArray)(obj["metadata"]["subject"]))
                        {
                            foreach (string subjectTagx in subjectTags.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                string subjectTag = subjectTagx.Trim();
                                if (!string.IsNullOrWhiteSpace(subjectTag))
                                {
                                    if (!RelatedUnique.Contains(subjectTag))
                                    {
                                        if (searchQuery.ToLowerInvariant() != CleanQuery(subjectTag.ToLowerInvariant()))
                                        {
                                            RelatedUnique.Add(subjectTag);
                                            Related.Add(subjectTag);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    string mediatype = obj["metadata"]["mediatype"].First.Value<string>();
                    switch(mediatype)
                    {
                        case "image":
                            {
                                string url = obj["misc"]?["image"]?.Value<string>();
                                string title = obj["metadata"]?["title"]?.First?.Value<string>();
                                string description = obj["metadata"]?["description"]?.First?.Value<string>();

                                if (!string.IsNullOrWhiteSpace(url))
                                {
                                    if (string.IsNullOrWhiteSpace(title))
                                    {
                                        Images.Add((url, $"ia://{id}"));
                                    }
                                    else
                                    {
                                        Images.Add((url, $"{title}\r\n\r\nia://{id}"));
                                    }

                                    yield return null;

                                    if (!string.IsNullOrWhiteSpace(description))
                                    {
                                        MatchCollection matchCollection = Regex.Matches(description, "<p>((.|\\n)*?)</p>");
                                        if (matchCollection.Count > 0)
                                        {
                                            (string Title, List<string> Paragraphs) tuple = (title, new List<string>());
                                            foreach (Match item in matchCollection)
                                            {
                                                string value = item.Value;
                                                value = Regex.Replace(value, "<[a-z]+ (.*?)>|</[a-z]+>|<[a-z]+ />|<[a-z]+>", "");
                                                value = value.Replace("<p>", "");
                                                value = value.Replace("</p>", "");
                                                if (value.Length >= 200)
                                                {
                                                    tuple.Paragraphs.Add($"{value}\r\n\r\nia://{id}");
                                                }
                                            }
                                            if (tuple.Paragraphs.Count > 0)
                                            {
                                                InfoParagraphs.Add(tuple);
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                        case "movies":
                            {
                                string title = obj["metadata"]?["title"]?.First?.Value<string>();
                                string dir = obj["dir"]?.Value<string>();

                                string filename = null;
                                long size = long.MaxValue;
                                foreach (JProperty data in ((JObject)(obj["files"])).Properties())
                                {
                                    // small video check
                                    if(new string[] { "512Kb MPEG4", /*"Ogg Video",*/ }.Contains(data.Value["format"].Value<string>()))
                                    {
                                        long newSize = long.Parse(data.Value["size"].Value<string>());
                                        if(newSize < size)
                                        {
                                            filename = data.Name;
                                            newSize = size;
                                        }
                                    }
                                }

                                if(filename != null)
                                {
                                    string url = "https://archive.org" + dir + filename;
                                    if (string.IsNullOrWhiteSpace(title))
                                    {
                                        VideoItems.Add((url, $"ia://{id}"));
                                    }
                                    else
                                    {
                                        VideoItems.Add((url, $"{title}\r\n\r\nia://{id}"));
                                    }
                                }

                            }
                            break;
                    }
                }
            }
            //isLoadingSource = false;
            isLoadingSourceCount--;
        }


        #region ImageScanner
        public bool TryGetPainting(out string url, out string description)
        {
            url = string.Empty;
            description = string.Empty;
            if (Images.Count == 0)
                return false;
            bool retVal = false;
            if (PaintingListIndex < Images.Count)
            {
                url = Images[PaintingListIndex].Url;
                description = Images[PaintingListIndex].Desc;
                retVal = true;
            }
            PaintingListIndex++;
            if (PaintingListIndex >= Images.Count)
            {
                PaintingListIndex = 0;
                page++;

                Related.Clear();
                RelatedUnique.Clear();
                Images.Clear();
                InfoParagraphs.Clear();

                LoadUrls(searchQuery, page);
            }
            return true;
        }
        #endregion ImageScanner

        #region InfoScanner
        public bool TryGetInfoText(out string description, out string info)
        {
            if (InfoParagraphs.Count == 0)
            {
                description = string.Empty;
                info = string.Empty;
                return false;
            }
            (string Title, List<string> Paragraphs) tuple = InfoParagraphs[UnityEngine.Random.Range(0, InfoParagraphs.Count)];
            description = tuple.Title;
            info = tuple.Paragraphs[UnityEngine.Random.Range(0, tuple.Paragraphs.Count)];
            return true;
        }
        #endregion InfoScanner


        #region RelatedScanner
        public bool TryGetRelatedQuery(out string relatedSearch)
        {
            if (Related.Count == 0)
            {
                relatedSearch = string.Empty;
                return false;
            }
            if (relatedPtr >= Related.Count)
            {
                relatedPtr = 0;
            }
            relatedSearch = Related[relatedPtr];
            relatedPtr = (relatedPtr + 1) % Related.Count;
            return true;
        }
        #endregion RelatedScanner
    }
}
