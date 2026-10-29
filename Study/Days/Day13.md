# Day 13 (토, 4h) · 컨베이어 벨트: 검사 파이프라인 통합

## 1. 오늘 목표

- **완료 기준**
  - 카메라 콜백 → 큐 → 파이프라인 → 출력 큐 → 저장까지 **스레드 수, 큐 수, 각 큐 용량, 꽉 찼을 때 동작**을 말로 설명할 수 있다 (2주차 졸업 시험).
  - Start 순서(**소비자 먼저**: 출력 → 파이프라인 → 카메라)와 Stop 순서(**생산자 먼저**: 취득 완료 → 파이프라인 drain → 출력 → 장치)를 이유와 함께 말할 수 있다.
  - Drain 정지와 Cancel 정지의 차이를 설명할 수 있다.
  - MiniSight: 카메라 2대 × 파이프라인 2개가 돌고, CSV·PNG가 저장되고, Stop 시 남은 프레임이 다 처리된다.
  - 일부러 Mat 누수를 만들고 **메모리 스냅샷 비교**로 찾았다.
- **최소분량**: 3, 4, 6.1~6.4
- **확장분량**: 6.5 누수 실험, 7번 전체

## 2. 🧒 초1 비유

**공장 컨베이어 벨트**.

- 카메라마다 **전용 벨트 하나와 작업자 한 명**(InspectionPipeline). 그래서 같은 카메라의 사진은 순서가 안 바뀐다. 카메라가 2대면 벨트 2개, 작업자 2명이 **동시에** 일한다.
- 작업자는 사진을 받아(WaitFrameAsync) → 다듬고(전처리) → 자로 재고(룰) → 필요하면 AI에게 물어보고 → 결과표를 출력 창구(OutputDispatcher)에 낸다.
- **퇴근(Stop)은 순서가 있다**: ① 사진 찍기를 멈춘다(입구 닫기) → ② 벨트 위에 남은 사진을 작업자가 끝까지 처리한다(drain) → ③ 출력 창구가 남은 결과표를 다 보낸다 → ④ 기계 전원을 끈다.
- **출근(Start)은 반대**: 출력 창구 직원이 먼저 앉고 → 작업자 → 마지막에 카메라 켜기. 거꾸로 하면 사진이 왔는데 받을 사람이 없다.

## 3. OmniSight 지도

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 파이프라인 설명 | `OmniSight/Application/Inspection/InspectionPipeline.cs:12-17` | 입력 1개 = 작업자 1개 = 순서 보장 (주석의 "병렬"은 실제와 다름 → 4.4) |
| 작업자 시작 | `OmniSight/Application/Inspection/InspectionPipeline.cs:70-100` | 이미 실행 중이면 무시, 통계 초기화, LongRunning |
| 작업 루프 | `OmniSight/Application/Inspection/InspectionPipeline.cs:188-241` | null이면 drain 완료 → break, 예외는 기록 후 **계속**, finally `frame?.Dispose()` |
| 한 장 처리 | `OmniSight/Application/Inspection/InspectionPipeline.cs:246-339` | `using (original)` 래퍼, 전처리 → 룰 → (조건부) AI → 마킹 → Publish, finally `processed?.Dispose()` |
| 발행 | `OmniSight/Application/Inspection/InspectionPipeline.cs:341-358` | `_acceptResults`가 false면 버림(취소 정지) |
| 파이프라인 정지 | `OmniSight/Application/Inspection/InspectionPipeline.cs:122-183` | drain이면 Cancel 안 함, `Wait(10000)` 후 무제한 대기 |
| 런타임 생성 | `OmniSight/Application/Inspection/InspectionRuntime.cs:42-53` | 입력 이름 중복 검사, 입력마다 파이프라인 |
| 런타임 시작 | `OmniSight/Application/Inspection/InspectionRuntime.cs:124-171` | **출력 먼저 Start**, 파이프라인 실패 시 역순 롤백 |
| 런타임 정지 | `OmniSight/Application/Inspection/InspectionRuntime.cs:193-222` | 파이프라인 역순 → 출력 마지막 |
| 시스템 정지 | `OmniSight/Application/VisionSystem.cs:430-493` | `CompleteAcquisition` → `_inspection.Stop` → `_devices.Stop` |
| 문서 | `OmniSight/docs/reference/OMNISIGHT_ARCHITECTURE.ko.md` §3 | 영상 한 장의 흐름 |

**집에서 Claude에게:**
```text
VisionSystem.StopInternal(430~493)부터 시작해서 CompleteAcquisition → InspectionPipeline.Stop(122~183)
→ RunAsync(188~241)가 null을 받고 끝나는 과정 → InspectionRuntime.Stop(193~222) → OutputDispatcher.Stop
순서로 "마지막 사진 한 장"이 어떻게 끝까지 저장되는지 따라가 줘.
그리고 drain 정지와 cancel 정지가 코드에서 어디서 갈리는지 알려줘.
```

## 4. 왜 이렇게 썼나

### 4.1 전체 그림 (졸업 시험 정답지. 외우지 말고 이해하고 그릴 것)

```text
[SDK 콜백 스레드] ──Copy→Mat──▶ ImageFrame ──FrameCaptured(미리보기, 10fps 제한)──▶ UI
        │
        └─Add (가득 차면 콜백 대기)──▶ [CameraFrameQueue 32]  ※ 시뮬/공유폴더는 3~5
                                            │ TakeAsync
                                            ▼
                              [파이프라인 작업자 (카메라당 1)]
                               전처리 → 룰(Task.Run) → AI(Task.Run, 조건부) → 마킹
                                            │ Publish
                     ┌──────────────────────┴───────────────────────┐
             Add (대기)                                    Clone → Add (대기)
         [보고 큐 64]                                       [이미지 큐 4]
               │                                                 │
     [보고 작업자 1] → CSV/PLC 송신 큐                    [이미지 작업자 1] → 파일 저장
```

카메라 2대라면: SDK 콜백 2 + 파이프라인 2 + 보고 1 + 이미지 1 + (룰/AI용 스레드풀) + 로그 1 + 감시들.

### 4.2 Drain vs Cancel

| | Drain 정지 (정상) | Cancel 정지 (비상/장치 정지) |
|---|---|---|
| 언제 | 사용자/PLC의 정상 Stop | 취득 완료를 못 했거나 장치 오류 |
| 큐에 남은 프레임 | **끝까지 검사·보고·저장** | 버림 (Dispose) + "정지 시 폐기" 카운트 |
| 파이프라인 토큰 | 취소 안 함 → `TakeAsync`가 null 줄 때까지 | 즉시 취소 |
| `_acceptResults` | true 유지 | false → 처리 중 결과도 발행 안 함 |
| 장점 | LOT의 마지막 제품까지 기록 | 빠르게 멈춤 |
| 위험 | 처리가 막혀 있으면 정지가 오래 걸림 | 기록 누락 |

### 4.3 결정과 이유

| 결정 | 이유 |
|---|---|
| 입력별 작업자 1개 | 같은 카메라 프레임 순서 보장(PLC 트리거 번호와 결과를 맞춰야 함). 카메라끼리는 독립 병렬 |
| 프레임 루프에서 예외는 기록 후 계속 | 이미지 한 장이 깨졌다고 라인 전체 검사가 멈추면 안 됨. 대신 **알람 키로 1분 1회 제한 로깅** |
| 오류도 결과로 발행 | 실패한 프레임도 CSV/PLC에 "에러"로 남겨야 전수 기록이 맞음 |
| `Mat.FromPixelData` 래퍼 (복사 없음) | 원본은 ImageFrame이 소유. 래퍼는 같은 메모리를 보는 창문일 뿐. `using`으로 래퍼만 해제 |
| 전처리 결과는 새 Mat(ProcessedImage) | 원본 보존(저장용), 룰/AI가 같은 전처리 영상을 **읽기만** |
| 출력 Start 먼저, Stop 나중 | 소비자가 준비되기 전 생산 금지, 생산자가 다 끝난 뒤 소비자 정리 |

### 4.4 시니어의 눈

- 주석(L14)은 "룰 + AI 병렬"이지만 코드(L276-289)는 **룰 먼저 → 결과로 AI 실행 여부 결정 → AI**의 순차다. `Task.Run`을 썼지만 바로 await 하므로 병렬이 아니다. **주석과 코드가 다르면 코드가 진실**. 이런 불일치를 찾아 보고하는 게 시니어의 일.
- `Pipeline.Stop`의 `worker.Wait()` 무제한 대기 + UI 스레드에서 호출되는 `VisionSystem.Stop` + `_sync` lock 보유 = 화면 멈춤 가능성 (Day14에서 재현, Day25에서 개선).
- 정지 순서가 맞아도 보고 큐가 꽉 차 있고 보고 작업자가 디스크 장애로 멈춰 있으면 drain이 끝나지 않는다. **모든 대기에는 상한과 알람**이 있어야 한다.

## 5. Kata (짧게): Start/Stop 순서 감각

```csharp
// KATA: Day13
using System;
using System.Collections.Generic;

namespace Katas.Day13
{
    public static class KataDay13
    {
        public static void Run()
        {
            var parts = new List<string> { "출력창구", "작업자1", "작업자2", "카메라1", "카메라2" };
            Console.WriteLine("Start: " + string.Join(" → ", parts));        // 소비자 → 생산자
            var reversed = new List<string>(parts);
            reversed.Reverse();
            Console.WriteLine("Stop : " + string.Join(" → ", reversed));      // 생산자 → 소비자

            // 퀴즈: OmniSight의 실제 Stop은 이 단순 역순과 무엇이 다른가?
            Console.WriteLine("힌트: 카메라를 '정지'하기 전에 '취득 완료(drain 신호)'를 먼저 보낸다. 장치 Stop은 맨 마지막.");
        }
    }
}
```

## 6. MiniSight 오늘 단계 (4시간)

### 6.1 계약 추가 (`Inspection/Contracts.cs`)
```csharp
public sealed class ProcessedImage : IDisposable { public Mat Image { get; } /* 소유, Dispose once */ }
public interface IPreprocessor { ProcessedImage Process(ImageFrame frame, CancellationToken ct); }
public interface IRuleEngine { RuleResult Inspect(ProcessedImage image, CancellationToken ct); }   // RuleResult: Ok, Message, ShouldRunAi
public interface IAiEngine : IDisposable { AiResult Inspect(ProcessedImage image, CancellationToken ct); }
public interface IInspectionController : IDisposable { void Start(); void Stop(); }
```
오늘은 구현을 간단히: `GrayPreprocessor`(BGR→Gray Clone), `MeanBrightnessRule`(평균 밝기 ≥ 임계값이면 OK, ShouldRunAi=true), `FakeAiEngine`(10ms Sleep 후 OK). Day16~18에서 진짜로 교체.

### 6.2 `InspectionPipeline : IInspectionController`
- 생성자에서 5개 의존성 null 검사.
- `Start()`: lock, 이미 실행 중이면 return, CTS, `_acceptResults = true`, 작업자 시작.
- `RunAsync(ct)`: `WaitFrameAsync` → null이면 drain 완료 확인 후 break → `ProcessFrame` → 예외는 Failed++ 후 계속 → finally `frame?.Dispose()`.
- `ProcessFrame`: 전처리 → 룰 → (ShouldRunAi면) AI → `InspectionResult` 생성 → `_output.Publish(result, processed.Image, ct)` → `InspectionCompleted` 이벤트 → finally `processed?.Dispose()`.
- `Stop()`: 소스가 drain 완료면 Cancel 없이 대기, 아니면 `_acceptResults=false` + Cancel. `Wait(10000)` 실패 시 **로그 남기고 무제한 대기 대신 예외**로 (OmniSight와 다르게 해 보기: 장단점은 Day25에서 토론).

### 6.3 `InspectionRuntime : IInspectionController`
- 입력 이름 중복이면 생성자에서 예외.
- `Start`: 출력 Start → 파이프라인 순서대로, 실패 시 역순 Stop + 출력 Stop.
- `Stop`: 파이프라인 역순 → 출력. 실패는 모아서 AggregateException.
- 이벤트 중계: 파이프라인 `InspectionCompleted` → 런타임 이벤트.

### 6.4 VisionSystem 연결
- Bootstrapper가 `new InspectionRuntime(cameras, pre, rule, ai, new OutputDispatcher(new CsvReporter(...), new PngImageStore(...)))` 생성 → `new VisionSystem(devices, runtime)`.
- `VisionSystem.Start`: 장치 검증 → `_devices.Start` → `_inspection.Start` (OmniSight 순서 그대로. **카메라 Start가 파이프라인보다 먼저인 이유를 생각**: 아키텍처 문서 §2 "시작 직후 프레임이 큐에 쌓일 수 있음")
- `VisionSystem.Stop`: drain 가능한 카메라 `CompleteAcquisition` → `_inspection.Stop` → `_devices.Stop`.
- 콘솔 데모: Start → 5초 → Stop. 로그에 "처리=N, 실패=0", CSV 줄 수 = 처리 수, PNG 생성 확인.

### 6.5 누수 실험 (확장)
- `InspectionPipeline.RunAsync`의 `finally { frame?.Dispose(); }`를 **주석 처리**하고 30초 운전.
- HealthMonitor의 PrivateMemory가 계속 오르는 것 확인 → 7번 스냅샷 비교 → 원복.

## 7. VS2022 디버깅 실습: 메모리 스냅샷으로 누수 찾기

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | 디버그 → **성능 프로파일러(Alt+F2)** 가 아니라, 먼저 디버깅 중 **진단 도구** → 메모리 사용량 탭 → **스냅숏 만들기** (Start 직후) | 스냅샷 #1 |
| 2 | 20초 후 스냅숏 #2 | 개체 수 차이(Objects Diff), 힙 크기 차이 |
| 3 | #2의 "개체(차이)" 클릭 → `OpenCvSharp.Mat` 개수 증가 확인 | 관리 객체 Mat는 작지만 **네이티브 메모리**를 붙잡고 있음 → PrivateMemory 급증의 원인 |
| 4 | Mat 항목 → **루트 경로(Paths to Root)** | 누가 잡고 있나? (finalizer 큐, 또는 아무도 → GC 대기) |
| 5 | 주석 원복 후 같은 실험 | Mat 개수가 일정 |
| 6 | 운전 중 모두 중단 → **병렬 스택** | 파이프라인 2개가 `TakeAsync` 대기, 보고·이미지 작업자가 `Take` 대기 |
| 7 | 이미지 저장에 `Sleep(300)` 추가 → 병렬 스택 | 파이프라인이 `BlockingCollection.Add`에서 대기 = backpressure를 눈으로 |

## 8. 셀프 체크

1. 파이프라인을 카메라당 1개로 한 이유와 단점은?
<details><summary>답</summary>이유: 같은 입력의 프레임 순서 보장, 입력 간 독립(한 카메라 지연이 다른 카메라에 영향 최소화), 구조 단순. 단점: 한 입력의 처리량은 작업자 1개 속도로 제한된다. 더 빨라야 하면 순서를 복원하는 재정렬 버퍼가 필요하다.</details>

2. Start 시 출력 디스패처를 파이프라인보다 먼저 시작하는 이유는?
<details><summary>답</summary>파이프라인이 첫 결과를 Publish할 때 소비자가 준비되어 있어야 한다. 출력이 시작 전이면 Publish가 무시되거나(OmniSight는 `_started` false면 return) 결과가 사라진다.</details>

3. drain 정지에서 파이프라인 토큰을 취소하지 않는 이유는?
<details><summary>답</summary>취소하면 처리 중이거나 큐에 남은 프레임을 버리게 된다. 취득을 먼저 완료(Complete)해 새 입력을 막고, 소비자는 null을 받을 때까지 남은 것을 처리한 뒤 스스로 종료한다.</details>

4. 프레임 처리 중 예외가 나도 루프를 계속하는 이유와 필요한 보완책은?
<details><summary>답</summary>단일 프레임 결함으로 라인 전체 검사가 멈추면 생산 손실이 크다. 대신 실패 카운트, 에러 결과 발행, 알람(스로틀된 로그)으로 반드시 드러내야 하고, 연속 실패 시 정지하는 정책도 고려한다.</details>

5. `Mat.FromPixelData` 래퍼를 Dispose하면 원본 메모리도 해제되나?
<details><summary>답</summary>아니다. 래퍼는 외부 버퍼를 가리키기만 하고 소유하지 않는다. Dispose는 래퍼 헤더만 해제한다. 원본은 소유자(ImageFrame의 owner Mat)가 해제한다. 그래서 래퍼보다 원본이 오래 살아야 한다.</details>

## 9. 블로그 포인트

- 제목 예: `[C# 멀티스레드] 영상 검사 파이프라인의 Start/Stop 순서와 Drain 정지`
- 4.1 그림을 MiniSight 이름으로 다시 그린 것 + 메모리 스냅샷으로 Mat 누수 찾은 과정(스크린샷).
