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
                //Debug.Log($"Load Library Attempt \"{args.RequestingAssembly}\" requests \"{args.Name}\"");

                string candidate = Path.Combine(ModInit.BasePath, new AssemblyName(args.Name).Name + ".dll");
                if (File.Exists(candidate))
                    return Assembly.LoadFile(candidate);
                return null;
            };

            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        }

        static List<IScanner> Scanners = new List<IScanner>();

        static List<IScanner> ImageScanUpdateEvent = new List<IScanner>();
        static List<IScanner> VideoScanUpdateEvent = new List<IScanner>();

        static List<IVideoScanner> VideoScanners = new List<IVideoScanner>();
        static List<IImageScanner> ImageScanners = new List<IImageScanner>();
        static List<IRelatedScanner> RelatedScanners = new List<IRelatedScanner>();
        static List<IInfoScanner> InfoScanners = new List<IInfoScanner>();

        static List<ITileset> TilesetManagers = new List<ITileset>();


        private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            // You have to wait until Unity is loaded before interacting with it.
            // The Main method is called before Unity has initialized.
            if (args.LoadedAssembly.GetName().Name == "UnityEngine")
            {
                Debug.Log($"Hello from AnythingGalleryLoader!");

                // TODO find a way to do this more reliably, but without this non-dev UnityPlayer.dll crashes
                new Thread(() =>
                {
                    Thread.Sleep(1000);

                    On.ImageScraper.MUpdate += ImageScraper_MUpdate;
                    On.ImageScraper.StartNewQuery += ImageScraper_StartNewQuery;
                    On.ImageScraper.StartNewElaborateQuery += ImageScraper_StartNewElaborateQuery;
                    On.ImageScraper.ClearData += ImageScraper_ClearData;
                    On.ImageScraper.TryGetURL += ImageScraper_TryGetURL;
                    On.ImageScraper.TryGetInfo += ImageScraper_TryGetInfo;
                    On.ImageScraper.TryGetRelatedSearch += ImageScraper_TryGetRelatedSearch;

                    On.VideoScraper.MUpdate += VideoScraper_MUpdate;
                    On.VideoScraper.TryGetDirectUrl += VideoScraper_TryGetDirectUrl;
                    On.VideoScraper.StartNewQuery += VideoScraper_StartNewQuery;

                    On.RequestManager.Show += RequestManager_Show;

                    On.Jigsaw.Tile.Overlaps_Tile += Tile_Overlaps_Tile;

                    On.Jigsaw.Connector.Start += Connector_Start;
                }).Start();

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
                                    LoadedAssembly = true;
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
                                if (item.GetInterfaces().Contains(typeof(ITileset)))
                                {
                                    LoadedAssembly = true;
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

                                            ITileset tilesetDescription = (ITileset)Activator.CreateInstance(item, paramList);
                                            TilesetManagers.Add(tilesetDescription);

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
            }
        }

        static bool TileSetupComplete = false;
        static Dictionary<string, Jigsaw.Tileset> TileSets = new Dictionary<string, Jigsaw.Tileset>();
        static Dictionary<string, Dictionary<string, int>> TileScannerCounts = new Dictionary<string, Dictionary<string, int>>()
        {
            { "Hallway_01",                   new Dictionary<string, int>(){ { "image"  , 2 } } },
            { "Hallway_02",                   new Dictionary<string, int>(){ { "image"  , 1 }, { "info", 1 } } },
            { "Hallway_03",                   new Dictionary<string, int>(){ { "image"  , 3 }, { "info", 1 } } },
            { "RelatedExhibitionEntrance_01", new Dictionary<string, int>(){ { "related", 1 } } },
            { "ViewingArea_01",               new Dictionary<string, int>(){ { "image"  , 2 }, { "info", 1 } } },
            { "Staircase_01",                 new Dictionary<string, int>(){ { "image"  , 2 } } },
            { "RelatedExhibitionEntrance_02", new Dictionary<string, int>(){ { "related", 1 } } },
            { "Cinema",                       new Dictionary<string, int>(){ { "video"  , 1 } } },
        };
        private static void Connector_Start(On.Jigsaw.Connector.orig_Start orig, Jigsaw.Connector self)
        {
            // this should only get triggered during initial startup of the main level, so we will get the tileset object from the connectors on the starting room
            if (!TileSetupComplete && self.possibleTilesets != null)
            {
                for (int i = 0; i < self.possibleTilesets.Length; i++)
                {
                    TileSets[self.possibleTilesets[i].name] = self.possibleTilesets[i];
                    //for (int j = 0; j < self.possibleTilesets[i].tiles.Length; j++)
                    //{}
                }

                // // load additional tilesets here
                // var testbundle = AssetUtils.LoadAssetBundleFromResources("newrooms_modernagallery", typeof(ModInit).Assembly);
                // var testgameobject = testbundle.LoadAsset<GameObject>("Entrance_01");
                // //ItemManager.Instance.AddItem(new CustomItem(testgameobject, fixReference: true));
                // testgameobject.FixReferences(true);
                // Jigsaw.Tile testtile = testgameobject.GetComponent<Jigsaw.Tile>();
                // testbundle.Unload(false);
                Dictionary<string, AssetBundle> AssetCache = new Dictionary<string, AssetBundle>();
                //Dictionary<string, Jigsaw.Tile> TileCache = new Dictionary<string, Jigsaw.Tile>();
                HashSet<TileData> NewTiles = new HashSet<TileData>();
                Dictionary<string, HashSet<Jigsaw.Tile>> TileToSetMap = new Dictionary<string, HashSet<Jigsaw.Tile>>();
                foreach (ITileset set in TilesetManagers)
                {
                    string key = set.GetType().Assembly.FullName + ":" + set.ResourceName;
                    if (!AssetCache.ContainsKey(key))
                        AssetCache[key] = AssetUtils.LoadAssetBundleFromResources(set.ResourceName, set.GetType().Assembly);

                    foreach (TileData tdata in set.Tiles)
                    {
                        if (!TileScannerCounts.ContainsKey(tdata.Name))
                            TileScannerCounts[tdata.Name] = new Dictionary<string, int>();
                        if (tdata.ScannerCounts != null)
                            foreach (var kv in tdata.ScannerCounts)
                                if (!TileScannerCounts[tdata.Name].ContainsKey(kv.Key))
                                    TileScannerCounts[tdata.Name][kv.Key] = kv.Value;

                        GameObject newRoom = AssetCache[key].LoadAsset<GameObject>(tdata.Name);
                        newRoom.FixReferences(true);
                        set.AttachMissingComponents(newRoom);
                        Jigsaw.Tile newTile = newRoom.GetComponent<Jigsaw.Tile>();
                        NewTiles.Add(tdata);
                        //TileCache[tdata.Name] = newTile;

                        foreach (Jigsaw.Connector con in newTile.connectors)
                        {
                            for (int i = 0; i < con.possibleTilesets.Length; i++)
                            {
                                if (!TileSets.ContainsKey(con.possibleTilesets[i].name))
                                    TileSets[con.possibleTilesets[i].name] = con.possibleTilesets[i];
                                if (!TileToSetMap.ContainsKey(con.possibleTilesets[i].name))
                                    TileToSetMap[con.possibleTilesets[i].name] = new HashSet<Jigsaw.Tile>();
                                TileToSetMap[con.possibleTilesets[i].name].Add(newTile);
                            }
                        }
                    }
                }

                // unload all the bundles we loaded
                foreach (var ac in AssetCache)
                    ac.Value.Unload(false);

                foreach (Jigsaw.Tileset set in TileSets.Values.ToList())
                {
                    List<Jigsaw.Tile> setTiles = new List<Jigsaw.Tile>();

                    HashSet<Jigsaw.Tile> tilesToCheck = TileToSetMap.ContainsKey(set.name) ? TileToSetMap[set.name] : new HashSet<Jigsaw.Tile>();
                    foreach (Jigsaw.Tile tile in set.tiles.ToList())
                        tilesToCheck.Add(tile);

                    // remove tiles from sets that have no valid scanners for them
                    foreach (Jigsaw.Tile tile in tilesToCheck)
                    {
                        if (TileScannerCounts.ContainsKey(tile.name))
                        {
                            bool usable = false;
                            bool hadAnyKeys = false;
                            foreach (string key in TileScannerCounts[tile.name].Keys)
                            {
                                hadAnyKeys = true;
                                switch (key)
                                {
                                    case "image": if (TileScannerCounts[tile.name][key] > 0 && ImageScanners.Count > 0) usable = true; break;
                                    case "video": if (TileScannerCounts[tile.name][key] > 0 && VideoScanners.Count > 0) usable = true; break;
                                    case "related": if (TileScannerCounts[tile.name][key] > 0 && RelatedScanners.Count > 0) usable = true; break;
                                    case "info": if (TileScannerCounts[tile.name][key] > 0 && InfoScanners.Count > 0) usable = true; break;
                                }
                                if (usable)
                                    break;
                            }
                            if(usable || !hadAnyKeys)
                                setTiles.Add(tile);
                        }
                        else
                        {
                            setTiles.Add(tile);
                        }
                    }

                    // add test tile
                    //if (set.name == "ModernGallery")
                    //{
                    //    setTiles.Add(testtile);
                    //}

                    // swap out tile list for new tile list
                    set.tiles = setTiles.ToArray();
                }
                TileSetupComplete = true;
            }
            orig(self);
        }

        // TODO This didn't work, so the entire intersect and check system needs a fix.  Issue seems to involve A-B-C where C double back over A.
        // Fix for rooms overlapping, the overlap check function was not checking for overlaps with a fixed height, so different level rooms could overlap
        // A better fix for this would be to alter the prefabs to have 8 corners instead of 4 but that would require modding the prefabs
        private static bool Tile_Overlaps_Tile(On.Jigsaw.Tile.orig_Overlaps_Tile orig, Jigsaw.Tile self, Jigsaw.Tile other)
        {
            //throw new NotImplementedException();
            for (int i = 0; i < self.boundVerticesParent.childCount - 1; i++)
            {
                Vector3 position = self.boundVerticesParent.GetChild(i).position;
				position.y = 0f;
                Vector3 position2 = self.boundVerticesParent.GetChild(i + 1).position;
				position2.y = 0f;
                for (int j = 0; j < other.boundVerticesParent.childCount - 1; j++)
                {
                    Vector3 position3 = other.boundVerticesParent.GetChild(j).position;
                    position3.y = 0f;
                    Vector3 position4 = other.boundVerticesParent.GetChild(j + 1).position;
                    position4.y = 0f;
                    if (self.Intersects(position, position2, position3, position4))
                    {
                        return true;
                    }
                }
            }
            for (int k = 0; k < other.boundVerticesParent.childCount; k++)
            {
                if (self.Contains(other.boundVerticesParent.GetChild(k).position))
                {
                    return true;
                }
            }
            for (int l = 0; l < self.boundVerticesParent.childCount; l++)
            {
                if (other.Contains(self.boundVerticesParent.GetChild(l).position))
                {
                    return true;
                }
            }
            return false;
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
