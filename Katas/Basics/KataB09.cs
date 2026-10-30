using System;
using System.IO;

namespace Katas.Basics
{
    enum Judge { Ok, Ng, Error };

    class Counter
    {
        public static int TotalCreated;
        public readonly string Name;

        public Counter(string name)
        {
            Name = name;
            TotalCreated = TotalCreated + 1;
        }
    }

    public static class KataB09
    {
        public const double MinWidth = 9.8;
        public const double MaxWidth = 10.2;

        public static void Run()
        {
            double[] widths = { 10.0, 10.3, -1 };
            foreach (var x in widths)
            {
                Judge j = Check(x);
                switch (j)
                {
                    case Judge.Ok:
                        Console.WriteLine(w + " -> OK");
                        break;
                    case Judge.Ng:
                        Console.WriteLine(w + " -> NG (PLC에 NG 신호)");
                        break;
                    default:
                        Console.WriteLine(w + " -> 측정 실패 (" + j + ")");
                        break;
                }
            }

            object[] things = { "cam1", 42, 3.14, null };
            foreach (var x in things)
            {
                if (x is string s)
                {
                    Console.WriteLine("문자열: " + s.ToUpper());
                }
                else if (x is int i)
                {
                    Console.WriteLine("정수 + 1: " + (i + 1));
                }
                else if (x is null)
                {
                    Console.WriteLine("null");
                }
                else
                {
                    Console.WriteLine("기타 타입: " + x.GetType().Name);
                }
            }

            string[] kinds = { "timeout", "disk" };
            foreach (var kind in kinds)
            {
                try
                {
                    Fail(kind);
                }
                catch (TimeoutException ex)
                {
                    Console.WriteLine("재시도할 오류: " + ex.Message);
                }
                catch (IOException ex) when (ex.Message.Contains("디스크"))
                {
                    Console.WriteLine("오래된 이미지 정리 필요: " + ex.Message);
                }
            }

            Counter a = new Counter("A");
            Counter b = new Counter("B");
            Console.WriteLine("만든 개수(static): " + Counter.TotalCreated);
            Console.WriteLine("이름(readonly): " + a.Name + ", " + b.Name);
            Console.WriteLine("규격(const): " + MinWidth + " ~ " + MaxWidth);
        }

        private static void Fail(string kind)
        {
            if (kind == "timeout")
            {
                throw new TimeoutException("PLC 응답 없음");
            }
            throw new IOException("디스크 공간 부족");
        }


        private static Judge Check(double x)
        {
            if (x < 0)
            {
                return Judge.Error;
            }
            else if (x >= MinWidth && x <= MaxWidth)
            {
                return Judge.Ok;
            }
            else
            {
                return Judge.Ng;
            }
        }
    }
}
