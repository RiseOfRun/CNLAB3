using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
                    if (user.connection.Client.Poll(0,SelectMode.SelectRead))
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
                for (int i = 0; i < ConnectedUsers.Count; i++)
                {
                    if (!IsConnected(ConnectedUsers[i]))
                    {
                        DisconnetedUsers.Add(ConnectedUsers[i]);
                    }
                }
                    
                

                foreach (var user in DisconnetedUsers)
                {
                    ConnectedUsers.Remove(user);
                    BroadCast(ConnectedUsers, $"user {user.Name} disconnected");
                }

                DisconnetedUsers = new List<User>();
            }
        }

        void AcceptClient(IAsyncResult ar)
        {
            User connectedUser = new User(ServerSock.EndAcceptTcpClient(ar));
            var client = connectedUser.connection;
            ConnectedUsers.Add(connectedUser);
            while(client.Available < 3)
            {
                // wait for enough bytes to be available
            }
            int size = client.GetStream().Read(connectedUser.buffer,0,connectedUser.buffer.Length);
            string data = Encoding.UTF8.GetString(connectedUser.buffer,0,size);
            if (new System.Text.RegularExpressions.Regex("^GET").IsMatch(data))
            {
                const string eol = "\r\n"; // HTTP/1.1 defines the sequence CR LF as the end-of-line marker

                string responseString = "HTTP/1.1 101 Switching Protocols" + eol
                 + "Connection: Upgrade" + eol
                 + "Upgrade: websocket" + eol
                 + "Sec-WebSocket-Accept: " +
                 Convert.ToBase64String(
                     System.Security.Cryptography.SHA1.Create()
                         .ComputeHash(
                             Encoding.UTF8.GetBytes(
                                 new System.Text.RegularExpressions.
                                         Regex(
                                             "Sec-WebSocket-Key: (.*)")
                                     .Match(data).Groups[1].Value
                                     .Trim() +
                                 "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
                             )
                         )
                 ) + eol
                 + eol;
                Byte[] response = Encoding.UTF8.GetBytes(responseString);
                client.GetStream().Write(response, 0, response.Length);
            }
            client.GetStream().BeginRead(connectedUser.buffer, 0, connectedUser.buffer.Length, OnRead, connectedUser);
            StartAcceptTcpClients();
        }

        void OnRead(IAsyncResult ar)
        {
            User client = (User) ar.AsyncState;
            if (!IsConnected(client))
            {
                return;
            }
            int size = client.connection.GetStream().EndRead(ar);
            string message = TransferMessage(client.buffer);
            Console.WriteLine(message);
            client.connection.GetStream().BeginRead(client.buffer, 0, client.buffer.Length, OnRead, client);
        }

        string TransferMessage(byte[] bytes)
        {
            string translated = "";
            bool fin = (bytes[0] & 0b10000000) != 0,
                mask = (bytes[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"
            int opcode = bytes[0] & 0b00001111, // expecting 1 - text message
                offset = 2;
            int msglen = bytes[1] & 0b01111111;

            if (msglen == 126) {
                // bytes are reversed because websocket will print them in Big-Endian, whereas
                // BitConverter will want them arranged in little-endian on windows
                msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                offset = 4;
            } else if (msglen == 127) {
                // To test the below code, we need to manually buffer larger messages — since the NIC's autobuffering
                // may be too latency-friendly for this code to run (that is, we may have only some of the bytes in this
                // websocket frame available through client.Available).
                msglen = (int) BitConverter.ToUInt64(new byte[] { bytes[9], bytes[8], bytes[7], bytes[6], bytes[5], bytes[4], bytes[3], bytes[2] },0);
                offset = 10;
            }
            if (msglen == 0) {
                translated = "msglen == 0";
            } else if (mask) {
                byte[] decoded = new byte[msglen];
                byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                offset += 4;

                for (ulong i = 0; i < (ulong) msglen; ++i)
                    decoded[i] = (byte)(bytes[offset + (int) i] ^ masks[i % 4]);

                string text = Encoding.UTF8.GetString(decoded);
                translated = text;
            } else
                translated = "mask bit not set";
            return translated;
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