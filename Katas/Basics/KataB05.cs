using System;

namespace Katas.Basics
{
    public class Umbrella : IDisposable
    {
        private readonly string _name;

        public Umbrella(string name)
        {
            _name = name;
            Console.WriteLine("  " + _name + " 빌림");
        }

        public void Use(bool rainTooHard)
        {
            if (rainTooHard)
            {
                throw new InvalidOperationException("비가 너무 많이 와서 우산이 뒤집힘");
            }
            Console.WriteLine("  " + _name + " 쓰고 걸어감");
        }

        public void Dispose()
        {
            Console.WriteLine("  " + _name + " 반납");
        }
    }

    public static class KataB05
    {
        public static void Run()
        {
            Console.WriteLine("1) using");
            using (Umbrella u = new Umbrella("파랑 우산"))
            {
                u.Use(false);               
            }

            Console.WriteLine("2) 예외가 나도 using은 반납한다");
            try
            {
                using (Umbrella u = new Umbrella("빨간 우산"))
                {
                    u.Use(true);
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("  잡은 예외: " + ex.Message);
            }

            Console.WriteLine("3) using을 풀어 쓰면");
            Umbrella u1 = new Umbrella("노란 우산");
            try
            {
                u1.Use(false);
            }
            finally
            {
                u1.Dispose();
            }
        }
    }
}