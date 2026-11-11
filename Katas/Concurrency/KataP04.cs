using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Katas.Concurrency
{
    public static class KataP04
    {
        public static void Chunk1()
        {
            using (ManualResetEvent stop = new ManualResetEvent(false))
            {
                Thread worker = new Thread(() => WashApples(stop));

                Console.WriteLine("정지하려면 Enter를 누르세요.");
                worker.Start();

                try
                {
                    Console.ReadLine();
                }
                finally
                {
                    stop.Set();    // 1. 정지 신호 보내기
                    worker.Join(); // 2. 작업자가 끝날 때까지 기다리기
                }
            }
        }

        public static void Run()
        {
            Chunk3();
        }

        public static void Chunk2()
        {
            using (ManualResetEvent stop = new ManualResetEvent(false))
            {
                Console.WriteLine("처음: " + stop.WaitOne(0));

                stop.Set();
                Console.WriteLine("첫 번째 확인: " + stop.WaitOne(0));
                Console.WriteLine("두 번째 확인: " + stop.WaitOne(0));

                stop.Reset();
                Console.WriteLine("Reset 후: " + stop.WaitOne(0));
            }
        }

        private static void WashApples(ManualResetEvent stop)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            stopwatch.Start();
            while (!stop.WaitOne(500))
            {

                Console.WriteLine("사과 한 개 씻음 " + stopwatch.ElapsedMilliseconds);
                stopwatch.Stop();
            }

            Console.WriteLine("작업자: 정지 신호 받음");
        }

        public static void Chunk3()
        {
            using (ManualResetEvent stop = new ManualResetEvent(false))
            {
                Thread first = new Thread(() => WaitForStop(stop, "민수"));
                Thread second = new Thread(() => WaitForStop(stop, "영희"));

                bool secondStarted = false;
                first.Start();

                try
                {
                    second.Start();
                    secondStarted = true;

                    Console.WriteLine("Enter를 누르면 정지 신호를 보냅니다.");
                    Console.ReadLine();
                }
                finally
                {
                    stop.Set(); // 두 작업자에게 같은 신호를 보냄
                    first.Join();

                    if (secondStarted)
                    {
                        second.Join();
                    }
                }
            }

            Console.WriteLine("메인: 정리 완료");
        }

        private static void WaitForStop(ManualResetEvent stop, string name)
        {
            stop.WaitOne(); // 정지 신호가 올 때까지 대기
            Console.WriteLine(name + ": 정지 신호 받음");
        }
    }
}
