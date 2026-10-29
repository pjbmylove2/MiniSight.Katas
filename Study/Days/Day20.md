# Day 20 (토, 4h) · MiniSight 화면 통합 + 성능 프로파일러

## 1. 오늘 목표

- **완료 기준**
  - MiniSight WinForms 메인 화면: **Start/Stop 버튼(화면 안 멈춤), 카메라 2대 라이브 화면(10fps), 결과 그리드(최근 200건), 처리 이미지 캔버스(줌), 상태 표시줄(상태/처리 수/메모리)**.
  - Stop을 누르는 동안 창을 움직여도 멈추지 않는다 (Stop을 `await Task.Run`으로 실행, 버튼 비활성화로 중복 클릭 방지).
  - 폼을 닫으면 모든 이벤트 구독이 해제되고, 종료 후 프로세스가 남지 않는다.
  - VS2022 **성능 프로파일러**로 CPU 사용량과 .NET 개체 할당을 측정해 "가장 비싼 함수 3개"를 찾았다.
- **최소분량**: 3, 4, 5.1~5.4
- **확장분량**: 5.5 결과 그리드 최적화, 6번 프로파일러 2단계

## 2. 🧒 초1 비유

**공장 관제실 만들기**. 창문(라이브 화면)으로 기계를 보고, 전광판(결과 그리드)으로 결과를 보고, 큰 버튼(Start/Stop)으로 운전한다. 관제실 직원(UI 스레드)은 **버튼 누르고 바로 다음 일**을 한다. 기계가 멈출 때까지 버튼 앞에 서서 기다리지 않는다. 대신 "멈추는 중..." 불을 켜고 버튼을 잠근다.

## 3. OmniSight 지도

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 이벤트 구독 | `OmniSight/UI/MainForm.cs:585-617` | VisionSystem·장치 이벤트 구독 |
| 구독 해제 | `OmniSight/UI/MainForm.cs:867-938` | `ReleaseUiResources`에서 전부 `-=` |
| 종료 확인 | `OmniSight/UI/MainForm.cs:70-95` 부근 | `OnFormClosing` 확인 대화상자, `OnFormClosed` 정리 |
| Start/Stop | `OmniSight/UI/MainForm.cs:150-192` | 동기 호출 (Day14 위험) |
| 수동 명령 | `OmniSight/UI/MainForm.cs:656-685` | `_cameraCommandInProgress` 중복 방지 + `await Task.Run` — **이 패턴을 Start/Stop에도 적용**하는 것이 오늘 과제 |
| 결과 그리드 | `OmniSight/UI/Controls/InspectionResultsGridView.cs` | 100ms Forms.Timer로 묶어서 갱신 |
| 대시보드 | `OmniSight/UI/Controls/InspectionDashboardControl.cs` | 통계 표시 |

**집에서 Claude에게:**
```text
OmniSight InspectionResultsGridView.cs에서 결과를 매번 바로 그리드에 넣지 않고 타이머로 묶어서 넣는
부분을 보여주고 이유를 설명해줘. 그리고 MainForm 656~685의 ExecuteManualCameraCommandAsync 패턴을
Start/Stop 버튼에 적용하려면 어떤 점(상태 일관성, PLC 명령과의 경쟁)을 더 고려해야 하는지 알려줘.
```

## 4. 왜 이렇게 (설계 포인트)

| 포인트 | 이유 |
|---|---|
| 결과 이벤트는 작업자 스레드에서 옴 | `InspectionCompleted`는 파이프라인 스레드. UI 반영은 `RunOnUi` |
| 결과는 **큐에 모았다가 100ms마다** 그리드에 일괄 반영 | 초당 60건을 하나씩 BeginInvoke하면 메시지 큐 폭주, 그리드가 매번 다시 그림 |
| 그리드 행 수 상한(200) | 하루 수십만 건이면 메모리 증가·스크롤 느려짐 |
| `VirtualMode` 또는 `BindingList` + `RaiseListChangedEvents=false` 일괄 | 대량 갱신 성능 |
| Start/Stop 중엔 버튼 비활성화 | async로 바꾸면 사용자가 연타 가능 → 상태 꼬임 방지 |
| `async void` 핸들러는 전체 try/catch | 예외가 UI 스레드로 튀면 ThreadException → 앱 종료(Day01 처리기) |

## 5. MiniSight 오늘 단계 (4시간)

### 5.1 MainForm 레이아웃
- 위: 버튼(Initialize/Start/Stop), 상태 라벨.
- 가운데: `LiveView` 2개(Cam1/Cam2), 오른쪽에 `ImageCanvas`(마지막 처리 이미지).
- 아래: `DataGridView`(Source, FrameId, Time, OK/NG, Message, ms).
- 맨 아래 `StatusStrip`: 상태 / 처리 수 / 실패 수 / PrivateMB (HealthMonitor, `Forms.Timer` 1초).

### 5.2 async Start/Stop
```csharp
private async void btnStop_Click(object sender, EventArgs e)
{
    if (_commandInProgress) return;
    _commandInProgress = true;
    SetButtons(false);
    lblState.Text = "정지 중...";
    try
    {
        var result = await Task.Run(() => _vision.Stop());   // 무거운 정지는 UI 밖에서
        if (!result.ok) MessageBox.Show(this, result.message, "정지 실패");
    }
    catch (Exception ex)
    {
        MessageBox.Show(this, ex.Message, "정지 오류");
    }
    finally
    {
        _commandInProgress = false;
        SetButtons(true);
    }
}
```
`(bool ok, string message)` 튜플 이름은 Day06 VisionSystem과 맞춘다. 생각해 볼 것: 정지 중에 **PLC가 Start 명령**을 보내면? (VisionSystem 내부 lock이 직렬화하지만, UI 표시는 어떻게 맞추나)

### 5.3 이벤트 연결/해제
- `OnLoad`에서 `_vision.StateChanged += ...`, `_runtime.InspectionCompleted += ...`, 카메라 `FrameCaptured += ...`.
- `OnFormClosed`에서 **같은 핸들러를 전부 `-=`** (람다로 구독하면 해제 불가 → 메서드로 구독).
- 종료 전 운전 중이면 확인 후 `await` 정지.

### 5.4 결과 묶음 반영
- `ConcurrentQueue<InspectionResult> _pendingResults` — 이벤트 핸들러(작업자 스레드)에서 Enqueue만.
- `Forms.Timer` 100ms: 큐에서 최대 100건 꺼내 그리드에 추가, 200행 초과분 삭제, 마지막 처리 이미지는 별도 슬롯(`Interlocked.Exchange`로 최신 1장만 보관)에서 꺼내 캔버스에.
- 처리 이미지 슬롯에 넣는 Mat은 **이벤트 핸들러 안에서 Clone**(이벤트 후 파이프라인이 Dispose하니까). 슬롯에서 밀려난 옛 Mat은 Dispose.

### 5.5 (확장) 그리드 최적화
- `DoubleBuffered`를 리플렉션으로 켜거나 `SuspendLayout/ResumeLayout`.
- 1분 운전 후 GDI 개체·핸들 수가 일정한지 확인.

## 6. VS2022 디버깅 실습: 성능 프로파일러

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | 구성 **Release**, 디버그 → **성능 프로파일러(Alt+F2)** → **CPU 사용량** 체크 → 시작 | |
| 2 | MiniSight Start → 30초 운전 → Stop → 수집 중지 | 보고서 |
| 3 | "상위 함수(Top Functions)" / "핫 경로(Hot Path)" | 예상: `Cv2.*`, `BitmapConverter.ToBitmap`, PNG 인코딩 |
| 4 | 라이브 화면 10fps 제한을 끄고 다시 측정 | ToBitmap 비중 증가 → 제한의 효과를 숫자로 |
| 5 | 프로파일러 다시 → **.NET 개체 할당 추적** | 할당 많은 타입: `Mat` 래퍼, `Bitmap`, 문자열(로그). 프레임당 할당을 줄일 후보 |
| 6 | (확장) **이벤트 뷰어/동시성 시각화 도우미**가 있는 버전이면 스레드 타임라인 | 파이프라인 스레드가 대기(큐)인지 실행인지 |

기록: "가장 비싼 함수 3개와 비율"을 PROGRESS.md에. 블로그 재료.

## 7. 셀프 체크

1. Stop을 `await Task.Run(() => _vision.Stop())`으로 바꾸면 어떤 새 문제가 생기나?
<details><summary>답</summary>정지 중에도 UI가 입력을 받으므로 버튼 연타, 다른 화면 이동, 폼 닫기, PLC 명령 등이 겹칠 수 있다. 진행 중 플래그·버튼 비활성화·종료 시 대기 처리가 필요하다. 또한 Stop 내부에서 UI 컨트롤에 직접 접근하는 코드가 있으면 크로스 스레드 예외가 난다.</details>

2. 결과를 이벤트마다 BeginInvoke하지 않고 큐 + 타이머로 묶는 이유는?
<details><summary>답</summary>고빈도 BeginInvoke는 메시지 큐를 채워 입력·페인트가 밀리고 그리드가 매 건 레이아웃을 다시 계산한다. 일정 주기로 묶으면 UI 부하가 상한을 갖고 화면 반응성이 유지된다.</details>

3. 람다로 이벤트를 구독하면 해제가 어려운 이유는?
<details><summary>답</summary>`-=`에 같은 델리게이트 인스턴스를 줘야 하는데 람다를 다시 쓰면 다른 인스턴스가 된다. 필드에 저장해 두거나 메서드 그룹으로 구독해야 해제할 수 있다.</details>

4. 처리 이미지를 화면에 보내려면 왜 이벤트 핸들러 안에서 Clone해야 하나?
<details><summary>답</summary>InspectionCompleted의 이미지는 이벤트 동안만 유효하고 이후 파이프라인이 Dispose한다. 타이머가 나중에 쓰려면 독립 사본이 필요하다.</details>

5. 성능 측정을 Release 구성에서 하는 이유는?
<details><summary>답</summary>Debug는 JIT 최적화가 꺼져 있고 디버그 검사 코드가 있어 실제 병목과 다른 결과가 나온다. 실제 배포와 같은 조건에서 측정해야 한다.</details>

## 8. 블로그 포인트

- 제목 예: `[WinForms + 멀티스레드] 검사 화면이 멈추지 않게 만드는 5가지 규칙 (프로파일러 측정 포함)`
- 프로파일러 Top Functions 캡처(MiniSight), 10fps 제한 전/후 CPU 비교.
