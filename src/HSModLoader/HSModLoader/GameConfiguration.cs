using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSModLoader
{
    /// <summary>
    /// An instance of this class represents a configurable .ini file.
    /// </summary>
    public class GameConfiguration
    {
        public string FileName { get; set; }
        public List<GameConfigurationSection> Sections { get; set; }

        public GameConfiguration(string filename)
        {
            this.FileName = filename;
            this.Sections = new List<GameConfigurationSection>();
        }

        public void Load()
        {
            this.Sections.Clear();
            if (File.Exists(this.FileName))
            {
                GameConfigurationSection currentSection = null;

                var lines = File.ReadAllLines(this.FileName);

                foreach (var line in lines)
                {
                    var tline = line.Trim();

                    if (tline.StartsWith("[") && tline.EndsWith("]"))
                    {
                        var sectionName = tline.Substring(1, tline.Length - 2);
                        currentSection = new GameConfigurationSection() { Name = sectionName };
                        this.Sections.Add(currentSection);
                    }
                    else if (tline.StartsWith(";"))
                    {
                        currentSection.Comments.Add(line);
                    }
                    else if (!string.IsNullOrEmpty(tline) && tline.Contains("="))
                    {
                        var tokens = tline.Split(new char[] { '=' }, 2);

                        var key = tokens[0];
                        var value = tokens[1];

                        currentSection.Items.Add(new GameConfigurationItem() { Key = key, Value = value });

                    }
                }
            }
        }

        public void Save()
        {
            File.WriteAllText(this.FileName, this.ToString());
        }

        public GameConfigurationSection FindSection(string sectionName)
        {
            return this.Sections.Where(x => x.Name == sectionName).FirstOrDefault();
        }

        public GameConfigurationItem FindItem(string sectionName, string key)
        {
            var configSection = this.FindSection(sectionName);

            if (configSection != null)
            {
                return configSection.Items.Where(x => x.Key == key).FirstOrDefault();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Ensures the specified key and value exist under the desired section. 
        /// If the key-value pair does not yet exists, it will be added. If the desired
        /// section doesn't yet exist it will be added.
        /// </summary>
        /// <param name="sectionName">Name of the section the key-value pair will reside in.</param>
        /// <param name="key">Name of the key.</param>
        /// <param name="value">The value in the key-value pair.</param>
        public void Include(string sectionName, string key, string value)
        {
            var section = this.Sections.FirstOrDefault(x => x.Name.Equals(sectionName));

            if(section == null)
            {
                section = new GameConfigurationSection() { Name = sectionName };
                this.Sections.Add(section);
            }

            if (section.Items.FirstOrDefault(x => x.Key == key && x.Value == value) == null)
            {
                section.Items.Add(new GameConfigurationItem() { Key = key, Value = value });
            }

        }


        /// <summary>
        /// Ensures the specified key and value do not exist in the specified section.
        /// If the key-value pair exists, it will be removed.
        /// </summary>
        /// <param name="sectionName"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Exclude(string sectionName, string key, string value)
        {
            var section = this.Sections.FirstOrDefault(x => x.Name.Equals(sectionName));

            if (section != null)
            {
                var item = section.Items.FirstOrDefault(x => x.Key == key && x.Value == value);

                if(item != null)
                {
                    section.Items.Remove(item);
                }
            }
        }

        public override string ToString()
        {
            var result = new StringBuilder();

            foreach(var section in this.Sections)
            {
                result.AppendLine();
                result.AppendLine(string.Format("{0}{1}{2}", "[", section.Name, "]"));

                foreach(var item in section.Items)
                {
                    result.AppendLine(string.Format("{0}={1}", item.Key, item.Value));
                }

                foreach(var comment in section.Comments)
                {
                    result.AppendLine(comment);
                }
            }

            return result.ToString();
        }

    }
}
