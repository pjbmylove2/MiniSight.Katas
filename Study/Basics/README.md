# 0주차 · 기초 체력 트랙 (B01~B14)

> 왜 생겼나 (2026-10-28): Day01~04를 해 보니 **디자인 패턴이 어려운 게 아니라, 패턴을 만드는 재료(키워드)를 손으로 써 본 적이 없는 것**이 문제였다. 코드는 많이 봤지만 복사·붙여넣기만 했다.
> 그래서 Day01을 다시 시작하기 전에 **키워드 하나에 작은 콘솔 Kata 하나씩** 직접 타이핑한다. 디자인 패턴은 나오지 않는다.

## 1. 순서와 일정 (평일 2시간 기준, 7일)

**1부 · 한 명이 일할 때 (B01~B10)**: 스레드 없이 C# 문법과 현장에서 매일 쓰는 도구를 익힌다.
**2부 · 여럿이 같이 일할 때 (B11~B14)**: 스레드, 취소, 동기화, 타이머를 익힌다.

| 기초 날 | Kata | 키워드 | 🧒 비유 | 처음 쓰이는 Day |
|---|---|---|---|---|
| 1일 | [B01](B01-NullAndArrow.md) | `?.` `??` `throw` 식 `nameof` `=>` | 도시락 확인 | Day02~04 |
| 1일 | [B02](B02-Inheritance.md) | `interface` `abstract` `virtual` `override` `base` `protected` | 동물 소리 키트 | Day02, Day03 |
| 2일 | [B03](B03-ActionFunc.md) | `Action` `Func` 람다 `() =>` | 심부름 쪽지 | Day04, Day05 |
| 2일 | [B04](B04-Event.md) | `event` `EventHandler<T>` `+=` `-=` `Invoke` | 초인종 | Day02, Day04 |
| 3일 | [B05](B05-UsingDispose.md) | `IDisposable` `using` `try` `catch` `finally` `throw` | 빌린 우산 | Day01~04 |
| 3일 | [B06](B06-Collections.md) | `List<T>` `Dictionary<K,V>` 제네릭 `<T>` `TryGetValue` `out var` | 줄 선 아이들, 이름표 사물함 | Day05, Day06 |
| 4일 | [B07](B07-Linq.md) | LINQ `Where` `Select` `FirstOrDefault` `Any` `Count` `OrderBy` `ToList` | 체에 거르기 | Day05~ |
| 4일 | [B08](B08-StringTime.md) | `$""` 형식 지정 `Stopwatch` `DateTime` `TimeSpan` `CultureInfo` | 빈칸 채우기, 초시계 | Day13, Day23, Day24 |
| 5일 | [B09](B09-SwitchPattern.md) | `enum` `switch` `is` 패턴 `catch when` `static` `readonly` `const` `partial` | 도장, 갈림길 표지판 | Day03, Day04, Day06 |
| 5일 | [B10](B10-FileBytes.md) | `Path` `File` `Directory` `byte[]` `BitConverter` 엔디언 `Buffer.BlockCopy` | 주소 쓰기, 상자 놓는 순서 | Day04, Day22, Day23 |
| 6일 | [B11](B11-Task.md) | `Thread` `Task.Run` `Task<T>` `async` `await` `GetAwaiter().GetResult()` | 배달 주문 | Day01, Day04, Day08 |
| 6일 | [B12](B12-Cancellation.md) | `CancellationTokenSource` `CancellationToken` `Cancel` `ThrowIfCancellationRequested` `CancelAfter` | 세탁기 정지 버튼 | Day03, Day08 |
| 7일 | [B13](B13-LockInterlocked.md) | `lock` `Interlocked` `ref` `static readonly object` | 번호표 기계 | Day03, Day04, Day09 |
| 7일 | [B14](B14-TimerCamera.md) | `System.Threading.Timer` `Change` `Timeout.Infinite` + B01~B13 합치기 | 패턴 없는 50줄 가짜 카메라 | Day04 |

7일째가 끝나면 **졸업 시험**(§4)을 보고 Day01부터 다시 시작한다. 하루에 2개가 버거우면 1개씩 해도 된다 (14일).

### 기초 트랙에 넣지 않은 키워드 (본편 Day에서 배운다)
OmniSight를 다루려면 아래도 필요하지만, **기초 14개가 손에 익어야 읽히는 것들**이라 해당 Day에서 처음부터 설명한다. 전체 목록은 [Cheatsheets/Keywords.md](../Cheatsheets/Keywords.md) §2에 있다.

| 키워드 | OmniSight 사용 수 | 배우는 Day |
|---|---|---|
| `volatile` `Monitor` `ManualResetEventSlim` | 28 / 33 / 21 | Day09 |
| `BlockingCollection` `ConcurrentQueue` `SemaphoreSlim` | 21 / 7 / 8 | Day10, Day11 |
| `TaskCompletionSource` `ConfigureAwait` `Task.Delay` `LongRunning` | 3 / 31 / 17 / 9 | Day08, Day12 |
| `DllImport` `IntPtr` `Marshal` `ReaderWriterLockSlim` | 69 / 198 / 19 / 4 | Day18 |
| `InvokeRequired` `BeginInvoke` `async void`(이벤트 핸들러) | 19 / 22 / 13 | Day19, Day20 |
| `Socket` `TcpClient` `NetworkStream` | 587 / 23 / 5 | Day22 |
| `JsonConvert`(Newtonsoft) | 57 | Day16, Day23 |
| `Lazy<T>` `where T :` (제네릭 제약) | 6 / 10 | Day05, Day16 |

## 2. 진행 규칙 (모든 Kata 공통)

1. **처음 나오는 키워드는 표로 설명한다.** 역할, 사용법, 같은 뜻의 긴 코드를 함께 적는다.
2. **한 줄짜리 `if`/`for`/`foreach`/`while`에도 무조건 중괄호 `{ }`를 쓴다.** (이유: [B01 §6](B01-NullAndArrow.md#6-중괄호-규칙-goto-fail))
3. **3번 타이핑 (복사·붙여넣기 금지)**
   - 1번째: 문서를 **보면서** 친다.
   - 2번째: 실행해서 "실행 결과"와 똑같은지 비교한다.
   - 3번째: 파일 내용을 **지우고 뼈대만 보고 채운다.** 뼈대는 Claude가 주는 "할 일 주석 + 기대 출력"이고 코드는 비어 있다. 뼈대는 보면서 쳐도 되고, 주석 아래 코드만 원래 코드를 안 보고 채운다. 막히면 ① 키워드 표 → ② 원래 코드의 **그 한 줄만** 순서로 본다. 본 줄은 PROGRESS에 적는다. ← 이게 진짜 연습
     (B06까지는 "통째로 안 보고 다시 치기"였다. 2026-10-29 B07부터 코드가 길어져 암기 시험이 되지 않도록 바꿨다. "무엇을 할지 알 때 키워드로 바꿔 쓰기"를 연습하는 단계다)
4. **바꿔 보기는 1개만** 한다. 확인했으면 되돌린다.
5. **셀프 체크 2문항**을 말로 답한다. 70% 정도면 넘어가고, 모르는 건 PROGRESS의 "다시 물어볼 질문"에 적는다.

## 3. 프로젝트 준비 (처음 한 번)

기존 `MiniSight.Katas` 콘솔 프로젝트(.NET Framework 4.7.2, x64)를 그대로 쓴다.

1. 솔루션 탐색기 → `MiniSight.Katas` 우클릭 → **추가 → 새 폴더** → `Basics`
2. Kata마다 `Basics` 폴더 우클릭 → **추가 → 클래스** → `KataB01.cs` 같은 이름으로 만든다.
3. `Program.cs`의 `Main`은 그날 할 Kata만 부른다. 나머지는 주석 처리한다.

```csharp
static void Main(string[] args)
{
    Basics.KataB01.Run();
    // Basics.KataB02.Run();
    Console.WriteLine("끝. 아무 키나 누르세요.");
    Console.ReadKey();
}
```

한글이 깨지면 `Main` 첫 줄에 `Console.OutputEncoding = System.Text.Encoding.UTF8;`을 넣는다.

> 이 문서의 Kata 코드는 C# 7.3 / .NET Framework 4.7.2 참조 어셈블리로 **실제 컴파일하고 실행해서** 출력을 확인했다 (2026-10-28). 스레드 번호와 일부 순서는 PC마다 다를 수 있다.

## 4. 졸업 시험 (기초 7일째)

- [ ] B14 `SimpleCamera`를 **문서를 안 보고** 15분 안에 쳐서 실행했다.
- [ ] 아래 8문장을 내 말로 설명했다.
  1. `?.Invoke`는 무엇을 막아 주나?
  2. `Func<T>`를 넘기면 객체는 **언제** 만들어지나?
  3. `using`은 예외가 나도 `Dispose`를 부르나? 왜?
  4. `dict["키"]`와 `TryGetValue`는 키가 없을 때 어떻게 다른가?
  5. 설정 파일의 `"10.5"`를 읽을 때 왜 `CultureInfo.InvariantCulture`를 쓰나?
  6. PLC에서 받은 4바이트 값이 엉뚱하면 무엇부터 의심하나?
  7. `CancellationToken`은 `Cancel()`만 하면 일이 멈추나?
  8. `_count++`가 두 스레드에서 틀리는 이유는?
- [ ] [Day04 그림](../Days/Day04.md)을 보고 "B14 카메라를 정해진 자리(어댑터·팩토리)에 넣은 것"이라고 설명했다.

통과하면 Claude에게 **"기초 트랙 졸업, Day 01 다시 시작"**이라고 말한다.
