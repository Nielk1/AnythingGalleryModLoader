using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Compression;

namespace AnythingGalleryModManager
{
    public partial class Form1 : Form
    {
        static readonly string[] SkipLibraries = new string[]
        {
            "Assembly-CSharp.dll",
            "Ilan.Google.API.dll",
            "Mono.Security.dll",
            "mscorlib.dll",
            "netstandard.dll",
            "Newtonsoft.Json.dll",
            "System.ComponentModel.Composition.dll",
            "System.Configuration.dll",
            "System.Core.dll",
            "System.Data.dll",
            "System.Diagnostics.StackTrace.dll",
            "System.dll",
            "System.Drawing.dll",
            "System.EnterpriseServices.dll",
            "System.Globalization.Extensions.dll",
            "System.IO.Compression.dll",
            "System.IO.Compression.FileSystem.dll",
            "System.Net.Http.dll",
            "System.Numerics.dll",
            "System.Runtime.Serialization.dll",
            "System.Runtime.Serialization.Xml.dll",
            "System.ServiceModel.Internals.dll",
            "System.Transactions.dll",
            "System.Xml.dll",
            "System.Xml.Linq.dll",
            "System.Xml.XPath.XDocument.dll",
            "Unity.InputSystem.dll",
            "Unity.Postprocessing.Runtime.dll",
            "Unity.TextMeshPro.dll",
            "Unity.Timeline.dll",
            "UnityEngine.AccessibilityModule.dll",
            "UnityEngine.AIModule.dll",
            "UnityEngine.AndroidJNIModule.dll",
            "UnityEngine.AnimationModule.dll",
            "UnityEngine.ARModule.dll",
            "UnityEngine.AssetBundleModule.dll",
            "UnityEngine.AudioModule.dll",
            "UnityEngine.ClothModule.dll",
            "UnityEngine.ClusterInputModule.dll",
            "UnityEngine.ClusterRendererModule.dll",
            "UnityEngine.CoreModule.dll",
            "UnityEngine.CrashReportingModule.dll",
            "UnityEngine.DirectorModule.dll",
            "UnityEngine.dll",
            "UnityEngine.DSPGraphModule.dll",
            "UnityEngine.GameCenterModule.dll",
            "UnityEngine.GridModule.dll",
            "UnityEngine.HotReloadModule.dll",
            "UnityEngine.ImageConversionModule.dll",
            "UnityEngine.IMGUIModule.dll",
            "UnityEngine.InputLegacyModule.dll",
            "UnityEngine.InputModule.dll",
            "UnityEngine.JSONSerializeModule.dll",
            "UnityEngine.LocalizationModule.dll",
            "UnityEngine.ParticleSystemModule.dll",
            "UnityEngine.PerformanceReportingModule.dll",
            "UnityEngine.Physics2DModule.dll",
            "UnityEngine.PhysicsModule.dll",
            "UnityEngine.ProfilerModule.dll",
            "UnityEngine.ScreenCaptureModule.dll",
            "UnityEngine.SharedInternalsModule.dll",
            "UnityEngine.SpriteMaskModule.dll",
            "UnityEngine.SpriteShapeModule.dll",
            "UnityEngine.StreamingModule.dll",
            "UnityEngine.SubstanceModule.dll",
            "UnityEngine.SubsystemsModule.dll",
            "UnityEngine.TerrainModule.dll",
            "UnityEngine.TerrainPhysicsModule.dll",
            "UnityEngine.TextCoreModule.dll",
            "UnityEngine.TextRenderingModule.dll",
            "UnityEngine.TilemapModule.dll",
            "UnityEngine.TLSModule.dll",
            "UnityEngine.UI.dll",
            "UnityEngine.UIElementsModule.dll",
            "UnityEngine.UIElementsNativeModule.dll",
            "UnityEngine.UIModule.dll",
            "UnityEngine.UmbraModule.dll",
            "UnityEngine.UNETModule.dll",
            "UnityEngine.UnityAnalyticsModule.dll",
            "UnityEngine.UnityConnectModule.dll",
            "UnityEngine.UnityTestProtocolModule.dll",
            "UnityEngine.UnityWebRequestAssetBundleModule.dll",
            "UnityEngine.UnityWebRequestAudioModule.dll",
            "UnityEngine.UnityWebRequestModule.dll",
            "UnityEngine.UnityWebRequestTextureModule.dll",
            "UnityEngine.UnityWebRequestWWWModule.dll",
            "UnityEngine.VehiclesModule.dll",
            "UnityEngine.VFXModule.dll",
            "UnityEngine.VideoModule.dll",
            "UnityEngine.VirtualTexturingModule.dll",
            "UnityEngine.VRModule.dll",
            "UnityEngine.WindModule.dll",
            "UnityEngine.XRModule.dll",
        };

        bool started = false;
        string GamePath = null;
        WebClient client;
        public Form1()
        {
            InitializeComponent();
            if (File.Exists("The Anything Gallery.exe"))
            {
                GamePath = Path.Combine(Directory.GetCurrentDirectory());
            }
            else if (File.Exists(Path.Combine("..", "The Anything Gallery.exe")))
            {
                GamePath = Path.Combine(Directory.GetCurrentDirectory(), "..");
            }
            else if (File.Exists(Path.Combine("..", "the-anything-gallery", "The Anything Gallery.exe")))
            {
                GamePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "the-anything-gallery");
            }
            client = new WebClient();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            if (GamePath == null)
            {
                lblStatus.Text = "\"The Anything Gallery.exe\" not found.\r\nPlease place this manager with \"The Anything Gallery.exe\", in a subfolder, or install in the same itch.io library install location";
                return;
            }
            lblStatus.Text = "Downloading Injector and Mod dependencies.";
            await DownloadModDependenciesAsync();
            lblStatus.Text = "Generating MMHook DLL.";
            MMHookGenerator.GenerateMMHook(Path.Combine(GamePath, @"The Anything Gallery_Data\Managed\Assembly-CSharp.dll"), "manager-hook", GamePath);
            lblStatus.Text = "Loading Mods";
            if (!Directory.Exists("manager-mods"))
                Directory.CreateDirectory("manager-mods");
            foreach (string dll in Directory.GetFiles("manager-mods", "*.dll", SearchOption.AllDirectories))
            {
                try
                {
                    clbMods.Items.Add(Path.GetFileNameWithoutExtension(dll), File.Exists(Path.Combine(GamePath, "mods", Path.GetFileName(dll))));
                }
                catch (FileLoadException loadEx)
                { } // The Assembly has already been loaded.
                catch (BadImageFormatException imgEx)
                { } // If a BadImageFormatException exception is thrown, the file is not an assembly.

            } // foreach dll
            started = true;
            lblStatus.Text = "Checking for Mod Loader install.";
            if (File.Exists(Path.Combine(GamePath, "winhttp.dll")))
            {
                btnInstallLoader.Enabled = false;
                btnUninstallLoader.Enabled = true;
                clbMods.Enabled = true;
            }
            else
            {
                btnInstallLoader.Enabled = true;
                btnUninstallLoader.Enabled = false;
                clbMods.Enabled = false;
            }
            lblStatus.Text = "Ready";
        }

        private async Task DownloadModDependenciesAsync()
        {
            if (!Directory.Exists("manager-hook"))
                Directory.CreateDirectory("manager-hook");
            foreach (string depFile in Directory.EnumerateFiles("manager-hook", "*.deps.json", SearchOption.TopDirectoryOnly))
                await DownloadDependenciesAsync(depFile);
            if (!Directory.Exists("manager-mods"))
                Directory.CreateDirectory("manager-mods");
            foreach (string depFile in Directory.EnumerateFiles("manager-mods", "*.deps.json", SearchOption.TopDirectoryOnly))
                await DownloadDependenciesAsync(depFile);
        }

        private async Task DownloadDependenciesAsync(string depFile)
        {
            string depContents = File.ReadAllText(depFile);
            try
            {
                JObject obj = JObject.Parse(depContents);
                foreach (JProperty prop in obj["libraries"])
                {
                    try
                    {
                        if (prop.Value["type"].Value<string>() == "package")
                        {
                            //prop.Value["sha512"]
                            string hashPath = prop.Value["hashPath"].Value<string>();
                            string path = prop.Value["path"].Value<string>();
                            string SavePath = Path.Combine("manager-deps", prop.Name);
                            if (!SkipLibraries.Contains(SavePath.Split('/').First() + ".dll"))
                            {
                                if (!Directory.Exists(SavePath))
                                    Directory.CreateDirectory(SavePath);
                                string FilePath = Path.Combine(SavePath, Path.GetFileNameWithoutExtension(hashPath));
                                if (!File.Exists(FilePath))
                                    await client.DownloadFileTaskAsync(new Uri($@"https://www.nuget.org/api/v2/package/{path}"), FilePath);
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void InstallDependencies(string depFile)
        {
            string depContents = File.ReadAllText(depFile);
            try
            {
                JObject obj = JObject.Parse(depContents);
                foreach (JProperty prop in obj["libraries"])
                {
                    try
                    {
                        if (prop.Value["type"].Value<string>() == "package")
                        {
                            //prop.Value["sha512"]
                            string hashPath = prop.Value["hashPath"].Value<string>();
                            string path = prop.Value["path"].Value<string>();
                            string SourcePath = Path.Combine("manager-deps", prop.Name);
                            if (!SkipLibraries.Contains(SourcePath.Split('/').First() + ".dll"))
                            {
                                if (!Directory.Exists(SourcePath))
                                    continue;
                                string SourceFilePath = Path.Combine(SourcePath, Path.GetFileNameWithoutExtension(hashPath));
                                if (File.Exists(SourceFilePath))
                                {
                                    //await client.DownloadFileTaskAsync(new Uri($@"https://www.nuget.org/api/v2/package/{path}"), SourceFilePath);
                                    ZipArchive archive = ZipFile.OpenRead(SourceFilePath);
                                    foreach(var entry in archive.Entries)
                                    {
                                        if (entry.FullName.StartsWith("lib/netstandard2.0/"))
                                        {
                                            string DestinationFile = Path.Combine(GamePath, "mod_deps", entry.FullName.Replace("lib/netstandard2.0/", String.Empty));
                                            if(!File.Exists(DestinationFile))
                                            {
                                                if (!Directory.Exists(Path.GetDirectoryName(DestinationFile)))
                                                    Directory.CreateDirectory(Path.GetDirectoryName(DestinationFile));
                                                entry.ExtractToFile(DestinationFile);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void btnInstallLoader_Click(object sender, EventArgs e)
        {
            File.Copy(Path.Combine("manager-hook", "winhttp.dll"), Path.Combine(GamePath, "winhttp.dll"), true);
            File.Copy(Path.Combine("manager-hook", "doorstop_config.ini"), Path.Combine(GamePath, "doorstop_config.ini"), true);

            foreach (string depFile in Directory.EnumerateFiles("manager-hook", "*.deps.json", SearchOption.TopDirectoryOnly))
                InstallDependencies(depFile);
            
            File.Copy(Path.Combine("manager-hook", "MMHOOK_Assembly-CSharp.dll"), Path.Combine(GamePath, "mod_deps", "MMHOOK_Assembly-CSharp.dll"), true);
            
            foreach (string depFile in Directory.EnumerateFiles("manager-hook", "AnythingGalleryLoader.*", SearchOption.TopDirectoryOnly))
                File.Copy(depFile, Path.Combine(GamePath, "mod_deps", Path.GetFileName(depFile)), true);

            btnInstallLoader.Enabled = false;
            btnUninstallLoader.Enabled = true;
            clbMods.Enabled = true;
        }

        private void btnUninstallLoader_Click(object sender, EventArgs e)
        {
            File.Delete(Path.Combine(GamePath, "winhttp.dll"));
            File.Delete(Path.Combine(GamePath, "doorstop_config.ini"));

            Directory.Delete(Path.Combine(GamePath, "mod_deps"), true);
            Directory.Delete(Path.Combine(GamePath, "mods"), true);

            btnInstallLoader.Enabled = true;
            btnUninstallLoader.Enabled = false;
            clbMods.Enabled = false;
            for (int i = 0; i < clbMods.Items.Count; i++)
                clbMods.SetItemChecked(i, false);
        }

        private void clbMods_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (!started)
                return;
            if (e.NewValue == e.CurrentValue)
                return;
            if(e.NewValue == CheckState.Checked)
            {
                string prefix = clbMods.GetItemText(clbMods.Items[e.Index]);
                foreach(string file in Directory.EnumerateFiles("manager-mods", prefix + ".*", SearchOption.TopDirectoryOnly))
                {
                    if (Directory.Exists(GamePath))
                    {
                        if (!Directory.Exists(Path.Combine(GamePath, "mods")))
                            Directory.CreateDirectory(Path.Combine(GamePath, "mods"));
                        File.Copy(file, Path.Combine(GamePath, "mods", Path.GetFileName(file)), true);
                    }
                }
                if (File.Exists(Path.Combine("manager-mods", prefix + ".deps.json")))
                    InstallDependencies(Path.Combine("manager-mods", prefix + ".deps.json"));
            }
            if(e.NewValue == CheckState.Unchecked)
            {
                string prefix = clbMods.GetItemText(clbMods.Items[e.Index]);
                foreach (string file in Directory.EnumerateFiles(Path.Combine(GamePath, "mods"), prefix + ".*", SearchOption.TopDirectoryOnly))
                {
                    File.Delete(file);
                }
            }
        }
    }
}
