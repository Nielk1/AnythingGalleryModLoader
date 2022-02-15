using Mono.Cecil;
using MonoMod;
using MonoMod.RuntimeDetour.HookGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AnythingGalleryModManager
{
    public static class MMHookGenerator
    {
        public static bool GenerateMMHook(string input, string output, string GamePath)
        {
            //Log.LogMessage(MessageImportance.High, $"Generating MMHOOK of {input}.");

            MonoModder modder = new MonoModder();
            modder.InputPath = input;
            modder.OutputPath = output;
            modder.ReadingMode = ReadingMode.Deferred;

            ((BaseAssemblyResolver)modder.AssemblyResolver)?.AddSearchDirectory(Path.Combine(Environment.CurrentDirectory, "bin", "Debug"));
            ((BaseAssemblyResolver)modder.AssemblyResolver)?.AddSearchDirectory(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase.Replace("file:///home", "/home").Replace("file:///", "")));

            if (Directory.Exists(Path.Combine(GamePath, "The Anything Gallery_Data", "Managed")))
            {
                ((BaseAssemblyResolver)modder.AssemblyResolver)?.AddSearchDirectory(Path.Combine(GamePath, "The Anything Gallery_Data", "Managed"));
            }

            /*if (Directory.Exists(Path.Combine(GamePath, "mod_deps")))
            {
                ((BaseAssemblyResolver)modder.AssemblyResolver)?.AddSearchDirectory(Path.Combine(GamePath, "mod_deps"));
            }*/

            if (Directory.Exists("manager-hook"))
            {
                ((BaseAssemblyResolver)modder.AssemblyResolver)?.AddSearchDirectory("manager-hook");
            }


            modder.Read();

            modder.MapDependencies();

            if (File.Exists(output))
            {
                //Log.LogMessage(MessageImportance.High, $"Clearing {output}");
                File.Delete(output);
            }

            HookGenerator hookGenerator = new HookGenerator(modder, Path.GetFileName(output));

            hookGenerator.HookPrivate = true;

            using (ModuleDefinition mOut = hookGenerator.OutputModule)
            {
                hookGenerator.Generate();
                //mOut.Types.Add(new TypeDefinition("BepHookGen", "hash" + md5, TypeAttributes.AutoClass));
                mOut.Write(output);
            }

            //Log.LogMessage(MessageImportance.High, $"Finished writing {output}");

            return true;
        }
    }
}
