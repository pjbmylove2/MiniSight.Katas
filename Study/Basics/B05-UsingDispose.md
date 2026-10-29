# B05 · 반드시 정리하기 (`IDisposable` `using` `try` `catch` `finally` `throw`) · 30분

## 1. 오늘 목표
**예외가 나도 빌린 것은 반드시 돌려주는** 코드를 쓴다. 카메라, 파일, Mat처럼 "다 쓰면 돌려줘야 하는 것"의 기본기다.

## 2. 🧒 비유: 빌린 우산
- 우산을 빌리면 **반드시 반납**해야 한다 (`Dispose`).
- 쓰다가 바람에 뒤집혀도(예외) **반납은 해야** 한다 → `finally`.
- `using`은 "다 쓰면 자동으로 반납해 줘"라는 **자동 반납 약속**이다.

## 3. 키워드 표

| 키워드 | 역할 | 사용법 | 기억할 점 |
|---|---|---|---|
| `IDisposable` | "나는 다 쓰면 `Dispose()`로 정리해야 해"라는 약속 (interface) | `class Umbrella : IDisposable` | 메서드는 `void Dispose()` 하나 |
| `using (...) { }` | 블록이 끝나면 **무조건** `Dispose()` 호출 | `using (var u = new Umbrella("A")) { ... }` | 예외가 나도, `return`해도 부른다 |
| `try { }` | 예외가 날 수 있는 구역 | | 혼자는 못 쓰고 `catch`나 `finally`와 짝 |
| `catch (타입 ex) { }` | 그 타입의 예외를 **잡아서** 처리 | `catch (InvalidOperationException ex)` | 잡으면 프로그램이 안 죽고 계속 간다 |
| `finally { }` | 예외가 나든 안 나든 **항상** 실행 | `finally { y.Dispose(); }` | 정리 코드 자리 |
| `throw new X(...)` | 예외를 **던진다** | `throw new InvalidOperationException("...");` | |
| `throw;` | catch 안에서 **잡은 예외를 그대로** 다시 던진다 | `catch (Exception) { 로그; throw; }` | 원래 위치(스택 추적) 유지 |
| `throw ex;` | ⚠️ 같은 예외를 던지지만 **위치 정보가 여기로 덮인다** | | 쓰지 말 것 (Day03) |

**`using`을 길게 풀면** (컴파일러가 실제로 이렇게 바꾼다)
```csharp
Umbrella u = new Umbrella("파란 우산");
try
{
    u.Use(false);
}
finally
{
    if (u != null)
    {
        u.Dispose();
    }
}
```

## 4. 코드: `Basics/KataB05.cs`

```csharp
using System;

namespace Katas.Basics
{
    public class Umbrella : IDisposable
    {
        private readonly string _name;

        public Umbrella(string name)
        {
            _name = name;
            Console.WriteLine("  " + _name + " 빌림");
        }

        public void Use(bool rainTooHard)
        {
            if (rainTooHard)
            {
                throw new InvalidOperationException("비가 너무 많이 와서 우산이 뒤집힘");
            }
            Console.WriteLine("  " + _name + " 쓰고 걸어감");
        }

        public void Dispose()
        {
            Console.WriteLine("  " + _name + " 반납");
        }
    }

    public static class KataB05
    {
        public static void Run()
        {
            Console.WriteLine("1) using");
            using (Umbrella u = new Umbrella("파란 우산"))
            {
                u.Use(false);
            }                                           // 여기서 자동 반납

            Console.WriteLine("2) 예외가 나도 using은 반납한다");
            try
            {
                using (Umbrella u = new Umbrella("빨간 우산"))
                {
                    u.Use(true);                        // 여기서 예외!
                    Console.WriteLine("  (이 줄은 실행 안 됨)");
                }                                       // 그래도 반납은 한다
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("  잡은 예외: " + ex.Message);
            }

            Console.WriteLine("3) using을 풀어 쓰면");
            Umbrella y = new Umbrella("노란 우산");
            try
            {
                y.Use(false);
            }
            finally
            {
                y.Dispose();
            }
        }
    }
}
```

## 5. 실행 결과 (검증함)
```text
1) using
  파란 우산 빌림
  파란 우산 쓰고 걸어감
  파란 우산 반납
2) 예외가 나도 using은 반납한다
  빨간 우산 빌림
  빨간 우산 반납
  잡은 예외: 비가 너무 많이 와서 우산이 뒤집힘
3) using을 풀어 쓰면
  노란 우산 빌림
  노란 우산 쓰고 걸어감
  노란 우산 반납
```
👀 2번에서 **"반납"이 "잡은 예외"보다 먼저** 찍혔다. 예외가 `catch`로 날아가는 길에 `using`의 `finally`를 먼저 지나가기 때문이다.

## 6. 바꿔 보기 (1개)
2번의 `using (Umbrella u = new Umbrella("빨간 우산"))`를 `Umbrella u = new Umbrella("빨간 우산");`로 바꾼다 (블록 중괄호는 그대로 두어도 된다).
→ "빨간 우산 반납"이 **안 찍힌다.** 장비 프로그램이라면 카메라·파일 핸들이 **샌 것(누수)**이다. 확인했으면 되돌린다.

## 7. 어디서 다시 나오나
| Day | 코드 | 뜻 |
|---|---|---|
| Day01 | `using (var mutex = ...)` | 단일 실행 열쇠 자동 반납 |
| Day02 | `ImageFrame : IDisposable` | 사진 상자 반납 |
| Day03 | `catch { State = Faulted; throw; }` | 상태 기록하고 **그대로** 다시 던짐 |
| Day04 | `try { ... } finally { frame?.Dispose(); mat?.Dispose(); }` | 넘기지 못하고 손에 남은 것만 반납 |

## 8. 셀프 체크
<details><summary>Q1. using 블록 안에서 예외가 나면 Dispose는 불리나?</summary>

불린다. using은 try/finally로 바뀌고, finally는 예외가 나도 실행된다.
</details>

<details><summary>Q2. catch 안에서 throw; 와 throw ex; 의 차이는?</summary>

`throw;`는 원래 예외가 난 위치(스택 추적)를 그대로 유지한다. `throw ex;`는 위치가 catch 줄로 덮여서, 로그를 봐도 진짜 원인 줄을 찾기 어렵다.
</details>

## 9. 체크
- [ ] 1번째: 보면서 타이핑 → 실행
- [ ] 2번째: 실행 결과 비교
- [ ] 3번째: 지우고 안 보고 다시 타이핑
- [ ] 바꿔 보기
