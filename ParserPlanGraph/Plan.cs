using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace ParserPlanGraph
{
    public class Plan
    {
        protected readonly JObject p;
        protected readonly FileInfo file;
        protected readonly string region;
        protected readonly int region_id;
        protected readonly string file_path;

        public Plan(FileInfo f, string region, int region_id, JObject json)
        {
            p = json;
            file = f;
            this.region = region;
            this.region_id = region_id;
            file_path = file.ToString();
        }
        
        public virtual void Parsing()
        {
        }

        public string GetXml(string xml)
        {
            string[] xmlt = xml.Split('/');
            int t = xmlt.Length;
            if (t >= 2)
            {
                string sxml = xmlt[t - 2] + "/" + xmlt[t - 1];
                return sxml;
            }

            return "";
        }
        public List<JToken> GetElements(JToken j, string s)
        {
            List<JToken> els = new List<JToken>();
            var els_obj = j.SelectToken(s);
            if (els_obj != null && els_obj.Type != JTokenType.Null)
            {
                switch (els_obj.Type)
                {
                    case JTokenType.Object:
                        els.Add(els_obj);
                        break;
                    case JTokenType.Array:
                        els.AddRange(els_obj);
                        break;
                }
            }

            return els;
        }

    }
}