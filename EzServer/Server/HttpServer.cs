using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class HttpServer
    {
        public const String MSG_DIR = @"\root\msg";
        public const String WEB_DIR = @"\root\web";
        public const String VERSION = "HTTP/1.0";
        public const String SERVERNAME = "myserv/1.1";

        bool running = false;
        TcpListener listener;

        public HttpServer(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
        }
        public void Start()
        {
            Thread thread = new Thread(new ThreadStart(Run));
            thread.Start();
        }
        private void Run()
        {
            listener.Start();
            running = true;
            Console.WriteLine("server is running.");
            while (running)
            {
                Console.WriteLine("waiting for connection...");
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("client connected.");
                HandleClient(client);
                client.Close();
            }

            running = false;
            listener.Stop();
            
        }
        private void HandleClient(TcpClient client)
        {
            StreamReader reader = new StreamReader(client.GetStream());
            String msg = "";
            while (reader.Peek() != -1)
            {
                msg += reader.ReadLine() + "\n";
            }
           
            Console.WriteLine("REQUEST: " + msg);
            Request request = Request.GetRequest(msg);
            Response response = Response.From(request);
            response.Post(client.GetStream());
        }
    }
}
