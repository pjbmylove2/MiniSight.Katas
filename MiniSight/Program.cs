using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MiniSight
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            RegisterUnhandledExceptionHandlers();

            using (Mutex mutex = new Mutex(false, @"Global\MiniSight_SingleInstance"))
            {
                if (!TryAcquireSingleInstanceLock(mutex))
                {
                    MessageBox.Show("이미 실행 중입니다.", "MiniSight", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                try
                {
                    Application.Run(new Form1());
                }
                finally
                {
                    WriteCrashLog("[finally] 열쇠 반납");   // ← 추가
                    mutex.ReleaseMutex();
                }
            }
        }

        private static void RegisterUnhandledExceptionHandlers()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.ThreadException += (sender, args) =>
            {
                WriteCrashLog("[UI 예외] " + args.Exception);
                Environment.Exit(1);
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Exception ex = (Exception)args.ExceptionObject;
                WriteCrashLog("[백그라운드 예외] " + ex);
                Environment.Exit(1);
            };

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                WriteCrashLog("[Task 예외] " + args.Exception);   
                args.SetObserved(); // 예외를 관찰된 것으로 표시하여 프로그램이 종료되지 않도록 함
            };
        }

        private static bool TryAcquireSingleInstanceLock(Mutex mutex)
        {
            try
            {
                return mutex.WaitOne(TimeSpan.Zero, false);
            }
            catch (AbandonedMutexException)
            {
                return true;
            }
        }

        private static void WriteCrashLog(string text)
        {
            File.AppendAllText("crash.log", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + text + Environment.NewLine);
        }
    }
}
