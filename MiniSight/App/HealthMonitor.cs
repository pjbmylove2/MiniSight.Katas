using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MiniSight.App
{
    public sealed class HealthMonitor : IDisposable
    {
        private readonly ManualResetEvent _stop = new ManualResetEvent(false);
        private readonly Thread _worker;
        private readonly int _intervalMs;
        private HealthSnapshot _latest;
        private bool _disposed;

        public HealthMonitor(int intervalMs)
        {
            _intervalMs = intervalMs;
            _latest = Measure();                        // 켜자마자 1번 재 두기

            _worker = new Thread(Run);
            _worker.Name = "MiniSight health";
            _worker.IsBackground = true;
            _worker.Priority = ThreadPriority.BelowNormal;
            _worker.Start();
        }

        private static HealthSnapshot Measure()
        {
            using (Process me = Process.GetCurrentProcess())
            {
                return new HealthSnapshot(
                    DateTime.Now,
                    me.WorkingSet64 / 1024 / 1024,
                    me.PrivateMemorySize64 / 1024 / 1024,
                    me.Threads.Count,
                    me.HandleCount);
            }
        }

        public HealthSnapshot Latest
        {
            get { return Volatile.Read(ref _latest); }
        }

        private void Run()
        {
            while (!_stop.WaitOne(_intervalMs))
            {
                try
                {
                    Volatile.Write(ref _latest, Measure());
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[health] 측정 실패: " + ex.Message);
                }
            }
        }


        public void Dispose() 
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            _stop.Set();
            if (!_worker.Join(1000))
            {
                Debug.WriteLine("[health] 1초 안에 안 끝남");
            }
            _stop.Dispose();
        }
    }
}
