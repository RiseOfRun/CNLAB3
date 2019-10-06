using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Text;

namespace CSChat
{
    class Client
    {
        private TcpClient connection;
        private IPAddress host = IPAddress.Parse("127.0.0.1");
        private int port = 2001;
        public string Name;
        public byte[] buffer = new byte[1024];
        public NetworkStream stream => connection.GetStream();

        public Client()
        {
            connection = new TcpClient(host.ToString(),port);
        }

        public void StartMassanging()
        {
            connection.GetStream().BeginRead(buffer, 0, buffer.Length, OnRead, stream);
            while (true)
            {
                string message = Console.ReadLine();
                stream.Write(Encoding.UTF8.GetBytes(message),0,message.Length);
            }
        }

        void OnRead(IAsyncResult ar)
        {
            int size = stream.EndRead(ar);
            string message = Encoding.UTF8.GetString(buffer, 0, size);
            if (message.Split(':')[0] != Name)
            {
                if (message.Split(':')[0]=="#Name")
                {
                    Name = message.Split(':').Last();
                    Console.WriteLine("ur name setted As: "+Name);                   
                }
                else Console.WriteLine(message);

            }
            stream.BeginRead(buffer, 0, buffer.Length, OnRead, stream);
        }
    }
}