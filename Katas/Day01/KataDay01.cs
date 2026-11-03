using System.IO;
using System;
using System.Threading;

namespace Katas.Day01
{
    public static class KataDay01
    {
        public static void Run()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Exception ex = (Exception)e.ExceptionObject;
                Console.WriteLine("[마지막 그물] 잡힘: " + ex.Message);
                Console.WriteLine("[마지막 그물] 곧 죽음? " + e.IsTerminating);

                File.AppendAllText("crash.log", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + ex + Environment.NewLine);
            };


            using (Mutex key = new Mutex(false, @"Local\MiniSight_Kata01"))
            {
                bool gotKey = key.WaitOne(TimeSpan.Zero);
                if (!gotKey)
                {
                    Console.WriteLine("이미 실행 중입니다.");
                    return;
                }

                Console.WriteLine("열쇠 잡음. 실행 중... (Enter로 종료)");
                Console.ReadLine();
                throw new InvalidOperationException("일부러 터뜨림");   // ← 추가 (실험용)
                // key.ReleaseMutex();   ← 이 줄은 throw 뒤라 닿지 않아요. 주석 처리하세요
            }
        }

        public static void RunAbandoned()
        {
            Mutex key = new Mutex(false, @"Local\MiniSight_Kata01b");
            try
            {
                if (key.WaitOne(TimeSpan.FromSeconds(30)))        // 최대 30초 줄 서기
                {
                    Console.WriteLine("열쇠 잡음. 5초 뒤 반납 없이 죽음");
                    Thread.Sleep(5000);
                    Environment.Exit(1);                           // ReleaseMutex 없이 종료
                }
            }
            catch (AbandonedMutexException)
            {
                Console.WriteLine("AbandonedMutexException: 앞 주인이 반납 없이 죽음. 열쇠는 내 것");
                key.ReleaseMutex();
                Console.ReadLine();
            }
        }
    }
}
