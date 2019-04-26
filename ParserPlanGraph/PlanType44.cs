using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
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
            var xml = GetXml(file.ToString());
            var root = (JObject) p.SelectToken("export");
            var firstOrDefault = root.Properties().FirstOrDefault(pr => pr.Name.Contains("tender"));
            if (firstOrDefault != null)
            {
                var plan = firstOrDefault.Value;
                var id_xml = ((string) plan.SelectToken("id") ?? "").Trim();

                if (String.IsNullOrEmpty(id_xml))
                {
                    Log.Logger("У плана нет id", file_path);
                    return;
                }
                var planNumber = ((string) plan.SelectToken("planNumber") ?? "").Trim();
                var versionNumber = ((string) plan.SelectToken("versionNumber") ?? "").Trim();
                if (String.IsNullOrEmpty(planNumber))
                {
                    Log.Logger("У плана нет planNumber", file_path);
                }

                using (var connect = ConnectToDb.GetDBConnection())
                {
                    connect.Open();
                    var select_plan =
                        $"SELECT id FROM {Program.Prefix}tender_plan WHERE id_xml = @id_xml AND id_region = @id_region AND plan_number = @plan_number AND num_version = @num_version";
                    var cmd = new MySqlCommand(select_plan, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id_xml", id_xml);
                    cmd.Parameters.AddWithValue("@id_region", region_id);
                    cmd.Parameters.AddWithValue("@plan_number", planNumber);
                    cmd.Parameters.AddWithValue("@num_version", versionNumber);
                    var reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Close();
                        Log.Logger("Такой план уже есть в базе", file_path, id_xml, planNumber);
                        //return;
                    }
                    reader.Close();
                    var purchasePlanNumber =
                        ((string) plan.SelectToken("commonInfo.purchasePlanNumber") ?? "").Trim();
                    var year = ((string) plan.SelectToken("commonInfo.year") ?? "").Trim();
                    var createDate = (JsonConvert.SerializeObject(plan.SelectToken("commonInfo.createDate") ?? "") ??
                                         "").Trim('"');
                    var confirmDate =
                    (JsonConvert.SerializeObject(plan.SelectToken("commonInfo.confirmDate") ?? "") ??
                     "").Trim('"');
                    var publishDate =
                    (JsonConvert.SerializeObject(plan.SelectToken("commonInfo.publishDate") ?? "") ??
                     "").Trim('"');
                    var printform = ((string) plan.SelectToken("printForm.url") ?? "").Trim();
                    var cancel_status = 0;
                    if (!String.IsNullOrEmpty(publishDate))
                    {
                        var select_date_p =
                            $"SELECT id, create_date FROM {Program.Prefix}tender_plan WHERE id_region = @id_region AND plan_number = @plan_number";
                        var cmd2 = new MySqlCommand(select_date_p, connect);
                        cmd2.Prepare();
                        cmd2.Parameters.AddWithValue("@id_region", region_id);
                        cmd2.Parameters.AddWithValue("@plan_number", planNumber);
                        var dt = new DataTable();
                        var adapter = new MySqlDataAdapter {SelectCommand = cmd2};
                        adapter.Fill(dt);
                        if (dt.Rows.Count > 0)
                        {
                            foreach (DataRow row in dt.Rows)
                            {
                                var date_new = DateTime.Parse(createDate);
                                var date_old = (DateTime) row["create_date"];
                                if (date_new > date_old)
                                {
                                    var update_plan_cancel =
                                        $"UPDATE {Program.Prefix}tender_plan SET cancel = 1 WHERE id = @id";
                                    var cmd3 = new MySqlCommand(update_plan_cancel, connect);
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
                    var id_customer = 0;
                    var id_owner = 0;
                    var customer_reg_num =
                        ((string) plan.SelectToken("commonInfo.customerInfo.regNum") ?? "").Trim();

                    if (!String.IsNullOrEmpty(customer_reg_num))
                    {
                        var select_cust = $"SELECT id FROM od_customer WHERE regNumber = @regNumber";
                        var cmd4 = new MySqlCommand(select_cust, connect);
                        cmd4.Prepare();
                        cmd4.Parameters.AddWithValue("@regNumber", customer_reg_num);
                        var reader2 = cmd4.ExecuteReader();
                        if (reader2.HasRows)
                        {
                            reader2.Read();
                            id_customer = reader2.GetInt32("id");
                            reader2.Close();
                        }
                        else
                        {
                            reader2.Close();
                            var cus_full_name = ((string) plan.SelectToken("commonInfo.customerInfo.fullName") ?? "")
                                .Trim();
                            var cus_inn = ((string) plan.SelectToken("commonInfo.customerInfo.INN") ?? "").Trim();
                            var cus_kpp = ((string) plan.SelectToken("commonInfo.customerInfo.KPP") ?? "").Trim();
                            var cus_phone =
                                ((string) plan.SelectToken("commonInfo.customerInfo.phone") ?? "").Trim();
                            var cus_email =
                                ((string) plan.SelectToken("commonInfo.customerInfo.email") ?? "").Trim();
                            var cus_last_name =
                                ((string) plan.SelectToken("commonInfo.responsibleContactInfo.lastName") ?? "").Trim();
                            var cus_first_name =
                                ((string) plan.SelectToken("commonInfo.responsibleContactInfo.firstName") ?? "").Trim();
                            var cus_middle_name =
                                ((string) plan.SelectToken("commonInfo.responsibleContactInfo.middleName") ?? "")
                                .Trim();
                            var cus_contact_name = $"{cus_last_name} {cus_first_name} {cus_middle_name}".Trim();
                            var insert_customer =
                                $"INSERT INTO od_customer SET regNumber = @regNumber, inn = @inn, kpp = @kpp, full_name = @full_name, phone = @phone, email = @email, contact_name = @contact_name";
                            var cmd5 = new MySqlCommand(insert_customer, connect);
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
                        //Log.Logger("Нет customer_reg_num", file_path);
                    }
                    var owner_reg_num = ((string) plan.SelectToken("commonInfo.ownerInfo.regNum") ?? "").Trim();
                    if (!String.IsNullOrEmpty(owner_reg_num))
                    {
                        var select_owner = $"SELECT id FROM od_customer WHERE regNumber = @regNumber";
                        var cmd6 = new MySqlCommand(select_owner, connect);
                        cmd6.Prepare();
                        cmd6.Parameters.AddWithValue("@regNumber", owner_reg_num);
                        var reader3 = cmd6.ExecuteReader();
                        if (reader3.HasRows)
                        {
                            reader3.Read();
                            id_owner = reader3.GetInt32("id");
                            reader3.Close();
                        }
                        else
                        {
                            reader3.Close();
                            var owner_full_name = ((string) plan.SelectToken("commonInfo.ownerInfo.fullName") ?? "")
                                .Trim();
                            var owner_inn = ((string) plan.SelectToken("commonInfo.ownerInfo.INN") ?? "")
                                .Trim();
                            var owner_kpp = ((string) plan.SelectToken("commonInfo.ownerInfo.KPP") ?? "")
                                .Trim();
                            var owner_phone = ((string) plan.SelectToken("commonInfo.ownerInfo.phone") ?? "")
                                .Trim();
                            var owner_email = ((string) plan.SelectToken("commonInfo.ownerInfo.email") ?? "")
                                .Trim();
                            var owner_first_name =
                                ((string) plan.SelectToken("commonInfo.confirmContactInfo.firstName") ?? "")
                                .Trim();
                            var owner_last_name =
                                ((string) plan.SelectToken("commonInfo.confirmContactInfo.lastName") ?? "")
                                .Trim();
                            var owner_middle_name =
                                ((string) plan.SelectToken("commonInfo.confirmContactInfo.middleName") ?? "").Trim();
                            var owner_contact_name =
                                $"{owner_last_name} {owner_first_name} {owner_middle_name}".Trim();
                            var insert_owner =
                                $"INSERT INTO od_customer SET regNumber = @regNumber, inn = @inn, kpp = @kpp, full_name = @full_name, phone = @phone, email = @email, contact_name = @contact_name";
                            var cmd7 = new MySqlCommand(insert_owner, connect);
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
                        //Log.Logger("Нет owner_reg_num", file_path);
                    }
                    var sum_pushases_small_business_total =
                        ((string) plan.SelectToken("totals.outcomeIndicators.sumPushasesSmallBusiness.total") ?? "")
                        .Trim();
                    var sum_pushases_small_business_current_year =
                        ((string) plan.SelectToken("totals.outcomeIndicators.sumPushasesSmallBusiness.currentYear") ??
                         "")
                        .Trim();
                    var sum_pushases_request_total =
                        ((string) plan.SelectToken("totals.outcomeIndicators.sumPushasesRequest.total") ?? "")
                        .Trim();
                    var sum_pushases_request_current_year =
                        ((string) plan.SelectToken("totals.outcomeIndicators.sumPushasesRequest.currentYear") ?? "")
                        .Trim();
                    var finance_support_total =
                        ((string) plan.SelectToken("totals.financeSupport.financeSupportTotal.total") ?? "")
                        .Trim();
                    var finance_support_current_year =
                        ((string) plan.SelectToken("totals.financeSupport.financeSupportTotal.currentYear") ?? "")
                        .Trim();
                    var insert_plan =
                        $"INSERT INTO {Program.Prefix}tender_plan SET id_xml = @id_xml, plan_number = @plan_number, num_version = @num_version, id_region = @id_region, purchase_plan_number = @purchase_plan_number, year = @year, create_date = @create_date, confirm_date = @confirm_date, publish_date = @publish_date, id_customer = @id_customer, id_owner = @id_owner, print_form = @print_form, cancel = @cancel, sum_pushases_small_business_total = @sum_pushases_small_business_total, sum_pushases_small_business_current_year = @sum_pushases_small_business_current_year, sum_pushases_request_total = @sum_pushases_request_total, sum_pushases_request_current_year = @sum_pushases_request_current_year, finance_support_total = @finance_support_total, finance_support_current_year = @finance_support_current_year, xml = @xml";
                    var cmd8 = new MySqlCommand(insert_plan, connect);
                    cmd8.Prepare();
                    cmd8.Parameters.AddWithValue("@id_xml", id_xml);
                    cmd8.Parameters.AddWithValue("@plan_number", planNumber);
                    cmd8.Parameters.AddWithValue("@num_version", versionNumber);
                    cmd8.Parameters.AddWithValue("@id_region", region_id);
                    cmd8.Parameters.AddWithValue("@purchase_plan_number", purchasePlanNumber);
                    cmd8.Parameters.AddWithValue("@year", year);
                    cmd8.Parameters.AddWithValue("@create_date", createDate);
                    cmd8.Parameters.AddWithValue("@confirm_date", confirmDate);
                    cmd8.Parameters.AddWithValue("@publish_date", publishDate);
                    cmd8.Parameters.AddWithValue("@id_customer", id_customer);
                    cmd8.Parameters.AddWithValue("@id_owner", id_owner);
                    cmd8.Parameters.AddWithValue("@print_form", printform);
                    cmd8.Parameters.AddWithValue("@cancel", cancel_status);
                    cmd8.Parameters.AddWithValue("@sum_pushases_small_business_total",
                        sum_pushases_small_business_total);
                    cmd8.Parameters.AddWithValue("@sum_pushases_small_business_current_year",
                        sum_pushases_small_business_current_year);
                    cmd8.Parameters.AddWithValue("@sum_pushases_request_total", sum_pushases_request_total);
                    cmd8.Parameters.AddWithValue("@sum_pushases_request_current_year",
                        sum_pushases_request_current_year);
                    cmd8.Parameters.AddWithValue("@finance_support_total", finance_support_total);
                    cmd8.Parameters.AddWithValue("@finance_support_current_year", finance_support_current_year);
                    cmd8.Parameters.AddWithValue("@xml", xml);
                    var res_plan = cmd8.ExecuteNonQuery();
                    var id_plan = (int) cmd8.LastInsertedId;
                    AddPlan44?.Invoke(res_plan);
                    var positions = GetElements(plan, "positions.position");
                    foreach (var pos in positions)
                    {
                        var position_number = ((string) pos.SelectToken("commonInfo.positionNumber") ?? "")
                            .Trim();
                        var purchase_plan_position_number =
                            ((string) pos.SelectToken("commonInfo.purchasePlanPositionInfo.positionNumber") ?? "")
                            .Trim();
                        var purchase_object_name =
                            ((string) pos.SelectToken("commonInfo.positionInfo.purchaseObjectName") ?? "")
                            .Trim();
                        var start_month =
                            ((string) pos.SelectToken("commonInfo.positionInfo.placingNotificationTerm.month") ?? "")
                            .Trim();
                        var end_month =
                            ((string) pos.SelectToken("commonInfo.positionInfo.endContratProcedureTerm.month") ?? "")
                            .Trim();
                        var id_placing_way = 0;
                        var placingWay_code =
                            ((string) pos.SelectToken("commonInfo.placingWayInfo.placingWay.code") ?? "").Trim();
                        var placingWay_name =
                            ((string) pos.SelectToken("commonInfo.placingWayInfo.placingWay.name") ?? "").Trim();
                        if (!String.IsNullOrEmpty(placingWay_code))
                        {
                            var select_placing_way =
                                $"SELECT id_placing_way FROM {Program.Prefix}tender_plan_placing_way WHERE code = @code";
                            var cmd9 = new MySqlCommand(select_placing_way, connect);
                            cmd9.Prepare();
                            cmd9.Parameters.AddWithValue("@code", placingWay_code);
                            var reader4 = cmd9.ExecuteReader();
                            if (reader4.HasRows)
                            {
                                reader4.Read();
                                id_placing_way = reader4.GetInt32("id_placing_way");
                                reader4.Close();
                            }
                            else
                            {
                                reader4.Close();
                                var insert_placing_way =
                                    $"INSERT INTO {Program.Prefix}tender_plan_placing_way SET code= @code, name= @name";
                                var cmd10 = new MySqlCommand(insert_placing_way, connect);
                                cmd10.Prepare();
                                cmd10.Parameters.AddWithValue("@code", placingWay_code);
                                cmd10.Parameters.AddWithValue("@name", placingWay_name);
                                cmd10.ExecuteNonQuery();
                                id_placing_way = (int) cmd10.LastInsertedId;
                                //Log.Logger("Добавлен новый placing_way", file_path, id_placing_way);
                            }
                        }
                        var finance_total =
                            ((string) pos.SelectToken("commonInfo.financeInfo.planPayments.total") ?? "").Trim();
                        var finance_total_current_year =
                            ((string) pos.SelectToken("commonInfo.financeInfo.planPayments.currentYear") ?? "").Trim();
                        var max_price = ((string) pos.SelectToken("commonInfo.financeInfo.maxPrice") ?? "").Trim();
                        var OKPD2_code = ((string) pos.SelectToken("purchaseObjectInfo.OKPD2Info.OKPD2.code") ?? "").Trim();
                        var OKPD2_name = ((string) pos.SelectToken("purchaseObjectInfo.OKPD2Info.OKPD2.name") ?? "").Trim();
                        var OKEI_code = ((string) pos.SelectToken("purchaseObjectInfo.OKEI.code") ?? "").Trim();
                        var OKEI_name = ((string) pos.SelectToken("purchaseObjectInfo.OKEI.name") ?? "").Trim();
                        var pos_description = ((string) pos.SelectToken("purchaseObjectInfo.objectDescription") ?? "").Trim();
                        var products_quantity_total = ((string) pos.SelectToken("purchaseObjectInfo.productsQuantityInfo.total") ?? "").Trim();
                        var products_quantity_current_year = ((string) pos.SelectToken("purchaseObjectInfo.productsQuantityInfo.currentYear") ?? "").Trim();
                        var purchase_fin_condition = ((string) pos.SelectToken("purchaseConditions.purchaseFinCondition.amount") ?? "").Trim();
                        var contract_fin_condition = ((string) pos.SelectToken("purchaseConditions.contractFinCondition.amount") ?? "").Trim();
                        var advance_fin_condition = ((string) pos.SelectToken("purchaseConditions.advanceFinCondition.amount") ?? "").Trim();
                        var purchase_graph = ((string) pos.SelectToken("purchaseConditions.purchaseGraph.plannedPeriod") ?? "").Trim();
                        if (String.IsNullOrEmpty(purchase_graph))
                        {
                            purchase_graph = ((string) pos.SelectToken("purchaseConditions.purchaseGraph.periodicity.periodicityType") ?? "").Trim();
                        }
                        if (String.IsNullOrEmpty(purchase_graph))
                        {
                            purchase_graph = ((string) pos.SelectToken("purchaseConditions.purchaseGraph.periodicity.otherPeriodicityText") ?? "").Trim();
                        }
                        var bank_support_info = ((string) pos.SelectToken("purchaseConditions.bankSupportInfo.bankSupportText") ?? "").Trim();

                        var insert_position = $"INSERT INTO {Program.Prefix}tender_plan_position SET id_plan = @id_plan, position_number = @position_number, purchase_plan_position_number = @purchase_plan_position_number, purchase_object_name = @purchase_object_name, start_month = @start_month, end_month = @end_month, id_placing_way = @id_placing_way, finance_total = @finance_total, finance_total_current_year = @finance_total_current_year, max_price = @max_price, OKPD2_code = @OKPD2_code, OKPD2_name = @OKPD2_name, OKEI_code = @OKEI_code, OKEI_name = @OKEI_name, pos_description = @pos_description, products_quantity_total = @products_quantity_total, products_quantity_current_year = @products_quantity_current_year, purchase_fin_condition = @purchase_fin_condition, contract_fin_condition = @contract_fin_condition, advance_fin_condition = @advance_fin_condition, purchase_graph = @purchase_graph, bank_support_info = @bank_support_info";
                        var cmd11 = new MySqlCommand(insert_position, connect);
                        cmd11.Prepare();
                        cmd11.Parameters.AddWithValue("@id_plan", id_plan);
                        cmd11.Parameters.AddWithValue("@position_number", position_number);
                        cmd11.Parameters.AddWithValue("@purchase_plan_position_number", purchase_plan_position_number);
                        cmd11.Parameters.AddWithValue("@purchase_object_name", purchase_object_name);
                        cmd11.Parameters.AddWithValue("@start_month", start_month);
                        cmd11.Parameters.AddWithValue("@end_month", end_month);
                        cmd11.Parameters.AddWithValue("@id_placing_way", id_placing_way);
                        cmd11.Parameters.AddWithValue("@finance_total", finance_total);
                        cmd11.Parameters.AddWithValue("@finance_total_current_year", finance_total_current_year);
                        cmd11.Parameters.AddWithValue("@max_price", max_price);
                        cmd11.Parameters.AddWithValue("@OKPD2_code", OKPD2_code);
                        cmd11.Parameters.AddWithValue("@OKPD2_name", OKPD2_name);
                        cmd11.Parameters.AddWithValue("@OKEI_code", OKEI_code);
                        cmd11.Parameters.AddWithValue("@OKEI_name", OKEI_name);
                        cmd11.Parameters.AddWithValue("@pos_description", pos_description);
                        cmd11.Parameters.AddWithValue("@products_quantity_total", products_quantity_total);
                        cmd11.Parameters.AddWithValue("@products_quantity_current_year", products_quantity_current_year);
                        cmd11.Parameters.AddWithValue("@purchase_fin_condition", purchase_fin_condition);
                        cmd11.Parameters.AddWithValue("@contract_fin_condition", contract_fin_condition);
                        cmd11.Parameters.AddWithValue("@advance_fin_condition", advance_fin_condition);
                        cmd11.Parameters.AddWithValue("@purchase_graph", purchase_graph);
                        cmd11.Parameters.AddWithValue("@bank_support_info", bank_support_info);
                        cmd11.ExecuteNonQuery();
                        var id_prod = (int) cmd11.LastInsertedId;
                        var rec_pref = GetElements(pos, "purchaseConditions.preferensesRequirements.preferenseRequirement");
                        foreach (var pr in rec_pref)
                        {
                            var group_code = ((string) pr.SelectToken("prefsReqsGroup.code") ?? "").Trim();
                            var group_name = ((string) pr.SelectToken("prefsReqsGroup.name") ?? "").Trim();
                            var name = ((string) pr.SelectToken("name") ?? "").Trim();
                            var add_info = ((string) pr.SelectToken("addInfo") ?? "").Trim();
                            var insert_pref_rec = $"INSERT INTO {Program.Prefix}tender_plan_pref_rec SET id_plan_prod = @id_plan_prod, group_code = @group_code, group_name = @group_name, name = @name, add_info = @add_info";
                            var cmd12 = new MySqlCommand(insert_pref_rec, connect);
                            cmd12.Prepare();
                            cmd12.Parameters.AddWithValue("@id_plan_prod", id_prod);
                            cmd12.Parameters.AddWithValue("@group_code", group_code);
                            cmd12.Parameters.AddWithValue("@group_name", group_name);
                            cmd12.Parameters.AddWithValue("@name", name);
                            cmd12.Parameters.AddWithValue("@add_info", add_info);
                            cmd12.ExecuteNonQuery();
                        }

                    }
                    var attach = GetElements(plan, "attachments.attachment");
                    foreach (var att in attach)
                    {
                        var file_name = ((string) att.SelectToken("fileName") ?? "").Trim();
                        var desc = ((string) att.SelectToken("docDescription") ?? "").Trim();
                        var url = ((string) att.SelectToken("url") ?? "").Trim();
                        var insert_attach = $"INSERT INTO {Program.Prefix}tender_plan_attach SET id_plan = @id_plan, file_name = @file_name, description = @description, url = @url";
                        var cmd13 = new MySqlCommand(insert_attach, connect);
                        cmd13.Prepare();
                        cmd13.Parameters.AddWithValue("@id_plan", id_plan);
                        cmd13.Parameters.AddWithValue("@file_name", file_name);
                        cmd13.Parameters.AddWithValue("@description", desc);
                        cmd13.Parameters.AddWithValue("@url", url);
                        cmd13.ExecuteNonQuery();
                    }
                    
                }
            }
            else
            {
                Log.Logger("Не могу найти тег Plan44", file_path);
            }
        }
    }
}