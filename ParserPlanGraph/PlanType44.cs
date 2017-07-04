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

        public override void Parsing()
        {
            string xml = GetXml(file.ToString());
            JObject root = (JObject) p.SelectToken("export");
            JProperty firstOrDefault = root.Properties().FirstOrDefault(pr => pr.Name.Contains("tender"));
            if (firstOrDefault != null)
            {
                JToken plan = firstOrDefault.Value;
                string id_xml = ((string) plan.SelectToken("id") ?? "").Trim();
                
                if (String.IsNullOrEmpty(id_xml))
                {
                    Log.Logger("У плана нет id", file_path);
                    return;
                }
                string planNumber = ((string) plan.SelectToken("planNumber") ?? "").Trim();
                if (String.IsNullOrEmpty(planNumber))
                {
                    Log.Logger("У плана нет planNumber", file_path);
                }

                using (MySqlConnection connect = ConnectToDb.GetDBConnection())
                {
                    connect.Open();
                    string select_plan = $"SELECT id FROM {Program.Prefix}tender_plan WHERE id_xml = @id_xml AND id_region = @id_region AND plan_number = @plan_number";
                    MySqlCommand cmd = new MySqlCommand(select_plan, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id_xml", id_xml);
                    cmd.Parameters.AddWithValue("@id_region", region_id);
                    cmd.Parameters.AddWithValue("@plan_number", planNumber);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Close();
                        Log.Logger("Такой план уже есть в базе", file_path);
                        return;
                    }
                    reader.Close();
                    string purchasePlanNumber = ((string) plan.SelectToken("commonInfo.purchasePlanNumber") ?? "").Trim();
                    string year = ((string) plan.SelectToken("commonInfo.year") ?? "").Trim();
                    string createDate = (JsonConvert.SerializeObject(plan.SelectToken("commonInfo.createDate") ?? "") ??
                                         "").Trim('"');
                    string confirmDate = (JsonConvert.SerializeObject(plan.SelectToken("commonInfo.confirmDate") ?? "") ??
                                         "").Trim('"');
                    string publishDate = (JsonConvert.SerializeObject(plan.SelectToken("commonInfo.publishDate") ?? "") ??
                                          "").Trim('"');
                    string printform = ((string) plan.SelectToken("printForm.url") ?? "").Trim();
                    int cancel_status = 0;
                    if (!String.IsNullOrEmpty(publishDate))
                    {
                        string select_date_p =
                            $"SELECT id, publish_date FROM {Program.Prefix}tender_plan WHERE id_region = @id_region AND plan_number = @plan_number";
                        MySqlCommand cmd2 = new MySqlCommand(select_date_p, connect);
                        cmd2.Prepare();
                        cmd2.Parameters.AddWithValue("@id_region", region_id);
                        cmd2.Parameters.AddWithValue("@plan_number", planNumber);
                        DataTable dt = new DataTable();
                        MySqlDataAdapter adapter = new MySqlDataAdapter {SelectCommand = cmd2};
                        adapter.Fill(dt);
                        if (dt.Rows.Count > 0)
                        {
                            foreach (DataRow row in dt.Rows)
                            {
                                DateTime date_new = DateTime.Parse(publishDate);
                                DateTime date_old = (DateTime) row["publish_date"];
                                if (date_new > date_old)
                                {
                                    string update_plan_cancel =
                                        $"UPDATE {Program.Prefix}tender_plan SET cancel = 1 WHERE id = @id";
                                    MySqlCommand cmd3 = new MySqlCommand(update_plan_cancel, connect);
                                    cmd3.Prepare();
                                    cmd3.Parameters.AddWithValue("id", (int) row["id"]);
                                    cmd3.ExecuteNonQuery();
                                }
                                else
                                {
                                    cancel_status = 1;
                                }
                            }
                        }
                    }
                    int id_customer = 0;
                    int id_owner = 0;
                    

                }
            }
            else
            {
                Log.Logger("Не могу найти тег Plan44", file_path);
            }
        }
    }
}