using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HSModLoader
{
    /// <summary>
    /// An instance of this class represents a mod and its
    /// current configuration (load order, enabled state, etc.) 
    /// with respect to the game.
    /// </summary>
    public class ModConfiguration
    {

        [JsonIgnore]
        public Mod Mod { get; set; }

        /// <summary>
        /// The location of unpackaged mod files that will be
        /// applied to the game folder if this mod is enabled.
        /// This is also the location of the mod.json file for
        /// this mod.
        /// </summary>
        public string Path { get; set; }

        public ModState State { get; set; }

        public int OrderIndex { get; set; }

        public List<ModFileMapping> Mappings { get; set; }

        public ModConfiguration()
        {
            this.Mappings = new List<ModFileMapping>();
        }

    }
}
