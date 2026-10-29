# Day 18 (목, 2h) · AI 엔진 다루기: Dispose 패턴, P/Invoke, Object Pool, 교체 잠금

## 1. 오늘 목표

- **완료 기준**
  - **Dispose(bool) + finalizer 패턴**을 손으로 쓰고, `disposing`이 true/false일 때 각각 무엇을 해제하는지 설명할 수 있다.
  - `[DllImport]`, `IntPtr` 핸들, `Marshal.Copy/PtrToStructure`로 네이티브 결과를 **관리 메모리로 복사**하는 이유를 설명할 수 있다.
  - **Object Pool**(엔진 N개, `ConcurrentQueue` + `SemaphoreSlim(N, N)`)이 왜 필요한지(핸들 1개 = 동시 추론 1개, 생성 비용 큼) 설명할 수 있다.
  - `ReaderWriterLockSlim`으로 "추론 중엔 모델 교체 대기, 교체는 새 사전을 만든 뒤 한 번에 바꾸기(copy-on-write)"를 설명할 수 있다.
  - x86/x64 불일치(`BadImageFormatException`), DLL 없음(`DllNotFoundException`)을 VS2022에서 재현·확인했다.
  - MiniSight: `FakeAiEngine`(가짜 네이티브 핸들) + `AiEnginePool` + `AiEngineRegistry` 작성.
- **최소분량**: 2, 3, 4.1~4.3, 5의 1~2
- **확장분량**: 5의 3 레지스트리, 4.5 OCR 디코딩, 7번 4~5

## 2. 🧒 초1 비유

- **네이티브 핸들 = 놀이공원 사물함 열쇠**. C# 세상 밖(C++ SDK)의 사물함을 빌린 것이다. GC 청소부는 사물함을 모른다. 다 쓰면 **직접 반납**(Close/Destroy)해야 한다. 잊으면 영업 끝날 때(finalizer) 겨우 회수되는데, 그때는 너무 늦거나 순서가 꼬일 수 있다.
- **Dispose(bool) = 반납 체크리스트**. 내가 직접 반납할 때(`disposing=true`)는 C# 물건(세마포어 등)도 같이 정리한다. 청소부가 대신 반납할 때(`false`)는 사물함 열쇠만 반납한다(C# 물건은 이미 청소됐을 수 있으니 건드리지 않음).
- **Object Pool = 놀이기구 탈 사람 줄과 좌석 N개**. AI 엔진(좌석)은 비싸서 매번 새로 못 만든다. 좌석 2개면 동시에 2명만 탄다. 내리면 다음 사람. 카메라가 2대면 좌석도 2개.
- **ReaderWriterLock = 도서관 열람실과 공사**. 여러 명이 동시에 책을 읽을 수 있다(읽기 잠금). 책장을 바꾸는 공사(모델 교체, 쓰기 잠금)는 **모두 나간 뒤** 한다. 공사는 새 책장을 미리 조립해 두고 **한 번에 바꿔 끼운다**.

## 3. OmniSight 지도

> ⚠️ AI SDK는 외부 공급사 계약 코드다. **블로그에는 SDK 이름·함수명을 절대 쓰지 않는다.** "네이티브 AI SDK"로만.

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 엔진 계약 | `OmniSight/Vision/IVisionEngine.cs` | `Initialize(model, license)`, `Inspect(channel, w, h, step, IntPtr, options)` |
| 템플릿 + Dispose 패턴 | `OmniSight/Vision/AiEngine/AiEngineBase.cs:13-47` | `IntPtr _handle`, 엔진당 `SemaphoreSlim(1,1)` (결과 포인터가 다음 호출에 바뀜) |
| 워밍업 | `OmniSight/Vision/AiEngine/AiEngineBase.cs:49-60` 이후 | 0으로 채운 **고정(pinned) 배열**로 더미 추론 → GPU 버퍼 미리 확보 |
| Dispose/finalizer | `OmniSight/Vision/AiEngine/AiEngineBase.cs:149-176` | `Dispose(true)` + `SuppressFinalize`, `~AiEngineBase() => Dispose(false)` |
| P/Invoke 선언 | (AI SDK 래퍼, 비공개) | `[DllImport]` static extern, 핸들 생성/열기/처리/닫기/파괴 |
| 결과 복사 | (AI SDK 엔진 구현, 비공개) | `Marshal.Copy`, `PtrToStructure` 후 잠금 해제 |
| 빌드 변형 | `OmniSight/OmniSight.csproj` (SDK 변형 조건부 Compile) | SDK 버전 두 개를 구성으로 골라 컴파일 |
| 엔진 공장 | `OmniSight/Vision/AiEngine/AiEngineRegistry.cs:23-51` | 도구 종류 → 엔진 클래스 |
| 레지스트리 싱글톤 | `OmniSight/Vision/AiEngine/AiEngineRegistry.cs:58-70` | `Shared` (Lazy), public 생성자는 테스트용 |
| 설정 적용 | `OmniSight/Vision/AiEngine/AiEngineRegistry.cs:98-184` | 새 사전 조립 → 같은 파일(경로·크기·수정시각)이면 재사용 → 실패 시 새로 만든 것만 해제 → 쓰기 잠금으로 교체 |
| 추론 | `OmniSight/Vision/AiEngine/AiEngineRegistry.cs:219-241` | **읽기 잠금** 안에서 풀에 위임 |
| 풀 | `OmniSight/Vision/AiEngine/AiEngineRegistry.cs:361-471` | `ConcurrentQueue` + `SemaphoreSlim(N,N)`, finally에서 반납 |
| 풀 크기 | `OmniSight/Application/ApplicationBootstrapper.cs:151` | 활성 입력 수 |
| OCR 디코딩 | `OmniSight/Application/Inspection/LegacyOcrDecoder.cs`, `OcrDetectionFilter.cs`, `OmniSight/Devices/OcrVoteWindow.cs` | 원형 문자열 해석, 중복 제거, 시간창 투표 (확장) |

**집에서 Claude에게:**
```text
AiEngineBase.cs 13~47, 149~176줄과 AiEngineRegistry.cs 98~184, 219~241, 361~471줄을
사물함·놀이기구 좌석·도서관 공사 비유로 설명해줘. 특히 (1) 엔진마다 SemaphoreSlim(1,1)이 있는데
풀에 또 SemaphoreSlim(N,N)이 있는 이유, (2) 읽기 잠금을 추론 내내 잡는 이유와 부작용,
(3) 풀 대기에 타임아웃이 없는 위험을 알려줘. SDK 함수 이름은 블로그용으로 일반화해서 말해줘.
```

## 4. 왜 이렇게 썼나

### 4.1 Dispose 패턴 규칙표

| 상황 | `disposing` | 해제할 것 | 하면 안 되는 것 |
|---|---|---|---|
| 사용자가 `Dispose()` 호출 | true | 관리 자원(SemaphoreSlim, 다른 IDisposable) + 네이티브 핸들 | — |
| GC finalizer | false | 네이티브 핸들만 | 다른 관리 객체 접근(이미 finalize됐을 수 있음), 예외 던지기, 로그 매니저 호출 |
| 두 번째 호출 | — | 아무것도 (`_isDisposed` 확인) | 이중 해제 |

`.NET` 권장은 `IntPtr` 대신 **`SafeHandle`**을 상속해 핸들을 감싸는 것이다(finalizer·재시도·스레드 안전을 런타임이 보장). OmniSight는 SDK 이식 단계라 `IntPtr` + 수동 패턴을 쓴다 → 개선 후보.

### 4.2 결정과 이유

| 결정 | 이유 |
|---|---|
| 엔진당 `SemaphoreSlim(1,1)` | SDK 결과 포인터가 **다음 Process 호출에서 덮임**. 결과를 관리 메모리로 다 복사할 때까지 다른 호출 금지 |
| 결과를 즉시 `Marshal.Copy` | 잠금 해제 후엔 네이티브 메모리를 믿을 수 없음 |
| 풀 크기 = 입력 수 | 카메라 2대가 동시에 추론해도 기다리지 않게. 더 많으면 GPU 메모리만 낭비 |
| 풀 = ConcurrentQueue + SemaphoreSlim | 세마포어가 "남은 좌석 수", 큐가 "실제 좌석". 세마포어 통과 = 큐에 반드시 하나 있음 |
| finally에서 반납 | 추론이 예외를 던져도 좌석이 사라지지 않게 |
| 파일 서명(크기+수정 시각) 비교 | 같은 모델이면 재사용 → 레시피 저장 때마다 수 초짜리 GPU 초기화 반복 방지 |
| 새 사전 조립 → 교체 → 옛것 해제 | 조립 중 실패해도 기존 엔진은 멀쩡 (all-or-nothing) |
| 추론 = 읽기 잠금, 교체 = 쓰기 잠금 | 교체가 **사용 중인 핸들을 해제하지 않음** |
| 워밍업 더미 추론 | 첫 실제 프레임에서 GPU 초기화(수백 ms~초) 지연이 생기지 않게 |

### 4.3 시니어의 눈

- 풀의 `_availableSignal.Wait()`에 **토큰·타임아웃이 없다**. SDK 하나가 멈추면 모든 파이프라인이 멈추고 Stop도 끝나지 않는다. 최소한 `Wait(timeout, ct)` + 알람이 필요하다.
- `ReaderWriterLockSlim`은 쓰기 대기자가 있으면 **새 읽기를 막는다**. 운전 중 모델 교체를 시도하면 그 순간 모든 추론이 멈춘다 → 교체는 정지 상태에서만 허용하는 규칙이 안전.
- `ReaderWriterLockSlim`은 **획득한 스레드에서 해제**해야 한다. async 메서드에서 await를 끼우면 깨진다. OmniSight 추론은 동기라 OK.

### 4.4 P/Invoke 체크리스트 (장비 개발자 필수)

1. 플랫폼: SDK DLL이 x64면 앱도 x64. Any CPU + "32비트 선호"면 `BadImageFormatException`.
2. DLL 위치: exe 폴더 또는 PATH. 의존 DLL(CUDA 런타임 등)까지. 없으면 `DllNotFoundException`(실제로는 의존 DLL이 없어도 같은 예외).
3. 호출 규약 `CallingConvention.Cdecl/StdCall`, 문자열 인코딩(UTF-8 바이트 + NUL 종료를 직접 만드는 경우 있음).
4. 구조체 `[StructLayout(LayoutKind.Sequential)]`, 크기 확인.
5. 관리 배열을 넘길 땐 `GCHandle.Alloc(Pinned)` 또는 `fixed`.
6. 네이티브가 준 포인터의 수명: 다음 호출까지만인지, 내가 해제해야 하는지 문서 확인.

### 4.5 (확장) OCR 결과 다듬기 아이디어

- **원형 문자열**: 원통에 찍힌 글자는 어디서부터 읽힐지 모름("23A1" → "A123"). 마커 글자(A/F/Y)를 기준으로 원형으로 이어 읽고, 후보가 여럿이면 다수결 → 점수 → 위치 순으로 고른다 (`LegacyOcrDecoder`).
- **중복 제거**: 중심이 가까운 검출은 점수×면적이 큰 것만 남김. 격자 해시로 이웃 3×3 칸만 검사 (`OcrDetectionFilter`).
- **시간창 투표**: 같은 제품을 여러 프레임에서 읽으면 가장 많이 나온 값을 보냄 (`OcrVoteWindow`).

## 5. Kata: 사물함과 좌석

```csharp
// KATA: Day18
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Katas.Day18
{
    // 1. 가짜 네이티브 엔진: AllocHGlobal을 "SDK 핸들"로 흉내
    public sealed class FakeNativeEngine : IDisposable
    {
        private static int _alive;
        private IntPtr _handle;
        private readonly SemaphoreSlim _inferenceLock = new SemaphoreSlim(1, 1);
        private bool _disposed;
        public int Id { get; private set; }
        public static int Alive { get { return Volatile.Read(ref _alive); } }

        public FakeNativeEngine(int id)
        {
            Id = id;
            _handle = Marshal.AllocHGlobal(1024 * 1024);      // "GPU 모델 로드"
            Interlocked.Increment(ref _alive);
            Thread.Sleep(200);                                  // 생성은 비싸다
        }

        public int[] Inspect(int seed)
        {
            if (_disposed) throw new ObjectDisposedException("engine" + Id);
            _inferenceLock.Wait();
            try
            {
                // 네이티브가 결과를 자기 버퍼에 썼다고 가정 → 관리 배열로 복사
                Marshal.WriteInt32(_handle, 0, seed * 10);
                Marshal.WriteInt32(_handle, 4, Id);
                var result = new int[2];
                Marshal.Copy(_handle, result, 0, 2);
                Thread.Sleep(50);                               // 추론 시간
                return result;
            }
            finally { _inferenceLock.Release(); }
        }

        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing) _inferenceLock.Dispose();            // 관리 자원은 직접 호출일 때만
            if (_handle != IntPtr.Zero)                         // 네이티브는 항상
            {
                Marshal.FreeHGlobal(_handle);
                _handle = IntPtr.Zero;
                Interlocked.Decrement(ref _alive);
            }
            _disposed = true;
        }

        ~FakeNativeEngine() { Dispose(false); }
    }

    // 2. 풀: 좌석 N개
    public sealed class EnginePool : IDisposable
    {
        private readonly List<FakeNativeEngine> _all = new List<FakeNativeEngine>();
        private readonly ConcurrentQueue<FakeNativeEngine> _available = new ConcurrentQueue<FakeNativeEngine>();
        private readonly SemaphoreSlim _seats;

        public EnginePool(int size)
        {
            try
            {
                for (int i = 0; i < size; i++) { var e = new FakeNativeEngine(i); _all.Add(e); _available.Enqueue(e); }
            }
            catch
            {
                foreach (FakeNativeEngine e in _all) e.Dispose();   // 일부만 만들어졌으면 정리
                throw;
            }
            _seats = new SemaphoreSlim(size, size);
        }

        public int[] Inspect(int seed, int timeoutMs, CancellationToken ct)
        {
            if (!_seats.Wait(timeoutMs, ct)) throw new TimeoutException("AI 엔진 대기 " + timeoutMs + "ms 초과");  // OmniSight 개선점
            FakeNativeEngine engine;
            if (!_available.TryDequeue(out engine)) { _seats.Release(); throw new InvalidOperationException("풀 상태 오류"); }
            try { return engine.Inspect(seed); }
            finally { _available.Enqueue(engine); _seats.Release(); }
        }

        public void Dispose()
        {
            foreach (FakeNativeEngine e in _all) e.Dispose();
            _seats.Dispose();
        }
    }

    // 3. 레지스트리: 읽기 잠금으로 사용, 쓰기 잠금으로 교체
    public sealed class EngineRegistry : IDisposable
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private Dictionary<string, EnginePool> _pools = new Dictionary<string, EnginePool>();

        public int[] Inspect(string model, int seed)
        {
            _lock.EnterReadLock();
            try { return _pools[model].Inspect(seed, 2000, CancellationToken.None); }
            finally { _lock.ExitReadLock(); }
        }

        public void Apply(IEnumerable<string> models, int poolSize)
        {
            var staged = new Dictionary<string, EnginePool>();
            try { foreach (string m in models) staged[m] = new EnginePool(poolSize); }   // 잠금 밖에서 비싼 조립
            catch { foreach (EnginePool p in staged.Values) p.Dispose(); throw; }

            Dictionary<string, EnginePool> old;
            _lock.EnterWriteLock();                                 // 사용 중인 읽기가 다 끝날 때까지 대기
            try { old = _pools; _pools = staged; }
            finally { _lock.ExitWriteLock(); }
            foreach (EnginePool p in old.Values) p.Dispose();       // 아무도 안 쓰는 옛 엔진 해제
        }

        public void Dispose()
        {
            foreach (EnginePool p in _pools.Values) p.Dispose();
            _lock.Dispose();
        }
    }

    public static class KataDay18
    {
        public static void Run()
        {
            using (var registry = new EngineRegistry())
            {
                registry.Apply(new[] { "ocr" }, 2);
                Console.WriteLine("살아 있는 엔진: " + FakeNativeEngine.Alive);
                Parallel.For(0, 6, i =>
                {
                    int[] r = registry.Inspect("ocr", i);
                    Console.WriteLine("요청 " + i + " → 결과 " + r[0] + " (엔진 " + r[1] + ")");
                });
                registry.Apply(new[] { "ocr" }, 2);                 // 모델 교체
                Console.WriteLine("교체 후 살아 있는 엔진: " + FakeNativeEngine.Alive + " (2여야 정상)");
            }
            Console.WriteLine("종료 후: " + FakeNativeEngine.Alive + " (0이어야 정상)");

            CreateAndForget();                                       // Dispose 안 함
            Console.WriteLine("Dispose 잊은 직후: " + FakeNativeEngine.Alive + " (1)");
            GC.Collect(); GC.WaitForPendingFinalizers();
            Console.WriteLine("finalizer 회수 후: " + FakeNativeEngine.Alive + " (0. 하지만 '언제' 회수될지는 GC 마음대로)");
        }

        // 별도 메서드로 만들어야 디버그 빌드에서도 참조가 확실히 사라진다
        // (같은 메서드 안이면 JIT가 임시 참조를 메서드 끝까지 살려 둘 수 있음)
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void CreateAndForget() { new FakeNativeEngine(99); }
    }
}
```

## 6. MiniSight 오늘 단계

1. `Inspection/FakeAiEngine.cs : IAiEngine` — Kata의 FakeNativeEngine처럼 가짜 핸들 + Dispose 패턴. `Inspect(ProcessedImage)`: 평균 밝기와 블랍 수로 "AI 점수"를 흉내, 30ms 소요.
2. `Inspection/AiEnginePool.cs` — 풀 크기 = 카메라 수, **대기 타임아웃 + 취소 토큰** 지원(OmniSight 개선).
3. `Inspection/AiEngineRegistry.cs` — Kata 3번. `Apply`는 VisionSystem이 **정지 상태일 때만** 허용(`InvalidOperationException`).
4. Bootstrapper: 레지스트리 생성 → `Apply` → 파이프라인에 `RegistryAiEngine`(레지스트리를 감싼 IAiEngine) 주입. 종료 시 레지스트리 Dispose.
5. 워밍업: `Apply` 후 각 엔진에 검은 영상 한 장 추론, 결과 버림.

## 7. VS2022 디버깅 실습

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | `_seats.Wait` 다음 줄에 중단점, Parallel.For 실행 → **병렬 스택** | 2개 스레드는 추론 중, 나머지는 `SemaphoreSlim.Wait`에서 대기 = 좌석 2개 |
| 2 | 디버그 → 창 → **모듈(Ctrl+Alt+U)** | 로드된 DLL 목록·경로·32/64비트. OpenCV 네이티브 DLL이 어디서 로드됐는지 확인 |
| 3 | 프로젝트 속성 → 빌드 → 플랫폼 대상 **x86**으로 바꾸고 OpenCV 코드 실행 | `BadImageFormatException` 또는 DLL 로드 실패 → 원복(x64) |
| 4 | `[DllImport("nonexistent.dll")] static extern int Foo();` 호출 | `DllNotFoundException` — 메시지에 어떤 이름을 찾았는지 |
| 5 | (확장) 프로젝트 속성 → 디버그 → **네이티브 코드 디버깅 사용** 체크 | 혼합 모드 디버깅: 네이티브 프레임까지 호출 스택에 보임. SDK 크래시 분석 때 사용 |

## 8. 셀프 체크

1. finalizer에서 다른 관리 객체를 건드리면 안 되는 이유는?
<details><summary>답</summary>finalizer 실행 순서는 보장되지 않아 참조하는 객체가 이미 finalize되었을 수 있다. 또 finalizer 스레드에서 예외가 나면 프로세스가 종료된다. 네이티브 핸들 해제만 한다.</details>

2. 엔진에 `SemaphoreSlim(1,1)`이 있는데 풀에도 세마포어가 있는 이유는?
<details><summary>답</summary>엔진 잠금은 "이 엔진 인스턴스의 결과 버퍼 보호"(정확성), 풀 세마포어는 "사용 가능한 엔진 개수 관리"(용량)다. 풀이 엔진 하나를 한 명에게만 빌려주므로 엔진 잠금은 방어선 역할도 한다.</details>

3. 모델 교체 시 새 엔진을 쓰기 잠금 밖에서 만드는 이유는?
<details><summary>답</summary>엔진 생성(모델 로드·GPU 초기화)은 수 초가 걸린다. 쓰기 잠금 안에서 하면 그동안 모든 추론이 멈춘다. 잠금 안에서는 참조 교체만 하고, 비싼 생성과 옛 엔진 해제는 잠금 밖에서 한다.</details>

4. `BadImageFormatException`과 `DllNotFoundException`의 흔한 원인은?
<details><summary>답</summary>BadImageFormat: 프로세스 비트(x86/x64)와 DLL 비트 불일치, 또는 .NET 어셈블리가 아닌 파일을 어셈블리로 로드. DllNotFound: DLL이 검색 경로에 없거나, DLL은 있는데 그 DLL이 의존하는 다른 네이티브 DLL(VC++ 런타임, CUDA)이 없음.</details>

5. 풀 대기에 타임아웃이 없으면 어떤 장애 시나리오가 생기나?
<details><summary>답</summary>SDK 호출 하나가 GPU 행 등으로 영원히 안 돌아오면 그 엔진이 반납되지 않는다. 다른 파이프라인들이 Wait에서 영원히 대기하고, Stop은 작업자 종료를 기다리며 멈추고, UI도 멈춘다. 타임아웃·알람·정지 경로 분리가 필요하다.</details>

## 9. 블로그 포인트

- 제목 예: `[C# Interop] 네이티브 AI 엔진을 안전하게 감싸기: Dispose 패턴 + Object Pool + ReaderWriterLockSlim`
- Kata의 Alive 카운트 출력(2 → 2 → 0 → finalizer)이 좋은 증거. **SDK 이름 금지**, "네이티브 추론 SDK"로.
