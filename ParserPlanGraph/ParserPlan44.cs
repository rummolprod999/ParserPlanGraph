using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using FluentFTP;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ParserPlanGraph
{
    public class ParserPlan44 : Parser
    {
        protected DataTable DtRegion;
        private string[] tender_plan = new[] {"tenderplan2017"};
        private string[] tender_plan_cancel = new[] {"tenderplancancel"};
        private string[] tender_plan_change = new[] {"tenderplanchange"};

        public ParserPlan44(TypeArguments arg) : base(arg)
        {
        }

        public override void Parsing()
        {
            DtRegion = GetRegions();
            foreach (DataRow row in DtRegion.Rows)
            {
                List<String> arch = new List<string>();
                string PathParse = "";
                string RegionPath = (string) row["path"];
                switch (Program.Periodparsing)
                {
                    case TypeArguments.Last44:
                        PathParse = $"/fcs_regions/{RegionPath}/plangraphs2017/";
                        arch = GetListArchLast(PathParse, RegionPath);
                        break;
                    case TypeArguments.Curr44:
                        PathParse = $"/fcs_regions/{RegionPath}/plangraphs2017/currMonth/";
                        arch = GetListArchCurr(PathParse, RegionPath);
                        break;
                    case TypeArguments.Prev44:
                        PathParse = $"/fcs_regions/{RegionPath}/plangraphs2017/prevMonth/";
                        arch = GetListArchPrev(PathParse, RegionPath);
                        break;
                }

                if (arch.Count == 0)
                {
                    Log.Logger("Получен пустой список архивов", PathParse);
                    continue;
                }

                foreach (var v in arch)
                {
                    GetListFileArch(v, PathParse, (string) row["conf"], (int) row["id"]);
                }
            }
        }

        public override void GetListFileArch(string Arch, string PathParse, string region, int region_id)
        {
            string filea = "";
            string path_unzip = "";
            filea = GetArch44(Arch, PathParse);
            if (!String.IsNullOrEmpty(filea))
            {
                path_unzip = Unzipped.Unzip(filea);
                if (path_unzip != "")
                {
                    if (Directory.Exists(path_unzip))
                    {
                        DirectoryInfo dirInfo = new DirectoryInfo(path_unzip);
                        FileInfo[] filelist = dirInfo.GetFiles();
                        List<FileInfo> arrayTenderPlan = filelist
                            .Where(a => tender_plan.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        List<FileInfo> arrayTenderPlanCancel = filelist
                            .Where(a => tender_plan_cancel.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        List<FileInfo> arrayTenderPlanChange = filelist
                            .Where(a => tender_plan_change.Any(
                                t => a.Name.ToLower().IndexOf(t, StringComparison.Ordinal) != -1))
                            .ToList();
                        foreach (var f in arrayTenderPlan)
                        {
                            Bolter(f, region, region_id, TypeFile44.Plan);
                        }
                        
                        foreach (var f in arrayTenderPlanCancel)
                        {
                            Bolter(f, region, region_id, TypeFile44.PlanCancel);
                        }
                        foreach (var f in arrayTenderPlanChange)
                        {
                            Bolter(f, region, region_id, TypeFile44.PlanChange);
                        }
                        dirInfo.Delete(true);
                    }
                }
            }
        }
        
        public override void Bolter(FileInfo f, string region, int region_id, TypeFile44 typefile)
        {
            if (!f.Name.ToLower().EndsWith(".xml", StringComparison.Ordinal))
            {
                return;
            }

            try
            {
                ParsingXML(f, region, region_id, typefile);
            }
            catch (Exception e)
            {
                Log.Logger("Ошибка при парсинге xml", e, f);
            }
        }

        public void ParsingXML(FileInfo f, string region, int region_id, TypeFile44 typefile)
        {
            using (StreamReader sr = new StreamReader(f.ToString(), Encoding.Default))
            {
                var ftext = sr.ReadToEnd();
                ftext = ClearText.ClearString(ftext);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(ftext);
                string jsons = JsonConvert.SerializeXmlNode(doc);
                JObject json = JObject.Parse(jsons);
                switch (typefile)
                {
                    case TypeFile44.Plan:
                        PlanType44 a = new PlanType44(f, region, region_id, json);
                        a.Parsing();
                        break;
                    case TypeFile44.PlanCancel:
                        PlanTypeCancel44 b = new PlanTypeCancel44(f, region, region_id, json);
                        b.Parsing();
                        break;
                    case TypeFile44.PlanChange:
                        PlanTypeChange44 c = new PlanTypeChange44(f, region, region_id, json);
                        c.Parsing();
                        break;
                }
            }
        }

        public override List<String> GetListArchLast(string PathParse, string RegionPath)
        {
            List<string> archtemp = new List<string>();
            /*FtpClient ftp = ClientFtp44();*/
            try
            {
                WorkWithFtp ftp = ClientFtp44_old();
                ftp.ChangeWorkingDirectory(PathParse);
                archtemp = ftp.ListDirectory();
            }
            catch (Exception e)
            {
                Log.Logger("Не могу найти директорию", PathParse);
            }

            return archtemp.Where(a => a.ToLower().IndexOf("tenderplan", StringComparison.Ordinal) != -1).ToList();
        }

        public override List<String> GetListArchCurr(string PathParse, string RegionPath)
        {
            List<String> arch = new List<string>();
            List<string> archtemp = new List<string>();
            /*FtpClient ftp = ClientFtp44();*/
            try
            {
                WorkWithFtp ftp = ClientFtp44_old();
                ftp.ChangeWorkingDirectory(PathParse);
                archtemp = ftp.ListDirectory();
            }
            catch (Exception e)
            {
                Log.Logger("Не могу найти директорию", PathParse);
            }
            foreach (var a in archtemp)
            {
                using (MySqlConnection connect = ConnectToDb.GetDBConnection())
                {
                    connect.Open();
                    string select_arch =
                        $"SELECT id FROM {Program.Prefix}archiv_plan_graphs WHERE arhiv = @archive";
                    MySqlCommand cmd = new MySqlCommand(select_arch, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@archive", a);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    bool res_read = reader.HasRows;
                    reader.Close();
                    if (!res_read)
                    {
                        string add_arch =
                            $"INSERT INTO {Program.Prefix}archiv_plan_graphs SET arhiv = @archive";
                        MySqlCommand cmd1 = new MySqlCommand(add_arch, connect);
                        cmd1.Prepare();
                        cmd1.Parameters.AddWithValue("@archive", a);
                        cmd1.ExecuteNonQuery();
                        arch.Add(a);
                    }
                }
            }
            return arch;
        }

        public override List<String> GetListArchPrev(string PathParse, string RegionPath)
        {
            List<String> arch = new List<string>();
            List<string> archtemp = new List<string>();
            /*FtpClient ftp = ClientFtp44();*/
            try
            {
                WorkWithFtp ftp = ClientFtp44_old();
                ftp.ChangeWorkingDirectory(PathParse);
                archtemp = ftp.ListDirectory();
            }
            catch (Exception e)
            {
                Log.Logger("Не могу найти директорию", PathParse);
            }
            string serachd = $"{Program.LocalDate:yyyyMMdd}";
            foreach (var a in archtemp.Where(a => a.IndexOf(serachd, StringComparison.Ordinal) != -1))
            {
                string prev_a = $"prev_{a}";
                using (MySqlConnection connect = ConnectToDb.GetDBConnection())
                {
                    connect.Open();
                    string select_arch =
                        $"SELECT id FROM {Program.Prefix}archiv_plan_graphs WHERE arhiv = @archive";
                    MySqlCommand cmd = new MySqlCommand(select_arch, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@archive", prev_a);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    bool res_read = reader.HasRows;
                    reader.Close();
                    if (!res_read)
                    {
                        string add_arch =
                            $"INSERT INTO {Program.Prefix}archiv_plan_graphs SET arhiv = @archive";
                        MySqlCommand cmd1 = new MySqlCommand(add_arch, connect);
                        cmd1.Prepare();
                        cmd1.Parameters.AddWithValue("@archive", prev_a);
                        cmd1.ExecuteNonQuery();
                        arch.Add(a);
                    }
                }
            }
            return arch;
        }
    }
}