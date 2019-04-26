using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace ParserPlanGraph
{
    public class Plan
    {
        protected readonly FileInfo File;
        protected readonly string FilePath;
        protected readonly JObject P;
        protected readonly string Region;
        protected readonly int RegionId;

        public Plan(FileInfo f, string region, int regionId, JObject json)
        {
            P = json;
            File = f;
            Region = region;
            RegionId = regionId;
            FilePath = File.ToString();
        }

        public virtual void Parsing()
        {
        }

        public string GetXml(string xml)
        {
            var xmlt = xml.Split('/');
            var t = xmlt.Length;
            if (t >= 2)
            {
                var sxml = xmlt[t - 2] + "/" + xmlt[t - 1];
                return sxml;
            }

            return "";
        }

        public List<JToken> GetElements(JToken j, string s)
        {
            var els = new List<JToken>();
            var elsObj = j.SelectToken(s);
            if (elsObj != null && elsObj.Type != JTokenType.Null)
            {
                switch (elsObj.Type)
                {
                    case JTokenType.Object:
                        els.Add(elsObj);
                        break;
                    case JTokenType.Array:
                        els.AddRange(elsObj);
                        break;
                }
            }

            return els;
        }
    }
}