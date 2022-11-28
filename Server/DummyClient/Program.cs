using ServerCore;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DummyClient
{
    class GameSession : Session
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected : {endPoint}");

            // 보낸다
            for (int i = 0; i < 5; i++)
            {
                byte[] sendBuff = Encoding.UTF8.GetBytes($"Hello World! {i}");
                Send(sendBuff);
            }
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnDisconnected : {endPoint}");
        }

        // 이동 패킷 (3,2 좌표로 이동하고 싶다!) 
        // 이동하고 싶다는 패킷의 번호가 15번이라고 하면 
        // 15, 3, 2 이런 식으로 데이터가 들어가서 서버쪽에다가 쏴주면 서버에서는 이 패킷을 까서 15번이니까 클라이언트는 이동하고 싶어하구나 하고 
        // 3, 2 좌표를 까서 이동하게 한다. 
        public override int OnRecv(ArraySegment<byte> buffer)
        {
            string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
            Console.WriteLine($"[From Server] {recvData}");
            return buffer.Count; 
        }

        public override void OnSend(int numOfBytes)
        {
            Console.WriteLine($"Transferred bytes: {numOfBytes}");
        }
    }

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

            connector.Connect(endPoint, () => { return new GameSession(); }); 

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

