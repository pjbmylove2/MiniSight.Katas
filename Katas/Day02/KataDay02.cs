using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Katas.Day02
{
    public interface IPowerSource
    {
        string Name { get; }
    }

    public interface IHasTimer
    {
        void SetTimer(int minutes);
    }

    public sealed class  Fan : IPowerSource, IHasTimer
    {
        public string Name
        {
            get { return "선풍기"; }
        }

        public void SetTimer(int minutes)
        {
            Console.WriteLine(Name + " 타이머 " + minutes + "분");
        }
    }

    public sealed class  LunchBox : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            int previous = Interlocked.Exchange(ref _disposed, 1);
            if (previous != 0)
            {
                return;
            }

            Console.WriteLine("설거지 완료 (딱 한 번)");
        }
    }

    public sealed class HairDryer : IPowerSource
    {
        public string Name
        {
            get { return "헤어드라이기"; }
        }
    }

    public static class KataDay02
    {
        public static void Run()
        {
            LunchBox lunchBox = new LunchBox();

            Console.WriteLine("1) Dispose 호출");
            lunchBox.Dispose();
            Console.WriteLine("2) Dispose 호출");
            lunchBox.Dispose();

            Console.WriteLine("끝");
        }

        private static void TrySetTimer(IPowerSource device)
        {
            IHasTimer timer = device as IHasTimer;

            if (timer != null)
            {
                timer.SetTimer(30);
            }
            else
            {
                Console.WriteLine(device.Name + ": 타이머 없음");
            }
        }

        private static void ShowName(IPowerSource device)
        {
            Console.WriteLine("연결한 기기: " + device.Name);
        }
    }
}
