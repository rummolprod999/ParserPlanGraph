using System;
using System.IO;
using System.Xml;

namespace ParserPlanGraph
{
    public class GetSettings
    {
        public readonly string Database;
        public readonly string LogPathPlan44;
        public readonly string PassDb;
        public readonly int Port;
        public readonly string Prefix;
        public readonly string Server;
        public readonly string TempPathPlan44;
        public readonly string UserDb;
        public readonly string Years;

        public GetSettings()
        {
            var xDoc = new XmlDocument();
            xDoc.Load(Program.PathProgram + Path.DirectorySeparatorChar + "setting_plan.xml");
            var xRoot = xDoc.DocumentElement;
            if (xRoot != null)
            {
                foreach (XmlNode xnode in xRoot)
                {
                    switch (xnode.Name)
                    {
                        case "database":
                            Database = xnode.InnerText;
                            break;
                        case "tempdir_plan44":
                            TempPathPlan44 = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "logdir_plan44":
                            LogPathPlan44 = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "prefix":
                            Prefix = xnode.InnerText;
                            break;
                        case "userdb":
                            UserDb = xnode.InnerText;
                            break;
                        case "passdb":
                            PassDb = xnode.InnerText;
                            break;
                        case "server":
                            Server = xnode.InnerText;
                            break;
                        case "port":
                            Port = int.TryParse(xnode.InnerText, out Port) ? int.Parse(xnode.InnerText) : 3306;
                            break;
                        case "years":
                            Years = xnode.InnerText;
                            break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(LogPathPlan44) && !string.IsNullOrEmpty(TempPathPlan44) &&
                !string.IsNullOrEmpty(Database) && !string.IsNullOrEmpty(UserDb) && !string.IsNullOrEmpty(Server) &&
                !string.IsNullOrEmpty(Years)) return;
            Console.WriteLine("Некоторые поля в файле настроек пустые");
            Environment.Exit(0);
        }
    }
}