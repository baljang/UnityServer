using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerCore
{
    public class Listener
    {
        Socket _listenSocket;
        Func<Session> _sessionFactory; 

         public void Init(IPEndPoint endPoint, Func<Session> sessionFactory)
        {
            // 문지기(가 들고있는 휴대폰)
            _listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp); // TCP로 할 때 설정
            _sessionFactory += sessionFactory;

            // 문지기 교육
            _listenSocket.Bind(endPoint); // 식당 주소와 후문인지 정문인지 기입을 해준 것

            // 영업 시작
            // backlog : 최대 대기수  
            _listenSocket.Listen(10);

            for(int i = 0; i < 10; i++)
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
                RegistterAccept(args);
            } 
        }

        void RegistterAccept(SocketAsyncEventArgs args)
        {
            args.AcceptSocket = null;

            bool pending = _listenSocket.AcceptAsync(args);
            if (pending == false)    // 운 좋게 바로 클라이언트가 접속했을 경우
                OnAcceptCompleted(null, args);  
        }

        void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
        {
            if(args.SocketError == SocketError.Success) // 모든 게 잘 처리 됐다는 뜻
            {
                Session session = _sessionFactory.Invoke(); 
                session.Start(args.AcceptSocket);
                session.OnConnected(args.AcceptSocket.RemoteEndPoint);
            }
            else
                Console.WriteLine(args.SocketError.ToString());

            RegistterAccept(args); // 다음 아이를 위해서 또 한번 등록을 해주는 거
        }

        public Socket Accept()
        { 
            return _listenSocket.Accept(); 
        }

    }
}
