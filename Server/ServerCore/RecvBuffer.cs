using System;
using System.Collections.Generic;
using System.Text;

namespace ServerCore
{
    public class RecvBuffer
    {
        // [ ][ ][ ][ ][ ][ ][ ][ ][ ][ ]
        ArraySegment<byte> _buffer;
        int _readPos;
        int _writePos; 
        
        public RecvBuffer(int bufferSize)
        {
            _buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);
        }

        public int DataSize { get { return _writePos - _readPos; } }   // 버퍼에 들어 있는 아직 처리 되지 않는 데이터의 사이즈
        public int FreeSize { get { return _buffer.Count - _writePos; } }   // 버퍼에 남아있는 공간

        public ArraySegment<byte> ReadSegment   // 데이터 유효 범위의 세그먼트로 어디부터 데이터를 읽으면 되냐
        {
            get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _readPos, DataSize); }
        }

        public ArraySegment<byte> WriteSegment   // 다음에 리시브를 할 때 어디부터 어디가 유효범위인지
        {
            get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _writePos, FreeSize); }
        }

        public void Clean()     // 정리를 안하면 r,w가 버퍼 끝까지 가기 때문에 한번씩 처음으로 당겨줄 필요가 있다. 
        {
            int dataSize = DataSize;
            if(dataSize == 0)
            {
                // 남은 데이터가 없으면 복사하지 않고 커서 위치만 리셋
                _readPos = _writePos = 0; 
            }
            else
            {
                // 남은 찌끄레기가 있으면 시작 위치로 복사 
                Array.Copy(_buffer.Array, _buffer.Offset + _readPos, _buffer.Array, _buffer.Offset, dataSize);
                _readPos = 0;
                _writePos = dataSize; 
            }
        }

        public bool OnRead(int numOfBytes)  // 컨텐츠 코드에서 데이터를 가공해서 처리를 할건데 성공적으로 처리했으면 OnRead를 호출해서 커서 위치를 이동해준다.
        {
            if (numOfBytes > DataSize)
                return false;
            _readPos += numOfBytes;
            return true; 
        }

        public bool OnWrite(int numOfByte)  // 클라에서 데이터를 싸줘가지고 recive를 했을 때 그 때 write 커서를 이동시켜 주는 게 되는 거다. 
        {
            if (numOfByte > FreeSize)
                return false;

            _writePos += numOfByte;
            return true; 
        }
    }
}
