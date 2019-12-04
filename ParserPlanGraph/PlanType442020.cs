using System;
using System.Data;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ParserPlanGraph
{
    public class PlanType442020 : Plan
    {
        public PlanType442020(FileInfo f, string region, int regionId, JObject json)
            : base(f, region, regionId, json)
        {
            AddPlan44 += delegate(int d)
            {
                if (d > 0)
                    Program.AddPlan44_2020++;
                else
                    Log.Logger("Не удалось добавить Plan44", FilePath);
            };
            UpdatePlan44 += delegate(int d)
            {
                if (d > 0)
                    Program.UpdatePlan44_2020++;
                else
                    Log.Logger("Не удалось обновить Plan44", FilePath);
            };
        }

        public event Action<int> AddPlan44;
        public event Action<int> UpdatePlan44;

        public override void Parsing()
        {
            var update = false;
            var xml = GetXml(File.ToString());
            var root = (JObject) P.SelectToken("export");
            var firstOrDefault = root.Properties().FirstOrDefault(pr => pr.Name.Contains("tender"));
            if (firstOrDefault != null)
            {
                var plan = firstOrDefault.Value;
                var idXml = ((string) plan.SelectToken("id") ?? "").Trim();

                if (string.IsNullOrEmpty(idXml))
                {
                    Log.Logger("У плана нет id", FilePath);
                    return;
                }

                var planNumber = ((string) plan.SelectToken("planNumber") ?? "").Trim();
                var versionNumber = (int?) plan.SelectToken("versionNumber") ?? 0;
                if (string.IsNullOrEmpty(planNumber))
                {
                    Log.Logger("У плана нет planNumber", FilePath);
                    return;
                }

                var planPeriodFirstYear = ((string) plan.SelectToken("commonInfo.planPeriod.firstYear") ?? "").Trim();
                var planPeriodSecondYear = ((string) plan.SelectToken("commonInfo.planPeriod.secondYear") ?? "").Trim();
                using (var connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();
                    var selectPlan =
                        $"SELECT id FROM {Program.Prefix}tender_plan WHERE id_xml = @id_xml AND id_region = @id_region AND plan_number = @plan_number AND num_version = @num_version";
                    var cmd = new MySqlCommand(selectPlan, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@id_xml", idXml);
                    cmd.Parameters.AddWithValue("@id_region", RegionId);
                    cmd.Parameters.AddWithValue("@plan_number", planNumber);
                    cmd.Parameters.AddWithValue("@num_version", versionNumber);
                    var reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Close();
                        return;
                    }

                    reader.Close();
                    var maxVerNumber =
                        $"SELECT IFNULL(MAX(num_version), 0) FROM {Program.Prefix}tender_plan WHERE id_region = @id_region AND plan_number = @plan_number";
                    var cmd0 = new MySqlCommand(maxVerNumber, connect);
                    cmd0.Prepare();
                    cmd0.Parameters.AddWithValue("@id_region", RegionId);
                    cmd0.Parameters.AddWithValue("@plan_number", planNumber);
                    int max;
                    switch (cmd0.ExecuteScalar())
                    {
                        case int i:
                            max = i;
                            break;
                        case long l:
                            max = (int) l;
                            break;
                        default:
                            throw new ArgumentException("type object not int and not long", nameof(max));
                    }

                    if (versionNumber >= max)
                    {
                        if (max != 0)
                        {
                            update = true;
                        }

                        var delAll =
                            $"DELETE tp, ta, tpos, tpr, t_prod, ts, tsp FROM {Program.Prefix}tender_plan AS tp LEFT JOIN {Program.Prefix}tender_plan_attach AS ta ON tp.id = ta.id_plan LEFT JOIN {Program.Prefix}tender_plan_position AS tpos ON tp.id = tpos.id_plan LEFT JOIN {Program.Prefix}tender_plan_pref_rec AS tpr ON tpos.id = tpr.id_plan_prod LEFT JOIN  {Program.Prefix}tender_plan_products AS t_prod ON t_prod.id_tender_plan_position = tpos.id LEFT JOIN {Program.Prefix}tender_plan_special_purchases AS tsp ON tsp.id_plan = tp.id LEFT JOIN {Program.Prefix}tender_plan_special_purchase AS ts ON ts.id_plan_special_purchases = tsp.id WHERE tp.id_region = @id_region AND tp.plan_number = @plan_number";
                        var cmd00 = new MySqlCommand(delAll, connect);
                        cmd00.Prepare();
                        cmd00.Parameters.AddWithValue("@id_region", RegionId);
                        cmd00.Parameters.AddWithValue("@plan_number", planNumber);
                        cmd00.ExecuteNonQuery();
                    }
                    else
                    {
                        return;
                    }

                    var purchasePlanNumber = "";
                    var year = ((string) plan.SelectToken("commonInfo.planYear") ?? "").Trim();
                    var createDate = (JsonConvert.SerializeObject(plan.SelectToken("commonInfo.createDate") ?? "") ??
                                      "").Trim('"');
                    var confirmDate =
                        (JsonConvert.SerializeObject(plan.SelectToken("commonInfo.confirmDate") ?? "") ??
                         "").Trim('"');
                    var publishDate =
                        (JsonConvert.SerializeObject(plan.SelectToken("commonInfo.publishDate") ?? "") ??
                         "").Trim('"');
                    var printform = ((string) plan.SelectToken("printForm.url") ?? "").Trim();
                    var cancelStatus = 0;
                    if (!string.IsNullOrEmpty(publishDate))
                    {
                        var selectDateP =
                            $"SELECT id, create_date FROM {Program.Prefix}tender_plan WHERE id_region = @id_region AND plan_number = @plan_number";
                        var cmd2 = new MySqlCommand(selectDateP, connect);
                        cmd2.Prepare();
                        cmd2.Parameters.AddWithValue("@id_region", RegionId);
                        cmd2.Parameters.AddWithValue("@plan_number", planNumber);
                        var dt = new DataTable();
                        var adapter = new MySqlDataAdapter {SelectCommand = cmd2};
                        adapter.Fill(dt);
                        if (dt.Rows.Count > 0)
                        {
                            foreach (DataRow row in dt.Rows)
                            {
                                var dateNew = DateTime.Parse(createDate);
                                var dateOld = (DateTime) row["create_date"];
                                if (dateNew > dateOld)
                                {
                                    var updatePlanCancel =
                                        $"UPDATE {Program.Prefix}tender_plan SET cancel = 1 WHERE id = @id";
                                    var cmd3 = new MySqlCommand(updatePlanCancel, connect);
                                    cmd3.Prepare();
                                    cmd3.Parameters.AddWithValue("id", (int) row["id"]);
                                    cmd3.ExecuteNonQuery();
                                }
                                else
                                {
                                    cancelStatus = 1;
                                }
                            }
                        }
                    }

                    var idCustomer = 0;
                    var idOwner = 0;
                    var customerRegNum =
                        ((string) plan.SelectToken("commonInfo.customerInfo.regNum") ?? "").Trim();
                    if (!string.IsNullOrEmpty(customerRegNum))
                    {
                        var selectCust = "SELECT id FROM od_customer WHERE regNumber = @regNumber";
                        var cmd4 = new MySqlCommand(selectCust, connect);
                        cmd4.Prepare();
                        cmd4.Parameters.AddWithValue("@regNumber", customerRegNum);
                        var reader2 = cmd4.ExecuteReader();
                        if (reader2.HasRows)
                        {
                            reader2.Read();
                            idCustomer = reader2.GetInt32("id");
                            reader2.Close();
                        }
                        else
                        {
                            reader2.Close();
                            var cusFullName = ((string) plan.SelectToken("commonInfo.customerInfo.fullName") ?? "")
                                .Trim();
                            var cusInn = ((string) plan.SelectToken("commonInfo.customerInfo.INN") ?? "").Trim();
                            var cusKpp = ((string) plan.SelectToken("commonInfo.customerInfo.KPP") ?? "").Trim();
                            var cusPhone =
                                ((string) plan.SelectToken("commonInfo.customerInfo.phone") ?? "").Trim();
                            var cusEmail =
                                ((string) plan.SelectToken("commonInfo.customerInfo.email") ?? "").Trim();
                            var cusLastName =
                                ((string) plan.SelectToken("commonInfo.confirmContactInfo.lastName") ?? "").Trim();
                            var cusFirstName =
                                ((string) plan.SelectToken("commonInfo.confirmContactInfo.firstName") ?? "").Trim();
                            var cusMiddleName =
                                ((string) plan.SelectToken("commonInfo.confirmContactInfo.middleName") ?? "")
                                .Trim();
                            var cusContactName = $"{cusLastName} {cusFirstName} {cusMiddleName}".Trim();
                            var insertCustomer =
                                "INSERT INTO od_customer SET regNumber = @regNumber, inn = @inn, kpp = @kpp, full_name = @full_name, phone = @phone, email = @email, contact_name = @contact_name";
                            var cmd5 = new MySqlCommand(insertCustomer, connect);
                            cmd5.Prepare();
                            cmd5.Parameters.AddWithValue("@regNumber", customerRegNum);
                            cmd5.Parameters.AddWithValue("@inn", cusInn);
                            cmd5.Parameters.AddWithValue("@kpp", cusKpp);
                            cmd5.Parameters.AddWithValue("@full_name", cusFullName);
                            cmd5.Parameters.AddWithValue("@phone", cusPhone);
                            cmd5.Parameters.AddWithValue("@email", cusEmail);
                            cmd5.Parameters.AddWithValue("@contact_name", cusContactName);
                            cmd5.ExecuteNonQuery();
                            idCustomer = (int) cmd5.LastInsertedId;
                        }
                    }

                    var ownerRegNum = ((string) plan.SelectToken("commonInfo.ownerInfo.regNum") ?? "").Trim();
                    if (!string.IsNullOrEmpty(ownerRegNum))
                    {
                        var selectOwner = "SELECT id FROM od_customer WHERE regNumber = @regNumber";
                        var cmd6 = new MySqlCommand(selectOwner, connect);
                        cmd6.Prepare();
                        cmd6.Parameters.AddWithValue("@regNumber", ownerRegNum);
                        var reader3 = cmd6.ExecuteReader();
                        if (reader3.HasRows)
                        {
                            reader3.Read();
                            idOwner = reader3.GetInt32("id");
                            reader3.Close();
                        }
                        else
                        {
                            reader3.Close();
                            var ownerFullName = ((string) plan.SelectToken("commonInfo.ownerInfo.fullName") ?? "")
                                .Trim();
                            var ownerInn = ((string) plan.SelectToken("commonInfo.ownerInfo.INN") ?? "")
                                .Trim();
                            var ownerKpp = ((string) plan.SelectToken("commonInfo.ownerInfo.KPP") ?? "")
                                .Trim();
                            var ownerPhone = ((string) plan.SelectToken("commonInfo.ownerInfo.phone") ?? "")
                                .Trim();
                            var ownerEmail = ((string) plan.SelectToken("commonInfo.ownerInfo.email") ?? "")
                                .Trim();
                            var ownerFirstName =
                                ((string) plan.SelectToken("commonInfo.confirmContactInfo.firstName") ?? "")
                                .Trim();
                            var ownerLastName =
                                ((string) plan.SelectToken("commonInfo.confirmContactInfo.lastName") ?? "")
                                .Trim();
                            var ownerMiddleName =
                                ((string) plan.SelectToken("commonInfo.confirmContactInfo.middleName") ?? "").Trim();
                            var ownerContactName =
                                $"{ownerLastName} {ownerFirstName} {ownerMiddleName}".Trim();
                            var insertOwner =
                                "INSERT INTO od_customer SET regNumber = @regNumber, inn = @inn, kpp = @kpp, full_name = @full_name, phone = @phone, email = @email, contact_name = @contact_name";
                            var cmd7 = new MySqlCommand(insertOwner, connect);
                            cmd7.Prepare();
                            cmd7.Parameters.AddWithValue("@regNumber", customerRegNum);
                            cmd7.Parameters.AddWithValue("@inn", ownerInn);
                            cmd7.Parameters.AddWithValue("@kpp", ownerKpp);
                            cmd7.Parameters.AddWithValue("@full_name", ownerFullName);
                            cmd7.Parameters.AddWithValue("@phone", ownerPhone);
                            cmd7.Parameters.AddWithValue("@email", ownerEmail);
                            cmd7.Parameters.AddWithValue("@contact_name", ownerContactName);
                            cmd7.ExecuteNonQuery();
                            idOwner = (int) cmd7.LastInsertedId;
                        }
                    }

                    var sumPushasesSmallBusinessTotal = "";
                    var sumPushasesSmallBusinessCurrentYear = "";
                    var sumPushasesRequestTotal = "";
                    var sumPushasesRequestCurrentYear = "";
                    var financeSupportTotal =
                        ((string) plan.SelectToken("outcomeIndicators.totalsInfo.total") ?? "")
                        .Trim();
                    var financeSupportCurrentYear =
                        ((string) plan.SelectToken("outcomeIndicators.totalsInfo.currentYear") ?? "")
                        .Trim();
                    var insertPlan =
                        $"INSERT INTO {Program.Prefix}tender_plan SET id_xml = @id_xml, plan_number = @plan_number, num_version = @num_version, id_region = @id_region, purchase_plan_number = @purchase_plan_number, year = @year, create_date = @create_date, confirm_date = @confirm_date, publish_date = @publish_date, id_customer = @id_customer, id_owner = @id_owner, print_form = @print_form, cancel = @cancel, sum_pushases_small_business_total = @sum_pushases_small_business_total, sum_pushases_small_business_current_year = @sum_pushases_small_business_current_year, sum_pushases_request_total = @sum_pushases_request_total, sum_pushases_request_current_year = @sum_pushases_request_current_year, finance_support_total = @finance_support_total, finance_support_current_year = @finance_support_current_year, xml = @xml, plan_specification = @plan_specification, plan_period_first_year = @plan_period_first_year, plan_period_second_year = @plan_period_second_year";
                    var cmd8 = new MySqlCommand(insertPlan, connect);
                    cmd8.Prepare();
                    cmd8.Parameters.AddWithValue("@id_xml", idXml);
                    cmd8.Parameters.AddWithValue("@plan_number", planNumber);
                    cmd8.Parameters.AddWithValue("@num_version", versionNumber);
                    cmd8.Parameters.AddWithValue("@id_region", RegionId);
                    cmd8.Parameters.AddWithValue("@purchase_plan_number", purchasePlanNumber);
                    cmd8.Parameters.AddWithValue("@year", year);
                    cmd8.Parameters.AddWithValue("@create_date", createDate);
                    cmd8.Parameters.AddWithValue("@confirm_date", confirmDate);
                    cmd8.Parameters.AddWithValue("@publish_date", publishDate);
                    cmd8.Parameters.AddWithValue("@id_customer", idCustomer);
                    cmd8.Parameters.AddWithValue("@id_owner", idOwner);
                    cmd8.Parameters.AddWithValue("@print_form", printform);
                    cmd8.Parameters.AddWithValue("@cancel", cancelStatus);
                    cmd8.Parameters.AddWithValue("@sum_pushases_small_business_total",
                        sumPushasesSmallBusinessTotal);
                    cmd8.Parameters.AddWithValue("@sum_pushases_small_business_current_year",
                        sumPushasesSmallBusinessCurrentYear);
                    cmd8.Parameters.AddWithValue("@sum_pushases_request_total", sumPushasesRequestTotal);
                    cmd8.Parameters.AddWithValue("@sum_pushases_request_current_year",
                        sumPushasesRequestCurrentYear);
                    cmd8.Parameters.AddWithValue("@finance_support_total", financeSupportTotal);
                    cmd8.Parameters.AddWithValue("@finance_support_current_year", financeSupportCurrentYear);
                    cmd8.Parameters.AddWithValue("@xml", xml);
                    cmd8.Parameters.AddWithValue("@plan_specification", 2020);
                    cmd8.Parameters.AddWithValue("@plan_period_first_year", planPeriodFirstYear);
                    cmd8.Parameters.AddWithValue("@plan_period_second_year", planPeriodSecondYear);
                    var resPlan = cmd8.ExecuteNonQuery();
                    var idPlan = (int) cmd8.LastInsertedId;
                    if (update)
                    {
                        UpdatePlan44?.Invoke(resPlan);
                    }
                    else
                    {
                        AddPlan44?.Invoke(resPlan);
                    }

                    var positions = GetElements(plan, "positions.position");
                    foreach (var pos in positions)
                    {
                        var positionNumber = ((string) pos.SelectToken("commonInfo.positionNumber") ?? "")
                            .Trim();
                        var purchasePlanPositionNumber =
                            ((string) pos.SelectToken("commonInfo.purchasePlanPositionInfo.positionNumber") ?? "")
                            .Trim();
                        var purchaseObjectName =
                            ((string) pos.SelectToken("commonInfo.purchaseObjectInfo") ?? "")
                            .Trim();
                        var startMonth = "";
                        var endMonth = "";
                        var idPlacingWay = 0;
                        var financeTotal =
                            ((string) pos.SelectToken("financeInfo.total") ?? "").Trim();
                        var financeTotalCurrentYear =
                            ((string) pos.SelectToken("financeInfo.currentYear") ?? "").Trim();
                        var insertPosition =
                            $"INSERT INTO {Program.Prefix}tender_plan_position SET id_plan = @id_plan, position_number = @position_number, purchase_plan_position_number = @purchase_plan_position_number, purchase_object_name = @purchase_object_name, start_month = @start_month, end_month = @end_month, id_placing_way = @id_placing_way, finance_total = @finance_total, finance_total_current_year = @finance_total_current_year, max_price = @max_price, purchase_fin_condition = @purchase_fin_condition, contract_fin_condition = @contract_fin_condition, advance_fin_condition = @advance_fin_condition, purchase_graph = @purchase_graph, bank_support_info = @bank_support_info";
                        var cmd11 = new MySqlCommand(insertPosition, connect);
                        cmd11.Prepare();
                        cmd11.Parameters.AddWithValue("@id_plan", idPlan);
                        cmd11.Parameters.AddWithValue("@position_number", positionNumber);
                        cmd11.Parameters.AddWithValue("@purchase_plan_position_number", purchasePlanPositionNumber);
                        cmd11.Parameters.AddWithValue("@purchase_object_name", purchaseObjectName);
                        cmd11.Parameters.AddWithValue("@start_month", startMonth);
                        cmd11.Parameters.AddWithValue("@end_month", endMonth);
                        cmd11.Parameters.AddWithValue("@id_placing_way", idPlacingWay);
                        cmd11.Parameters.AddWithValue("@finance_total", financeTotal);
                        cmd11.Parameters.AddWithValue("@finance_total_current_year", financeTotalCurrentYear);
                        cmd11.Parameters.AddWithValue("@max_price", "");
                        cmd11.Parameters.AddWithValue("@purchase_fin_condition", "");
                        cmd11.Parameters.AddWithValue("@contract_fin_condition", "");
                        cmd11.Parameters.AddWithValue("@advance_fin_condition", "");
                        cmd11.Parameters.AddWithValue("@purchase_graph", "");
                        cmd11.Parameters.AddWithValue("@bank_support_info", "");
                        cmd11.ExecuteNonQuery();
                        var idProd = (int) cmd11.LastInsertedId;
                        var insertProduct =
                            $"INSERT INTO {Program.Prefix}tender_plan_products SET id_tender_plan_position = @id_tender_plan_position, OKPD2_code = @OKPD2_code, OKPD2_name = @OKPD2_name, OKEI_code = @OKEI_code, OKEI_name = @OKEI_name, prod_description = @prod_description, products_quantity_total = @products_quantity_total, products_quantity_current_year = @products_quantity_current_year, product_sum_total = @product_sum_total, product_sum_current_year = @product_sum_current_year";
                        var products = GetElements(pos, "commonInfo.undefined.OKPD2s.OKPD2Info");
                        if (products.Count == 0)
                        {
                            var okpd2Code =
                                ((string) pos.SelectToken("commonInfo.OKPD2Info.OKPDCode") ?? "").Trim();
                            var okpd2Name =
                                ((string) pos.SelectToken("commonInfo.OKPD2Info.OKPDName") ?? "").Trim();
                            var okeiCode = "";
                            var okeiName = "";
                            var posDescription =
                                ((string) pos.SelectToken("commonInfo.KVRInfo.KVR.name") ?? "")
                                .Trim();
                            var productsQuantityTotal = "";
                            var productsQuantityCurrentYear = "";
                            var cmd14 = new MySqlCommand(insertProduct, connect);
                            cmd14.Prepare();
                            cmd14.Parameters.AddWithValue("@id_tender_plan_position", idProd);
                            cmd14.Parameters.AddWithValue("@OKPD2_code", okpd2Code);
                            cmd14.Parameters.AddWithValue("@OKPD2_name", okpd2Name);
                            cmd14.Parameters.AddWithValue("@OKEI_code", okeiCode);
                            cmd14.Parameters.AddWithValue("@OKEI_name", okeiName);
                            cmd14.Parameters.AddWithValue("@prod_description", posDescription);
                            cmd14.Parameters.AddWithValue("@products_quantity_total", productsQuantityTotal);
                            cmd14.Parameters.AddWithValue("@products_quantity_current_year",
                                productsQuantityCurrentYear);
                            cmd14.Parameters.AddWithValue("@product_sum_total", 0.0m);
                            cmd14.Parameters.AddWithValue("@product_sum_current_year", 0.0m);
                            cmd14.ExecuteNonQuery();
                        }
                        else if (products.Count != 0)
                        {
                            foreach (var p in products)
                            {
                                var okpd2Code =
                                    ((string) p.SelectToken("OKPDCode") ?? "").Trim();
                                var okpd2Name =
                                    ((string) p.SelectToken("OKPDName") ?? "").Trim();
                                var okeiCode = "";
                                var okeiName = "";
                                var posDescription = ((string) pos.SelectToken("commonInfo.KVRInfo.KVR.name") ?? "")
                                    .Trim();
                                var productQuantityTotal = 0.0m;
                                var productQuantityCurrentYear = 0.0m;
                                var productSumTotal = 0.0m;
                                var productSumCurrentYear = 0.0m;
                                var cmd14 = new MySqlCommand(insertProduct, connect);
                                cmd14.Prepare();
                                cmd14.Parameters.AddWithValue("@id_tender_plan_position", idProd);
                                cmd14.Parameters.AddWithValue("@OKPD2_code", okpd2Code);
                                cmd14.Parameters.AddWithValue("@OKPD2_name", okpd2Name);
                                cmd14.Parameters.AddWithValue("@OKEI_code", okeiCode);
                                cmd14.Parameters.AddWithValue("@OKEI_name", okeiName);
                                cmd14.Parameters.AddWithValue("@prod_description", posDescription);
                                cmd14.Parameters.AddWithValue("@products_quantity_total", productQuantityTotal);
                                cmd14.Parameters.AddWithValue("@products_quantity_current_year",
                                    productQuantityCurrentYear);
                                cmd14.Parameters.AddWithValue("@product_sum_total", productSumTotal);
                                cmd14.Parameters.AddWithValue("@product_sum_current_year", productSumCurrentYear);
                                cmd14.ExecuteNonQuery();
                            }
                        }
                    }

                    var specPositions = GetElements(plan, "specialPurchases.specialPurchase");
                    foreach (var spec in specPositions)
                    {
                        var typeCode =
                            ((string) spec.SelectToken("type.code") ?? "")
                            .Trim();
                        var typeName =
                            ((string) spec.SelectToken("type.name") ?? "")
                            .Trim();
                        var financeTotal =
                            ((string) spec.SelectToken("yearFinanceInfo.total") ?? "").Trim();
                        var financeTotalCurrentYear =
                            ((string) spec.SelectToken("yearFinanceInfo.currentYear") ?? "").Trim();
                        var insertSpecPositions =
                            $"INSERT INTO {Program.Prefix}tender_plan_special_purchases SET id_plan = @id_plan, type_code = @type_code, type_name = @type_name, finance_total = @finance_total, finance_total_current_year = @finance_total_current_year";
                        var cmd12 = new MySqlCommand(insertSpecPositions, connect);
                        cmd12.Prepare();
                        cmd12.Parameters.AddWithValue("@id_plan", idPlan);
                        cmd12.Parameters.AddWithValue("@type_code", typeCode);
                        cmd12.Parameters.AddWithValue("@type_name", typeName);
                        cmd12.Parameters.AddWithValue("@finance_total", financeTotal);
                        cmd12.Parameters.AddWithValue("@finance_total_current_year", financeTotalCurrentYear);
                        cmd12.ExecuteNonQuery();
                        var idSpecPos = (int) cmd12.LastInsertedId;
                        var specPos = GetElements(spec, "purchases.purchase");
                        foreach (var spp in specPos)
                        {
                            var posNum =
                                ((string) spp.SelectToken("positionNumber") ?? "")
                                .Trim();
                            var ikz =
                                ((string) spp.SelectToken("IKZ") ?? "")
                                .Trim();
                            var pubYear =
                                ((string) spp.SelectToken("publishYear") ?? "")
                                .Trim();
                            var purNum =
                                ((string) spp.SelectToken("purchaseNumber") ?? "")
                                .Trim();
                            var kvrName =
                                ((string) spp.SelectToken("KVRInfo.KVR.name") ?? "")
                                .Trim();
                            var financeTotalP =
                                ((string) spp.SelectToken("financeInfo.total") ?? "").Trim();
                            var financeTotalCurrentYearP =
                                ((string) spp.SelectToken("financeInfo.currentYear") ?? "").Trim();
                            var insertSpecPosition =
                                $"INSERT INTO {Program.Prefix}tender_plan_special_purchase SET id_plan_special_purchases = @id_plan_special_purchases, position_number = @position_number, ikz = @ikz, publish_year = @publish_year, purchase_number = @purchase_number, kvr_name = @kvr_name , finance_total_current_year = @finance_total_current_year, finance_total = @finance_total";
                            var cmd13 = new MySqlCommand(insertSpecPosition, connect);
                            cmd13.Prepare();
                            cmd13.Parameters.AddWithValue("@id_plan_special_purchases", idSpecPos);
                            cmd13.Parameters.AddWithValue("@position_number", posNum);
                            cmd13.Parameters.AddWithValue("@ikz", ikz);
                            cmd13.Parameters.AddWithValue("@publish_year", pubYear);
                            cmd13.Parameters.AddWithValue("@purchase_number", purNum);
                            cmd13.Parameters.AddWithValue("@kvr_name", kvrName);
                            cmd13.Parameters.AddWithValue("@finance_total_current_year", financeTotalCurrentYearP);
                            cmd13.Parameters.AddWithValue("@finance_total", financeTotalP);
                            cmd13.ExecuteNonQuery();
                        }
                    }

                    var attach = GetElements(plan, "attachments.attachment");
                    foreach (var att in attach)
                    {
                        var fileName = ((string) att.SelectToken("fileName") ?? "").Trim();
                        var desc = ((string) att.SelectToken("docDescription") ?? "").Trim();
                        var url = ((string) att.SelectToken("url") ?? "").Trim();
                        var insertAttach =
                            $"INSERT INTO {Program.Prefix}tender_plan_attach SET id_plan = @id_plan, file_name = @file_name, description = @description, url = @url";
                        var cmd13 = new MySqlCommand(insertAttach, connect);
                        cmd13.Prepare();
                        cmd13.Parameters.AddWithValue("@id_plan", idPlan);
                        cmd13.Parameters.AddWithValue("@file_name", fileName);
                        cmd13.Parameters.AddWithValue("@description", desc);
                        cmd13.Parameters.AddWithValue("@url", url);
                        cmd13.ExecuteNonQuery();
                    }
                }
            }
            else
            {
                Log.Logger("Не могу найти тег Plan44", FilePath);
            }
        }
    }
}