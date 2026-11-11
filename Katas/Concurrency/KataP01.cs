using System;
using System.Diagnostics;
using System.Threading;

namespace Katas.Concurrency
{
    public static class KataP01
    {
        public static void Run()
        {
            //Chunk1();
            //Chunk2();
            Chunk3();
        }

        public static void Chunk1()
        {
            Console.WriteLine("두뇌(코어) 수: " + Environment.ProcessorCount);
            Console.WriteLine("지금 내 번호: " + Thread.CurrentThread.ManagedThreadId);
            Console.WriteLine("이 프로그램의 일꾼 수: " + Process.GetCurrentProcess().Threads.Count);
        }

        public static void Chunk2()
        {
            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 1; i <= 3; i++)
            {
                Console.WriteLine("사과 " + i + " 씻는 중 (일꾼 " +
                    Thread.CurrentThread.ManagedThreadId + "번)");

                Thread.Sleep(1000);
            }

            sw.Stop();
            Console.WriteLine("다 씻음: " + sw.ElapsedMilliseconds + "ms");
        }

        public static void Chunk3()
        {
            using (Process process = Process.GetCurrentProcess())
            {
                Console.WriteLine("프로세스 ID: " + process.Id);
                Console.WriteLine("스레드 수: " + process.Threads.Count);
            }

            Console.WriteLine("작업 관리자에서 확인한 뒤 Enter를 누르세요.");
            Console.ReadLine();
        }
    }
}
