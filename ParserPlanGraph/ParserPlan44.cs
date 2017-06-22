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
    public class ParserPlan44: Parser
    {
        protected DataTable DtRegion;
        
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
            
            return archtemp;
        }
    }
}