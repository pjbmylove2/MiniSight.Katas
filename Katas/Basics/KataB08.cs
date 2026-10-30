using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace Katas.Basics
{
    public static class KataB08
    {
        public static void Run()
        {
            string cam = "Cam1";
            int id = 7;
            double width = 10.0456;
            DateTime shotAt = new DateTime(2026, 10, 28, 9, 5, 3);

            Console.WriteLine("A: " + cam + " #" + id + " 폭=" + width);
            Console.WriteLine(string.Format("B: {0} #{1} 폭={2}", cam, id, width));
            Console.WriteLine($"C: {cam} #{id} 폭={width}");

            Console.WriteLine($"소수 2자리: {width:F2}");
            Console.WriteLine($"0 채우기: {id:D4}");
            
            Console.WriteLine($"시각: {shotAt:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"파일 이름: {cam}_{shotAt:yyyyMMdd_HHmmss}_{id:D4}.png");

            Stopwatch sw = Stopwatch.StartNew();
            Thread.Sleep(120);
            sw.Stop();
            Console.WriteLine($"검사 시간: {sw.ElapsedMilliseconds} ms");

            if (sw.Elapsed > TimeSpan.FromMilliseconds(100)) 
            {
                Console.WriteLine("[경고] 제한 100 ms 초과");
            }

            string number = "10.5";
            Console.WriteLine("Invariant로 읽기: " + double.Parse(number, CultureInfo.InvariantCulture));
            Console.WriteLine("독일어 설정으로 읽기: " + double.Parse(number, new CultureInfo("de-DE")));

            if (double.TryParse("abc", NumberStyles.Float, CultureInfo.InvariantCulture, out double c))
            {
                Console.WriteLine("숫자: " + c);
            }
            else
            {
                Console.WriteLine($"'abc'는 숫자가 아님 (예외 없이 false)");
            }
        }
    }
}
