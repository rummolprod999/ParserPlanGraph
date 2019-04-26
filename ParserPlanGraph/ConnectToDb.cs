using MySql.Data.MySqlClient;

namespace ParserPlanGraph
{
    public class ConnectToDb
    {
        public static MySqlConnection GetDbConnection()
        {
            // Connection String.
            var connString =
                $"Server={Program.Server};port={Program.Port};Database={Program.Database};User Id={Program.User};password={Program.Pass};CharSet=utf8;Convert Zero Datetime=True;default command timeout=900;Connection Timeout=900;SslMode=none";

            var conn = new MySqlConnection(connString);

            return conn;
        }
    }
}