using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MiniSight.Devices
{
    // 장치 상태 7단계
    public enum DeviceState { Created, Initializing, Ready, Running, Stopped, Faulted, Disposed }

    // 모든 장치의 기본 약속
    public interface IDevice : IDisposable
    {
        string Name { get; }
        DeviceState State { get; }
        void Initialize(CancellationToken cancellationToken);
        void Start(CancellationToken cancellationToken);
        void Stop();
    }

    // 영상을 주는 장치 약속
    public interface IImageSource : IDevice
    {
        event EventHandler<ImageFrame> FrameCaptured;
        Task<ImageFrame> WaitFrameAsync(CancellationToken cancellationToken);
    }

    // 사진 한 장 = 도시작 상자
    public sealed class ImageFrame : IDisposable
    {
        private Mat _image;
        private int _disposed; // 0=not disposed, 1=disposed

        public ImageFrame(long frameId, string sourceName, Mat image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            FrameId = frameId;
            SourceName = sourceName ?? string.Empty;
            Timestamp = DateTime.UtcNow;
            _image = image; // 이 순간부터 Mat의 주인은 ImageFrame.
        }

        public long FrameId { get; private set; }
        public string SourceName { get; private set; }
        public DateTime Timestamp { get; private set; }
        public Mat Image { get { return Volatile.Read(ref _image); } }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            Mat image = Interlocked.Exchange(ref _image, null);
            if (image != null)
            {
                image.Dispose();
            }

        }
    }

    internal class Contracts
    {
    }
}
