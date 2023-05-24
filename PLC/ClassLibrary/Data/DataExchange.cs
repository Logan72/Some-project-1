using ClassLibrary.MySQL_connection;
using ClassLibrary.Sever_connection.Keyence_PLC;
using ClassLibrary.Sever_connection.Mitsubishi_PLC;
using ClassLibrary.Sever_connection.Omron_PLC;
using ClassLibrary.Supporting_functions;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary.Data
{
    public abstract class DataExchange
    {        
        public static void executeNonQuery(string query)
        {
            var dBConnection = new DBConnection();

            if (dBConnection.IsConnect())
            {                  
                try
                {
                    var command = new MySqlCommand(query, dBConnection.Connection);
                    command.ExecuteNonQuery();
                }
                catch (MySqlException ex)
                {
                    //Console.WriteLine("*MySqlException");
                    dBConnection.Close();
                }
                catch (InvalidOperationException ex)
                {
                    //Console.WriteLine("*InvalidOperationException");
                    dBConnection.Close();
                }

                dBConnection.Close();
            }
        }

        public static void executeAllCommands(ConcurrentDictionary<string, bool> flags)
        {
            var dBConnection = new DBConnection();

            if (dBConnection.IsConnect())
            {
                string str;

                List<string> machineList = AccessIni.getKeys("Machines");

                string group = "";

                try
                {
                    foreach (string machine in machineList)
                    {
                        group += "'" + machine.Trim('\0') + "',";
                    }

                    group = group.Substring(0, group.Length - 1);

                    str = "SELECT m.Machine_Id, m.IP, m.Port, m_de.Command, m_de.Command_Id, m_da.Value, m_de.Command_Type, m_de.ValueType, m.Type_Id, m_de.ValueNumber " +
                            "FROM ((machine as m " +
                            "INNER JOIN machine_detail as m_de ON m.Machine_Id = m_de.Machine_Id) " +
                            "LEFT JOIN machine_data as m_da ON m_de.Command_Id = m_da.Command_Id) " +
                            "WHERE m.Machine_Id in (" + group + ") " +
                            "ORDER BY m.Machine_Id;";
                }
                catch (NullReferenceException ex)
                {
                    str = "CALL proc_getCommands();"; //Stored procedure. Selected columns: (0) Machine_Id | (1) IP | (2) Port | (3) Command | (4) Command_Id | (5) Value | (6) Command_Type | (7) ValueType | (8) Type_Id | (9) ValueNumber
                }              
                              
                List <CommandData> list = new List<CommandData>();

                string previousMachine = null;                

                try
                {
                    var command = new MySqlCommand(str, dBConnection.Connection);

                    var reader = command.ExecuteReader();

                    while (reader.Read()) //Retrieving the data of commands from DB
                    {
                        bool flag;

                        if (flags.TryGetValue(reader.GetString(0), out flag) && !flag) continue;

                        CommandData commandData = new CommandData();

                        commandData.Machine_Id = reader.GetString(0);
                        commandData.IP = reader.GetString(1);
                        commandData.Port = reader.GetInt32(2);
                        commandData.Command = reader.GetString(3);
                        commandData.Command_Id = reader.GetString(4);
                        commandData.Command_Type = reader.GetString(6);
                        if (reader.GetString(6).Equals("W")) commandData.Value = reader.GetString(5);
                        commandData.ValueType = reader.GetInt32(7);
                        commandData.Type_Id = reader.GetString(8);
                        commandData.ValueNumber = reader.GetInt32(9);

                        if (previousMachine != reader.GetString(0)) // forming lists of commands belonging to each machine
                        {
                            previousMachine = reader.GetString(0);

                            if (list.Count != 0)
                            {
                                List<CommandData> clone = new List<CommandData>(list);

                                list.Clear();

                                createRunThread(clone, flags);
                            }

                            list.Add(commandData);
                        }
                        else
                        {
                            list.Add(commandData);
                        }
                    }

                    reader.Close();
                    reader.Dispose();

                    new MySqlCommand("CALL proc_writeHistory();", dBConnection.Connection).ExecuteNonQuery();
                }
                catch (MySqlException ex)
                {
                    //Console.WriteLine("*MySqlException");
                    dBConnection.Close();
                }
                catch (InvalidOperationException ex)
                {
                    //Console.WriteLine("*InvalidOperationException");
                    dBConnection.Close();
                }                

                if (list.Count != 0) // dealing with the last list that can't be dealt with in the above while loop
                {
                    createRunThread(list, flags);
                }                

                dBConnection.Close();
            }
        }

        private static async void createRunThread(List<CommandData> list, ConcurrentDictionary<string, bool> flags) //create and run a thread for each machine
        {
            flags.AddOrUpdate(list[0].Machine_Id, false, (k, v) => false);

            Thread thread = new Thread(new ThreadStart(async delegate ()
            {
                switch (list[0].Type_Id)
                {
                    case "001": //keyence PLC
                        Communication_PLC communication_PLC = new Communication_PLC();

                        if (communication_PLC.TcpConnect(list[0].IP, list[0].Port))
                        {
                            flags.TryUpdate(list[0].Machine_Id, true, false);

                            foreach (CommandData commandData in list)
                            {
                                string data;

                                if (commandData.Command_Type.Equals("R"))
                                {
                                    data = communication_PLC.SendCommand(commandData.Command);

                                    if (!(data.Equals("timeout") || data.Equals("not connect")))
                                    {
                                        data = data.Replace("\r", "").Replace("\n", "");
                                        
                                        switch (commandData.ValueType)
                                        {
                                            case 1:
                                                data = data.TrimStart('0');
                                                executeNonQuery("CALL proc_insertUpdate('" + commandData.Machine_Id + "', '" + commandData.Command_Id + "', '" + data + "');");
                                                break;
                                            case 2:
                                                try
                                                {
                                                    data = Support.HexStringToString(data);
                                                    executeNonQuery("CALL proc_insertUpdate('" + commandData.Machine_Id + "', '" + commandData.Command_Id + "', '" + data + "');");
                                                }
                                                catch (Exception ex)
                                                {
                                                    Console.WriteLine("there's something wrong");
                                                }
                                                break;                                            
                                        }                                        
                                    }
                                    else
                                    {
                                        Console.WriteLine(++CommandData.errorCount + "########################################################################################################\n########################################################################################################");
                                        //throw new Exception(data);
                                    }

                                    Console.WriteLine(data + " =====FROM===== " + commandData.IP + " (" + commandData.Command + ") " + data.Length);
                                }
                                else if (commandData.Command_Type.Equals("W"))
                                {
                                    if (commandData.ValueType == 2)
                                    {
                                        commandData.Value = Support.StringToHexString(commandData.Value);
                                    }

                                    data = communication_PLC.SendCommand(commandData.Command + " " + commandData.Value);

                                    if (!(data.Equals("timeout") || data.Equals("not connect")))
                                    {
                                        data = data.Replace("\r", "").Replace("\n", "");
                                    }
                                    else
                                    {
                                        Console.WriteLine(++CommandData.errorCount + "########################################################################################################\n########################################################################################################");
                                        //throw new Exception(data);
                                    }

                                    Console.WriteLine("writing " + commandData.IP + " (" + commandData.Command + ") " + data);
                                }
                            }

                            communication_PLC.TcpClose();
                        }
                        else
                        {
                            Console.WriteLine(list[0].IP + " is disconnected.");
                            flags.TryUpdate(list[0].Machine_Id, true, false);
                            //throw new Exception(list[0].IP + " is disconnected");
                        }

                        break;

                    case "002": //mitsubishi PLC
                        McProtocolTcp mcProtocolTcp = new McProtocolTcp(list[0].IP, 5040, McFrame.MC3E);

                        if (await mcProtocolTcp.Open() == 0)
                        {
                            flags.TryUpdate(list[0].Machine_Id, true, false);

                            foreach (CommandData commandData in list)
                            {
                                string[] commandArg = commandData.Command.Split(' ');

                                if (commandData.Command_Type.Equals("R"))
                                {
                                    try
                                    {
                                        string data;

                                        data = await mcProtocolTcp.ReadAndGetResult(commandArg[0], Convert.ToInt32(commandArg[1]), commandData.ValueType, commandData.ValueNumber);

                                        executeNonQuery("CALL proc_insertUpdate('" + commandData.Machine_Id + "', '" + commandData.Command_Id + "', '" + data + "');");
                                        Console.WriteLine(data + " =====FROM===== " + commandData.IP + " (" + commandData.Command + ") " + data.Length);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(++CommandData.errorCount + "########################################################################################################\n########################################################################################################");
                                        Console.WriteLine("error =====FROM===== " + commandData.IP + " (" + commandData.Command + ")");
                                        Console.WriteLine(ex.Message);
                                    }
                                }
                                else if (commandData.Command_Type.Equals("W"))
                                {
                                    try
                                    {
                                        await mcProtocolTcp.WriteDeviceBlock(commandArg[0], Convert.ToInt16(commandData.Value));
                                        Console.WriteLine("writing " + commandData.Value + " into " + commandData.IP + " (" + commandData.Command + ") OK");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(++CommandData.errorCount + "########################################################################################################\n########################################################################################################");
                                        Console.WriteLine("writing " + commandData.Value + " into " + commandData.IP + " (" + commandData.Command + ") error");
                                        Console.WriteLine(ex.Message);
                                    }
                                }
                            }

                            mcProtocolTcp.Dispose();
                        }
                        else
                        {
                            Console.WriteLine(list[0].IP + " is disconnected.");
                            flags.TryUpdate(list[0].Machine_Id, true, false);
                            //throw new Exception(list[0].IP + " is disconnected");
                        }

                        break;

                    case "003": //omron PLC
                        OmronConnection omronConnection = new OmronConnection(list[0].IP, list[0].Port);

                        if (omronConnection.Connected)
                        {
                            flags.TryUpdate(list[0].Machine_Id, true, false);

                            foreach (CommandData commandData in list)
                            {
                                string[] commandArg = commandData.Command.Split(' ');

                                if (commandData.Command_Type.Equals("R"))
                                {
                                    try
                                    {
                                        string data;

                                        data = omronConnection.ReadDM(commandArg[0], Convert.ToInt32(commandArg[1]), Convert.ToInt32(commandArg[2]), commandData.ValueType);

                                        executeNonQuery("CALL proc_insertUpdate('" + commandData.Machine_Id + "', '" + commandData.Command_Id + "', '" + data + "');");
                                        Console.WriteLine(data + " =====FROM===== " + commandData.IP + " (" + commandData.Command + ") " + data.Length);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(++CommandData.errorCount + "########################################################################################################\n########################################################################################################");
                                        Console.WriteLine("error =====FROM===== " + commandData.IP + " (" + commandData.Command + ")");
                                        Console.WriteLine(ex.Message);
                                    }
                                }
                                else if (commandData.Command_Type.Equals("W"))
                                {
                                    try
                                    {
                                        if (omronConnection.WriteDM(commandArg[0], Convert.ToInt32(commandArg[1]), Convert.ToInt32(commandArg[2]), commandData.Value, commandData.ValueType))
                                        {
                                            Console.WriteLine("writing " + commandData.Value + " into " + commandData.IP + " (" + commandData.Command + ") OK");
                                        }
                                        else Console.WriteLine("writing " + commandData.Value + " into " + commandData.IP + " (" + commandData.Command + ") error");

                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(++CommandData.errorCount + "########################################################################################################\n########################################################################################################");
                                        Console.WriteLine("writing " + commandData.Value + " into " + commandData.IP + " (" + commandData.Command + ") error");
                                        Console.WriteLine(ex.Message);
                                    }
                                }
                            }

                            omronConnection.Close();
                        }
                        else
                        {
                            Console.WriteLine(list[0].IP + " is disconnected.");
                            flags.TryUpdate(list[0].Machine_Id, true, false);
                            //throw new Exception(list[0].IP + " is disconnected");
                        }

                        break;
                }                
            }));

            thread.Start();
        }
    }
}
