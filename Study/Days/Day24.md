# Day 24 (수, 2h) · 기록과 마무리: 비동기 로거, 종료 체인, 멱등 Dispose, 덤프

## 1. 오늘 목표

- **완료 기준**
  - 로그 쓰기를 호출 스레드에서 하지 않고 **전용 작업자 + 유한 큐**로 넘기는 이유, 그리고 로그는 검사 결과와 달리 **가득 차면 버리고 개수를 센다**는 정책 차이를 설명할 수 있다.
  - 우선순위 큐(알람)와 일반 큐, **배치 flush**(100ms 또는 512건), **flush 장벽(TaskCompletionSource)**, **알람 키/스로틀**의 목적을 설명할 수 있다.
  - OmniSight 종료 체인 5단계(보관 서비스 → VisionSystem → 설정 저장 → AI 해제 → 로그 해제)와 **로그가 마지막인 이유**를 말할 수 있다.
  - **멱등 Shutdown**(두 번째 호출자는 첫 번째 완료를 기다림)을 구현했다.
  - VS2022에서 **덤프 파일(.dmp)을 만들고 열어서** 스레드·스택을 분석했다.
  - MiniSight: `AsyncLogger`와 종료 체인 완성.
- **최소분량**: 2, 3, 4, 5, 6.1~6.2
- **확장분량**: 6.3 알람 키, 7번 덤프

## 2. 🧒 초1 비유

- **비동기 로거 = 반 일기 담당**. 친구들이 "오늘 이런 일 있었어" 쪽지(로그)를 상자에 넣고 바로 자리로 돌아간다. 일기 담당 한 명이 모아서 공책에 쓴다. 상자가 꽉 차면? 일기는 몇 줄 빠져도 괜찮다(검사 결과는 절대 안 됨). 대신 "몇 장 못 적었음"을 센다.
- **배치 flush = 쪽지 한 장마다 공책을 펴지 않고 모아서 한 번에 쓰기**.
- **flush 장벽 = "지금까지 넣은 쪽지 다 썼으면 알려줘" 종이**. 그 종이까지 처리하면 이전 쪽지는 다 쓴 것.
- **알람 스로틀 = 같은 말 1분에 한 번만 하기**. 초당 30번 같은 오류가 나도 일기는 한 줄.
- **종료 순서 = 가게 문 닫기**. 손님 내보내기(검사 정지) → 장부 정리(설정 저장) → 기계 끄기(AI 해제) → **마지막에 일기 덮기**(로그 종료). 일기를 먼저 덮으면 그 뒤 일을 기록 못 한다.
- **멱등 = 문 잠그기 두 번 해도 괜찮음**. 두 번째 사람은 첫 번째 사람이 잠글 때까지 기다렸다가 돌아간다.

## 3. OmniSight 지도

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 큐 2개 + 깨우기 | `OmniSight/Logging/LogManager.cs:42-44,88-91` | 일반/우선순위 `BlockingCollection`, `AutoResetEvent`, LongRunning 작성기 |
| 넣기 | `OmniSight/Logging/LogManager.cs:165-185` 부근 | 바이트 예산 예약(`Reserve`) → `TryAdd` 실패면 버림 + `_dropped` |
| 바이트 예산 | `OmniSight/Logging/LogManager.cs:186-200` 부근 | CAS 루프로 큐 전체 메모리 상한 |
| flush 장벽 | `OmniSight/Logging/LogManager.cs:203-215` 부근 | TCS를 큐에 넣고 기다림 |
| 작성 루프 | `OmniSight/Logging/LogManager.cs:217-258` 부근 | 우선순위 먼저, 512건/100ms 배치, 비면 `WaitOne(20)` |
| 알람 키 | `OmniSight/Logging/LogManager.cs:121-160` 부근 | `SetAlarmActive`(처음 한 번만 기록), `ClearAlarm`, 스로틀 `ConcurrentDictionary.TryUpdate` |
| 파일 캐시 | `OmniSight/Logging/LogManager.cs:320-370` 부근 | 스트림 최대 128개, 64KB 버퍼 |
| 로거 해제 | `OmniSight/Logging/LogManager.cs:507-520` | CompleteAdding → Cancel → 깨우기 → 제한 시간 대기 |
| 종료 체인 | `OmniSight/Application/ApplicationBootstrapper.cs:287-359` | 멱등(lock + ManualResetEventSlim), 단계별 try/catch, 로그는 마지막 |
| VisionSystem 해제 | `OmniSight/Application/VisionSystem.cs:86-120` 부근 (`DisposeCore`) | 헬스 타이머 → 운전 중이면 StopInternal → 구독 해제 → 검사·카메라·장치 역순 |
| Program 최후 정리 | `OmniSight/Program.cs:98-104` | `LogManager.Instance?.Dispose()` 한 번 더 (멱등이라 안전) |
| 장애 시 종료 | `OmniSight/Program.cs:167-178` | 로그 Dispose 후 `Environment.Exit(1)` |

**집에서 Claude에게:**
```text
LogManager.cs에서 큐 생성, 넣기(Reserve/TryAdd), Flush 장벽, ProcessLogQueue 루프, Dispose 부분만 골라서
반 일기 담당 비유로 설명해줘. 검사 결과 큐는 Add(대기)인데 로그는 TryAdd(버림)인 이유도.
그다음 ApplicationBootstrapper.Shutdown(287~359) 순서를 가게 문 닫기 비유로.
```

## 4. 왜 이렇게 썼나

| 결정 | 이유 |
|---|---|
| 로그는 호출 스레드에서 안 씀 | 카메라 콜백·파이프라인이 디스크 I/O로 느려지면 안 됨 |
| 가득 차면 버림 + 카운트 | 로그 때문에 검사가 멈추는 건 본말전도. 대신 손실을 **숫자로** 남김 |
| 바이트 예산 | 건수만 제한하면 거대한 예외 문자열 몇 개로 메모리 폭발 |
| 우선순위 큐 | 일반 로그 폭주 중에도 알람은 먼저 기록 |
| 배치 쓰기 | 한 줄마다 Flush하면 디스크 쓰기 횟수 폭증 → 느림, SSD 수명 |
| 장벽 TCS | "종료 전 여기까지는 확실히 기록" 보장 |
| 알람 키 | 같은 장애를 프레임마다 기록하지 않음. 해제(Clear)도 기록 → 장애 구간 파악 |
| 종료에서 로그를 마지막에 | 앞 단계의 실패도 로그로 남겨야 함 |
| 단계마다 try/catch | 하나가 실패해도 다음 정리(설정 저장, 네이티브 해제)는 계속 |
| 멱등 Shutdown | 창 닫기, `using` 해제, 크래시 처리기가 각각 Shutdown을 부를 수 있음 |
| 두 번째 호출자는 대기 | "벌써 끝났다"고 착각하고 프로세스를 끝내 버리면 첫 번째 정리가 중간에 잘림 |

**시니어의 눈**: 종료 시 `Environment.Exit(1)` 경로는 finally 블록과 다른 스레드 정리를 건너뛴다. 그래서 그 전에 로그만이라도 **동기로 flush**한다. "죽을 때 마지막 말을 남기는 법"은 장비 프로그램의 필수 기술이다.

## 5. Kata: 반 일기 담당

```csharp
// KATA: Day24
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Katas.Day24
{
    public sealed class MiniLogger : IDisposable
    {
        private sealed class Entry { public string Text; public TaskCompletionSource<bool> Barrier; }

        private readonly BlockingCollection<Entry> _queue = new BlockingCollection<Entry>(1000);
        private readonly Task _writer;
        private readonly string _path;
        private long _dropped;
        private int _disposed;

        public MiniLogger(string path)
        {
            _path = path;
            _writer = Task.Factory.StartNew(WriteLoop, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public long Dropped { get { return Interlocked.Read(ref _dropped); } }

        public void Log(string message)
        {
            if (Volatile.Read(ref _disposed) != 0) return;
            var e = new Entry { Text = DateTime.Now.ToString("HH:mm:ss.fff") + " [" + Thread.CurrentThread.ManagedThreadId + "] " + message };
            bool added;
            try { added = _queue.TryAdd(e); } catch (InvalidOperationException) { added = false; }   // CompleteAdding 이후
            if (!added) Interlocked.Increment(ref _dropped);                                            // 버리고 센다
        }

        public bool Flush(int timeoutMs)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            try { if (!_queue.TryAdd(new Entry { Barrier = tcs }, timeoutMs)) return false; }
            catch (InvalidOperationException) { return false; }
            return tcs.Task.Wait(timeoutMs);
        }

        private void WriteLoop()
        {
            using (var writer = new StreamWriter(_path, true, new UTF8Encoding(true), 65536))
            {
                int batch = 0;
                foreach (Entry e in _queue.GetConsumingEnumerable())
                {
                    if (e.Barrier != null) { writer.Flush(); batch = 0; e.Barrier.TrySetResult(true); continue; }
                    writer.WriteLine(e.Text);
                    if (++batch >= 256 || _queue.Count == 0) { writer.Flush(); batch = 0; }   // 모아서 쓰기
                }
                writer.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " 버린 로그 " + Dropped + "건");
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;   // 멱등
            _queue.CompleteAdding();
            if (!_writer.Wait(3000)) Console.WriteLine("로그 작성기 3초 내 종료 실패");
            _queue.Dispose();
        }
    }

    public sealed class Shop : IDisposable
    {
        private readonly object _sync = new object();
        private readonly ManualResetEventSlim _done = new ManualResetEventSlim(false);
        private bool _started;
        private readonly MiniLogger _log;
        public Shop(MiniLogger log) { _log = log; }

        public void Dispose()
        {
            bool owner;
            lock (_sync) { owner = !_started; _started = true; }
            if (!owner) { _done.Wait(); _log.Log("두 번째 호출자: 첫 번째 종료 완료를 기다림"); return; }
            try
            {
                Step("1. 손님 내보내기(검사 정지)", false);
                Step("2. 장부 정리(설정 저장)", true);      // 실패해도 다음 단계 계속
                Step("3. 기계 끄기(AI 해제)", false);
            }
            finally { _done.Set(); }
        }

        private void Step(string name, bool fail)
        {
            try
            {
                Thread.Sleep(100);
                if (fail) throw new IOException("디스크 오류");
                _log.Log(name + " 완료");
            }
            catch (Exception ex) { _log.Log(name + " 실패: " + ex.Message + " (계속 진행)"); }
        }
    }

    public static class KataDay24
    {
        public static void Run()
        {
            string path = @"C:\MiniSight\kata24.log";
            using (var log = new MiniLogger(path))
            {
                Parallel.For(0, 5000, i => log.Log("프레임 " + i));        // 폭주
                Console.WriteLine("버린 로그: " + log.Dropped);
                var shop = new Shop(log);
                Task a = Task.Run(() => shop.Dispose());
                Task b = Task.Run(() => shop.Dispose());                   // 동시에 두 번
                Task.WaitAll(a, b);
                Console.WriteLine("flush 장벽: " + log.Flush(2000));
            }                                                               // 로그는 마지막에
            Console.WriteLine(File.ReadAllText(path).Length + "자 기록됨: " + path);
        }
    }
}
```

## 6. MiniSight 오늘 단계

### 6.1 `Logging/AsyncLogger.cs`
- Kata를 기반으로: 일반 큐(10000) + 알람 큐(1000), 일 단위 파일(`yyyyMMdd_System.log`, `_Alarm.log`), 배치 flush 100ms/512건.
- 정적 접근: `Log.Info(...)`, `Log.Alarm(...)` (Lazy 싱글톤, Day05).
- `SetAlarmActive(key, msg)` / `ClearAlarm(key)` — `ConcurrentDictionary<string, byte>`, 처음 활성화와 해제만 기록.
- `InfoThrottled(key, msg, TimeSpan)` — `ConcurrentDictionary<string, long>` + `TryUpdate` CAS.

### 6.2 종료 체인
- `Bootstrapper.Shutdown()`: Kata의 `Shop.Dispose` 구조. 단계: HealthMonitor → VisionSystem.Dispose → 설정 저장 → AI 레지스트리 → PlcClient(장치 안에 있으면 VisionSystem이 처리) → `Log.Flush(2000)` → 로거 Dispose.
- `Program`의 `finally`에도 `Log.Instance.Dispose()` (멱등).
- 크래시 처리기: 로그 → Flush(1000) → `Environment.Exit(1)`.
- 확인: 종료 로그의 순서가 단계 순서와 같은지. 종료 후 작업 관리자에 MiniSight.exe가 남지 않는지.

### 6.3 (확장) 파이프라인 오류 알람
- `InspectionPipeline` 프레임 오류에서 `Log.SetAlarmActive("Inspection." + source, ...)`, 성공하면 `ClearAlarm`. 오류 영상 10장 연속 → 알람 1줄 + 해제 1줄만 남는지.

## 7. VS2022 디버깅 실습: 덤프로 "멈춘 프로그램" 분석

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | Day14 실험 C(정지 중 UI 멈춤)를 재현한 상태로 **모두 중단** → 디버그 → **덤프를 다른 이름으로 저장** → "힙 포함 미니 덤프" | `.dmp` 파일 |
| 2 | 디버깅 중지 → 파일 → 열기 → `.dmp` → **"관리 전용으로 디버그"** | 멈춘 순간의 스냅샷 |
| 3 | 스레드 창, **병렬 스택** | 멈춘 이유가 그대로 보인다 (UI 스레드 → Stop → Wait, 작업자 → Add 대기) |
| 4 | 현장 방식: 작업 관리자 → MiniSight.exe 우클릭 → **덤프 파일 만들기** → 같은 방식으로 열기 | 디버거 없는 현장 PC에서 멈춤을 가져오는 방법 |
| 5 | (확장) `dotnet-dump`/WinDbg + SOS의 `!clrstack`, `!syncblk`(lock 소유자) | dotnet-debugging 스킬 참고. 이직 면접에서 차별화 포인트 |

## 8. 셀프 체크

1. 로그 큐는 버리고 검사 결과 큐는 기다리는 이유는?
<details><summary>답</summary>검사 결과는 생산 기록이라 손실이 품질·추적성 문제가 된다. 로그는 진단 보조라 일부 손실보다 검사 정지가 더 큰 피해다. 대신 손실 건수를 기록해 "로그가 빠진 구간"을 알 수 있게 한다.</details>

2. 배치 flush의 장점과 위험은?
<details><summary>답</summary>장점: 디스크 쓰기 횟수가 줄어 빠르고 장치 부하가 적다. 위험: 크래시 시 마지막 배치(최대 100ms 또는 512건)가 손실될 수 있다. 그래서 크래시 경로와 종료 경로에서 명시적으로 flush한다.</details>

3. Shutdown을 멱등으로 만들 때 두 번째 호출자가 그냥 return하면 안 되는 이유는?
<details><summary>답</summary>첫 번째 호출이 아직 진행 중인데 두 번째 호출자(예: using 해제 후 프로세스 종료)가 끝났다고 판단하고 진행하면 정리 도중 프로세스가 종료되어 설정 저장·핸들 해제가 잘린다. 첫 번째 완료 신호를 기다려야 한다.</details>

4. 종료 단계마다 try/catch를 두는 이유는?
<details><summary>답</summary>앞 단계 실패(예: 장치 정지 오류)가 뒤 단계(설정 저장, 네이티브 핸들 해제, 로그 flush)를 막으면 피해가 커진다. 각 단계 실패는 기록하고 다음 단계를 계속한다.</details>

5. 덤프 파일로 알 수 있는 것과 없는 것은?
<details><summary>답</summary>알 수 있음: 그 순간 모든 스레드 스택, 관리 힙 객체(힙 포함 시), lock 소유, 예외 정보. 알 수 없음: 그 이전의 시간 흐름(로그가 필요), 이후 상태, 외부 장치 상태. 그래서 로그 + 덤프를 같이 수집한다.</details>

## 9. 블로그 포인트

- 제목 예: `[C#] 장비 프로그램의 로그는 버려도 되고 결과는 안 된다: 비동기 로거와 종료 순서`
- Kata의 "버린 로그 N건" 출력, 멱등 종료 로그, 덤프로 멈춤 분석 과정(MiniSight).
