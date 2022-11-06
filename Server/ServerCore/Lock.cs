using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ServerCore
{
    // 재귀적 락을 허용할지(Yes) WriteLock->WriteLock OK, WriteLock->ReadLock OK, ReadLock->WriteLock NO(ReadLock은 애초 독점이 아니기에)
    // 스핀락 정책 ( 5000번 -> Yield)
    class Lock
    {
        const int EMPTY_FLAG = 0x00000000;
        const int WRITE_MASK = 0x7FFF0000;      // 15비트 사용
        const int READ_MASK = 0x0000FFFF;       // 16비트만 사용
        const int MAX_SPIN_COUNT = 5000; 

        // [Unused(1)] [WriteThreadId(15)] [ReadCount(16)]
        int _flag = EMPTY_FLAG;
        int _writeCount = 0; 

        public void WriteLock()
        {
            // 동일 쓰레드가 WriteLock을 이미 획득하고 있는지 확인 
            int lockThreadId = (_flag & WRITE_MASK) >> 16; 
            if(Thread.CurrentThread.ManagedThreadId == lockThreadId)
            {
                _writeCount++;
                return; 
            }

            // 아무도 WriteLock or ReadLock을 획득하고 있지 않을 때, 경합해서 소유권을 얻는다.
            int desired = (Thread.CurrentThread.ManagedThreadId << 16) & WRITE_MASK; 
            while(true)
            {
                for(int i =0; i<MAX_SPIN_COUNT; i++)
                {
                    if (Interlocked.CompareExchange(ref _flag, desired, EMPTY_FLAG) == EMPTY_FLAG)
                    {
                        _writeCount = 1;
                        return;
                    }
                }
                Thread.Yield(); 
            }
        }

        public void WriteUnlock()
        {
            int lockCount = --_writeCount; 
            if(lockCount == 0)
                Interlocked.Exchange(ref _flag, EMPTY_FLAG);
        }

        public void ReadLock()
        {
            // 동일 쓰레드가 WriteLock을 이미 획득하고 있는지 확인 
            int lockThreadId = (_flag & WRITE_MASK) >> 16;
            if (Thread.CurrentThread.ManagedThreadId == lockThreadId)
            {
                Interlocked.Increment(ref _flag); 
                return;
            }

            // 아무도 WriteLock을 획득하고 있지 않으면, ReadCount를 1늘린다. 
            while (true)
            {
                for(int i=0; i<MAX_SPIN_COUNT; i++)
                {
                    int expected = (_flag & READ_MASK); // A(0)  B(0) 스레드 동시에 들어왔다 가정
                    if (Interlocked.CompareExchange(ref _flag, expected + 1, expected) == expected) // A(0->1)  B(0->1) , A가 성공하면 _flag 값 1로 됨. B는 flag값 바뀐 상태라 (0->1) 실패한다. B는 다음 턴에 경합을 한다. 이렇게 계속 뺑뺑이 도는게 Lock free 프로그램의 기본
                        return; 
                }

                Thread.Yield(); 
            }
        }

        public void ReadUnlock()
        {
            Interlocked.Decrement(ref _flag); 
        }

    }
}
