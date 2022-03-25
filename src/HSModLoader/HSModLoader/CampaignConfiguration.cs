using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HSModLoader
{
    public class CampaignConfiguration
    {

        public string FileName { get; set; }
        public List<Campaign> Campaigns { get; set; }

        private GameConfiguration GameConfiguration { get; set; }

        public CampaignConfiguration(string filename)
        {
            this.FileName = filename;
            this.Campaigns = new List<Campaign>();
        }

        public void Load()
        {
            this.Campaigns.Clear();

            this.GameConfiguration = new GameConfiguration(this.FileName);
            this.GameConfiguration.Load();

            var section = this.GameConfiguration.Sections.FirstOrDefault(x => x.Name == "RPGTacCampaignLoader");

            if(section != null)
            {
                foreach(var pair in section.Items)
                {
                    this.Campaigns.Add(new Campaign(pair.Value));
                }
            }
        }

        public void Save()
        {
            var section = this.GameConfiguration.Sections.FirstOrDefault(x => x.Name == "RPGTacCampaignLoader");

            if (section != null)
            {
                section.Items.Clear();

                for(int i = 0; i < this.Campaigns.Count; i++)
                {
                    var item = new GameConfigurationItem()
                    {
                        Key = String.Format("CampaignList[{0}]", i),
                        Value = this.Campaigns[i].ToString()
                    };

                    section.Items.Add(item);
                }
            }

            this.GameConfiguration.Save();
        }

    }
}
