using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSModLoader.App
{
    public enum ModState
    {
        Disabled,
        SoftDisabled,
        Enabled
    }

    public class ConfigurableMod : Mod
    {
        public ModState State { get; set; }

        public int Order { get; set; }

        public string TruncatedName { 
            get
            {
                if(this.Name.Length <= 32)
                {
                    return this.Name;
                }
                else
                {
                    return this.Name.Substring(0, 29) + "...";
                }    
            } 
        }

        public string OptionalUrl
        {
            get
            {
                if (!string.IsNullOrEmpty(this.Url) && !string.IsNullOrWhiteSpace(this.Url))
                {
                    return this.Url;
                }
                else
                {
                    return "N/A";
                }
            }
        }

        public ConfigurableMod()
        {
            State = ModState.Disabled;
        }

    }
}
