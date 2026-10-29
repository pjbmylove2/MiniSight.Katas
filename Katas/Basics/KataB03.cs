using System;

namespace Katas.Basics
{
    public class Toy
    {
        public Toy(string name)
        {
            Console.WriteLine("  " + name + " 만들어짐");
        }
    }

    public static class KataB03
    {
        public static void Run()
        {
            Action sayHi = SayHi;
            sayHi();

            Action<string> greet = name => Console.WriteLine("안녕, " + name);
            greet("영희");

            Func<int, int, int> add = (a, b) => a + b;
            Console.WriteLine("3 + 4 = " + add(3, 4));

            Func<Toy> maker = () => new Toy("로봇");
            Console.WriteLine("아직 안 만듦");
            Toy t1 = maker();
            Toy t2 = maker();
            Console.WriteLine("같은 장난감인가? " + ReferenceEquals(t1, t2));
        }

        private static void SayHi()
        {
            Console.WriteLine("안녕!");
        }
    }
}