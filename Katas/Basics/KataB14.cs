// 시작:13:34


using System;
using System.Threading;

namespace Katas.Basics
{
    public sealed class SimpleCamera : IDisposable
    {
        private long _frameId;
        private Timer _timer;
        private readonly int _intervalMs;

        public event EventHandler<long> FrameArrived;

        public SimpleCamera(int intervalMs)
        {
            _intervalMs = intervalMs;
        }

        public void Start()
        {
            if (_timer == null)
            {
                _timer = new Timer(OnTick, null, Timeout.Infinite, Timeout.Infinite);
            }
            _timer.Change(0, _intervalMs);
        }

        public void Stop()
        {
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _timer = null;
        }

        private void OnTick(object state)
        {
            long id = Interlocked.Increment(ref _frameId);
            Console.WriteLine("[스레드 " + Thread.CurrentThread.ManagedThreadId + "] 찰칵 #" + id);
            FrameArrived?.Invoke(this, id);
        }
    }

    public static class KataB14
    {
        public static void Run()
        {
            Console.WriteLine("[스레드 " + Thread.CurrentThread.ManagedThreadId + "] Main 시작");
            using (CancellationTokenSource remote = new CancellationTokenSource())
            using (SimpleCamera cam = new SimpleCamera(300))
            {
                cam.FrameArrived += (sender, id) => 
                {
                    Console.WriteLine("    받음 #" + id);
                    if (id == 5)
                    {
                        remote.Cancel();
                    }
                };

                cam.Start();
                remote.Token.WaitHandle.WaitOne();
                cam?.Stop();
                Console.WriteLine("[스레드 " + Thread.CurrentThread.ManagedThreadId + "] Main: 5장 받고 멈춤");
            }
        }
    }
}


// 종료: 13:46