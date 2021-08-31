﻿using System;
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
    public class ModViewModel : INotifyPropertyChanged
    {
        public ModConfiguration Configuration { get; private set; }

        public int Order
        {
            get
            {
                return Configuration.OrderIndex + 1;
            }
        }

        public string Name
        {
            get
            {
                var name = Configuration.Mod?.Name ?? string.Empty;

                if(!this.IsManaged)
                {
                    name += " (Unmanaged)";
                }

                return name;
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
                return Configuration.Mod?.Version ?? string.Empty;
            }
        }

        public string Author
        {
            get
            {
                return Configuration.Mod?.Author ?? string.Empty;
            }
        }

        public bool IsManaged
        {
            get
            {
                return Configuration.IsManaged;
            }
            
        }

        public string RegistrationType
        {
            get
            {
                if (Configuration.Mod == null) 
                { 
                    return string.Empty; 
                }
                else if (Configuration.RegistrationType == HSModLoader.RegistrationType.Standalone)
                {
                    return "Standalone";
                }
                else if (Configuration.RegistrationType == HSModLoader.RegistrationType.SteamWorkshopItem)
                {
                    return "Steam";
                }
                else
                {
                    return "Undetermined";
                }
            }
        }

        public bool IsEnabled
        {
            get
            {
                return Configuration.State == ModState.Enabled;
            }
            set {
                if (value)
                {
                    Configuration.State = ModState.Enabled;
                    this.Refresh();
                }
            }
        }

        public bool IsDisabled
        {
            get
            {
                return Configuration.State == ModState.Disabled;
            }
            set
            {
                if (value)
                {
                    Configuration.State = ModState.Disabled;
                    this.Refresh();
                }
            }
        }

        public bool IsSoftDisabled
        {
            get
            {
                return Configuration.State == ModState.SoftDisabled;
            }
            set
            {
                if (value)
                {
                    Configuration.State = ModState.SoftDisabled;
                    this.Refresh();
                }
            }
        }

        public string State
        {
            get
            {
                if(Configuration.State == ModState.Enabled)
                {
                    return "Enabled";
                }
                else if(Configuration.State == ModState.Disabled)
                {
                    return "Disabled";
                }
                else if (Configuration.State == ModState.SoftDisabled)
                {
                    return "Soft-Disabled";
                }
                else if (Configuration.State == ModState.Undetermined)
                {
                    return "Undetermined";
                }
                else
                {
                    return "Unknown";
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ModViewModel(ModConfiguration configuration) 
        {
            this.Configuration = configuration;
        }

        public void Set(ModConfiguration configuration)
        {
            this.Configuration = configuration;
            this.Refresh();

        }

        public void Refresh()
        {
            // Assumes all properties have changed
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }

    }
}