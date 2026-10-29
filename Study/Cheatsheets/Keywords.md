# 키워드·기호 한 줄 사전

> 자세한 설명과 Kata는 "배우는 곳" 링크에 있다. 새 키워드가 나오면 Claude가 이 표에 한 줄씩 추가한다.

## 1. 기초 트랙에서 배우는 키워드 (B01~B14)

| 키워드 | 한 줄 뜻 | 🧒 비유 | 배우는 곳 | 처음 나오는 Day |
|---|---|---|---|---|
| `?.` | 왼쪽이 null이면 멈추고 null | 도시락 있으면 열기 | [B01](../Basics/B01-NullAndArrow.md) | Day04 |
| `??` | 왼쪽이 null이면 오른쪽 값 | 없으면 매점 빵 | [B01](../Basics/B01-NullAndArrow.md) | Day04 |
| `?? throw` | null이면 예외 | | [B01](../Basics/B01-NullAndArrow.md) | Day04 |
| `nameof(x)` | 이름을 문자열로 | | [B01](../Basics/B01-NullAndArrow.md) | Day04 |
| `=>` (식 본문) | 한 줄 속성·메서드 | | [B01](../Basics/B01-NullAndArrow.md) | Day03 |
| `interface` | 할 일 약속만 | 약속 종이 | [B02](../Basics/B02-Inheritance.md) | Day02 |
| `abstract` | 자식이 반드시 채움, `new` 불가 | 반쯤 조립된 키트 | [B02](../Basics/B02-Inheritance.md) | Day03 |
| `virtual` / `override` | 기본 동작 / 바꾸기 | 기본 부품 / 갈아 끼우기 | [B02](../Basics/B02-Inheritance.md) | Day03 |
| `base.X()` | 부모의 원래 버전 호출 | | [B02](../Basics/B02-Inheritance.md) | |
| `protected` | 나와 자식만 | | [B02](../Basics/B02-Inheritance.md) | Day03 |
| `Action` / `Action<T>` | 값 없는 메서드를 담는 변수 | 심부름 쪽지 | [B03](../Basics/B03-ActionFunc.md) | Day04 |
| `Func<..., TResult>` | 값 돌려주는 메서드를 담는 변수 (마지막 = 결과) | 들고 오는 쪽지 | [B03](../Basics/B03-ActionFunc.md) | Day04 |
| 람다 `x => ...` | 이름 없는 짧은 메서드 | | [B03](../Basics/B03-ActionFunc.md) | Day01 |
| `event` | 바깥은 `+=`/`-=`만 가능한 알림 줄 | 초인종 줄 | [B04](../Basics/B04-Event.md) | Day02 |
| `EventHandler<T>` | `(object sender, T 값)` 모양 | | [B04](../Basics/B04-Event.md) | Day02 |
| `+=` / `-=` | 구독 / 해제 | 줄 잡기 / 놓기 | [B04](../Basics/B04-Event.md) | Day04 |
| `Invoke` | 건 메서드를 차례로 실행 (부른 스레드에서) | 버튼 누르기 | [B04](../Basics/B04-Event.md) | Day04 |
| `IDisposable` / `Dispose()` | 다 쓰면 정리 약속 | 빌린 우산 반납 | [B05](../Basics/B05-UsingDispose.md) | Day02 |
| `using (...) { }` | 끝나면 무조건 Dispose | 자동 반납 | [B05](../Basics/B05-UsingDispose.md) | Day01 |
| `try` / `catch` / `finally` | 예외 구역 / 잡기 / 항상 실행 | | [B05](../Basics/B05-UsingDispose.md) | Day01 |
| `throw;` vs `throw ex;` | 위치 유지 / 위치 덮어씀(쓰지 말 것) | | [B05](../Basics/B05-UsingDispose.md) | Day03 |
| `List<T>` | 순서 있는 목록 (`Add` `Remove` `Count` `[i]`) | 줄 선 아이들 | [B06](../Basics/B06-Collections.md) | Day05 |
| `Dictionary<K,V>` | 키로 찾는 사물함 (없는 키 `[ ]` 읽기 = 예외) | 이름표 사물함 | [B06](../Basics/B06-Collections.md) | Day06 |
| `TryGetValue(k, out v)` | 있으면 true + 값, 없으면 false | | [B06](../Basics/B06-Collections.md) | Day06 |
| `out int x` | 메서드가 채워 주는 변수를 그 자리에서 선언 | | [B06](../Basics/B06-Collections.md) | |
| 제네릭 `<T>` | 타입을 나중에 정하는 칸 | 칸에 넣을 물건 종류 | [B06](../Basics/B06-Collections.md) | Day02 |
| `Where` / `Select` | 거르기 / 바꾸기 | 체 / 베껴 적기 | [B07](../Basics/B07-Linq.md) | Day05 |
| `FirstOrDefault` vs `First` | 없으면 null / 없으면 예외 | | [B07](../Basics/B07-Linq.md) | Day05 |
| `Any` `Count` `Average` `Max` `OrderBy` `ToList` | 있나 / 개수 / 평균 / 최대 / 정렬 / 지금 계산해서 List로 | | [B07](../Basics/B07-Linq.md) | |
| `$"{x}"` | 문자열 보간 | 빈칸 채우기 | [B08](../Basics/B08-StringTime.md) | Day01 |
| `{x:F2}` `{x:D4}` `{t:yyyy-MM-dd HH:mm:ss}` | 소수 2자리 / 0 채우기 4자리 / 날짜 (MM=월, mm=분) | | [B08](../Basics/B08-StringTime.md) | |
| `Stopwatch` | 처리 시간 측정 | 초시계 | [B08](../Basics/B08-StringTime.md) | Day13 |
| `DateTime` / `TimeSpan` | 시각 / 시간의 길이 | | [B08](../Basics/B08-StringTime.md) | |
| `CultureInfo.InvariantCulture` | 나라 상관없이 `.`을 소수점으로 | 숫자 쓰는 나라 규칙 | [B08](../Basics/B08-StringTime.md) | Day23 |
| `enum` | 정해진 이름 목록 | 도장 세트 | [B09](../Basics/B09-SwitchPattern.md) | Day03 |
| `switch` / `default` | 갈림길 / 나머지 전부 | 표지판 | [B09](../Basics/B09-SwitchPattern.md) | Day04 |
| `o is string s` | 타입 확인 + 변수 담기 | 라벨 보고 꺼내기 | [B09](../Basics/B09-SwitchPattern.md) | |
| `catch (X ex) when (조건)` | 조건 맞을 때만 잡기 | 조건 붙은 그물 | [B09](../Basics/B09-SwitchPattern.md) | Day22 |
| `static` / `readonly` / `const` | 클래스에 하나 / 생성자에서만 / 컴파일 때 고정 | 반 칠판 / 이름표 / 새긴 숫자 | [B09](../Basics/B09-SwitchPattern.md) | Day01 |
| `partial class` | 한 클래스를 여러 파일에 | | [B09](../Basics/B09-SwitchPattern.md) | Day19 |
| `Path.Combine` | 경로 이어 붙이기 | 주소 쓰기 | [B10](../Basics/B10-FileBytes.md) | Day04 |
| `File.*` / `Directory.*` | 파일 읽기·쓰기 / 폴더 | | [B10](../Basics/B10-FileBytes.md) | Day04 |
| `byte[]` / `0x3E8` | 바이트 배열 / 16진수 | 작은 상자들 | [B10](../Basics/B10-FileBytes.md) | Day22 |
| `BitConverter` | 숫자 ↔ 바이트 | | [B10](../Basics/B10-FileBytes.md) | Day22 |
| 엔디언 | 바이트 놓는 순서 (PC=리틀, 네트워크=빅) | 상자 놓는 순서 | [B10](../Basics/B10-FileBytes.md) | Day22 |
| `Buffer.BlockCopy` | 바이트 배열 일부 복사 | | [B10](../Basics/B10-FileBytes.md) | Day22 |
| `Thread.Sleep(ms)` | 일꾼 재우기 (취소 불가) | | [B11](../Basics/B11-Task.md) | Day03 |
| `Task` / `Task<T>` | 나중에 끝나는 일의 영수증 | 배달 영수증 | [B11](../Basics/B11-Task.md) | Day01 |
| `Task.Run(() => ...)` | 스레드 풀에 일 맡기기 | 배달 주문 | [B11](../Basics/B11-Task.md) | Day04 |
| `.GetAwaiter().GetResult()` | 끝날 때까지 지금 스레드 멈춰 기다림 (UI 스레드 금지) | 현관 앞에서 기다리기 | [B11](../Basics/B11-Task.md) | Day04 |
| `async` / `await` | 스레드를 붙잡지 않고 기다림 | "오면 불러줘" | [B11](../Basics/B11-Task.md) | Day12 |
| `CancellationTokenSource` | 취소를 시키는 쪽 | 리모컨 | [B12](../Basics/B12-Cancellation.md) | Day03 |
| `CancellationToken` | 취소를 확인하는 쪽 (확인해야 멈춤) | 수신기 | [B12](../Basics/B12-Cancellation.md) | Day03 |
| `ThrowIfCancellationRequested()` | 취소됐으면 `OperationCanceledException` | 불 켜졌나 확인 | [B12](../Basics/B12-Cancellation.md) | Day04 |
| `CancelAfter(ms)` | ms 뒤 자동 취소 (타임아웃) | 타이머 정지 | [B12](../Basics/B12-Cancellation.md) | Day12 |
| `lock (obj) { }` | 한 번에 한 스레드만 | 분필 하나 | [B13](../Basics/B13-LockInterlocked.md) | Day03 |
| `Interlocked.Increment(ref x)` | 끊김 없이 +1, 새 값 반환 | 번호표 기계 | [B13](../Basics/B13-LockInterlocked.md) | Day04 |
| `ref` | 복사본이 아니라 변수 자체를 넘김 | 칠판 자체를 건넴 | [B13](../Basics/B13-LockInterlocked.md) | Day04 |
| `System.Threading.Timer` | 간격마다 스레드 풀에서 콜백 | 알람 시계 | [B14](../Basics/B14-TimerCamera.md) | Day04 |
| `Timeout.Infinite` | -1, 타이머에서는 "안 울림" | | [B14](../Basics/B14-TimerCamera.md) | Day04 |
| `sealed` | 상속 금지 | 완성품 | [B14](../Basics/B14-TimerCamera.md) | Day04 |

## 2. 본편 Day에서 배우는 키워드

기초 트랙을 마친 뒤 해당 Day에서 처음부터 표로 설명한다. 사용 수는 OmniSight 소스 기준(2026-10-28, 236개 .cs 파일, 대략값)이다.

| 키워드 | 한 줄 뜻 | OmniSight 사용 수 | 배우는 Day |
|---|---|---|---|
| `new Thread` / `TaskCreationOptions.LongRunning` | 전용 일꾼 고용 | 2 / 9 | Day08 |
| `token.WaitHandle.WaitOne(ms)` | 취소 가능한 sleep | | Day08 |
| `volatile` | 다른 스레드의 최신 값을 보장해서 읽기 | 28 | Day09 |
| `Monitor.Enter/Exit/Wait/Pulse` | lock의 본체 + 신호 주고받기 | 33 | Day09 |
| `ManualResetEventSlim` | 문 열림/닫힘 신호 | 21 | Day09 |
| `BlockingCollection<T>` | 꽉 차면 넣는 쪽이 기다리는 줄 | 21 | Day10 |
| `ConcurrentQueue<T>` / `ConcurrentDictionary` | 여러 스레드가 같이 써도 되는 큐/사전 | 7 / 4 | Day10 |
| `SemaphoreSlim` | 동시에 들어갈 수 있는 인원 제한 (async 가능) | 8 | Day11 |
| `Task.Delay` / `Task.WhenAny` | 기다리는 Task / 먼저 끝난 하나 | 17 / 2 | Day12 |
| `TaskCompletionSource<T>` | 콜백을 Task로 바꾸기 | 3 | Day12 |
| `ConfigureAwait(false)` | await 뒤에 원래 스레드로 안 돌아와도 됨 | 31 | Day12 |
| `Lazy<T>` | 처음 쓸 때 만들기 | 6 | Day05 |
| `where T : class` | 제네릭 제약 | 10 | Day16 |
| `JsonConvert` (Newtonsoft.Json) | 객체 ↔ JSON | 57 | Day16, Day23 |
| `[DllImport]` / `IntPtr` / `Marshal` | C/C++ DLL 호출 / 포인터 / 메모리 변환 | 69 / 198 / 19 | Day18 |
| `ReaderWriterLockSlim` | 읽기는 여럿, 쓰기는 혼자 | 4 | Day18 |
| `InvokeRequired` / `BeginInvoke` | 화면은 UI 스레드에서만 | 19 / 22 | Day19 |
| `async void` | 이벤트 핸들러에서만 허용 | 13 | Day20 |
| `Socket` / `TcpClient` / `NetworkStream` | TCP 통신 | 587 / 23 / 5 | Day22 |
| `GC.Collect` / `GC.SuppressFinalize` | 가비지 수집 / 소멸자 생략 | 18 | Day03, Day18 |
