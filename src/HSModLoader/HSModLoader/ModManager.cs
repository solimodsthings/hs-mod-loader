
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
        private static readonly string ModFolder = "mods";
        private static readonly string ScriptsFolder = @"RPGTacGame\Script";
        private static readonly string ContentFolder = @"RPGTacGame\Content";
        private static readonly string LocalizationFolder = @"RPGTacGame\Localization";
        private static readonly string ConfigFolder = @"RPGTacGame\Config";
        private static readonly string GameExecutable64Bit = @"Binaries\Win64\RPGTacGame.exe";
        private static readonly string GameExecutable32Bit = @"Binaries\Win32\RPGTacGame.exe";
        private static readonly string MutatorLoaderSection = "rpgtacgame.RPGTacMutatorLoader";
        private static readonly string MutatorLoaderItem = "MutatorsLoaded";
        private static readonly string ManagedModPrefix = "managed-mod-";
        private static readonly string ChangeLogFile = "filechanges.log";

        public static readonly string ModInfoFile = "mod.json";

        public List<ModConfiguration> ModConfigurations { get; set; }

        public string GameFolderPath { get; set; }

        private StringBuilder ChangeLog;

        public ModManager()
        {
            ModConfigurations = new List<ModConfiguration>();
            ChangeLog = new StringBuilder();
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
                        var modinfo = configuration.Path + Path.DirectorySeparatorChar + ModInfoFile;

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
                var temporaryDestination = Path.Combine(ModFolder, Path.GetRandomFileName());

                try
                {
                    Directory.CreateDirectory(temporaryDestination);
                    ZipFile.ExtractToDirectory(filepath, temporaryDestination);

                    var modinfo = temporaryDestination + Path.DirectorySeparatorChar + ModInfoFile;

                    if (File.Exists(modinfo))
                    {
                        var contents = File.ReadAllText(modinfo);
                        var mod = JsonSerializer.Deserialize<Mod>(contents);

                        if (!this.IsRegistered(mod.Name, mod.Version))
                        {

                            var trueDestination = Path.Combine(ModFolder, string.Format("{0}_{1}", mod.Name.Trim(), mod.Version.Trim()).Replace(" ", string.Empty));

                            Directory.Move(temporaryDestination, trueDestination);

                            var mappingResult = this.AddModConfiguration(mod, trueDestination);

                            if (mappingResult.IsSuccessful)
                            {
                                result.IsSuccessful = true;
                            }
                            else
                            {
                                result.ErrorMessage += mappingResult.ErrorMessage;
                            }
                            
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
                }
            }
            else
            {
                result.ErrorMessage = "Mod package file could not be loaded because it cannot be accessed or does not exist.";
            }

            return result;

        }

        private Result AddModConfiguration(Mod mod, string repository)
        {
            var configuration = new ModConfiguration();

            configuration.Mod = mod;
            configuration.Path = repository;
            configuration.State = ModState.Disabled;

            var mappingResult = this.CreateModFileMappings(configuration);

            if (mappingResult.IsSuccessful)
            {
                // order matters for next two statements
                this.ModConfigurations.Add(configuration);
                configuration.OrderIndex = this.ModConfigurations.Count - 1;
            }

            return mappingResult;
        }

        private Result CreateModFileMappings(ModConfiguration configuration)
        {
            bool invalidLocalizationFile = false;
            bool invalidFileType = false;

            foreach (var file in configuration.Mod.ModFiles)
            {
                var mapping = new ModFileMapping();

                mapping.Type = file.Type;
                mapping.SourceFile = file.Name;

                if (file.Type == ModFileType.Script)
                {

                    mapping.DestinationFile = Path.Combine(ScriptsFolder, ManagedModPrefix + file.Name);
                    configuration.Mappings.Add(mapping);

                }
                else if (file.Type == ModFileType.Content)
                {

                    mapping.DestinationFile = Path.Combine(ContentFolder, ManagedModPrefix + file.Name);
                    configuration.Mappings.Add(mapping);

                }
                else if (file.Type == ModFileType.Localization)
                {

                    var tokens = file.Name.Split('.');
                    var language = tokens[tokens.Length - 1];
                    var localizationFolder = Path.Combine(LocalizationFolder, language.ToUpper());

                    if (Directory.Exists(Path.Combine(this.GameFolderPath, localizationFolder)))
                    {
                        mapping.DestinationFile = Path.Combine(localizationFolder, ManagedModPrefix + file.Name);
                        configuration.Mappings.Add(mapping);
                    }
                    else
                    {
                        invalidLocalizationFile = true;
                        LogFileExtensions.AppendToLogFile(string.Format(
                            "Mod '{0}' version {1} has a localization file that does not match any current supported game language. Invalid file was '{2}'.", configuration.Mod.Name, configuration.Mod.Version, file.Name));
                    }

                }
                else
                {
                    invalidFileType = true;
                    LogFileExtensions.AppendToLogFile(string.Format(
                            "Mod '{0}' version {1} has a file type that is not yet supported by this mod loader. Invalid file was '{2}'. ", configuration.Mod.Name, configuration.Mod.Version, file.Name));
                }
            }

            var result = new Result() { IsSuccessful = true };

            if(invalidLocalizationFile)
            {
                result.IsSuccessful = false;
                result.ErrorMessage += " Mod contains an invalid localization file.";
            }
            else if(invalidFileType)
            {
                result.IsSuccessful = false;
                result.ErrorMessage += " Mod contains an invalid file type.";
            }

            if(!result.IsSuccessful)
            {
                result.ErrorMessage += " See error.log for details.";
                result.ErrorMessage = result.ErrorMessage.Trim();
            }

            return result;

        }
        
        private bool IsRegistered(string modname, string version)
        {
            foreach (var mod in ModConfigurations.Select(x => x.Mod))
            {
                if (mod.Name.Equals(modname) && mod.Version.Equals(version))
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
                this.UninstallModFiles(configuration);
                Directory.Delete(configuration.Path, true);
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
            var mutatorsConfig = new GameConfiguration(Path.Combine(this.GameFolderPath, ConfigFolder, "RPGTacMods.ini"));
            mutatorsConfig.Load();

            // This list of all available mutator classes will be used to preserve unmanaged
            // mods the player may have added manually
            var allAvailableMutators = this.ModConfigurations
                .Select(x => x.Mod)
                .Where(x => x.HasMutator && !string.IsNullOrEmpty(x.MutatorStartClass))
                .Select(x => x.MutatorStartClass);

            // First we find out what exactly is enabled now
            var enabledMutatorClasses = new List<string>();

            foreach (var config in this.ModConfigurations)
            {
                if (config.State == ModState.Enabled)
                {
                    if (config.Mod.HasMutator && !string.IsNullOrEmpty(config.Mod.MutatorStartClass))
                    {
                        enabledMutatorClasses.Add(config.Mod.MutatorStartClass);
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
            }


            mutatorsConfig.Save();
            this.AppendChangeLog(string.Format("File updated: {0}", mutatorsConfig.FileName));
        }

        private Result ApplyModsToGameFolder()
        {
            var result = new Result() { IsSuccessful = true };

            foreach (var configuration in this.ModConfigurations)
            {
                if (configuration.State == ModState.Enabled)
                {
                    var install = this.InstallModFiles(configuration);

                    if(!install.IsSuccessful)
                    {
                        result.IsSuccessful = false;
                        result.ErrorMessage += install.ErrorMessage;
                        this.UninstallModFiles(configuration); // in case some files managed to be added
                        configuration.State = ModState.Disabled;
                    }
                }
                else if (configuration.State == ModState.SoftDisabled)
                {
                    this.UninstallModFiles(configuration);
                    var install = this.InstallModFiles(configuration, ModFileType.Content, ModFileType.Localization);

                    if (!install.IsSuccessful)
                    {
                        result.IsSuccessful = false;
                        result.ErrorMessage += install.ErrorMessage;
                        this.UninstallModFiles(configuration);
                        configuration.State = ModState.Disabled;
                    }
                }
                else if (configuration.State == ModState.Disabled)
                {
                    this.UninstallModFiles(configuration);
                }
            }

            return result;
        }

        private Result InstallModFiles(ModConfiguration configuration)
        {
            var types = (ModFileType[])Enum.GetValues(typeof(ModFileType));
            return this.InstallModFiles(configuration, types);
        }

        private Result InstallModFiles(ModConfiguration configuration, params ModFileType[] typesToInstall)
        {
            this.AppendChangeLog(string.Format("Adding files for mod {0}, for the following types:...", configuration.Mod.Name));
            foreach(var type in typesToInstall)
            {
                this.AppendChangeLog(Enum.GetName(typeof(ModFileType), type));
            }

            var result = new Result() { IsSuccessful = true };

            foreach(var mapping in configuration.Mappings)
            {
                if(typesToInstall.Contains(mapping.Type))
                {
                    var source = Path.Combine(configuration.Path, mapping.SourceFile);

                    if (File.Exists(source))
                    {
                        var destination = Path.Combine(this.GameFolderPath, mapping.DestinationFile);
                        if (!File.Exists(destination))
                        {
                            File.Copy(source, destination);
                            this.AppendChangeLog(string.Format("File added: {0}", destination));
                        }
                        else
                        {
                            this.AppendChangeLog(string.Format("File confirmed to already exist: {0}", destination));
                        }
                        
                    }
                    else
                    {
                        result.IsSuccessful = false;
                        LogFileExtensions.AppendToLogFile(string.Format(
                            "File '{0} for mod '{1}' version {2} does not exist and cannot be installed into the game directory.", 
                            source, 
                            configuration.Mod.Name, 
                            configuration.Mod.Version));
                        this.AppendChangeLog(string.Format("File could not be added because the source does not exist: {0}", source));
                    }
                }
            }

            if(!result.IsSuccessful)
            {
                result.ErrorMessage = string.Format("Could not install Mod '{0}' version {1} into the game. The mod has an issue with one or more files.", configuration.Mod.Name, configuration.Mod.Version);
            }

            return result;
        }

        private void UninstallModFiles(ModConfiguration configuration)
        {
            this.AppendChangeLog(string.Format("Checking files that need to be removed for mod {0}...", configuration.Mod.Name));
            foreach (var mapping in configuration.Mappings)
            {
                var target = Path.Combine(this.GameFolderPath, mapping.DestinationFile);

                if (File.Exists(target))
                {
                    File.Delete(target);
                    this.AppendChangeLog(string.Format("File removed: {0}", target));
                }
                else
                {
                    this.AppendChangeLog(string.Format("File confirmed to have already been removed: {0}", target));
                }
            }
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
            this.ChangeLog.Clear();
        }

        /// <summary>
        /// Used to record file changes in the game folder.
        /// </summary>
        private void AppendChangeLog(string message)
        {
            this.ChangeLog.AppendLine(string.Format("[{0}] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), message));
        }

        /// <summary>
        /// Writes recent changes to filechange.log.
        /// </summary>
        private void EndChangeLog()
        {
            File.WriteAllText(ChangeLogFile, this.ChangeLog.ToString());
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
