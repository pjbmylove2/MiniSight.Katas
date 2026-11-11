using System;
using System.Diagnostics;
using System.Threading;

namespace Katas.Concurrency
{
    public static class KataP02
    {
        public static void Run()
        {
            Console.WriteLine("메인 스레드: " + Thread.CurrentThread.ManagedThreadId);

            Thread worker = new Thread(WashApple);
            worker.Name = "민수";
            worker.Start();
            worker.Join();

            Console.WriteLine("메인: 다음 작업");
        }

        public static void Chunk2()
        {
            Console.WriteLine("--- 일꾼 세 명 ---");

            Thread first = new Thread(WashApple) { Name = "1번 일꾼" };
            Thread second = new Thread(WashApple) { Name = "2번 일꾼" };
            Thread third = new Thread(WashApple) { Name = "3번 일꾼" };

            Stopwatch sw = Stopwatch.StartNew();
            first.Start();
            second.Start();
            third.Start();

            first.Join();
            second.Join();
            third.Join();

            sw.Stop();
            Console.WriteLine("다 씻음: " + sw.ElapsedMilliseconds + "ms");
        }


        private static void WashApple()
        {
            Thread.Sleep(500);
            Console.WriteLine(Thread.CurrentThread.Name + "이 사과를 다 씻음");
            Console.WriteLine("작업 스레드: " + Thread.CurrentThread.ManagedThreadId);
        }
    }
}
