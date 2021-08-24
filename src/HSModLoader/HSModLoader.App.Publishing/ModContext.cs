using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSModLoader.App.Publishing
{
    public class ModContext : INotifyPropertyChanged
    {
        private Mod _Mod;
        public Mod Mod {
            get
            {
                return _Mod;
            }
            set
            {
                _Mod = value;
                _HasAuthorUrl = !string.IsNullOrEmpty(value.AuthorUrl);
                _HasModUrl = !string.IsNullOrEmpty(value.Url);
                _HasMutatorClass = !string.IsNullOrEmpty(value.MutatorClass);
                NotifyPropertyChangedEventHandlers();
            }
        }

        private string _Directory;
        public string Directory
        {
            get
            {
                return _Directory;
            }
            set
            {
                _Directory = value;
                NotifyPropertyChangedEventHandlers();
            }
        }

        public string Name { 
            get
            {
                return Mod == null ? string.Empty : Mod.Name;
            } 
            set
            {
                Mod.Name = value;
                NotifyPropertyChangedEventHandlers();
            }
        }

        public string Id
        {
            get
            {
                return Mod == null ? string.Empty : Mod.Id;
            }
            set
            {
                Mod.Id= value;
                NotifyPropertyChangedEventHandlers();
            }
        }


        public string SteamId
        {
            get
            {
                return Mod == null ? string.Empty : Mod.SteamWorkshopId.ToString();
            }
            set
            {
                Mod.SteamWorkshopId = ulong.Parse(value);
                NotifyPropertyChangedEventHandlers();
            }
        }


        public string Version
        {
            get
            {
                return Mod == null ? string.Empty : Mod.Version;
            }
            set
            {
                Mod.Version = value;
                NotifyPropertyChangedEventHandlers();
            }
        }

        public string Author
        {
            get
            {
                return Mod == null ? string.Empty : Mod.Author;
            }
            set
            {
                Mod.Author = value;
                NotifyPropertyChangedEventHandlers();
            }
        }

        private bool _HasModUrl;
        public bool HasModUrl
        {
            get
            {
                return _HasModUrl;
            }
            set
            {
                _HasModUrl = value;
                NotifyPropertyChangedEventHandlers();

                if (!value)
                {
                    this.ModUrl = string.Empty;
                }
            }
        }

        public string ModUrl
        {
            get
            {
                return Mod == null ? string.Empty : Mod.Url;
            }
            set
            {
                Mod.Url = !string.IsNullOrEmpty(value) ? value : null;
                NotifyPropertyChangedEventHandlers();
            }
        }

        private bool _HasAuthorUrl;
        public bool HasAuthorUrl
        {
            get
            {
                return _HasAuthorUrl;
            }
            set
            {
                _HasAuthorUrl = value;
                NotifyPropertyChangedEventHandlers();

                if (!value)
                {
                    this.AuthorUrl = string.Empty;
                }
            }
        }

        public string AuthorUrl
        {
            get
            {
                return Mod == null ? string.Empty : Mod.AuthorUrl;
            }
            set
            {
                Mod.AuthorUrl = !string.IsNullOrEmpty(value) ? value : null;
                NotifyPropertyChangedEventHandlers();
            }
        }

        private bool _HasMutatorClass;
        public bool HasMutatorClass
        {
            get
            {
                return _HasMutatorClass;
            }
            set
            {
                _HasMutatorClass = value;
                NotifyPropertyChangedEventHandlers();

                if(!value)
                {
                    this.MutatorClass = string.Empty;
                }
            }
        }

        public string MutatorClass
        {
            get
            {
                return Mod == null ? string.Empty : Mod.MutatorClass;
            }
            set
            {
                Mod.MutatorClass = !string.IsNullOrEmpty(value) ? value : null;
                NotifyPropertyChangedEventHandlers();
            }
        }

        public string Description
        {
            get
            {
                return Mod == null ? string.Empty : Mod.Description;
            }
            set
            {
                Mod.Description = value;
                NotifyPropertyChangedEventHandlers();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChangedEventHandlers()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }
}
