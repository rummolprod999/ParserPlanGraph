using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace ParserPlanGraph
{
    public class PlanTypeCancel44 : Plan
    {
        public PlanTypeCancel44(FileInfo f, string region, int regionId, JObject json)
            : base(f, region, regionId, json)
        {
            AddPlanCancel44 += delegate(int d)
            {
                if (d > 0)
                    Program.AddPlanCancel44++;
                else
                    Log.Logger("Не удалось добавить PlanCancel44", FilePath);
            };
        }

        public event Action<int> AddPlanCancel44;

        public override void Parsing()
        {
            var xml = GetXml(File.ToString());
            Log.Logger("План cancel", FilePath, xml);
        }

        protected virtual void OnAddPlanCancel44(int obj)
        {
            AddPlanCancel44?.Invoke(obj);
        }
    }
}