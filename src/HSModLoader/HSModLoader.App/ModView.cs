using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HSModLoader.App
{
    /// <summary>
    /// Used for displaying mod information in the main window's ordered table of mods.
    /// </summary>
    public class ModView : INotifyPropertyChanged
    {
        private ModConfiguration _configuration;

        public int Order
        {
            get
            {
                return _configuration.OrderIndex + 1;
            }
        }

        public string Name
        {
            get
            {
                return _configuration.Mod?.Name ?? string.Empty;
            }
        }

        public string TruncatedName
        {
            get
            {
                if (this.Name.Length <= 32)
                {
                    return this.Name;
                }
                else
                {
                    return this.Name.Substring(0, 29) + "...";
                }
            }
        }

        public string Version
        {
            get
            {
                return _configuration.Mod?.Version ?? string.Empty;
            }
        }

        public string Author
        {
            get
            {
                return _configuration.Mod?.Author ?? string.Empty;
            }
        }

        public bool IsEnabled
        {
            get
            {
                return _configuration.State == ModState.Enabled;
            }
            set {
                if (value)
                {
                    _configuration.State = ModState.Enabled;
                    this.Refresh();
                }
            }
        }

        public bool IsDisabled
        {
            get
            {
                return _configuration.State == ModState.Disabled;
            }
            set
            {
                if (value)
                {
                    _configuration.State = ModState.Disabled;
                    this.Refresh();
                }
            }
        }

        public bool IsSoftDisabled
        {
            get
            {
                return _configuration.State == ModState.SoftDisabled;
            }
            set
            {
                if (value)
                {
                    _configuration.State = ModState.SoftDisabled;
                    this.Refresh();
                }
            }
        }

        public string State
        {
            get
            {
                if(_configuration.State == ModState.Enabled)
                {
                    return "Enabled";
                }
                else if(_configuration.State == ModState.Disabled)
                {
                    return "Disabled";
                }
                else if (_configuration.State == ModState.SoftDisabled)
                {
                    return "Soft-Disabled";
                }
                else
                {
                    return "Unknown";
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ModView(ModConfiguration configuration) 
        {
            this._configuration = configuration;
        }

        public void Set(ModConfiguration configuration)
        {
            this._configuration = configuration;
            this.Refresh();

        }

        public void Refresh()
        {
            // Assumes all properties have changed
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }

    }
}
