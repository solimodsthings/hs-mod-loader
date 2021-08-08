using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSModLoader.App
{
    public class ModManager
    {
        public List<ConfigurableMod> Mods { get; set; }

        public string GameFolderPath { get; set; }

        public ModManager()
        {
            Mods = new List<ConfigurableMod>();
        }

        public void Save()
        {
            // Save settings to file
        }

        public void RegisterMod(ConfigurableMod mod)
        {
            this.Mods.Add(mod);
            mod.State = ModState.Disabled;
            mod.Order = this.Mods.Count;
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
