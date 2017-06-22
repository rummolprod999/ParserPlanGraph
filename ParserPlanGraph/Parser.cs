using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using FluentFTP;
using MySql.Data.MySqlClient;

namespace ParserPlanGraph
{
    public class Parser : IParser
    {
        protected TypeArguments arg;

        public Parser(TypeArguments a)
        {
            this.arg = a;
        }

        public virtual void Parsing()
        {
        }

        public DataTable GetRegions()
        {
            string reg = "SELECT * FROM region";
            DataTable dt;
            using (MySqlConnection connect = ConnectToDb.GetDBConnection())
            {
                connect.Open();
                MySqlDataAdapter adapter = new MySqlDataAdapter(reg, connect);
                DataSet ds = new DataSet();
                adapter.Fill(ds);
                dt = ds.Tables[0];
            }
            return dt;
        }

        public virtual void GetListFileArch(string Arch, string PathParse, string region, int region_id)
        {
        }

        public string GetArch44(string Arch, string PathParse)
        {
            string file = "";
            int count = 1;
            while (true)
            {
                try
                {
                    /*string FileOnServer = $"{PathParse}/{Arch}";*/
                    string FileOnServer = $"{Arch}";
                    file = $"{Program.TempPath}{Path.DirectorySeparatorChar}{Arch}";
                    FtpClient ftp = ClientFtp44();
                    ftp.SetWorkingDirectory(PathParse);
                    ftp.DownloadFile(file, FileOnServer);
                    ftp.Disconnect();
                    if (count > 1)
                    {
                        Log.Logger("Удалось скачать архив после попытки", count);
                    }
                    return file;
                }
                catch (Exception e)
                {
                    Log.Logger("Не удалось скачать файл", Arch, e);
                    if (count > 50)
                    {
                        return file;
                    }

                    count++;
                    Thread.Sleep(5000);
                }
            }
        }

        public FtpClient ClientFtp44()
        {
            FtpClient client = new FtpClient("ftp://ftp.zakupki.gov.ru", "free", "free");
            client.Connect();
            return client;
        }

        public virtual void Bolter(FileInfo f, string region, int region_id)
        {
        }

        public virtual List<String> GetListArchLast(string PathParse, string RegionPath)
        {
            List<String> arch = new List<string>();

            return arch;
        }

        public virtual List<String> GetListArchCurr(string PathParse, string RegionPath)
        {
            List<String> arch = new List<string>();

            return arch;
        }

        public virtual List<String> GetListArchPrev(string PathParse, string RegionPath)
        {
            List<String> arch = new List<string>();

            return arch;
        }
        public WorkWithFtp ClientFtp44_old()
        {
            WorkWithFtp ftpCl = new WorkWithFtp("ftp://ftp.zakupki.gov.ru", "free", "free");
            return ftpCl;
        }

        
    }
}