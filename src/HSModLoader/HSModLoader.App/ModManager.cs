using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSModLoader.App
{

    public class ModManager
    {
        [JsonIgnore]
        public static readonly string ConfigurationFile = "config.json";
        [JsonIgnore]
        public static readonly string ModInfoFile = "mod.json";
        [JsonIgnore]
        public static readonly string ModFolder = "mods";
        [JsonIgnore]
        public static readonly string ScriptsFolder = @"RPGTacGame\Script";
        [JsonIgnore]
        public static readonly string ContentFolder = @"RPGTacGame\Content";
        [JsonIgnore]
        public static readonly string LocalizationFolder = @"RPGTacGame\Localization";
        [JsonIgnore]
        public static readonly string ConfigFolder = @"RPGTacGame\Config";
        [JsonIgnore]
        public static readonly string MutatorLoaderSection = "rpgtacgame.RPGTacMutatorLoader";
        [JsonIgnore]
        public static readonly string MutatorLoaderItem = "MutatorsLoaded";
        [JsonIgnore]
        public static readonly string ManagedModPrefix = "managed-mod-";

        public List<ModConfiguration> ModConfigurations { get; set; }

        public string GameFolderPath { get; set; }

        public ModManager()
        {
            ModConfigurations = new List<ModConfiguration>();
        }

        public void LoadFromFile()
        {
            if (File.Exists(ConfigurationFile))
            {
                var json = File.ReadAllText(ConfigurationFile);

                try
                {
                    var m = JsonConvert.DeserializeObject<ModManager>(json);

                    this.ModConfigurations = m.ModConfigurations;
                    this.GameFolderPath = m.GameFolderPath;

                    foreach(var configuration in this.ModConfigurations)
                    {
                        var modinfo = configuration.Path + Path.DirectorySeparatorChar + ModInfoFile;

                        if (File.Exists(modinfo))
                        {
                            var contents = File.ReadAllText(modinfo);
                            configuration.Mod = JsonConvert.DeserializeObject<Mod>(contents);
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

        public void SaveToFile()
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(ConfigurationFile, json);
        }

        private bool Exists(string modname, string version)
        {
            foreach(var mod in ModConfigurations.Select(x => x.Mod))
            {
                if(mod.Name.Equals(modname) && mod.Version.Equals(version))
                {
                    return true;
                }
            }

            return false;
        }

        public Result RegisterModFromFile(string filepath)
        {
            var result = new Result() { Value = false };

            if (File.Exists(filepath))
            {
                // TODO: Think of a more usable folder path
                var destination = Path.Combine(ModFolder, Path.GetRandomFileName());

                try
                {
                    Directory.CreateDirectory(destination);
                    ZipFile.ExtractToDirectory(filepath, destination);

                    var modinfo = destination + Path.DirectorySeparatorChar + ModInfoFile;

                    if (File.Exists(modinfo))
                    {
                        var contents = File.ReadAllText(modinfo);
                        var mod = JsonConvert.DeserializeObject<Mod>(contents);

                        if (!this.Exists(mod.Name, mod.Version))
                        {
                            var mappingResult = this.RegisterMod(mod, destination);

                            if (mappingResult.Value)
                            {
                                result.Value = true;
                            }
                            else
                            {
                                result.Message += mappingResult.Message;
                            }
                            
                        }
                        else
                        {
                            result.Message = string.Format("Could not load mod. '{0}' version {1} already exists!", mod.Name, mod.Version);
                        }

                    }
                    else
                    {
                        result.Message = "Could not load mod. Could not find and extract mod.json within the mod package file.";
                    }

                }
                catch (Exception e)
                {
                    e.AppendToLogFile();
                    result.Message = "Could not load mod. See error.log file.";
                }
            }
            else
            {
                result.Message = "Mod package file could not be loaded because it cannot be accessed or does not exist.";
            }

            return result;

        }

        private Result RegisterMod(Mod mod, string repository)
        {
            var configuration = new ModConfiguration();

            configuration.Mod = mod;
            configuration.Path = repository;
            configuration.State = ModState.Disabled;

            var mappingResult = this.CreateFilesMappings(configuration);

            // order matters for next two statements
            if (mappingResult.Value)
            {
                this.ModConfigurations.Add(configuration);
                configuration.OrderIndex = this.ModConfigurations.Count - 1;
            }

            return mappingResult;
        }

        private Result CreateFilesMappings(ModConfiguration configuration)
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

            var result = new Result() { Value = true };

            if(invalidLocalizationFile)
            {
                result.Value = false;
                result.Message += " Mod contains an invalid localization file.";
            }
            else if(invalidFileType)
            {
                result.Value = false;
                result.Message += " Mod contains an invalid file type.";
            }

            if(!result.Value)
            {
                result.Message += " See error.log for details.";
                result.Message = result.Message.Trim();
            }

            return result;

        }

 
        /// <summary>
        /// Updates the game folder by adding or removing mod files based on the respective mod's
        /// state (ie. enabled, soft disabled, or disabled). The method does not modify existing base
        /// game files except for RPGTacMods.ini which is necessary for any mod that has a mutator class.
        /// </summary>
        public void ApplyMods()
        {
            var mutatorsConfig = new GameConfiguration(Path.Combine(this.GameFolderPath, ConfigFolder, "RPGTacMods.ini"));

            try
            {
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
                        if(config.Mod.HasMutator && !string.IsNullOrEmpty(config.Mod.MutatorStartClass))
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
                        if(enabledMutatorClasses.Count > 0)
                        {
                            newMutatorList += ",";
                        }

                        newMutatorList += string.Join(",", unmanagedMutatorClasses);
                    }

                    configItem.Value = newMutatorList;
                }
                

                mutatorsConfig.Save();

            }
            catch(Exception e)
            {
                e.AppendToLogFile();
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


    }
}
