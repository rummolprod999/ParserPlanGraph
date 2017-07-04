using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ParserPlanGraph
{
    public class PlanType44:Plan
    {
        public event Action<int> AddPlan44;
        public PlanType44(FileInfo f, string region, int region_id, JObject json)
            : base(f, region, region_id, json)
        {
            AddPlan44 += delegate(int d)
            {
                if (d > 0)
                    Program.AddPlan44++;
                else
                    Log.Logger("Не удалось добавить Plan44", file_path);
            };
        }
    }
}