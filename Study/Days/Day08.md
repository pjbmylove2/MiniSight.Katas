# Day 08 (월, 2h) · 일꾼 만들기: Thread, ThreadPool, Task, LongRunning

> 2주차는 이 스터디의 심장이다. 이번 주는 밀려도 **3주차를 줄이고 2주차를 지킨다.**

## 1. 오늘 목표

- **완료 기준**
  - `new Thread`, `ThreadPool`, `Task.Run`, `Task.Factory.StartNew(LongRunning)`의 차이를 표로 설명할 수 있다.
  - "프로그램 내내 도는 일꾼은 왜 전용 스레드인가"를 ThreadPool 고갈로 설명할 수 있다.
  - `Thread.Sleep` 대신 **깨울 수 있는 잠(`WaitHandle.WaitOne(timeout)`)**을 쓰는 이유를 실험으로 확인했다.
  - MiniSight `HealthMonitor`(전용 스레드, 1초 주기, Dispose 시 즉시 종료)를 작성했다.
  - VS2022 **스레드 창**에서 스레드 이름·Freeze/Thaw·Flag를 써 봤다.
- **최소분량**: 2, 3, 4.1, 5, 6
- **확장분량**: 4.3 LongRunning + async 함정, 7번 Freeze 실험

## 2. 🧒 초1 비유

- **스레드 = 일하는 사람**. 프로그램 = 식당. 처음엔 사장님(UI 스레드) 혼자다. 사장님이 설거지까지 하면 손님 주문(버튼 클릭)을 못 받는다(화면 멈춤).
- **ThreadPool = 인력사무소**. 잠깐 일할 알바를 빌려 온다. 일이 끝나면 돌려보낸다. 빨리 끝나는 일에 좋다. 그런데 알바를 **하루 종일 붙잡아 두면** 다른 가게가 알바를 못 구한다(스레드풀 고갈).
- **전용 스레드(new Thread / LongRunning) = 정규직 직원**. 하루 종일 같은 일(카메라 감시, 로그 쓰기)을 할 사람은 정규직으로 뽑는다.
- **깨울 수 있는 잠 = 알람 + 사장님 호출벨**. `Thread.Sleep(1000)`은 귀마개 끼고 자는 것(퇴근하라고 해도 1초 다 잘 때까지 못 들음). `stop.WaitOne(1000)`은 호출벨이 울리면 바로 깬다.

## 3. OmniSight 지도

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 감시 전용 스레드 | `OmniSight/Application/HardwareMonitorService.cs:64-83` | `IsBackground = true`, `Name`, `BelowNormal` |
| 깨울 수 있는 잠 | `OmniSight/Application/HardwareMonitorService.cs:92,122` | `_stop.WaitOne(0)` 확인, `_stop.WaitOne(1000)` 대기 |
| 최신값 게시 | `OmniSight/Application/HardwareMonitorService.cs:73,116` | `Volatile.Read/Write`로 불변 스냅샷 교체 (Day09) |
| 정리 전용 스레드 | `OmniSight/Application/SystemRetentionService.cs:20-34` | `CancellationTokenSource` + Thread |
| 토큰으로 깨우기 | `OmniSight/Application/SystemRetentionService.cs:41,62-68` | `_stop.Token.WaitHandle.WaitOne(...)`, 설정이 바뀌면 일찍 깸 |
| LongRunning 검사 작업자 | `OmniSight/Application/Inspection/InspectionPipeline.cs:93-96` | `StartNew(..., LongRunning).Unwrap()` + 주석 |
| LongRunning 출력 작업자 | `OmniSight/Application/Inspection/InspectionOutputDispatcher.cs:83-86` | 동기 루프라 전용 스레드가 끝까지 쓰임 |
| 부팅 비동기 | `OmniSight/Application/ApplicationBootstrapper.cs:45-54` | `Task.Run` (잠깐 일 → 스레드풀) |

**집에서 Claude에게:**
```text
HardwareMonitorService.cs 64~129줄과 SystemRetentionService.cs 20~70줄을 보여주고
정규직 직원·호출벨 비유로 설명해줘. Thread.Sleep 대신 WaitOne(timeout)을 쓴 이유,
IsBackground=true가 없으면 프로그램 종료 때 무슨 일이 생기는지도.
```

## 4. 왜 이렇게 썼나

### 4.1 일꾼 종류 비교

| 방법 | 누가 실행 | 언제 쓰나 | OmniSight 예 |
|---|---|---|---|
| `new Thread(Run)` | 새 OS 스레드 | 프로그램 내내 도는 루프, 이름·우선순위 지정 필요 | HardwareMonitorService, SystemRetentionService |
| `ThreadPool.QueueUserWorkItem` | 스레드풀 | 짧은 작업 (직접 쓸 일은 거의 없음) | 사용 안 함 |
| `Task.Run(...)` | 스레드풀 | 짧은 CPU 작업, UI에서 무거운 일 떼어내기, await 가능 | 부팅, 룰/AI 검사 1회 |
| `Task.Factory.StartNew(..., LongRunning)` | (보통) 새 전용 스레드 | 오래 도는 루프인데 Task로 관리(대기/예외)하고 싶을 때 | 검사 파이프라인, 출력 작업자, 로그 작성기 |
| `System.Threading.Timer` | 스레드풀 | 주기 작업. 콜백이 겹칠 수 있음 | 10분 헬스 로그 (Day19) |

### 4.2 결정과 이유

| 결정 | 이유 |
|---|---|
| 감시·정리는 `new Thread` | 이름과 우선순위(`BelowNormal`)를 줘서 **검사보다 덜 중요**하다고 OS에 알림. 디버거 스레드 창에서 이름으로 찾기 쉬움 |
| `IsBackground = true` | 메인 폼이 닫혀도 이 스레드 때문에 프로세스가 안 죽는 일을 막음. 대신 **정리는 Dispose에서 명시적으로** |
| `WaitOne(1000)` | Dispose하면 1초를 다 안 기다리고 즉시 깸 → 종료가 빠름 |
| 설정 교체 감지(`ReferenceEquals`) | 15분 대기 중에도 사용자가 경로를 바꾸면 바로 반영 |
| 검사 루프는 `LongRunning` | 무한 루프가 스레드풀 스레드를 잡으면 `Task.Run`으로 띄운 다른 짧은 작업이 줄 서서 늦어짐 |

### 4.3 LongRunning + async 람다 함정 (확장, 면접 고급 질문)

`InspectionPipeline`은 `StartNew(() => RunAsync(token), LongRunning).Unwrap()`이다. `RunAsync`는 `async` 메서드라 **첫 번째 `await`에서 전용 스레드를 반납**한다. 그 뒤의 코드는 `ConfigureAwait(false)` 때문에 **스레드풀**에서 이어서 실행된다. 즉 전용 스레드는 시작 몇 줄만 쓰고 끝난다. 효과는 `Task.Run(() => RunAsync(token))`과 거의 같다. 동기 루프(`InspectionOutputDispatcher.ReportLoop`)에서만 LongRunning이 의미가 있다.

→ 면접에서 "LongRunning에 async 람다를 넣으면?"이라고 물으면 이걸 말한다.

## 5. Kata: 계란 삶는 정규직 3명

```csharp
// KATA: Day08
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Katas.Day08
{
    public sealed class EggBoiler : IDisposable
    {
        private readonly ManualResetEvent _stop = new ManualResetEvent(false);
        private readonly Thread _worker;
        private readonly int _boilMs;
        private int _eggs;
        private int _disposed;

        public EggBoiler(string name, int boilMs)
        {
            _boilMs = boilMs;
            _worker = new Thread(Run) { Name = name, IsBackground = true, Priority = ThreadPriority.BelowNormal };
            _worker.Start();
        }

        public int Eggs { get { return Volatile.Read(ref _eggs); } }

        private void Run()
        {
            while (!_stop.WaitOne(0))
            {
                // 일하기
                Interlocked.Increment(ref _eggs);
                Console.WriteLine(Thread.CurrentThread.Name + " 계란 " + Eggs + "개 (스레드 " + Thread.CurrentThread.ManagedThreadId + ")");
                // 깨울 수 있는 잠
                if (_stop.WaitOne(_boilMs)) break;
            }
            Console.WriteLine(Thread.CurrentThread.Name + " 퇴근");
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            _stop.Set();
            if (!_worker.Join(2000)) Console.WriteLine(_worker.Name + " 2초 안에 안 끝남!");
            _stop.Dispose();
        }
    }

    public static class KataDay08
    {
        public static void Run()
        {
            var sw = Stopwatch.StartNew();
            using (new EggBoiler("영희", 300))
            using (new EggBoiler("철수", 500))
            using (new EggBoiler("민수", 5000))     // 5초 주기지만 즉시 퇴근해야 함
            {
                Thread.Sleep(1600);
            }
            Console.WriteLine("모두 퇴근까지 " + sw.ElapsedMilliseconds + "ms (5초보다 훨씬 짧아야 정상)");

            // 스레드풀 고갈 실험: 오래 붙잡는 일을 Task.Run으로 많이 띄우면?
            int min, io;
            ThreadPool.GetMinThreads(out min, out io);
            Console.WriteLine("스레드풀 최소 스레드: " + min);
            var blockers = new Task[min + 4];
            for (int i = 0; i < blockers.Length; i++) blockers[i] = Task.Run(() => Thread.Sleep(3000));
            var probe = Stopwatch.StartNew();
            Task.Run(() => { }).Wait();             // 아주 짧은 일인데...
            Console.WriteLine("짧은 작업 대기 시간: " + probe.ElapsedMilliseconds + "ms (고갈되면 수백 ms 이상)");
            Task.WaitAll(blockers);
        }
    }
}
```

실험 포인트:
1. `민수`를 `Thread.Sleep(5000)`으로 바꾸면 Dispose가 5초 걸리는지 확인 → 다시 `WaitOne`으로.
2. `IsBackground = false`로 바꾸고 `Dispose`를 빼면 콘솔이 안 꺼지는지 확인.
3. 스레드풀 실험에서 `blockers`를 `TaskCreationOptions.LongRunning`으로 바꾸면 짧은 작업 대기 시간이 어떻게 변하나?

## 6. MiniSight 오늘 단계

`App/HealthMonitor.cs : IDisposable` (직접 작성)
- 전용 스레드, 이름 `"MiniSight health"`, `BelowNormal`, 백그라운드.
- 1초마다 `Process.GetCurrentProcess()`로 `WorkingSet64`, `PrivateMemorySize64`, `Threads.Count`, `HandleCount`를 읽어 **불변 스냅샷 클래스** `HealthSnapshot`을 만들고 `Volatile.Write`로 게시.
- `Latest` 속성은 `Volatile.Read`.
- `Dispose`: 신호 → `Join(1000)` → 이벤트 Dispose.
- Bootstrapper가 생성하고, VisionSystem이 아닌 **Bootstrapper가 Dispose** (누가 만들었나 = 누가 정리하나).
- 콘솔 데모: 매초 `Latest`를 출력. Day13 메모리 누수 실험에 이 값을 쓴다.

## 7. VS2022 디버깅 실습

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | Kata 실행 중 **모두 중단(Ctrl+Alt+Break)** → 디버그 → 창 → **스레드(Ctrl+Alt+H)** | 영희/철수/민수 이름으로 보임. 이름 없는 스레드는 `<이름 없음>` → 이름 짓기가 왜 중요한지 |
| 2 | 스레드 창에서 `철수` 우클릭 → **고정(Freeze)** → F5 | 철수만 계란이 안 늘어남. 다른 스레드는 계속 |
| 3 | 다시 **고정 해제(Thaw)** | |
| 4 | 스레드 창에서 깃발 아이콘으로 **Flag** → "플래그 지정된 스레드만 표시" | 관심 스레드만 보기 |
| 5 | `Run` 안 중단점 → 조건: `Thread.CurrentThread.Name == "민수"` | 특정 스레드에서만 멈춤 |
| 6 | 디버그 → 창 → **병렬 스택(Ctrl+Shift+D, S)** | 스레드별 호출 스택을 그림으로. Day14 데드락 찾기에서 주력 |

## 8. 셀프 체크

1. `Task.Run`과 `new Thread`의 차이 3가지.
<details><summary>답</summary>① Task.Run은 스레드풀 스레드를 빌리고, new Thread는 새 OS 스레드를 만든다. ② Task는 결과·예외·완료를 await/Wait로 관찰할 수 있고, Thread는 Join만 가능하며 예외 시 프로세스가 죽는다. ③ Thread는 이름·우선순위·IsBackground·아파트 모델(STA)을 지정할 수 있다.</details>

2. 스레드풀 고갈이란? 증상은?
<details><summary>답</summary>스레드풀 스레드들이 오래 걸리는(블로킹) 작업에 묶여 새 작업이 대기하는 상태. 스레드풀은 최소 개수 이후 천천히(대략 초당 1~2개) 스레드를 늘리므로 짧은 작업, 타이머 콜백, await 이후 코드가 수백 ms~초 단위로 지연된다.</details>

3. `IsBackground = false` 스레드가 살아 있으면?
<details><summary>답</summary>모든 포그라운드 스레드가 끝날 때까지 프로세스가 종료되지 않는다. 메인 폼을 닫아도 프로세스가 남아 다음 실행 시 Mutex 때문에 "이미 실행 중"이 뜬다.</details>

4. `Thread.Sleep(1000)` 루프 대신 `WaitOne(1000)`을 쓰는 이유는?
<details><summary>답</summary>종료 신호가 오면 즉시 깨어나므로 Dispose가 빠르다. Sleep은 신호를 받을 수 없어 최대 주기만큼 종료가 늦어지고, 주기가 길면 Join 타임아웃이 난다.</details>

5. `LongRunning`에 async 람다를 넘기면 어떻게 되나?
<details><summary>답</summary>전용 스레드는 첫 await까지만 쓰이고 반납된다. 이후는 스레드풀에서 실행된다. 또 StartNew는 Task<Task>를 반환하므로 Unwrap()을 하지 않으면 바깥 Task가 첫 await에서 바로 완료된 것으로 보인다.</details>

## 9. 블로그 포인트

- 제목 예: `[C# 스레드] 하루 종일 도는 작업에 Task.Run을 쓰면 안 되는 이유 (스레드풀 고갈 실험)`
- Kata의 "짧은 작업 대기 시간" 측정값이 좋은 근거 자료.
