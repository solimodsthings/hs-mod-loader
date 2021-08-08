using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
        public readonly string ModFolder = "mods";

        public List<ConfigurableMod> Mods { get; set; }

        public string GameFolderPath { get; set; }

        public ModManager()
        {
            Mods = new List<ConfigurableMod>();
        }

        public void LoadFromFile()
        {
            if (File.Exists(ConfigurationFile))
            {
                var json = File.ReadAllText(ConfigurationFile);

                try
                {
                    var m = JsonConvert.DeserializeObject<ModManager>(json);

                    this.Mods = m.Mods;
                    this.GameFolderPath = m.GameFolderPath;
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

        public void RegisterMod(Mod mod)
        {
            var cmod = new ConfigurableMod(mod);
            this.Mods.Add(cmod);
            cmod.State = ModState.Disabled;
            cmod.Order = this.Mods.Count;
            
        }

        public void UpdateModOrderValue()
        {
            int order = 1;
            foreach (var mod in this.Mods)
            {
                mod.Order = order++;
            }
        }


    }
}
