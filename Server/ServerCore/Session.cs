using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ServerCore
{
    public abstract class PacketSession : Session
    {
        public static readonly int HeaderSize = 2;

        // [size(2)][packetId(2)[ ... ][size(2)][packetId(2)[ ... ]
        public sealed override int OnRecv(ArraySegment<byte> buffer)
        {
            int processLen = 0;

            while (true)
            {
                // 최소한 헤더는 파싱할 수 있는지 확인
                if (buffer.Count < HeaderSize)
                    break;

                // 패킷이 완전체로 도착했는지 확인
                ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
                if (buffer.Count < dataSize)
                    break;

                // 여기까지 왔으면 패킷 조립 가능
                OnRecvPacket(new ArraySegment<byte>(buffer.Array, buffer.Offset, dataSize));  // 패킷이 해당하는 영역을 다시 찝어서 넘겨 줄거야

                processLen += dataSize;
                buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + dataSize, buffer.Count - dataSize); // 바뀐 애는 다음 부분을 찝어주는 애가 된다. 
            }

            return processLen;
        }

        public abstract void OnRecvPacket(ArraySegment<byte> buffer);

    }

    public abstract class Session
    {      
        Socket _socket;
        int _disconnect = 0;

        RecvBuffer _recvBuffer = new RecvBuffer(1024); 

        object _lock = new object();
        Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();
        List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();
        SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
        SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();

        public abstract void OnConnected(EndPoint endPoint); // 어떤 IP 주소에서 접근을 했다 로그를 남길 수 있으니까 EndPoint를 인자로 받았다. 
        public abstract int OnRecv(ArraySegment<byte> buffer); // 누군가가 클라인트 쪽에서 패킷을 보내서 받았다는 메시지가 올텐데 
                                                                // 나중에는 패킷으로 받는 것도 고려할 수 있다. 
        public abstract void OnSend(int numOfBytes);
        public abstract void OnDisconnected(EndPoint endPoint);
                
        public void Start(Socket socket)
        {
            _socket = socket;

            _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
            _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

            RegisterRecv(); 
        }

        public void Send(ArraySegment<byte> sendBuff)   
        {
            lock (_lock)
            {
                _sendQueue.Enqueue(sendBuff);
                if (_pendingList.Count == 0)
                    RegisterSend();
            }           
        }

        public void Disconnect()
        {
            if (Interlocked.Exchange(ref _disconnect, 1) == 1)
                return;

            OnDisconnected(_socket.RemoteEndPoint); 
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }

        #region 네트워크 통신

        void RegisterSend()
        {
            while (_sendQueue.Count > 0)
            {
                ArraySegment<byte> buff = _sendQueue.Dequeue();
                _pendingList.Add(buff); 
            }

            _sendArgs.BufferList = _pendingList; 

            bool pending = _socket.SendAsync(_sendArgs);
            if (pending == false)
                OnSendCompleted(null, _sendArgs); 
        }

        void OnSendCompleted(object sender, SocketAsyncEventArgs args)
        {
            lock(_lock)
            {
                if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
                {
                    try
                    {
                        _sendArgs.BufferList = null; // 더이상 굳이 pendingList를 갖고 있을 필요 없으니까
                        _pendingList.Clear(); // bool 역할을 얘가 대신 해주는 거. 

                        OnSend(_sendArgs.BytesTransferred); 

                        if(_sendQueue.Count > 0)
                            RegisterSend(); 
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"OnSendCompletedFailed {e}");
                    }
                }
                else
                {
                    Disconnect();
                }
            }          
        }

        void RegisterRecv()
        {
            _recvBuffer.Clean(); 
            ArraySegment<byte> segment = _recvBuffer.WriteSegment;
            _recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count); 

            bool pending = _socket.ReceiveAsync(_recvArgs);
            if (pending == false)
                OnRecvCompleted(null, _recvArgs); 
        }

        void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
        {
            if(args.BytesTransferred >0 && args.SocketError == SocketError.Success)
            {
                try
                {
                    // Write 커서 이동
                    if(_recvBuffer.OnWrite(args.BytesTransferred) == false)
                    {
                        Disconnect();
                        return;
                    }

                    // 컨텐츠 쪽으로 데이터를 넘겨주고 얼마나 처리했는지 받는다. 
                    int processLen = OnRecv(_recvBuffer.ReadSegment); 
                    if(processLen < 0 || _recvBuffer.DataSize < processLen)
                    {
                        Disconnect();
                        return; 
                    }

                    // Read 커서 이동
                    if (_recvBuffer.OnRead(processLen) == false)
                    {
                        Disconnect();
                        return;
                    }

                    RegisterRecv();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"OnRecvCompletedFailed {e}"); 
                }          
            }
            else
            {
                Disconnect();
            }
        }

        #endregion

    }
}
