using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary.MySQL_connection
{
    public class DBConnection
    {
        string iniPath = Directory.GetCurrentDirectory() + "\\Config.ini";
        private string server;
        private string database;
        private string user;
        private string password;

        private MySqlConnection connection = null;
        public DBConnection()
        {
        }

        public string Server { get; set; }
        public string DatabaseName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public MySqlConnection Connection { get { return connection; } }

        //private DBConnection _instance = null;

        //public DBConnection Instance()
        //{
        //    if (_instance == null)
        //        _instance = new DBConnection();
        //    return _instance;
        //}

        public bool IsConnect()
        {
            StringBuilder sb = new StringBuilder();
            
            AccessIni.GetPrivateProfileString("MySQL", "server", "", sb, 32, iniPath);
            server = sb.ToString();

            AccessIni.GetPrivateProfileString("MySQL", "database", "", sb, 32, iniPath);
            database = sb.ToString();
            DatabaseName = database;

            AccessIni.GetPrivateProfileString("MySQL", "user", "", sb, 32, iniPath);
            user = sb.ToString();

            AccessIni.GetPrivateProfileString("MySQL", "password", "", sb, 32, iniPath);
            password = sb.ToString();

            if (connection == null)
            {
                if (string.IsNullOrEmpty(DatabaseName))
                    return false;
                string connstring = string.Format("server={0}; database={1}; user={2}; password={3};", server, database, user, password);

                connection = new MySqlConnection(connstring);
                try
                {
                    connection.Open();
                }
                catch (Exception ex)
                {
                    //Console.WriteLine("*DBConnection");
                    connection = null;
                    return false;
                }
            }

            return true;
        }

        public void Close()
        {
            Connection.Close();
            //Connection.Dispose();     
        }
    }
}
