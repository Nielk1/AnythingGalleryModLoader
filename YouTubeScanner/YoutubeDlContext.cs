using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace YouTubeScanner
{
    class YoutubeDlContext
    {
        private static YoutubeDlContext _instance;
        private YoutubeDlContext() { }
        public static YoutubeDlContext Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new YoutubeDlContext();
                return _instance;
            }
        }

        private Process GetProc(string args, bool newWindow = false)
        {
            Process proc = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    //WorkingDirectory = "steamcmd",
                    FileName = "mod_deps/youtube-dl.exe",
                    //FileName = "youtube-dlc.exe",
                    Arguments = "--no-check-certificate " + args,
                    UseShellExecute = newWindow,//false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = !newWindow,//true,
                    RedirectStandardInput = !newWindow,//true,
                    RedirectStandardError = !newWindow,//true,
                    //StandardOutputEncoding = Encoding.Unicode,
                    //StandardErrorEncoding = Encoding.Unicode,
                }
            };

            return proc;
        }

        public string Update()
        {
            string outputText = null;
            StringBuilder output = new StringBuilder();

            if (!File.Exists(Path.Combine("mod_deps", "youtube-dl.exe")))
            {
                WebClient client = new WebClient();
                string versionInfo = client.DownloadString(@"https://yt-dl.org/update/versions.json");
                JObject verObj = JObject.Parse(versionInfo);
                string latestVersion = verObj["latest"].Value<string>();
                string downloadURL = verObj["versions"][latestVersion]["exe"].First.Value<string>();
                client.DownloadFile(downloadURL, Path.Combine("mod_deps", "youtube-dl.exe"));
            }

            {
                Process proc = GetProc($"--update");
                proc.Start();

                while (!proc.StandardOutput.EndOfStream)
                {
                    output.AppendLine(proc.StandardOutput.ReadLine());
                }
                proc.WaitForExit();
                outputText = output.ToString();
            }

            return outputText?.Trim();
        }

        public YoutubeDlVideo GetYoutubeDlVideoData(string url)
        {
            string outputText = null;
            string outputEText = null;
            StringBuilder output = new StringBuilder();
            StringBuilder outputE = new StringBuilder();

            {
                //Process proc = GetProc($"--user-agent \"Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)\" --dump-single-json \"{url.Replace("\"", "\"\"")}\"");
                Process proc = GetProc($"--dump-single-json \"{url.Replace("\"", "\"\"")}\"");
                proc.Start();

                while (!proc.StandardOutput.EndOfStream)
                {
                    output.AppendLine(proc.StandardOutput.ReadLine());
                }
                while (!proc.StandardError.EndOfStream)
                {
                    outputE.AppendLine(proc.StandardError.ReadLine());
                }
                proc.WaitForExit();
                outputText = output.ToString();
                outputEText = outputE.ToString();
                Console.Error.WriteLine(outputEText);
            }

            if (string.IsNullOrWhiteSpace(outputText))
            {
                return null;
            }

            JsonSerializerSettings settings = new JsonSerializerSettings();
            //settings.MissingMemberHandling = MissingMemberHandling.Error;

            //try
            {
                YoutubeDlVideo val = JsonConvert.DeserializeObject<YoutubeDlVideo>(outputText, settings);
                return val;
            }
            //catch (Exception ex)
            //{
            //}
        }
    }

    public class YoutubeDlVideo : YoutubeDlFormat
    {
        public bool removed_by_user { get; set; }
        public bool removed { get; set; }
        public bool unavailable { get; set; }
        public bool copystruck { get; set; }

        public string alt_title { get; set; }
        public bool? is_live { get; set; }
        public float? stretched_ratio { get; set; }
        public int? playlist_index { get; set; }
        public int? age_limit { get; set; }
        public float? average_rating { get; set; }
        public string description { get; set; }
        public long? dislike_count { get; set; }
        public string display_id { get; set; }
        public float? duration { get; set; }
        public string episode { get; set; }
        public int? episode_number { get; set; }
        public string extractor { get; set; }
        public string extractor_key { get; set; }
        public string id { get; set; }
        public string license { get; set; }
        public long? like_count { get; set; }
        public string season { get; set; }
        public int? season_number { get; set; }
        public string series { get; set; }
        public string thumbnail { get; set; }
        public string title { get; set; }
        public string upload_date { get; set; }
        public string uploader { get; set; }
        public string uploader_id { get; set; }
        public string uploader_url { get; set; }
        public long? view_count { get; set; }
        public string webpage_url { get; set; }
        public string webpage_url_basename { get; set; }
        public List<YoutubeDlTumbnail> thumbnails { get; set; }
        public List<YoutubeDlFormat> formats { get; set; }
        public List<YoutubeDlFormat> requested_formats { get; set; }
        public List<string> tags { get; set; }
        public List<string> categories { get; set; }
    }

    public class YoutubeDlTumbnail
    {
        public string url { get; set; }
        public string id { get; set; }
    }

    public class YoutubeDlDownloadOptions
    {
        public int? http_chunk_size { get; set; }
    }

    public class YoutubeDlFormat
    {
        public float? abr { get; set; }
        public float? tbr { get; set; }
        public float? fps { get; set; } // should this be an int or a float?
        public string vcodec { get; set; }
        public string format_note { get; set; }
        public string player_url { get; set; }
        public string format_id { get; set; }
        public Int64? filesize { get; set; }
        public Dictionary<string, string> http_headers { get; set; }
        public string protocol { get; set; }
        public string resolution { get; set; }
        public string acodec { get; set; }
        public YoutubeDlDownloadOptions downloader_options { get; set; }
        public int? quality { get; set; }
        public string format { get; set; }
        public string ext { get; set; }
        public string container { get; set; }
        public string manifest_url { get; set; }
        public string url { get; set; }
        public int? height { get; set; }
        public int? width { get; set; }
    }
}
