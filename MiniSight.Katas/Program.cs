using MiniSight.Devices.Cameras;
using MiniSight.Devices;
using OpenCvSharp;
using System;
using System.IO;
using System.Threading;

namespace MiniSight.Katas
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IImageSource cam2 = CameraFactory.Create(CameraKind.Fake, "Cam2", @"C:\MiniSight\Images\Cam1", 200);
            cam2.Initialize(CancellationToken.None);
            cam2.Start(CancellationToken.None);
            for (int i = 0; i < 7; i++)          // 5장보다 많이 받아서 순환 확인
            {
                using (ImageFrame f = cam2.WaitFrameAsync(CancellationToken.None).GetAwaiter().GetResult())
                {
                    Console.WriteLine("검사용 수신: #" + f.FrameId + " " + f.Image.Width + "x" + f.Image.Height);
                }
            }
            cam2.Dispose();
        }
    }
}
