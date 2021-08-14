
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HSModLoader
{
    // Not yet sure if this should just be a set of static methods or extension methods
    public class Validator
    {
        /// <summary>
        /// Checks whether the specified path is the root folder
        /// containing the game.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <returns>True if the path is the root folder containing the game, else false.</returns>
        public bool IsGameFolder(string path)
        {
            try
            {
                var attributes = File.GetAttributes(path);
                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    if( Directory.Exists(path)
                        && Directory.Exists(path + @"\RPGTacGame\Config")
                        && Directory.Exists(path + @"\RPGTacGame\Content")
                        && Directory.Exists(path + @"\RPGTacGame\Localization")
                        && Directory.Exists(path + @"\RPGTacGame\Script")
                        && File.Exists(path + @"\Binaries\Win64\RPGTacGame.exe")
                        && File.Exists(path + @"\Binaries\Win32\RPGTacGame.exe")
                        && File.Exists(path + @"\RPGTacGame\Config\RPGTacMods.ini"))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                // If the path is not a file or something that cannot be accessed, an exception is thrown
                ex.AppendToLogFile();
            }

            return false;
        }

        /// <summary>
        /// Checks if the parent folder of the specified path is the root
        /// folder containing the game. This method can recurse up the directory
        /// hierarchy.
        /// </summary>
        /// <param name="path">The path whose parent folder needs to be validated.</param>
        /// <param name="recurse">The number of times this method should recurse up the directory hierarchy.</param>
        /// <returns></returns>
        public string CheckIfParentIsGameFolder(string path, int recurse = 0)
        {
            try
            {
                var directory = new DirectoryInfo(path);

                int limit = recurse;

                if (directory.Exists)
                {
                    while (directory != null && limit >= 0)
                    {
                        if (this.IsGameFolder(directory.FullName))
                        {
                            return directory.FullName;
                        }

                        directory = directory.Parent;

                        limit--;
                    }
                }
            }
            catch (Exception e)
            {
                // If the path is not a file or something that cannot be accessed, an exception is thrown
                e.AppendToLogFile();
            }

            return null;
        }

        /// <summary>
        /// Checks if the file at the specificed path is a mod package for the game.
        /// This method unpackages the mod into a temporary folder which is immediately
        /// deleted after the validation is complete.
        /// </summary>
        /// <param name="filepath">The path to the file to check.</param>
        /// <returns>True if the file is a mod package, otherwise it is false.</returns>
        public bool IsModPackage(string filepath)
        {

            bool result = false;

            if(File.Exists(filepath))
            {
                var temporaryFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

                try
                {
                    Directory.CreateDirectory(temporaryFolder);
                    ZipFile.ExtractToDirectory(filepath, temporaryFolder);

                    var modinfo = temporaryFolder + Path.DirectorySeparatorChar + ModManager.ModInfoFile;

                    if (File.Exists(modinfo))
                    {
                        var contents = File.ReadAllText(modinfo);

                        var mod = JsonSerializer.Deserialize<Mod>(contents);

                        if(mod != null)
                        {
                            result = true;
                        }
                    }

                }
                catch(Exception e)
                {
                    e.AppendToLogFile();
                }

                if(Directory.Exists(temporaryFolder))
                {
                    Directory.Delete(temporaryFolder, true);
                }

            }

            return result;
        }

        public bool IsEnabledInMutatorLoader(GameConfiguration mutatorLoader, ModConfiguration configuration)
        {

            if(configuration.Mod.HasMutator)
            {
                if (!string.IsNullOrEmpty(configuration.Mod.MutatorStartClass))
                {
                    var mutatorsItem = mutatorLoader.Sections.SelectMany(x => x.Items).Where(y => y.Key == "MutatorsLoaded").FirstOrDefault();

                    var mutators = mutatorsItem.Value.Split(',');

                    foreach(var m in mutators)
                    {
                        if(m.Equals(configuration.Mod.MutatorStartClass))
                        {
                            return true;
                        }
                    }

                }
            }

            return false;

        }

        /// <summary>
        /// Checks if the specified mod (as represented by an
        /// insance of ModConfiguration) is installed in the game
        /// directory. This method will individually check each file 
        /// mappings of ModConfiguration.
        /// </summary>
        /// <param name="configuration">The mod to check</param>
        /// <returns>A Result object with the value true if all mapped files
        /// exist in the game directory. Otherwise the Result will have
        /// a Value of false and a list of issues identified in the
        /// Message property.</returns>
        public Result IsInstalled(ModConfiguration configuration)
        {
            throw new NotImplementedException();
        }

        public Result IsPartiallyInstalled(ModConfiguration configuration)
        {
            throw new NotImplementedException();
        }

        public Result IsUninstalled(ModConfiguration configuration)
        {
            throw new NotImplementedException();
        }

    }
}
