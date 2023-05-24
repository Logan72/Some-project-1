using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ClassLibrary.Supporting_functions;
using MySqlX.XDevAPI.Common;
using Org.BouncyCastle.Utilities;
using System.Text.RegularExpressions;

namespace ClassLibrary.Sever_connection.Omron_PLC
{
    //public enum MemoryAreaType
    //{
    //    DM,
    //    CIO
    //}
    public class OmronConnection
    {
        private Socket Ethernet_Socket;
        private IPEndPoint Remote_IPEndPoint;
        private string ip;
        
        public bool Connected
        {
            get { return Ethernet_Socket.Connected; }
        }

        public OmronConnection(string ip, int port)
        {
            this.ip = ip;
            Remote_IPEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            Ethernet_Socket = new Socket(Remote_IPEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            Ethernet_Socket.Connect(Remote_IPEndPoint);
            Thread.Sleep(50);
        }        

        public void Close()
        {
            Ethernet_Socket.Close();
        }

        public string PLC_Ping(string ip)
        {
            Ping pingSender = new Ping();
            IPAddress Remote_IP;

            try
            {
                Remote_IP = IPAddress.Parse(ip);
            }
            catch (Exception e)
            {
                throw new Exception("Indirizzo IP PLC Invalido" + e);
            }

            PingReply reply = pingSender.Send(Remote_IP);

            if (reply.Status == IPStatus.Success)
            {
                string txt = "";
                txt += "IP:      " + reply.Address.ToString() + "\r\n";
                txt += "RTT:   " + reply.RoundtripTime + "ms \r\n";
                txt += "TTL:   " + reply.Options.Ttl + "\r\n";

                return "Ping Andato a buon fine : \r\n\r\n" + txt;
            }
            else
            {
                throw new Exception("Indirizzo IP non raggiungibile");
            }
        }

        /// <summary>
        /// Write value in DM
        /// </summary>
        /// <param name="S_ip">Source IP: Ip of the PC</param>
        /// <param name="S_port">Source Port: Port of the PC (9600)</param>
        /// <param name="D_ip">Destination IP: IP of the PLC</param>
        /// <param name="D_port">Destination Port: Port of the PLC (UDP Port default: 9600)</param>
        /// <param name="dmnum">DM Position: (DM100)</param>
        /// <param name="value">Value of the DM</param>
        /// <returns>True if there aren't error </returns>
        public bool WriteDM(string memoryAreaType, int dmnum, int qnt, string value, int valueType)
        {
            string ICF = "80", RSV = "00", GTC = "02", DNA = "00", DA1 = "00", DA2 = "00", SNA = "00", SA1 = "0A", SA2 = "00", SID = "AA";
            int k = 0, retval;
            byte[] ByteArray;
            byte[] myByte = new byte[1024];
            string Header, tmp1, CMND;            

            //Random rnd = new Random();
            //string SID = rnd.Next(1, 99).ToString("00");

            string[] D_parts = ip.Split('.');
            int D_fin = Convert.ToInt16(D_parts[3]);

            DA1 = D_fin.ToString("x2").ToUpper();
            
            Header = ICF + RSV + GTC + DNA + DA1 + DA2 + SNA + SA1 + SA2 + SID;            

            string cmd_write = "0102";
            string cmd_area;

            switch (memoryAreaType)
            {
                case "DM":
                    cmd_area = "82";
                    break;
                case "CIO":
                    cmd_area = "B0";
                    break;
                default:
                    cmd_area = "82";
                    break;
            }

            string cmd_startaddress = dmnum.ToString("x4");
            string cmd_byteempty = "00";
            string cmd_wordnumber = qnt.ToString("x4");
            string cmd_value;
            if (valueType == 2)
            {
                byte[] valueBytes = Encoding.ASCII.GetBytes(value);                

                cmd_value = Convert.ToHexString(valueBytes);
            }
            else
            {
                cmd_value = int.Parse(value).ToString("x" + (qnt * 4));                
            }

            if (qnt > 1)
            {
                for (int i = 0; (i + 4) <= cmd_value.Length; i += 4)
                {
                    cmd_value = cmd_value.Insert(i + 4, cmd_value.Substring(i, 2));
                    cmd_value = cmd_value.Remove(i, 2);
                }
            }

            CMND = Header + cmd_write + cmd_area + cmd_startaddress + cmd_byteempty + cmd_wordnumber + cmd_value;
                     
            ByteArray = new byte[CMND.Length / 2];

            for (int i = 0; i < CMND.Length; i += 2)
            {
                tmp1 = CMND.Substring(i, 2);
                ByteArray[k] = Convert.ToByte(tmp1, 16);
                k++;
            }

            if (Ethernet_Socket != null)
            {
                //Ethernet_Socket.SendTo(ByteArray, Remote_EndPoint); //send FINS cmd
                Ethernet_Socket.Send(ByteArray, SocketFlags.None);
                Ethernet_Socket.ReceiveTimeout = 200;

                //try
                //{
                //retval = Ethernet_Socket.ReceiveFrom(myByte, ref Remote_EndPoint);  //recv from socket
                retval = Ethernet_Socket.Receive(myByte, SocketFlags.None);
                return WriteDM_Received_Data(myByte, retval);      //call sub to process byte array
                //}
                //catch (Exception e)
                //{
                //    throw new Exception("No response from PLC \r\n\r\n" + e);
                //}
            }            
            
            return false;
        }

        public bool WriteDM_Received_Data(Byte[] receivedArray, int receivedQuantity)
        {
            if (receivedArray[12] == 0 && receivedArray[13] == 0)
            {
                return true;
            }
            else
            {
                throw new Exception("some error");
            }         
        }

        /// <summary>
        /// Read DM's
        /// </summary>
        /// <param name="S_ip">Source IP: Ip of the PC</param>
        /// <param name="S_port">Source Port: Port of the PC (9600)</param>
        /// <param name="D_ip">Destination IP: IP of the PLC</param>
        /// <param name="D_port">Destination Port: Port of the PLC (UDP Port default: 9600)</param>
        /// <param name="dmnum">DM start reading</param>
        /// <param name="qnt">Number of DM's to Read</param>
        /// <returns>Array of values</returns>
        public string ReadDM(string memoryAreaType, int dmnum, int qnt, int valueType = 1)
        {
            string ICF = "80", RSV = "00", GTC = "02", DNA = "00", DA1 = "00", DA2 = "00", SNA = "00", SA1 = "0A", SA2 = "00", SID = "AA";
            int X, Cnt = 0, retval;
            byte[] ByteArray;
            byte[] myByte = new byte[1024];
            string Header, tmp1, CMND;            

            //Random rnd = new Random();
            //string SID = rnd.Next(1, 99).ToString("00");            

            string[] D_parts = ip.Split('.');
            int D_fin = Convert.ToInt16(D_parts[3]);

            DA1 = D_fin.ToString("x2").ToUpper();

            Header = ICF + RSV + GTC + DNA + DA1 + DA2 + SNA + SA1 + SA2 + SID;            

            //0101820064000019

            string cmd_write = "0101";
            string cmd_area;

            switch (memoryAreaType)
            {
                case "DM":
                    cmd_area = "82";
                    break;
                case "CIO":
                    cmd_area = "B0";
                    break;
                default:
                    cmd_area = "82";
                    break;
            }

            string cmd_startaddress = dmnum.ToString("x4");
            string cmd_byteempty = "00";
            string cmd_wordnumber = qnt.ToString("x4");

            CMND = Header + cmd_write + cmd_area + cmd_startaddress + cmd_byteempty + cmd_wordnumber;                       

            //'txtSend.Text = ""

            ByteArray = new byte[CMND.Length / 2];

            for (X = 0; X < CMND.Length; X += 2)
            {
                tmp1 = CMND.Substring(X, 2);
                ByteArray[Cnt] = Convert.ToByte(tmp1, 16);
                Cnt += 1;
            }                        

            //try
            //{
            //    Remote_IP = IPAddress.Parse(D_ip);
            //}
            //catch (Exception e)
            //{
            //    throw new Exception("invalid PLC IP address \r\n" + e);
            //}

            //Remote_IPEndPoint = new IPEndPoint(Remote_IP, D_port);

            //Ethernet_Socket = new Socket(Remote_IPEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            //Ethernet_Socket.Connect(Remote_IPEndPoint);

            //if (Ethernet_Socket != null)
            //{
                //Ethernet_Socket.SendTo(ByteArray, Remote_EndPoint); //send FINS cmd
                Ethernet_Socket.Send(ByteArray, SocketFlags.None);
                Ethernet_Socket.ReceiveTimeout = 200;

                //try
                //{
                    //retval = Ethernet_Socket.ReceiveFrom(myByte, ref Remote_EndPoint);  //recv from socket
                    retval = Ethernet_Socket.Receive(myByte, SocketFlags.None);
                    return ReadDM_Received_Data(myByte, retval, qnt, valueType);      //call sub to process byte array
                //}
                //catch (Exception e)
                //{
                //    throw new Exception("No response from PLC \r\n\r\n" + e);
                //}
            //}
            //else
            //{
            //    //throw new Exception("Errore apertura socket \r\n\r\n");
            //}
        }

        public string ReadDM_Received_Data(Byte[] receivedArray, int receivedQuantity, int qnt, int valueType)
        {
            if (receivedArray[12] != 0 || receivedArray[13] != 0)
            {
                throw new Exception("some error");
            }            

            byte[] bytes = new byte[receivedQuantity - 14];
            Array.Copy(receivedArray, 14, bytes, 0, bytes.Length);

            for (int i = 1; i < bytes.Length; i += 2)
            {
                byte b = bytes[i];
                bytes[i] = bytes[i - 1];
                bytes[i - 1] = b;
            }

            if (valueType == 2)
            {
                string result = Encoding.ASCII.GetString(bytes);

                return result;
            }
            else
            {
                int result = Support.AutoBitConverter(bytes, 0, qnt);

                return result.ToString();
            }
        }
    }
}
