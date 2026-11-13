using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Katas.Concurrency
{
    public static class KataP10
    {
        private static int _count;
        private static readonly object _key = new object(); 

        public static void Run()
        {
            //Chunk1();
            //Chunk2();
            Chunk3();
        }

        public static void Chunk1()
        {
            _count = 0;
            RunFour(AddNoKey);
            Console.WriteLine("열쇠 없이: " + _count + " (기대 400000)");

            _count = 0;
            RunFour(AddWithKey);
            Console.WriteLine("열쇠 들고: " + _count + " (기대 400000)");
        }

        private static void AddNoKey()
        {
            for (int i = 0; i < 100000; i++)
            {
                _count++;
            }
        }

        private static void AddWithKey()
        {
            for (int i = 0; i < 100000; i++)
            {
                lock (_key)
                {
                    _count++;
                }
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

        public static void Chunk2()
        {
            Stopwatch sw = Stopwatch.StartNew();
            RunFour(WriteSlow);
            Console.WriteLine("열쇠 안에서 생각: " + sw.ElapsedMilliseconds + "ms");

            sw.Restart();
            RunFour(WriteFast);
            Console.WriteLine("열쇠 밖에서 생각: " + sw.ElapsedMilliseconds + "ms");
        }

        private static void WriteSlow()
        {
            lock (_key)
            {
                Thread.Sleep(200);   // 생각(검사)을 열쇠 들고 한다
                _count++;
            }
        }

        private static void WriteFast()
        {
            Thread.Sleep(200);       // 생각(검사)은 열쇠 없이 한다
            lock (_key)
            {
                _count++;            // 칠판에 쓰는 순간만 열쇠
            }
        }

        private static readonly object _keyA = new object();
        private static readonly object _keyB = new object();

        public static void Chunk3()
        {
            Thread minsu = new Thread(() => TakeTwo("민수", _keyA, _keyB));
            Thread younghee = new Thread(() => TakeTwo("영희", _keyB, _keyA));
            minsu.Start();
            younghee.Start();
            minsu.Join();
            younghee.Join();
            Console.WriteLine("메인: 끝");
        }

        private static void TakeTwo(string name, object first, object second)
        {
            lock (first)
            {
                Console.WriteLine(name + ": 첫 열쇠 잡음");
                Thread.Sleep(100);

                lock (second)
                //if (Monitor.TryEnter(second, 1000))
                {
                    try
                    {
                        Console.WriteLine(name + ": 두 열쇠 다 잡음");
                    }
                    finally
                    {
                        Monitor.Exit(second);
                    }
                }
                //else
                //{
                //    Console.WriteLine(name + ": 1초 기다려도 두 번째 열쇠 못 받음");
                //}
            }
        }
    }
}
