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
        protected TypeArguments Arg;

        public Parser(TypeArguments a)
        {
            Arg = a;
        }

        public virtual void Parsing()
        {
        }

        public DataTable GetRegions()
        {
            var reg = "SELECT * FROM region";
            DataTable dt;
            using (var connect = ConnectToDb.GetDbConnection())
            {
                connect.Open();
                var adapter = new MySqlDataAdapter(reg, connect);
                var ds = new DataSet();
                adapter.Fill(ds);
                dt = ds.Tables[0];
            }

            return dt;
        }

        public virtual void GetListFileArch(string arch, string pathParse, string region, int regionId)
        {
        }

        public string GetArch44(string arch, string pathParse)
        {
            var file = "";
            var count = 1;
            while (true)
            {
                try
                {
                    var fileOnServer = $"{arch}";
                    file = $"{Program.TempPath}{Path.DirectorySeparatorChar}{arch}";
                    using (var ftp = ClientFtp44())
                    {
                        ftp.SetWorkingDirectory(pathParse);
                        ftp.DownloadFile(file, fileOnServer);
                        ftp.Disconnect();
                    }

                    if (count > 1)
                    {
                        Log.Logger("Удалось скачать архив после попытки", count);
                    }

                    return file;
                }
                catch (Exception e)
                {
                    Log.Logger("Не удалось скачать файл", arch, e);
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
            var client = new FtpClient("ftp://ftp.zakupki.gov.ru", "free", "VNIMANIE!_otkluchenie_FTP_s_01_01_2025_podrobnee_v_ATFF");
            client.Connect();
            return client;
        }

        public virtual void Bolter(FileInfo f, string region, int regionId, TypeFile44 typefile)
        {
        }

        public virtual List<string> GetListArchLast(string pathParse, string regionPath)
        {
            var arch = new List<string>();

            return arch;
        }

        public virtual List<string> GetListArchCurr(string pathParse, string regionPath)
        {
            var arch = new List<string>();

            return arch;
        }

        public virtual List<string> GetListArchPrev(string pathParse, string regionPath)
        {
            var arch = new List<string>();

            return arch;
        }

        protected WorkWithFtp ClientFtp44_old()
        {
            var ftpCl = new WorkWithFtp("ftp://ftp.zakupki.gov.ru", "free", "VNIMANIE!_otkluchenie_FTP_s_01_01_2025_podrobnee_v_ATFF");
            return ftpCl;
        }
    }
}