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
    public class PlanType44 : Plan
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
                    string select_plan =
                        $"SELECT id FROM {Program.Prefix}tender_plan WHERE id_xml = @id_xml AND id_region = @id_region AND plan_number = @plan_number";
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
                    string purchasePlanNumber =
                        ((string) plan.SelectToken("commonInfo.purchasePlanNumber") ?? "").Trim();
                    string year = ((string) plan.SelectToken("commonInfo.year") ?? "").Trim();
                    string createDate = (JsonConvert.SerializeObject(plan.SelectToken("commonInfo.createDate") ?? "") ??
                                         "").Trim('"');
                    string confirmDate =
                    (JsonConvert.SerializeObject(plan.SelectToken("commonInfo.confirmDate") ?? "") ??
                     "").Trim('"');
                    string publishDate =
                    (JsonConvert.SerializeObject(plan.SelectToken("commonInfo.publishDate") ?? "") ??
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
                    string customer_reg_num =
                        ((string) plan.SelectToken("commonInfo.customerInfo.regNum") ?? "").Trim();

                    if (!String.IsNullOrEmpty(customer_reg_num))
                    {
                        string select_cust = $"SELECT id FROM od_customer WHERE regNumber = @regNumber";
                        MySqlCommand cmd4 = new MySqlCommand(select_cust, connect);
                        cmd4.Prepare();
                        cmd4.Parameters.AddWithValue("@regNumber", customer_reg_num);
                        MySqlDataReader reader2 = cmd4.ExecuteReader();
                        if (reader2.HasRows)
                        {
                            reader2.Read();
                            id_customer = reader2.GetInt32("id");
                            reader2.Close();
                        }
                        else
                        {
                            reader2.Close();
                            string cus_full_name = ((string) plan.SelectToken("commonInfo.customerInfo.fullName") ?? "")
                                .Trim();
                            string cus_inn = ((string) plan.SelectToken("commonInfo.customerInfo.INN") ?? "").Trim();
                            string cus_kpp = ((string) plan.SelectToken("commonInfo.customerInfo.KPP") ?? "").Trim();
                            string cus_phone =
                                ((string) plan.SelectToken("commonInfo.customerInfo.phone") ?? "").Trim();
                            string cus_email =
                                ((string) plan.SelectToken("commonInfo.customerInfo.email") ?? "").Trim();
                            string cus_last_name =
                                ((string) plan.SelectToken("commonInfo.responsibleContactInfo.lastName") ?? "").Trim();
                            string cus_first_name =
                                ((string) plan.SelectToken("commonInfo.responsibleContactInfo.firstName") ?? "").Trim();
                            string cus_middle_name =
                                ((string) plan.SelectToken("commonInfo.responsibleContactInfo.middleName") ?? "")
                                .Trim();
                            string cus_contact_name = $"{cus_last_name} {cus_first_name} {cus_middle_name}".Trim();
                            string insert_customer =
                                $"INSERT INTO od_customer SET regNumber = @regNumber, inn = @inn, kpp = @kpp, full_name = @full_name, phone = @phone, email = @email, contact_name = @contact_name";
                            MySqlCommand cmd5 = new MySqlCommand(insert_customer, connect);
                            cmd5.Prepare();
                            cmd5.Parameters.AddWithValue("@regNumber", customer_reg_num);
                            cmd5.Parameters.AddWithValue("@inn", cus_inn);
                            cmd5.Parameters.AddWithValue("@kpp", cus_kpp);
                            cmd5.Parameters.AddWithValue("@full_name", cus_full_name);
                            cmd5.Parameters.AddWithValue("@phone", cus_phone);
                            cmd5.Parameters.AddWithValue("@email", cus_email);
                            cmd5.Parameters.AddWithValue("@contact_name", cus_contact_name);
                            cmd5.ExecuteNonQuery();
                            id_customer = (int) cmd5.LastInsertedId;
                        }
                    }
                    else
                    {
                        Log.Logger("Нет customer_reg_num", file_path);
                    }
                    string owner_reg_num = ((string) plan.SelectToken("commonInfo.ownerInfo.regNum") ?? "").Trim();
                    if (!String.IsNullOrEmpty(owner_reg_num))
                    {
                        string select_owner = $"SELECT id FROM od_customer WHERE regNumber = @regNumber";
                        MySqlCommand cmd6 = new MySqlCommand(select_owner, connect);
                        cmd6.Prepare();
                        cmd6.Parameters.AddWithValue("@regNumber", owner_reg_num);
                        MySqlDataReader reader3 = cmd6.ExecuteReader();
                        if (reader3.HasRows)
                        {
                            reader3.Read();
                            id_owner = reader3.GetInt32("id");
                            reader3.Close();
                        }
                        else
                        {
                            reader3.Close();
                            string owner_full_name = ((string) plan.SelectToken("commonInfo.ownerInfo.fullName") ?? "")
                                .Trim();
                            string owner_inn = ((string) plan.SelectToken("commonInfo.ownerInfo.INN") ?? "")
                                .Trim();
                            string owner_kpp = ((string) plan.SelectToken("commonInfo.ownerInfo.KPP") ?? "")
                                .Trim();
                            string owner_phone = ((string) plan.SelectToken("commonInfo.ownerInfo.phone") ?? "")
                                .Trim();
                            string owner_email = ((string) plan.SelectToken("commonInfo.ownerInfo.email") ?? "")
                                .Trim();
                            string owner_first_name =
                                ((string) plan.SelectToken("commonInfo.confirmContactInfo.firstName") ?? "")
                                .Trim();
                            string owner_last_name =
                                ((string) plan.SelectToken("commonInfo.confirmContactInfo.lastName") ?? "")
                                .Trim();
                            string owner_middle_name =
                                ((string) plan.SelectToken("commonInfo.confirmContactInfo.middleName") ?? "").Trim();
                            string owner_contact_name =
                                $"{owner_last_name} {owner_first_name} {owner_middle_name}".Trim();
                            string insert_owner =
                                $"INSERT INTO od_customer SET regNumber = @regNumber, inn = @inn, kpp = @kpp, full_name = @full_name, phone = @phone, email = @email, contact_name = @contact_name";
                            MySqlCommand cmd7 = new MySqlCommand(insert_owner, connect);
                            cmd7.Prepare();
                            cmd7.Parameters.AddWithValue("@regNumber", customer_reg_num);
                            cmd7.Parameters.AddWithValue("@inn", owner_inn);
                            cmd7.Parameters.AddWithValue("@kpp", owner_kpp);
                            cmd7.Parameters.AddWithValue("@full_name", owner_full_name);
                            cmd7.Parameters.AddWithValue("@phone", owner_phone);
                            cmd7.Parameters.AddWithValue("@email", owner_email);
                            cmd7.Parameters.AddWithValue("@contact_name", owner_contact_name);
                            cmd7.ExecuteNonQuery();
                            id_owner = (int) cmd7.LastInsertedId;

                        }
                    }
                    else
                    {
                        Log.Logger("Нет owner_reg_num", file_path);
                    }
                    Console.WriteLine(id_customer);
                    Console.WriteLine(id_owner);
                }
            }
            else
            {
                Log.Logger("Не могу найти тег Plan44", file_path);
            }
        }
    }
}