using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using FluentFTP;
using Limilabs.FTP.Client;
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
                    /*string FileOnServer = $"{PathParse}/{Arch}";*/
                    var fileOnServer = $"{arch}";
                    file = $"{Program.TempPath}{Path.DirectorySeparatorChar}{arch}";
                    /*FtpClient ftp = ClientFtp44();
                    ftp.SetWorkingDirectory(PathParse);
                    ftp.DownloadFile(file, FileOnServer);
                    ftp.Disconnect();*/
                    using (var client = new Ftp())
                    {
                        client.Connect("ftp.zakupki.gov.ru"); // or ConnectSSL for SSL
                        client.Login("free", "free");
                        client.ChangeFolder(pathParse);
                        client.Download(fileOnServer, file);

                        client.Close();
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
            var client = new FtpClient("ftp://ftp.zakupki.gov.ru", "free", "free");
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
            var ftpCl = new WorkWithFtp("ftp://ftp.zakupki.gov.ru", "free", "free");
            return ftpCl;
        }
    }
}