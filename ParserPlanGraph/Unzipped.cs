using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace ParserPlanGraph
{
    public class Unzipped
    {
        public static string Unzip(string filea)
        {
            var fileInf = new FileInfo(filea);
            if (fileInf.Exists)
            {
                var rPoint = filea.LastIndexOf('.');
                var lDir = filea.Substring(0, rPoint);
                Directory.CreateDirectory(lDir);
                try
                {
                    ZipFile.ExtractToDirectory(filea, lDir);
                    fileInf.Delete();
                    return lDir;
                }
                catch (Exception e)
                {
                    Log.Logger("Не удалось извлечь файл", e, filea);
                    try
                    {
                        var myProcess = new Process {StartInfo = new ProcessStartInfo("unzip", $"{filea} -d {lDir}")};
                        myProcess.Start();
                        myProcess.WaitForExit();
                        Log.Logger("Извлекли файл альтернативным методом", filea);
                        return lDir;
                    }
                    catch (Exception exception)
                    {
                        Log.Logger("Не удалось извлечь файл альтернативным методом", exception, filea);
                        return lDir;
                    }
                }
            }

            return "";
        }
    }
}