﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSModLoader.App.Publishing
{
    public class FileView
    {
        public string Name { get; set; }
        public string Type
        {
            get
            {
                if (string.IsNullOrEmpty(this.Name))
                {
                    return "Unknown";
                }

                var lower = this.Name.ToLower();

                if (lower.Equals(ModManager.ModInfoFile))
                {
                    return "Info";
                }
                else if (lower.EndsWith(".u"))
                {
                    return "Script";
                }
                else if (lower.EndsWith(".upk"))
                {
                    return "Content";
                }
                else if (lower.EndsWith(".txt"))
                {
                    return "Text";
                }
                else if (lower.EndsWith(".ini"))
                {
                    return "Config";
                }
                else
                {
                    return "Other";
                }
            }
        }

        public FileView(string path)
        {
            this.Name = Path.GetFileName(path);
        }
    }
}
