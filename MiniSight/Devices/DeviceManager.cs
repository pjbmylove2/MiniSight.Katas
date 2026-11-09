using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MiniSight.Devices
{
    public sealed class DeviceManager : IDisposable
    {
        private readonly IDevice[] _devices;
        private bool _disposed;

        public DeviceManager(IEnumerable<IDevice> devices)
        {
            if (devices == null)
            {
                throw new ArgumentNullException(nameof(devices));
            }

            _devices = devices.ToArray();
        }

        public void Initialize(CancellationToken cancellationToken) 
        {
            foreach (var device in _devices) 
            {
                device.Initialize(cancellationToken);
            }
        }

        public void Start(CancellationToken cancellationToken)
        {
            List<IDevice> attempted = new List<IDevice> ();

            try
            {
                foreach (var device in _devices)
                {
                    attempted.Add(device);
                    device.Start(cancellationToken);
                }
            }
            catch (Exception startError)
            {
                List<Exception> errors = new List<Exception> ();
                errors.Add(startError);

                for (int i = attempted.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        attempted[i].Stop();
                    }
                    catch (Exception stopError)
                    {
                        errors.Add(stopError);
                    }
                }

                throw new AggregateException("장치 시작 실패, 역순 정리함", errors);
            }
        }

        public void Stop()
        {
            List<Exception> errors = new List<Exception>();

            for (int i = _devices.Length - 1; i >= 0; i--)
            {
                try
                {
                    _devices[i].Stop();
                }
                catch (Exception stopError)
                {
                    errors.Add (stopError);
                }
            }

            if (errors.Count > 0)
            {
                throw new AggregateException("일부 장치 정지 실패", errors);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            for (int i = _devices.Length - 1; i >= 0; i--)
            {
                try
                {
                    _devices[i].Dispose();          
                }
                catch (Exception disposeError)
                {
                    Console.WriteLine("정리 오류(계속 진행): " + disposeError.Message);
                }
            }
        }

    }
}
