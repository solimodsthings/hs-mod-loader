using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSModLoader.App
{
    public class ModFileMapping
    {
        public string SourceFile { get; set; }
        public string DestinationFile { get; set; }
        public ModFileType Type { get; set; }
    }
}
