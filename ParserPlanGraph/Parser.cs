using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using FluentFTP;
using MySql.Data.MySqlClient;
using Limilabs.FTP.Client;

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
            var reg = "SELECT * FROM region";
            DataTable dt;
            using (var connect = ConnectToDb.GetDBConnection())
            {
                connect.Open();
                var adapter = new MySqlDataAdapter(reg, connect);
                var ds = new DataSet();
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
            var file = "";
            var count = 1;
            while (true)
            {
                try
                {
                    /*string FileOnServer = $"{PathParse}/{Arch}";*/
                    var FileOnServer = $"{Arch}";
                    file = $"{Program.TempPath}{Path.DirectorySeparatorChar}{Arch}";
                    /*FtpClient ftp = ClientFtp44();
                    ftp.SetWorkingDirectory(PathParse);
                    ftp.DownloadFile(file, FileOnServer);
                    ftp.Disconnect();*/
                    using (var client = new Ftp())
                    {
                        client.Connect("ftp.zakupki.gov.ru");    // or ConnectSSL for SSL
                        client.Login("free", "free");
                        client.ChangeFolder(PathParse);
                        client.Download(FileOnServer, file);

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
            var client = new FtpClient("ftp://ftp.zakupki.gov.ru", "free", "free");
            client.Connect();
            return client;
        }

        public virtual void Bolter(FileInfo f, string region, int region_id, TypeFile44 typefile)
        {
        }

        public virtual List<String> GetListArchLast(string PathParse, string RegionPath)
        {
            var arch = new List<string>();

            return arch;
        }

        public virtual List<String> GetListArchCurr(string PathParse, string RegionPath)
        {
            var arch = new List<string>();

            return arch;
        }

        public virtual List<String> GetListArchPrev(string PathParse, string RegionPath)
        {
            var arch = new List<string>();

            return arch;
        }
        public WorkWithFtp ClientFtp44_old()
        {
            var ftpCl = new WorkWithFtp("ftp://ftp.zakupki.gov.ru", "free", "free");
            return ftpCl;
        }

        
    }
}