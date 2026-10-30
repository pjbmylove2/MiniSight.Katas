# B11 · 다른 일꾼에게 맡기기 (`Thread` `Task.Run` `Task<T>` `async` `await`) · 40분

## 1. 오늘 목표
"이 일은 **다른 일꾼(스레드)**이 하고, 나는 기다리거나 다른 일을 한다"를 코드로 쓰고, **스레드 번호**로 눈으로 확인한다.

## 2. 🧒 비유: 중국집 배달 주문
- 나(**Main 스레드**)는 짜장면을 **직접 만들지 않는다.** 중국집(**스레드 풀**)에 주문한다 (`Task.Run`).
- 주문하면 **주문서(`Task`)**를 받는다. 음식이 아니라 "나중에 음식이 올 거야"라는 영수증이다.
- 기다리는 방법은 두 가지다.
  - **현관 앞에서 꼼짝 않고 기다리기**: `GetAwaiter().GetResult()` → 그동안 나는 아무것도 못 한다.
  - **"오면 불러줘" 하고 다른 일 하기**: `await` → 기다리는 동안 내 손이 자유롭다.
- 두 가게에 **동시에** 주문하면 더 빨리 받는다.

## 3. 키워드 표

| 키워드 | 역할 | 사용법 | 기억할 점 |
|---|---|---|---|
| 스레드 | 코드를 실행하는 **일꾼 한 명** | | 프로그램은 Main 스레드 1명으로 시작 |
| `Thread.CurrentThread.ManagedThreadId` | 지금 이 줄을 실행 중인 일꾼의 **번호** | | 디버깅할 때 아주 유용 |
| `Thread.Sleep(ms)` | 이 일꾼을 ms만큼 **재운다** (아무것도 안 함) | `Thread.Sleep(1000);` | 취소가 안 된다 (B12에서 다룸) |
| `Task` | "끝나면 알려 줄게" **영수증** (결과 없음) | `Task t = Task.Run(() => ...);` | |
| `Task<T>` | 끝나면 **T 결과**를 주는 영수증 | `Task<string> order = ...;` | |
| `Task.Run(() => 일)` | 일을 **스레드 풀**(공용 일꾼들)에게 맡긴다 | `Task.Run(() => Cook("짜장면", 1000))` | 람다(B03)로 "할 일 쪽지"를 넘김 |
| `.GetAwaiter().GetResult()` | 결과가 올 때까지 **지금 스레드를 멈추고** 기다린다 | `string food = order.GetAwaiter().GetResult();` | ⚠️ 콘솔 실습에서만. **WinForms UI 스레드에서 쓰면 멈춤(데드락)** (Day14) |
| `async` | "이 메서드 안에서 `await`를 쓸 거야" 표시 | `private static async Task<string> OrderTwoAsync()` | 이름 끝에 `Async`를 붙이는 관례 |
| `await` | 결과를 기다리되 **스레드를 붙잡지 않는다** | `string ra = await a;` | `async` 메서드 안에서만 사용 |

**Thread와 Task의 차이 (한 줄)**: `new Thread(...)`는 일꾼을 **직접 고용**한다. `Task.Run`은 **공용 일꾼 풀**에서 빌려 쓴다. 대부분 Task를 쓰고, 계속 도는 전용 일꾼만 Thread/LongRunning으로 만든다 (Day08).

## 4. 코드: `Basics/KataB11.cs`

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Katas.Basics
{
    public static class KataB11
    {
        public static void Run()
        {
            Show("주문 전");

            // 1) 다른 일꾼에게 맡기고 영수증(Task)을 받는다
            Task<string> order = Task.Run(() => Cook("짜장면", 1000));
            Show("주문 넣고 숙제하는 중");                // 요리하는 동안 나는 다른 일

            // 2) 올 때까지 여기서 꼼짝 않고 기다림
            string food = order.GetAwaiter().GetResult();
            Show("받음: " + food);

            // 3) 두 가게에 동시에 주문 (async/await)
            string two = OrderTwoAsync().GetAwaiter().GetResult();
            Show("받음: " + two);
        }

        private static string Cook(string menu, int ms)
        {
            Show(menu + " 요리 시작");
            Thread.Sleep(ms);                            // 요리하는 척
            Show(menu + " 요리 끝");
            return menu;
        }

        private static async Task<string> OrderTwoAsync()
        {
            Task<string> a = Task.Run(() => Cook("탕수육", 800));
            Task<string> b = Task.Run(() => Cook("짬뽕", 500));   // 기다리지 않고 바로 두 번째 주문
            string ra = await a;
            string rb = await b;
            return ra + " + " + rb;
        }

        // 지금 몇 번 일꾼이 이 줄을 실행하는지 찍는다
        private static void Show(string text)
        {
            Console.WriteLine("[스레드 " + Thread.CurrentThread.ManagedThreadId + "] " + text);
        }
    }
}
```

## 5. 실행 결과 (검증함, 번호와 일부 순서는 PC마다 다름)
```text
[스레드 1] 주문 전
[스레드 1] 주문 넣고 숙제하는 중
[스레드 3] 짜장면 요리 시작
[스레드 3] 짜장면 요리 끝
[스레드 1] 받음: 짜장면
[스레드 3] 탕수육 요리 시작
[스레드 4] 짬뽕 요리 시작
[스레드 4] 짬뽕 요리 끝
[스레드 3] 탕수육 요리 끝
[스레드 1] 받음: 탕수육 + 짬뽕
```
👀 볼 것 3가지
1. **요리는 다른 번호(3, 4)**가 했다. Main(1)은 주문하고 받기만 했다.
2. 2번째 줄과 3번째 줄은 **순서가 바뀔 수 있다.** 두 일꾼이 동시에 움직이기 때문이다. 이게 멀티스레드의 첫 번째 특징이다.
3. 탕수육(800ms)과 짬뽕(500ms)을 **동시에** 시켜서 1.3초가 아니라 **약 0.8초**에 둘 다 받았다. 짬뽕이 먼저 끝났다.

## 6. 바꿔 보기 (1개)
`OrderTwoAsync`를 아래처럼 바꿔 **하나 받고 나서 다음 주문**하게 한다.
```csharp
string ra = await Task.Run(() => Cook("탕수육", 800));
string rb = await Task.Run(() => Cook("짬뽕", 500));
```
→ "탕수육 요리 끝"이 나온 **뒤에야** "짬뽕 요리 시작"이 나온다. 총 1.3초가 걸린다. "동시에 시키기"와 "차례로 시키기"의 차이다. 확인했으면 되돌린다.

## 7. 어디서 다시 나오나
| Day | 코드 | 뜻 |
|---|---|---|
| Day01 | `TaskScheduler.UnobservedTaskException` | 아무도 결과를 안 받아 간 Task의 예외를 잡는 안전요원 |
| Day04 | `Task.Run(() => _queue.Take(ct), ct)` | 보관함에서 꺼내는 일을 일꾼에게 맡김 |
| Day04 | `WaitFrameAsync(...).GetAwaiter().GetResult()` | 사진 올 때까지 Main이 기다림 |
| Day08 | Thread, LongRunning | 계속 도는 전용 일꾼 |
| Day12 | async/await 깊게 | 타임아웃, ConfigureAwait |

## 8. 셀프 체크
<details><summary>Q1. Task.Run을 부른 직후 결과가 바로 나오나?</summary>

아니다. 영수증(Task)만 받는다. 결과는 GetResult()나 await로 기다려서 받는다.
</details>

<details><summary>Q2. 실행할 때마다 출력 순서가 조금씩 바뀌는 이유는?</summary>

두 스레드가 동시에 움직이고, 누가 먼저 Console에 쓰는지는 운영체제가 그때그때 정하기 때문이다. 그래서 멀티스레드 코드는 "순서가 항상 같다"고 가정하면 안 된다.
</details>

## 9. 체크
- [ ] 1번째: 보면서 타이핑 → 실행
- [ ] 2번째: 실행 결과 비교 (3번 실행해서 순서가 바뀌는지 보기)
- [ ] 3번째: 지우고 뼈대(할 일 주석)만 보고 채우기
- [ ] 바꿔 보기
