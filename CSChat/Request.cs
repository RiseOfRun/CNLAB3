using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSChat
{
    class Request
    {
        public String Type { get; set; }

        public String Url { get; set; }
        private String Host { get; set; }
        public String Referer { get; set; }

        private Request(String type, String url, String host, String referer)
        {
            Type = type;
            Url = url;
            Host = host;
            Referer = referer;
        }
        public static Request GetRequest(String msg)
        {
            Console.WriteLine(msg+"\n\n\n");
            if (String.IsNullOrEmpty(msg))
                return null;
            String referer = "";
            String[] tokens = msg.Split(' ');
            for (int i = 0; i < tokens.Length; i++)
            {
                if (tokens[i] == "Referer")
                {
                    referer = tokens[i + 1];
                }
            }
            Console.WriteLine("Type is: {0}, url is: {1}, host is: {2}, referer: {3}", tokens[0], tokens[1], tokens[3],referer);
            return new Request(tokens[0], tokens[1], tokens[4], referer);

        }

    }
}