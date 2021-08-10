using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSModLoader.App
{
    public class Validator
    {
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

        public bool IsModPackage(string filepath)
        {

            bool result = false;

            if(File.Exists(filepath))
            {
                var temporaryFolder = Path.Combine(Path.GetTempPath(), "HIMEKO-" + Path.GetRandomFileName());

                try
                {
                    Directory.CreateDirectory(temporaryFolder);
                    ZipFile.ExtractToDirectory(filepath, temporaryFolder);

                    var modinfo = temporaryFolder + Path.DirectorySeparatorChar + "mod.json";

                    if (File.Exists(modinfo))
                    {
                        var contents = File.ReadAllText(modinfo);

                        var mod = JsonConvert.DeserializeObject<Mod>(contents);

                        // Directory.Delete(temporaryFolder, true);

                        result = true;
                    }

                }
                catch(Exception e)
                {
                    e.AppendToLogFile();
                }

                if(Directory.Exists(temporaryFolder))
                {
                    // Directory.Delete(temporaryFolder, true);
                }

            }

            return result;
        }

    }
}
