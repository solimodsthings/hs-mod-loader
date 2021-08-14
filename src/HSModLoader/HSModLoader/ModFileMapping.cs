using System;

namespace HSModLoader
{
    public class ModFileMapping
    {
        public string SourceFile { get; set; }
        public string DestinationFile { get; set; }
        public ModFileType Type { get; set; }
    }
}
