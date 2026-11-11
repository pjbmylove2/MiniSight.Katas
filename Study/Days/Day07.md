# Day 07 (일, 4h) · 1주차 복습, 백지 구조도, 블로그

## 1. 오늘 목표

- **완료 기준**
  - 밀린 Day의 최소분량을 끝냈다.
  - **백지 시험**을 통과했다: 아무것도 안 보고 1주차 구조도를 그리고 화살표마다 패턴 이름을 적는다.
  - 셀프 체크 Day01~06 중 틀린 문항을 Claude와 말로 다시 풀었다.
  - 블로그 2편 초안을 썼다.
- **최소분량**: 2(밀린 것), 3(백지 시험)
- **확장분량**: 5 리팩토링 과제, 블로그 2편째

## 1-1. 이번 회차 진행 기록 (2026-11-09 ~ 11-10)

이번 회차는 조각 1(밀린 것 ①) → 조각 2(밀린 것 ②) → 조각 3(백지 시험) → 조각 4(구술 면접 질문 1까지)로 진행했다. 구술 면접 꼬리 질문 1-1에서 "더 고민하고 공부해야겠다"로 멈추고 기록했다. 용어 카드·셀프 체크·블로그·리팩토링 과제는 하지 않았다.

### 조각 1 · 밀린 것 ①: `VisionSystem.cs` 손보기

**지금 하는 것:** Day06에서 남긴 MiniSight `VisionSystem`의 세 가지를 고친다.

1. `ChangeState`의 방송 코드가 전부 주석이었다. 주석 속 `if (handlers != null) { return; }`는 비교가 반대라 그대로 풀면 방송이 절대 안 나간다. 힌트(Day06 조각 6 B의 `if` 줄과 비교 연산자 비교)로만 전달했다. 주석 안 코드는 컴파일러가 검사하지 않아 이런 버그가 숨는다.
2. 자동 완성으로 들어온 `using static OpenCvSharp.Stitcher;`, `using static System.Windows.Forms.AxHost;` 삭제. 창구는 화면·영상 라이브러리를 몰라야 한다.
3. `VisionSystem : IDisposable` 추가.

```csharp
public void Dispose()
{
    lock (_sync)
    {
        if (State == VisionSystemState.Disposed)
        {
            return;                                   // 두 번째 호출은 그냥 나감
        }

        _devices.Dispose();                           // 장치 정지 + 해제 (반장은 안 던짐)
        ChangeState(VisionSystemState.Disposed, VisionMode.Manual, VisionStatus.Stop);
    }
}
```

`State == Disposed` 검사가 Day06 조각 4의 `_disposed` 깃발 역할을 한다.

사용자 `button12` 실행 결과 (예상 출력과 일치, `로그 수신:` 두 번):

```text
[시스템] Created -> Initializing/Manual/Stop
예외 발생: 'System.InvalidOperationException'(MiniSight.exe)
[시스템] 구독자 오류(무시하고 계속): 화면 갱신 실패
    로그 수신: Initializing
카메라1: Created -> Initializing
 [카메라1] 연결
카메라1: Initializing -> Ready
[시스템] Initializing -> Ready/Manual/Stop
예외 발생: 'System.InvalidOperationException'(MiniSight.exe)
[시스템] 구독자 오류(무시하고 계속): 화면 갱신 실패
    로그 수신: Ready
결과: True / 준비 완료
```

> **Q. 예상 출력에 없던 `예외 발생: 'System.InvalidOperationException'(MiniSight.exe)` 줄은 무엇인가?**
> 코드가 아니라 VS 디버거가 F5 실행 때 찍는 first-chance 예외 알림이다. 예외가 **던져진 순간** 잡혔는지와 상관없이 찍힌다. 그래서 고장 난 구독자의 throw(디버거 알림)와 우리 catch(`구독자 오류`)가 항상 짝으로 나온다. Ctrl+F5에서는 안 나온다. 짝 없이 반복되면 어딘가에서 예외를 던지고 조용히 삼키는 곳이 있다는 신호이고, 예외 설정(Ctrl+Alt+E)에서 해당 예외를 체크하면 던지는 순간에 멈춰 위치를 볼 수 있다.

**질문:** 새 `Dispose`는 `_devices.Dispose()`를 `try/catch`로 감싸지 않았다. 괜찮은 이유는?

<details><summary>답과 사용자 확인</summary>

`DeviceManager.Dispose` 안에 장치마다 `try/catch`가 있고 기록만 하고 던지지 않는다. 그래서 창구의 `Dispose`도 중간에 끊기지 않고 `ChangeState(Disposed)`까지 가며, 다른 오류가 진행 중이어도 진짜 원인을 덮어쓰지 않는다. 안 던지는 약속은 아래(장치 → 반장 → 창구)에서부터 쌓인다. 반대로 던지는 `Stop()`을 부를 때는 창구가 감싼다.

사용자 ✅: "Dispose 안에 중복 검사도 있고 그 안에 try/catch가 따로 있다." "그래서" 부분은 보충.
</details>

### 조각 2 · 밀린 것 ②: `Bootstrapper`로 시스템 조립 + 준비/시작/정지 버튼

**지금 하는 것:** 버튼마다 하던 `new DeviceManager(...)`, `new VisionSystem(...)`을 조립 책상 한 곳으로 모으고, Form1은 완성된 창구 하나만 받는다.

**비유:** 조립 책상에서 자동차 한 대를 통째로 조립해 손님에게 열쇠(창구) 하나만 준다.

`App/Bootstrapper.cs`:

```csharp
public static VisionSystem BuildSystem(string configPath)
{
    IImageSource camera = BuildCamera(configPath);              // ① 설정 읽고 카메라 만들기 (Day05)
    IDevice light = new TestDevice("조명");                      // ② 조명 (지금은 연습 장치)
    DeviceManager devices = new DeviceManager(new IDevice[] { light, camera });   // ③ 조명 먼저 켜지게
    return new VisionSystem(devices);                           // ④ 이제 주인은 VisionSystem
}
```

`Form1.cs` (버튼 `button16` 준비 / `button17` 시작 / `button18` 정지):

```csharp
private VisionSystem _system;          // 창구 열쇠 (준비를 누를 때 만들어짐)

private void button16_Click(object sender, EventArgs e)       // 준비
{
    if (_system == null)
    {
        string path = Path.Combine(Application.StartupPath, "camera-study.json");
        try
        {
            _system = Bootstrapper.BuildSystem(path);
        }
        catch (Exception ex)
        {
            MessageBox.Show("조립 실패: " + ex.Message);
            return;
        }
        _system.StateChanged += OnSystemStateChanged;
    }

    (bool ok, string message) result = _system.Initialize(CancellationToken.None);
    if (!result.ok)
    {
        MessageBox.Show(result.message);
    }
}

private void button17_Click(object sender, EventArgs e)       // 시작
{
    if (_system == null)
    {
        MessageBox.Show("먼저 준비를 누르세요.");
        return;
    }

    (bool ok, string message) result = _system.Start(CancellationToken.None);
    if (!result.ok)
    {
        MessageBox.Show(result.message);
    }
}

private void button18_Click(object sender, EventArgs e)       // 정지
{
    if (_system == null)
    {
        MessageBox.Show("먼저 준비를 누르세요.");
        return;
    }

    (bool ok, string message) result = _system.Stop();
    if (!result.ok)
    {
        MessageBox.Show(result.message);
    }
}

private void OnSystemStateChanged(object sender, StateChangedEventArgs e)
{
    Text = "MiniSight - " + e.State + "/" + e.Mode + "/" + e.Status;
}

protected override void OnFormClosed(FormClosedEventArgs e)
{
    if (_system != null)
    {
        _system.StateChanged -= OnSystemStateChanged;         // 구독 먼저 끊고
    }
    _system?.Dispose();                                       // 창구 정리 → 반장 → 장치 전부
    base.OnFormClosed(e);
}
```

| 코드 | 역할 | 같은 뜻의 긴 코드 |
|---|---|---|
| `protected override void OnFormClosed(...)` | 폼이 닫힐 때 WinForms가 부르는 메서드를 덮어씀 | 디자이너 `FormClosed` 이벤트 메서드 |
| `base.OnFormClosed(e);` | 원래 하던 일(FormClosed 구독자 알림)도 하게 함 | — |
| `_system?.Dispose();` | 준비를 한 번도 안 눌렀으면(null) 넘어감 | `if (_system != null) { _system.Dispose(); }` |

눌러 볼 순서와 기대 결과: 시작 먼저 → "먼저 준비를 누르세요." / 준비 → 제목 `MiniSight - Ready/Manual/Stop` / 정지 → "운전 중이 아닙니다." / 시작 → `Ready/Auto/Running` / 시작 한 번 더 → "이미 운전 중입니다." / 정지 → `Ready/Manual/Stop` / 창 닫기 → 정리 로그. 운전 중 받아 가는 곳이 없어 `큐 가득 참 -> #N 버림`이 반복될 수 있다(검사 연결은 Day13). WinForms 실행 출력은 강사가 확인하지 못했고, 사용자 실행 결과도 **미보고**.

`OnSystemStateChanged`에서 `Text`를 바로 바꿀 수 있는 건 지금은 버튼(UI 스레드)이 부른 방송이라서다. PLC 스레드가 `Start`를 부르면 `BeginInvoke`가 필요하다(2주차).

**질문:** Form1은 카메라도 DeviceManager도 변수로 갖고 있지 않다. 창을 닫으면 카메라는 누가, 어떤 순서로 정리하나?

<details><summary>답과 사용자 확인</summary>

창 닫기 → `Form1.OnFormClosed`(`_system?.Dispose()`) → `VisionSystem.Dispose`(`_devices.Dispose()`) → `DeviceManager.Dispose`(역순) → 연습 카메라 → 조명. Bootstrapper는 `static`이고 조립해서 돌려주고 끝이라 아무것도 갖고 있지 않다. 정리 체인은 만든 곳이 아니라 **필드로 갖고 있는 곳**을 따라간다. 그래서 Form1은 `_system?.Dispose()` 한 줄만 부르면 된다.

사용자 △: "Bootstrapper >> VisionSystem >> DeviceManager >> light >> camera, Bootstrapper가 다 가지고 있다. 그래서 Form1은 생성 및 해제 함수만 호출하면 된다." → Bootstrapper는 안 갖고 있음(주인은 Form1), 장치는 역순(카메라 → 조명)으로 정정. 재확인 `{ light, camera, camera2 }`의 정리 순서 ✅ "camera2 → camera → light".
</details>

### 조각 3 · 백지 시험 (20분)

아무것도 안 보고 ① Main부터 장치가 켜질 때까지 객체 순서 ② 패턴 이름 7개 ③ `DeviceState` 상태 그림 ④ 롤백·Stop·Dispose 순서 ⑤ ImageFrame 소유권을 그린다.

사용자 제출(요약): ① Main → Mutex → 그물 3개 → Form1 → (준비) Bootstrapper.BuildSystem(ConfigStore) → "DeviceManager에 CameraFactory가 있고 VisionSystem은 따로" ② Bootstrapper = Singleton, CameraFactory = Factory ③ Created → Initializing → Ready → Running → Stopped → Disposed ④ 켜기 조명 → 카메라1 → 카메라2, 해제 카메라2 → 카메라1(에러) → 조명 ⑤ 빈칸.

**정답 정리**

① 객체가 넘겨지는 순서 (안쪽에서 바깥으로, 마트료시카):

```text
Main
 ├ Mutex 확인 (두 번 실행 막기)
 ├ 그물 3개 등록 (UI / 백그라운드 / Task 예외)
 └ Application.Run(new Form1())
      └ [준비 버튼] Bootstrapper.BuildSystem(path)
           ├ ① BuildCamera
           │    ├ ConfigStore.Instance.Load(path)   ← 설정 창고에서 JSON 읽기
           │    └ CameraFactory.Create(Kind=Fake)   ← 종류 보고 카메라 만들기
           │         └ new DriverCameraAdapter(... FakeCameraDriver ...)
           ├ ② new TestDevice("조명")
           ├ ③ new DeviceManager({ 조명, 카메라 })
           └ ④ new VisionSystem(devices)
      └ Form1._system = 그 창구
```

② 패턴 (한 동사로 외우기):

| 클래스 | 패턴 | 한 동사 |
|---|---|---|
| Bootstrapper | Composition Root | 조립한다 |
| ConfigStore | Singleton | 하나만 있다 |
| CameraFactory | Factory | 만든다 |
| DriverCameraAdapter | Adapter | 모양을 맞춘다 |
| DeviceBase | Template Method | 순서를 고정한다 |
| VisionSystem | Facade | 창구가 된다 |
| StateChanged / FrameCaptured | Observer | 방송한다 |

③ `DeviceState` (MiniSight `DeviceBase.cs` 기준):

```text
Created ──Initialize──▶ Initializing ──성공──▶ Ready ──Start──▶ Running ──Stop──▶ Stopped
                             │                   ▲                                 │
                             │                   └──────────── Start ◀──────────────┘  (다시 켜기)
                             └──실패──▶ Faulted ◀── Start 실패 / Stop 실패
                                          │
                                          └──Initialize──▶ Initializing   (복구는 준비부터)

어느 상태에서든 ──Dispose──▶ Disposed  (끝, 다시 못 씀)
```

④ 순서는 셋 다 역순(카메라2 → 카메라1 → 조명), 실패 처리만 다르다.

| 상황 | 실패하면 |
|---|---|
| Start 실패 롤백 (실패한 장치 포함) | 처음 원인 + 끄다 난 오류를 모아 `AggregateException` |
| Stop | 하나 실패해도 계속 끄고, 끝나면 모아서 던짐 |
| Dispose | 하나 실패해도 계속, 기록만 하고 안 던짐, 한 번만 |

⑤ ImageFrame 소유권 (`DriverCameraAdapter.OnFrame`):

```text
드라이버가 Mat을 줌
 → new ImageFrame(...)                    ← 어댑터가 만듦 (주인 = 어댑터)
 → FrameCaptured?.Invoke(this, frame)     ← ① 이벤트로 "보여 주기만"
 → _queue.TryAdd(frame) 성공 → frame = null   ← ② 큐로 넘김 (주인 바뀜)
   실패(큐 가득) → finally에서 frame.Dispose()
 → WaitFrameAsync()로 꺼낸 쪽              ← ③ 새 주인, 다 쓰면 using으로 Dispose
```

| 받는 방법 | 비유 | 책임 |
|---|---|---|
| 이벤트(`FrameCaptured`) | 박물관 그림 구경 | 핸들러 안에서만 유효. 나중에 쓰려면 `Clone`하고 그건 내가 Dispose |
| `WaitFrameAsync` | 선물로 받음 | 내가 주인. 다 쓰면 `using`으로 Dispose |

<details><summary>채점과 사용자 확인</summary>

1차: ① △(앞부분 순서 정확, CameraFactory가 DeviceManager 안에 있다·VisionSystem은 따로 → 안쪽에서 바깥쪽으로 넘겨짐으로 정정), ② 1/7(Bootstrapper를 Singleton으로, Bootstrapper·ConfigStore 혼동), ③ △(큰 줄기 맞음, Faulted와 `Stopped → Running`·실패 → Faulted·`Faulted → Initializing` 빠짐), ④ △(역순은 맞음, 롤백·Stop·Dispose가 한 줄에 섞임), ⑤ 빈칸. 합격 기준(패턴 6/7, 상태 오류 0, "핸들러 안에서만 유효") 미달.

재시험: ② 클래스 7개 패턴 **7/7 ✅**. ⑤ "이벤트로 받은 프레임을 핸들러 뒤에도 쓰려면?"에 "복사해서 쓰거나, 그대로 들고 있으면 메모리가 해제가 안 된다" △ → Clone은 정답, 그대로 들고 있으면 누수가 아니라 **주인이 먼저 해제해서** 빈 이미지·`NullReferenceException`·깨진 화면이 난다로 정정(누수는 Clone한 쪽을 Dispose하지 않을 때). ①·③은 설명으로 이해, 재시험 안 함.
</details>

### 조각 4 · 구술 면접 (질문 1에서 멈춤)

답하는 요령: 결론 한 마디 → 이유(그래서 ○○) → 내 코드 예시 하나.

**질문 1:** "MiniSight에서 화면 버튼을 누르면 카메라가 켜지기까지 어떤 객체들을 거치는지, 왜 그렇게 나눴는지 1분 정도로 설명해 주세요."

<details><summary>모범 답과 사용자 확인</summary>

모범 답: "버튼을 누르면 Form1이 창구(VisionSystem) 하나에 '시작'을 부탁하고, 창구는 반장(DeviceManager)에게, 반장은 조명과 카메라를 순서대로 켭니다. 이 묶음은 시작할 때 Bootstrapper가 한 곳에서 조립해 Form1에 건네줍니다. 그래서 Form1은 장치가 몇 개인지, 어느 회사 카메라인지 몰라도 되고, 장치를 바꿔도 화면 코드는 그대로입니다."

사용자 △: 실행 순서(Mutex → 안전망 3개 → 메인 폼)로 시작해 따라가기 쉬웠고 패턴 이름을 섞은 점은 좋음. 정정 3군데: ① "Bootstrapper 안에 DeviceManager가 있다" → 조립해서 건네주고 끝, 창구를 가진 건 Form1 ② "DeviceManager가 Facade" → Facade는 VisionSystem ③ "어댑터 패턴으로 하나의 베이스 객체를 구현받아" → 상속해서 순서 고정은 Template Method(`DeviceBase`), 벤더 SDK를 감싸는 건 Adapter(`DriverCameraAdapter`). "왜 나눴나"가 빠짐. MiniSight에 아직 없는 PLC를 있는 것처럼 말함 → 만든 것과 계획을 구분.
</details>

**꼬리 질문 1-1 (보류):** "Form1이 카메라와 조명을 직접 `new` 해서 직접 `Start`를 부르면 현장에서 어떤 문제가 생기나?" 사용자가 "더 고민하고 공부해야겠다"로 보류. 고민 재료: 직접 만드는 `button14`와 창구를 쓰는 `button17` 비교, "카메라를 2대로 늘리면 몇 군데를 고치나", 순서·실패(롤백)·변경(Factory/조립 책상) 중 하나를 골라 구체적으로. 다시 읽을 곳: Day05 조각 1~2, Day06 2절 그림과 조각 5.

## 2. 시간표

| 시간 | 할 일 |
|---|---|
| 0:00~1:30 | PROGRESS.md의 "일요일 버퍼"에 쌓인 확장분량 처리 |
| 1:30~2:10 | 백지 시험 (3번) |
| 2:10~2:50 | Claude 구술 면접 (4번) |
| 2:50~4:00 | 블로그 (Templates/BlogPost.md) |

## 3. 백지 시험

종이(또는 그림판)에 **아무것도 보지 않고** 그린다. 20분 제한.

1. `Program.Main`부터 `Application.Run`까지 객체가 만들어지는 순서
2. 각 화살표 옆에 패턴 이름: Composition Root / Factory / Adapter / Template Method / Facade / Observer / Singleton
3. `DeviceState` 상태 그림 (전이 조건 포함)
4. Start 실패 시 롤백 순서, Stop 순서, Dispose 순서
5. ImageFrame의 소유권: 누가 만들고, 이벤트로 받은 사람과 `WaitFrameAsync`로 받은 사람의 책임 차이

다 그렸으면 Claude에게:
```text
1주차 백지 시험 그림을 말로 설명할게. OmniSight 실제 코드와 비교해서 틀린 부분이나
빠진 부분을 짚어줘. 정답을 바로 말하지 말고 "어디가 이상한지"만 먼저 힌트로 줘.
```

**합격 기준**: 패턴 7개 중 6개 이상 정확한 위치, 상태 그림 전이 오류 0개, 소유권 설명에서 "이벤트 핸들러 안에서만 유효"를 말했다.

## 4. 구술 면접 연습

Claude에게:
```text
면접관 역할 해줘. 주제는 "디자인 패턴을 실제 장비 프로그램에 어떻게 썼나".
내 MiniSight 기준으로 질문 5개를 하나씩 하고, 내 답에 꼬리 질문 2개씩 해줘.
답이 부족하면 마지막에 OmniSight의 어느 코드를 다시 보면 되는지 알려줘.
```

자주 나오는 꼬리 질문 예시:
- "Factory를 switch로 만들었는데 벤더가 30개면?"
- "싱글톤을 썼다는데 테스트는 어떻게 했나?"
- "상태 enum을 쓰면 State 패턴(클래스)과 뭐가 다른가? 언제 클래스로 바꾸나?"
- "이벤트 구독자가 UI라면 어떤 스레드에서 불리나?" (→ 2주차 복선)

## 5. 리팩토링 과제 (확장)

OmniSight에서 본 약점 하나를 MiniSight에서 **다르게** 해 본다. 하나만 고른다.

| 과제 | 내용 |
|---|---|
| A. 팩토리 등록 방식 | `CameraFactory`의 `switch`를 `Dictionary<CameraKind, Func<CameraSettings, IImageSource>>` 등록 방식으로 바꾸고 장단점 정리 |
| B. 상태 읽기 안전 | `DeviceBase.State`를 `volatile` 백킹 필드로 바꾸거나 lock으로 읽게 하고, 왜 필요한지 주석으로 |
| C. 방송을 lock 밖으로 | `VisionSystem.ChangeState`에서 lock 안에서는 상태와 구독자 스냅샷만, 방송은 lock 밖. 순서 역전 문제를 어떻게 막을지 고민 |

## 6. 1주차 용어 카드 (말로 30초씩 설명)

| 용어 | 30초 설명 키워드 |
|---|---|
| DIP | 고수준(파이프라인)이 저수준(카메라)에 의존하지 않고 둘 다 추상(인터페이스)에 의존 |
| ISP | 쓰지 않는 기능에 의존하지 않게 인터페이스를 쪼갬 (Preview 분리) |
| Template Method | 순서 고정(non-virtual) + 빈칸(abstract/virtual hook) |
| Simple Factory | 조건 → 구체 객체, new를 한 곳에 |
| Adapter | SDK API를 우리 계약 모양으로. 로직 넣지 않기 |
| Composition Root | 객체 그래프 조립은 시작 지점 한 곳 |
| Singleton(Lazy) | 프로세스당 하나, 스레드 안전 지연 생성, 숨은 의존성 주의 |
| Facade | 여러 하위 시스템을 하나의 단순 API로 |
| Observer | 이벤트 방송, 구독자 격리(try/catch), 해제 필수 |
| 보상 롤백 | 실패 시 이미 한 일을 역순으로 되돌림 |

## 7. VS2022 복습 (10분)

- F9, F5, F10, F11, Shift+F11, Ctrl+Alt+C(호출 스택), Ctrl+Alt+I(직접 실행), Ctrl+Alt+E(예외 설정), Ctrl+Alt+B(중단점 창)
- 조건부 중단점과 Tracepoint를 하나씩 다시 만들어 본다.

## 8. 셀프 체크 (1주차 종합)

1. OmniSight에서 "부팅 실패해도 설정 화면은 열린다"는 어떻게 구현되어 있나?
<details><summary>답</summary>Bootstrapper가 단계별 예외를 이슈 목록에 쌓고, 장치 생성이 실패하면 빈 DeviceManager(Null Object)로 VisionSystem을 만든다. VisionSystem은 이슈가 있으면 StartInternal에서 시작을 거부한다.</details>

2. 1주차 패턴 중 "상속"을 쓰는 것과 "합성"을 쓰는 것을 나눠 보라.
<details><summary>답</summary>상속: Template Method(DeviceBase, CallbackCameraAdapterBase, AiEngineBase). 합성: Adapter(드라이버를 필드로), Facade(DeviceManager·Inspection을 필드로), Strategy/DI(파이프라인이 인터페이스를 필드로), Factory는 생성 책임 분리.</details>

3. 소유권 이전을 코드에서 표시하는 관용구 두 가지는?
<details><summary>답</summary>① 넘긴 뒤 로컬 변수를 null로 만들어 finally/catch의 Dispose 대상에서 빼기(`devices = null`, `frame = null`). ② `Interlocked.Exchange(ref _owner, null)`로 꺼내서 한 번만 Dispose.</details>

4. 시작 실패 롤백과 정지 실패 처리는 왜 다르게(예외를 삼키느냐 던지느냐) 했나?
<details><summary>답</summary>시작 롤백 중의 정지 실패는 원래 시작 실패 예외를 가리지 않도록 기록만 하고 삼킨다(throwOnFailure:false). 정상 정지에서의 실패는 호출자가 Faulted로 판단해야 하므로 AggregateException으로 던진다.</details>

5. 다음 주(멀티스레드)를 위해 1주차 코드에서 "다른 스레드가 접근하는 곳"을 3군데 찾아라.
<details><summary>답</summary>① FakeCameraDriver 타이머 콜백(작업자 스레드) → 어댑터 OnFrame. ② ImageFrame.Dispose(정지 스레드 vs 작업자). ③ VisionSystem 이벤트 → UI 구독자(UI 스레드가 아닌 곳에서 호출될 수 있음). 그 밖에 Bootstrapper.InitializeAsync(Task.Run).</details>
