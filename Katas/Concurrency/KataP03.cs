using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Katas.Concurrency
{
    public static class KataP03
    {
        public static void Chunk2()
        {
            Thread first = new Thread(() => WashApple("민수", 100));
            Thread second = new Thread(() => WashApple("영희", 200));

            first.IsBackground = true;
            second.IsBackground = true;

            first.Start();
            second.Start();

            first.Join();
            second.Join();

            Console.WriteLine("모두 완료");
        }

        private static void WashApple(string name, int milliseconds)
        {
            Thread.Sleep(milliseconds);
            Console.WriteLine(name + " 완료");
        }

        public static void Run()
        {
            Chunk2();
        }
    }
}
