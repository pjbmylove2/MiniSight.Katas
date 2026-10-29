# Day 06 (토, 4h) · Facade + State Machine + Observer, 그리고 1주차 통합

## 1. 오늘 목표

- **완료 기준**
  - UI가 `VisionSystem` 하나만 보고 운전하는 구조(Facade)를 설명할 수 있다.
  - `VisionSystemState / VisionMode / VisionStatus` 3개 enum으로 시작 가능 여부를 판단하는 규칙을 말할 수 있다.
  - "구독자 하나가 터져도 다른 구독자와 상태 전환은 계속된다"를 `GetInvocationList`로 구현했다.
  - `DeviceManager`: 등록 순서 시작, 역순 정지, **시작 실패 시 이미 시작한 것만 역순 롤백**을 구현했다.
  - 콘솔 데모: `Created → Initializing → Ready → Start(Auto/Running) → Stop(Manual/Stop)` 상태 로그가 출력된다.
- **최소분량**: 3, 4, 6.1~6.3
- **확장분량**: 6.4 시작 실패 롤백 실험, 7번 Diagnostic Tools

## 2. 🧒 초1 비유

- **Facade = 리모컨의 "전원" 버튼 하나**. 버튼 하나 누르면 안에서 TV·셋톱박스·스피커가 순서대로 켜진다. 사용자(UI)는 안에 기계가 몇 개인지 몰라도 된다.
- **State Machine = 신호등**. 빨강 → 초록 → 노랑 → 빨강 순서로만 바뀐다. 빨강에서 갑자기 "출발"은 안 된다. `Ready`가 아니면 `Start`는 거부.
- **Observer = 학교 방송**. 교장 선생님(VisionSystem)이 "운전 시작!"이라고 방송하면 각 반(UI 화면들)이 듣는다. 한 반 스피커가 고장 나도(구독자 예외) 다른 반은 방송을 들어야 한다.
- **역순 정지 = 옷 벗기**. 입을 땐 속옷 → 셔츠 → 외투, 벗을 땐 외투 → 셔츠 → 속옷.

## 3. OmniSight 지도

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 상태 enum 3개 | `OmniSight/Application/VisionSystem.cs:16-35` | State(준비도), Mode(수동/자동), Status(정지/운전) |
| Facade 필드 | `OmniSight/Application/VisionSystem.cs:55-79` | DeviceManager, IInspectionController, ManualCameraController, `_sync` 공유 |
| 시작 규칙 | `OmniSight/Application/VisionSystem.cs:380-427` | lock, Ready 아니면 거부, 중복 시작 무시, **실패 시 StopInternal로 롤백** |
| 정지 순서 | `OmniSight/Application/VisionSystem.cs:430-493` | 취득 완료(drain) → 검사 정지 → 장치 정지. 실패 시 Faulted 유지 |
| 안전한 방송 | `OmniSight/Application/VisionSystem.cs:495-528` | 상태 먼저 확정 → `GetInvocationList()` 하나씩 try/catch |
| 장치 시작/롤백 | `OmniSight/Devices/DeviceManager.cs:184-204` | `started.Add(device)`를 **Start 호출 전에** (L192) |
| 장치 역순 정지 | `OmniSight/Devices/DeviceManager.cs:207-248` | 실패를 모아 `AggregateException` |
| 장치 역순 해제 | `OmniSight/Devices/DeviceManager.cs:250-266` | 하나 실패해도 다음 장치 계속 |
| UI 구독/해제 | `OmniSight/UI/MainForm.cs:585-617`, `:879-938` | 폼 종료 시 반드시 `-=` |

**집에서 Claude에게:**
```text
VisionSystem.cs 380~528줄을 신호등 비유로 설명해줘. StartInternal 실패 시 StopInternal을 부르는 이유,
ChangeState가 GetInvocationList로 하나씩 try/catch 하는 이유를 알려줘.
그리고 DeviceManager.cs 192줄에서 started.Add를 Start 전에 하는 이유도.
```

## 4. 왜 이렇게 썼나

### 4.1 상태 3개로 나눈 이유

| 축 | 값 | 질문 |
|---|---|---|
| `VisionSystemState` | Created, Initializing, Ready, Faulted, Disposed | 시스템이 **쓸 수 있는 상태**인가? |
| `VisionMode` | Manual, Auto | 사람이 조작 중인가, 자동 운전 중인가? |
| `VisionStatus` | Stop, Running | 지금 검사가 돌고 있나? |

하나의 enum(예: `ManualStopped, AutoRunning, FaultedWhileRunning…`)으로 합치면 조합 폭발이 난다. 축을 나누면 규칙이 단순해진다: "Start는 `State == Ready && Status == Stop`일 때만".

### 4.2 결정과 이유

| 결정 | 이유 |
|---|---|
| 시작 실패 → 즉시 `StopInternal` | 카메라 2대 중 1대만 켜진 상태로 남지 않게 (**보상 트랜잭션**) |
| `started.Add`를 `Start` 전에 | Start가 **중간까지 진행하다** 실패한 장치도 롤백 대상에 넣어 반쯤 열린 자원을 닫는다 |
| 정지 실패 시 `Faulted` 유지 | "멈췄다"고 거짓말하지 않기. 재시작을 막는다 |
| 상태를 먼저 바꾸고 방송 | 구독자가 `system.State`를 읽으면 새 값이 보여야 한다 |
| 구독자별 try/catch | 화면 하나 버그로 상태 전환·로그·다른 화면이 멈추지 않게 |
| 이벤트 `-=` 해제 | 폼이 닫혔는데 VisionSystem이 폼을 참조하면 **메모리 누수 + 해제된 컨트롤 접근 예외** |

### 4.3 시니어의 눈 (Day25에서 고칠 것)

- `ChangeState`는 `lock(_sync)` **안에서** 이벤트를 부른다. 구독자가 lock을 기다리는 다른 일을 하면 **교착 상태**가 날 수 있다. 지금은 UI가 `BeginInvoke`(비동기 전달)만 해서 괜찮다. "lock 안에서 외부 코드 호출 금지"는 일반 원칙.
- `Stop`은 UI 스레드에서 동기로 호출되고, 내부에서 파이프라인 종료를 기다린다 → 화면 멈춤 위험.

## 5. Kata: 신호등과 방송

```csharp
// KATA: Day06
using System;
using System.Collections.Generic;

namespace Katas.Day06
{
    public enum Light { Red, Green, Yellow }

    public sealed class TrafficLight
    {
        private static readonly Dictionary<Light, Light> _next = new Dictionary<Light, Light>
        {
            { Light.Red, Light.Green }, { Light.Green, Light.Yellow }, { Light.Yellow, Light.Red }
        };
        private readonly object _sync = new object();
        public Light Current { get; private set; }
        public event EventHandler<Light> Changed;

        public void MoveTo(Light target)
        {
            EventHandler<Light> handlers;
            lock (_sync)
            {
                if (_next[Current] != target)
                    throw new InvalidOperationException(Current + " -> " + target + " 금지");
                Current = target;            // 1. 상태 먼저 확정
                handlers = Changed;          // 2. 구독자 목록 스냅샷
            }
            if (handlers == null) return;
            foreach (EventHandler<Light> h in handlers.GetInvocationList())   // 3. lock 밖에서 하나씩
            {
                try { h(this, target); }
                catch (Exception ex) { Console.WriteLine("구독자 오류 무시: " + ex.Message); }
            }
        }
    }

    // 역순 정지 + 시작 롤백
    public sealed class Outfit
    {
        private readonly List<string> _items = new List<string> { "속옷", "셔츠", "외투" };
        public void PutOn(string failAt)
        {
            var worn = new List<string>();
            try
            {
                foreach (string item in _items)
                {
                    worn.Add(item);                         // 시도 전에 기록
                    if (item == failAt) throw new InvalidOperationException(item + " 지퍼 고장");
                    Console.WriteLine("입음: " + item);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("실패: " + ex.Message + " → 롤백");
                for (int i = worn.Count - 1; i >= 0; i--) Console.WriteLine("벗음: " + worn[i]);
                throw;
            }
        }
    }

    public static class KataDay06
    {
        public static void Run()
        {
            var light = new TrafficLight();
            light.Changed += (s, l) => Console.WriteLine("1반 들음: " + l);
            light.Changed += (s, l) => { throw new Exception("2반 스피커 고장"); };
            light.Changed += (s, l) => Console.WriteLine("3반 들음: " + l);
            light.MoveTo(Light.Green);
            try { light.MoveTo(Light.Red); } catch (InvalidOperationException ex) { Console.WriteLine(ex.Message); }

            try { new Outfit().PutOn("외투"); } catch { }
        }
    }
}
```

**OmniSight와 다른 점 찾기**: Kata는 lock **밖**에서 방송한다. OmniSight `ChangeState`는 lock 안에서 방송한다. 어느 쪽이 안전하고, 대신 무엇을 잃나? (힌트: lock 밖이면 방송 순서가 뒤바뀔 수 있다.)

## 6. MiniSight 오늘 단계 (4시간)

### 6.1 `Devices/DeviceManager.cs : IDisposable`
- `DeviceManager(IEnumerable<IDevice> devices)`, `GetImageSources()`.
- `Initialize(ct)`: 순서대로. 실패한 장치는 이슈로 기록.
- `Start(ct)`: 순서대로, `started.Add` 후 `Start`. 실패 시 `started` 역순 Stop(실패 무시) 후 rethrow.
- `Stop()`: 역순, 실패를 모아 `AggregateException`.
- `Dispose()`: 역순, 하나 실패해도 계속.

### 6.2 `App/VisionSystem.cs : IDisposable` (Facade)
- enum 3개 + `StateChangedEventArgs`.
- `Initialize(ct)`, `Start(ct)`, `Stop()`이 `(bool ok, string message)` 튜플 반환.
- 모든 공개 메서드는 `lock (_sync)`.
- `ChangeState`: 상태 먼저 → 로그 → `GetInvocationList` try/catch 방송.
- 오늘은 검사 부분이 없으니 `IInspectionController`는 `null` 허용 (Day13에 연결).

### 6.3 Bootstrapper 연결 + 콘솔 데모
- Bootstrapper가 `new VisionSystem(new DeviceManager(cameras))` 후 로컬 변수를 `null`로 (소유권 이전).
- Katas 콘솔 또는 Form1 버튼 3개(Init/Start/Stop)로 상태 로그 확인. FolderCamera의 `FrameCaptured`를 구독해 파일 이름 출력.

### 6.4 시작 실패 롤백 실험
- 카메라 2대 중 2번째를 `FailOnStart` 테스트 장치로 바꾸고 Start → 1번째가 **Stop 로그**를 찍는지, VisionSystem이 Ready/Manual/Stop로 돌아오는지 확인.

## 7. VS2022 디버깅 실습

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | `ChangeState`에 Tracepoint: `{previous} -> {state}/{mode}/{status}` | **출력(Output) 창**에 상태 이력이 쭉 찍힘. 디버그 소스 필터에서 "프로그램 출력"만 보기 |
| 2 | 구독자 하나에서 예외를 던지게 하고 실행 | 예외 설정이 켜져 있으면 거기서 멈춤 → F5로 계속 → 다른 구독자 출력 확인 |
| 3 | 디버그 → 창 → **진단 도구(Diagnostic Tools, Ctrl+Alt+F2)** | 이벤트 탭: 예외·중단점 타임라인. 메모리 그래프 |
| 4 | Start/Stop을 20번 반복하는 버튼을 만들고 메모리 그래프 관찰 | 계속 올라가면 누수 의심 (Day13에서 본격적으로) |
| 5 | 6.4 실험에서 롤백 중 **호출 스택** 확인 | VisionSystem.Start → DeviceManager.Start → catch → StopReverse |

## 8. 셀프 체크

1. Facade와 Adapter의 차이는?
<details><summary>답</summary>Adapter는 이미 있는 인터페이스 하나를 다른 모양으로 바꾼다(1:1 변환). Facade는 여러 하위 시스템을 묶어 단순한 새 진입점을 제공한다(N:1 단순화).</details>

2. 이벤트를 `handler?.Invoke(...)`로 한 번에 부를 때의 문제는?
<details><summary>답</summary>멀티캐스트 델리게이트는 구독자를 순서대로 호출하다가 하나가 예외를 던지면 나머지 구독자는 호출되지 않고 예외가 발행자에게 전파된다. 상태 전환 코드가 중간에 끊긴다.</details>

3. lock 안에서 이벤트를 발생시키면 생길 수 있는 문제는?
<details><summary>답</summary>구독자가 같은 lock이 필요한 메서드를 다른 스레드로 부르고 그 결과를 동기로 기다리면 교착 상태가 된다. 또 구독자가 오래 걸리면 lock 보유 시간이 길어져 다른 호출이 모두 대기한다.</details>

4. 장치 시작 롤백에서 "시작에 실패한 그 장치"도 Stop해야 하는 이유는?
<details><summary>답</summary>Start 중간에 일부 자원(스트림 그래버, 트리거 루프)만 열렸을 수 있다. 상태가 Faulted여도 열린 자원은 닫아야 다음 시작에서 충돌하지 않는다.</details>

5. 이벤트 구독 해제를 안 하면 어떤 일이 생기나?
<details><summary>답</summary>발행자가 구독자를 참조하므로 폼이 닫혀도 GC가 회수하지 못한다(메모리 누수). 또 이벤트가 오면 이미 Dispose된 컨트롤에 접근해 ObjectDisposedException이 난다.</details>

## 9. 블로그 포인트

- 제목 예: `[설계] 장비 운전 상태를 enum 3개로 나눈 이유와 안전한 이벤트 방송`
- 신호등 Kata 결과(2반 고장에도 3반이 들음) 출력 캡처, 롤백 실험 로그.
