using Newtonsoft.Json;
using System.Reflection;
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

        [JsonIgnore]
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

        [JsonIgnore]
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

        public ConfigurableMod(Mod mod) : base()
        {

            var modProperties = typeof(Mod).GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var property in modProperties)
            {
                var cmodProperty = typeof(ConfigurableMod).GetProperty(property.Name);

                if(cmodProperty != null)
                {
                    cmodProperty.SetValue(this, property.GetValue(mod, null), null);
                }
            }

        }

    }
}
