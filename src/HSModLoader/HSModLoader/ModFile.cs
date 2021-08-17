using System;

namespace HSModLoader
{

    /// <summary>
    /// An instance of this class represents a single script, 
    /// content pacakge, or localization file that is part of
    /// a greater mod.
    /// </summary>
    public class ModFile
    {
        public string Name { get; set; }
        public ModFileType Type { get; set; }
    }
}
