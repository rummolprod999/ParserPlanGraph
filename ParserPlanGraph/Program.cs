using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ParserPlanGraph
{
    internal class Program
    {
        public static readonly DateTime LocalDate = DateTime.Now;
        public static string FileLog;
        public static string StrArg { get; private set; }
        public static TypeArguments Periodparsing;
        public static string PathProgram;
        public static int AddPlan44 = 0;
        public static int UpdatePlan44 = 0;
        public static int AddPlanCancel44 = 0;
        public static int AddPlanChange44 = 0;
        public static string Database { get; private set; }

        public static string Prefix { get; private set; }

        public static string User { get; private set; }

        public static string Pass { get; private set; }

        public static string Server { get; private set; }

        public static int Port { get; private set; }

        private static List<string> Years { get; } = new List<string>();

        public static string TempPath { get; private set; }

        private static string LogPath { get; set; }

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine(
                    "Недостаточно аргументов для запуска, используйте last44, prev44, curr44 в качестве аргумента");
                return;
            }

            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName()
                .CodeBase);
            if (path != null) PathProgram = path.Substring(5);
            StrArg = args[0];
            switch (args[0])
            {
                case "last44":
                    Periodparsing = TypeArguments.Last44;
                    Init(Periodparsing);
                    ParserPlan44();
                    break;
                case "prev44":
                    Periodparsing = TypeArguments.Prev44;
                    Init(Periodparsing);
                    ParserPlan44();
                    break;
                case "curr44":
                    Periodparsing = TypeArguments.Curr44;
                    Init(Periodparsing);
                    ParserPlan44();
                    break;
                default:
                    Console.WriteLine(
                        "Неправильно указан аргумент, используйте last44, prev44, curr44");
                    break;
            }
        }

        private static void Init(TypeArguments arg)
        {
            var set = new GetSettings();
            Database = set.Database;
            LogPath = set.LogPathPlan44;
            Prefix = set.Prefix;
            User = set.UserDb;
            Pass = set.PassDb;
            TempPath = set.TempPathPlan44;
            Server = set.Server;
            Port = set.Port;
            var tmp = set.Years;
            var tempYears = tmp.Split(',');

            foreach (var s in tempYears.Select(v => $"_{v.Trim()}"))
            {
                Years.Add(s);
            }

            if (string.IsNullOrEmpty(TempPath) || string.IsNullOrEmpty(LogPath))
            {
                Console.WriteLine("Не получится создать папки для парсинга");
                Environment.Exit(0);
            }

            if (Directory.Exists(TempPath))
            {
                var dirInfo = new DirectoryInfo(TempPath);
                dirInfo.Delete(true);
                Directory.CreateDirectory(TempPath);
            }
            else
            {
                Directory.CreateDirectory(TempPath);
            }

            if (!Directory.Exists(LogPath))
            {
                Directory.CreateDirectory(LogPath);
            }

            if (arg == TypeArguments.Curr44 || arg == TypeArguments.Last44 || arg == TypeArguments.Prev44)
                FileLog = $"{LogPath}{Path.DirectorySeparatorChar}Plan44_{LocalDate:dd_MM_yyyy}.log";
        }

        private static void ParserPlan44()
        {
            Log.Logger("Время начала парсинга Plan44");
            var p44 = new ParserPlan44(Periodparsing);
            p44.Parsing();
            /*ParserPlan44 p44 = new ParserPlan44(Periodparsing);
            FileInfo f = new FileInfo("/home/alex/Рабочий стол/parser/tenderPlan2017_2017017610000440010001_1549630.xml");
            p44.ParsingXML(f, "moskow", 50, TypeFile44.Plan);*/
            Log.Logger("Добавили Plan44", AddPlan44);
            Log.Logger("Обновили Plan44", UpdatePlan44);
            Log.Logger("Добавили PlanCancel44", AddPlanCancel44);
            Log.Logger("Добавили PlanChange44", AddPlanChange44);
            Log.Logger("Время окончания парсинга Plan44");
        }
    }
}