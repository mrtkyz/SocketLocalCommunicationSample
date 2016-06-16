using socket.lib;
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
                int port = 8001;

                SocketHandler handler = new SocketHandler(IPAddress.Any, port);
                handler.OnStart += new SocketHandler.OnStartEventHandler((SocketEventArgs sarg) =>
                {
                    Console.WriteLine(string.Format("listeining on {0}. Press any key fot exit.", port));
                });
                
                handler.OnClientConnected += new SocketHandler.OnClientConnectectedEventHandler((SocketEventArgs sarg) =>
                {
                    Console.WriteLine("new client connected. total client count: {0}", SocketHandler.ClientCount);
                });

                handler.OnClientDisconnected += new SocketHandler.OnClientDisconnectedEventHandler((SocketEventArgs sarg) =>
                {
                    Console.WriteLine("client disconnected. total client count: {0}", SocketHandler.ClientCount);
                });

                handler.OnEnd += new SocketHandler.OnEndEventHandler((SocketEventArgs sarg) => {
                    Console.WriteLine("listener closed");
                });

                handler.OnData += new SocketHandler.OnDataEventHandler((SocketEventArgs sarg) => {
                    Console.WriteLine(sarg.Data);

                    //write data
                    SocketHandler.WriteToStream(sarg.Stream, "OK");

                });


                handler.Start();
                


                Console.ReadLine();
                handler.Stop();
                Console.ReadLine();


            }
            catch (Exception e)
            {
                Console.WriteLine("Error... " + e.StackTrace);
            }
        }

    }
}

