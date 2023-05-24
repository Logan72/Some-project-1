using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary.Sever_connection.Keyence_PLC
{
    public class Communication_PLC
    {
        public TcpClient m_tcp;
        public NetworkStream m_ns;

        private const int PORT = 10001;

        string ip = "0.0.0.0";
        string path = Directory.GetCurrentDirectory() + "\\port.ini";

        // 通信インターフェイスと接続状態を取得するプロパティ(値を設定しないのでgetのみでOK)
        public bool Connected
        {
            get
            {
                if (m_tcp == null)
                    return false;
                else
                    return m_tcp.Connected;
            }

        }
        //hàm khởi tao
        //public Communication_PLC(string obj)
        //{
        //    if (!System.IO.File.Exists(path))
        //    {
        //        throw new TcpNotConnectException("config.iniが存在しないためソフトウェアを終了します。");
        //        // richTextBox1.AppendText("" + "\n");
        //    }

        //    StringBuilder sb = new StringBuilder();
        //    AccessIni.GetPrivateProfileString(obj, "ip", "", sb, 16, path);
        //    ip = sb.ToString();

        //}

        public Communication_PLC() { }

        /// <summary>
        /// ホスト名"localhost"でMVCと接続します。
        /// </summary>
        /// <returns></returns>
        public bool TcpConnect()
        {
            try
            {
                if (Connected == true)
                    TcpClose();

                m_tcp = new TcpClient();

                m_tcp.Connect("localhost", PORT);
                m_ns = m_tcp.GetStream();

                while (Connected == false)
                    Thread.Sleep(100);

                return true;
            }
            catch
            {
                return false;
            }
        }

        //------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 指定されたIPアドレスでMVCと接続します。
        ///It connects to MVC with the specified IP address.
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public bool TcpConnect(string ip)
        {
            try
            {
                if (Connected == true)
                    TcpClose();

                m_tcp = new TcpClient();
                m_tcp.Connect(ip, PORT);
                m_ns = m_tcp.GetStream();

                while (Connected == false)
                    Thread.Sleep(100);

                return true;
            }
            catch
            {
                //	return false;  //20170530 For debug test
                return true;
            }
        }

        //-----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 指定されたIPアドレスとポート番号でMVCと接続します。
        /// It connects to MVC with the specified IP address and port number.
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public bool TcpConnect(string ip, int port)
        {
            try
            {
                if (Connected == true)
                    TcpClose();

                m_tcp = new TcpClient();
                m_tcp.Connect(ip, port);
                Thread.Sleep(200);
                m_ns = m_tcp.GetStream();
                if (m_tcp.Available > 0)
                {
                    byte[] ret_mes = new byte[m_tcp.Available];                         // 受信文字数分の配列確保 Một mảng cho số ký tự đã nhật
                    m_ns.Read(ret_mes, 0, ret_mes.Length);
                }

                //System.Threading.Thread.Sleep(100);

                if (Connected == false)
                {
                    return false;
                }

                //while (Connected == false)
                //    System.Threading.Thread.Sleep(100);

                return true;
            }
            catch
            {
                return false;
            }
        }

        //-----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// MVCから切断します。
        /// Disconnect from MVC.
        /// </summary>
        public void TcpClose()
        {
            if (m_ns != null)
            {
                m_ns.Close();
                m_ns = null;
            }

            m_tcp.Close();
            m_tcp = null;

        }

        //-----------------------------------------------------------------------------------------------------------
        /// <summary>
        /// 指定されたコマンドを送信し、その応答を受信します。
        /// Sends the specified command and receives the response.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public string SendCommand(string command)
        {
            string receive = "";

            try
            {
                string recBuff = "";
                bool recFlg = false;
                int loop = 0;
                byte[] temp_mes = Encoding.GetEncoding("SHIFT-JIS").GetBytes(command);      // コマンドを一旦S-JISでbyte配列に変換
                byte[] send_mes = new byte[temp_mes.Length + 1];                            // 最後にCRを付けるための要素+1されたbyte配列作成
                Array.Copy(temp_mes, send_mes, temp_mes.Length);                            // 要素のコピー
                send_mes[temp_mes.Length] = 0x0D;                                           // 最後にCRを付ける
                m_ns.Write(send_mes, 0, send_mes.Length);                                // コマンド送信
                while (true)
                {
                    ++loop;                                                                 // ループ回数 Số vòng lặp
                    Thread.Sleep(10);                                      // 待機 Đợi
                    if (m_tcp.Available > 0)
                    {
                        byte[] ret_mes = new byte[m_tcp.Available];                         // 受信文字数分の配列確保 Một mảng cho số ký tự đã nhật
                        m_ns.Read(ret_mes, 0, ret_mes.Length);                              // 読み込み Đọc

                        receive = Encoding.GetEncoding("SHIFT-JIS").GetString(ret_mes);     // byte→string変換 // Chuyển đổi
                        //int lala = BitConverter.
                        recBuff += receive;                                                 // 一時バッファにためておく // Giu trong bộ đệm tạm thời

                        recFlg = true;
                        //break;
                    }
                    // 
                    else if (m_tcp.Available == 0 && recFlg == true)
                    {
                        break;
                    }

                    // 一定時間経ったらタイムアウト
                    if (loop >= 5)
                    {
                        //receive = "timeout";
                        recBuff = "timeout";
                        //recBuff = "";
                        break;
                    }
                }

                receive = recBuff;
                return receive;
            }
            catch (NullReferenceException e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                receive = "not connect";
                //receive = "";
                return receive;
            }
            catch (IOException e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                receive = "not connect";
                //receive = "";
                return receive;
            }
        }

        public string SendCommand_NonOD(string command)
        {
            string receive = "";

            try
            {
                string recBuff = "";
                bool recFlg = false;
                int loop = 0;
                byte[] temp_mes = Encoding.GetEncoding("SHIFT-JIS").GetBytes(command);      // コマンドを一旦S-JISでbyte配列に変換
                byte[] send_mes = new byte[temp_mes.Length + 1];                            // 最後にCRを付けるための要素+1されたbyte配列作成
                Array.Copy(temp_mes, send_mes, temp_mes.Length);                            // 要素のコピー
                send_mes[temp_mes.Length] = 0x0D;                                           // 最後にCRを付ける
                //m_ns.Write(send_mes, 0, send_mes.Length);                                   // コマンド送信
                m_ns.Write(temp_mes, 0, temp_mes.Length);                                                                           //m_ns.Write(temp_mes, 0, temp_mes.Length);	
                while (true)
                {
                    ++loop;                                                                 // ループ回数 Số vòng lặp
                    Thread.Sleep(10);                                      // 待機 Đợi
                    if (m_tcp.Available > 0)
                    {
                        byte[] ret_mes = new byte[m_tcp.Available];                         // 受信文字数分の配列確保 Một mảng cho số ký tự đã nhật
                        m_ns.Read(ret_mes, 0, ret_mes.Length);                              // 読み込み Đọc

                        receive = Encoding.GetEncoding("SHIFT-JIS").GetString(ret_mes);     // byte→string変換 // Chuyển đổi
                        recBuff += receive;                                                 // 一時バッファにためておく // Giu trong bộ đệm tạm thời

                        recFlg = true;
                        //break;
                    }
                    // 
                    else if (m_tcp.Available == 0 && recFlg == true)
                    {
                        break;
                    }

                    // 一定時間経ったらタイムアウト
                    if (loop >= 100)
                    {
                        //receive = "timeout";
                        recBuff = "timeout";
                        break;
                    }
                }

                receive = recBuff;
                return receive;
            }
            catch (NullReferenceException e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                receive = "not connect";
                return receive;
            }
            catch (IOException e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                receive = "not connect";
                return receive;
            }
        }
        public string ReceiveCommand()
        {
            string receive = "";

            try
            {
                if (m_tcp.Available == 0) return null;
                string recBuff = "";
                bool recFlg = false;
                int loop = 0;
                // コマンド送信
                while (true)
                {
                    ++loop;                                                                 // ループ回数 Số vòng lặp
                    //System.Threading.Thread.Sleep(10);                                      // 待機 Đợi
                    if (m_tcp.Available > 0)
                    {
                        byte[] ret_mes = new byte[m_tcp.Available];                         // 受信文字数分の配列確保 Một mảng cho số ký tự đã nhật
                        m_ns.Read(ret_mes, 0, ret_mes.Length);                              // 読み込み Đọc

                        receive = Encoding.GetEncoding("SHIFT-JIS").GetString(ret_mes);     // byte→string変換 // Chuyển đổi
                        recBuff += receive;                                                 // 一時バッファにためておく // Giu trong bộ đệm tạm thời

                        recFlg = true;
                        //break;

                    }
                    else if (m_tcp.Available == 0 && recFlg == true)
                    {
                        break;
                    }
                    if (loop >= 100)
                    {
                        break;
                    }
                }
                receive = recBuff;
                return receive;
            }

            catch (NullReferenceException e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                receive = "not connect";
                return receive;
            }
            catch (IOException e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                receive = "not connect";
                return receive;
            }
        }
    }
}
