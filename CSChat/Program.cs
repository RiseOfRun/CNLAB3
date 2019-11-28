using System;
using System.Net;
using System.Threading;

namespace CSChat
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Server server = new Server(IPAddress.Any, 8080);
            server.StartAcceptTcpClients();
            server.Start();
        }
    }
}