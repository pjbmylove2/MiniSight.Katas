using System;

namespace Katas.Basics
{
    public class Lunchbox
    {
        public string Menu { get; set; }
    }

    public class Student
    {
        private readonly string _name;

        public Student(string name)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string Name => _name;
        public string Hello() => "hello i'm " + _name;
    }

    public static class KataB01
    {
        public static void Run()
        {
            Lunchbox m = new Lunchbox();
            m.Menu = "ddong";
            Lunchbox f = null;

            string a = m?.Menu;
            string b = f?.Menu;
            Console.WriteLine(a);
            Console.WriteLine(b);
            string c = f?.Menu ?? "good";
            Console.WriteLine(c);

            Student s = new Student(null);
            Console.WriteLine(s.Name);
            Console.WriteLine(s.Hello());
        }
    }
}