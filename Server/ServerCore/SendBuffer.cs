using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ServerCore
{
    public class SendBufferHelper
    {
        public static ThreadLocal<SendBuffer> CurrentBuffer = new ThreadLocal<SendBuffer>(() => { return null;  });
        // 전역이지만 나의 쓰레드에서만 고유하게 사용할 수 있는 전역

        public static int ChunkSize { get; set; } = 4096 * 100; 

        public static ArraySegment<byte> Open(int reserveSize)
        {
            if (CurrentBuffer.Value == null)
                CurrentBuffer.Value = new SendBuffer(ChunkSize);

            if (CurrentBuffer.Value.FreeSize < reserveSize)
                CurrentBuffer.Value = new SendBuffer(ChunkSize);

            return CurrentBuffer.Value.Open(reserveSize); 
        }

        public static ArraySegment<byte> Close(int usedSize)
        {
            return CurrentBuffer.Value.Close(usedSize);
        }
    }

    public class SendBuffer
    {
        // [u] [] [] [] [] [] [] [] [] []
        byte[] _buffer;
        int _usedSize = 0; // recvBuffer에서 wrtieSize에 해당

        public int FreeSize { get { return _buffer.Length - _usedSize;  } }

        public SendBuffer(int chunkSize)
        {
            _buffer = new byte[chunkSize];  
        }

        public ArraySegment<byte> Open(int reserveSize) // 할당할 최대 크기를 넣어줄거야.
        {
            if (reserveSize > FreeSize)
                return null;

            return new ArraySegment<byte>(_buffer, _usedSize, reserveSize); 
        }

        public ArraySegment<byte> Close(int usedSize) // 다 쓴 다음에는 실제로 사용한 사이즈를 넣어주게 될거야. 
        {
            ArraySegment<byte> segment = new ArraySegment<byte>(_buffer, _usedSize, usedSize); 
            _usedSize += usedSize;
            return segment; 
        }
    }
}
