# Day 09 (화, 2h) · 같이 쓰면 망가진다: race, lock, Monitor, Interlocked, volatile

## 1. 오늘 목표

- **완료 기준**
  - race condition을 **직접 재현**했다 (기대 4,000,000인데 더 작게 나옴).
  - `lock`, `Interlocked`, `volatile/Volatile`이 각각 무엇을 보장하고 무엇을 보장하지 않는지 표로 설명할 수 있다.
  - CAS(Compare-And-Swap) 루프를 손으로 작성했다.
  - OmniSight에서 "lock 대신 Interlocked를 쓴 곳"과 "불변 스냅샷 교체"를 2곳씩 찾았다.
  - MiniSight: 통계 카운터를 Interlocked로, `ImageFrame.Dispose` once 확인.
- **최소분량**: 2, 3, 4.1, 5의 1~3단계
- **확장분량**: 5의 CAS와 SharedFileWriteGate 흉내, 7번 Freeze로 race 재현

## 2. 🧒 초1 비유

- **race condition = 칠판 숫자 동시에 고치기**. 칠판에 "5"가 있다. 영희와 철수가 동시에 보고 "6"으로 고친다. 둘이 1씩 더했는데 결과는 6. 하나가 사라졌다. `count++`는 "읽기 → 더하기 → 쓰기" 세 번 움직임이라 중간에 끼어들 수 있다.
- **lock = 화장실 열쇠 하나**. 열쇠를 가진 사람만 들어간다(임계 구역). 나오면서 열쇠를 돌려준다. 다른 사람은 문 앞에서 기다린다. 열쇠를 들고 화장실에서 낮잠 자면(lock 안에서 오래 걸리는 일) 줄이 엄청 길어진다.
- **Interlocked = 은행 번호표 기계**. 누르면 기계가 **한 번에** 번호를 하나 올려 준다. 두 명이 동시에 눌러도 번호가 겹치지 않는다. 대신 아주 단순한 일(더하기, 바꾸기)만 할 수 있다.
- **volatile = 게시판 직접 보기**. 친구가 "어제 게시판 봤는데 소풍 연기래"(CPU 캐시에 남은 옛 값)라고 하는 대신 매번 게시판을 직접 본다. 하지만 게시판을 보는 것만으로 "동시에 고치기"를 막지는 못한다.

## 3. OmniSight 지도

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 큰 열쇠 하나 | `OmniSight/Application/VisionSystem.cs:60`, `:383` | `_sync`로 Start/Stop/모드 변경을 한 줄로 세움 |
| 열쇠 공유 | `OmniSight/Application/VisionSystem.cs:73` | `ManualCameraController`에 **같은 `_sync`**를 넘김 → 수동 조작과 자동 운전이 동시에 안 일어남 |
| lock 없는 통계 | `OmniSight/Application/Inspection/InspectionPipeline.cs:34-37,67-68,218,313` | `Interlocked.Increment/Read` |
| 한 번만 해제 | `OmniSight/Devices/DeviceContracts.cs:91-101` | `Interlocked.Exchange(ref _disposed, 1)`, `Exchange(ref _owner, null)` |
| volatile 플래그 | `OmniSight/Application/Inspection/InspectionPipeline.cs:33` | `_acceptResults` (정지 스레드가 쓰고 작업자가 읽음) |
| 불변 스냅샷 교체 | `OmniSight/Application/HardwareMonitorService.cs:73,116` | 객체 통째로 새로 만들어 `Volatile.Write` |
| CAS 루프 | `OmniSight/UI/Controls/LiveCameraDisplayControl.cs:123-139` | 10fps 제한을 lock 없이 |
| CAS 바쁨 플래그 | `OmniSight/UI/Controls/LiveCameraDisplayControl.cs:73,106,116` | `CompareExchange(ref _busy, 1, 0)` → 하나만 실행 |
| 경로별 잠금 | `OmniSight/Application/Inspection/SharedFileWriteGate.cs:9-52` | 사전 lock + 경로별 Monitor + 사용자 수 세기 |

**집에서 Claude에게:**
```text
LiveCameraDisplayControl.cs 123~139줄 TryAcceptLivePreviewFrame을 번호표 기계 비유로 한 줄씩 설명해줘.
왜 while(true) 루프가 필요한지, CompareExchange가 실패하면 무슨 뜻인지.
그다음 SharedFileWriteGate.cs 전체를 보여주고 lock 두 종류(RegistrySync, entry.Sync)를 왜 나눴는지 알려줘.
```

## 4. 왜 이렇게 썼나

### 4.1 도구 비교표 (암기)

| 도구 | 보장하는 것 | 보장 안 하는 것 | 비용 | 쓰는 곳 |
|---|---|---|---|---|
| `lock (obj)` = `Monitor.Enter/Exit` | 상호 배제 + 메모리 가시성 | 교착 방지, 공정성 | 경쟁 없으면 싸고, 경쟁 시 대기 | 여러 필드를 함께 바꿀 때 |
| `Interlocked.Increment/Add` | 원자적 숫자 변경 | 여러 변수의 일관성 | 매우 쌈 | 카운터 |
| `Interlocked.Exchange` | 원자적 교체 + 이전 값 반환 | | 쌈 | 한 번만 하기(Dispose), 소유권 꺼내기 |
| `Interlocked.CompareExchange` | "기대값이면 바꾸기"를 원자적으로 | 실패 시 재시도는 내가 | 쌈 | 스로틀, 바쁨 플래그, lock-free 갱신 |
| `volatile` / `Volatile.Read/Write` | 최신 값 읽기/쓰기 순서 | `x++` 같은 복합 연산 원자성 | 거의 0 | 정지 플래그, 스냅샷 참조 |
| `Interlocked.Read(ref long)` | 32비트 프로세스에서 long 찢어짐 방지 | | 쌈 | long 카운터 읽기 |

### 4.2 결정과 이유

| 결정 | 이유 |
|---|---|
| VisionSystem은 큰 lock 하나 | 운전 상태 전환은 드물고(사람 버튼, PLC 명령) **정확성이 속도보다 중요**. lock 여러 개면 순서 실수로 교착 |
| 프레임 통계는 Interlocked | 초당 수십 번 올라감. lock은 과함 |
| 스냅샷은 통째로 교체 | 읽는 쪽이 lock 없이 일관된 값 세트를 봄. 필드를 하나씩 바꾸면 CPU는 새 값, RAM은 옛 값처럼 **섞인 상태**를 볼 수 있음 |
| 10fps 제한은 CAS | 카메라 콜백 스레드가 UI 때문에 lock 대기하면 안 됨 → 실패하면 그냥 이번 프레임 미리보기 건너뜀 |
| 경로별 Monitor | 서로 다른 파일 쓰기는 동시에, **같은 파일만** 줄 세움. 다 끝나면 사전에서 지워 메모리 안 늘어남 |

### 4.3 시니어의 함정 목록

- `lock(this)`, `lock(typeof(X))`, `lock("문자열")` 금지: 외부 코드도 같은 객체로 lock 할 수 있다. 항상 `private readonly object _sync = new object();`
- lock 안에서 **await 불가**(컴파일 에러). `Monitor`는 같은 스레드에서 Exit 해야 한다. async 코드에서는 `SemaphoreSlim(1,1)` (Day11·18).
- lock 안에서 이벤트·콜백·I/O 호출 = 교착/지연의 씨앗 (Day06의 ChangeState 이야기).
- `volatile bool _stop`으로 루프를 끝내는 건 OK. `volatile int _count; _count++`는 여전히 race.
- `DeviceBase.State`처럼 lock 일부 구간에서만 쓰는 필드는 **어떤 스레드가 쓰는지 규칙**을 문서로 남겨야 한다.

## 5. Kata: 은행 잔고 race → lock → Interlocked → CAS

```csharp
// KATA: Day09
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Katas.Day09
{
    public static class KataDay09
    {
        private static int _unsafeCount;
        private static int _lockedCount;
        private static int _atomicCount;
        private static readonly object _sync = new object();

        // 출발 신호를 같이 기다렸다가 동시에 뛰게 한다 (안 그러면 먼저 시작한 스레드가 혼자 다 끝내 버려 race가 안 보인다)
        private static void RunTogether(int threads, Action work)
        {
            using (var go = new ManualResetEventSlim(false))
            {
                var workers = new Thread[threads];
                for (int t = 0; t < threads; t++)
                {
                    workers[t] = new Thread(() => { go.Wait(); work(); });
                    workers[t].Start();
                }
                go.Set();                               // 땅!
                foreach (Thread w in workers) w.Join();
            }
        }

        public static void Run()
        {
            const int threads = 4;
            const int perThread = 1000000;
            const int expected = threads * perThread;

            // 1단계: race 재현
            RunTogether(threads, () => { for (int i = 0; i < perThread; i++) _unsafeCount++; });
            Console.WriteLine("unsafe : " + _unsafeCount + " (" + expected + "보다 작으면 race)");

            // 2단계: lock
            RunTogether(threads, () => { for (int i = 0; i < perThread; i++) { lock (_sync) { _lockedCount++; } } });
            Console.WriteLine("lock   : " + _lockedCount);

            // 3단계: Interlocked
            RunTogether(threads, () => { for (int i = 0; i < perThread; i++) Interlocked.Increment(ref _atomicCount); });
            Console.WriteLine("atomic : " + _atomicCount);

            // 4단계: CAS - "최대값 갱신"은 Interlocked에 없다. 직접 만든다.
            int max = 0;
            Parallel.For(0, 100000, i =>
            {
                int candidate = (i * 7919) % 100003;
                while (true)
                {
                    int seen = Volatile.Read(ref max);
                    if (candidate <= seen) break;                                  // 바꿀 필요 없음
                    if (Interlocked.CompareExchange(ref max, candidate, seen) == seen) break; // 내가 이김
                    // 누가 먼저 바꿨다 → 다시 읽고 재시도
                }
            });
            Console.WriteLine("CAS max: " + max);

            // 5단계: 한 번만 실행 (Dispose once)
            int disposed = 0;
            int winners = 0;
            Parallel.For(0, 8, i =>
            {
                if (Interlocked.Exchange(ref disposed, 1) == 0) Interlocked.Increment(ref winners);
            });
            Console.WriteLine("Dispose 실제 실행 횟수: " + winners + " (항상 1)");
        }
    }
}
```

확장 과제: `SharedFileWriteGate`를 흉내 낸 `PathGate.Enter(path)`를 만들고, 같은 경로 2개 + 다른 경로 1개를 동시에 쓰는 스레드 3개로 "같은 경로만 줄 서는지" 로그로 확인한다.

## 6. MiniSight 오늘 단계

1. `Inspection/PipelineStats.cs`(Day13에서 쓸 것 미리): `Processed`, `Failed`, `Dropped`를 `long` + `Interlocked.Increment`, 읽기는 `Interlocked.Read`. `Reset()`은 `Interlocked.Exchange(ref x, 0)`.
2. `ImageFrame.Dispose`를 `Parallel.For(0, 8, _ => frame.Dispose())`로 때려 보고 예외가 없는지 확인 (Mat 이중 해제면 크래시).
3. `DeviceBase`에 "State는 lock 안 또는 Start/Stop을 부르는 단일 스레드에서만 쓴다" 주석 + `State` 읽기를 `Volatile.Read` 백킹 필드로 바꿔 본다 (enum은 int로 저장해야 Volatile 사용 가능 → 실제로 해 보고 왜 불편한지 느끼기).

## 7. VS2022 디버깅 실습: race를 눈으로 보기

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | 아래 코드를 콘솔에 작성: 두 스레드가 `int temp = _balance; temp += 100; _balance = temp;` | |
| 2 | `temp += 100;` 줄에 중단점 → 실행 → 스레드 A에서 멈춤 | |
| 3 | 스레드 창에서 **A를 Freeze**, F5 → B가 같은 줄에서 멈춤 → B도 F10으로 끝까지 진행 | B가 `_balance`를 썼다 |
| 4 | B Freeze, A Thaw → A 진행 | A가 **옛 temp**로 덮어씀 → 100 사라짐. race를 손으로 재현 |
| 5 | 같은 코드를 `lock`으로 감싸고 반복 | B가 lock 앞에서 기다리는 것 (병렬 스택에서 `Monitor.Enter`) |
| 6 | 디버그 → 창 → **병렬 조사식(Parallel Watch)** 에 `_balance`, `temp` | 스레드별 값이 표로 보임 |

## 8. 셀프 체크

1. `count++`가 원자적이지 않은 이유는?
<details><summary>답</summary>메모리에서 읽기, 레지스터에서 1 더하기, 메모리에 쓰기의 3단계다. 다른 스레드가 사이에 끼어들어 같은 옛 값을 읽으면 증가가 사라진다.</details>

2. `volatile`만으로 카운터를 안전하게 만들 수 없는 이유는?
<details><summary>답</summary>volatile은 읽기/쓰기의 가시성과 순서만 보장한다. 읽고-고치고-쓰는 복합 연산 사이의 끼어들기는 막지 못한다. Interlocked나 lock이 필요하다.</details>

3. CAS 루프에서 `CompareExchange`의 반환값이 기대값과 다르면 무슨 뜻이고 어떻게 하나?
<details><summary>답</summary>내가 읽은 뒤 다른 스레드가 값을 먼저 바꿨다는 뜻이다. 내 변경은 적용되지 않았으므로 최신 값을 다시 읽고 조건을 재검사해 재시도한다.</details>

4. 왜 `lock(this)`를 쓰면 안 되나?
<details><summary>답</summary>객체 바깥 코드도 같은 인스턴스로 lock을 걸 수 있어 의도치 않은 경쟁·교착이 생긴다. 잠금 객체는 private으로 숨겨 잠금 규칙을 클래스 안에 가둔다.</details>

5. 여러 필드로 된 상태를 lock 없이 안전하게 공유하는 방법은?
<details><summary>답</summary>불변(immutable) 객체로 묶어 통째로 새로 만들고 참조만 Volatile.Write(또는 Interlocked.Exchange)로 교체한다. 읽는 쪽은 Volatile.Read로 참조를 한 번 얻어 그 객체만 쓴다(HardwareMonitorSnapshot, 소켓 Snapshot).</details>

## 9. 블로그 포인트

- 제목 예: `[C# 동기화] lock, Interlocked, volatile을 실험으로 비교하기`
- Kata 1~3단계 출력 숫자 비교표, Freeze/Thaw로 race를 손으로 재현한 과정(스크린샷).
