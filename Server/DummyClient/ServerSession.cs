using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ServerCore;

namespace DummyClient
{
    public abstract class Packet
    {
        public ushort size;
        public ushort packetId;

        public abstract ArraySegment<byte> Write(); // 밀어 넣는 부분 Serialize
        public abstract void Read(ArraySegment<byte> s); // 빼내 주는 부분 DeSerialize
    }
    
    class PlayerInfoReq : Packet    // 클라에서 서버로 플레이어 정보를 알고 싶어
    {
        public long playerId;

        public PlayerInfoReq()
        {
            this.packetId = (ushort)PacketID.PlayerInfoReq; 
        }

        public override void Read(ArraySegment<byte> s)
        {
            ushort count = 0;

            //ushort size = BitConverter.ToUInt16(s.Array, s.Offset); 
            count += 2;
            //ushort id = BitConverter.ToUInt16(s.Array, s.Offset + count); 
            count += 2;
            this.playerId = BitConverter.ToInt64(new ReadOnlySpan<byte>(s.Array, s.Offset + count, s.Count - count)); 
            count += 8; 
        }

        public override ArraySegment<byte> Write()
        {
            ArraySegment<byte> s = SendBufferHelper.Open(4096);

            ushort count = 0; // 지금까지 몇 바이트를 버퍼에 밀어 넣었는지 추적할거야.
            bool success = true;

            // 혹시라도 중간에 한번이라도 실패하면 false가 뜬다.
            //success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset, s.Count), packet.size);
            count += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + count, s.Count - count), (ushort)PacketID.PlayerInfoReq); 
            count += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + count, s.Count - count), this.playerId);
            count += 8;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset, s.Count), (ushort)4);

            if (success == false)
                return null; 

            return SendBufferHelper.Close(count);
        }
    }


    public enum PacketID
    {
        PlayerInfoReq = 1, 
        PlayerInfoOk = 2,
    }

    class ServerSession : Session
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected : {endPoint}");

            PlayerInfoReq packet = new PlayerInfoReq() { size = 4,playerId = 1001 };

            // 보낸다
            //for (int i = 0; i < 5; i++)
            {
                ArraySegment<byte> s = packet.Write();
                if (s != null)
                    Send(s); 
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

}
