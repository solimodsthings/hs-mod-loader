using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HSModLoader.App
{

    public class ModView : INotifyPropertyChanged
    {
        private ConfigurableMod _mod;

        public int Order
        {
            get
            {
                return _mod.OrderIndex + 1;
            }
        }

        public string Name
        {
            get
            {
                return _mod.Name;
            }
        }

        public string TruncatedName
        {
            get
            {
                if (_mod.Name.Length <= 32)
                {
                    return _mod.Name;
                }
                else
                {
                    return _mod.Name.Substring(0, 29) + "...";
                }
            }
        }

        public string Version
        {
            get
            {
                return _mod.Version;
            }
        }

        public string Author
        {
            get
            {
                return _mod.Author;
            }
        }

        public bool IsEnabled
        {
            get
            {
                return _mod.State == ModState.Enabled;
            }
            set {
                if (value)
                {
                    _mod.State = ModState.Enabled;
                    this.Refresh();
                }
            }
        }

        public bool IsDisabled
        {
            get
            {
                return _mod.State == ModState.Disabled;
            }
            set
            {
                if (value)
                {
                    _mod.State = ModState.Disabled;
                    this.Refresh();
                }
            }
        }

        public bool IsSoftDisabled
        {
            get
            {
                return _mod.State == ModState.SoftDisabled;
            }
            set
            {
                if (value)
                {
                    _mod.State = ModState.SoftDisabled;
                    this.Refresh();
                }
            }
        }

        public string State
        {
            get
            {
                if(_mod.State == ModState.Enabled)
                {
                    return "Enabled";
                }
                else if(_mod.State == ModState.Disabled)
                {
                    return "Disabled";
                }
                else if (_mod.State == ModState.SoftDisabled)
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

        public ModView(ConfigurableMod mod) 
        {
            this._mod = mod;
        }

        public void Set(ConfigurableMod mod)
        {
            this._mod = mod;
            this.Refresh();

        }
        public void Refresh()
        {
            // Assumes all properties have changed
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }


    }
}
