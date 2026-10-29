# Day 11 (목, 2h) · 줄 서기 2: SemaphoreSlim 두 개로 만든 비동기 유한 큐

## 1. 오늘 목표

- **완료 기준**
  - `CameraFrameQueue`의 세마포어 두 개(`_slots` 빈 칸 수, `_available` 꺼낼 수 있는 수)가 어떻게 움직이는지 숫자로 따라갈 수 있다.
  - Day04의 임시 구현 `Task.Run(() => queue.Take(ct))`가 왜 아쉬운지(스레드풀 스레드를 붙잡고 기다림) 설명할 수 있다.
  - `Complete()`가 "추가 신호 한 번"으로 소비자를 깨워 `null`을 돌려주는 **drain 종료** 방식을 설명할 수 있다.
  - MiniSight `FrameQueue`를 손으로 작성하고 `DriverCameraAdapter`, `FolderCamera`의 임시 큐를 교체했다.
- **최소분량**: 2, 3, 4.1, 5, 6
- **확장분량**: 4.3 신호 불일치, 7번 Tasks 창

## 2. 🧒 초1 비유

**주차장 + 전광판 두 개**.

- 입구 전광판 `_slots` = **빈 자리 수**(처음 32). 차(프레임)가 들어올 때 하나 줄인다. 0이면 입구에서 기다린다.
- 출구 전광판 `_available` = **나갈 수 있는 차 수**(처음 0). 차가 주차하면 하나 늘린다. 0이면 출구 직원(파이프라인)이 기다린다.
- 차가 나가면 빈 자리 하나 늘림.
- 영업 종료(`Complete`) = 출구 직원에게 "마지막 손님 끝났어요" 종 한 번 울리기. 직원이 깼는데 차가 없으면 퇴근(`null` 반환).
- 출구 직원이 기다리는 동안 **의자에 앉아 쉰다**(`WaitAsync`: 스레드를 반납). Day04 방식은 직원 한 명을 **서서 기다리게** 고용한 것(스레드풀 스레드를 붙잡음).

## 3. OmniSight 지도

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 설계 의도 주석 | `OmniSight/Devices/Cameras/CameraFrameQueue.cs:8` | "버리지 않는 유한 FIFO, 프레임마다 Task.Run 생성 안 함" |
| 필드 | `OmniSight/Devices/Cameras/CameraFrameQueue.cs:11-18` | ConcurrentQueue + SemaphoreSlim 2개 + volatile `_completed` |
| 생성자 | `OmniSight/Devices/Cameras/CameraFrameQueue.cs:19-27` | `_slots = new SemaphoreSlim(capacity, capacity)` |
| 넣기 | `OmniSight/Devices/Cameras/CameraFrameQueue.cs:79-116` | `Wait(0)` 실패 시 대기 횟수 세고 `Wait(ct)`, 실패 시 **자리 돌려주기** |
| 꺼내기 | `OmniSight/Devices/Cameras/CameraFrameQueue.cs:118-140` | `WaitAsync(ct).ConfigureAwait(false)`, 꺼내면 `_slots.Release()` |
| 완료 신호 | `OmniSight/Devices/Cameras/CameraFrameQueue.cs:53-60` | `_available.Release()` 한 번 더 |
| 재사용 | `OmniSight/Devices/Cameras/CameraFrameQueue.cs:62-77` | 비어 있을 때만 리셋 |
| 해제 | `OmniSight/Devices/Cameras/CameraFrameQueue.cs:29-45` | 남은 프레임 Dispose 후 세마포어 Dispose |
| 소비자 쪽 | `OmniSight/Application/Inspection/InspectionPipeline.cs:198-207` | `null`이면 완료된 카메라인지 확인 후 break |
| 비교: 공유폴더 | `OmniSight/Devices/SharedFolderAdapter.cs` (`WaitFrameAsync`) | `Task.Run(() => _frameQueue.Take(ct))` — 아직 Day04 방식 |

**집에서 Claude에게:**
```text
CameraFrameQueue.cs 전체를 주차장 전광판 비유로 설명해줘. 용량 3이라고 치고,
Add 4번 → TakeAsync 1번 → Complete → TakeAsync 4번을 하면 _slots와 _available 숫자가
어떻게 변하는지 표로 따라가 줘. 그리고 SharedFolderAdapter의 WaitFrameAsync와 차이도.
```

## 4. 왜 이렇게 썼나

### 4.1 숫자 따라가기 (용량 3)

| 동작 | `_slots` | `_available` | 큐 | 비고 |
|---|---|---|---|---|
| 시작 | 3 | 0 | [] | |
| Add(1) | 2 | 1 | [1] | |
| Add(2), Add(3) | 0 | 3 | [1,2,3] | |
| Add(4) | 0 | 3 | [1,2,3] | **생산자 대기**, `_waits++` |
| TakeAsync → 1 | 1→0 | 2→3 | [2,3]→[2,3,4] | 자리 생겨서 4가 들어감 |
| Complete | 0 | 4 | [2,3,4] | 종 한 번 (+1) |
| TakeAsync ×3 → 2,3,4 | 3 | 1 | [] | |
| TakeAsync | 3 | 0 | [] | 깼는데 큐 비었고 completed → `null` |
| TakeAsync | | | | 시작부터 `completed && empty` → 바로 `null` |

### 4.2 결정과 이유

| 결정 | 이유 |
|---|---|
| `BlockingCollection` 대신 직접 구현 | `BlockingCollection.Take`는 동기 대기만 있다. 비동기로 쓰려면 `Task.Run`으로 감싸야 하고 **기다리는 동안 스레드풀 스레드 하나를 점유**. 카메라 4대면 4개가 상시 점유 |
| `SemaphoreSlim.WaitAsync` | 기다리는 동안 스레드를 반납. 깨어나면 스레드풀에서 이어서 실행 |
| `Wait(0)` 먼저 | 대부분은 자리가 있어 바로 통과(빠른 경로). 막힐 때만 대기 횟수를 세서 **알람**("검사가 카메라를 못 따라감") |
| 넣기 실패 시 `_slots.Release()` | 자리만 차지하고 프레임은 안 들어간 "유령 자리"를 막음 |
| `Complete` = 신호 1번 추가 | `CompleteAdding`처럼 "끝"을 알리되, 이미 쌓인 프레임은 **다 처리한 뒤** 종료 (drain). 정지할 때 마지막 프레임 결과까지 남김 |
| 생산자는 동기 `Wait` | 생산자는 SDK 콜백 스레드. 콜백 안에서 await 할 수 없고, 기다림 자체가 backpressure |

### 4.3 시니어 디테일

- `TakeAsync`에서 신호는 받았는데 큐가 비어 있으면 "신호 불일치" 예외. 정상이라면 `Complete`의 추가 신호뿐이므로 `_completed`일 때만 null, 아니면 버그다. **불변 조건(invariant)을 코드로 확인**하는 습관.
- 소비자가 2명이면 `Complete`의 추가 신호는 1명만 깨운다. 두 번째 소비자는 다음 `TakeAsync` 호출에서 `completed && empty` 검사로 빠진다. 단, 이미 `WaitAsync`에서 기다리던 두 번째 소비자는 계속 기다린다 → 이 큐는 **소비자 1명 전용**으로 설계된 것. 문서화가 필요한 가정.
- 생산자가 SDK 콜백에서 막히면 SDK 내부 버퍼가 차서 SDK가 프레임을 버릴 수 있다. 소프트웨어 큐가 안 버린다고 전수 촬영이 보장되진 않는다(onboarding 11 문서).

## 5. Kata: 비동기 유한 큐 직접 만들기

```csharp
// KATA: Day11
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Katas.Day11
{
    public sealed class AsyncBoundedQueue<T> : IDisposable where T : class
    {
        private readonly ConcurrentQueue<T> _items = new ConcurrentQueue<T>();
        private readonly SemaphoreSlim _slots;
        private readonly SemaphoreSlim _available = new SemaphoreSlim(0);
        private volatile bool _completed;
        private long _producerWaits;

        public AsyncBoundedQueue(int capacity) { _slots = new SemaphoreSlim(capacity, capacity); }
        public long ProducerWaits { get { return Interlocked.Read(ref _producerWaits); } }

        public void Add(T item, CancellationToken ct)
        {
            if (_completed) throw new InvalidOperationException("완료된 큐");
            if (!_slots.Wait(0))
            {
                Interlocked.Increment(ref _producerWaits);
                _slots.Wait(ct);
            }
            try
            {
                if (_completed) throw new InvalidOperationException("완료된 큐");
                _items.Enqueue(item);
            }
            catch
            {
                _slots.Release();   // 유령 자리 방지
                throw;
            }
            _available.Release();
        }

        public void Complete()
        {
            if (_completed) return;
            _completed = true;
            _available.Release();   // 기다리는 소비자 깨우기
        }

        public async Task<T> TakeAsync(CancellationToken ct)
        {
            if (_completed && _items.IsEmpty) return null;
            await _available.WaitAsync(ct).ConfigureAwait(false);
            T item;
            if (!_items.TryDequeue(out item))
            {
                if (_completed) return null;
                throw new InvalidOperationException("신호 불일치");
            }
            _slots.Release();
            return item;
        }

        public void Dispose() { _slots.Dispose(); _available.Dispose(); }
    }

    public static class KataDay11
    {
        public static void Run()
        {
            RunAsync().GetAwaiter().GetResult();   // 콘솔 Kata라서 여기서만 동기 대기
        }

        private static async Task RunAsync()
        {
            using (var queue = new AsyncBoundedQueue<string>(3))
            {
                Task producer = Task.Run(() =>
                {
                    for (int i = 1; i <= 10; i++) queue.Add("frame" + i, CancellationToken.None);
                    queue.Complete();
                });

                int count = 0;
                while (true)
                {
                    string frame = await queue.TakeAsync(CancellationToken.None).ConfigureAwait(false);
                    if (frame == null) break;
                    await Task.Delay(30).ConfigureAwait(false);   // 검사는 느리다
                    count++;
                    Console.WriteLine("검사 " + frame + " (스레드 " + Thread.CurrentThread.ManagedThreadId + ")");
                }
                await producer.ConfigureAwait(false);
                Console.WriteLine("처리 " + count + "장, 생산자 대기 " + queue.ProducerWaits + "회, 손실 0");
            }
        }
    }
}
```

관찰: 출력의 스레드 번호가 **바뀐다**. await 이후 다른 스레드풀 스레드에서 이어지기 때문. (Day12에서 자세히)

## 6. MiniSight 오늘 단계

1. `Devices/Cameras/FrameQueue.cs`: Kata를 `ImageFrame` 전용으로. 추가 요구사항:
   - `Count`, `EnqueuedCount`, `DequeuedCount`, `ProducerWaitCount` (Interlocked)
   - `TryTake(out ImageFrame)` — 비동기 대기 없이 꺼내기 (정지 시 남은 것 버릴 때)
   - `Dispose()` — 남은 프레임 **전부 Dispose** 후 세마포어 해제
   - `Reset()` — 비어 있을 때만, 신호 비우고 completed=false
2. `DriverCameraAdapter`, `FolderCamera`의 `BlockingCollection` + `Task.Run(Take)`를 `FrameQueue`로 교체. `WaitFrameAsync(ct) => _queue.TakeAsync(ct)`.
3. 어댑터에 `IDrainableSource { bool AcquisitionCompleted { get; } void CompleteAcquisition(); }` 구현: 드라이버 Stop → `_queue.Complete()`.

## 7. VS2022 디버깅 실습

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | Kata의 `await _available.WaitAsync` 다음 줄에 중단점 | |
| 2 | 디버그 → 창 → **작업(Tasks)** | 소비자 Task가 "대기 중(Awaiting)" 상태. **스레드 창에는 소비자 스레드가 없다** = 스레드를 반납했다는 증거 |
| 3 | 같은 지점에서 **병렬 스택 → 작업 보기(Tasks view)** | async 논리 호출 스택 |
| 4 | Day04 방식(`Task.Run(() => bc.Take())`)으로 바꿔서 같은 관찰 | 스레드 창에 `Take`에서 막힌 스레드가 **보인다** |
| 5 | `ProducerWaitCount`에 조건부 중단점 `ProducerWaits > 0`(Hit Count 1) | 처음 막힌 순간만 잡기 |

## 8. 셀프 체크

1. 세마포어 두 개 각각의 의미는?
<details><summary>답</summary>_slots: 남은 빈 칸 수(생산자가 기다리는 기준, 초기값 = 용량). _available: 꺼낼 수 있는 항목 수(소비자가 기다리는 기준, 초기값 0). 넣으면 slots−1, available+1. 꺼내면 available−1, slots+1.</details>

2. `Task.Run(() => blockingCollection.Take(ct))`를 WaitFrameAsync로 쓰면 무엇이 문제인가?
<details><summary>답</summary>기다리는 동안 스레드풀 스레드 하나가 블로킹된다. 입력 수만큼 스레드가 상시 점유돼 스레드풀 고갈과 지연을 유발한다. 진짜 비동기 대기(SemaphoreSlim.WaitAsync)는 기다리는 동안 스레드를 쓰지 않는다.</details>

3. `Complete()`가 큐를 바로 비우지 않는 이유는?
<details><summary>답</summary>정지 시에도 이미 촬영된 프레임의 검사 결과를 남기기 위해서다(drain). 소비자는 남은 것을 다 처리한 뒤 null을 받아 정상 종료한다. 즉시 버려야 하는 비상 정지는 Cancel + TryTake로 따로 처리한다.</details>

4. Add에서 enqueue 전에 예외가 나면 왜 `_slots.Release()`를 해야 하나?
<details><summary>답</summary>이미 `_slots.Wait`로 자리를 하나 차지했기 때문이다. 반납하지 않으면 실제 항목 없이 자리가 줄어 용량이 영구히 줄어든다.</details>

5. 생산자 대기 횟수를 세는 이유는?
<details><summary>답</summary>큐가 가득 차 SDK 콜백이 막혔다는 것은 검사·저장이 입력 속도를 못 따라간다는 경고다. 카운터와 알람으로 현장에서 처리량 문제를 조기에 발견한다.</details>

## 9. 블로그 포인트

- 제목 예: `[C# 비동기] BlockingCollection을 await 하고 싶을 때: SemaphoreSlim 두 개로 만든 Bounded Async Queue`
- 숫자 따라가기 표, Tasks 창과 스레드 창 비교 스크린샷(MiniSight).
