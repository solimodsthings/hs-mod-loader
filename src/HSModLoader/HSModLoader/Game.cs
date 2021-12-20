
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HSModLoader
{

    /// <summary>
    /// Shared constants and utility methods for interacting with
    /// the game.
    /// </summary>
    public class Game
    {
        /// <summary>
        /// This is the process name for the game. Used to avoid making
        /// changes to game files while the game itself is running.
        /// </summary>
        public static readonly string ProcessName = "RPGTacGame";

        /// <summary>
        /// Relative to game root folder. The 64-bit game executable.
        /// </summary>
        public static readonly string RelativePathToExecutable64Bit = @"Binaries\Win64\RPGTacGame.exe";

        /// <summary>
        /// Relative to game root folder. The 32-bit game executable.
        /// </summary>
        public static readonly string RelativePathToExecutable32Bit = @"Binaries\Win32\RPGTacGame.exe";

        /// <summary>
        /// Relative to game root folder. This folder contains .ini files.
        /// </summary>
        public static readonly string RelativePathToConfigurationsFolder = @"RPGTacGame\Config";

        /// <summary>
        /// Relative to game root folder. Used for Steam Workshop integration.
        /// </summary>
        public static readonly string RelativePathToSteamModsFolder = @"..\..\workshop\content\669500";

        /// <summary>
        /// Relative to location of game executable. Used for Steam Workshop integration, specifically for paths in RPGTacEngine.ini.
        /// </summary>
        public static readonly string RelativeIniPathToSteamModsFolder = @"..\..\..\..\workshop\content\669500"; // relative to the game executable and used in .ini key-value pairs

        /// <summary>
        /// Name of the configuration file where content, script, and localization paths need to be defined for mods.
        /// </summary>
        public static readonly string EngineConfigurationFile = "RPGTacEngine.ini";

        /// <summary>
        /// The section of <see cref="EngineConfigurationFile"/> that contains content, script, and localization paths.
        /// </summary>
        public static readonly string EnginePathSection = "Core.System";

        /// <summary>
        /// The key in section <see cref="EnginePathSection"/> used to define content paths.
        /// </summary>
        public static readonly string EngineContentPathKey = "Paths";

        /// <summary>
        /// The key in section <see cref="EnginePathSection"/> used to define script paths.
        /// </summary>
        public static readonly string EngineScriptPathKey = "ScriptPaths";

        /// <summary>
        /// The key in section <see cref="EnginePathSection"/> used to define localization paths.
        /// </summary>
        public static readonly string EngineLocalizationPathKey = "LocalizationPaths";

        /// <summary>
        /// Name of the configuration file where mutator classes are defined for mods.
        /// </summary>
        public static readonly string MutatorConfigurationFile = "RPGTacMods.ini";

        /// <summary>
        /// The section of <see cref="MutatorConfigurationFile"/> where mutator classes are defined.
        /// </summary>
        public static readonly string MutatorLoaderSection = "rpgtacgame.RPGTacMutatorLoader";

        /// <summary>
        /// The key in section <see cref="MutatorLoaderSection"/> where mutator classes are defined.
        /// </summary>
        public static readonly string MutatorLoaderKey = "MutatorsLoaded";

        /// <summary>
        /// Checks to see if the game is already running.
        /// </summary>
        public static bool IsRunning()
        {
            return Process.GetProcessesByName(Game.ProcessName).Length > 0;
        }
        
        /// <summary>
        /// Checks whether the specified path is the root folder
        /// containing the game.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <returns>True if the path is the root folder containing the game, else false.</returns>
        public static bool IsInsideFolder(string path)
        {
            try
            {
                var attributes = File.GetAttributes(path);
                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    if (Directory.Exists(path)
                        && File.Exists(Path.Combine(path, @"RPGTacGame\Config\RPGTacMods.ini"))
                        && File.Exists(Path.Combine(path, @"RPGTacGame\Config\RPGTacEngine.ini"))
                        && File.Exists(Path.Combine(path, @"Binaries\Win64\RPGTacGame.exe"))
                        && File.Exists(Path.Combine(path, @"Binaries\Win32\RPGTacGame.exe"))
                    )
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
        /// <returns>The path to a parent directory that is actually the game directory. If no such parent exists, the return value is null.</returns>
        public static string FindFolder(string path, int recurse = 0)
        {
            try
            {
                var directory = new DirectoryInfo(path);

                int limit = recurse;

                if (directory.Exists)
                {
                    while (directory != null && limit >= 0)
                    {
                        if (Game.IsInsideFolder(directory.FullName))
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
        /// Launches the Steam Workshop page within Steam itself.
        /// </summary>
        public static void OpenSteamWorkshop()
        {
            try
            {
                var p = new ProcessStartInfo("steam://url/SteamWorkshopPage/669500") { UseShellExecute = true, Verb = "open" };
                Process.Start(p);
            }
            catch(Exception e)
            {
                e.AppendToLogFile();
            }
        }

        public static void OpenSteamWorkshopItem(ulong id)
        {
            try
            {
                var p = new ProcessStartInfo(string.Format("steam://url/CommunityFilePage/{0}", id)) { UseShellExecute = true, Verb = "open" };
                Process.Start(p);
            }
            catch(Exception e)
            {
                e.AppendToLogFile();
            }
            
        }

        public static void StartGame()
        {
            try
            {
                var p = new ProcessStartInfo(string.Format("steam://run/669500")) { UseShellExecute = true, Verb = "open" };
                Process.Start(p);
            }
            catch (Exception e)
            {
                e.AppendToLogFile();
            }
        }

    }
}
