using OpenCvSharp;
using System;
using System.Threading;

namespace MiniSight.Devices.Cameras
{
    public sealed class FakeCameraDriver : ICameraDriver
    {
        private readonly int _intervalMs;
        private Timer _timer;
        private long _frameId;

        public event Action<Mat, long> FrameReady;

        public FakeCameraDriver(int intervalMs)
        {
            if (intervalMs <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(intervalMs));
            }
            _intervalMs = intervalMs;
        }

        public void Open(string cameraName)
        {
            _timer = new Timer(OnTick, null, Timeout.Infinite, Timeout.Infinite);
            Console.WriteLine(" [Fake] " + cameraName + " 열기");
        }

        public void Start()
        {
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
            var mat = new Mat(480, 640, MatType.CV_8UC1, Scalar.All(0));
            Cv2.PutText(mat, "#" + id, new Point(20, 260), HersheyFonts.HersheySimplex, 3, Scalar.All(255), 3);

            Action<Mat, long> handler = FrameReady;
            if (handler == null)
            {
                mat.Dispose();
                return;
            }
            handler(mat, id);
        }
    }
}
