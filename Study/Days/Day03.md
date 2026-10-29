# Day 03 (수, 2h) · Template Method와 장치 상태

## 1. 오늘 목표

- **완료 기준**
  - Template Method를 "순서는 부모가, 내용은 자식이"라고 설명하고 OmniSight에서 3곳 이상 찾을 수 있다.
  - `DeviceBase`의 `Initialize/Start/Stop/Dispose`가 상태를 어떻게 바꾸는지 **상태 그림**으로 그렸다.
  - MiniSight `DeviceBase`를 작성하고, 일부러 `OnStart`에서 예외를 던져 `Faulted`가 되는 것을 디버거로 봤다.
- **최소분량**: 2, 3, 4, 6
- **확장분량**: Kata 확장 과제, 조건부 중단점/Tracepoint

## 2. 🧒 초1 비유

**라면 끓이기 순서표**를 엄마(부모 클래스)가 정했다.

1. 물 끓이기 → 2. **면 넣기** → 3. **스프 넣기** → 4. 불 끄기

신라면이든 짜파게티든(자식 클래스) 1번과 4번은 똑같다. 2, 3번만 자기 방식으로 한다. 순서를 자식이 바꾸면 안 된다(불 끄고 면 넣으면 안 되니까). 그래서 순서표 자체는 **고칠 수 없게(non-virtual)** 만들고, 2·3번 칸만 비워 둔다(`abstract`/`virtual`).

OmniSight의 `DeviceBase`가 바로 이 순서표다. "상태 확인 → 자식 작업(OnStart) → 상태 바꾸기, 실패하면 Faulted" 순서를 모든 장치가 똑같이 따른다.

## 3. OmniSight 지도

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 순서표 Initialize | `OmniSight/Devices/DeviceBase.cs:19-42` | lock 안에서 상태 확인, `OnInitialize`, 실패 시 Faulted 후 `throw` |
| 순서표 Start | `OmniSight/Devices/DeviceBase.cs:44-63` | Ready/Stopped에서만 시작 가능 |
| 순서표 Stop | `OmniSight/Devices/DeviceBase.cs:65-83` | 정지 실패를 Stopped로 표시하지 않음 (주석 L79) |
| 빈 칸(hook) | `OmniSight/Devices/DeviceBase.cs:85-88` | `abstract OnInitialize`, `virtual OnStart/OnStop/OnDispose` |
| Dispose 순서 | `OmniSight/Devices/DeviceBase.cs:90-115` | Stop 실패해도 OnDispose는 무조건 (중첩 finally) |
| 2단 템플릿 | `OmniSight/Devices/Cameras/CallbackCameraAdapterBase.cs` (OnInitialize 등 오버라이드 부분) | 카메라 공통 로직을 한 번 더 템플릿화 |
| AI 엔진 템플릿 | `OmniSight/Vision/AiEngine/AiEngineBase.cs` | abstract Initialize/Inspect/ReleaseNativeHandle |
| UI 템플릿 | `OmniSight/UI/Controls/BaseUserControl.cs` | OnHandleCreated에서 공통 폰트·번역 적용 |

**집에서 Claude에게:**
```text
OmniSight/Devices/DeviceBase.cs 전체를 보여주고 라면 순서표 비유로 설명해줘.
CallbackCameraAdapterBase가 DeviceBase를 어떻게 한 번 더 템플릿으로 쓰는지
OnInitialize/OnStart/OnStop/OnDispose 오버라이드 부분만 골라서 보여줘.
```

## 4. 왜 이렇게 썼나

### 4.1 상태 그림 (직접 그려 보기)

```text
           Initialize 성공              Start 성공
Created ───────────────▶ Ready ─────────────────▶ Running
   │                       ▲  ▲                      │
   │ Initialize 실패       │  └──── Start ────┐      │ Stop 성공
   ▼                       │                  │      ▼
Faulted ◀── 어떤 단계든 실패 ┘              Stopped ◀─┘
   (Stop 실패도 Faulted. "멈췄다고 거짓말하지 않기")
모든 상태 ── Dispose ──▶ Disposed
```

### 4.2 결정과 이유

| 결정 | 이유 |
|---|---|
| `Initialize/Start/Stop/Dispose`를 **virtual이 아니게** | 자식이 상태 관리 순서를 망가뜨리지 못하게. 상태 규칙이 한 곳에만 있음 |
| 실패 시 상태를 `Faulted`로 바꾸고 **다시 throw** | 상태는 정확히 기록하고, 판단(롤백)은 호출자(DeviceManager)가 하게 |
| Stop 실패를 Stopped로 표시 안 함 | 카메라가 실제로 안 멈췄는데 "정지"로 보이면 작업자가 재시작 → 장비 충돌. **안전 우선** |
| Dispose는 Stop 실패와 무관하게 OnDispose 실행 | 네이티브 핸들·큐는 무조건 해제해야 메모리/핸들 누수가 없음 |
| `OnInitialize`만 abstract | 모든 장치는 연결 단계가 반드시 있지만, Start/Stop은 없어도 되는 장치가 있다 |

### 4.3 시니어의 눈으로 본 약점

- `State`는 일반 자동 속성이다. `Initialize`의 상태 확인은 lock 안이지만 `State = Ready`(L35)와 `Start/Stop`의 대입은 lock 밖이다. 다른 스레드가 `State`를 읽으면 오래된 값을 볼 수 있다. 현재는 Start/Stop을 한 스레드(VisionSystem의 lock)에서만 부르는 **사용 규칙**으로 지킨다. 규칙이 문서에 없으면 언젠가 깨진다.
- Template Method는 **상속**이라 부모를 바꾸면 모든 자식이 영향을 받는다. 그래서 부모는 작고 안정적이어야 한다. 변하는 부분이 많으면 Strategy(합성)가 더 낫다 (Day16에서 비교).

## 5. Kata: 라면 공장

```csharp
// KATA: Day03
using System;

namespace Katas.Day03
{
    public enum CookState { Idle, Cooking, Done, Burnt }

    public abstract class RamenRecipe
    {
        public CookState State { get; private set; }

        // 템플릿 메서드: virtual 아님. 순서는 여기서만 정한다.
        public void Cook()
        {
            if (State == CookState.Cooking) throw new InvalidOperationException("이미 조리 중");
            State = CookState.Cooking;
            try
            {
                BoilWater();
                AddNoodles();          // 자식이 채우는 칸
                AddSoup();             // 자식이 채우는 칸
                if (WantsEgg()) AddEgg(); // hook: 기본값이 있는 선택 칸
                State = CookState.Done;
            }
            catch
            {
                State = CookState.Burnt;   // 실패 기록 후
                throw;                     // 판단은 호출자에게
            }
            finally
            {
                TurnOffFire();             // 실패해도 불은 반드시 끈다 (OnDispose 같은 역할)
            }
        }

        private void BoilWater() { Console.WriteLine("물 550ml 끓이기"); }
        private void TurnOffFire() { Console.WriteLine("불 끄기"); }
        protected abstract void AddNoodles();
        protected abstract void AddSoup();
        protected virtual bool WantsEgg() { return false; }
        private void AddEgg() { Console.WriteLine("계란 투하"); }
    }

    public sealed class Shin : RamenRecipe
    {
        protected override void AddNoodles() { Console.WriteLine("신라면 면 4분 30초"); }
        protected override void AddSoup() { Console.WriteLine("빨간 스프"); }
        protected override bool WantsEgg() { return true; }
    }

    public sealed class BrokenRamen : RamenRecipe
    {
        protected override void AddNoodles() { Console.WriteLine("면 넣기"); }
        protected override void AddSoup() { throw new InvalidOperationException("스프가 없다!"); }
    }

    public static class KataDay03
    {
        public static void Run()
        {
            var shin = new Shin();
            shin.Cook();
            Console.WriteLine("상태: " + shin.State);

            var broken = new BrokenRamen();
            try { broken.Cook(); }
            catch (Exception ex) { Console.WriteLine("실패: " + ex.Message); }
            Console.WriteLine("상태: " + broken.State);   // Burnt
        }
    }
}
```

**확장 과제**: `Cook()`을 두 스레드에서 동시에 부르면 `State == Cooking` 확인을 둘 다 통과할 수 있다. 어디에 lock을 걸어야 할까? (`DeviceBase.Initialize`가 lock을 어디까지 잡는지와 비교)

## 6. MiniSight 오늘 단계

`Devices/DeviceBase.cs` 작성. 요구사항(코드는 직접):

- `public abstract class DeviceBase : IDevice`
- `Initialize(ct)`: Disposed면 `ObjectDisposedException`. 이미 Ready/Running이면 return. Initializing → `OnInitialize` → Ready, 실패 시 Faulted + rethrow.
- `Start(ct)`: Ready/Stopped가 아니면 `InvalidOperationException`. 성공 Running, 실패 Faulted.
- `Stop()`: Created/Disposed면 무시. 성공 Stopped, 실패 Faulted + rethrow.
- `Dispose()`: Stop 시도 → 예외와 무관하게 `OnDispose` → Disposed.
- 상태가 바뀔 때마다 `Console.WriteLine(Name + ": " + old + " -> " + new)` (Day06에서 이벤트로 교체).

테스트용 `TestDevice : DeviceBase`를 만들어 `FailOnStart` 속성이 true면 `OnStart`에서 예외를 던지게 한다.

## 7. VS2022 디버깅 실습

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | `DeviceBase.Start`의 `State = DeviceState.Running;` 줄에 중단점 → 우클릭 → **조건** → `Name == "Cam2"` | Cam2일 때만 멈춤 |
| 2 | 중단점 우클릭 → **작업(Actions)** → 메시지 `{Name}: {State} 시작` 입력, "실행 계속" 체크 | **Tracepoint**: 멈추지 않고 출력 창에 로그. 현장 재현이 어려운 타이밍 문제에서 유용 |
| 3 | `FailOnStart = true`로 실행 | `catch` 블록에서 `State`가 Faulted로 바뀌는 순간 확인 |
| 4 | 디버그 → 창 → **예외 설정(Ctrl+Alt+E)** → CLR Exceptions → `InvalidOperationException` 체크 | throw 되는 **바로 그 줄**에서 멈춤 |
| 5 | 중단점 창(Ctrl+Alt+B)에서 중단점 일괄 끄기/켜기 | |

## 8. 셀프 체크

1. Template Method와 Strategy의 차이를 한 문장씩.
<details><summary>답</summary>Template Method: 상속으로 알고리즘 순서를 부모가 고정하고 일부 단계를 자식이 채운다. Strategy: 합성으로 알고리즘 전체(또는 한 단계)를 객체로 갈아끼운다. 전자는 컴파일 시점 고정, 후자는 실행 중 교체 가능.</details>

2. 템플릿 메서드를 virtual로 만들면 무엇이 위험한가?
<details><summary>답</summary>자식이 오버라이드해서 상태 전이·예외 처리 순서를 생략할 수 있다. 상태 규칙이 여러 클래스에 흩어져 장비 안전 규칙이 깨진다.</details>

3. Stop이 실패했을 때 Stopped가 아니라 Faulted로 두는 이유는?
<details><summary>답</summary>실제로 장치가 멈췄는지 확인하지 못했기 때문이다. Stopped로 보이면 운영자나 코드가 재시작을 시도해 이중 취득, 핸들 충돌, 기계 동작 오류가 날 수 있다. 모르면 "고장"이라고 표시하는 게 안전하다.</details>

4. Dispose 안에서 중첩 try/finally를 쓴 이유는?
<details><summary>답</summary>Stop()이 예외를 던져도 OnDispose()(네이티브 자원 해제)가 반드시 실행되고, OnDispose가 실패해도 _disposed/State 표시는 반드시 되도록 하기 위해서다.</details>

5. `catch { State = Faulted; throw; }`에서 `throw ex;`가 아니라 `throw;`인 이유는?
<details><summary>답</summary>`throw;`는 원래 스택 트레이스를 보존한다. `throw ex;`는 스택 트레이스를 현재 줄로 덮어써 원인 위치를 잃는다.</details>

## 9. 블로그 포인트

- 제목 예: `[디자인패턴] 장비 클래스의 수명 관리를 Template Method로 고정한 이유`
- 상태 그림(직접 그린 것) + 라면 Kata + "Stop 실패는 Faulted" 안전 원칙.
