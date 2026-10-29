# B03 · 메서드를 변수에 담기 (`Action` `Func` 람다) · 30분

## 1. 오늘 목표
"메서드를 **지금 실행**하지 않고 **변수에 담아 두었다가 나중에** 실행"할 수 있다. Day04의 `Func<ICameraDriver>`가 무엇인지 이해한다.

## 2. 🧒 비유: 심부름 쪽지
- 엄마가 쪽지에 "우유 사 와"라고 **적기만** 한다. 적는 순간에는 아무도 안 움직인다.
- 나중에 누가 쪽지를 **펼치면**(`()`) 그때 우유를 사러 간다.
- 쪽지를 여러 번 펼치면 **여러 번** 간다.
- `Action` = 다녀오기만 하는 쪽지. `Func` = 다녀와서 **뭔가 들고 오는** 쪽지.

## 3. 키워드 표

| 키워드 | 역할 | 사용법 | 기억할 점 |
|---|---|---|---|
| `Action` | 값을 **안 돌려주는** 메서드를 담는 변수 | `Action sayHi = SayHi;` | 실행: `sayHi();` |
| `Action<T>` | 인자 T를 받고 값을 안 돌려줌 | `Action<string> greet = ...;` | 인자는 최대 16개 |
| `Func<TResult>` | 인자 없이 TResult를 **돌려줌** | `Func<Toy> maker = ...;` | **마지막 타입 = 돌려받는 값** |
| `Func<T1, T2, TResult>` | T1, T2를 받고 TResult를 돌려줌 | `Func<int, int, int> add` | (int, int) → int |
| 람다 `x => 식` | 이름 없는 짧은 메서드 | `name => Console.WriteLine(name)` | `=>` 왼쪽은 인자, 오른쪽은 몸통 |
| 람다 `() => 식` | 인자 없는 람다 | `() => new Toy("로봇")` | `()` = 인자 없음 |
| 람다 `(a, b) => { ... }` | 여러 줄 몸통 | `(a, b) => { return a + b; }` | 여러 줄이면 `{ }`와 `return` |
| `SayHi` vs `SayHi()` | 괄호 없으면 **메서드 자체**(쪽지), 괄호 있으면 **지금 실행** | `Action a = SayHi;` | 헷갈리는 1순위 |

**긴 코드로 풀면**
```csharp
Func<int, int, int> add = (a, b) => a + b;

// 위 람다는 아래 메서드를 이름 없이 쓴 것과 같다
private static int Add(int a, int b)
{
    return a + b;
}
Func<int, int, int> add2 = Add;
```

## 4. 코드: `Basics/KataB03.cs`

```csharp
using System;

namespace Katas.Basics
{
    public class Toy
    {
        public Toy(string name)
        {
            Console.WriteLine("  " + name + " 만들어짐");   // 언제 만들어지는지 보려고
        }
    }

    public static class KataB03
    {
        public static void Run()
        {
            // 1) Action: 메서드 이름만 담기 (괄호 없음!)
            Action sayHi = SayHi;
            sayHi();                                   // 이때 실행

            // 2) Action<string>: 람다로 바로 적기
            Action<string> greet = name => Console.WriteLine("안녕, " + name);
            greet("영희");

            // 3) Func: 값을 돌려주는 쪽지. 마지막 int가 돌려받는 값
            Func<int, int, int> add = (a, b) => a + b;
            Console.WriteLine("3 + 4 = " + add(3, 4));

            // 4) Func<Toy>: "만드는 방법"만 담아 둔다
            Func<Toy> maker = () => new Toy("로봇");
            Console.WriteLine("아직 안 만듦");          // 쪽지만 적었으니 아직 안 만들어짐
            Toy t1 = maker();                          // 펼칠 때마다
            Toy t2 = maker();                          // 새로 만든다
            Console.WriteLine("같은 장난감인가? " + ReferenceEquals(t1, t2));
        }

        private static void SayHi()
        {
            Console.WriteLine("안녕!");
        }
    }
}
```
`ReferenceEquals(a, b)`: 두 변수가 **같은 객체 하나**를 가리키면 True.

## 5. 실행 결과 (검증함)
```text
안녕!
안녕, 영희
3 + 4 = 7
아직 안 만듦
  로봇 만들어짐
  로봇 만들어짐
같은 장난감인가? False
```
👀 "아직 안 만듦"이 "로봇 만들어짐"보다 **먼저** 찍혔다. `maker`에 담는 순간에는 만들지 않는다.

## 6. 바꿔 보기 (1개)
`Action sayHi = SayHi;`를 `Action sayHi = SayHi();`로 바꾸고 빌드한다.
→ **CS0029: 암시적으로 'void' 형식을 'System.Action' 형식으로 변환할 수 없습니다.** `SayHi()`는 **지금 실행한 결과**(void = 아무것도 없음)라서 변수에 담을 수 없다. 확인했으면 되돌린다.

## 7. 어디서 다시 나오나
| Day | 코드 | 쪽지 비유 |
|---|---|---|
| Day04 | `Func<ICameraDriver> driverFactory` | "드라이버 만드는 방법" 쪽지. **재연결할 때마다 펼쳐서 새 드라이버**를 받는다 |
| Day04 | `new DriverCameraAdapter(name, () => new FakeCameraDriver(intervalMs))` | 쪽지에 "가짜 드라이버 만들어"라고 적어서 넘김 |
| Day05 | Bootstrapper의 `Func` 주입 | 같은 원리 |
| B11 | `Task.Run(() => Cook(...))` | "요리해" 쪽지를 다른 일꾼에게 넘김 |

## 8. 셀프 체크
<details><summary>Q1. Func&lt;Toy&gt; maker = () =&gt; new Toy("로봇"); 이 줄이 실행될 때 Toy가 만들어지나?</summary>

아니다. 만드는 **방법**만 담는다. `maker()`를 부를 때마다 새로 만들어진다.
</details>

<details><summary>Q2. Day04는 왜 드라이버 객체 대신 Func를 넘겼나?</summary>

연결이 끊겨서 다시 연결할 때 **새 드라이버**가 필요하다. 객체 하나만 넘기면 망가진 걸 계속 쓰게 된다. 쪽지(Func)를 넘기면 필요할 때마다 새로 받을 수 있다. 테스트에서는 쪽지 내용만 가짜로 바꾸면 된다.
</details>

## 9. 체크
- [ ] 1번째: 보면서 타이핑 → 실행
- [ ] 2번째: 실행 결과 비교
- [ ] 3번째: 지우고 안 보고 다시 타이핑
- [ ] 바꿔 보기
