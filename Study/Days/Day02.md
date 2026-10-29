# Day 02 (화, 2h) · 인터페이스와 계약, SOLID, 소유권

## 1. 오늘 목표

- **완료 기준**
  - "인터페이스에 의존한다"는 말을 OmniSight 예로 설명할 수 있다 (`InspectionPipeline`은 카메라 종류를 모른다).
  - SOLID 5개 중 **DIP, ISP, SRP**를 OmniSight 코드로 하나씩 가리킬 수 있다.
  - MiniSight `Devices/Contracts.cs`(IDevice, IImageSource, DeviceState, ImageFrame)를 손으로 작성했다.
  - `ImageFrame.Dispose()`를 두 번 불러도 안전하다는 걸 디버거로 확인했다.
- **최소분량**: 2, 3, 4, 6
- **확장분량**: 5 Kata 전부, 7번 Watch 실험

## 2. 🧒 초1 비유

- **인터페이스 = 콘센트 모양 약속**. 벽 콘센트(검사기)는 "구멍 두 개" 모양만 약속한다. 드라이기든 선풍기든(카메라든 공유폴더든) 플러그 모양만 맞으면 꽂힌다. 콘센트는 뭐가 꽂혔는지 몰라도 된다.
- **ISP(인터페이스 분리) = 리모컨을 기능별로 나누기**. TV 리모컨에 에어컨 버튼까지 다 있으면 헷갈린다. "영상 주는 장치"와 "미리보기 되는 장치"를 다른 리모컨(인터페이스)으로 나눈다.
- **ImageFrame = 도시락 상자 + 이름표**. 상자 안 밥(픽셀 메모리)은 누가 먹고 누가 설거지(Dispose)하는지 정해야 한다. 설거지를 두 번 해도 깨지면 안 된다.

## 3. OmniSight 지도

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 장치 상태 | `OmniSight/Devices/DeviceContracts.cs:8-17` | Created → … → Disposed 7단계 |
| IDevice | `OmniSight/Devices/DeviceContracts.cs:19-26` | `IDisposable` 상속, Initialize/Start/Stop |
| IImageSourceDevice | `OmniSight/Devices/DeviceContracts.cs:31-38` | 미리보기 이벤트 + 검사용 `WaitFrameAsync` |
| 인터페이스 분리 | `OmniSight/Devices/DeviceContracts.cs:40-51` | ICameraDevice(표식), ICameraPreviewDevice |
| ImageFrame 소유권 | `OmniSight/Devices/DeviceContracts.cs:53-102` | `_owner`, `Interlocked.Exchange`로 한 번만 Dispose |
| 검사 계약들 | `OmniSight/Application/Inspection/InspectionContracts.cs:10-80` | IImagePreprocessor, IRuleInspectionEngine, IAiInspectionEngine, IInspectionResultReporter, IInspectionImageStore |
| 파이프라인은 인터페이스만 앎 | `OmniSight/Application/Inspection/InspectionPipeline.cs:23-27,44-52` | 생성자 주입 + null 검사 |

**집에서 Claude에게:**
```text
OmniSight/Devices/DeviceContracts.cs 전체와 InspectionPipeline.cs 23~52줄을 보여줘.
InspectionPipeline이 Basler 카메라인지 공유폴더인지 모르는데도 동작하는 이유를 콘센트 비유로 설명해줘.
그리고 ICameraDevice가 멤버 없이 비어 있는 이유도.
```

## 4. 왜 이렇게 썼나

### 4.1 SOLID를 OmniSight에서 찾기

| 원칙 | 한 줄 뜻 | OmniSight 예 |
|---|---|---|
| **S** 단일 책임 | 바뀌는 이유는 하나 | `InspectionPipeline`은 순서만 관리. 전처리/룰/AI/저장은 각자 클래스 |
| **O** 개방-폐쇄 | 확장엔 열리고 수정엔 닫힘 | 새 카메라 = 새 어댑터 + 팩토리 한 줄. 파이프라인은 수정 안 함 |
| **L** 리스코프 치환 | 자식은 부모 자리에 그대로 | 어떤 `IImageSourceDevice`든 `WaitFrameAsync`는 "프레임 또는 null(완료)" 약속을 지켜야 함 |
| **I** 인터페이스 분리 | 안 쓰는 메서드에 의존 안 함 | 미리보기 기능은 `ICameraPreviewDevice`로 분리. 공유폴더는 미리보기 없음 |
| **D** 의존 역전 | 구체 말고 추상에 의존 | 파이프라인 생성자가 인터페이스 5개를 받음 |

### 4.2 이벤트와 WaitFrameAsync를 왜 둘 다 두었나

- `FrameCaptured` 이벤트: **화면 미리보기용**. 여러 구독자가 볼 수 있지만, **핸들러 안에서만 유효**하다(핸들러가 끝나면 상자가 큐로 넘어감). 오래 붙잡으면 안 되고 필요하면 복사(Clone)한다.
- `WaitFrameAsync`: **검사용**. 한 명(파이프라인)만 받고, **받은 사람이 Dispose할 책임**이 생긴다.
- 두 용도를 하나로 합치면 "누가 설거지하나?"가 모호해진다. 그래서 갈랐다.

### 4.3 ImageFrame.Dispose가 Interlocked를 쓰는 이유

`Dispose()`가 정지 스레드와 작업자 스레드에서 **동시에** 불릴 수 있다. `if (_disposed) return; _disposed = true;`는 두 스레드가 동시에 if를 통과할 수 있다. `Interlocked.Exchange(ref _disposed, 1)`은 "바꾸면서 이전 값을 돌려주기"를 **한 번에** 하므로 딱 한 명만 0을 받는다. (Day09에서 깊게.)

### 4.4 약점

- `ImageFrame.Data`는 그냥 `IntPtr`이라 Dispose 후에도 다른 곳에 복사된 포인터 값은 살아 있다. 규칙(소유권 문서)으로 지키는 구조다.
- `ICameraDevice`처럼 **빈 표식 인터페이스(marker)**는 의미가 약해질 수 있다. "카메라 종류인지"를 `is`로 확인할 때만 쓴다.

## 5. Kata: 콘센트와 플러그 (DIP + ISP)

```csharp
// KATA: Day02
using System;
using System.Collections.Generic;

namespace Katas.Day02
{
    public interface IPowerSource { string Name { get; } int Watt { get; } }
    public interface IHasTimer { void SetTimer(int minutes); }   // ISP: 타이머 있는 기기만

    public sealed class Fan : IPowerSource, IHasTimer
    {
        public string Name { get { return "선풍기"; } }
        public int Watt { get { return 40; } }
        public void SetTimer(int minutes) { Console.WriteLine(Name + " 타이머 " + minutes + "분"); }
    }

    public sealed class HairDryer : IPowerSource
    {
        public string Name { get { return "드라이기"; } }
        public int Watt { get { return 1500; } }
    }

    // DIP: 멀티탭은 구체 클래스를 모르고 약속(IPowerSource)만 안다
    public sealed class PowerStrip
    {
        private readonly List<IPowerSource> _plugged = new List<IPowerSource>();
        private readonly int _maxWatt;
        public PowerStrip(int maxWatt) { _maxWatt = maxWatt; }

        public bool Plug(IPowerSource device)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));
            int total = device.Watt;
            foreach (IPowerSource d in _plugged) total += d.Watt;
            if (total > _maxWatt) { Console.WriteLine(device.Name + " 과부하! 거부"); return false; }
            _plugged.Add(device);
            Console.WriteLine(device.Name + " 연결 (합계 " + total + "W)");
            return true;
        }

        public void SetAllTimers(int minutes)
        {
            foreach (IPowerSource d in _plugged)
            {
                var timer = d as IHasTimer;          // 기능이 있는 기기만 골라 쓴다
                if (timer != null) timer.SetTimer(minutes);
            }
        }
    }

    // 소유권 연습: 두 번 Dispose해도 안전한 상자
    public sealed class LunchBox : IDisposable
    {
        private int _disposed;
        public void Dispose()
        {
            if (System.Threading.Interlocked.Exchange(ref _disposed, 1) != 0) return;
            Console.WriteLine("설거지 완료 (딱 한 번)");
        }
    }

    public static class KataDay02
    {
        public static void Run()
        {
            var strip = new PowerStrip(1600);
            strip.Plug(new Fan());
            strip.Plug(new HairDryer());   // 40 + 1500 = 1540 OK
            strip.Plug(new Fan());         // 1580 OK
            strip.Plug(new HairDryer());   // 과부하
            strip.SetAllTimers(30);

            var box = new LunchBox();
            box.Dispose();
            box.Dispose();                  // 아무 일 없음
        }
    }
}
```

생각해 볼 것: `PowerStrip`에 `if (device is Fan)` 같은 분기를 넣고 싶어지면 그건 DIP 위반 신호다.

## 6. MiniSight 오늘 단계

1. NuGet: `OpenCvSharp4`, `OpenCvSharp4.runtime.win`, `OpenCvSharp4.Extensions` 설치 (MiniSight 프로젝트).
2. `Devices/Contracts.cs`에 [설계서 4장](../01-MiniSight-Spec.md) 코드를 타이핑.
3. 같은 파일에 `ImageFrame`을 직접 작성. 요구사항:
   - 생성자: `ImageFrame(long frameId, string sourceName, Mat image)` — **image의 소유권을 넘겨받는다**.
   - 속성: `FrameId`, `SourceName`, `Timestamp`(DateTime.Now), `Image`(Mat, Dispose 후 null).
   - `Dispose()`는 `Interlocked.Exchange`로 한 번만, `Image`를 Dispose하고 null로.
4. 콘솔 확인 코드(Katas 프로젝트에서 MiniSight 참조 또는 임시로 Form1_Load):

```csharp
var frame = new ImageFrame(1, "Cam1", new Mat(100, 200, MatType.CV_8UC1, Scalar.All(0)));
Console.WriteLine(frame.Image.Width);   // 200
frame.Dispose();
frame.Dispose();                         // 예외 없어야 함
Console.WriteLine(frame.Image == null);  // True
```

## 7. VS2022 디버깅 실습

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | `frame.Dispose()` 첫 줄에 중단점 | |
| 2 | 디버그 → 창 → **지역(Locals)**, **자동(Autos)** | Autos는 "지금 줄 근처 변수"만, Locals는 현재 메서드 전부 |
| 3 | **조사식(Watch) 1**에 `frame.Image.IsDisposed`, `frame.Image.Width` 입력 | Dispose 전/후 값 변화 |
| 4 | Dispose 안에서 `_disposed` 값을 Watch로 보며 F10 | 0 → 1 |
| 5 | 두 번째 Dispose 호출에서 F11 → `return`으로 바로 빠지는지 | |
| 6 | Watch에서 `frame.Image` 오른쪽 돋보기 → **Text 시각화 도우미** | (Mat은 시각화가 없다. Day15에서 Cv2.ImWrite 요령) |

## 8. 셀프 체크

1. `InspectionPipeline`이 구체 카메라 클래스를 모르면 무엇이 좋아지나? (2가지)
<details><summary>답</summary>① 새 카메라/입력 방식을 추가해도 파이프라인을 수정하지 않는다(개방-폐쇄). ② 가짜 입력(시뮬레이션, 테스트 더블)을 넣어 하드웨어 없이 테스트할 수 있다.</details>

2. `FrameCaptured` 이벤트로 받은 프레임을 다른 스레드에 넘겨 나중에 쓰면 어떤 문제가 생기나?
<details><summary>답</summary>이벤트가 끝나면 그 프레임은 큐로 넘어가 파이프라인이 처리 후 Dispose한다. 나중에 접근하면 이미 해제된 메모리(또는 다른 프레임으로 재사용된 SDK 버퍼)를 읽게 된다. 오래 써야 하면 핸들러 안에서 Clone한다.</details>

3. ISP를 어긴 인터페이스의 증상은?
<details><summary>답</summary>구현 클래스가 쓸모없는 메서드를 `throw new NotSupportedException()`으로 채우거나 빈 몸통으로 둔다. 호출하는 쪽은 그 메서드가 동작하는지 믿을 수 없다.</details>

4. `if (_disposed) return; _disposed = true;`가 멀티스레드에서 위험한 이유는?
<details><summary>답</summary>검사와 대입 사이에 다른 스레드가 끼어들 수 있다. 두 스레드가 동시에 false를 읽으면 둘 다 해제 코드를 실행해 이중 해제가 된다. 원자적 연산(Interlocked.Exchange)이나 lock이 필요하다.</details>

5. 생성자에서 `?? throw new ArgumentNullException(...)`을 하는 이유는?
<details><summary>답</summary>잘못된 조립을 **만드는 순간** 발견하기 위해서다. 안 하면 운전 중 첫 프레임에서 NullReferenceException이 나고 원인 추적이 어렵다(Fail fast).</details>

## 9. 블로그 포인트

- 제목 예: `[설계] 영상 검사기가 카메라 종류를 몰라도 되는 이유 (DIP와 ISP)`
- 콘센트·리모컨 비유 + PowerStrip Kata + MiniSight ImageFrame 소유권 규칙.
