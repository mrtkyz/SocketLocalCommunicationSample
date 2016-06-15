﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace socket.lib
{
    public class SocketEventArgs : EventArgs
    {
        public TcpClient Client { get; set; }
        public string Data { get; set; }

        public NetworkStream Stream { get; set; }

        public void WriteToStream(string response)
        {
            if(this.Stream != null)
            {
                byte[] en = SocketHandler.EncodeMessageToSend(response);
                this.Stream.Write(en, 0, en.Length);
            }
        }
    }
}
