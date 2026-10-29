# Day 26 (금, 2h) · 증명하기: 가짜 장치 테스트, 골든 이미지, MSTest

## 1. 오늘 목표

- **완료 기준**
  - "하드웨어 없이 장비 코드를 테스트하는 법"을 설명할 수 있다: 인터페이스 + 가짜 구현(Fake) + `Func<T>` 설정 주입 + 시간·스레드 제어.
  - OmniSight 스모크 테스트 구조(독립 실행 콘솔, `Check()`, 성공 증거 파일, PowerShell 실행기)를 설명할 수 있다.
  - **골든 테스트**(알려진 입력 → 저장된 기대 출력과 비교)로 "리팩토링해도 결과가 같다"를 증명하는 법을 말할 수 있다.
  - MiniSight.Tests(MSTest, .NET Framework 4.7.2)에 테스트 10개 이상: 큐, 파이프라인 drain, 시작 롤백, 프레이밍, 원자적 저장, 게이지 정확도, 필터 골든.
  - Test Explorer에서 테스트를 **디버그 실행**하고 실패를 고쳤다.
- **최소분량**: 3, 4, 6.1~6.3
- **확장분량**: 6.4 골든, 6.5 BDD 시나리오

## 2. 🧒 초1 비유

- **가짜 장치 = 연습용 모형 자동차**. 진짜 차(카메라) 없이도 운전 연습(코드 시험)을 할 수 있다. 모형은 "언제 고장 날지"를 내가 정할 수 있어서 오히려 더 좋다.
- **골든 테스트 = 정답 사진과 비교**. 오늘 그린 그림을 어제 저장한 정답 그림과 한 픽셀씩 비교한다. 다르면 "뭔가 바뀌었다".
- **테스트 = 내일의 나를 위한 안전벨트**. 지금 고친 게 어제 고친 걸 망가뜨리지 않았는지 1초 만에 알려준다.

## 3. OmniSight 지도

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 스모크 테스트 예 | `OmniSight/Tests/ProcessingOrderSmokeTests.cs:1-40` | 앱 프로젝트에서 제외, `Check(bool, msg)`, 리플렉션으로 private 접근 |
| 카메라 수명 테스트 | `OmniSight/Tests/CameraAcceptanceSmokeTests.cs`, `CameraTransformSmokeTests.cs` | 가짜 `ICameraDriver` 주입 |
| 영상 비교 | `OmniSight/Tests/SobelDilationImageComparison.cs` | 필터 결과 비교 |
| 실행기 | `OmniSight/Tests/RunCleanupRegressionTests.ps1` | MSBuild → csc로 테스트 컴파일 → 실행 → 증거 파일 확인 |
| 독립 테스트 프로젝트 | `OmniSight/Tests/Standalone/LegacyOcrDecoderChecks/`, `RuntimeLifecycleChecks/` | 소스 파일을 직접 링크해서 검사 |
| 기준 데이터 | `OmniSight/Tests/Fixtures/Baselines/` | 골든 데이터 압축본 |
| 문서 | `OmniSight/docs/onboarding/14-TestingReleaseAndRollback.ko.md` | 테스트·배포·롤백 |
| 스킬 | `.claude/skills/bdd-dotnet/SKILL.md`, `.claude/skills/legacy-code-refactoring/SKILL.md` | Given/When/Then, 골든 테스트 절차 |

**집에서 Claude에게:**
```text
OmniSight Tests 폴더 구조와 RunCleanupRegressionTests.ps1이 하는 일을 순서대로 설명해줘.
왜 MSTest/xUnit 같은 프레임워크 대신 독립 콘솔 + 증거 파일 방식을 썼을지 추측하고 장단점을 정리해줘.
CameraAcceptanceSmokeTests.cs에서 가짜 드라이버를 어떻게 만들었는지 구조만 보여줘.
```

## 4. 왜 이렇게 (테스트 설계 원칙)

| 원칙 | 장비 코드에서의 의미 |
|---|---|
| 인터페이스 경계에 Fake | `ICameraDriver`, `IImageSource`, `IResultReporter`, `IAiEngine`. 1주차 설계가 여기서 보상받는다 |
| 시간을 주입 | `Stopwatch.GetTimestamp()`를 직접 부르는 `LineSpeedAcquisitionGate.ShouldAcquire(speed, fov, timestamp)`처럼 **타임스탬프를 인자로** 받으면 테스트가 결정적 |
| 동시성 테스트는 "반드시 끝나야" | 모든 Wait에 타임아웃. 테스트가 멈추면 실패로 |
| 골든 데이터 | 이미지·CSV·패킷 바이트를 파일로 고정. 바꿀 땐 **의도적으로** 갱신하고 리뷰 |
| 실패 경로를 테스트 | 정상보다 Start 실패 롤백, Stop 실패 Faulted, 큐 포화, 파일 손상 복구가 더 중요 |
| 회사 방식(독립 콘솔)의 이유 추정 | 무거운 네이티브 의존(카메라 SDK, AI DLL, x64)과 WinForms가 섞여 테스트 프레임워크 설정이 번거로움, 현장 PC에서도 exe 하나로 돌릴 수 있음. 대신 발견·보고 도구가 약함 |

## 5. Kata: 결정적 시간 테스트

```csharp
// KATA: Day26
using System;

namespace Katas.Day26
{
    // 시간을 인자로 받는 순수 판단 객체 → 테스트가 쉽다
    public sealed class EveryNMillimetersGate
    {
        private double _carriedMm;
        private long _lastTicks;
        private readonly long _ticksPerSecond;
        public EveryNMillimetersGate(long ticksPerSecond) { _ticksPerSecond = ticksPerSecond; }

        public bool ShouldTake(double speedMetersPerMinute, double fovMm, long nowTicks)
        {
            if (speedMetersPerMinute <= 0 || fovMm <= 0) { _lastTicks = 0; _carriedMm = 0; return false; }
            if (_lastTicks == 0) { _lastTicks = nowTicks; return true; }
            double seconds = (double)(nowTicks - _lastTicks) / _ticksPerSecond;
            _lastTicks = nowTicks;
            _carriedMm += speedMetersPerMinute * 1000.0 / 60.0 * seconds;
            if (_carriedMm < fovMm) return false;
            _carriedMm -= fovMm;              // 남은 거리는 다음으로
            return true;
        }
    }

    public static class KataDay26
    {
        private static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException("FAIL: " + message); }

        public static void Run()
        {
            // 60 m/min = 1000 mm/s, FOV 100mm → 100ms마다 1장
            var gate = new EveryNMillimetersGate(1000);       // 1 tick = 1ms 로 가정
            int taken = 0;
            for (long t = 1; t <= 1001; t += 10) if (gate.ShouldTake(60, 100, t)) taken++;
            Check(taken == 11, "1초에 첫 장 + 10장 기대, 실제 " + taken);

            Check(!gate.ShouldTake(0, 100, 2000), "속도 0이면 안 찍음");
            Check(gate.ShouldTake(60, 100, 2010), "속도 복귀 후 첫 장은 찍음");
            Console.WriteLine("모든 검사 통과 (실제 시간을 기다리지 않고 1초 운전을 시험)");
        }
    }
}
```

OmniSight의 `LineSpeedAcquisitionGate`가 같은 아이디어다: "시각을 인자로 받는 작은 판단 객체" → 하드웨어·시간 없이 검증.

## 6. MiniSight 오늘 단계

### 6.1 프로젝트
- 솔루션에 **MSTest 테스트 프로젝트(.NET Framework)** 추가, 4.7.2, x64, MiniSight 참조, OpenCvSharp4 NuGet.
- 테스트 → 테스트 설정 → 프로세서 아키텍처 **x64** (OpenCV 네이티브 때문).

### 6.2 단위 테스트 (최소 8개)
| 테스트 | 검증 |
|---|---|
| `FrameQueue_Full_ProducerWaits_NoLoss` | 용량 3, 10개 넣고 다 꺼냄, ProducerWaitCount > 0 |
| `FrameQueue_Complete_DrainsThenNull` | Complete 후 남은 것 다 받고 null |
| `ImageFrame_DisposeTwice_Safe` | 병렬 8회 Dispose |
| `DeviceManager_StartFailure_RollsBackStarted` | 2번째 실패 → 1번째 Stop 호출됨, 3번째 Start 안 됨 |
| `DeviceBase_StopFailure_IsFaulted` | Stop 예외 → Faulted |
| `FrameParser_RandomChunks_AllFrames` | Day22 Kata를 시드 10개로 |
| `JsonStore_CorruptFile_LoadsBackup` | Day23 |
| `PointGauge_SyntheticImages_Within1Px` | Day15 이미지 20장 |

### 6.3 통합 테스트 (2개 이상)
- `Pipeline_DrainStop_ProcessesAllFrames`: FakeCamera 50장 → Start → `CompleteAcquisition` → Stop → 리포터가 50건 받았는지. **전체 타임아웃 10초**.
- `Pipeline_SlowReporter_StopWithinTimeout`: 리포터 Sleep 2초 + 보고 큐 2 → Stop이 상한 안에 끝나고 폐기 수가 기록되는지 (Day25 개선 검증).

### 6.4 골든 테스트 (확장)
- `FilterChain_Golden`: 고정 입력 이미지 + 레시피 → 결과 PNG를 `Golden/filter_001.png`와 `Cv2.Compare` 또는 `Cv2.Norm(a, b, NormTypes.L1) == 0`.
- 골든 갱신은 환경 변수 `MINISIGHT_UPDATE_GOLDEN=1`일 때만 (실수로 덮어쓰기 방지).

### 6.5 BDD 시나리오 (확장)
```text
Given 카메라 2대와 느린 보고기(2초)가 있고
When 운전 중 Stop을 누르면
Then UI는 멈추지 않고
And 13초 안에 정지가 끝나며
And 폐기된 프레임 수가 로그에 남는다
```
이 문장을 테스트 메서드 이름과 주석으로 옮긴다(`bdd-dotnet` 스킬 참고).

## 7. VS2022 디버깅 실습

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | 테스트 → **테스트 탐색기(Ctrl+E, T)** → 모두 실행 | 결과 트리 |
| 2 | 실패한 테스트 우클릭 → **디버그** | 테스트 안 중단점에서 멈춤. 예외 위치로 바로 |
| 3 | 통합 테스트 실행 중 모두 중단 → 병렬 스택 | 테스트가 멈췄을 때 원인 찾기 |
| 4 | 테스트 → **라이브 단위 테스트**(Enterprise만) 또는 "빌드 후 테스트 실행" 옵션 | 자동 회귀 |
| 5 | (확장) 코드 커버리지(Enterprise) 또는 `coverlet` 콘솔 | 안 덮인 실패 경로 찾기 |

## 8. 셀프 체크

1. 하드웨어 의존 코드를 테스트 가능하게 만드는 설계 3가지는?
<details><summary>답</summary>① SDK 호출을 인터페이스(ICameraDriver) 뒤로 숨기고 생성은 Func 팩토리로 주입. ② 시간·설정을 인자나 Func로 받아 결정적으로. ③ 판단 로직을 SDK 콜백에서 분리한 작은 순수 객체로(LineSpeedAcquisitionGate).</details>

2. 동시성 테스트가 "가끔 실패"할 때 의심할 것은?
<details><summary>답</summary>테스트 자체의 타이밍 가정(Sleep으로 기다림), 실제 race condition, 공유 정적 상태(싱글톤) 오염, 타임아웃이 너무 짧음. Sleep 대신 신호(ManualResetEvent, TCS)로 동기화하고 반복 실행(100회)으로 재현한다.</details>

3. 골든 테스트의 장점과 함정은?
<details><summary>답</summary>장점: 복잡한 출력(이미지, 바이트)을 한 번에 고정해 리팩토링 안전망이 된다. 함정: 기대값 자체가 틀렸어도 통과하고, 의도된 변경 때 갱신을 습관적으로 하면 의미가 없어진다. 갱신은 명시적 절차와 리뷰로.</details>

4. OmniSight가 테스트 프레임워크 대신 독립 콘솔을 쓴 이유를 추측하면?
<details><summary>답</summary>x64 네이티브 DLL, 카메라 SDK, WinForms가 얽혀 테스트 러너 설정이 까다롭고, 현장 PC에서 exe만으로 회귀 확인을 하고 싶었을 것이다. 대신 결과 보고·필터링·IDE 통합이 약하다.</details>

5. Start 실패 롤백 테스트에서 무엇을 확인해야 하나?
<details><summary>답</summary>실패한 장치까지 포함해 이미 시작 시도한 장치가 역순으로 Stop되었는지, 시도하지 않은 장치는 Start되지 않았는지, 원래 예외가 호출자에게 전달되는지, 시스템 상태가 Ready/Stop(또는 정책에 따른 상태)으로 돌아왔는지.</details>

## 9. 블로그 포인트

- 제목 예: `[테스트] 카메라 없이 영상 검사 프로그램 테스트하기: Fake 드라이버와 골든 이미지`
- 테스트 목록 표, Test Explorer 캡처, 시간 주입 Kata.
