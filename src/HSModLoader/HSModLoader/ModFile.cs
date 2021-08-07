using System;

namespace HSModLoader
{
    public enum ModFileType
    {
        Script,
        Content,
        Localization,
    }

    public class ModFile
    {
        public string Name { get; set; }
        public ModFileType Type { get; set; }
    }
}
