using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Websocket.server
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                TcpListener listener = new TcpListener(IPAddress.Any, 8001);
                /* Start Listeneting at the specified port */
                listener.Start();


                Console.WriteLine("The local End point is  :" + listener.LocalEndpoint);
                Console.WriteLine("Waiting for a connection...");


                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    new Thread(() => HandleClient(client)).Start();
                }
                //IAsyncResult results = listener.BeginAcceptTcpClient(new AsyncCallback(HandleTcpConnection), listener);

                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error... " + e.StackTrace);
            }
        }

        public static void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            bool handShake = false;

            while (true)
            {
                while (!stream.DataAvailable) ;

                byte[] bytes = new byte[client.Available];

                stream.Read(bytes, 0, bytes.Length);

                if (!handShake)
                {
                    String data = Encoding.UTF8.GetString(bytes);

                    if (new Regex("^GET").IsMatch(data))
                    {

                        Byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + Environment.NewLine
                            + "Connection: Upgrade" + Environment.NewLine
                            + "Upgrade: websocket" + Environment.NewLine
                            + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                                SHA1.Create().ComputeHash(
                                    Encoding.UTF8.GetBytes(
                                        new Regex("Sec-WebSocket-Key: (.*)").Match(data).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
                                    )
                                )
                            ) + Environment.NewLine
                            + Environment.NewLine);

                        stream.Write(response, 0, response.Length);
                        handShake = true;
                    }
                }
                else {
                    string data = GetDecodedData(bytes, bytes.Length);//Encoding.UTF8.GetString(DecodeMessage(bytes));
                    Console.WriteLine(data);
                }

            }
        }

        public static byte[] DecodeMessage(byte[] encoded)
        {
            byte[] decoded = new byte[encoded.Length];

            byte[] key = new byte[4] { 61, 84, 35, 6 };

            for (int i = 0; i < encoded.Length; i++)
            {
                decoded[i] = (byte)(encoded[i] ^ key[i % 4]);
            }

            return decoded;
        }

        public static string GetDecodedData(byte[] buffer, int length)
        {
            byte b = buffer[1];
            int dataLength = 0;
            int totalLength = 0;
            int keyIndex = 0;

            if (b - 128 <= 125)
            {
                dataLength = b - 128;
                keyIndex = 2;
                totalLength = dataLength + 6;
            }

            if (b - 128 == 126)
            {
                dataLength = BitConverter.ToInt16(new byte[] { buffer[3], buffer[2] }, 0);
                keyIndex = 4;
                totalLength = dataLength + 8;
            }

            if (b - 128 == 127)
            {
                dataLength = (int)BitConverter.ToInt64(new byte[] { buffer[9], buffer[8], buffer[7], buffer[6], buffer[5], buffer[4], buffer[3], buffer[2] }, 0);
                keyIndex = 10;
                totalLength = dataLength + 14;
            }

            if (totalLength > length)
                throw new Exception("The buffer length is small than the data length");

            byte[] key = new byte[] { buffer[keyIndex], buffer[keyIndex + 1], buffer[keyIndex + 2], buffer[keyIndex + 3] };

            int dataIndex = keyIndex + 4;
            int count = 0;
            for (int i = dataIndex; i < totalLength; i++)
            {
                buffer[i] = (byte)(buffer[i] ^ key[count % 4]);
                count++;
            }

            return Encoding.ASCII.GetString(buffer, dataIndex, dataLength);
        }


    }
}

