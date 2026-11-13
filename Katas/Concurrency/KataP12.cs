using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Katas.Concurrency
{
    public static class KataP12
    {
        private static int _busy;    // 팻말: 0 = 비어 있음, 1 = 사용 중
        private static int _drawn;   // 그린 사진 수
        private static int _skipped; // 건너뛴 사진 수

        public static void Run()
        {
            Thread[] painters = new Thread[10];
            for (int i = 0; i < 10; i++)
            {
                int frame = i + 1;
                painters[i] = new Thread(() => TryDraw(frame));
                painters[i].Start();
                Thread.Sleep(40);   // 사진기: 40ms마다 한 장
            }
            foreach (Thread t in painters)
            {
                t.Join();
            }
            Console.WriteLine("그림: " + _drawn + ", 건너뜀: " + _skipped);
        }

        private static void TryDraw(int frame)
        {
            if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
            {
                Interlocked.Increment(ref _skipped);
                return;
            }
            try
            {
                Console.WriteLine("사진 " + frame + " 그리는 중");
                Thread.Sleep(100);  // 그리기: 100ms
                Interlocked.Increment(ref _drawn);
            }
            finally
            {
                Volatile.Write(ref _busy, 0); // 팻말 내리기
            }
        }

        private static int _box;      // 우편함: 0 = 비어 있음, 숫자 = 사진 번호
        private static int _replaced; // 그려지기 전에 새 사진에 밀려난 수

        public static void Chunk2()
        {
            ManualResetEvent cameraDone = new ManualResetEvent(false);
            Thread painter = new Thread(() => PaintLoop(cameraDone));
            painter.Start();

            for (int frame = 1; frame <= 10; frame++)
            {
                int old = Interlocked.Exchange(ref _box, frame); // 새 사진 넣고, 있던 것 꺼내기
                if (old != 0) 
                {
                    Interlocked.Increment(ref _replaced);
                }
                Thread.Sleep(40);   // 사진기: 40ms마다 한 장
            }

            cameraDone.Set();
            painter.Join();
            cameraDone.Dispose();
            Console.WriteLine("밀려남: " + _replaced);
        }

        private static void PaintLoop(ManualResetEvent cameraDone)
        {
            while (true)
            {
                int frame = Interlocked.Exchange(ref _box, 0);
                if (frame != 0)
                {
                    Console.WriteLine("사진 " + frame + " 그리는 중");
                    Thread.Sleep(100);  // 그리기: 100ms
                }
                else if (cameraDone.WaitOne(10))
                {
                    return;
                }
            }
        }


    }
}
