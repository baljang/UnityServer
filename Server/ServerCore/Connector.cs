using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerCore
{
    public class Connector
    {
        Func<Session> _sessionFactory;


        // Socket _socket; // 이렇게 하지 않는 이유는 Connect를 하나만 받는 경우도 있겠지만 여러명 받을 수 있기 때문
        public void Connect(IPEndPoint endPoint, Func<Session> sessionFactory)
        {
            // 휴대폰 설정
            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _sessionFactory = sessionFactory;

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += OnConnectCompleted;
            args.RemoteEndPoint = endPoint;
            args.UserToken = socket;

            RegisterConnect(args); 
        }
       
        void RegisterConnect(SocketAsyncEventArgs args)
        {
            Socket socket = args.UserToken as Socket;
            if (socket == null)
                return; 

            bool pending = socket.ConnectAsync(args);
            if (pending == false)
                OnConnectCompleted(null, args); 
        }

        void OnConnectCompleted(object sender, SocketAsyncEventArgs args)
        {
            if(args.SocketError == SocketError.Success)
            {
                Session session = _sessionFactory.Invoke();
                session.Start(args.ConnectSocket);
                session.OnConnected(args.RemoteEndPoint); 
            }
            else
            {
                Console.WriteLine($"OnConnectCompletedFail: {args.SocketError}");
            }
        }
    }
}
