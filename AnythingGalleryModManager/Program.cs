using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AnythingGalleryModManager
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Contains("-generatehooks"))
            {
                string GamePath = Form1.GetGamePath();
                if (!string.IsNullOrWhiteSpace(GamePath))
                {
                    string OutFile = Path.Combine("manager-hook", $"MMHOOK_Assembly-CSharp.dll");
                    if (File.Exists(OutFile))
                        File.Delete(OutFile);
                    MMHookGenerator.GenerateMMHook(Path.Combine(GamePath, @"The Anything Gallery_Data\Managed\Assembly-CSharp.dll"), OutFile, GamePath);
                }
            }
            else
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
        }
    }
}
