
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
    /// <summary>
    /// Utility methods for validating or extracting the game path.
    /// </summary>
    public class GamePath
    {
        /// <summary>
        /// Checks whether the specified path is the root folder
        /// containing the game.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <returns>True if the path is the root folder containing the game, else false.</returns>
        public static bool IsGameFolder(string path)
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
        public static string ExtractGameFolder(string path, int recurse = 0)
        {
            try
            {
                var directory = new DirectoryInfo(path);

                int limit = recurse;

                if (directory.Exists)
                {
                    while (directory != null && limit >= 0)
                    {
                        if (GamePath.IsGameFolder(directory.FullName))
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

    }
}
