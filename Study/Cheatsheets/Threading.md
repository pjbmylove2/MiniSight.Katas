# 멀티스레드·병렬처리 치트시트 (C# 7.3 / .NET Framework 4.7.2)

## 1. OmniSight 스레드 지도 (카메라 2대 기준)

| 스레드/작업 | 종류 | 하는 일 | 위치 |
|---|---|---|---|
| UI (Main) | STA 메시지 루프 | 화면, 버튼, Forms.Timer | `OmniSight/Program.cs:75` |
| SDK 콜백 ×2 | 벤더 SDK 스레드 | 픽셀 복사 → ImageFrame → 큐 Add(대기) | `CallbackCameraAdapterBase.cs:409-444` |
| 파이프라인 ×2 | LongRunning + async (실제로는 첫 await 후 스레드풀) | 큐 Take → 전처리 → 룰 → AI → 발행 | `InspectionPipeline.cs:96,188-339` |
| 룰/AI 실행 | `Task.Run` (바로 await = 순차) | 계산 | `InspectionPipeline.cs:278,283` |
| 보고 작업자 | LongRunning 동기 | CSV, PLC 송신 요청 | `InspectionOutputDispatcher.cs:83,228-256` |
| 이미지 작업자 | LongRunning 동기 | 이미지 저장 | `InspectionOutputDispatcher.cs:85,258-286` |
| PLC 연결 루프 | LongRunning + async | 연결, 송신, 하트비트 | `LegacyOcrSocketDevice.cs:85,345-463` |
| PLC 수신 | `Task.Run` | ReadAsync, 재동기화 | `LegacyOcrSocketDevice.cs:384,465-512` |
| 로그 작성기 | LongRunning 동기, BelowNormal | 배치 쓰기 | `LogManager.cs:91` |
| 하드웨어 감시 | `new Thread`, BelowNormal | CPU/RAM/디스크 | `HardwareMonitorService.cs:76` |
| 보관 정리 | `new Thread`, BelowNormal | 오래된 파일 삭제 | `SystemRetentionService.cs:27` |
| 헬스 로그 | `System.Threading.Timer` 10분 | 상태 로그 | `VisionSystem.cs` (`_healthTimer`) |

## 2. 큐 용량과 포화 정책

| 큐 | 용량 | 가득 차면 | 이유 |
|---|---|---|---|
| 실물 카메라 FIFO | 32 | 생산자(SDK 콜백) 대기 | 검사 영상 손실 금지 |
| 시뮬레이션/공유폴더 | 3 / 5 | 대기 | 원격 폴더가 버퍼 |
| 보고 | 64 | 검사 작업자 대기 | 결과 손실 금지 |
| 이미지 저장 | 4 | 검사 작업자 대기 | 메모리(장당 수 MB) |
| PLC 송신 | 256 | TryAdd 실패 → 예외 | |
| 로그 | 32768 + 우선 4096, 바이트 예산 | **버림 + 카운트** | 로그가 검사를 막지 않게 |
| UI 프리뷰 | 1 (바쁨 플래그) | **버림** | 최신만 표시 |

## 3. 도구 선택표

| 하고 싶은 일 | 도구 | 주의 |
|---|---|---|
| 카운터 | `Interlocked.Increment/Add`, 읽기 `Interlocked.Read` | |
| 한 번만 | `Interlocked.Exchange(ref flag, 1) == 0` | |
| 정지 신호 | `CancellationToken` / `volatile bool` | volatile은 복합 연산 원자성 없음 |
| 여러 필드 함께 | `lock (private readonly object)` | lock 안에서 대기·이벤트·I/O 금지 |
| async 안 상호 배제 | `SemaphoreSlim(1,1).WaitAsync` | lock 안 await 불가 |
| 읽기 많고 쓰기 드묾 | `ReaderWriterLockSlim` 또는 불변 스냅샷 교체 | RWLock은 같은 스레드에서 해제 |
| 최신 묶음 공유 | 불변 객체 + `Volatile.Write/Read` | 받은 객체 수정 금지 |
| lock 없는 갱신 | `CompareExchange` 루프 | 실패 시 다시 읽고 재시도 |
| 유한 큐 (동기) | `BlockingCollection<T>(capacity)` | `CompleteAdding`으로 종료 |
| 유한 큐 (async 소비) | `ConcurrentQueue` + `SemaphoreSlim` 2개 | 소비자 1명 가정 |
| 콜백 → Task | `TaskCompletionSource` + `RunContinuationsAsynchronously` | |
| 타임아웃 | `Task.WhenAny(task, Task.Delay(ms, ct))` | 진 Task 관찰 |
| 깨울 수 있는 sleep | `WaitHandle.WaitOne(ms)`, `token.WaitHandle.WaitOne(ms)`, `Task.Delay(ms, ct)` | |
| 오래 도는 동기 루프 | `new Thread` 또는 `StartNew(LongRunning)` | async 람다면 의미 약함 |
| 짧은 CPU 작업 | `Task.Run` | 프레임마다 남발 금지 |
| UI 갱신 | `BeginInvoke` + `IsHandleCreated`/`IsDisposed` 확인 | `Invoke`는 교착 후보 |

## 4. 정지 순서 (외우기)

```text
Start:  출력 디스패처 → 파이프라인(입력 순) → (장치는 VisionSystem이 먼저 Start: 큐에 쌓일 수 있음)
Stop :  ① 취득 완료(CompleteAcquisition: SDK 정지 → 큐 Complete)
        ② 파이프라인 역순 Stop (drain이면 Cancel 없이 null까지 처리)
        ③ 출력 Stop (Cancel → 작업자 대기 → 남은 큐 drain, 호출자는 3초만 대기)
        ④ 장치 역순 Stop
Dispose: VisionSystem → 설정 저장 → AI 해제 → 로그 (마지막)
```

## 5. async 규칙 7

1. 백그라운드/라이브러리 await는 `ConfigureAwait(false)`.
2. `async void`는 UI 이벤트 핸들러만, 내부 전체 try/catch.
3. UI 스레드에서 `.Wait()`, `.Result` 금지.
4. 전용 스레드(컨텍스트 없음)에서만 `GetAwaiter().GetResult()` 허용.
5. 취소는 `OperationCanceledException` + `when (token.IsCancellationRequested)`.
6. 타임아웃과 취소는 함께, 진 Task 관찰.
7. Fire-and-forget이면 예외를 반드시 잡아 로그.

## 6. 데드락 4조건과 깨기

상호 배제(불변·CAS) / 점유 대기(쥔 채로 기다리지 않기) / 비선점(타임아웃) / 순환 대기(lock 순서 고정).
UI `.Result` 데드락 = UI 스레드 점유 + 계속이 UI 스레드 필요 → 끝까지 async.

## 7. 소유권 한 줄 규칙

- 만든 사람이 넘기기 전까지 책임, 넘긴 뒤 로컬은 `null`.
- 큐에 넣기 실패 = 아직 내 것 → 내가 Dispose.
- 이벤트로 받은 것 = 핸들러 안에서만 유효. 오래 쓰려면 Clone.
- 래퍼(`FromPixelData`)는 소유하지 않는다. 원본보다 오래 살면 안 된다.
