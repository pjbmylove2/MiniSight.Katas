using System;
using System.Diagnostics;
using System.Threading;

namespace Katas.Day09
{
    public static class KataDay09
    {
        public static void Run()
        {
            //Chunk1();
            //Chunk2();
            //Chunk3();
            //Chunk4();
            Chunk5();
        }

        // 조각 0: 칠판 숫자 동시에 고치기 (race condition)
        private static int _count;

        public static void Chunk0()
        {
            _count = 0;
            Thread[] workers = new Thread[4];
            for (int i = 0; i < workers.Length; i++)
            {
                workers[i] = new Thread(AddMillion);
                workers[i].Start();
            }
            for (int i = 0; i < workers.Length; i++)
            {
                workers[i].Join();
            }
            Console.WriteLine("기대값: 4000000");
            Console.WriteLine("실제값: " + _count);
        }

        private static void AddMillion()
        {
            for (int i = 0; i < 1000000; i++)
            {
                _count++;
            }
        }

        // 조각 1: 화장실 열쇠 (lock)
        private static readonly object _sync = new object();

        public static void Chunk1()
        {
            RunFour("열쇠 없음", AddMillion);
            RunFour("열쇠 있음", AddMillionLocked);
        }

        private static void RunFour(string title, ThreadStart work)
        {
            _count = 0;
            Stopwatch sw = Stopwatch.StartNew();
            Thread[] workers = new Thread[8];
            for (int i = 0; i < workers.Length; i++)
            {
                workers[i] = new Thread(work);
                workers[i].Start();
            }
            for (int i = 0; i < workers.Length; i++)
            {
                workers[i].Join();
            }
            Console.WriteLine(title + ": " + _count + " (" + sw.ElapsedMilliseconds + "ms)");
        }

        private static void AddMillionLocked()
        {
            for (int i = 0; i < 1000000; i++)
            {
                lock (_sync)
                {
                    _count++;
                }
            }
        }

        public static void Chunk2()
        {
            RunFour("열쇠 없음", AddMillion);
            RunFour("열쇠 있음", AddMillionLocked);
            RunFour("번호표", AddMillionInterlocked);
        }
        private static void AddMillionInterlocked()
        {
            for (int i = 0; i < 1000000; i++)
            {
                Interlocked.Increment(ref _count);
            }
        }

        private static volatile bool _stopFlag;

        public static void Chunk3()
        {
            _stopFlag = false;
            Thread worker = new Thread(SpinUntilStop);
            worker.IsBackground = true;
            worker.Start();

            Thread.Sleep(500);
            _stopFlag = true;
            Console.WriteLine("멈춤 깃발 올림");

            bool finished = worker.Join(2000);
            Console.WriteLine("일꾼 끝남: " + finished);
        }

        private static void SpinUntilStop()
        {
            long spins = 0;
            while (!_stopFlag)
            {
                spins++;
            }
            Console.WriteLine("일꾼 퇴근 (돈 횟수: " + spins + ")");
        }

        public static void Chunk4()
        {
            Camera cam1 = new Camera();
            RunTwo(cam1.CloseNaive);
            Console.WriteLine("그냥 bool: 정리 " + cam1.CleanupCount + "번");

            Camera cam2 = new Camera();
            RunTwo(cam2.CloseOnce);
            Console.WriteLine("Exchange: 정리 " + cam2.CleanupCount + "번");
        }

        private static void RunTwo(ThreadStart work)
        {
            Thread a = new Thread(work);
            Thread b = new Thread(work);
            a.Start();
            b.Start();
            a.Join();
            b.Join();
        }

        private static int _busy;
        private static int _started;
        private static int _skipped;

        public static void Chunk5()
        {
            _busy = 0;
            RunTriggers("1회차");
            Thread.Sleep(300);
            RunTriggers("2회차");

        }

        private static void RunTriggers(string title)
        {
            _started = 0;
            _skipped = 0;
            Thread[] triggers = new Thread[4];
            for (int i = 0; i < triggers.Length; i++)
            {
                triggers[i] = new Thread(OnTrigger);
                triggers[i].Start();
            }
            for (int i = 0; i < triggers.Length; i++)
            {
                triggers[i].Join();
            }
            Console.WriteLine(title + ": 시작 " + _started + ", 건너뜀 " + _skipped);
        }

        private static void OnTrigger()
        {
            if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
            {
                Interlocked.Increment(ref _skipped);
                return;
            }
            try
            {
                Interlocked.Increment(ref _started);
                Console.WriteLine("  검사 중...");
                Thread.Sleep(100);
            }
            finally
            {
                //Volatile.Write(ref _busy, 0);
            }
        }
    }

    public class Camera
    {
        private bool _closedNaive;
        private int _closed;
        private int _cleanupCount;

        public int CleanupCount
        {
            get { return _cleanupCount; }
        }

        public void CloseNaive()
        {
            if (_closedNaive)
            {
                return;
            }
            //Thread.Sleep(10);
            _closedNaive = false;
            Cleanup();
        }

        public void CloseOnce()
        {
            if (Interlocked.Exchange(ref _closed, 1) == 1)
            {
                return;
            }
            Thread.Sleep(10);
            Cleanup();
        }

        private void Cleanup()
        {
            Interlocked.Increment(ref _cleanupCount);
            Console.WriteLine("  버퍼 반납");
        }
    }
}