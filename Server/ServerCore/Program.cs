using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    // [JobQueue]라는 애가 있는데 static이라는 공간에 있다고 하면 모든 스레드들이 다 동시 다발적으로 경합을 하게 될거야. 
    // 그럼 락을 걸었다가 풀었다가 반복을 해야 하는데 한번 락을 잡을 때 일감을 하나만 빼오는게 아니라 TLS공간에다가 실컷 많이 뽑아 오면 된다는 얘기다. 
    // 락을 한 번 걸고 최대한 많은 일감을 빼온 거니까  static ThreadLocal<string> ThreadName 여기 있는 일감을 처리하기 전까지는 다시하면 JobQueue이 전역에 있는 좌표에다 접근을 할 필요가 없게 된다. 
    // 그런 식으로 부하를 줄일 수 있다는 얘기다 .

    class Program
    {
        static ThreadLocal<string> ThreadName = new ThreadLocal<string>(() => { return $"My Name Is {Thread.CurrentThread.ManagedThreadId}"; }); // 자신만의 공간에 저장되기 때문에 특정 스레드에서 ThreadName을 고친다고 해도 다른 애들한테 영향을 주지 않게 된다. 

        static void WhoAmI()
        {
            bool repeat = ThreadName.IsValueCreated;
            if (repeat)
                Console.WriteLine(ThreadName.Value + " (repeat)");
            else
                Console.WriteLine(ThreadName.Value); 
        }
        
        static void Main(string[] args)
        {
            ThreadPool.SetMinThreads(1, 1);
            ThreadPool.SetMaxThreads(3, 3); 
            Parallel.Invoke(WhoAmI, WhoAmI, WhoAmI, WhoAmI, WhoAmI, WhoAmI, WhoAmI, WhoAmI);

            ThreadName.Dispose(); 
        }
    }
}