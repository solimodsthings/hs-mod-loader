using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            cmod.OrderIndex = this.Mods.Count - 1;
            cmod.State = ModState.Disabled;
            
        }

        /// <summary>
        /// Shifts a mod up the order list by one.
        /// </summary>
        /// <param name="index">Index of the mod to be shifted up the order list.</param>
        public void ShiftModOrderUp(int index)
        {
            if (index > 0 && index < this.Mods.Count)
            {
                var cmod = this.Mods[index];
                this.Mods.RemoveAt(index);
                this.Mods.Insert(index - 1, cmod);
                this.UpdateModOrderValue();
            }
        }

        /// <summary>
        /// Shifts a mod down the order list by one.
        /// </summary>
        /// <param name="index">Index of the mod to be shifted down the order list.</param>
        public void ShiftModOrderDown(int index)
        {
            if (index >= 0 && index + 1 < this.Mods.Count)
            {
                var cmod = this.Mods[index];
                this.Mods.RemoveAt(index);
                this.Mods.Insert(index + 1, cmod);
                this.UpdateModOrderValue();
            }
        }


        public void UpdateModOrderValue()
        {
            int order = 0;
            foreach (var mod in this.Mods)
            {
                mod.OrderIndex = order++;
            }
        }


    }
}
