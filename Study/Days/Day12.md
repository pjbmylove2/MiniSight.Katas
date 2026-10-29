# Day 12 (금, 2h) · 기다리는 법: async/await, 취소, 타임아웃, TaskCompletionSource

## 1. 오늘 목표

- **완료 기준**
  - `await`가 "스레드를 멈추는 것"이 아니라 "**나중에 이어서 하라고 예약하고 스레드를 돌려주는 것**"임을 설명할 수 있다.
  - `CancellationToken`은 강제 종료가 아니라 **협조 요청**임을 설명할 수 있다.
  - `Task.WhenAny`로 타임아웃을 만들고, **진 쪽 Task의 예외를 관찰**해야 하는 이유를 말할 수 있다.
  - 콜백/이벤트 API를 `TaskCompletionSource`로 await 가능하게 바꿀 수 있다.
  - `ConfigureAwait(false)`, `async void`, `.Wait()/.Result` 데드락의 원리를 설명할 수 있다.
  - MiniSight: `TimeoutHelper.WithTimeout`과 가짜 `ConnectAsync`(TCS + 타임아웃 + 취소) 작성.
- **최소분량**: 2, 3, 4.1~4.3, 5의 1~3
- **확장분량**: 회사 예제 프로젝트 `Examples/FaTaskAwaitDemo` 읽기, 4.4

## 2. 🧒 초1 비유

- **await = 식당 진동벨**. 음식(I/O 결과)을 주문하고 진동벨을 받는다. 카운터 앞에 서서 기다리지 않고(스레드 블로킹 X) 자리에 가서 다른 일을 한다. 벨이 울리면 아무 직원(스레드풀 스레드)이나 이어서 서빙한다.
- **.Wait() / .Result = 카운터 앞에 서서 기다리기**. 그 사이 아무 일도 못 한다. 더 나쁜 경우: 사장님(UI 스레드)이 카운터에 서서 기다리는데, 음식이 "사장님이 직접 서빙해야 하는" 메뉴라면? 영원히 안 끝난다 = **데드락**.
- **CancellationToken = "그만해도 돼" 쪽지**. 쪽지를 받은 요리사가 **알아서** 정리하고 멈춘다. 강제로 끌고 나가지 않는다. 쪽지를 안 보는 요리사는 안 멈춘다.
- **WhenAny 타임아웃 = 모래시계와 경주**. 음식이 먼저 나오면 OK, 모래시계가 먼저면 "시간 초과". 그런데 늦게라도 나온 음식은 **누군가 치워야** 한다(진 Task 관찰).
- **TaskCompletionSource = 진동벨을 내가 만들기**. "전화 오면 알려줘"(콜백) 같은 옛날 방식을 진동벨(Task)로 바꾼다.

## 3. OmniSight 지도

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 개념 가이드 | `OmniSight/docs/reference/FA_TASK_AWAIT_GUIDE.ko.md` §3, §5, §7, §11, §12, §13 | 회사가 정한 async 규칙 |
| 예제 프로젝트 | `Examples/FaTaskAwaitDemo/FaInspectionLine.cs`, `TimeoutHelper.cs` | 취득 취소 → 완료 → drain 3초 → 비상 취소 |
| DNS 타임아웃 | `OmniSight/Devices/LegacyOcrSocketDevice.cs:525-535` | `WhenAny(dnsTask, Delay)` + 진 쪽 `ObserveFault` |
| TCS로 소켓 연결 | `OmniSight/Devices/LegacyOcrSocketDevice.cs:575-614` | `RunContinuationsAsynchronously`, `Completed` 이벤트 → `TrySetResult` |
| 타임아웃 후 정리 | `OmniSight/Devices/LegacyOcrSocketDevice.cs:594-603` | 소켓 Close → **완료를 끝까지 await** → TimeoutException |
| 진 Task 관찰 | `OmniSight/Devices/LegacyOcrSocketDevice.cs:616-623` | `ContinueWith(OnlyOnFaulted)`로 `Exception` 읽기 |
| 파이프라인 await | `OmniSight/Application/Inspection/InspectionPipeline.cs:198,210,278,283` | 모두 `ConfigureAwait(false)` |
| 취소 예외 필터 | `OmniSight/Application/Inspection/InspectionPipeline.cs:212,316` | `catch (OperationCanceledException) when (token.IsCancellationRequested)` |
| 동기 위에 async | `OmniSight/Application/Inspection/InspectionOutputDispatcher.cs:240,266` | 전용 스레드에서 `GetAwaiter().GetResult()` — 왜 여기선 괜찮나 |
| UI async void | `OmniSight/UI/MainForm.cs` 버튼 핸들러 부분 (`await Task.Run(command)`) | 이벤트 핸들러만 예외적으로 async void |

**집에서 Claude에게:**
```text
LegacyOcrSocketDevice.cs 515~623줄을 진동벨·모래시계 비유로 설명해줘.
TaskCompletionSource에 RunContinuationsAsynchronously를 준 이유, 타임아웃 뒤에도
completion.Task를 await하는 이유, ObserveFault가 없으면 생기는 일을 알려줘.
그리고 FA_TASK_AWAIT_GUIDE.ko.md 13장 "피할 패턴"을 요약해줘.
```

## 4. 왜 이렇게 썼나

### 4.1 await가 실제로 하는 일

```text
async Task RunAsync()
{
    A();                          // 스레드 T1에서 실행
    var x = await GetAsync();     // 아직 안 끝났으면: "끝나면 B부터 이어서" 예약, T1 반환
    B(x);                         // 끝난 뒤: ConfigureAwait(false)면 스레드풀 아무 스레드(T7)
}                                 //          UI에서 기본값이면 UI 스레드로 돌아옴
```

### 4.2 규칙표

| 규칙 | 이유 | OmniSight |
|---|---|---|
| 라이브러리/백그라운드 코드는 `ConfigureAwait(false)` | UI 스레드로 돌아갈 필요 없음, 데드락 방지, 약간 빠름 | 파이프라인·소켓 전부 |
| UI 이벤트 핸들러만 `async void` | 이벤트 시그니처가 void라서. 다른 곳의 async void는 예외를 잡을 방법이 없음 | MainForm 버튼 |
| UI 스레드에서 `.Wait()`, `.Result` 금지 | 이어서 실행할 코드가 UI 스레드를 기다림 ↔ UI 스레드는 그 Task를 기다림 = 데드락 | 가이드 §12 (MainForm의 동기 Stop은 약점, Day14·25) |
| 전용 스레드(SynchronizationContext 없음)에서는 `GetAwaiter().GetResult()` 허용 | 돌아갈 컨텍스트가 없어 데드락 조건이 없음. AggregateException 대신 원래 예외 | 출력 디스패처 루프 |
| 취소는 `OperationCanceledException`으로 끝내고 `when (token.IsCancellationRequested)`로 구분 | "내가 요청한 취소"와 "다른 이유의 타임아웃·취소"를 구분 | 파이프라인 |
| 타임아웃과 취소는 함께 | 취소 토큰이 없으면 영원히 기다릴 수 있고, 타임아웃만 있으면 사람이 멈출 수 없음 | `Task.Delay(timeout, token)` |
| WhenAny의 진 Task는 관찰 | 나중에 fault되면 UnobservedTaskException (Day01) | `ObserveFault` |

### 4.3 TaskCompletionSource와 `RunContinuationsAsynchronously`

`TrySetResult`를 부른 스레드(소켓 I/O 완료 스레드)에서 await 뒤 코드가 **바로 이어서** 실행될 수 있다. 그러면 I/O 스레드가 우리 로직을 처리하느라 붙잡히고, lock을 잡은 상태라면 교착 위험도 생긴다. 이 옵션은 이어지는 코드를 **항상 스레드풀로** 보낸다.

### 4.4 Task.Run은 언제?

| 작업 | Task.Run? |
|---|---|
| CPU 계산(이미지 처리, AI 추론) | ✅ UI·I/O 스레드에서 떼어낼 때 |
| I/O(파일, 소켓, DB)의 async API | ❌ 이미 비동기. 그냥 await |
| 동기 I/O만 있는 옛 API (DNS 일부, SDK) | 어쩔 수 없이 ✅ 또는 전용 스레드 |
| 프레임마다 `Task.Run`을 새로 띄워 병렬화 | ❌ 순서·소유권·정지가 깨짐 (onboarding 11 §4) |

## 5. Kata: 진동벨 만들기

```csharp
// KATA: Day12
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Katas.Day12
{
    // 옛날 방식 API: 콜백으로 결과를 알려준다 (SDK, SocketAsyncEventArgs 같은 것)
    public sealed class OldStylePlc
    {
        public event Action<bool> Connected;
        public void BeginConnect(int delayMs, bool succeed)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(delayMs);
                var h = Connected;
                if (h != null) h(succeed);
            });
        }
    }

    public static class TimeoutHelper
    {
        public static async Task<T> WithTimeout<T>(Task<T> task, int timeoutMs, CancellationToken ct)
        {
            Task delay = Task.Delay(timeoutMs, ct);
            Task winner = await Task.WhenAny(task, delay).ConfigureAwait(false);
            if (winner != task)
            {
                Observe(task);                        // 진 쪽 예외 관찰
                ct.ThrowIfCancellationRequested();    // 사람이 취소한 거면 취소로
                throw new TimeoutException(timeoutMs + "ms 초과");
            }
            return await task.ConfigureAwait(false);  // 예외가 있으면 여기서 원래 예외로
        }

        private static void Observe(Task task)
        {
            task.ContinueWith(t => { var ignored = t.Exception; },
                CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
        }
    }

    public static class KataDay12
    {
        // 콜백 → Task 변환
        public static Task<bool> ConnectAsync(OldStylePlc plc, int delayMs, bool succeed)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Action<bool> handler = null;
            handler = ok =>
            {
                plc.Connected -= handler;            // 한 번만
                tcs.TrySetResult(ok);
            };
            plc.Connected += handler;
            plc.BeginConnect(delayMs, succeed);
            return tcs.Task;
        }

        public static void Run()
        {
            RunAsync().GetAwaiter().GetResult();
        }

        private static async Task RunAsync()
        {
            var plc = new OldStylePlc();
            Console.WriteLine("빠른 연결: " + await TimeoutHelper.WithTimeout(ConnectAsync(plc, 100, true), 500, CancellationToken.None));

            try { await TimeoutHelper.WithTimeout(ConnectAsync(plc, 2000, true), 300, CancellationToken.None); }
            catch (TimeoutException ex) { Console.WriteLine("느린 연결: " + ex.Message); }

            using (var cts = new CancellationTokenSource(150))   // 사람이 150ms에 중지 버튼
            {
                try { await TimeoutHelper.WithTimeout(ConnectAsync(plc, 2000, true), 1000, cts.Token); }
                catch (OperationCanceledException) { Console.WriteLine("사용자 취소 (타임아웃과 구분됨)"); }
            }

            // 협조적 취소: 토큰을 확인하는 일만 멈춘다
            using (var cts = new CancellationTokenSource())
            {
                Task work = Task.Run(async () =>
                {
                    for (int i = 0; i < 100; i++)
                    {
                        cts.Token.ThrowIfCancellationRequested();     // 쪽지 확인
                        await Task.Delay(50, cts.Token).ConfigureAwait(false);
                    }
                });
                await Task.Delay(200).ConfigureAwait(false);
                cts.Cancel();
                try { await work.ConfigureAwait(false); }
                catch (OperationCanceledException) { Console.WriteLine("작업이 쪽지를 보고 멈춤. 상태=" + work.Status); }
            }
        }
    }
}
```

## 6. MiniSight 오늘 단계

1. `Common/TimeoutHelper.cs`: Kata의 `WithTimeout<T>` + `Task`(결과 없는) 버전.
2. `Devices/Plc/FakePlcLink.cs`(Day22 준비): `Task<bool> ConnectAsync(int timeoutMs, CancellationToken ct)` — 내부는 `OldStylePlc` 같은 가짜 콜백을 TCS로 감싼다.
3. `HealthMonitor`나 `FolderCamera`에서 `async void`가 있으면 전부 `async Task`로 바꾸고 호출 측에서 관찰.
4. 코드 전체를 검색(Ctrl+Shift+F) `".Result"`, `".Wait()"` → UI 스레드에서 부르는 곳이 있으면 표시해 두기 (Day14 실험 재료).

## 7. VS2022 디버깅 실습

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | Kata `ConnectAsync` 안 `tcs.TrySetResult` 줄과 `RunAsync`의 await 다음 줄에 중단점 | |
| 2 | 두 번째 중단점에서 **호출 스택** 확인 | `[비동기 호출]` 경계 표시, `RunAsync`를 누가 이어서 실행했는지 |
| 3 | 디버그 → 창 → **병렬 스택 → 작업 보기** | 대기 중인 async 메서드들의 논리 스택 |
| 4 | 도구 → 옵션 → 디버깅 → **내 코드만 사용(Just My Code)** 끄기 | 호출 스택에 `AsyncMethodBuilder`, `ExecutionContext` 같은 프레임워크 프레임이 보임 → 다시 켜기 |
| 5 | 예외 설정에서 `TimeoutException`, `OperationCanceledException` 체크 | 어디서 throw 되는지. 취소 예외는 평소엔 끄고 다님(정상 흐름이라 너무 자주 멈춤) |
| 6 | `Observe(task);`를 주석 처리 → 느린 연결을 실패(`succeed=false`→예외로 바꿔서)하게 하고 `GC.Collect()` | Day01 처리기의 `[Task]` 로그 출력 확인 |

## 8. 셀프 체크

1. `await`는 스레드를 멈추나?
<details><summary>답</summary>아니다. 대상 Task가 아직 안 끝났으면 나머지 코드를 계속(continuation)으로 등록하고 현재 스레드를 호출자에게 돌려준다. 완료되면 캡처한 컨텍스트(UI) 또는 스레드풀에서 이어서 실행한다.</details>

2. UI 스레드에서 `.Result`를 쓰면 왜 데드락이 나나?
<details><summary>답</summary>UI 스레드가 Task 완료를 블로킹 대기한다. 그 Task 내부의 await는 기본적으로 UI SynchronizationContext로 돌아와 이어서 실행하려 한다. UI 스레드는 대기 중이라 계속을 실행할 수 없고, Task는 계속이 실행돼야 끝난다. 서로 기다린다.</details>

3. `async void`의 문제는?
<details><summary>답</summary>호출자가 완료를 기다리거나 예외를 잡을 수 없다. 예외는 SynchronizationContext로 바로 던져져 UI면 ThreadException, 아니면 프로세스 크래시가 된다. 이벤트 핸들러에서만 쓰고 내부를 try/catch로 감싼다.</details>

4. `Task.WhenAny`로 타임아웃을 만든 뒤 꼭 해야 할 일은?
<details><summary>답</summary>진 Task를 관찰(ContinueWith로 Exception 접근 또는 나중에 await)해 미관찰 예외를 막고, 가능하면 그 작업 자체를 취소하거나 관련 자원(소켓)을 닫는다. 진 작업은 백그라운드에서 계속 돌고 있을 수 있다.</details>

5. `TaskCompletionSource`에 `RunContinuationsAsynchronously`를 주는 이유는?
<details><summary>답</summary>TrySetResult를 호출한 스레드에서 await 뒤 코드가 동기적으로 실행되는 것을 막는다. I/O 완료 스레드나 lock을 잡은 스레드가 예상치 못한 사용자 코드에 붙잡히는 재진입·교착을 방지한다.</details>

## 9. 블로그 포인트

- 제목 예: `[C# async] 장비 통신에서 꼭 지키는 async/await 규칙 7가지 (타임아웃은 WhenAny, 진 Task는 관찰)`
- Kata 출력 3가지(빠른 연결/타임아웃/사용자 취소)로 타임아웃과 취소의 차이를 보여주기.
