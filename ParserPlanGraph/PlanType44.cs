using System;
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
        public PlanType44(FileInfo f, string region, int regionId, JObject json)
            : base(f, region, regionId, json)
        {
            AddPlan44 += delegate(int d)
            {
                if (d > 0)
                    Program.AddPlan44++;
                else
                    Log.Logger("Не удалось добавить Plan44", FilePath);
            };
        }

        public event Action<int> AddPlan44;

        public override void Parsing()
        {
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
                        //Log.Logger("Такой план уже есть в базе", FilePath, idXml, planNumber);
                        return;
                    }

                    reader.Close();
                    var maxVerNumber =
                        $"SELECT IFNULL(MAX(num_version), 0) FROM {Program.Prefix}tender_plan WHERE id_region = @id_region AND plan_number = @plan_number";
                    var cmd0 = new MySqlCommand(maxVerNumber, connect);
                    cmd0.Prepare();
                    cmd0.Parameters.AddWithValue("@id_region", RegionId);
                    cmd0.Parameters.AddWithValue("@plan_number", planNumber);
                    var max = (long) cmd0.ExecuteScalar();
                    if (versionNumber >= max)
                    {
                        var delAll =
                            $"DELETE tp, ta, tpos, tpr, t_prod FROM {Program.Prefix}tender_plan AS tp LEFT JOIN {Program.Prefix}tender_plan_attach AS ta ON tp.id = ta.id_plan LEFT JOIN {Program.Prefix}tender_plan_position AS tpos ON tp.id = tpos.id_plan LEFT JOIN {Program.Prefix}tender_plan_pref_rec AS tpr ON tpos.id = tpr.id_plan_prod LEFT JOIN  {Program.Prefix}tender_plan_products AS t_prod ON t_prod.id_tender_plan_position = tpos.id WHERE tp.id_region = @id_region AND tp.plan_number = @plan_number";
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
                                ((string) plan.SelectToken("commonInfo.responsibleContactInfo.lastName") ?? "").Trim();
                            var cusFirstName =
                                ((string) plan.SelectToken("commonInfo.responsibleContactInfo.firstName") ?? "").Trim();
                            var cusMiddleName =
                                ((string) plan.SelectToken("commonInfo.responsibleContactInfo.middleName") ?? "")
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

                    var sumPushasesSmallBusinessTotal =
                        ((string) plan.SelectToken("totals.outcomeIndicators.sumPushasesSmallBusiness.total") ?? "")
                        .Trim();
                    var sumPushasesSmallBusinessCurrentYear =
                        ((string) plan.SelectToken("totals.outcomeIndicators.sumPushasesSmallBusiness.currentYear") ??
                         "")
                        .Trim();
                    var sumPushasesRequestTotal =
                        ((string) plan.SelectToken("totals.outcomeIndicators.sumPushasesRequest.total") ?? "")
                        .Trim();
                    var sumPushasesRequestCurrentYear =
                        ((string) plan.SelectToken("totals.outcomeIndicators.sumPushasesRequest.currentYear") ?? "")
                        .Trim();
                    var financeSupportTotal =
                        ((string) plan.SelectToken("totals.financeSupport.financeSupportTotal.total") ?? "")
                        .Trim();
                    var financeSupportCurrentYear =
                        ((string) plan.SelectToken("totals.financeSupport.financeSupportTotal.currentYear") ?? "")
                        .Trim();
                    var insertPlan =
                        $"INSERT INTO {Program.Prefix}tender_plan SET id_xml = @id_xml, plan_number = @plan_number, num_version = @num_version, id_region = @id_region, purchase_plan_number = @purchase_plan_number, year = @year, create_date = @create_date, confirm_date = @confirm_date, publish_date = @publish_date, id_customer = @id_customer, id_owner = @id_owner, print_form = @print_form, cancel = @cancel, sum_pushases_small_business_total = @sum_pushases_small_business_total, sum_pushases_small_business_current_year = @sum_pushases_small_business_current_year, sum_pushases_request_total = @sum_pushases_request_total, sum_pushases_request_current_year = @sum_pushases_request_current_year, finance_support_total = @finance_support_total, finance_support_current_year = @finance_support_current_year, xml = @xml";
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
                    var resPlan = cmd8.ExecuteNonQuery();
                    var idPlan = (int) cmd8.LastInsertedId;
                    AddPlan44?.Invoke(resPlan);
                    var positions = GetElements(plan, "positions.position");
                    foreach (var pos in positions)
                    {
                        var positionNumber = ((string) pos.SelectToken("commonInfo.positionNumber") ?? "")
                            .Trim();
                        var purchasePlanPositionNumber =
                            ((string) pos.SelectToken("commonInfo.purchasePlanPositionInfo.positionNumber") ?? "")
                            .Trim();
                        var purchaseObjectName =
                            ((string) pos.SelectToken("commonInfo.positionInfo.purchaseObjectName") ?? "")
                            .Trim();
                        var startMonth =
                            ((string) pos.SelectToken("commonInfo.positionInfo.placingNotificationTerm.month") ?? "")
                            .Trim();
                        var endMonth =
                            ((string) pos.SelectToken("commonInfo.positionInfo.endContratProcedureTerm.month") ?? "")
                            .Trim();
                        var idPlacingWay = 0;
                        var placingWayCode =
                            ((string) pos.SelectToken("commonInfo.placingWayInfo.placingWay.code") ?? "").Trim();
                        var placingWayName =
                            ((string) pos.SelectToken("commonInfo.placingWayInfo.placingWay.name") ?? "").Trim();
                        if (!string.IsNullOrEmpty(placingWayCode))
                        {
                            var selectPlacingWay =
                                $"SELECT id_placing_way FROM {Program.Prefix}tender_plan_placing_way WHERE code = @code";
                            var cmd9 = new MySqlCommand(selectPlacingWay, connect);
                            cmd9.Prepare();
                            cmd9.Parameters.AddWithValue("@code", placingWayCode);
                            var reader4 = cmd9.ExecuteReader();
                            if (reader4.HasRows)
                            {
                                reader4.Read();
                                idPlacingWay = reader4.GetInt32("id_placing_way");
                                reader4.Close();
                            }
                            else
                            {
                                reader4.Close();
                                var insertPlacingWay =
                                    $"INSERT INTO {Program.Prefix}tender_plan_placing_way SET code= @code, name= @name";
                                var cmd10 = new MySqlCommand(insertPlacingWay, connect);
                                cmd10.Prepare();
                                cmd10.Parameters.AddWithValue("@code", placingWayCode);
                                cmd10.Parameters.AddWithValue("@name", placingWayName);
                                cmd10.ExecuteNonQuery();
                                idPlacingWay = (int) cmd10.LastInsertedId;
                                //Log.Logger("Добавлен новый placing_way", file_path, id_placing_way);
                            }
                        }

                        var financeTotal =
                            ((string) pos.SelectToken("commonInfo.financeInfo.planPayments.total") ?? "").Trim();
                        var financeTotalCurrentYear =
                            ((string) pos.SelectToken("commonInfo.financeInfo.planPayments.currentYear") ?? "").Trim();
                        var maxPrice = ((string) pos.SelectToken("commonInfo.financeInfo.maxPrice") ?? "").Trim();
                        var purchaseFinCondition =
                            ((string) pos.SelectToken("purchaseConditions.purchaseFinCondition.amount") ?? "").Trim();
                        var contractFinCondition =
                            ((string) pos.SelectToken("purchaseConditions.contractFinCondition.amount") ?? "").Trim();
                        var advanceFinCondition =
                            ((string) pos.SelectToken("purchaseConditions.advanceFinCondition.amount") ?? "").Trim();
                        var purchaseGraph =
                            ((string) pos.SelectToken("purchaseConditions.purchaseGraph.plannedPeriod") ?? "").Trim();
                        if (string.IsNullOrEmpty(purchaseGraph))
                        {
                            purchaseGraph =
                                ((string) pos.SelectToken(
                                     "purchaseConditions.purchaseGraph.periodicity.periodicityType") ?? "").Trim();
                        }

                        if (string.IsNullOrEmpty(purchaseGraph))
                        {
                            purchaseGraph =
                                ((string) pos.SelectToken(
                                     "purchaseConditions.purchaseGraph.periodicity.otherPeriodicityText") ?? "").Trim();
                        }

                        var bankSupportInfo =
                            ((string) pos.SelectToken("purchaseConditions.bankSupportInfo.bankSupportText") ?? "")
                            .Trim();

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
                        cmd11.Parameters.AddWithValue("@max_price", maxPrice);
                        cmd11.Parameters.AddWithValue("@purchase_fin_condition", purchaseFinCondition);
                        cmd11.Parameters.AddWithValue("@contract_fin_condition", contractFinCondition);
                        cmd11.Parameters.AddWithValue("@advance_fin_condition", advanceFinCondition);
                        cmd11.Parameters.AddWithValue("@purchase_graph", purchaseGraph);
                        cmd11.Parameters.AddWithValue("@bank_support_info", bankSupportInfo);
                        cmd11.ExecuteNonQuery();
                        var idProd = (int) cmd11.LastInsertedId;
                        var products = GetElements(pos, "KTRUInfo.productsSpecification.product");
                        var insertProduct =
                            $"INSERT INTO {Program.Prefix}tender_plan_products SET id_tender_plan_position = @id_tender_plan_position, OKPD2_code = @OKPD2_code, OKPD2_name = @OKPD2_name, OKEI_code = @OKEI_code, OKEI_name = @OKEI_name, prod_description = @prod_description, products_quantity_total = @products_quantity_total, products_quantity_current_year = @products_quantity_current_year, product_sum_total = @product_sum_total, product_sum_current_year = @product_sum_current_year";
                        if (products.Count == 0)
                        {
                            var okpd2Code =
                                ((string) pos.SelectToken("purchaseObjectInfo.OKPD2Info.OKPD2.code") ?? "").Trim();
                            var okpd2Name =
                                ((string) pos.SelectToken("purchaseObjectInfo.OKPD2Info.OKPD2.name") ?? "").Trim();
                            var okeiCode = ((string) pos.SelectToken("purchaseObjectInfo.OKEI.code") ?? "").Trim();
                            var okeiName = ((string) pos.SelectToken("purchaseObjectInfo.OKEI.name") ?? "").Trim();
                            var posDescription =
                                ((string) pos.SelectToken("purchaseObjectInfo.objectDescription") ?? "")
                                .Trim();
                            var productsQuantityTotal =
                                ((string) pos.SelectToken("purchaseObjectInfo.productsQuantityInfo.total") ?? "")
                                .Trim();
                            var productsQuantityCurrentYear =
                                ((string) pos.SelectToken("purchaseObjectInfo.productsQuantityInfo.currentYear") ?? "")
                                .Trim();
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
                        else
                        {
                            foreach (var p in products)
                            {
                                var okpd2Code =
                                    ((string) p.SelectToken("OKPD2.code") ?? "").Trim();
                                var okpd2Name =
                                    ((string) p.SelectToken("OKPD2.name") ?? "").Trim();
                                var okeiCode = ((string) p.SelectToken("productOKEI.code") ?? "").Trim();
                                var okeiName = ((string) p.SelectToken("productOKEI.name") ?? "").Trim();
                                var posDescription = ((string) p.SelectToken("productName") ?? "")
                                    .Trim();
                                var productQuantityTotal =
                                    (decimal?) p.SelectToken("productsQuantityInfo.total") ?? 0.0m;
                                var productQuantityCurrentYear =
                                    (decimal?) p.SelectToken("productsQuantityInfo.currentYear") ?? 0.0m;
                                var productSumTotal =
                                    (decimal?) p.SelectToken("productsQuantityInfo.total") ?? 0.0m;
                                var productSumCurrentYear =
                                    (decimal?) p.SelectToken("productsQuantityInfo.currentYear") ?? 0.0m;
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

                        var pills = GetElements(pos, "KTRUInfo.drugPurchaseObjectsInfo.drugPurchaseObjectInfo");
                        foreach (var pl in pills)
                        {
                            var drugInfo = pl.SelectToken("objectInfoUsingReferenceInfo");
                            if (drugInfo is null || drugInfo.Type != JTokenType.Null)
                            {
                                drugInfo = pl.SelectToken("objectInfoUsingTextForm");
                            }

                            if (drugInfo is null || drugInfo.Type != JTokenType.Null)
                            {
                                continue;
                            }

                            var dInf = GetElements(drugInfo, "drugsInfo.drugInfo");
                            foreach (var di in dInf)
                            {
                                var posDescription1 = ((string) di.SelectToken("MNNInfo.MNNName") ?? "")
                                    .Trim();
                                var posDescription2 = ((string) di.SelectToken("tradeInfo.positionTradeNam") ?? "")
                                    .Trim();
                                var posDescription3 =
                                    ((string) di.SelectToken("medicamentalFormInfo.medicamentalFormName") ?? "")
                                    .Trim();
                                var posDescription4 = ((string) di.SelectToken("dosageInfo.dosageGRLSValue") ?? "")
                                    .Trim();
                                var posDescription5 =
                                    ((string) di.SelectToken("packagingInfo.packaging1Quantity") ?? "")
                                    .Trim();
                                var posDescription =
                                    $"{posDescription1} {posDescription2} {posDescription3} {posDescription4} {posDescription5}"
                                        .Trim();
                                var productQuantityTotal =
                                    (decimal?) di.SelectToken("drugQuantityInfo.total") ?? 0.0m;
                                var productQuantityCurrentYear =
                                    (decimal?) di.SelectToken("drugQuantityInfo.currentYear") ?? 0.0m;
                                var productSumTotal =
                                    (decimal?) pl.SelectToken("drugSumPaymentsInfo.total") ?? 0.0m;
                                var productSumCurrentYear =
                                    (decimal?) pl.SelectToken("drugSumPaymentsInfo.currentYear") ?? 0.0m;
                                var cmd14 = new MySqlCommand(insertProduct, connect);
                                cmd14.Prepare();
                                cmd14.Parameters.AddWithValue("@id_tender_plan_position", idProd);
                                cmd14.Parameters.AddWithValue("@OKPD2_code", "");
                                cmd14.Parameters.AddWithValue("@OKPD2_name", "");
                                cmd14.Parameters.AddWithValue("@OKEI_code", "");
                                cmd14.Parameters.AddWithValue("@OKEI_name", "");
                                cmd14.Parameters.AddWithValue("@prod_description", posDescription);
                                cmd14.Parameters.AddWithValue("@products_quantity_total", productQuantityTotal);
                                cmd14.Parameters.AddWithValue("@products_quantity_current_year",
                                    productQuantityCurrentYear);
                                cmd14.Parameters.AddWithValue("@product_sum_total", productSumTotal);
                                cmd14.Parameters.AddWithValue("@product_sum_current_year", productSumCurrentYear);
                                cmd14.ExecuteNonQuery();
                            }
                        }

                        var recPref = GetElements(pos,
                            "purchaseConditions.preferensesRequirements.preferenseRequirement");
                        foreach (var pr in recPref)
                        {
                            var groupCode = ((string) pr.SelectToken("prefsReqsGroup.code") ?? "").Trim();
                            var groupName = ((string) pr.SelectToken("prefsReqsGroup.name") ?? "").Trim();
                            var name = ((string) pr.SelectToken("name") ?? "").Trim();
                            var addInfo = ((string) pr.SelectToken("addInfo") ?? "").Trim();
                            var insertPrefRec =
                                $"INSERT INTO {Program.Prefix}tender_plan_pref_rec SET id_plan_prod = @id_plan_prod, group_code = @group_code, group_name = @group_name, name = @name, add_info = @add_info";
                            var cmd12 = new MySqlCommand(insertPrefRec, connect);
                            cmd12.Prepare();
                            cmd12.Parameters.AddWithValue("@id_plan_prod", idProd);
                            cmd12.Parameters.AddWithValue("@group_code", groupCode);
                            cmd12.Parameters.AddWithValue("@group_name", groupName);
                            cmd12.Parameters.AddWithValue("@name", name);
                            cmd12.Parameters.AddWithValue("@add_info", addInfo);
                            cmd12.ExecuteNonQuery();
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