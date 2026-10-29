using System;
using System.Media;
using System.Runtime.InteropServices;

namespace Katas.Basics
{
    public interface ISpeaker
    {
        string Name { get; }
        void Speak();
    }

    public abstract class Animal : ISpeaker
    {
        public abstract string Name { get; }
        public void Speak()
        {
            Console.WriteLine("[" + Name + "]");
            Console.WriteLine("  " + Sound());
            Console.WriteLine("  " + Bye());
        }

        protected abstract string Sound();
        protected virtual string Bye()
        {
            return "안녕";
        }
    }

    public class Dog : Animal
    {
        public override string Name => "강아지";
        protected override string Sound()
        {
            return "멍멍";
        }
    }

    public class Cat : Animal
    {
        public override string Name => "고양이";
        protected override string Sound()
        {
            return "야옹";
        }

        protected override string Bye()
        {
            return base.Bye() + "... 냥";
        }
    }

    public static class KataB02
    {
        public static void Run()
        {
            ISpeaker[] list = { new Dog(), new Cat() };
            foreach (ISpeaker s in list) 
            {
                s.Speak();
            }
        }
    }
}