using System;
using System.Threading;
using System.Threading.Tasks;

namespace Katas.Day01
{
    public static class KataDay01
    {
        public static void Run()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Console.WriteLine("[AppDomain] " + e.ExceptionObject.ToString());
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Console.WriteLine("[Task] " + e.Exception.InnerException?.Message);
                e.SetObserved();
            };

            bool createNew;
            using (var mutex = new Mutex(false, @"Local\MiniSight_Kata01", out createNew))
            {
                bool acquired;
                try
                {
                    acquired = mutex.WaitOne(TimeSpan.Zero, false);
                }
                catch (AbandonedMutexException)
                {
                    Console.WriteLine("[Mutex] 이전 프로세스 비정상 종료 감지. 열쇠를 이어받습니다.");
                    acquired = false;
                }

                if (!acquired)
                {
                    Console.WriteLine("이미 실행 중입니다.");
                    Console.ReadLine();
                    return;
                }

                try
                {
                    Console.WriteLine("실행 중... createNew=" + createNew + "( Enter로 종료)");

                    Task.Run(() =>
                    {
                        throw new InvalidOperationException("아무도 안 본 Task 오류");
                    });

                    Thread.Sleep(200);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    Console.ReadLine();
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }
    }
}
