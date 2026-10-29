using MiniSight.Devices;
using OpenCvSharp;
using System;
using System.Threading.Tasks;

namespace Katas.Day02
{
    public static class KataDay02Frame
    {
        public static void Run()
        {
            var mat = new Mat(100, 200, MatType.CV_8UC1, Scalar.All(0));
            var frame = new ImageFrame(1, "Cam1", mat);
            mat = null; // 이제 mat는 ImageFrame이 소유하므로 null 처리

            Console.WriteLine("너비: " + frame.Image.Width);
            Console.WriteLine("높이: " + frame.Image.Height);

            frame.Dispose();
            frame.Dispose();
            Console.WriteLine("Dispose 후 Image가 null? " + (frame.Image == null));

            // 8개 스레드가 동시에 dispose해도 안전한지
            var frame2 = new ImageFrame(2, "Cam2", new Mat(50, 50, MatType.CV_8UC3, Scalar.All(255)));
            Parallel.For(0, 8, i => frame2.Dispose());
            Console.WriteLine("동시 Dispose 8번 후에도 프로그램 살아 있음");

            Console.ReadLine();
        }
    }
}
