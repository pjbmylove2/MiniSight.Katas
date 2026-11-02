using System;
using System.Threading;
using System.Threading.Tasks;

namespace Katas.Basics
{
    public static class KataB12
    {
        public static void Run()
        {
            Console.WriteLine("1) 정지 버튼");
            using (CancellationTokenSource remote = new CancellationTokenSource())
            {
                CancellationToken token = remote.Token;
                Task work = Task.Run(() => Wash(token));
                Thread.Sleep(1000);
                Console.WriteLine("정지 버튼 누름!");
                remote.Cancel();

                try
                {
                    work.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("세탁기: 취소되어 멈췄음");
                }
            }

            Console.WriteLine("2) 시간 지나면 자동 정지");
            using (CancellationTokenSource timeout = new CancellationTokenSource())
            {
                timeout.CancelAfter(700);
                try
                {
                    Wash(timeout.Token);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("타임아웃으로 멈췄음");
                }
            }
        }

        private static void Wash(CancellationToken token)
        {
            for (int i = 1; i <= 10; i++)
            {
                token.ThrowIfCancellationRequested();
                Console.WriteLine("  세탁 " + i + "분");
                Thread.Sleep(300);
            }
            Console.WriteLine("  세탁 완료");
        }

    }
}
