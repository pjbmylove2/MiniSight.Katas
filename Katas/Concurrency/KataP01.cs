using System;
using System.Diagnostics;
using System.Threading;

namespace Katas.Concurrency
{
    public static class KataP01
    {
        public static void Run()
        {
            Chunk1();
        }

        public static void Chunk1()
        {
            Console.WriteLine("두뇌(코어) 수: " + Environment.ProcessorCount);
            Console.WriteLine("지금 내 번호: " + Thread.CurrentThread.ManagedThreadId);
            Console.WriteLine("이 프로그램의 일꾼 수: " + Process.GetCurrentProcess().Threads.Count);
        }
    }
}
