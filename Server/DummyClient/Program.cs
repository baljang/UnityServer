﻿using ServerCore;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DummyClient
{
   
    internal class Program
    {
        static void Main(string[] args)
        {
            // DNS ( Domain Name System )
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);
            // ipAddre는 식당 주소, 7777은 식당 정문인지 후문인지 문의 번호        }
            // 식당 주소 찾는 부분은 똑같을 거야. 

            Connector connector = new Connector();

            connector.Connect(endPoint, () => { return new ServerSession(); }); 

            while(true)
            {
                try
                {
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                Thread.Sleep(100);
            }
          
        }
    }
}

