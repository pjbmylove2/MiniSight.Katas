# B12 · 멈추라고 알리기 (`CancellationTokenSource` `CancellationToken`) · 30분

## 1. 오늘 목표
다른 스레드에서 도는 일을 **안전하게 멈추라고 알리는** 코드를 쓴다. 그리고 "토큰은 **일하는 쪽이 확인해야** 멈춘다"를 직접 확인한다 (Day03에서 막혔던 부분).

## 2. 🧒 비유: 세탁기 정지 버튼
- **리모컨** = `CancellationTokenSource`. 정지 버튼이 달려 있다. 들고 있는 사람만 누를 수 있다.
- **세탁기 안의 수신기** = `CancellationToken`. 세탁기에 넘겨준다.
- 버튼을 누름 = `Cancel()`. 수신기에 **불이 켜질 뿐**이다.
- 세탁기가 1분마다 **불이 켜졌나 확인**(`ThrowIfCancellationRequested`)해야 멈춘다. **확인하지 않으면 버튼을 눌러도 끝까지 돈다.**
- **타이머 정지** = `CancelAfter(ms)`. "0.7초 뒤에 자동으로 버튼 눌러 줘." (타임아웃)

## 3. 키워드 표

| 키워드 | 역할 | 사용법 | 기억할 점 |
|---|---|---|---|
| `CancellationTokenSource` | **리모컨**. 취소를 **시키는** 쪽 | `var remote = new CancellationTokenSource();` | `IDisposable` → `using`으로 감싼다 |
| `.Token` | 리모컨과 짝인 **수신기**를 꺼낸다 | `CancellationToken token = remote.Token;` | 일하는 쪽에 **토큰만** 넘긴다 (리모컨은 안 줌) |
| `CancellationToken` | **수신기**. 취소됐는지 **확인하는** 쪽 | 메서드 인자 `(CancellationToken token)` | 구조체. 확인만 할 수 있고 취소는 못 시킴 |
| `.Cancel()` | 정지 버튼 누르기 | `remote.Cancel();` | 불만 켠다. 일을 강제로 끊지 않는다 |
| `.CancelAfter(ms)` | ms 뒤에 자동으로 Cancel | `timeout.CancelAfter(700);` | 타임아웃 만들 때 |
| `.IsCancellationRequested` | 불이 켜졌나? (true/false) | `if (token.IsCancellationRequested) { ... }` | 예외 없이 조용히 끝내고 싶을 때 |
| `.ThrowIfCancellationRequested()` | 불이 켜졌으면 **`OperationCanceledException`을 던진다** | 반복문 맨 앞에 한 줄 | 가장 흔한 확인 방법 |
| `OperationCanceledException` | "취소돼서 멈췄음" 예외 | `catch (OperationCanceledException) { }` | 오류가 아니라 **정상적인 멈춤** 신호 |
| `CancellationToken.None` | 절대 취소되지 않는 토큰 | `Initialize(CancellationToken.None)` | 취소 기능이 필요 없을 때 |

**`ThrowIfCancellationRequested()`를 길게 풀면**
```csharp
if (token.IsCancellationRequested)
{
    throw new OperationCanceledException(token);
}
```

## 4. 코드: `Basics/KataB12.cs`

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Katas.Basics
{
    public static class KataB12
    {
        public static void Run()
        {
            Console.WriteLine("1) 정지 버튼");
            using (CancellationTokenSource remote = new CancellationTokenSource())   // 리모컨
            {
                CancellationToken token = remote.Token;                              // 수신기
                Task work = Task.Run(() => Wash(token));                             // 세탁기는 다른 일꾼

                Thread.Sleep(1000);
                Console.WriteLine("정지 버튼 누름!");
                remote.Cancel();

                try
                {
                    work.GetAwaiter().GetResult();       // 세탁기가 멈출 때까지 기다림
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("세탁기: 취소되어 멈췄음");
                }
            }

            Console.WriteLine("2) 시간 지나면 자동 정지");
            using (CancellationTokenSource timeout = new CancellationTokenSource())
            {
                timeout.CancelAfter(700);                 // 0.7초 뒤 자동 Cancel
                try
                {
                    Wash(timeout.Token);                  // 이번엔 Main이 직접 세탁
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("타임아웃으로 멈췄음");
                }
            }
        }

        private static void Wash(CancellationToken token)
        {
            for (int i = 1; i <= 10; i++)
            {
                token.ThrowIfCancellationRequested();     // 1분마다 불 확인
                Console.WriteLine("  세탁 " + i + "분");
                Thread.Sleep(300);
            }
            Console.WriteLine("  세탁 완료");
        }
    }
}
```

## 5. 실행 결과 (검증함, 몇 분까지 찍히는지는 ±1 차이 날 수 있음)
```text
1) 정지 버튼
  세탁 1분
  세탁 2분
  세탁 3분
  세탁 4분
정지 버튼 누름!
세탁기: 취소되어 멈췄음
2) 시간 지나면 자동 정지
  세탁 1분
  세탁 2분
  세탁 3분
타임아웃으로 멈췄음
```
👀 버튼을 누른 **즉시** 멈추지 않는다. 세탁기가 자고 있던 0.3초가 끝나고 **다음 확인 줄에 도착했을 때** 멈춘다.

## 6. 바꿔 보기 (1개) ← Day03 복습
`Wash`의 `token.ThrowIfCancellationRequested();` 줄을 주석 처리하고 실행한다.
→ "정지 버튼 누름!" 뒤에도 **세탁 10분과 "세탁 완료"까지** 다 찍힌다. 리모컨을 눌러도 **세탁기가 확인하지 않으면 아무 소용이 없다.** Day03에서 "CancellationToken은 직접 확인해야 동작한다"고 한 이유가 이것이다. 확인했으면 되돌린다.

💡 `Thread.Sleep(300)` 동안에는 확인을 못 해서 최대 0.3초 늦게 멈춘다. 자는 중에도 버튼에 바로 반응하는 "취소 가능한 sleep"(`token.WaitHandle.WaitOne(300)`)은 Day08에서 한다.

## 7. 어디서 다시 나오나
| Day | 코드 | 뜻 |
|---|---|---|
| Day03 | `OnInitialize(CancellationToken cancellationToken)` | 연결 도중 멈출 수 있게 수신기를 넘김 |
| Day04 | `cancellationToken.ThrowIfCancellationRequested();` | 드라이버 만들기 전에 불 확인 |
| Day04 | `_queue.Take(ct)` | 사진 기다리는 중에도 버튼에 반응 |
| Day08 | 취소 가능한 sleep | 자는 중에도 반응 |
| Day12 | 타임아웃 Connect (`CancelAfter`) | 연결이 5초 넘으면 포기 |

## 8. 셀프 체크
<details><summary>Q1. Cancel()을 부르면 다른 스레드의 일이 즉시 끊기나?</summary>

아니다. 수신기에 불만 켠다. 일하는 코드가 토큰을 확인하는 줄에 도착해야 멈춘다. 확인하는 줄이 없으면 끝까지 돈다.
</details>

<details><summary>Q2. 왜 일하는 쪽에 리모컨(Source)이 아니라 토큰만 넘기나?</summary>

일하는 쪽은 "멈추라는 말을 듣기만" 하면 된다. 리모컨까지 주면 아무나 취소를 시킬 수 있어서 누가 멈췄는지 추적하기 어렵다. 역할을 나눈 것이다.
</details>

## 9. 체크
- [ ] 1번째: 보면서 타이핑 → 실행
- [ ] 2번째: 실행 결과 비교
- [ ] 3번째: 지우고 안 보고 다시 타이핑
- [ ] 바꿔 보기
