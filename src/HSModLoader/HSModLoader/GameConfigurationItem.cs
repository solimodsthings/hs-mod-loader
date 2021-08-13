using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSModLoader
{
    /// <summary>
    /// An instance of this class represents a key-value pair and
    /// single line item of an .ini file section.
    /// </summary>
    public class GameConfigurationItem
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
