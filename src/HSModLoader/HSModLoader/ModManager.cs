
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
        // Base game folders
        private static readonly string GameModsFolder = @"RPGTacGame\Mods";                    // relative to GameFolderPath
        private static readonly string RelativeGameModsFolder = @"..\..\RPGTacGame\Mods";      // relative to the game executable
        private static readonly string GameExecutable64Bit = @"Binaries\Win64\RPGTacGame.exe"; // relative to GameFolderPath
        private static readonly string GameExecutable32Bit = @"Binaries\Win32\RPGTacGame.exe"; // relative to GameFolderPath
        private static readonly string GameConfigurationsFolder = @"RPGTacGame\Config";        // relative to GameFolderPath

        // Configuration files and their structure
        private static readonly string GameEngineConfigurationFile = "RPGTacEngine.ini";
        private static readonly string GameEnginePathSection = "Core.System";
        private static readonly string GameEngineContentPathKey = "Paths";
        private static readonly string GameEngineScriptPathKey = "ScriptPaths";
        private static readonly string GameEngineLocalizationPathKey = "LocalizationPaths";

        private static readonly string GameMutatorConfigurationFile = "RPGTacMods.ini";
        private static readonly string MutatorLoaderSection = "rpgtacgame.RPGTacMutatorLoader";
        private static readonly string MutatorLoaderKey = "MutatorsLoaded";

        // Files that are not part of the base game
        private static readonly string ConfigurationFile = "config.json"; // relative to app folder
        private static readonly string ChangeLogFile = "filechanges.log"; // relative to app folder
        public static readonly string ModInfoFile = "mod.json";           // relative to a mod's storage folder, TODO: move this to Mod class

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
        /// </summary>
        public void InitializeGameModsFolder()
        {

            if(string.IsNullOrEmpty(this.GameFolderPath) || !Directory.Exists(this.GameFolderPath))
            {
                throw new InvalidOperationException("Cannot initialize game mods folder because the path to the game folder has not been defined.");
            }

            var active = Path.Combine(this.GameFolderPath, GameModsFolder);

            if (!Directory.Exists(active))
            {
                Directory.CreateDirectory(active);
            }

        }

        /// <summary>
        /// Deserializes this ModManager from disk. Also checks the
        /// mods folder for any new mod packages.
        /// </summary>
        public Result Load()
        {
            var result = new Result();

            if (File.Exists(ConfigurationFile))
            {
                var json = File.ReadAllText(ConfigurationFile);

                try
                {
                    var m = JsonSerializer.Deserialize<ModManager>(json);

                    this.ModConfigurations = m.ModConfigurations;
                    this.GameFolderPath = m.GameFolderPath;

                    // Instantiate mod configurations from the mod manager's
                    // json file.
                    foreach(var configuration in this.ModConfigurations)
                    {

                        if(!Directory.Exists(configuration.ModStorageFolder))
                        {
                            throw new ModException("The storage folder for a previously managed mod no longer exists.");
                        }

                        var modinfo = Path.Combine(configuration.ModStorageFolder, ModManager.ModInfoFile);

                        if(!File.Exists(modinfo))
                        {
                            throw new ModException(string.Format("A registered mod is missing its mod.json file. Expected the file to exist at {0}.", modinfo));
                        }

                        var contents = File.ReadAllText(modinfo);
                        configuration.Mod = JsonSerializer.Deserialize<Mod>(contents);
                        configuration.IsManaged = true;
                    }

                    var modsFolder = Path.Combine(this.GameFolderPath, GameModsFolder);

                    // Attempt to instantiate mod configurations from the mods
                    // folder.
                    if(Directory.Exists(modsFolder))
                    {
                        var subfolders = Directory.GetDirectories(modsFolder);

                        foreach(var subfolder in subfolders)
                        {
                            var modinfo = Path.Combine(subfolder, ModManager.ModInfoFile);

                            if(File.Exists(modinfo))
                            {
                                var contents = File.ReadAllText(modinfo);
                                var mod = JsonSerializer.Deserialize<Mod>(contents);

                                if(!this.IsRegistered(mod))
                                {
                                    var newConfiguration = this.RegisterMod(mod, subfolder);
                                    newConfiguration.State = this.AssessFileState(newConfiguration);
                                    newConfiguration.IsManaged = false;
                                }
                            }
                        }

                    }
                    else
                    {
                        // No action. It is a valid scenario for the mods folder to have been created yet.
                    }
                    
                    result.IsSuccessful = true;

                }
                catch(ModException e)
                {
                    result.ErrorMessage = e.Message;
                    e.AppendToLogFile();
                }
                catch(Exception e)
                {
                    result.ErrorMessage = "Failed to load mod manager configuration file. See error.log.";
                    e.AppendToLogFile();
                }
                
            }
            else
            {
                // No action. It is a valid scenario for this file to not exist.
            }

            return result;

        }

        private ModState AssessFileState(ModConfiguration configuration)
        {
            var mod = configuration.Mod;
            var gameEngineConfigPath = Path.Combine(this.GameFolderPath, GameConfigurationsFolder, GameEngineConfigurationFile);
            var gameEngineConfig = new GameConfiguration(gameEngineConfigPath);
            gameEngineConfig.Load();

            var relativePath = this.GetRelativeModStoragePath(configuration);
            var hasContentPathDefined = gameEngineConfig.IsIncluded(GameEnginePathSection, GameEngineContentPathKey, relativePath);
            var hasLocalizationPathDefined = gameEngineConfig.IsIncluded(GameEnginePathSection, GameEngineLocalizationPathKey, relativePath);
            var hasScriptsPathDefined = gameEngineConfig.IsIncluded(GameEnginePathSection, GameEngineScriptPathKey, relativePath);

            if(hasContentPathDefined && hasLocalizationPathDefined && hasScriptsPathDefined)
            {
                return ModState.Enabled;
            }
            else if(hasContentPathDefined && hasLocalizationPathDefined)
            {
                return ModState.SoftDisabled;
            }
            else if(!(hasContentPathDefined && hasLocalizationPathDefined && hasScriptsPathDefined))
            {
                return ModState.Disabled;
            }
            else
            {
                return ModState.Undetermined;
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

            if (!File.Exists(filepath))
            {
                result.ErrorMessage = "Mod package file could not be loaded because it cannot be accessed or does not exist.";
            }
            else
            {
                var temporaryDestination = Path.Combine(this.GameFolderPath, GameModsFolder, Path.GetRandomFileName());
                ModConfiguration configuration = null;

                try
                {
                    Directory.CreateDirectory(temporaryDestination);
                    ZipFile.ExtractToDirectory(filepath, temporaryDestination);

                    var modinfo = Path.Combine(temporaryDestination, ModInfoFile);

                    if (!File.Exists(modinfo))
                    {
                        throw new ModRegistrationException("Could not load mod. Could not find and extract mod.json within the mod package file.");
                    }

                    var contents = File.ReadAllText(modinfo);
                    var mod = JsonSerializer.Deserialize<Mod>(contents);
                    configuration = this.RegisterMod(mod, temporaryDestination);
                    configuration.IsManaged = true;
                    result.IsSuccessful = true;

                }
                catch (ModRegistrationException e)
                {
                    // If we land in here, we know exactly what the problem was. The
                    // error message in the result object can be surfaced to the user
                    // and it should make sense.
                    e.AppendToLogFile();
                    result.ErrorMessage = e.Message;
                }
                catch (Exception e)
                {
                    // If we land in here, we weren't expecting the problem. The error
                    // message might be too cryptic and so the message surfaced to the
                    // user is genericized.
                    e.AppendToLogFile();
                    result.ErrorMessage = "Could not load mod. See error.log file.";
                }

                // The rest of this method is just folder clean-up
                if(!result.IsSuccessful)
                {
                    if (configuration != null && this.ModConfigurations.Contains(configuration))
                    {
                        this.ModConfigurations.Remove(configuration);
                    }

                    if (configuration != null && Directory.Exists(configuration.ModStorageFolder))
                    {
                        Directory.Delete(configuration.ModStorageFolder, true);
                    }
                }

                if(Directory.Exists(temporaryDestination))
                {
                    Directory.Delete(temporaryDestination, true);
                }

            }

            return result;

        }

        /// <summary>
        /// Note: this method will throw ModRegistrationExceptions.
        /// Note: the specified path for unmanagedStorageFolder will get moved.
        /// </summary>
        private ModConfiguration RegisterMod(Mod newMod, string unmanagedStorageFolder = null)
        {
            if (string.IsNullOrEmpty(newMod.Id))
            {
                throw new ModRegistrationException(newMod, "Cannot register a mod that has no defined ID.");
            }

            if (this.IsRegistered(newMod))
            {
                throw new ModRegistrationException(newMod, string.Format("Could not register mod because '{0}' version {1} already exists.", newMod.Name, newMod.Version));
            }

            if (unmanagedStorageFolder != null && !Directory.Exists(unmanagedStorageFolder))
            {
                throw new ModRegistrationException(newMod, string.Format("The specified unmanaged storage folder doesn't exist for mod with ID '{0}'", newMod.Id));
            }

            var newModStorageFolder = Path.Combine(this.GameFolderPath, GameModsFolder, newMod.Id);

            if (unmanagedStorageFolder != newModStorageFolder)
            {
                if (Directory.Exists(newModStorageFolder))
                {
                    throw new ModRegistrationException(newMod, string.Format("Could not register mod because a managed storage folder already exists for mod with ID '{0}'", newMod.Id));
                }

                if (unmanagedStorageFolder != null)
                {
                    Directory.Move(unmanagedStorageFolder, newModStorageFolder);
                }
            }
            
            var configuration = new ModConfiguration()
            {
                Mod = newMod,
                ModStorageFolder = newModStorageFolder,
                State = ModState.Disabled
            };

            // order matters for next two statements
            this.ModConfigurations.Add(configuration);
            configuration.OrderIndex = this.ModConfigurations.Count - 1;

            return configuration;

        }

        private bool IsRegistered(Mod newMod)
        {
            foreach (var mod in ModConfigurations.Select(x => x.Mod))
            {
                if (string.Equals(mod.Id, newMod.Id, StringComparison.OrdinalIgnoreCase) 
                    || (string.Equals(mod.Name, newMod.Name, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(mod.Version, newMod.Version, StringComparison.OrdinalIgnoreCase)))
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
                var gameEngineConfigPath = Path.Combine(this.GameFolderPath, GameConfigurationsFolder, GameEngineConfigurationFile);
                var gameEngineConfig = new GameConfiguration(gameEngineConfigPath);

                gameEngineConfig.Load();
                this.DisableModStoragePaths(gameEngineConfig, configuration);
                gameEngineConfig.Save();

                if(Directory.Exists(configuration.ModStorageFolder))
                {
                    Directory.Delete(configuration.ModStorageFolder, true);
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
                var apply = this.ApplyChanges();

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
            var mutatorsConfigPath = Path.Combine(this.GameFolderPath, GameConfigurationsFolder, GameMutatorConfigurationFile);
            var mutatorsConfig = new GameConfiguration(mutatorsConfigPath);
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
                if (config.State == ModState.Enabled) // SoftDisabled mods will not have their mutator class included in RPGTacMods.ini
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

            var configSection = mutatorsConfig.FindSection(MutatorLoaderSection);

            if (configSection == null)
            {
                mutatorsConfig.Sections.Add(new GameConfigurationSection() { Name = MutatorLoaderSection });
            }

            var configItem = mutatorsConfig.FindItem(MutatorLoaderSection, MutatorLoaderKey);

            if (configItem == null)
            {
                configSection.Items.Add(new GameConfigurationItem() { Key = MutatorLoaderKey, Value = newMutatorList });
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

        /// <summary>
        /// Applies changes to files in the game directory.
        /// </summary>
        private Result ApplyChanges()
        {
            var result = new Result() { IsSuccessful = true };

            var gameEngineConfigPath = Path.Combine(this.GameFolderPath, GameConfigurationsFolder, GameEngineConfigurationFile);
            var gameEngineConfig = new GameConfiguration(gameEngineConfigPath);
            gameEngineConfig.Load();

            foreach (var configuration in this.ModConfigurations)
            {
                configuration.IsManaged = true;

                // A reminder here that soft-disabled mods have scripts disabled, but content files remain enabled
                // to allow save files to mostly work correctly
                if (configuration.State == ModState.Enabled || configuration.State == ModState.SoftDisabled)
                {
                    this.AppendChangeLog(string.Format("Mod {0} needs to be added to the game directory...", configuration.Mod.Id));

                    var install = this.EnableModStoragePaths(gameEngineConfig, configuration);

                    if(!install.IsSuccessful)
                    {
                        result.IsSuccessful = false;
                        result.ErrorMessage += install.ErrorMessage;
                        this.DisableModStoragePaths(gameEngineConfig, configuration); // in case some files managed to be added
                        configuration.State = ModState.Disabled;
                    }
                }
                else if (configuration.State == ModState.Disabled)
                {
                    this.AppendChangeLog(string.Format("Mod {0} needs to be removed from game directory...", configuration.Mod.Id));

                    var uninstall = this.DisableModStoragePaths(gameEngineConfig, configuration);
                    if(!uninstall.IsSuccessful)
                    {
                        result.IsSuccessful = false;
                        result.ErrorMessage += uninstall.ErrorMessage;
                    }
                }
            }

            gameEngineConfig.Save();

            return result;
        }


        private Result EnableModStoragePaths(GameConfiguration gameEngineConfig, ModConfiguration configuration)
        {
            var result = new Result();

            try
            {
                if(configuration.State == ModState.Disabled)
                {
                    throw new ModException("Mod is not in an enabled or soft-disabled state.");
                }

                if (!Directory.Exists(configuration.ModStorageFolder))
                {
                    throw new ModException(string.Format("Mod '{0}' doesn't have an existing mod storage folder at '{1}'", configuration.Mod.Id, configuration.ModStorageFolder));
                }

                var relativePath = this.GetRelativeModStoragePath(configuration);
                gameEngineConfig.Include(GameEnginePathSection, GameEngineContentPathKey, relativePath);
                gameEngineConfig.Include(GameEnginePathSection, GameEngineLocalizationPathKey, relativePath);

                if(configuration.State == ModState.Enabled)
                {
                    gameEngineConfig.Include(GameEnginePathSection, GameEngineScriptPathKey, relativePath);
                }
                else if(configuration.State == ModState.SoftDisabled)
                {
                    gameEngineConfig.Exclude(GameEnginePathSection, GameEngineScriptPathKey, relativePath);
                }

                result.IsSuccessful = true;
                this.AppendChangeLog(string.Format("RPGTacMods.ini will include paths for mod '{0}' for folder '{1}'", configuration.Mod.Id, configuration.ModStorageFolder));
                
            }
            catch(ModException e)
            {
                // Exception message can be surfaced to user here
                e.AppendToLogFile();
                result.ErrorMessage = e.Message;
                this.AppendChangeLog(e.Message);
            }
            catch(Exception e)
            {
                // Exception message should not be surfaced to user here
                e.AppendToLogFile();
                result.ErrorMessage = string.Format("Installation failed for mod '{0}'.", configuration.Mod.Id);
                this.AppendChangeLog(result.ErrorMessage);
            }

            return result;

        }

        private Result DisableModStoragePaths(GameConfiguration gameEngineConfig, ModConfiguration configuration)
        {
            var result = new Result();

            try
            {
                var relativePath = this.GetRelativeModStoragePath(configuration);
                gameEngineConfig.Exclude(GameEnginePathSection, GameEngineContentPathKey, relativePath);
                gameEngineConfig.Exclude(GameEnginePathSection, GameEngineScriptPathKey, relativePath);
                gameEngineConfig.Exclude(GameEnginePathSection, GameEngineLocalizationPathKey, relativePath);
                
                result.IsSuccessful = true;
                this.AppendChangeLog(string.Format("RPGTacMods.ini will be updated to *not* include paths for mod '{0}' for folder '{1}'", configuration.Mod.Id, configuration.ModStorageFolder));
            }
            catch(ModException e)
            {
                e.AppendToLogFile();
                result.ErrorMessage = e.Message; // Safe to display to user
                this.AppendChangeLog(e.Message);
            }
            catch(Exception e)
            {
                e.AppendToLogFile();
                result.ErrorMessage = string.Format("Could not disable mod {0}.", configuration.Mod.Id);
                this.AppendChangeLog(result.ErrorMessage);
            }

            return result;

        }

        /// <summary>
        /// Returns a path to the mod's storage folder relative to
        /// the game's executable.
        /// </summary>
        private string GetRelativeModStoragePath(ModConfiguration configuration)
        {
            if (!Directory.Exists(configuration.ModStorageFolder))
            {
                throw new ModException("Cannot extract relative mod storage path becuse the specified mod does not have a storage folder that exists.");
            }

            return Path.Combine(RelativeGameModsFolder, new DirectoryInfo(configuration.ModStorageFolder).Name);
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
