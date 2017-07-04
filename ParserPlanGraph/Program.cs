using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ParserPlanGraph
{
    internal class Program
    {
        private static string _database;
        private static string _tempPath44;
        private static string _logPath44;
        private static string _prefix;
        private static string _user;
        private static string _pass;
        private static string _server;
        private static int _port;
        private static List<string> _years = new List<string>();
        public static string Database => _database;
        public static string Prefix => _prefix;
        public static string User => _user;
        public static string Pass => _pass;
        public static string Server => _server;
        public static int Port => _port;
        public static List<string> Years => _years;
        public static readonly DateTime LocalDate = DateTime.Now;
        public static string FileLog;
        public static string StrArg;
        public static TypeArguments Periodparsing;
        public static string PathProgram;
        public static string TempPath => _tempPath44;
        public static string LogPath => _logPath44;
        public static int AddPlan44 = 0;
        public static int AddPlanCancel44 = 0;
        public static int AddPlanChange44 = 0;

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine(
                    "Недостаточно аргументов для запуска, используйте last44, prev44, curr44 в качестве аргумента");
                return;
            }

            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName()
                .CodeBase);
            if (path != null) PathProgram = path.Substring(5);
            StrArg = args[0];
            switch (args[0])
            {
                case "last44":
                    Periodparsing = TypeArguments.Last44;
                    Init(Periodparsing);
                    ParserPlan44(Periodparsing);
                    break;
                case "prev44":
                    Periodparsing = TypeArguments.Prev44;
                    Init(Periodparsing);
                    ParserPlan44(Periodparsing);
                    break;
                case "curr44":
                    Periodparsing = TypeArguments.Curr44;
                    Init(Periodparsing);
                    ParserPlan44(Periodparsing);
                    break;
                default:
                    Console.WriteLine(
                        "Неправильно указан аргумент, используйте last44, prev44, curr44");
                    break;
            }
        }
        
        private static void Init(TypeArguments arg)
        {
            GetSettings set = new GetSettings();
            _database = set.Database;
            _logPath44 = set.LogPathPlan44;
            _prefix = set.Prefix;
            _user = set.UserDB;
            _pass = set.PassDB;
            _tempPath44 = set.TempPathPlan44;
            _server = set.Server;
            _port = set.Port;
            string tmp = set.Years;
            string[] temp_years = tmp.Split(new char[] {','});

            foreach (var s in temp_years.Select(v => $"_{v.Trim()}"))
            {
                _years.Add(s);
            }
            if (String.IsNullOrEmpty(TempPath) || String.IsNullOrEmpty(LogPath))
            {
                Console.WriteLine("Не получится создать папки для парсинга");
                Environment.Exit(0);
            }

            if (Directory.Exists(TempPath))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(TempPath);
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

        private static void ParserPlan44(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга Plan44");
            /*ParserPlan44 p44 = new ParserPlan44(Periodparsing);
            p44.Parsing();*/
            ParserPlan44 p44 = new ParserPlan44(Periodparsing);
            FileInfo f = new FileInfo("/home/alex/Рабочий стол/parser/tenderPlan2017_2016017330000410020002_688.xml");
            p44.ParsingXML(f, "moskow", 50, TypeFile44.Plan);
            Log.Logger("Добавили Plan44", AddPlan44);
            Log.Logger("Добавили PlanCancel44", AddPlanCancel44);
            Log.Logger("Добавили PlanChange44", AddPlanChange44);
            Log.Logger("Время окончания парсинга Plan44");
            
        }
    }
}