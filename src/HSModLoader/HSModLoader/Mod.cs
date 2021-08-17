using System;
using System.Collections.Generic;


namespace HSModLoader
{
    /// <summary>
    /// An instance of this class represents a mod
    /// that can be installed on top of the base game.
    /// </summary>
    public class Mod
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string Author { get; set; }
        public string AuthorUrl { get; set; }
        public bool HasCampaign { get; set; }
        public bool HasMutator { get; set; }
        public string MutatorStartClass { get; set; }
        public bool IsSteamWorkshopItem { get; set; }
        public ulong? SteamWorkshopId { get; set; }
        public string LastUpdated { get; set; }
        public List<ModFile> ModFiles { get; set; }
        public List<ModDependency> Depedencies { get; set; }
    
        public Mod()
        {
            this.ModFiles = new List<ModFile>();
            this.Depedencies = new List<ModDependency>();
        }

        public void AddScript(string name)
        {
            this.ModFiles.Add(new ModFile() { Type = ModFileType.Script, Name = name });
        }

        public void AddContentPackage(string name)
        {
            this.ModFiles.Add(new ModFile() { Type = ModFileType.Content, Name = name });
        }

        public void AddLocalization(string name)
        {
            this.ModFiles.Add(new ModFile() { Type = ModFileType.Localization, Name = name });
        }

    }
}
