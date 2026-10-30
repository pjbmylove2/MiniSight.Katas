# B14 · 패턴 없는 가짜 카메라 (`Timer` + B01~B13 합치기) · 40분 + 졸업 시험

## 1. 오늘 목표
지금까지 배운 키워드만으로 **50줄짜리 가짜 카메라**를 만든다. 디자인 패턴은 하나도 없다. 그리고 Day04가 "이 카메라를 정해진 자리에 넣은 것"이라는 걸 확인한다.

## 2. 🧒 비유: 자동 사진기
- **알람 시계(`Timer`)**가 0.3초마다 울린다. 울릴 때마다 사진기가 **찰칵**하고 번호를 붙인다.
- 찰칵할 때마다 **초인종(event)**을 눌러 "사진 나왔어요!" 하고 알린다.
- Main은 **리모컨(CancellationToken)** 불이 켜질 때까지 기다린다. 5번째 사진을 받으면 정지 버튼을 누른다.
- 다 쓰면 알람 시계를 **반납(Dispose)**한다.

## 3. 오늘 새 키워드 표

| 키워드 | 역할 | 사용법 | 기억할 점 |
|---|---|---|---|
| `System.Threading.Timer` | 정해진 간격마다 **스레드 풀 일꾼**이 메서드를 부른다 | `new Timer(OnTick, null, 처음대기, 간격)` | WinForms의 `Timer`(UI 스레드)와 **다른 것**. `using System.Threading;` |
| `new Timer(OnTick, null, Timeout.Infinite, Timeout.Infinite)` | 만들기만 하고 **아직 안 울림** | | 4개 인자: 부를 메서드, 상태값, 첫 울림까지(ms), 간격(ms) |
| `_timer.Change(0, 300)` | 지금 바로(0) 울리고, 그 뒤 300ms마다 | | 시작 |
| `_timer.Change(Timeout.Infinite, Timeout.Infinite)` | 알람 **끄기** (객체는 살아 있음) | | 정지 |
| `Timeout.Infinite` | "무한" = -1. 타이머에서는 "안 울림" | | |
| `void OnTick(object state)` | 타이머가 부르는 메서드의 정해진 모양 | | `state` = 만들 때 넘긴 값 (여기선 null) |
| `token.WaitHandle.WaitOne()` | 토큰 불이 켜질 때까지 **지금 스레드를 재운다** | `stop.Token.WaitHandle.WaitOne();` | |
| `(sender, id) => { ... }` | 이벤트에 **람다**를 바로 건다 | `cam.FrameArrived += (sender, id) => { ... };` | B03 람다 + B04 이벤트 |
| `sealed` | 이 클래스를 **상속 금지** | `public sealed class SimpleCamera` | 더 고칠 일 없는 완성품 표시 |
| `using (A) using (B) { }` | 두 개를 겹쳐 쓴다. **안쪽(B)이 먼저** Dispose | | 만든 순서의 **반대로** 정리 |

## 4. 코드: `Basics/KataB14.cs`

```csharp
using System;
using System.Threading;

namespace Katas.Basics
{
    public sealed class SimpleCamera : IDisposable                  // B05
    {
        private readonly int _intervalMs;
        private Timer _timer;
        private long _frameId;

        public event EventHandler<long> FrameArrived;               // B04: 사진 번호를 들고 울리는 초인종

        public SimpleCamera(int intervalMs)
        {
            _intervalMs = intervalMs;
        }

        public void Start()
        {
            if (_timer == null)
            {
                _timer = new Timer(OnTick, null, Timeout.Infinite, Timeout.Infinite);
            }
            _timer.Change(0, _intervalMs);                          // 지금 바로, 그 뒤 간격마다
        }

        public void Stop()
        {
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);     // B01: 있으면 끄기
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _timer = null;
        }

        // 타이머 스레드(스레드 풀)에서 불린다 ← B11
        private void OnTick(object state)
        {
            long id = Interlocked.Increment(ref _frameId);          // B13: 겹쳐 불려도 번호 안 겹침
            Console.WriteLine("[스레드 " + Thread.CurrentThread.ManagedThreadId + "] 찰칵 #" + id);
            FrameArrived?.Invoke(this, id);                         // B04
        }
    }

    public static class KataB14
    {
        public static void Run()
        {
            Console.WriteLine("[스레드 " + Thread.CurrentThread.ManagedThreadId + "] Main 시작");
            using (CancellationTokenSource stop = new CancellationTokenSource())   // B12 리모컨
            using (SimpleCamera cam = new SimpleCamera(300))
            {
                cam.FrameArrived += (sender, id) =>                 // B03 람다를 B04 이벤트에 건다
                {
                    Console.WriteLine("  받음 #" + id);
                    if (id == 5)
                    {
                        stop.Cancel();                              // 5장 받으면 정지 버튼
                    }
                };

                cam.Start();
                stop.Token.WaitHandle.WaitOne();                    // 버튼 눌릴 때까지 Main은 잠
                cam.Stop();
                Console.WriteLine("[스레드 " + Thread.CurrentThread.ManagedThreadId + "] Main: 5장 받고 멈춤");
            }                                                       // cam 먼저 Dispose, 그다음 stop
        }
    }
}
```

## 5. 실행 결과 (검증함, 스레드 번호는 PC마다 다름)
```text
[스레드 1] Main 시작
[스레드 4] 찰칵 #1
  받음 #1
[스레드 4] 찰칵 #2
  받음 #2
[스레드 5] 찰칵 #3
  받음 #3
[스레드 4] 찰칵 #4
  받음 #4
[스레드 5] 찰칵 #5
  받음 #5
[스레드 1] Main: 5장 받고 멈춤
```
👀 볼 것 2가지
1. 찰칵할 때 **스레드 번호가 4, 5로 바뀔 수 있다** (실행할 때마다 다르다. 어떤 때는 계속 4번만 나온다). 타이머는 매번 스레드 풀에서 **아무 일꾼이나** 빌려 쓰고, 어느 일꾼일지는 약속하지 않는다. 그래서 `_frameId`를 `Interlocked`로 올린다.
2. "받음"은 찰칵과 **같은 스레드**에서 찍힌다. 이벤트는 **Invoke를 부른 스레드**에서 실행되기 때문이다 (B04).

## 6. 바꿔 보기 (1개)
`if (id == 5)`를 `if (id == 3)`으로 바꾼다. → 3장 받고 멈춘다. 그리고 문구도 "3장 받고 멈춤"으로 고쳐 본다. 확인했으면 되돌린다.

## 7. Day04 다시 보기: 패턴 = 이 카메라를 정해진 자리에 넣은 것

| B14 (패턴 없음) | Day04 MiniSight | 무엇이 추가됐나 |
|---|---|---|
| `SimpleCamera` | `FakeCameraDriver` | 번호 대신 **사진(Mat)**을 보낸다 |
| `FrameArrived` 이벤트 | `FrameReady` 이벤트 | 같다 |
| Main의 람다 `(sender, id) => {...}` | `DriverCameraAdapter.OnFrame` | 받은 사진을 **보관함(큐)**에 넣는다 |
| `stop.Token.WaitHandle.WaitOne()` | `WaitFrameAsync()` | 사진이 올 때까지 기다린다 |
| `new SimpleCamera(300)` | `CameraFactory.Create(CameraKind.Fake, ...)` | **누가 만들지를 한 곳에** 모은다 (Factory) |
| 없음 | `DeviceBase` 상속 | 시작·정지 **순서와 상태**를 부모가 지킨다 (B02 Template Method) |
| 없음 | `ICameraDriver` → `IImageSource` 변환 | 회사마다 다른 드라이버를 **같은 모양**으로 (Adapter) |

> Day04 코드를 열어 위 표의 왼쪽 줄을 하나씩 찾아보자. **패턴은 새 문법이 아니라, 이 키워드들을 어느 클래스에 둘지 정한 배치도**다.

## 8. 셀프 체크
<details><summary>Q1. OnTick은 어느 스레드에서 실행되나? 그게 왜 중요한가?</summary>

스레드 풀의 아무 일꾼이다. 매번 바뀔 수 있다. 그래서 공유 변수는 Interlocked나 lock으로 보호해야 하고, WinForms 화면은 직접 건드리면 안 된다 (Day19 BeginInvoke).
</details>

<details><summary>Q2. using 두 개를 겹쳤을 때 어느 쪽이 먼저 Dispose되나? 왜 이 순서가 맞나?</summary>

안쪽 cam이 먼저, 바깥 stop이 나중이다. 카메라가 먼저 꺼져야 늦은 찰칵이 이미 버린 리모컨(stop)에 Cancel을 부르는 일이 없다. 만든 순서의 반대로 정리하는 게 원칙이다 (Day04 DeviceFactory 역순 정리).
</details>

## 9. 체크
- [ ] 1번째: 보면서 타이핑 → 실행
- [ ] 2번째: 실행 결과 비교
- [ ] 3번째: 지우고 뼈대(할 일 주석)만 보고 채우기
- [ ] 바꿔 보기
- [ ] **졸업 시험** → [README §4](README.md#4-졸업-시험-기초-5일째)
