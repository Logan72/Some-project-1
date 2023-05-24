using K4os.Hash.xxHash;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary.Supporting_functions
{
    public abstract class Support
    {       
        public static dynamic AutoBitConverter(byte[] bytes, int valueIndex, int iCellNumber = 1, int iValueNumber = 1)
        {
            int bytesPerValue = (iCellNumber / iValueNumber) * 2;
            switch (bytesPerValue)
            {
                case 4:
                    return BitConverter.ToInt32(bytes, valueIndex * bytesPerValue);
                case 2:
                default:
                    return BitConverter.ToInt16(bytes, valueIndex * bytesPerValue);
            }
        }
        
        public static string ByteArrToString(byte[] bytes, Encoding encoding, bool hiLoReversal = false)
        {
            if (bytes == null)
                return null;

            if (hiLoReversal)
            {
                var byteList = bytes.ToList();
                var length = byteList.Count();
                for (int i = 0; i < length; i += 2)
                {
                    var a = byteList[i];
                    var b = byteList[i + 1];

                    byteList[i] = b;
                    byteList[i + 1] = a;
                }
                return encoding.GetString(byteList.ToArray());
            }
            else
            {
                return encoding.GetString(bytes);
            }
        }

        //public static int BaseXToBase10(string input, int baseX) // Base65536
        //{
        //    string[] stringSections = input.Split(' ');

        //    int result = 0;
        //    int converted;

        //    for (int i = 0; i < stringSections.Length; i++)
        //    {
        //        stringSections[i] = stringSections[i].TrimStart('0');

        //        if (stringSections[i].Length == 0) stringSections[i] = "0";

        //        try
        //        {
        //            converted = Convert.ToInt32(stringSections[i]);

        //            if (converted >= baseX)
        //            {
        //                throw new Exception("Wrong base!");
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            //Console.WriteLine(e.Message);                    
        //            return -1; //if unsuccessful
        //        }

        //        result += converted * intPow(baseX, i);
        //    }

        //    return result;
        //}        

        public static int intPow(int number, int power)
        {
            int result = 1;

            for (int i = 0; i < power; i++)
            {
                result *= number;
            }

            return result;
        }

        public static string HexStringToString(string hexString)
        {
            /**
             * Author: DaTdP
             * Convert hex string to string
            */
            hexString = hexString.Replace(" ", "");

            if (hexString == null || (hexString.Length & 1) == 1) { return ""; }

            var sb = new StringBuilder();

            for (var i = 0; i < hexString.Length; i += 2)
            {
                if (hexString.Substring(i, 2).Equals("00"))
                {
                    hexString = hexString.Remove(i, 2);
                    i -= 2;
                }
                else
                {
                    var hexChar = hexString.Substring(i, 2);
                    sb.Append((char)Convert.ToByte(hexChar, 16));
                }
            }
            Console.WriteLine(hexString);
            return sb.ToString();
        }

        public static string StringToHexString(string str)
        {
            /**
             * Author: DaTdP
             * Convert string to hex string             
            */
            var sb = new StringBuilder();
            var bytes = Encoding.ASCII.GetBytes(str);
            //byte j = 0;

            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] != 0)
                {
                    //j++;
                    sb.Append(bytes[i].ToString("X2"));

                    //if (j == 2)
                    //{
                    //    sb.Append(" ");
                    //    j = 0;
                    //}
                }
            }
            return sb.ToString().Trim(); // return: "48656C6C6F20776F726C64" for "Hello world"
        }
    }
}
