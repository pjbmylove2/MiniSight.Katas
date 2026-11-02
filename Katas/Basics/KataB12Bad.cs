using System;
using System.Threading;
using System.Threading.Tasks;

namespace Katas.Basics
{
    public static class KataB12Bad
    {
        public static void Run()
        {
            Console.WriteLine("나쁜 설계: 일꾼에게 리모컨을 줌");
            using (CancellationTokenSource remote = new CancellationTokenSource())
            {
                Task camera = Task.Run(() => CameraBad(remote.Token));
                Task plc = Task.Run(() => PlcBad(remote));

                try
                {
                    camera.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Main: 카메라가 멈췄음");
                }

                try
                {
                    plc.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Main: PLC도 멈췄음 (정지 버튼은 안 눌렀는데?)");
                }
            }
        }

        private static void CameraBad(CancellationToken token)
        {
            for (int i = 1; i <= 5; i++)
            {
                token.ThrowIfCancellationRequested();
                Console.WriteLine("  카메라 " + i + "장");
                if (i == 2)
                {
                    Console.WriteLine("  카메라: 사진이 흐리네, 나만 쉬어야지 -> Cancel()");
                    token.Cancel();
                }
                Thread.Sleep(300);
            }
        }

        private static void PlcBad(CancellationTokenSource remote)
        {
            for (int i = 1; i <= 5; i++)
            {
                remote.Token.ThrowIfCancellationRequested();
                Console.WriteLine("    PLC 통신 " + i + "번");
                Thread.Sleep(300);
            }
        }
    }
}
