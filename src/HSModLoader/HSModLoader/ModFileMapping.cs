using System;

namespace HSModLoader
{
    /// <summary>
    /// This class is used to track mod files that are added
    /// inside the game folder so they can easily be removed.
    /// </summary>
    public class ModFileMapping
    {
        public ModFileType Type { get; set; }
        public string SourceFile { get; set; }
        public string DestinationFile { get; set; }
    }
}
