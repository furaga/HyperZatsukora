using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using FLib;

namespace FLib
{
    public class SockswitchServer
    {
        TcpListener server;
        TcpClient client;
        byte[] recieveTextBytes = new byte[1];

        public bool IsConnected { get { return client != null && client.Connected; } }

        public SockswitchServer(string Host, int Port)
        {
            // プロセス通信
            try
            {
                server = new TcpListener(IPAddress.Parse(Host), Port);
                server.Start();
                Console.WriteLine("waiting a client...\r\n");
                client = server.AcceptTcpClient();
                Console.WriteLine("connected!\r\n");
            }
            catch (SocketException ee)
            {
                MessageBox.Show(ee.Message + ":" + ee.StackTrace);
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message + ":" + ee.StackTrace);
            }
        }

        public void Dispose()
        {
            client.Close();
            client = null;
            server.Stop();
            server = null;
        }

        public string SendQuery(string query)
        {
            try
            {
                if (client != null && client.Connected)
                {
                    var stream = client.GetStream();
                    byte[] sendData = Encoding.UTF8.GetBytes(query + ";");
                    stream.Write(sendData, 0, sendData.Length);

                    int total = 0;
                    while (true)
                    {
                        if (total >= recieveTextBytes.Length) break;
                        int readSize = stream.Read(recieveTextBytes, total, recieveTextBytes.Length - total);
                        if (readSize <= 0) break;
                        total += readSize;
                    }

                    // 文字列の長さを測定
                    int length = 0;
                    for (; length < recieveTextBytes.Length; length++)
                        if (recieveTextBytes[length] == 0) break;

                    return Encoding.UTF8.GetString(recieveTextBytes, 0, length);
                }
                return "";
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
                return "";
            }
        }


    }
}