using System;
using System.Net;
using System.Threading;

namespace CSChat
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            string option = Console.ReadLine();
            switch (option.Split(' ')[0])
            {
                case "start":
                    Server chat = new Server(IPAddress.Parse("127.0.0.1"), 2001);
                    chat.StartAcceptTcpClients();
                    chat.Start();
                    break;
                case "connect":
                    Client user = new Client();
                    user.StartMassanging();
                    break;
            }
        }
    }
}