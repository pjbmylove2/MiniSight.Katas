using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace Katas.Concurrency
{
    public static class KataP14
    {
        public static void Run()
        {
            //Chunk1();
        }

        public static void Chunk1()
        {
            BlockingCollection<int> basket = new BlockingCollection<int>(3);
            Stopwatch watch = Stopwatch.StartNew();
            Thread inspector = new Thread(() => InspectLoop(basket));
            inspector.Start();

            for (int apple = 1; apple <= 8; apple++)
            {
                basket.Add(apple);   // 꽉 차 있으면 자리가 날 때까지 기다린다
                Console.WriteLine("넣음: 사과 " + apple + " (바구니 " + basket.Count + "개)");
                Thread.Sleep(10);    // 사진기: 10ms마다 한 개 (아주 빠름)
            }
            Console.WriteLine("사진기 끝: " + watch.ElapsedMilliseconds + "ms");

            basket.CompleteAdding();
            inspector.Join();
            Console.WriteLine("검사 끝: " + watch.ElapsedMilliseconds + "ms");
            basket.Dispose();
        }

        private static void InspectLoop(BlockingCollection<int> basket)
        {
            foreach (int app in basket.GetConsumingEnumerable()) 
            {
                Thread.Sleep(100);
            }
        }

        public static void Chunk2()
        {
            BlockingCollection<int> basket = new BlockingCollection<int>(3);
            int dropped = 0;
            Stopwatch watch = Stopwatch.StartNew();
            Thread inspector = new Thread(() => InspectAndPrint(basket));
            inspector.Start();

            for (int apple = 1; apple <= 8; apple++)
            {
                if (!basket.TryAdd(apple))         // 자리가 없으면 기다리지 않고 false
                {
                    dropped++;
                    Console.WriteLine("버림: 사과 " + apple);
                }
                Thread.Sleep(10);                  // 사진기: 10ms마다 한 개
            }

            Console.WriteLine("사진기 끝: " + watch.ElapsedMilliseconds + "ms, 버림 " + dropped + "개");

            basket.CompleteAdding();
            inspector.Join();
            basket.Dispose();
        }

        private static void InspectAndPrint(BlockingCollection<int> basket)
        {
            foreach (var apple in basket.GetConsumingEnumerable())
            {
                Console.WriteLine("  검사: 사과 " + apple);
                Thread.Sleep(100);
            }
        }
    }
}
