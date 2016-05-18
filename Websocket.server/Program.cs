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


                SocketHandler handler = new SocketHandler(IPAddress.Any, 8001);
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

