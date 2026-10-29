# Day 10 (수, 2h) · 줄 서기 1: Producer/Consumer와 BlockingCollection

## 1. 오늘 목표

- **완료 기준**
  - Producer/Consumer를 그림으로 그리고 **유한 큐(bounded)**가 왜 필요한지(메모리 무한 증가 방지, backpressure) 설명할 수 있다.
  - "큐가 가득 차면?" 정책 3가지(생산자 대기 / 새 것 버림 / 옛 것 버림)를 장단점과 함께 말할 수 있다.
  - OmniSight 출력 디스패처의 **보고 큐 64, 이미지 큐 4**와 각각의 포화 정책을 설명할 수 있다.
  - MiniSight: `OutputDispatcher`(보고·이미지 큐 + 전용 작업자 2개), `CsvReporter`, `PngImageStore` 작성.
- **최소분량**: 2, 3, 4, 5, 6.1~6.2
- **확장분량**: 6.3 Stop 시 남은 큐 처리, 7번 Parallel Watch

## 2. 🧒 초1 비유

**빵집**. 제빵사(생산자, 검사 스레드)가 빵(검사 결과)을 굽고 진열대(큐)에 올린다. 포장 직원(소비자, 보고 작업자)이 진열대에서 하나씩 가져가 포장(CSV 쓰기)한다.

- 진열대가 **무한**이면? 포장이 느릴 때 빵이 끝없이 쌓여 가게가 터진다(메모리 부족).
- 진열대 칸이 **64개**로 정해져 있으면? 가득 찰 때 제빵사가 **잠깐 기다린다**(backpressure). 빵은 하나도 버리지 않는다.
- 사진 액자(이미지 저장)는 크고 무거워서 진열대가 **4칸**뿐이다.

## 3. OmniSight 지도

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 큐 생성 | `OmniSight/Application/Inspection/InspectionOutputDispatcher.cs:63-64` | `new BlockingCollection<T>(capacity)` |
| 기본 용량 | `OmniSight/Application/Inspection/InspectionOutputDispatcher.cs:18` 부근 | `DefaultReportQueueCapacity` 상수 |
| 작업자 2개 | `OmniSight/Application/Inspection/InspectionOutputDispatcher.cs:67-89` | LongRunning, 이전 정지 정리가 안 끝나면 시작 거부(L76-79) |
| 생산자 쪽 | `OmniSight/Application/Inspection/InspectionOutputDispatcher.cs:91-163` | 보고 먼저 `Add`(대기), 이미지 **Clone** 후 `Add`, 실패 시 request Dispose |
| 보고 소비자 | `OmniSight/Application/Inspection/InspectionOutputDispatcher.cs:228-256` | `TryTake(out, 1000, ct)` — 1초 비면 유휴 파일 닫기 |
| 이미지 소비자 | `OmniSight/Application/Inspection/InspectionOutputDispatcher.cs:258-286` | `Take(ct)`, finally에서 `request?.Dispose()` |
| 정지 | `OmniSight/Application/Inspection/InspectionOutputDispatcher.cs:165-201` | Cancel → 정리 Task(작업자 대기 → 남은 큐 drain) → 호출자는 **3초만** 기다림 |
| 남은 큐 처리 | `OmniSight/Application/Inspection/InspectionOutputDispatcher.cs:203-226` | `TryTake` 루프로 끝까지 기록 |
| 로그도 같은 패턴 | `OmniSight/Logging/LogManager.cs` (큐 생성·`TryAdd` 부분) | 로그는 **TryAdd = 가득 차면 버리고 개수 셈** (Day24) |
| 설계 문서 | `OmniSight/docs/onboarding/11-MemoryThreadsAndPerformance.ko.md` §2 | 큐 제한 표 |

**집에서 Claude에게:**
```text
InspectionOutputDispatcher.cs 63~286줄을 빵집 비유로 설명해줘.
보고 큐와 이미지 큐가 가득 찼을 때 각각 무슨 일이 생기는지, 이미지를 왜 Clone해서 넣는지,
Stop에서 3초만 기다리고 나머지를 백그라운드로 넘기는 이유를 알려줘.
```

## 4. 왜 이렇게 썼나

### 4.1 그림

```text
검사 스레드(카메라마다 1개) ──Publish──▶ [보고 큐 64] ──▶ 보고 작업자 1개 ─▶ CSV / 소켓 송신 요청
                              │                 (가득 차면 검사 스레드가 기다림)
                              └─Clone──▶ [이미지 큐 4] ──▶ 이미지 작업자 1개 ─▶ PNG/BMP 저장
                                                 (가득 차면 검사 스레드가 기다림)
```

### 4.2 포화 정책 비교

| 정책 | 코드 | 장점 | 단점 | 쓰는 곳 |
|---|---|---|---|---|
| 생산자 대기 | `Add(item, ct)` | 데이터 손실 0 | 생산자가 느려짐 → 앞단 큐가 참 | 검사 결과, 이미지 (생산 기록) |
| 새 것 버림 | `TryAdd` 실패 시 버림 + 카운트 | 생산자 절대 안 멈춤 | 손실 | 로그, UI 미리보기 |
| 옛 것 버림 | 꺼내 버리고 넣기 | 최신 상태 유지 | 손실, 구현 복잡 | 실시간 표시 |

**핵심 원칙**: 생산 기록(검사 결과)을 **조용히 버리지 않는다**. 버림을 허용한다면 카운터·알람·작업자 조치가 같이 있어야 한다 (아키텍처 문서 §7).

### 4.3 결정과 이유

| 결정 | 이유 |
|---|---|
| 보고와 이미지 큐 분리 | 디스크가 느려도 결과(CSV·PLC)는 먼저 나가야 함. 한 큐면 큰 이미지 저장이 결과를 막음 |
| 이미지 큐는 4로 작게 | 컬러 2000×1000 한 장 ≈ 6MB. 64장이면 수백 MB |
| 이미지는 Clone해서 큐에 | 검사 스레드는 Publish 직후 원본 Mat을 Dispose한다. 저장 작업자는 **자기 소유 사본**이 필요 |
| 큐 등록 실패 시 request Dispose | 큐에 못 넣은 사본은 주인이 없음 → 직접 해제 |
| `TryTake(…, 1000, ct)` | 1초 동안 결과가 없으면 열린 CSV 파일을 닫는 "유휴 작업"을 끼워 넣음 |
| Stop은 3초만 대기 | 디스크가 멈춰도 UI/정지 스레드가 영원히 안 멈춤. 대신 다음 Start는 이전 정리가 끝날 때까지 거부 |

### 4.4 약점 (Day25 재료)

- 이미지 큐도 `Add`로 기다리므로 **느린 디스크 = 느린 검사**. 현장 정책("이미지는 일부 생략 허용?")을 정해야 한다.
- 보고 큐가 가득 차 검사 스레드가 기다리는데 drain 모드 정지라 토큰이 취소되지 않으면 정지가 길어질 수 있다.

## 5. Kata: 빵집

```csharp
// KATA: Day10
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Katas.Day10
{
    public static class KataDay10
    {
        public static void Run()
        {
            using (var shelf = new BlockingCollection<int>(boundedCapacity: 5))
            using (var cts = new CancellationTokenSource())
            {
                var sw = Stopwatch.StartNew();
                long bakerWaitMs = 0;

                Task baker = Task.Run(() =>
                {
                    try
                    {
                        for (int bread = 1; bread <= 20; bread++)
                        {
                            if (!shelf.TryAdd(bread))                 // 바로 못 넣으면
                            {
                                var wait = Stopwatch.StartNew();
                                shelf.Add(bread, cts.Token);           // 기다렸다 넣기 (backpressure)
                                Interlocked.Add(ref bakerWaitMs, wait.ElapsedMilliseconds);
                            }
                            Console.WriteLine("[굽기] 빵 " + bread + " (진열대 " + shelf.Count + ")");
                            Thread.Sleep(20);                          // 굽기는 빠르다
                        }
                    }
                    finally
                    {
                        shelf.CompleteAdding();                        // "오늘 빵 끝!"
                    }
                });

                Task packer = Task.Run(() =>
                {
                    // CompleteAdding 후 남은 것까지 다 꺼내면 루프가 끝난다
                    foreach (int bread in shelf.GetConsumingEnumerable(cts.Token))
                    {
                        Thread.Sleep(100);                             // 포장은 느리다
                        Console.WriteLine("    [포장] 빵 " + bread);
                    }
                });

                Task.WaitAll(baker, packer);
                Console.WriteLine("총 " + sw.ElapsedMilliseconds + "ms, 제빵사가 기다린 시간 " + bakerWaitMs + "ms");
                Console.WriteLine("진열대가 5칸이라 메모리는 일정, 빵은 0개 버림");
            }
        }
    }
}
```

실험:
1. `boundedCapacity`를 없애면(무한) 제빵사 대기 0ms, 진열대 숫자가 20까지 커짐 → 메모리 증가 모델.
2. `shelf.Add` 대신 `TryAdd` 실패 시 버리고 `dropped++` → 손실 개수 출력.
3. 포장 직원을 2명(Task 2개)으로 늘리면? 순서가 섞이는지 관찰 → **순서가 중요하면 소비자는 1명** (OmniSight가 보고 작업자를 1개로 둔 이유).

## 6. MiniSight 오늘 단계

### 6.1 계약 (`Inspection/Contracts.cs` 일부)

```csharp
public sealed class InspectionResult      // 불변: 생성자에서만 값 설정
{
    public InspectionResult(string source, long frameId, DateTime time, bool ok, string message, double totalMs) { ... }
    public string Source { get; } public long FrameId { get; } public DateTime Time { get; }
    public bool Ok { get; } public string Message { get; } public double TotalMs { get; }
}
public interface IResultReporter { void Report(InspectionResult result); }
public interface IImageStore { bool ShouldSave(InspectionResult result); void Save(InspectionResult result, Mat image); }
```

### 6.2 `OutputDispatcher : IDisposable` (직접 작성)

- 생성자: `(IResultReporter reporter, IImageStore store, int reportCapacity = 64, int imageCapacity = 4)`.
- `Start()`: CTS 생성, `ReportLoop`, `ImageLoop`를 `LongRunning`으로.
- `Publish(result, Mat image, ct)`: 보고 큐 `Add` → `ShouldSave`면 `image.Clone()`을 `SaveRequest(result, mat)`에 담아 `Add`. **Add 실패/취소 시 request Dispose**.
- 루프: 예외는 잡아서 실패 카운트 + 콘솔 로그, 루프는 계속.
- `CsvReporter`: `Source,FrameId,Time,Ok,Message,TotalMs` 한 줄씩 append.
- `PngImageStore`: `{root}\{source}\{frameId:D6}.png` 로 `Cv2.ImWrite`. NG만 저장 옵션.

### 6.3 Stop (확장)

- Cancel → 작업자 대기 → 남은 큐를 `TryTake`로 끝까지 처리(drain) → 이 전체를 별도 Task로 돌리고 호출자는 `Wait(3000)`.
- 이전 정리 Task가 안 끝났으면 `Start()`에서 `InvalidOperationException`.

## 7. VS2022 디버깅 실습

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | Kata 실행 중 포장 루프 안에 중단점 | |
| 2 | 디버그 → 창 → **병렬 조사식 1** 에 `shelf.Count`, `bread` | 스레드별 값 |
| 3 | 디버그 → 창 → **작업(Tasks)** | baker/packer Task 상태: 활성 / 대기 중 / 예약됨 |
| 4 | 제빵사가 `Add`에서 기다릴 때 모두 중단 → **병렬 스택** | baker 스택 맨 위에 `BlockingCollection.TryAddWithNoTimeValidation` 류의 대기 프레임 → "어디서 막혔나"를 보는 법 |
| 5 | MiniSight `PngImageStore.Save`에 `Thread.Sleep(500)`을 넣고 카메라 10fps로 돌리기 | 이미지 큐 4가 차고 검사가 느려지는 걸 콘솔로 확인 → Sleep 제거 |

## 8. 셀프 체크

1. 유한 큐의 장점과 대가는?
<details><summary>답</summary>장점: 메모리 상한이 생기고, 소비자가 느리면 생산자가 자동으로 속도를 맞춘다(backpressure). 대가: 생산자가 막히면 그 앞단(카메라 큐, SDK 버퍼)이 차서 결국 상류에서 손실이나 지연이 생길 수 있다.</details>

2. `CompleteAdding()`은 왜 필요한가?
<details><summary>답</summary>소비자에게 "더 이상 안 들어온다"를 알려 `GetConsumingEnumerable`이 남은 항목을 다 꺼낸 뒤 정상 종료하게 한다. 없으면 소비자는 영원히 Take에서 기다린다.</details>

3. 보고 큐와 이미지 큐를 나눈 이유는?
<details><summary>답</summary>처리 비용과 우선순위가 다르다. 이미지 저장(큰 디스크 IO)이 느려져도 결과 보고(CSV, PLC)는 지연되면 안 된다. 메모리 크기도 달라 용량을 따로 정해야 한다.</details>

4. 큐에 넣을 이미지를 Clone하는 이유와, 넣기 실패 시 해야 할 일은?
<details><summary>답</summary>검사 스레드는 발행 직후 원본을 해제하므로 저장 작업자는 독립 사본이 필요하다. 큐 등록이 취소·실패하면 사본의 소유자가 없으므로 그 자리에서 Dispose해야 누수가 없다.</details>

5. 순서가 중요한 결과를 처리할 때 소비자를 여러 개 두면 안 되는 이유는?
<details><summary>답</summary>각 소비자의 처리 시간이 달라 완료 순서가 입력 순서와 달라진다. CSV 줄 순서·PLC 송신 순서가 섞인다. 병렬이 필요하면 키(카메라)별로 파티션해 키마다 소비자 1개를 둔다(OmniSight PartitionedInspectionOutputDispatcher).</details>

## 9. 블로그 포인트

- 제목 예: `[C# 동시성] 검사 결과는 버리지 않는다: Bounded BlockingCollection과 Backpressure`
- 빵집 Kata의 무한 vs 유한 비교, 포장 직원 2명일 때 순서 섞임 실험.
