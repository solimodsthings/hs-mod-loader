
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HSModLoader
{

    public class ModManager
    {
        private static readonly string ConfigurationFile = "config.json";
        private static readonly string LocalPackageFolder = "mods";

        private static readonly string ActiveGameModsFolder = @"RPGTacGame\Mods";
        private static readonly string GameConfigurationsFolder = @"RPGTacGame\Config";
        private static readonly string GameEngineConfigurationFile = "RPGTacEngine.ini";
        private static readonly string PathsSection = "Core.System";
        private static readonly string ModContentPathKey = "Paths";
        private static readonly string ModScriptPathKey = "ScriptPaths";
        private static readonly string ModLocalizationPathKey = "LocalizationPaths";
        private static readonly string ModContentPathValue = @"..\..\RPGTacGame\Mods";
        private static readonly string ModScriptPathValue = @"..\..\RPGTacGame\Mods";
        private static readonly string ModLocalizationPathValue = @"..\..\RPGTacGame\Mods";

        private static readonly string GameExecutable64Bit = @"Binaries\Win64\RPGTacGame.exe";
        private static readonly string GameExecutable32Bit = @"Binaries\Win32\RPGTacGame.exe";
        private static readonly string MutatorLoaderSection = "rpgtacgame.RPGTacMutatorLoader";
        private static readonly string MutatorLoaderItem = "MutatorsLoaded";
        private static readonly string ChangeLogFile = "filechanges.log";

        public static readonly string ModInfoFile = "mod.json";

        public List<ModConfiguration> ModConfigurations { get; set; }

        public string GameFolderPath { get; set; }

        private StringBuilder FileChangeLog;

        public ModManager()
        {
            ModConfigurations = new List<ModConfiguration>();
            FileChangeLog = new StringBuilder();
        }

        /// <summary>
        /// Creates a subdirectory in the game directory that will contain all mod files.
        /// Also updates RPGTacEngine.ini to have content, script, and localization paths defined
        /// for the mod subdirectory.
        /// </summary>
        public void InitializeGameModsFolder()
        {

            if(string.IsNullOrEmpty(this.GameFolderPath) || !Directory.Exists(this.GameFolderPath))
            {
                throw new InvalidOperationException("Cannot initialize game mods folder because the path to the game folder has not been defined.");
            }

            var active = Path.Combine(this.GameFolderPath, ActiveGameModsFolder);

            if (!Directory.Exists(active))
            {
                Directory.CreateDirectory(active);
            }

            var gameEngineConfig = new GameConfiguration(Path.Combine(this.GameFolderPath, GameConfigurationsFolder, GameEngineConfigurationFile));
            gameEngineConfig.Load();

            var section = gameEngineConfig.Sections.FirstOrDefault(x => x.Name == PathsSection);

            if (section != null)
            {
                if (section.Items.FirstOrDefault(x => x.Key == ModContentPathKey && x.Value == ModContentPathValue) == null)
                {
                    section.Items.Add(new GameConfigurationItem() { Key = ModContentPathKey, Value = ModContentPathValue });
                }

                if (section.Items.FirstOrDefault(x => x.Key == ModScriptPathKey && x.Value == ModScriptPathValue) == null)
                {
                    section.Items.Add(new GameConfigurationItem() { Key = ModScriptPathKey, Value = ModScriptPathValue });
                }

                if (section.Items.FirstOrDefault(x => x.Key == ModLocalizationPathKey && x.Value == ModLocalizationPathValue) == null)
                {
                    section.Items.Add(new GameConfigurationItem() { Key = ModLocalizationPathKey, Value = ModLocalizationPathValue });
                }

                gameEngineConfig.Save();
            }
            else
            {
                throw new InvalidOperationException(string.Format("RPGTacEngine.ini does not have a section called '{0}'.", PathsSection));
            }

        }

        /// <summary>
        /// Deserializes this ModManager from disk.
        /// </summary>
        public void Load()
        {
            if (File.Exists(ConfigurationFile))
            {
                var json = File.ReadAllText(ConfigurationFile);

                try
                {
                    var m = JsonSerializer.Deserialize<ModManager>(json);

                    this.ModConfigurations = m.ModConfigurations;
                    this.GameFolderPath = m.GameFolderPath;

                    foreach(var configuration in this.ModConfigurations)
                    {
                        var modinfo = configuration.LocalInfoFile;

                        if (File.Exists(modinfo))
                        {
                            var contents = File.ReadAllText(modinfo);
                            configuration.Mod = JsonSerializer.Deserialize<Mod>(contents);
                        }
                        else
                        {
                            throw new FileNotFoundException(string.Format("A registered mod is missing its mod.json file. Expected the file to exist at {0}.", modinfo));
                        }
                    }

                }
                catch(Exception e)
                {
                    e.AppendToLogFile();
                }
                
            }
        }

        /// <summary>
        /// Serializes this ModManager and current ModConfigurations to disk.
        /// </summary>
        public void Save()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(ConfigurationFile, json);
        }

        /// <summary>
        /// Registers the mod package file at the specified file path
        /// with this mod manager.
        /// </summary>
        public Result RegisterMod(string filepath)
        {
            var result = new Result() { IsSuccessful = false };

            if (File.Exists(filepath))
            {
                var temporaryDestination = Path.Combine(LocalPackageFolder, Path.GetRandomFileName());
                var configuration = new ModConfiguration();

                try
                {
                    Directory.CreateDirectory(temporaryDestination);
                    ZipFile.ExtractToDirectory(filepath, temporaryDestination);

                    var modinfo = Path.Combine(temporaryDestination, ModInfoFile);

                    if (File.Exists(modinfo))
                    {
                        var contents = File.ReadAllText(modinfo);
                        var mod = JsonSerializer.Deserialize<Mod>(contents);

                        if (!this.IsRegistered(mod))
                        {
                            var localPackageFile = Path.Combine(LocalPackageFolder, mod.Id + ".hsmod");
                            var localInfoFile = Path.Combine(LocalPackageFolder, mod.Id + ".json");

                            File.Copy(filepath, localPackageFile);
                            File.Copy(modinfo, localInfoFile);

                            configuration.Mod = mod;
                            configuration.LocalPackageFile = localPackageFile;
                            configuration.LocalInfoFile = localInfoFile;
                            configuration.DestinationGamePath = Path.Combine(this.GameFolderPath, ActiveGameModsFolder, configuration.Mod.Id);
                            configuration.State = ModState.Disabled;

                            // order matters for next two statements
                            this.ModConfigurations.Add(configuration);
                            configuration.OrderIndex = this.ModConfigurations.Count - 1;

                            result.IsSuccessful = true;

                        }
                        else
                        {
                            result.ErrorMessage = string.Format("Could not load mod. '{0}' version {1} already exists!", mod.Name, mod.Version);
                        }

                    }
                    else
                    {
                        result.ErrorMessage = "Could not load mod. Could not find and extract mod.json within the mod package file.";
                    }

                }
                catch (Exception e)
                {
                    e.AppendToLogFile();
                    result.ErrorMessage = "Could not load mod. See error.log file.";

                    if (this.ModConfigurations.Contains(configuration))
                    {
                        this.ModConfigurations.Remove(configuration);
                    }

                    if (File.Exists(configuration.LocalInfoFile))
                    {
                        File.Delete(configuration.LocalInfoFile);
                    }

                    if (File.Exists(configuration.LocalPackageFile))
                    {
                        File.Delete(configuration.LocalPackageFile);
                    }

                }

                Directory.Delete(temporaryDestination, true);

            }
            else
            {
                result.ErrorMessage = "Mod package file could not be loaded because it cannot be accessed or does not exist.";
            }

            return result;

        }

        private bool IsRegistered(Mod newMod)
        {
            foreach (var mod in ModConfigurations.Select(x => x.Mod))
            {
                if (mod.Id.ToLower().Equals(newMod.Id.ToLower()) || (mod.Name.ToLower().Equals(newMod.Name.ToLower()) && mod.Version.ToLower().Equals(newMod.Version.ToLower())))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Unregisters the specified mod configuration from this mod manager.
        /// This method will automatically uninstall the mod if it is currently
        /// in an enabled state.
        /// </summary>
        public Result UnregisterMod(ModConfiguration configuration)
        {
            var result = new Result();
            try
            {
                this.UninstallMod(configuration);

                if(File.Exists(configuration.LocalPackageFile))
                {
                    File.Delete(configuration.LocalPackageFile);
                }

                if (File.Exists(configuration.LocalInfoFile))
                {
                    File.Delete(configuration.LocalInfoFile);
                }

                this.ModConfigurations.Remove(configuration);
                this.UpdateModOrderValue();
                result.IsSuccessful = true;
            }
            catch (Exception e)
            {
                e.AppendToLogFile();
                result.ErrorMessage = "Mod was not uninstalled correctly. See error.log.";
            }


            return result;
        }

        /// <summary>
        /// Updates the game folder by adding or removing mod files based on the respective mod's
        /// state (ie. enabled, soft disabled, or disabled). The method does not modify existing base
        /// game files except for RPGTacMods.ini which is necessary for any mod that has a mutator class.
        /// </summary>
        public Result ApplyMods()
        {

            var result = new Result();
            this.StartChangeLog();
            this.AppendChangeLog("Beginning file changes...");

            try
            {
                
                this.ApplyModsToMutatorIniFile();
                var apply = this.ApplyModsToGameFolder();

                if(!apply.IsSuccessful)
                {
                    result.ErrorMessage += apply.ErrorMessage;
                    this.AppendChangeLog(result.ErrorMessage);
                }
                else
                {
                    result.IsSuccessful = true;
                }
                
            }
            catch(Exception e)
            {
                e.AppendToLogFile();
                result.ErrorMessage = "Encountered an error while trying to update mod/game files.";
                this.AppendChangeLog(result.ErrorMessage);
            }

            this.AppendChangeLog("File changes finished.");
            this.EndChangeLog();
            return result;
        }

        private void ApplyModsToMutatorIniFile()
        {
            var mutatorsConfig = new GameConfiguration(Path.Combine(this.GameFolderPath, GameConfigurationsFolder, "RPGTacMods.ini"));
            mutatorsConfig.Load();

            // This list of all available mutator classes will be used to preserve unmanaged
            // mods the player may have added manually
            var allAvailableMutators = this.ModConfigurations
                .Select(x => x.Mod)
                .Where(x => x.HasMutator && !string.IsNullOrEmpty(x.MutatorClass))
                .Select(x => x.MutatorClass);

            // First we find out what exactly is enabled now
            var enabledMutatorClasses = new List<string>();

            foreach (var config in this.ModConfigurations)
            {
                if (config.State == ModState.Enabled) // SoftDisabled mods will not have their mutator class enabled, but their files will still be present
                {
                    if (config.Mod.HasMutator && !string.IsNullOrEmpty(config.Mod.MutatorClass))
                    {
                        enabledMutatorClasses.Add(config.Mod.MutatorClass);
                        this.AppendChangeLog(string.Format("Mod {0} is enabled and has a mutator, and needs to be added to the mutator loader list...", config.Mod.Id));
                    }
                }
            }

            // Begin manipulating the .ini file. This is built to work even if new config sections or
            // items are added to the .ini file.
            var newMutatorList = string.Join(",", enabledMutatorClasses);

            var configSection = mutatorsConfig.Sections.Where(x => x.Name == MutatorLoaderSection).FirstOrDefault();

            if (configSection == null)
            {
                mutatorsConfig.Sections.Add(new GameConfigurationSection() { Name = MutatorLoaderSection });
            }

            var configItem = configSection.Items.Where(x => x.Key == MutatorLoaderItem).FirstOrDefault();

            if (configItem == null)
            {
                configSection.Items.Add(new GameConfigurationItem() { Key = MutatorLoaderItem, Value = newMutatorList });
            }
            else
            {

                // Attempt to preserve any mutator classes that were manually added
                // by player to .ini file if they are not already managed by this mod loader
                var unmanagedMutatorClasses = new List<string>();

                if (allAvailableMutators != null)
                {
                    var existingMutatorList = configItem.Value;
                    var existingMutatorClasses = existingMutatorList.Split(',').ToList();

                    foreach (var existingClass in existingMutatorClasses)
                    {
                        if (!string.IsNullOrEmpty(existingClass) && !allAvailableMutators.Contains(existingClass))
                        {
                            unmanagedMutatorClasses.Add(existingClass);
                            this.AppendChangeLog(string.Format("Encountered unmanaged mutator '{0}' in mutator loader list. Attempting to preserve the entry...", existingClass));
                        }
                    }
                }

                // Finally, update the mutator list
                if (unmanagedMutatorClasses.Count > 0)
                {
                    if (enabledMutatorClasses.Count > 0)
                    {
                        newMutatorList += ",";
                    }

                    newMutatorList += string.Join(",", unmanagedMutatorClasses);
                }

                configItem.Value = newMutatorList;
                this.AppendChangeLog(string.Format("New mutator list will be: {0}", configItem.Value));
            }


            mutatorsConfig.Save();
            this.AppendChangeLog(string.Format("Mutator list changes saved to file: {0}", mutatorsConfig.FileName));
        }

        private Result ApplyModsToGameFolder()
        {
            var result = new Result() { IsSuccessful = true };

            foreach (var configuration in this.ModConfigurations)
            {
                if (configuration.State == ModState.Enabled || configuration.State == ModState.SoftDisabled)
                {

                    this.AppendChangeLog(string.Format("Mod {0} needs to be added to the game directory...", configuration.Mod.Id));

                    var install = this.InstallMod(configuration);

                    if(!install.IsSuccessful)
                    {
                        result.IsSuccessful = false;
                        result.ErrorMessage += install.ErrorMessage;
                        this.UninstallMod(configuration); // in case some files managed to be added
                        configuration.State = ModState.Disabled;
                    }
                }
                else if (configuration.State == ModState.Disabled)
                {
                    this.AppendChangeLog(string.Format("Mod {0} needs to be removed from game directory...", configuration.Mod.Id));

                    var uninstall = this.UninstallMod(configuration);
                    if(!uninstall.IsSuccessful)
                    {
                        result.IsSuccessful = false;
                        result.ErrorMessage += uninstall.ErrorMessage;
                    }
                }
            }

            return result;
        }

        private Result InstallMod(ModConfiguration configuration)
        {
            var result = new Result();

            try
            {
                this.AppendChangeLog(string.Format("Adding files for mod {0} to folder {1}", configuration.Mod.Id, configuration.DestinationGamePath));

                if (!Directory.Exists(configuration.DestinationGamePath))
                {
                    Directory.CreateDirectory(configuration.DestinationGamePath);
                }

                if (Directory.GetFiles(configuration.DestinationGamePath).Length > 0)
                {
                    this.AppendChangeLog(string.Format("Folder {0} already contains files. Skipping addition of more files", configuration.DestinationGamePath));
                }
                else
                {
                    ZipFile.ExtractToDirectory(configuration.LocalPackageFile, configuration.DestinationGamePath);
                    this.AppendChangeLog(string.Format("Successfully unpackaged mod into folder {0}", configuration.DestinationGamePath));
                }

                result.IsSuccessful = true;
            }
            catch(Exception e)
            {
                e.AppendToLogFile();
                result.ErrorMessage = string.Format("Installation failed for mod {0}. Could not unpackage the mod into folder {1}", configuration.Mod.Id, configuration.DestinationGamePath);
            }

            return result;

        }

  
        private Result UninstallMod(ModConfiguration configuration)
        {
            var result = new Result();

            try
            {
                this.AppendChangeLog(string.Format("Removing folder {0} for mod {1}...", configuration.DestinationGamePath, configuration.Mod.Id));
                if (Directory.Exists(configuration.DestinationGamePath))
                {
                    Directory.Delete(configuration.DestinationGamePath, true);
                    this.AppendChangeLog(string.Format("Succesfully removed folder {0} for mod {1}", configuration.DestinationGamePath, configuration.Mod.Id));
                }
                else
                {
                    this.AppendChangeLog(string.Format("Folder {0} does not exist and does not need to be deleted ", configuration.DestinationGamePath));
                }

                result.IsSuccessful = true;

            }
            catch(Exception e)
            {
                e.AppendToLogFile();
                result.ErrorMessage = string.Format("Could not disable mod {0}. Could not delete mod folder at {1}", configuration.Mod.Id, configuration.DestinationGamePath);
            }

            return result;

        }

        /// <summary>
        /// Shifts a mod up the order list by one.
        /// </summary>
        /// <param name="index">Index of the mod to be shifted up the order list.</param>
        public void ShiftModOrderUp(int index)
        {
            if (index > 0 && index < this.ModConfigurations.Count)
            {
                var configuration = this.ModConfigurations[index];
                this.ModConfigurations.RemoveAt(index);
                this.ModConfigurations.Insert(index - 1, configuration);
                this.UpdateModOrderValue();
            }
        }

        /// <summary>
        /// Shifts a mod down the order list by one.
        /// </summary>
        /// <param name="index">Index of the mod to be shifted down the order list.</param>
        public void ShiftModOrderDown(int index)
        {
            if (index >= 0 && index + 1 < this.ModConfigurations.Count)
            {
                var configuration = this.ModConfigurations[index];
                this.ModConfigurations.RemoveAt(index);
                this.ModConfigurations.Insert(index + 1, configuration);
                this.UpdateModOrderValue();
            }
        }


        private void UpdateModOrderValue()
        {
            int order = 0;
            foreach (var configuration in this.ModConfigurations)
            {
                configuration.OrderIndex = order++;
            }
        }

        /// <summary>
        /// Clears the file change log. Subsequent calls to AppendChangeLog() wil be 
        /// written to disk once EndChangeLog() is called.
        /// </summary>
        private void StartChangeLog()
        {
            this.FileChangeLog.Clear();
        }

        /// <summary>
        /// Used to record file changes in the game folder.
        /// </summary>
        private void AppendChangeLog(string message)
        {
            this.FileChangeLog.AppendLine(string.Format("[{0}] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), message));
        }

        /// <summary>
        /// Writes recent changes to filechange.log.
        /// </summary>
        private void EndChangeLog()
        {
            File.WriteAllText(ChangeLogFile, this.FileChangeLog.ToString());
        }

        /// <summary>
        /// Returns the path to the game's executable.
        /// </summary>
        public string GetPathToGameExecutable(bool Use64Bit = true)
        {
            if (!string.IsNullOrEmpty(this.GameFolderPath))
            {
                if (Use64Bit)
                {
                    return Path.Combine(this.GameFolderPath, GameExecutable64Bit);
                }
                else
                {
                    return Path.Combine(this.GameFolderPath, GameExecutable32Bit);
                }
            }
            else
            {
                throw new InvalidOperationException("Cannot provide a path to the game executable because the game golder path has not yet been set.");
            }
        }
    }
}
