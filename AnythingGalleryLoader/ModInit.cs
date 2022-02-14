using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using TMPro;
using UnityEngine;

namespace AnythingGalleryLoader
{
    public class ModInit
    {
        static string BasePath;
        public static void Main()
        {
            // This method is called by Unity Doorstop during Mono initialization.
            // Place your mod initialization code here.
            // See https://github.com/NeighTools/UnityDoorstop/wiki for documentation.

            BasePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                Debug.Log($"Load Library Attempt \"{args.RequestingAssembly}\" requests \"{args.Name}\"");

                string candidate = Path.Combine(ModInit.BasePath, new AssemblyName(args.Name).Name + ".dll");
                if (File.Exists(candidate))
                    return Assembly.LoadFile(candidate);
                return null;
            };

            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        }

        //static bool IsSetup = false;
        //static object SetupLock = new object();

        static List<IScanner> Scanners = new List<IScanner>();

        static List<IScanner> ImageScanUpdateEvent = new List<IScanner>();
        static List<IScanner> VideoScanUpdateEvent = new List<IScanner>();

        static List<IVideoScanner> VideoScanners = new List<IVideoScanner>();
        static List<IImageScanner> ImageScanners = new List<IImageScanner>();
        static List<IRelatedScanner> RelatedScanners = new List<IRelatedScanner>();
        static List<IInfoScanner> InfoScanners = new List<IInfoScanner>();

        private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            // You have to wait until Unity is loaded before interacting with it.
            // The Main method is called before Unity has initialized.
            if (args.LoadedAssembly.GetName().Name == "UnityEngine")
            {
                Debug.Log($"Hello from AnythingGalleryLoader! ({args.LoadedAssembly})");
                //lock (SetupLock)
                {
                    //if (!IsSetup)
                    {
                        Debug.Log($"Applying Hooks");
                        
                        Debug.Log($"Hook ImageScraper.MUpdate");
                        On.ImageScraper.MUpdate += ImageScraper_MUpdate;
                        Debug.Log($"Hook ImageScraper.StartNewQuery");
                        On.ImageScraper.StartNewQuery += ImageScraper_StartNewQuery;
                        Debug.Log($"Hook ImageScraper.StartNewElaborateQuery");
                        On.ImageScraper.StartNewElaborateQuery += ImageScraper_StartNewElaborateQuery;
                        Debug.Log($"Hook ImageScraper.ClearData");
                        On.ImageScraper.ClearData += ImageScraper_ClearData;
                        Debug.Log($"Hook ImageScraper.TryGetURL");
                        On.ImageScraper.TryGetURL += ImageScraper_TryGetURL;
                        Debug.Log($"Hook ImageScraper.TryGetInfo");
                        On.ImageScraper.TryGetInfo += ImageScraper_TryGetInfo;
                        Debug.Log($"Hook ImageScraper.TryGetRelatedSearch");
                        On.ImageScraper.TryGetRelatedSearch += ImageScraper_TryGetRelatedSearch;

                        // these below hooks somehow break release mode
                        Debug.Log($"Hook VideoScraper.MUpdate");
                        On.VideoScraper.MUpdate += VideoScraper_MUpdate;
                        Debug.Log($"Hook VideoScraper.TryGetDirectUrl");
                        On.VideoScraper.TryGetDirectUrl += VideoScraper_TryGetDirectUrl;
                        Debug.Log($"Hook VideoScraper.StartNewQuery");
                        On.VideoScraper.StartNewQuery += VideoScraper_StartNewQuery;

                        Debug.Log($"Hook RequestManager.Show");
                        On.RequestManager.Show += RequestManager_Show;
                        
                        Debug.Log($"Collecting Mods");

                        try
                        {
                            List<Assembly> allAssemblies = new List<Assembly>();
                            allAssemblies.Add(typeof(IImageScanner).GetTypeInfo().Assembly);
                            string path = Path.Combine(Path.GetDirectoryName(BasePath), "mods");

                            foreach (string dll in Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories))
                            {
                                try
                                {
                                    Assembly loadedAssembly = Assembly.LoadFile(dll);
                                    allAssemblies.Add(loadedAssembly);
                                }
                                catch (FileLoadException loadEx)
                                { } // The Assembly has already been loaded.
                                catch (BadImageFormatException imgEx)
                                { } // If a BadImageFormatException exception is thrown, the file is not an assembly.

                            } // foreach dll

                            foreach (Assembly asm in allAssemblies)
                            {
                                try
                                {
                                    bool LoadedAssembly = false;
                                    foreach (Type item in asm.GetTypes())
                                    {
                                        //if (!item.IsClass) continue;
                                        if (item.GetInterfaces().Contains(typeof(IScanner)))
                                        {
                                            if (!LoadedAssembly)
                                            {
                                                LoadedAssembly = true;
                                            }
                                            ConstructorInfo[] cons = item.GetConstructors();
                                            foreach (ConstructorInfo con in cons)
                                            {
                                                try
                                                {
                                                    ParameterInfo[] @params = con.GetParameters();
                                                    object[] paramList = new object[@params.Length];
                                                    // don't worry about paramaters for now
                                                    //for (int i = 0; i < @params.Length; i++)
                                                    //{
                                                    //    paramList[i] = ServiceProvider.GetService(@params[i].ParameterType);
                                                    //}

                                                    IScanner scannerObject = (IScanner)Activator.CreateInstance(item, paramList);
                                                    Scanners.Add(scannerObject);
                                                    bool sawNonVideoScanner = false;
                                                    if (scannerObject is IImageScanner) { ImageScanners.Add((IImageScanner)scannerObject); sawNonVideoScanner = true; }
                                                    if (scannerObject is IRelatedScanner) { RelatedScanners.Add((IRelatedScanner)scannerObject); sawNonVideoScanner = true; }
                                                    if (scannerObject is IInfoScanner) { InfoScanners.Add((IInfoScanner)scannerObject); sawNonVideoScanner = true; }
                                                    if (scannerObject is IVideoScanner) { VideoScanners.Add((IVideoScanner)scannerObject); }
                                                    if (sawNonVideoScanner)
                                                    {
                                                        ImageScanUpdateEvent.Add(scannerObject);
                                                    }
                                                    else
                                                    {
                                                        // not sure what the deal is with differnt update triggers, so we're ggoing to stuff video only providers into the video scanner update event to be safe
                                                        VideoScanUpdateEvent.Add(scannerObject);
                                                    }

                                                    break;
                                                }
                                                catch { }
                                            }
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.Log(ex);
                        }

                        //IsSetup = true;
                    }
                }
            }
        }

        private static void RequestManager_Show(On.RequestManager.orig_Show orig, bool visible)
        {
            orig(visible);
            if (visible)
            {
                Type type = typeof(RequestManager);
                FieldInfo info = type.GetField("instance", BindingFlags.NonPublic | BindingFlags.Static);
                RequestManager value = (RequestManager)info.GetValue(null);

                TMP_InputField inputField = value.inputField;

                inputField.ActivateInputField();
            }
        }




        private static void VideoScraper_MUpdate(On.VideoScraper.orig_MUpdate orig, VideoScraper self)
        {
            foreach(IScanner scanner in VideoScanUpdateEvent)
            {
                scanner.MUpdate();
            }
        }
        private static bool VideoScraper_TryGetDirectUrl(On.VideoScraper.orig_TryGetDirectUrl orig, out string url, out string title)
        {
            foreach (IVideoScanner scanner in VideoScanners.Shuffle())
            {
                if (scanner.TryGetVideo(out url, out title))
                    return true;
            }
            url = string.Empty;
            title = string.Empty;
            return false;
        }
        private static void VideoScraper_StartNewQuery(On.VideoScraper.orig_StartNewQuery orig, string query)
        {
            foreach(IVideoScanner scanner in VideoScanners)
            {
                scanner.StartNewQuery(query);
            }
        }




        private static void ImageScraper_MUpdate(On.ImageScraper.orig_MUpdate orig, ImageScraper self)
        {
            foreach (IScanner scanner in ImageScanUpdateEvent)
            {
                scanner.MUpdate();
            }
        }

        private static void ImageScraper_ClearData(On.ImageScraper.orig_ClearData orig)
        {
            foreach (IScanner scanner in Scanners)
            {
                scanner.ClearData(); // normally this is only image based, but let's do it for all of them!
            }
        }

        private static void ImageScraper_StartNewQuery(On.ImageScraper.orig_StartNewQuery orig, string query)
        {
            foreach (IScanner scanner in ImageScanUpdateEvent)
            {
                scanner.StartNewQuery(query);
            }
        }

        // TODO Not sure this is ever actually used
        private static void ImageScraper_StartNewElaborateQuery(On.ImageScraper.orig_StartNewElaborateQuery orig, string query)
        {
            //foreach (IImageScanner scanner in ImageScanners)
            foreach (IScanner scanner in Scanners)
            {
                scanner.StartNewElaborateQuery(query);
            }
        }

        private static bool ImageScraper_TryGetURL(On.ImageScraper.orig_TryGetURL orig, out string url, out string description)
        {
            foreach (IImageScanner scanner in ImageScanners.Shuffle())
            {
                if (scanner.TryGetPainting(out url, out description))
                    return true;
            }
            url = string.Empty;
            description = string.Empty;
            return false;
        }

        private static bool ImageScraper_TryGetInfo(On.ImageScraper.orig_TryGetInfo orig, out string description, out string info)
        {
            foreach (IInfoScanner scanner in InfoScanners.Shuffle())
            {
                if (scanner.TryGetInfoText(out description, out info))
                    return true;
            }
            description = string.Empty;
            info = string.Empty;
            return false;
        }

        private static bool ImageScraper_TryGetRelatedSearch(On.ImageScraper.orig_TryGetRelatedSearch orig, out string relatedSearch)
        {
            foreach (IRelatedScanner scanner in RelatedScanners.Shuffle())
            {
                if (scanner.TryGetRelatedQuery(out relatedSearch))
                    return true;
            }
            relatedSearch = string.Empty;
            return false;
        }

        public static Coroutine ImageScraper_StartCoroutine(System.Collections.IEnumerator routine)
        {
            Type type = typeof(ImageScraper);
            FieldInfo info = type.GetField("instance", BindingFlags.NonPublic | BindingFlags.Static);
            ImageScraper value = (ImageScraper)info.GetValue(null);
            return value.StartCoroutine(routine);
        }

        public static Coroutine VideoScraper_StartCoroutine(System.Collections.IEnumerator routine)
        {
            Type type = typeof(VideoScraper);
            FieldInfo info = type.GetField("instance", BindingFlags.NonPublic | BindingFlags.Static);
            VideoScraper value = (VideoScraper)info.GetValue(null);
            return value.StartCoroutine(routine);
        }
    }
}
