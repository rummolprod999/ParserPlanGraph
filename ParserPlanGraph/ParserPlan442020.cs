using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ParserPlanGraph
{
    public class ParserPlan442020: Parser
    {
        private readonly string[] _tenderPlan = {"tenderplan2020"};
        private string[] _tenderPlanCancel = {"cancel"};
        private string[] _tenderPlanChange = {"change"};
        protected DataTable DtRegion;

        public ParserPlan442020(TypeArguments arg) : base(arg)
        {
        }

        public override void Parsing()
        {
            DtRegion = GetRegions();
            foreach (DataRow row in DtRegion.Rows)
            {
                var arch = new List<string>();
                var pathParse = "";
                var regionPath = (string) row["path"];
                switch (Program.Periodparsing)
                {
                    case TypeArguments.Last442020:
                        pathParse = $"/fcs_regions/{regionPath}/plangraphs2020/";
                        arch = GetListArchLast(pathParse, regionPath);
                        break;
                    case TypeArguments.Curr442020:
                        pathParse = $"/fcs_regions/{regionPath}/plangraphs2020/currMonth/";
                        arch = GetListArchCurr(pathParse, regionPath);
                        break;
                    case TypeArguments.Prev442020:
                        pathParse = $"/fcs_regions/{regionPath}/plangraphs2020/prevMonth/";
                        arch = GetListArchPrev(pathParse, regionPath);
                        break;
                }

                if (arch.Count == 0)
                {
                    Log.Logger("Получен пустой список архивов", pathParse);
                    continue;
                }

                foreach (var v in arch)
                {
                    GetListFileArch(v, pathParse, (string) row["conf"], (int) row["id"]);
                }
            }
        }
        
        public override void GetListFileArch(string arch, string pathParse, string region, int regionId)
        {
            string filea;
            string pathUnzip;
            filea = GetArch44(arch, pathParse);
            if (string.IsNullOrEmpty(filea)) return;
            pathUnzip = Unzipped.Unzip(filea);
            if (pathUnzip == "") return;
            if (!Directory.Exists(pathUnzip)) return;
            var dirInfo = new DirectoryInfo(pathUnzip);
            var filelist = dirInfo.GetFiles();
            var arrayTenderPlan = filelist
                .Where(a => _tenderPlan.Any(
                    t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                .ToList();
            var arrayTenderPlanCancel = filelist
                .Where(a => _tenderPlanCancel.Any(
                    t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                .ToList();
            var arrayTenderPlanChange = filelist
                .Where(a => _tenderPlanChange.Any(
                    t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                .ToList();
            foreach (var f in arrayTenderPlan)
            {
                Bolter(f, region, regionId, TypeFile44.Plan2020);
            }

            foreach (var f in arrayTenderPlanCancel)
            {
                Bolter(f, region, regionId, TypeFile44.PlanCancel2020);
            }

            foreach (var f in arrayTenderPlanChange)
            {
                Bolter(f, region, regionId, TypeFile44.PlanChange2020);
            }

            dirInfo.Delete(true);
        }
        
        public override void Bolter(FileInfo f, string region, int regionId, TypeFile44 typefile)
        {
            if (!f.Name.ToLower().EndsWith(".xml", StringComparison.Ordinal))
            {
                return;
            }

            try
            {
                ParsingXml(f, region, regionId, typefile);
            }
            catch (Exception e)
            {
                Log.Logger("Ошибка при парсинге xml", e, f);
            }
        }
        
        public void ParsingXml(FileInfo f, string region, int regionId, TypeFile44 typefile)
        {
            using (var sr = new StreamReader(f.ToString(), Encoding.Default))
            {
                var ftext = sr.ReadToEnd();
                ftext = ClearText.ClearString(ftext);
                var doc = new XmlDocument();
                doc.LoadXml(ftext);
                var jsons = JsonConvert.SerializeXmlNode(doc);
                var json = JObject.Parse(jsons);
                switch (typefile)
                {
                    case TypeFile44.Plan2020:
                        var a = new PlanType442020(f, region, regionId, json);
                        a.Parsing();
                        break;
                    case TypeFile44.PlanCancel2020:
                        /*var b = new PlanTypeCancel44(f, region, regionId, json);
                        b.Parsing();*/
                        break;
                    case TypeFile44.PlanChange2020:
                        /*var c = new PlanTypeChange44(f, region, regionId, json);
                        c.Parsing();*/
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(typefile), typefile, null);
                }
            }
        }
        
        public override List<string> GetListArchLast(string pathParse, string regionPath)
        {
            var archtemp = new List<string>();
            /*FtpClient ftp = ClientFtp44();*/
            try
            {
                var ftp = ClientFtp44_old();
                ftp.ChangeWorkingDirectory(pathParse);
                archtemp = ftp.ListDirectory();
            }
            catch (Exception)
            {
                Log.Logger("Не могу найти директорию", pathParse);
            }

            return archtemp.Where(a => a.ToLower().IndexOf("tenderplan", StringComparison.Ordinal) != -1).ToList();
        }
        
        public override List<string> GetListArchCurr(string pathParse, string regionPath)
        {
            var arch = new List<string>();
            var archtemp = new List<string>();
            /*FtpClient ftp = ClientFtp44();*/
            try
            {
                var ftp = ClientFtp44_old();
                ftp.ChangeWorkingDirectory(pathParse);
                archtemp = ftp.ListDirectory();
            }
            catch (Exception)
            {
                Log.Logger("Не могу найти директорию", pathParse);
            }

            foreach (var a in archtemp)
            {
                using (var connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();
                    var selectArch =
                        $"SELECT id FROM {Program.Prefix}archiv_plan_graphs WHERE arhiv = @archive";
                    var cmd = new MySqlCommand(selectArch, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@archive", a);
                    var reader = cmd.ExecuteReader();
                    var resRead = reader.HasRows;
                    reader.Close();
                    if (resRead) continue;
                    var addArch =
                        $"INSERT INTO {Program.Prefix}archiv_plan_graphs SET arhiv = @archive";
                    var cmd1 = new MySqlCommand(addArch, connect);
                    cmd1.Prepare();
                    cmd1.Parameters.AddWithValue("@archive", a);
                    cmd1.ExecuteNonQuery();
                    arch.Add(a);
                }
            }

            return arch;
        }
        public override List<string> GetListArchPrev(string pathParse, string regionPath)
        {
            var arch = new List<string>();
            var archtemp = new List<string>();
            /*FtpClient ftp = ClientFtp44();*/
            try
            {
                var ftp = ClientFtp44_old();
                ftp.ChangeWorkingDirectory(pathParse);
                archtemp = ftp.ListDirectory();
            }
            catch (Exception)
            {
                Log.Logger("Не могу найти директорию", pathParse);
            }

            //var serachd = $"{Program.LocalDate:yyyyMMdd}";
            var serachd = $"{Program.LocalDate:yyyy}"; //TODO change it
            foreach (var a in archtemp.Where(a => a.IndexOf(serachd, StringComparison.Ordinal) != -1))
            {
                var prevA = $"prev_{a}";
                using (var connect = ConnectToDb.GetDbConnection())
                {
                    connect.Open();
                    var selectArch =
                        $"SELECT id FROM {Program.Prefix}archiv_plan_graphs WHERE arhiv = @archive";
                    var cmd = new MySqlCommand(selectArch, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@archive", prevA);
                    var reader = cmd.ExecuteReader();
                    var resRead = reader.HasRows;
                    reader.Close();
                    if (resRead) continue;
                    var addArch =
                        $"INSERT INTO {Program.Prefix}archiv_plan_graphs SET arhiv = @archive";
                    var cmd1 = new MySqlCommand(addArch, connect);
                    cmd1.Prepare();
                    cmd1.Parameters.AddWithValue("@archive", prevA);
                    cmd1.ExecuteNonQuery();
                    arch.Add(a);
                }
            }

            return arch;
        }
    }
}