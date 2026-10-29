using System;
using System.Runtime.CompilerServices;

namespace Katas.Day03
{
    public sealed class Trash : IDisposable
    {
        private readonly bool _suppress;
        public Trash(bool suppress) { _suppress = suppress; }
        ~Trash() { Console.WriteLine(   "소멸자 실행 (청소 아저씨 출동"); }

        public void Dispose()
        {
            Console.WriteLine(" Dispose 실행 (내가 치움)");
            if (_suppress)
            {
                GC.SuppressFinalize(this);
			}
        }
	}

    public static class FinalizerDemo
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void UseAndDispose(bool suppress)
        {
            var t = new Trash(suppress);
            t.Dispose();
        }

        public static void Run()
        {
            Console.WriteLine("1) SuppressFinalize 안 함");
            UseAndDispose(false);
            GC.Collect(); GC.WaitForPendingFinalizers();

            Console.WriteLine("2) SuppressFinalize 함");
            UseAndDispose(true);
            GC.Collect(); GC.WaitForPendingFinalizers();
		}
	}
}
