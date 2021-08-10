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
        public readonly string ConfigurationFile = "config.json";
        [JsonIgnore]
        public readonly string ModInfoFile = "mod.json";

        [JsonIgnore]
        public readonly string ModFolder = "mods";

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

        private void RegisterMod(Mod mod, string repository)
        {
            var configuration = new ModConfiguration();

            configuration.Mod = mod;
            configuration.Path = repository;
            configuration.State = ModState.Disabled;

            // order matters for next two statements
            this.ModConfigurations.Add(configuration); 
            configuration.OrderIndex = this.ModConfigurations.Count - 1;
            
        }

        public void RegisterModFromFile(string filepath)
        {
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

                        this.RegisterMod(mod, destination);
                    }

                }
                catch (Exception e)
                {
                    e.AppendToLogFile();
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


    }
}
