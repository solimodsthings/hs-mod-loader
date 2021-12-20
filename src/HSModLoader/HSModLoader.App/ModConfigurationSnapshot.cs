using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSModLoader.App
{
    public class ModConfigurationSnapshot
    {
        public ModConfiguration Mod { get; set; }
        public int OrderIndexSnapshot { get; set; }
        public ModState ModStateSnapshot { get; set; }

        public ModConfigurationSnapshot(ModConfiguration mod)
        {
            this.Mod = mod;
            this.OrderIndexSnapshot = mod.OrderIndex;
            this.ModStateSnapshot = mod.State;
        }
    }
}
