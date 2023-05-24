using ClassLibrary.Data;
using ClassLibrary;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using Org.BouncyCastle.Crypto.IO;
using Org.BouncyCastle.Tls;
using System.Collections.Concurrent;
using System.Data.Common;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Timers;
using Timer = System.Timers.Timer;

namespace PLCproject
{
    class Program
    {
        static ConcurrentDictionary<string, bool> flags;
        
        static void Main(string[] args)
        {
            flags = new ConcurrentDictionary<string, bool>();
            CommandData.errorCount = 0;

            Timer timer = new Timer(1000);
            timer.Elapsed += delegate (Object o, ElapsedEventArgs e)
            {
                DataExchange.executeAllCommands(flags);
            };
            timer.AutoReset = false;
            timer.Enabled = true;

            if (Console.ReadKey().Key == ConsoleKey.Enter)
            {
                Console.WriteLine("The number of \"timeout\" and \"not connect\": " + CommandData.errorCount);
                Environment.Exit(0);
            }

            //Console.ReadLine();
        }                        
    }
}