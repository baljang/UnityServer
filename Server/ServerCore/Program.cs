using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    internal class Program
    {
        volatile static bool _stop = false;  // 전역은 모든 스레드들이 동시 접근 가능. 스레드들이 동시 접근할 때 어떤 일이 일어날지 살펴본다.

        static void ThreadMain()
        {
            Console.WriteLine("쓰레드 시작!");

            while(_stop == false)
            {
                // 누군가가 stop 신호를 해주기를 기다린다
            }

            Console.WriteLine("쓰레드 종료!");
        }

        static void Main(string[] args)
        {
            Task t = new Task(ThreadMain);
            t.Start();

            Thread.Sleep(1000); // 1초 동안 잠들었다 다시 깨는 함수

            _stop = true;

            Console.WriteLine("Stop 호출");
            Console.WriteLine("종료 대기중");

            t.Wait();   // 스레드가 끝났는지 알아 보는 함수. Thread일 때는 Join, Task는 Wait

            Console.WriteLine("종료 성공");
        }
    }
}
