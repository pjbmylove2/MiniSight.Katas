using OpenCvSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MiniSight.Devices.Cameras
{
    public sealed class DriverCameraAdapter : DeviceBase, IImageSource
    {
        private readonly string _name;
        private readonly Func<ICameraDriver> _driverFactory; // 드라이버가 아니라 '만드는 법'
        private readonly BlockingCollection<ImageFrame> _queue = new BlockingCollection<ImageFrame>(8);
        private ICameraDriver _driver;

        public event EventHandler<ImageFrame> FrameCaptured;

        public DriverCameraAdapter(string name, Func<ICameraDriver> driverFactory)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _driverFactory = driverFactory ?? throw new ArgumentNullException(nameof(driverFactory));
        }

        public override string Name => _name;

        protected override void OnInitialize(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReleaseDriver();
            ICameraDriver driver = _driverFactory();
            driver.FrameReady += OnFrame;
            try
            {
                driver.Open(_name);
                _driver = driver;
            }
            catch
            {
                driver.FrameReady -= OnFrame;
                driver.Dispose();
                throw;  
            }
        }

        protected override void OnStart(CancellationToken cancellationToken)
        {
            _driver.Start();
        }

        protected override void OnStop()
        {
            _driver?.Stop();
        }

        protected override void OnDispose()
        {
            ReleaseDriver();
            _queue.CompleteAdding();
            ImageFrame left;
            while (_queue.TryTake(out left)) { left.Dispose(); }    
            _queue.Dispose();
        }

        public Task<ImageFrame> WaitFrameAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => _queue.Take(cancellationToken), cancellationToken);
        }

        private void ReleaseDriver() 
        {
            ICameraDriver old = _driver;    
            _driver = null;

            if (old == null) { return; }
            old.FrameReady -= OnFrame;
            old.Dispose();
        }

        // 드라이버의 작업자 스레드에서 불린다. 여기서 예외가 밖으로 나가면 프로그램이 죽으므로 전부 잡는다. 
        private void OnFrame(Mat mat, long id)
        {
            ImageFrame frame = null;
            try
            {
                frame = new ImageFrame(id, _name, mat);
                mat = null;

                try { FrameCaptured?.Invoke(this, frame); }
                catch (Exception ex) { Console.WriteLine(" [" + _name + "] 미리보기 오류: " + ex.Message); }

                if (_queue.TryAdd(frame))
                {
                    frame = null;
                }
                else
                {
                    Console.WriteLine(" [" + _name + "] 큐 가득 참 -> #" + id + " 버림");
                }
            }
            catch (Exception ex) 
            {
                Console.WriteLine(" [" + _name + "] 프레임 처리 오류: " + ex.Message);
            }
            finally
            {
                frame?.Dispose();
                mat?.Dispose();                     
            }
        }
    }
}
