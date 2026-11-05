using Katas.Day02;
using System;
using System.Xml.Resolvers;

namespace Katas.Day04
{
    public interface ICameraDriver : IDisposable
    {
        void Open(string name);
        void Start();
        void Stop();
    }

    public sealed class ConsoleCameraDriver : ICameraDriver
    {
        private bool _disposed;

        void IDisposable.Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Console.WriteLine("드라이버 정리");
        }

        void ICameraDriver.Open(string name)
        {
            Console.WriteLine(name + " 연결");
        }

        void ICameraDriver.Start()
        {
            Console.WriteLine("촬영 시작");
        }

        void ICameraDriver.Stop()
        {
            Console.WriteLine("촬영 정지");
        }
    }

    public enum PowerSourceKind
    {
        Fan, 
        HairDryer,
        ForeignFan
    }

    public static class PowerSourceFactory
    {
        public static IPowerSource Create(PowerSourceKind kind)
        {
            switch (kind)
            {
                case PowerSourceKind.Fan: return new Fan();
                case PowerSourceKind.HairDryer: return new HairDryer();
                case PowerSourceKind.ForeignFan:
                    return new ForeignFanAdapter();
                default: throw new NotSupportedException("지원하지 않는 전원 장치 종류: " + kind);
            }
        }
    }

    public sealed class ForeignFanSdk
    {
        public string ReadModelName()
        {
            return "외부 선풍기";
        }
    }

    public sealed class ForeignFanAdapter : IPowerSource
    {
        private readonly ForeignFanSdk _sdk = new ForeignFanSdk();

        public string Name
        {
            get { return _sdk.ReadModelName(); }
        }
    }

    public static class KataDay04
    {
        public static void Run()
        {
            ICameraDriver driver = new ConsoleCameraDriver();
            ICameraDriver borrowed = driver;

            driver.Dispose();
            Console.WriteLine("Dispose 후 driver null: " + (driver == null));
            Console.WriteLine("Dispose 후 borrowed null: " + (borrowed == null));

            driver = null;
            Console.WriteLine("대입 후 driver null: " + (driver == null));
            Console.WriteLine("대입 후 borrowed null: " + (borrowed == null));

        }
    }
}
