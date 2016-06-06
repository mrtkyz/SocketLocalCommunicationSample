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

namespace socket.lib
{
    public class SocketHandler
    {
        public SocketHandler(IPAddress address, int port)
        {
            this.IpAddress = address;
            this.Port = port;

            _threads = new List<Thread>();
        }

        #region properties
        private IPAddress IpAddress { get; set; }

        private int Port { get; set; }

        private List<Thread> _threads { get; set; }
        private Thread _main { get; set; }

        private bool _listening { get; set; }

        private TcpListener _listener { get; set; }

        public static int ClientCount { get; private set; }
        #endregion

        #region events
        public delegate void OnDataEventHandler(SocketEventArgs e);
        public event OnDataEventHandler OnData;

        public delegate void OnStartEventHandler(SocketEventArgs e);
        public event OnStartEventHandler OnStart;

        public delegate void OnEndEventHandler(SocketEventArgs e);
        public event OnEndEventHandler OnEnd;

        public delegate void OnClientConnectectedEventHandler(SocketEventArgs e);
        public event OnClientConnectectedEventHandler OnClientConnected;

        public delegate void OnClientDisconnectedEventHandler(SocketEventArgs e);
        public event OnClientDisconnectedEventHandler OnClientDisconnected;
        #endregion

        #region public methods

        public void Start()
        {
            if (_main != null)
            {
                _main.Abort();
            }

            _main = new Thread(() =>
            {
                _listener = new TcpListener(this.IpAddress, this.Port);

                _listener.Start();
                _listening = true;

                if (OnStart != null)
                {
                    OnStart(null);
                }

                while (_listening)
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    Thread t = new Thread(() => HandleClient(client));
                    _threads.Add(t);

                    t.Start();
                }
            });

            _main.Start();
        }

        public void Stop()
        {

            _listening = false;
            _listener.Stop();
            _main.Abort();
            

            if (OnEnd != null)
            {
                OnEnd(null);
            }

        } 
        #endregion

        #region privates methods
        private void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            bool handShake = false;

            while (_listening)
            {
                while (!stream.DataAvailable)
                {
                    if (!this.IsConnected(client))
                    {
                        SocketHandler.ClientCount--;
                        if (OnClientDisconnected != null)
                        {
                            OnClientDisconnected(new SocketEventArgs() { Client = client });
                        }
                        
                        return;
                    }
                }

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
                        SocketHandler.ClientCount++;

                        if (OnClientConnected != null)
                        {
                            OnClientConnected(new SocketEventArgs() { Client = client, Data = data });
                        }
                    }
                }
                else {
                    if (OnData != null)
                    {
                        string data = GetDecodedData(bytes, bytes.Length);
                        OnData(new SocketEventArgs() { Client = client, Data = data });
                    }
                }

            }
        }

        private byte[] DecodeMessage(byte[] encoded)
        {
            byte[] decoded = new byte[encoded.Length];

            byte[] key = new byte[4] { 61, 84, 35, 6 };

            for (int i = 0; i < encoded.Length; i++)
            {
                decoded[i] = (byte)(encoded[i] ^ key[i % 4]);
            }

            return decoded;
        }

        private string GetDecodedData(byte[] buffer, int length)
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

        private bool IsConnected(TcpClient client)
        {
            try
            {

                bool part1 = client.Client.Poll(1000, SelectMode.SelectRead);
                bool part2 = (client.Client.Available == 0);
                if (part1 && part2)
                    return false;
                else
                    return true;


            }
            catch
            {
                return false;
            }

        }
        #endregion

    }
}
