using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Katas.Basics
{
    public static class KataB13
    {
        private static int _count;
        private static readonly object _gate = new object();

        public static void Run()
        {
            _count = 0;
            RunTwoWorkers(() => 
            {
                _count++;
            });
            Console.WriteLine("1) 그냥 ++    : " + _count + "(기대값 2000000) 출력" );

            _count = 0;
            RunTwoWorkers(() => 
            { 
                lock (_gate)
                {
                    _count++;
                }
            });
            Console.WriteLine("2) lock       : " + _count);

            _count = 0;
            RunTwoWorkers(() => 
            { 
                Interlocked.Increment(ref _count);
            });
            Console.WriteLine("3) Interlocked : " + _count);
        }

        private static void RunTwoWorkers(Action addOne)
        {
            Task a = Task.Run(() => Repeat(addOne));
            Task b = Task.Run(() => Repeat(addOne));

            Task.WaitAll(a, b); 
        }

        private static void Repeat(Action addOne)
        {
            for (int i = 0; i < 1000000; i++)
            {
                addOne();
            }
        }
    }
}
