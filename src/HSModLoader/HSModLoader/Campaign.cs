﻿using System;
using System.Collections.Generic;
using System.Text;

namespace HSModLoader
{
    public class Campaign
    {
        public string Name { get; set; }
        public string Prefix { get; set; }
        public string Description { get; set; }
        public string BaseLevel { get; set; }
        public string GameType { get; set; }

        public Campaign() { }
        public Campaign(string payload)
        {

            if(!payload.StartsWith("(") || !payload.EndsWith(")"))
            {
                throw new InvalidOperationException("Campaign payload was not in expected format.");
            }

            var tokens = payload.Substring(1, payload.Length - 2).Split(new string[]{ "\"," }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in tokens)
            {
                var pair = token.Split('=');
                var key = pair[0];
                var value = pair[1].Replace("\"","");

                if (key == "CampaignName")
                {
                    this.Name = value;
                }
                else if (key == "CampaignPrefix")
                {
                    this.Prefix = value;
                }
                else if (key == "CampaignDescription")
                {
                    this.Description = value;
                }
                else if (key == "CampaignBaseLevel")
                {
                    this.BaseLevel = value;
                }
                else if (key == "CampaignGameType")
                {
                    this.GameType = value;
                }

            }
        }
        public override string ToString()
        {
            var result = new StringBuilder();

            result.Append("(");
            result.Append(string.Format("CampaignName=\"{0}\",", Name));
            result.Append(string.Format("CampaignPrefix=\"{0}\",", Prefix));
            result.Append(string.Format("CampaignDescription=\"{0}\",", Description));
            result.Append(string.Format("CampaignBaseLevel=\"{0}\",", BaseLevel));
            result.Append(string.Format("CampaignGameType=\"{0}\"", GameType));
            result.Append(")");

            return result.ToString();
        }

    }
}