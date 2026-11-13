using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Katas.Concurrency
{
    public static class KataP11
    {
        private static int _count;
        private static readonly object _key = new object();

        public static void Run()
        {
            //Chunk1();
            Chunk2();
        }

        private static void AddWithKey()
        {
            for (int i = 0; i < 1000000; i++)
            {
                lock (_key)
                {
                    _count++;
                }
            }
        }

        private static void AddWithTicket()
        {
            for (int i = 0; i < 1000000; i++)
            {
                Interlocked.Increment(ref _count);
                //_count++;
            }
        }

        private static void RunFour(ThreadStart work)
        {
            Thread[] workers = new Thread[4];
            for (int i = 0; i < 4; i++)
            {
                workers[i] = new Thread(work);
                workers[i].Start();
            }
            foreach (Thread t in workers)
            {
                t.Join();
            }
        }

        private static void Chunk1()
        {
            _count = 0;
            Stopwatch sw = Stopwatch.StartNew();
            RunFour(AddWithKey);
            Console.WriteLine("열쇠: " + _count + ", " + sw.ElapsedMilliseconds + "ms");

            _count = 0;
            sw.Restart();
            RunFour(AddWithTicket);
            Console.WriteLine("번호표: " + _count + ", " + sw.ElapsedMilliseconds + "ms");
        }

        private static int _total; // 들어온 사과 수
        private static int _ok;    // 그중 OK 수
        private static int _bad;   // "OK가 전체보다 많은" 이상한 칠판을 본 횟수

        public static void Chunk2()
        {
            _total = 0;
            _ok = 0;
            _bad = 0;
            RunPair(WriteTwoTickets, ReadTwoTickets);
            Console.WriteLine("번호표 두 장: 이상한 칠판 " + _bad + "번");

            _total = 0;
            _ok = 0;
            _bad = 0;
            RunPair(WriteWithKey, ReadWithKey);
            Console.WriteLine("열쇠 하나: 이상한 칠판 " + _bad + "번");
        }


        private static void WriteTwoTickets()
        {
            for (int i = 0; i < 1000000; i++)
            {
                Interlocked.Increment(ref _total); // 사과 하나 들어옴
                Interlocked.Increment(ref _ok);    // 검사해 보니 OK
            }
        }

        private static void ReadTwoTickets()
        {
            for (int i = 0; i < 1000000; i++)
            {
                int total = Volatile.Read(ref _total);
                int ok = Volatile.Read(ref _ok);
                if (ok > total)
                {
                    _bad++; // OK가 전체보다 많을 수는 없다
                }
            }
        }

        private static void WriteWithKey()
        {
            for (int i = 0; i < 1000000; i++)
            {
                lock (_key)
                {
                    _total++;
                    _ok++;
                }
            }
        }

        private static void ReadWithKey()
        {
            for (int i = 0; i < 1000000; i++)
            {
                int total;
                int ok;
                lock (_key)
                {
                    total = _total;
                    ok = _ok;
                }
                if (ok > total)
                {
                    _bad++;
                }
            }
        }


        // 쓰는 일꾼 1명과 읽는 일꾼 1명을 동시에 돌린다
        private static void RunPair(ThreadStart writer, ThreadStart reader)
        {
            Thread w = new Thread(writer);
            Thread r = new Thread(reader);
            w.Start();
            r.Start();
            w.Join();
            r.Join();
        }
    }
}
