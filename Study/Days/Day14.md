# Day 14 (일, 4h) · 데드락 실험실 + 2주차 복습

## 1. 오늘 목표

- **완료 기준**
  - WinForms에서 **UI 스레드 `.Wait()` 데드락**과 **lock 순서 데드락**을 직접 만들고, VS2022 **병렬 스택**으로 원인을 찾았다.
  - 교착 상태 4조건(상호 배제, 점유 대기, 비선점, 순환 대기)을 말하고, 각각 어떻게 깨는지 예를 들 수 있다.
  - OmniSight에서 UI 멈춤 위험이 있는 경로 1개를 코드로 짚을 수 있다.
  - 2주차 졸업 시험(00-Roadmap 참고)을 통과했다.
  - 블로그 1편.
- **최소분량**: 3.1~3.2 데드락 실험, 5 졸업 시험
- **확장분량**: 3.3 OmniSight 위험 경로 시뮬레이션, 블로그 2편

## 2. 🧒 초1 비유

- **데드락 = 좁은 다리 위 두 자동차**. 서로 "네가 먼저 비켜"라며 기다리면 둘 다 영원히 못 간다.
- **UI `.Wait()` 데드락 = 사장님이 배달을 기다리며 문을 잠금**. 배달원(await 이후 코드)은 "사장님께 직접 전달" 규칙이라 문 앞에서 기다린다. 사장님은 배달이 와야 문을 연다. 둘 다 멈춤.
- **lock 순서 데드락 = 숟가락과 젓가락**. 영희는 숟가락을 들고 젓가락을 기다리고, 철수는 젓가락을 들고 숟가락을 기다린다. 해결: "항상 숟가락 먼저" 규칙(순서 고정).

## 3. 실험실

### 3.1 실험 A: UI 스레드 `.Result` 데드락

MiniSight에 `DeadlockLabForm`을 만들고 버튼 3개:

```csharp
private async Task<string> LoadAsync()
{
    await Task.Delay(500);            // ConfigureAwait 없음 → UI 스레드로 돌아오려 함
    return "완료";
}

private void btnDeadlock_Click(object sender, EventArgs e)
{
    label1.Text = LoadAsync().Result; // ❌ 영원히 멈춤
}

private void btnLibraryFix_Click(object sender, EventArgs e)
{
    // 임시 처방: 내부가 UI로 안 돌아오면 안 멈춘다. 하지만 UI는 0.5초 얼어 있다.
    label1.Text = Task.Run(() => LoadAsync()).Result;
}

private async void btnCorrect_Click(object sender, EventArgs e)
{
    try { label1.Text = await LoadAsync(); }     // ✅ 정답: 끝까지 async
    catch (Exception ex) { label1.Text = ex.Message; }
}
```

절차:
1. `btnDeadlock` 클릭 → 화면이 멈춤 (창 이동 안 됨, "응답 없음").
2. VS2022 **모두 중단(Ctrl+Alt+Break)** → 디버그 → 창 → **병렬 스택**.
3. 메인 스레드 스택 맨 위: `Task.Wait` / `get_Result` ← `btnDeadlock_Click`. 작업 보기로 바꾸면 `LoadAsync`가 "UI 스레드에 예약됨/대기" 상태.
4. **스레드 창**에서 Main Thread 더블클릭 → 정확한 줄 확인.
5. 왜 `btnLibraryFix`는 안 멈추는지 말로 설명 → 그래도 왜 나쁜지(UI가 얼어 있음, 스레드 낭비).

### 3.2 실험 B: lock 순서 데드락

```csharp
private readonly object _spoon = new object();
private readonly object _chopsticks = new object();

private void Younghee() { lock (_spoon) { Thread.Sleep(100); lock (_chopsticks) { } } }
private void Cheolsu()  { lock (_chopsticks) { Thread.Sleep(100); lock (_spoon) { } } }

private void btnLockDeadlock_Click(object sender, EventArgs e)
{
    var a = new Thread(Younghee) { Name = "영희", IsBackground = true };
    var b = new Thread(Cheolsu) { Name = "철수", IsBackground = true };
    a.Start(); b.Start();
    label1.Text = a.Join(2000) && b.Join(2000) ? "OK" : "데드락 의심 (2초 초과)";
}
```

절차:
1. 클릭 → "데드락 의심".
2. 모두 중단 → **병렬 스택**: 영희·철수 두 스레드가 `Monitor.Enter`에서 서로 막힌 그림.
3. 스레드 창에서 각 스레드의 **위치** 열 확인.
4. 수정: 둘 다 `_spoon` → `_chopsticks` 순서로. 또는 `Monitor.TryEnter(_chopsticks, 500)` 타임아웃(비선점 조건 깨기)으로 실패 처리.

### 3.3 실험 C: OmniSight 위험 경로 시뮬레이션 (확장)

OmniSight의 실제 경로를 MiniSight로 흉내:
- `MainForm`의 정지 버튼 핸들러가 `_visionSystem.Stop()`을 **UI 스레드에서 동기로** 호출 (`OmniSight/UI/MainForm.cs:186`).
- `VisionSystem.Stop`은 `lock(_sync)` 안에서 `InspectionPipeline.Stop` → `worker.Wait(10000)` → 실패 시 `worker.Wait()` 무제한 (`OmniSight/Application/Inspection/InspectionPipeline.cs:160-163`).
- 작업자가 보고 큐(가득 참)에서 `Add` 대기 중이고, 보고 작업자가 느린 디스크에서 멈춰 있으면 → **UI가 오래(최악은 무한히) 멈춤**.

MiniSight 재현:
1. `CsvReporter.Report`에 `Thread.Sleep(5000)` 삽입, 보고 큐 용량을 2로.
2. 카메라 30fps로 Start → 3초 뒤 Stop 버튼.
3. 화면이 얼마나 멈추는지 측정. 병렬 스택으로 UI 스레드가 어디서 기다리는지 확인.
4. 오늘은 **고치지 않는다**. 증상과 원인만 PROGRESS.md에 기록 → Day25에서 async Stop + 타임아웃으로 개선.

**집에서 Claude에게:**
```text
OmniSight MainForm.cs 175~191줄(Start/Stop 버튼)과 656~680줄(ExecuteManualCameraCommandAsync)을 비교해줘.
왜 카메라 수동 명령은 Task.Run + await로 했고 Stop은 동기로 했는지 추측해 보고,
Stop을 동기로 한 장점(상태 일관성, 중복 클릭 방지)과 위험(UI 멈춤)을 정리해줘.
```

## 4. 교착 상태 4조건과 깨는 법

| 조건 | 뜻 | 깨는 법 | 예 |
|---|---|---|---|
| 상호 배제 | 자원을 한 명만 | lock 없는 설계(불변 스냅샷, CAS) | HardwareMonitor 스냅샷 |
| 점유 대기 | 하나 쥐고 다른 것 기다림 | 필요한 걸 한 번에 잡거나, 쥔 채로 기다리지 않기 | lock 안에서 Wait/Invoke 금지 |
| 비선점 | 강제로 뺏을 수 없음 | 타임아웃(`TryEnter`, `Wait(ms)`)으로 포기하게 | `Monitor.TryEnter`, `Wait(3000)` |
| 순환 대기 | A→B, B→A | **lock 순서 고정** | 숟가락 먼저 규칙 |

UI 데드락은 "UI 스레드라는 자원"을 쥔 채(`.Wait()`) 그 자원이 필요한 계속(continuation)을 기다리는 **점유 대기 + 순환**이다. 해결: 끝까지 async(점유 대기 제거) 또는 `ConfigureAwait(false)`(순환 제거).

## 5. 2주차 졸업 시험 (40분, 아무것도 보지 않고)

1. 카메라 2대 기준 MiniSight 전체 흐름을 그린다: 스레드(이름과 수), 큐(이름, 용량, 꽉 찼을 때 동작), 소유권 이동 지점(누가 Dispose).
2. Stop 순서 4단계를 말한다. drain과 cancel의 차이를 말한다.
3. 아래 표를 채운다:

| 상황 | 쓸 도구 |
|---|---|
| 카운터 증가 | |
| 정지 플래그 | |
| 여러 필드 한꺼번에 변경 | |
| 최신 상태 묶음을 lock 없이 공유 | |
| async 메서드 안에서 상호 배제 | |
| 한 번만 실행 | |
| 미리보기 초당 10회 제한 | |
| 콜백 API를 await로 | |
| 작업 타임아웃 | |

<details><summary>답</summary>Interlocked.Increment / volatile bool 또는 CancellationToken / lock / 불변 객체 + Volatile.Write / SemaphoreSlim(1,1).WaitAsync / Interlocked.Exchange / CompareExchange 루프(타임스탬프) / TaskCompletionSource / Task.WhenAny + Task.Delay(timeout, token)</details>

4. Claude 구술 면접:
```text
2주차 졸업 면접 해줘. 주제: 멀티스레드 영상 처리 파이프라인. 질문 8개, 꼬리 질문 포함.
특히 "프레임을 버리지 않는다면서 왜 미리보기는 버리나", "LongRunning + async",
"UI 데드락", "Stop이 오래 걸리면 어떻게 할 건가"는 꼭 물어봐줘.
```

## 6. 2주차 용어 카드

| 용어 | 30초 키워드 |
|---|---|
| 스레드풀 고갈 | 블로킹 작업이 풀 스레드를 붙잡음 → 짧은 작업·타이머·await 계속 지연 |
| LongRunning | 전용 스레드 힌트. async 람다면 첫 await까지만 의미 |
| race condition | 읽고-고치고-쓰기 사이 끼어들기 |
| CAS | 기대값이면 교체, 실패 시 재시도 |
| Backpressure | 유한 큐가 차면 생산자가 기다려 속도를 맞춤 |
| Drain | 입력을 막고 남은 일을 끝까지 처리한 뒤 종료 |
| TCS | 콜백/이벤트를 Task로 바꾸는 수동 진동벨 |
| ConfigureAwait(false) | 원래 컨텍스트(UI)로 돌아가지 않음 |
| 데드락 4조건 | 상호 배제, 점유 대기, 비선점, 순환 대기 |

## 7. VS2022 정리 (이번 주에 쓴 창)

스레드(Ctrl+Alt+H) · 작업(Tasks) · 병렬 스택(Ctrl+Shift+D,S) · 병렬 조사식 · Freeze/Thaw/Flag · 진단 도구 메모리 스냅샷 · 예외 설정 · 비동기 호출 스택. → [Cheatsheets/VS2022-Debugging.md](../Cheatsheets/VS2022-Debugging.md)

## 8. 셀프 체크

1. UI `.Result` 데드락을 `ConfigureAwait(false)`로 고치는 것과 끝까지 async로 고치는 것의 차이는?
<details><summary>답</summary>ConfigureAwait(false)는 내부 계속이 UI로 돌아오지 않게 해 순환을 끊지만, UI 스레드는 여전히 블로킹되어 화면이 언다. 또 호출 체인 전체에 빠짐없이 붙여야 한다. 끝까지 async는 UI 스레드를 점유하지 않으므로 화면이 반응하고 근본적으로 안전하다.</details>

2. 병렬 스택 창에서 데드락을 어떻게 알아보나?
<details><summary>답</summary>여러 스레드의 스택 맨 위가 Monitor.Enter, WaitOne, Task.Wait 같은 대기 프레임에 멈춰 있고, 각자 쥔 자원과 기다리는 자원이 서로 엇갈린다. 작업 보기에서는 대기 중인 Task와 그 Task가 기다리는 대상을 함께 본다.</details>

3. lock 안에서 하면 안 되는 일 3가지.
<details><summary>답</summary>① 다른 lock 획득(순서 규칙 없이) ② 블로킹 대기(Task.Wait, Join, 큐 Add) ③ 외부 코드 호출(이벤트, 콜백, Control.Invoke) 및 긴 I/O.</details>

4. Stop을 UI에서 동기로 호출하는 설계의 장점은?
<details><summary>답</summary>정지 완료 전까지 UI가 다른 조작(Start 연타, 설정 변경)을 받지 않아 상태 일관성이 단순해진다. 대신 정지가 길어지면 화면이 멈춘다. async로 바꾸면 버튼 비활성화·진행 표시·타임아웃 정책을 따로 설계해야 한다.</details>

5. 대기에 타임아웃을 줬는데 타임아웃이 나면 무엇을 해야 하나?
<details><summary>답</summary>그냥 넘어가면 안 된다. 알람·로그로 드러내고, 상태를 Faulted 등으로 표시해 재시작을 막거나, 남은 작업을 백그라운드에서 마무리하고 다음 시작을 그 완료에 묶는다(OmniSight 출력 디스패처의 `_stopCompletion` 방식).</details>

## 9. 블로그 포인트

- 제목 예: `[WinForms] async 메서드를 .Result로 부르면 왜 화면이 멈출까: 병렬 스택으로 데드락 잡기`
- 실험 A/B 병렬 스택 스크린샷(MiniSight)이 핵심. 실험 C는 "회사 코드" 대신 "흔한 장비 프로그램 패턴"으로 일반화해서 쓴다.
