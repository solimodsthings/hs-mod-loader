using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace HSModLoader.Console
{
    public class Program
    {
        private const string ModInfoFile = "mod.json";
        private const string ModPackageExtension = ".hsmod";
        private const string ModWorkingFolder = "mod";

        public static void Main(string[] args)
        {

            if (!Directory.Exists(ModWorkingFolder))
            {
                Directory.CreateDirectory(ModWorkingFolder);
            }

            var options = new JsonSerializerOptions() { WriteIndented = true };

            var mods = new Mod[]
            {
                InstantiateModWithNoFiles(),
                InstantiateBrokenLocalizationMod()
            };

            foreach (var mod in mods)
            {
                var output = JsonSerializer.Serialize(mod, options);
                File.WriteAllText(Path.Combine(ModWorkingFolder, ModInfoFile), output);

                foreach(var file in mod.ModFiles)
                {
                    File.WriteAllText(Path.Combine(ModWorkingFolder, file.Name), "debug file");
                }

                var package = mod.Name.Replace(" ", string.Empty).Trim() + ModPackageExtension;

                if (File.Exists(package))
                {
                    File.Delete(package);
                }

                ZipFile.CreateFromDirectory(ModWorkingFolder, package);

                System.Console.WriteLine(string.Format("Created test mod '{0}'", package));

            }


        }

        private static Mod InstantiateModWithNoFiles()
        {
            var mod = new Mod()
            {
                Name = "Test Mod",
                Version = "0.9.1",
                Author = "Test Author",
                HasCampaign = false,
                HasMutator = true,
                MutatorStartClass = "Nonexistent.MutatorClass"
            };

            mod.AddScript("test.u");
            mod.AddLocalization("some-localization-file.INT");

            return mod;
        }

        private static Mod InstantiateBrokenLocalizationMod()
        {
            var mod = new Mod()
            {
                Name = "Broken Localization Mod",
                Version = "0.9.1",
                Author = "Test Author",
                HasCampaign = false,
                HasMutator = true,
                MutatorStartClass = "Nonexistent.MutatorClass"
            };

            mod.AddScript("test.u");
            mod.AddLocalization("some-localization-file.INT");
            mod.AddLocalization("non-existent-language.XYZ");

            return mod;
        }

    }
}
