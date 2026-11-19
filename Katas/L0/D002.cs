using System;

namespace Katas.L0
{
    public static class D002
    {
        public static void Run()
        {
            Chunk0();
        }

        public static void Chunk0()
        {
            int? a = 5;
            int? b = a;
            b = 9;
            Console.WriteLine("a=" + a + " b=" + b);
            Console.WriteLine("int? 값 형식? " + typeof(int?).IsValueType);

            int[] x = { 5 };
            int[] y = x;
            y[0] = 9;
            Console.WriteLine("x[0]=" + x[0] + " y[0]=" + y[0]);
            Console.WriteLine("int[] 값 형식? " + typeof(int[]).IsValueType);
        }
    }
}
