using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace ParserPlanGraph
{
    public class PlanTypeChange44 : Plan
    {
        public PlanTypeChange44(FileInfo f, string region, int regionId, JObject json)
            : base(f, region, regionId, json)
        {
            AddPlanCange44 += delegate(int d)
            {
                if (d > 0)
                    Program.AddPlanChange44++;
                else
                    Log.Logger("Не удалось добавить Plan44", FilePath);
            };
        }

        public event Action<int> AddPlanCange44;

        public override void Parsing()
        {
            var xml = GetXml(File.ToString());
            Log.Logger("План change", FilePath, xml);
        }

        protected virtual void OnAddPlanCange44(int obj)
        {
            AddPlanCange44?.Invoke(obj);
        }
    }
}