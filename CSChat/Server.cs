using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Text;
using CSChat;

namespace CSChat
{
    class User
    {
        public string Name = "Guest";
        public byte[] buffer = new byte[1024];
        private IPAddress IP;
        public TcpClient connection;


        public User(TcpClient connection)
        {
            this.connection = connection;
            IP = IPAddress.Parse(connection.Client.LocalEndPoint.ToString().Split(':')[0]);
        }
    }

    class Server
    {
        private IPAddress IP;
        private int port;
        private TcpListener ServerSock;
        List<User> ConnectedUsers = new List<User>();
        List<User> DisconnetedUsers = new List<User>();
        public const String MSG_DIR = @"\root\msg";
        public const String WEB_DIR = @"\root\web";
        public const String VERSION = "HTTP/1.0";
        public const String SERVERNAME = "myserv/1.1";

        public Server(IPAddress ip, int port)
        {
            IP = ip;
            this.port = port;
            ServerSock = new TcpListener(ip, port);
            ServerSock.Start();
        }

        public void StartAcceptTcpClients()
        {
            ServerSock.BeginAcceptTcpClient(AcceptClient, ServerSock);
        }

        bool IsConnected(User user)
        {
            try
            {
                if (user != null && user.connection.Client != null && user.connection.Connected)
                {
                    if (user.connection.Available!=0&&user.connection.Client.Poll(0,SelectMode.SelectRead))
                    {
                        return user.connection.Client.Receive(new byte[1], SocketFlags.Peek) != 0;
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
        public void Start()
        {
            StartAcceptTcpClients();
            while (true)
            {
                
            }
        }

        void AcceptClient(IAsyncResult ar)
        {
            User client = new User(ServerSock.EndAcceptTcpClient(ar));
            Console.Write(client.connection.Client.RemoteEndPoint.ToString() + " connected\n");
            HandleClient(client.connection);
            client.connection.Close();
            StartAcceptTcpClients();
        }

        void HandleClient(TcpClient client)
        {
            StreamReader reader = new StreamReader(client.GetStream());
            String msg = "";
            if (IsConnected(new User(client)))
            {
                while (reader.Peek() != -1)
                {
                    msg += reader.ReadLine() + "\n";
                }
            }
            
       
            Console.WriteLine("REQUEST: " + msg);
            Request request = Request.GetRequest(msg);
            Response response = Response.From(request);
            if (IsConnected(new User(client))&&msg!="")
            {
                response.Post(client.GetStream());
            }
        }

        void OnRead(IAsyncResult ar)
        {
            User client = (User) ar.AsyncState;
            if (!IsConnected(client))
            {
                return;
            }
            int size = client.connection.GetStream().EndRead(ar);
            string message = Encoding.UTF8.GetString(client.buffer, 0, size);
            if (message.Split(' ')[0] == "/setName")
            {
                try
                {
                    client.Name = message.Split(' ')[1];
                }
                catch (Exception e)
                {
                    BroadCast(new List<User>{client}, "invalid Name");
                }
                
                string callback = "#Name:" + client.Name;
                client.connection.GetStream().Write(Encoding.UTF8.GetBytes(callback), 0, callback.Length);
            }
            else
            {
                Console.WriteLine(message);
                message = $"{client.Name}: " + message;
                BroadCast(ConnectedUsers, message);
            }

            client.connection.GetStream().BeginRead(client.buffer, 0, client.buffer.Length, OnRead, client);
        }

        void BroadCast(List<User> toSend, string message)
        {
            foreach (var user in toSend)
            {
                Console.WriteLine(message);
                user.connection.GetStream().Write(Encoding.UTF8.GetBytes(message), 0, message.Length);
            }
        }
    }
}