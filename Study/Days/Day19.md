# Day 19 (금, 2h) · 화면은 한 명만 만진다: WinForms UI 스레드

## 1. 오늘 목표

- **완료 기준**
  - "컨트롤은 만든 스레드(UI 스레드)에서만 만진다"는 규칙과 그 이유(Win32 메시지 루프, 핸들 스레드 친화성)를 설명할 수 있다.
  - "크로스 스레드 작업이 잘못되었습니다" 예외를 **재현하고** `InvokeRequired` + `BeginInvoke`로 고쳤다.
  - `Invoke`(동기)와 `BeginInvoke`(비동기)의 차이와, 장비 프로그램에서 `BeginInvoke`를 쓰는 이유(작업자 스레드를 UI가 붙잡지 않게, 교착 방지)를 설명할 수 있다.
  - "UI가 바쁘면 새 프레임을 버린다"(CAS 바쁨 플래그)와 "대기 중 비트맵의 소유권은 먼저 실행된 쪽이 갖는다"(`Interlocked.Exchange`) 기법을 손으로 구현했다.
  - 타이머 3종(`Forms.Timer`, `Threading.Timer`, `Timers.Timer`)이 어느 스레드에서 도는지 말할 수 있다.
  - MiniSight: `ControlExtensions.RunOnUi`, `LiveView`(바쁘면 버림, 10fps), `ImageCanvas`(더블 버퍼, 줌) 작성.
- **최소분량**: 2, 3, 4.1~4.2, 5의 1~3
- **확장분량**: 5의 4 줌 캔버스, 7번 4~5

## 2. 🧒 초1 비유

- **UI 스레드 = 칠판 담당 반장**. 칠판(화면)은 반장만 쓸 수 있다. 다른 친구(작업자 스레드)가 직접 쓰면 선생님(Windows)이 혼낸다(예외).
- **BeginInvoke = 반장에게 쪽지 주고 자리로 돌아가기**. "이거 칠판에 써 줘" 쪽지를 반장 책상(메시지 큐)에 놓고 바로 자기 일을 한다.
- **Invoke = 반장 옆에 서서 다 쓸 때까지 기다리기**. 반장이 마침 나(작업자)를 기다리는 중이었다면? 둘 다 멈춘다(교착).
- **바쁘면 버리기 = 반장이 아직 쓰고 있으면 새 쪽지는 버린다**. 사진은 1초에 30장 오는데 칠판은 10번만 고쳐도 충분하다. 검사는 30장 다 하고, **화면만** 줄인다.
- **더블 버퍼 = 뒤에서 그림 다 그리고 한 번에 보여주기**. 칠판에 직접 지우고 그리면 깜빡거린다.

## 3. OmniSight 지도

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| UI로 보내기 | `OmniSight/UI/MainForm.cs:777-798` | `RunOnUiThread`: Disposed 검사 → `InvokeRequired`면 `BeginInvoke`, 실패(핸들 없음) 무시 |
| 상태 표시 | `OmniSight/UI/Controls/LiveCameraDisplayControl.cs:144-160` | `SetCameraState`도 같은 패턴 |
| 10fps 제한 | `OmniSight/UI/Controls/LiveCameraDisplayControl.cs:121-139` | 검사 경로와 무관, 화면만 |
| 라이브 선 미리보기 | `OmniSight/UI/Controls/LiveCameraDisplayControl.cs:68-119` | 바쁨 CAS → Clone → `Task.Run`에서 처리 → 끝나면 플래그 해제, 예약 실패 시 즉시 정리 |
| 바쁘면 버림 | `OmniSight/UI/Controls/ImageDisplayControl.cs:53-58` | `CompareExchange(ref _isUpdating, 1, 0)` |
| 대기 비트맵 소유권 | `OmniSight/UI/Controls/ImageDisplayControl.cs:66-90` | `Exchange(ref _pendingBitmap, newBitmap)?.Dispose()`, 콜백에서 `Exchange(ref _pendingBitmap, null)` |
| 옛 이미지 해제 | `OmniSight/UI/Controls/ImageDisplayControl.cs:103-125` | `old = pic.Image; pic.Image = new; old?.Dispose();` 순서 |
| 핸들 파괴 정리 | `OmniSight/UI/Controls/ImageDisplayControl.cs:135-147` | 콜백이 못 온 비트맵도 여기서 해제 |
| 더블 버퍼 | `OmniSight/UI/Controls/BaseUserControl.cs:83-100` | `WS_EX_COMPOSITED`, 디자이너에선 끔 |
| 공통 적용 | `OmniSight/UI/Controls/BaseUserControl.cs:102-110` | `OnHandleCreated`에서 폰트·번역 (Template Method hook) |
| 줌 캔버스 | `OmniSight/UI/Controls/ZoomableImageCanvas.cs:19-40`, `:103-130` | `ScrollableControl`, Mat → Bitmap 복사 후 옛 비트맵 해제 |
| 설계 원칙 | `OmniSight/docs/reference/FA_TASK_AWAIT_GUIDE.ko.md` §12 | WinForms 주의 사항 |

**집에서 Claude에게:**
```text
ImageDisplayControl.cs 40~147줄을 반장·쪽지 비유로 설명해줘. 특히 _pendingBitmap을
Interlocked.Exchange로 주고받는 이유(BeginInvoke 콜백과 OnHandleDestroyed 중 누가 먼저 실행될지 모름)와
picImage.Image를 바꾸기 전에 old를 잡아두는 이유를 알려줘. 그리고 MainForm.RunOnUiThread에서
BeginInvoke의 InvalidOperationException을 삼키는 이유도.
```

## 4. 왜 이렇게 썼나

### 4.1 타이머 3종

| 타이머 | 콜백 스레드 | 겹침 | 쓰는 곳 |
|---|---|---|---|
| `System.Windows.Forms.Timer` | **UI 스레드** (메시지 루프) | 안 겹침. UI가 바쁘면 늦게 옴 | 화면 갱신, 감시 표시 (1초, 100ms) |
| `System.Threading.Timer` | 스레드풀 | **겹칠 수 있음** → `Interlocked` 가드 필요 | 10분 헬스 로그 (`VisionSystem.cs:547-552`) |
| `System.Timers.Timer` | 기본 스레드풀 (`SynchronizingObject`로 UI 가능) | 겹칠 수 있음 | OmniSight는 안 씀 |

### 4.2 결정과 이유

| 결정 | 이유 |
|---|---|
| 항상 `BeginInvoke` | 작업자(카메라 콜백, 파이프라인)가 UI를 기다리면 UI가 느릴 때 검사가 느려지고, UI가 작업자를 기다리는 순간 교착 |
| `IsDisposed`/`IsHandleCreated` 확인 + 예외 삼킴 | 폼이 닫히는 중에 이벤트가 올 수 있음. 핸들이 없으면 `BeginInvoke`가 던짐 → 종료 중엔 무시가 정답 |
| 화면만 10fps | 비트맵 변환·GDI+ 그리기는 비쌈. 검사 처리량을 화면이 갉아먹지 않게 |
| 바쁘면 버림 | UI 큐에 비트맵이 쌓이면 메모리 폭증 + 화면이 몇 초 늦게 보임 |
| 소유권 경쟁을 Exchange로 | "콜백이 실행되기 전에 컨트롤이 파괴됨" 경로에서 비트맵 누수/이중 해제 방지 |
| 옛 Image를 잡고 교체 후 Dispose | PictureBox가 그리는 중에 이미지가 해제되면 `ArgumentException`(Parameter is not valid) 크래시 |
| `WS_EX_COMPOSITED` | 자식 컨트롤이 많은 화면의 깜빡임 제거. 디자이너에서는 렌더링 버그 → 런타임만 |

### 4.3 시니어 함정

- `Control.InvokeRequired`는 **핸들이 아직 없으면 false**를 돌려준다. 생성 직후 백그라운드에서 부르면 "UI 스레드인 줄 알고" 직접 접근 → 예외. 그래서 `IsHandleCreated` 확인.
- `Invoke`를 lock 안에서 부르면 거의 확실히 교착 후보.
- GDI+ `Bitmap`도 네이티브 자원. Dispose 안 하면 **GDI 개체 수 한도(10,000)**에 걸려 화면이 깨진다. 작업 관리자 → 세부 정보 → 열 선택 → "GDI 개체".
- 이벤트를 `+=`만 하고 폼 종료 시 `-=` 안 하면 Day06의 누수.

## 5. Kata: 반장과 쪽지 (WinForms)

`MiniSight.Katas`가 콘솔이면 `UiLabForm`을 띄우는 메서드로 만든다(`Application.Run(new UiLabForm())`, `[STAThread]` 필요).

```csharp
// KATA: Day19 (WinForms)
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Katas.Day19
{
    public sealed class UiLabForm : Form
    {
        private readonly Label _label = new Label { Dock = DockStyle.Top, Height = 40 };
        private readonly PictureBox _picture = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom };
        private System.Threading.Timer _producer;
        private int _busy;
        private Bitmap _pending;
        private long _lastShown;
        private int _received, _shown;

        public UiLabForm()
        {
            Text = "Day19 UI Lab";
            Width = 640; Height = 520;
            Controls.Add(_picture);
            Controls.Add(_label);
            var wrong = new Button { Text = "1. 잘못된 방법", Dock = DockStyle.Bottom };
            var right = new Button { Text = "2. 30fps 생산 + 10fps 표시", Dock = DockStyle.Bottom };
            wrong.Click += (s, e) => ThreadPool.QueueUserWorkItem(_ =>
            {
                try { _label.Text = "작업자 스레드가 직접 씀"; }       // 디버거에서 InvalidOperationException
                catch (InvalidOperationException ex) { RunOnUi(() => _label.Text = "예외: " + ex.Message); }
            });
            right.Click += (s, e) => StartProducer();
            Controls.Add(wrong);
            Controls.Add(right);
        }

        private void RunOnUi(Action action)
        {
            if (IsDisposed || Disposing || !IsHandleCreated) return;
            if (!InvokeRequired) { action(); return; }
            try { BeginInvoke(action); }
            catch (InvalidOperationException) { }                       // 종료 중 핸들 없음
        }

        private void StartProducer()
        {
            if (_producer != null) return;
            _producer = new System.Threading.Timer(_ => OnFrame(), null, 0, 33);   // 스레드풀에서 30fps
        }

        // 카메라 콜백 흉내: 작업자 스레드
        private void OnFrame()
        {
            Interlocked.Increment(ref _received);
            long now = Stopwatch.GetTimestamp();
            long last = Interlocked.Read(ref _lastShown);
            if (last != 0 && now - last < Stopwatch.Frequency / 10) return;        // 10fps 제한
            if (Interlocked.CompareExchange(ref _lastShown, now, last) != last) return;
            if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) return;          // UI 바쁘면 버림

            var bmp = new Bitmap(320, 240);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
                g.DrawString(DateTime.Now.ToString("HH:mm:ss.fff"), Font, Brushes.Lime, 10, 10);
            }
            Interlocked.Exchange(ref _pending, bmp)?.Dispose();
            try
            {
                BeginInvoke(new Action(() =>
                {
                    Bitmap mine = Interlocked.Exchange(ref _pending, null);
                    try
                    {
                        if (mine == null) return;
                        Image old = _picture.Image;
                        _picture.Image = mine;                                 // 교체 먼저
                        if (old != null) old.Dispose();                        // 그다음 옛것 해제
                        _shown++;
                        _label.Text = "받음 " + Volatile.Read(ref _received) + " / 표시 " + _shown;
                    }
                    finally { Interlocked.Exchange(ref _busy, 0); }
                }));
            }
            catch (InvalidOperationException)
            {
                Interlocked.Exchange(ref _pending, null)?.Dispose();
                Interlocked.Exchange(ref _busy, 0);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            var timer = Interlocked.Exchange(ref _producer, null);
            if (timer != null)
            {
                using (var done = new ManualResetEvent(false))
                {
                    timer.Dispose(done);                // 실행 중인 콜백이 끝날 때까지
                    done.WaitOne(1000);
                }
            }
            base.OnFormClosing(e);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            Interlocked.Exchange(ref _pending, null)?.Dispose();
            if (_picture.Image != null) { _picture.Image.Dispose(); _picture.Image = null; }
            base.OnHandleDestroyed(e);
        }
    }

    public static class KataDay19
    {
        public static void Run() { Application.Run(new UiLabForm()); }
    }
}
```

> 참고: `OnFormClosing`에서 UI 스레드가 `done.WaitOne(1000)`으로 잠깐 기다리는데, 타이머 콜백은 UI를 **기다리지 않고**(`BeginInvoke`) 끝나므로 교착이 없다. 콜백에서 `Invoke`를 썼다면 여기서 교착이 난다 → 직접 바꿔서 확인해 볼 것(확장).

확인: "받음"은 30/초씩, "표시"는 약 10/초씩 늘어난다.

## 6. MiniSight 오늘 단계

1. `UI/ControlExtensions.cs`: `public static void RunOnUi(this Control c, Action a)` — Kata의 `RunOnUi`.
2. `UI/LiveView.cs : UserControl` — `ShowFrame(Mat frame)`는 **어떤 스레드에서든** 호출 가능. 10fps 제한 → 바쁘면 버림 → `BitmapConverter.ToBitmap(frame)`(OpenCvSharp.Extensions) → 대기 비트맵 Exchange → BeginInvoke로 교체.
3. `UI/ImageCanvas.cs : Control` — 더블 버퍼(`SetStyle(ControlStyles.OptimizedDoubleBuffer | AllPaintingInWmPaint | UserPaint, true)`), `SetImage(Mat)`(UI 스레드 전용, 옛 비트맵 해제), `OnPaint`에서 줌 배율로 `DrawImage`, 마우스 휠로 줌(1.2배, 0.05~16 범위).
4. 구독: `FrameCaptured` 핸들러에서 `liveView.ShowFrame(frame.Image)` — **핸들러 안에서만 프레임이 유효**하니 ToBitmap(복사)은 핸들러 안에서.

## 7. VS2022 디버깅 실습

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | Kata 버튼 1 클릭 (디버거 연결 상태) | `InvalidOperationException: 크로스 스레드 작업이 잘못되었습니다` 즉시 중단. 디버거가 있을 때만 검사함(`Control.CheckForIllegalCrossThreadCalls`) |
| 2 | 같은 줄에서 **스레드 창** | 현재 스레드가 "작업자 스레드", 주 스레드는 "Main Thread"(녹색 화살표/이름) |
| 3 | BeginInvoke 람다 안 중단점 → 스레드 창 | 이번엔 **Main Thread** |
| 4 | 콜백에서 `BeginInvoke`를 `Invoke`로 바꾸고 폼 닫기 | 교착 재현(1초 후 풀림) → 병렬 스택으로 확인 → 원복 |
| 5 | 작업 관리자 → 세부 정보 → 열 선택 "GDI 개체" → `old.Dispose()`를 지우고 1분 실행 | GDI 개체 수가 계속 증가 → 원복 |

## 8. 셀프 체크

1. 작업자 스레드에서 컨트롤을 직접 바꾸면 왜 안 되나?
<details><summary>답</summary>Win32 창 핸들은 만든 스레드의 메시지 큐에 묶여 있고 WinForms 컨트롤은 스레드 안전하지 않다. 다른 스레드가 접근하면 메시지 처리와 경쟁해 화면 깨짐·교착·랜덤 크래시가 난다. 디버거 연결 시 WinForms가 예외로 알려준다.</details>

2. `Invoke` 대신 `BeginInvoke`를 쓰는 이유 2가지.
<details><summary>답</summary>① 작업자가 UI 처리 완료를 기다리지 않아 UI가 느려도 검사·취득이 느려지지 않는다. ② UI 스레드가 그 작업자를 기다리고 있을 때(Stop, Join) 발생하는 교착을 피한다.</details>

3. 화면 갱신을 10fps로 제한해도 검사 결과에 영향이 없는 이유는?
<details><summary>답</summary>제한은 미리보기 이벤트 경로에서만 프레임 표시를 건너뛴다. 검사용 프레임은 별도로 큐에 들어가 파이프라인이 전부 처리한다. 화면 표시와 검사가 다른 경로다.</details>

4. `System.Threading.Timer` 콜백에 재진입 가드가 필요한 이유는?
<details><summary>답</summary>콜백이 주기보다 오래 걸리면 다음 콜백이 다른 스레드풀 스레드에서 동시에 실행된다. 공유 상태 경쟁과 작업 누적이 생기므로 Interlocked 플래그로 실행 중이면 건너뛴다.</details>

5. PictureBox 이미지를 교체할 때 옛 이미지를 먼저 Dispose하면 생기는 문제는?
<details><summary>답</summary>PictureBox가 아직 옛 이미지를 참조하고 있는 상태에서 다시 그리면 해제된 GDI+ 객체를 그리려다 ArgumentException("매개 변수가 잘못되었습니다")이 나며 앱이 죽는다. 새 이미지로 교체한 뒤 옛것을 해제한다.</details>

## 9. 블로그 포인트

- 제목 예: `[WinForms] 카메라 30fps, 화면은 10fps: lock 없이 UI 프리뷰 제한하기`
- 받음/표시 카운터 캡처, GDI 개체 수 증가 실험, Invoke 교착 재현.
