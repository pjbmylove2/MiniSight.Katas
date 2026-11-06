using Katas.Day02;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Katas.Day05
{
    public sealed class SettingsStore
    {
        private static readonly Lazy<SettingsStore> _instance = new Lazy<SettingsStore>(() => new SettingsStore());

        public static SettingsStore Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        private SettingsStore()
        {
            Console.WriteLine("설정 창고 생성");
        }

        public int Threshold { get; set; }
    }

    public sealed class Inspector
    {
        private readonly Func<int> _getThreshold;

        public Inspector(Func<int> getThreshold)
        {
            if (getThreshold == null)
            {
                throw new ArgumentNullException(nameof(getThreshold));  
            }

            _getThreshold = getThreshold;
        }

        public bool Inspect(int brightness)
        {
            int threshold = _getThreshold();
            return brightness >= threshold;
        }
    }

    public sealed class PowerDisplay
    {
        private readonly IPowerSource _source;

        public PowerDisplay(IPowerSource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _source = source;
        }

        public void Show()
        {
            Console.WriteLine("연결한 기기: " + _source.Name);
        }

        public static class Bootstrapper
        {
            public static PowerDisplay Build()
            {
                MemoryStream buffer = new MemoryStream();
                try
                {
                    Console.WriteLine("임시 버퍼 생성");
                    IPowerSource source = new Fan();
                    return new PowerDisplay(source);
                }
                finally
                {
                    buffer.Dispose();
                    Console.WriteLine("임시 버퍼 정리");
                }
            }

            public static MemoryStream BuildBuffer()
            {
                MemoryStream buffer = new MemoryStream();
                try
                {
                    MemoryStream result = buffer;
                    buffer = null;
                    return result;
                }
                finally
                {
                    buffer?.Dispose();
                }
            }

            public static Inspector BuildInspector()
            {
                return new Inspector(() => SettingsStore.Instance.Threshold);
            }
        }

        public static class KataDay05
        {
            public static void Run()
            {
                SettingsStore store = SettingsStore.Instance;
                store.Threshold = 100;
                Inspector inspector = Bootstrapper.BuildInspector();

                Console.WriteLine("변경 전: " + inspector.Inspect(120));
                store.Threshold = 150;
                Console.WriteLine("변경 후: " + inspector.Inspect(120));

                Inspector test = new Inspector(() => 10);
                Console.WriteLine("테스트: " + test.Inspect(20));
            }
        }
    }
}
