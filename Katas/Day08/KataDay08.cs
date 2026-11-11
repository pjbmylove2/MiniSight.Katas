using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Katas.Day08
{
    // 조각 3: 계란 삶는 정규직 (전용 Thread + WaitOne + Dispose)
    internal sealed class EggBoiler : IDisposable
    {
        private readonly ManualResetEvent _stop = new ManualResetEvent(false);
        private readonly Thread _worker;
        private readonly int _boilMs;
        private bool _disposed;

        public EggBoiler(string name, int boilMs)
        {
            _boilMs = boilMs;
            _worker = new Thread(Run);
            _worker.Name = name;
            _worker.IsBackground = true;
            _worker.Priority = ThreadPriority.BelowNormal;
            _worker.Start();
        }

        private void Run()
        {
            int eggs = 0;
            while (!_stop.WaitOne(_boilMs))
            {
                eggs++;
                Console.WriteLine("  " + _worker.Name + ": 계란 " + eggs + "개");
            } 
            Console.WriteLine("  " + _worker.Name + ": 퇴근");
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            _stop.Set();                     // 실험: Join 다음으로 옮기면 모두 2초씩 시간 초과 (약 7651ms)
            if (!_worker.Join(2000))
            {
                Console.WriteLine("  " + _worker.Name + ": 2초 안에 안 끝남!");
            }
            _stop.Dispose();
        }
    }

    internal static class KataDay08
    {
        // Program.cs에서 Day08Kata.Run(); 으로 부르고, 보고 싶은 조각 한 줄만 주석을 푼다.
        public static void Run()
        {
            //Chunk0();
            //Chunk1();
            //Chunk2();
            //Chunk3();
            //Chunk4();
            //Chunk4Exception();
            //Chunk5();
            Chunk7Practice();
        }

        // 조각 0: 일꾼 한 명 보내기 (Start / Join)
        // 실험: worker.Join(); 을 주석 → "설거지 끝난 것 확인"이 먼저 찍히고, 일꾼은 끝까지 씻는다 (전경 스레드)
        public static void Chunk0()
        {
            Console.WriteLine("사장님 스레드 번호: " + Thread.CurrentThread.ManagedThreadId);

            Thread worker = new Thread(WashDishes3);
            worker.Start();
            Console.WriteLine("사장님: 일꾼 보내 놓고 바로 손님 주문 받는 중");

            worker.Join();
            Console.WriteLine("사장님: 설거지 끝난 것 확인");
        }

        private static void WashDishes3()
        {
            for (int i = 1; i <= 3; i++)
            {
                Thread.Sleep(200);
                Console.WriteLine("  일꾼(스레드 " + Thread.CurrentThread.ManagedThreadId + "): 접시 " + i + "개 씻음");
            }
        }

        // 조각 1: 이름표와 퇴근 규칙 (Name / IsBackground)
        // 실험: IsBackground = true 를 주석 → 사장님 퇴근 뒤에도 접시 5개 + "설거지 완료"까지 찍힌다
        public static void Chunk1()
        {
            Thread worker = new Thread(WashDishes5);
            worker.Name = "설거지 일꾼";
            worker.IsBackground = true;
            worker.Start();

            Thread.Sleep(500);
            Console.WriteLine("사장님: 퇴근! (Main 끝)");
        }

        private static void WashDishes5()
        {
            for (int i = 1; i <= 5; i++)
            {
                Thread.Sleep(200);
                Console.WriteLine("  [" + Thread.CurrentThread.Name + "] 접시 " + i + "개 씻음");
            }
            Console.WriteLine("  [" + Thread.CurrentThread.Name + "] 설거지 완료");
        }

        // 조각 2: 귀마개 잠(Sleep) vs 호출벨 잠(WaitOne)
        // 실험: 2000을 둘 다 5000으로 → 귀마개 약 2500ms, 호출벨 0ms
        private static readonly ManualResetEvent _bell = new ManualResetEvent(false);

        public static void Chunk2()
        {
            RunGuard("귀마개 경비", EarplugGuard);
            RunGuard("호출벨 경비", BellGuard);
        }

        private static void RunGuard(string name, ThreadStart work)
        {
            _bell.Reset();
            Thread guard = new Thread(work);
            guard.Name = name;
            guard.IsBackground = true;
            guard.Start();

            Thread.Sleep(2500);
            Stopwatch sw = Stopwatch.StartNew();
            _bell.Set();
            guard.Join();
            Console.WriteLine(name + ": 그만하라고 한 뒤 " + sw.ElapsedMilliseconds + "ms 만에 퇴근");
            Console.WriteLine();
        }

        private static void EarplugGuard()
        {
            while (!_bell.WaitOne(0))
            {
                Thread.Sleep(2000);
                Console.WriteLine("  [" + Thread.CurrentThread.Name + "] 금고 확인");
            }
        }

        private static void BellGuard()
        {
            while (!_bell.WaitOne(2000))
            {
                Console.WriteLine("  [" + Thread.CurrentThread.Name + "] 금고 확인");
            }
        }

        // 조각 3: EggBoiler 세 명, 1.65초 뒤 역순 Dispose (약 1651ms)
        public static void Chunk3()
        {
            Stopwatch sw = Stopwatch.StartNew();
            using (new EggBoiler("영희", 300))
            using (new EggBoiler("철수", 700))
            using (new EggBoiler("민수", 5000))
            {
                Thread.Sleep(1650);
            }
            Console.WriteLine("모두 퇴근까지 " + sw.ElapsedMilliseconds + "ms");
        }

        // 조각 4: 정규직(new Thread) vs 알바(Task.Run)
        public static void Chunk4()
        {
            Report("사장님");

            Thread worker = new Thread(() => Report("정규직"));
            worker.Start();
            worker.Join();

            Task<int> job = Task.Run(() =>
            {
                Report("알바");
                return 3 + 4;
            });
            Console.WriteLine("알바가 가져온 결과: " + job.Result);
        }

        // 조각 4 실험: 알바가 던진 예외는 영수증(Task)에 담겨 Result에서 AggregateException으로 다시 나온다
        public static void Chunk4Exception()
        {
            Task<int> job = Task.Run(() =>
            {
                Report("알바");
                if (DateTime.Now.Year > 2000)
                {
                    throw new InvalidOperationException("계란 깨뜨림");
                }
                return 3 + 4;
            });
            try
            {
                Console.WriteLine("알바가 가져온 결과: " + job.Result);
            }
            catch (AggregateException ex)
            {
                Console.WriteLine("사장님이 받은 예외: " + ex.InnerException.Message);
            }
            Console.WriteLine("사장님: 가게는 계속 영업 중");
        }

        private static void Report(string who)
        {
            Thread me = Thread.CurrentThread;
            Console.WriteLine(who + ": 스레드 " + me.ManagedThreadId
                + ", 스레드풀? " + me.IsThreadPoolThread
                + ", 백그라운드? " + me.IsBackground);
        }

        // 조각 5: 스레드풀 고갈
        // 실험: Task.Run(...) 줄을 Task.Factory.StartNew(() => Thread.Sleep(3000), TaskCreationOptions.LongRunning) 로 → 0ms
        public static void Chunk5()
        {
            int min;
            int io;
            ThreadPool.GetMinThreads(out min, out io);
            Console.WriteLine("인력사무소 즉시 대기 인원: " + min + "명");

            Task[] blockers = new Task[min + 4];
            for (int i = 0; i < blockers.Length; i++)
            {
                blockers[i] = Task.Run(() => Thread.Sleep(3000));
            }
            Console.WriteLine("알바 " + blockers.Length + "명을 3초씩 붙잡음");

            Stopwatch sw = Stopwatch.StartNew();
            Task.Run(() => { }).Wait();
            Console.WriteLine("아주 짧은 일 대기 시간: " + sw.ElapsedMilliseconds + "ms");

            Task.WaitAll(blockers);
        }

        // 조각 7: 스레드 창 실습용. 조각 3과 같고 60초 동안 살아 있다. F5로 실행 → 모두 중단 → 스레드 창
        public static void Chunk7Practice()
        {
            using (new EggBoiler("영희", 300))
            using (new EggBoiler("철수", 700))
            using (new EggBoiler("민수", 5000))
            {
                Console.WriteLine("60초 동안 삶는 중... (디버그 > 모두 중단 → 디버그 > 창 > 스레드)");
                Thread.Sleep(10000);
            }
            Console.WriteLine("실습 끝");
        }
    }
}
