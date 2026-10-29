using System;
using OpenCvSharp;

namespace MiniSight.Devices.Cameras
{
    public interface ICameraDriver : IDisposable
    {
        // 벤더 sdk가 지켜야 할 모양. FrameReady로 받은 Mat은 받은 쪽이 주인이 된다.
        event Action<Mat, long> FrameReady;
        void Open(string cameraName);
        void Start();
        void Stop();
    }
}
