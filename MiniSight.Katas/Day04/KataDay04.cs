using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSight.Katas.Day04
{
    public interface IKoreanPlug
    {
        void PowerOn(int volt);
    }

    public sealed class UsaSocketSdk
    {
        public void Connect110V() { Console.WriteLine("USA 110V 연결"); }
    }

    public sealed class JapanSocketSdk
    {
        public void Start(double hz) { Console.WriteLine("Japan " + hz + "Hz 시작"); }
    }

    public sealed class UsaAdapter : IKoreanPlug, IDisposable
    {
        private readonly UsaSocketSdk _sdk;
        public UsaAdapter(UsaSocketSdk sdk)
        {
            _sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
        }

        public void PowerOn(int vold)
        {
            _sdk.Connect110V();
        }

        public void Dispose() { Console.WriteLine("USA 돼지코 정리"); }
    }

    public sealed class JapanAdapter : IKoreanPlug
    {
        private readonly Func<JapanSocketSdk> _sdkFactory;
        private JapanSocketSdk _sdk;

        public JapanAdapter(Func<JapanSocketSdk> sdkFactory)
        {
            _sdkFactory = sdkFactory ?? throw new ArgumentNullException(nameof(sdkFactory));
        }

        public void PowerOn(int volt)
        {
            _sdk = _sdkFactory();
            _sdk.Start(60.0);
        }
    }

    public enum Country {  Usa, Japan }

    public static class PlugFactory
    {
        public static IKoreanPlug Create(Country country)
        {
            switch (country)
            {
                case Country.Usa: return new UsaAdapter(new UsaSocketSdk());
                case Country.Japan: return new JapanAdapter(() => new JapanSocketSdk());
                default: throw new NotSupportedException(country.ToString());
            }
        }

        public static List<IKoreanPlug> CreateAll(IEnumerable<Country> countries)
        {
            var created = new List<IKoreanPlug>();
            try
            {
                foreach (Country c in countries)
                {
                    created.Add(Create(c));
                }
                return created;
            }
            catch
            {
                for (int i = created.Count - 1; i >= 0; i--)
                {
                    var disposabled = created[i] as IDisposable;
                    if ( disposabled != null)
                    {
                        disposabled.Dispose();
                    }
                }
                throw;
            }
        }
    }

    public static class KataDay04
    {
        public static void Run()
        {
            foreach (IKoreanPlug plug in PlugFactory.CreateAll(new[] { Country.Usa, Country.Japan }))
            {
                plug.PowerOn(220);
            }
        }
    }
}
