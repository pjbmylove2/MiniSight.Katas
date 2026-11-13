using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Katas.Concurrency
{
    public static class KataP13
    {
        public static void Run()
        {
            Chunk1();
        }

        public static void Chunk1()
        {
            BlockingCollection<int> basket = new BlockingCollection<int>();
            Thread inspector = new Thread(() => InspectLoop(basket));
            inspector.Start();

            for (int apple = 1; apple <= 5; apple++)
            {
                basket.Add(apple);              // 바구니에 넣기
                Console.WriteLine("넣음: 사과 " + apple);
                Thread.Sleep(40);               // 사진기: 40ms마다 한 개
            }

            basket.CompleteAdding();            // "이제 더 안 넣어요" 알림
            Console.WriteLine("사진기: 다 넣었음");
            inspector.Join();
            basket.Dispose();
        }

        private static void InspectLoop(BlockingCollection<int> basket)
        {
            foreach (int apple in basket.GetConsumingEnumerable())  // 비면 기다렸다가 꺼내기
            {
                Console.WriteLine("    검사: 사과 " + apple);
                Thread.Sleep(100);              // 검사: 100ms
            }
            Console.WriteLine("    검사 일꾼: 바구니 끝, 퇴근");
        }
    }
}
