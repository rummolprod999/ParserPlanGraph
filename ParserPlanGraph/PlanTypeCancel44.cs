using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace ParserPlanGraph
{
    public class PlanTypeCancel44:Plan
    {
        public event Action<int> AddPlanCancel44;
        public PlanTypeCancel44(FileInfo f, string region, int region_id, JObject json)
            : base(f, region, region_id, json)
        {
            AddPlanCancel44 += delegate(int d)
            {
                if (d > 0)
                    Program.AddPlanCancel44++;
                else
                    Log.Logger("Не удалось добавить PlanCancel44", file_path);
            };
        }

        public override void Parsing()
        {
            var xml = GetXml(file.ToString());
            Log.Logger("План cancel", file_path, xml);
        }
    }
}