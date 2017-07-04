﻿using System;
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
    }
}