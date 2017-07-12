using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace ParserPlanGraph
{
    public class PlanTypeChange44:Plan
    {
        public event Action<int> AddPlanCange44;
        public PlanTypeChange44(FileInfo f, string region, int region_id, JObject json)
            : base(f, region, region_id, json)
        {
            AddPlanCange44 += delegate(int d)
            {
                if (d > 0)
                    Program.AddPlanChange44++;
                else
                    Log.Logger("Не удалось добавить Plan44", file_path);
            };
        }
        
        public override void Parsing()
        {
            string xml = GetXml(file.ToString());
            Log.Logger("План change", file_path, xml);
        }
    }
}