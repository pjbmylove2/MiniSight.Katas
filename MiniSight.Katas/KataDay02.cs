using System;
using System.Collections.Generic;
using System.Threading;

namespace Katas.Day02
{
    // 콘센트 약속: 이름과 전력만 알려주면 된다.
    public interface IPowerSource
    {
        string Name { get; }
        int Watt { get; }
    }

    // 리모컨 나누기: 타이머 기능은 따로
    public interface IHasTimer
    {
        void SetTimer(int minutes);
    }

    // 선풍기: 콘센트 약속 + 타이머 약속 둘 다 구현 
    public sealed class Fan : IPowerSource, IHasTimer
    {
        public string Name { get { return "선풍기"; } }
        public int Watt { get { return 40; } }
        public void SetTimer(int minutes)
        {
            Console.WriteLine(Name + " 타이머 " + minutes + "분");
        }
    }

    public sealed class HairDryer : IPowerSource
    {
        public string Name { get { return "드라이기"; } }
        public int Watt
        {
            get { return 1500; }
        }
    }

    public sealed class ElectricKettle : IPowerSource
    {
        public string Name { get { return "전기포트";  } }
        public int Watt { get { return 1000; } }
    }

    public sealed class PowerStrip
    {
        private readonly List<IPowerSource> _plugged = new List<IPowerSource>();
        private readonly int _maxWatt;

        public PowerStrip(int maxWatt)
        {
            _maxWatt = maxWatt;
        }

        public bool Plug(IPowerSource device)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            int total = device.Watt;
            foreach (IPowerSource d in _plugged)
            {
                total += d.Watt;
            }

            if (total > _maxWatt)
            {
                Console.WriteLine(device.Name + " 과부하! 거부");
                return false;
            }

            _plugged.Add(device);
            Console.WriteLine(device.Name + " 연결 (합계 " + total + "W)");
            return true;
        }

        public void SetAllTimers(int minutes)
        {
            foreach (IPowerSource d in _plugged)
            {
                var timer = d as IHasTimer;
                if (timer != null)
                {
                    timer.SetTimer(minutes);
                }
            }
        }
    }

    public sealed class LunchBox : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            Console.WriteLine("설거지 완료 (딱 한 번)");
        }
    }

    public static class KataDay02
    {
        public static void Run()
        {
            var strip = new PowerStrip(1600);
            strip.Plug(new Fan());
            strip.Plug(new ElectricKettle());
            strip.Plug(new Fan());
            strip.Plug(new HairDryer());
            strip.SetAllTimers(30);

            var box = new LunchBox();
            box.Dispose();
            box.Dispose();

            Console.ReadLine();
        }
    }
}