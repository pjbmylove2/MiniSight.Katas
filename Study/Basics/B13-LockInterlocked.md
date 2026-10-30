# B13 · 같이 쓰면 망가진다 (`lock` `Interlocked` `ref`) · 30분

## 1. 오늘 목표
두 스레드가 **같은 변수**를 고치면 숫자가 틀린다는 것을 **직접 눈으로** 보고, `lock`과 `Interlocked`로 고친다.

## 2. 🧒 비유: 칠판 숫자 세기
- 칠판에 숫자가 적혀 있다. "1 더하기"는 사실 **3단계**다: ① 칠판 숫자 **읽기** → ② 머릿속으로 +1 → ③ 칠판에 **쓰기**.
- 철수와 영희가 **동시에** 한다. 둘 다 ①에서 "5"를 읽고, 둘 다 ③에서 "6"을 쓴다. **두 번 더했는데 한 번만 늘었다.**
- **`lock` = 분필 하나**: 분필을 든 사람만 칠판에 갈 수 있다. 다른 사람은 줄 서서 기다린다.
- **`Interlocked` = 버튼식 번호표 기계**: 버튼을 누르면 기계가 ①②③을 **한 번에, 끊김 없이** 해 준다.

## 3. 키워드 표

| 키워드 | 역할 | 사용법 | 기억할 점 |
|---|---|---|---|
| 경쟁 상태 (race) | 여러 스레드가 같은 값을 동시에 고쳐서 **결과가 틀리는** 것 | | 가끔만 틀려서 찾기 어렵다 |
| `_count++` | 읽기 → +1 → 쓰기 **3단계** | | 한 줄이지만 한 번에 안 끝난다 |
| `lock (obj) { }` | `{ }` 안에는 **한 번에 한 스레드만** 들어간다 | `lock (_gate) { _count++; }` | 안을 **짧게**. 안에서 오래 기다리면 다른 스레드가 다 멈춘다 |
| `private static readonly object _gate = new object();` | lock에 쓰는 **분필 전용 물건** | | 바깥에서 못 잡게 `private`. 바뀌지 않게 `readonly` |
| `Interlocked.Increment(ref x)` | x를 **한 번에** +1 하고 **새 값을 돌려준다** | `long id = Interlocked.Increment(ref _frameId);` | 숫자 하나 더하기·빼기는 lock보다 가볍다 |
| `ref` | 값의 **복사본이 아니라 변수 자체**를 넘긴다 | `Interlocked.Increment(ref _count)` | 칠판 사진이 아니라 **칠판 자체**를 건넴 |
| `Task.WaitAll(a, b)` | a, b 둘 다 끝날 때까지 기다림 | | |
| `static` 필드 | 객체마다가 아니라 **클래스에 하나만** 있는 변수 | `private static int _count;` | 모든 스레드가 같은 칠판을 본다 |

**`lock`을 길게 풀면** (컴파일러가 이렇게 바꾼다)
```csharp
bool taken = false;
try
{
    Monitor.Enter(_gate, ref taken);   // 분필 잡기 (없으면 기다림)
    _count++;
}
finally
{
    if (taken)
    {
        Monitor.Exit(_gate);           // 예외가 나도 분필 내려놓기
    }
}
```

## 4. 코드: `Basics/KataB13.cs`

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Katas.Basics
{
    public static class KataB13
    {
        private static int _count;                             // 칠판
        private static readonly object _gate = new object();   // 분필

        public static void Run()
        {
            // 1) 그냥 ++ : 두 일꾼이 100만 번씩 → 기대값 200만
            _count = 0;
            RunTwoWorkers(() => { _count++; });
            Console.WriteLine("1) 그냥 ++     : " + _count + " (기대값 2000000)");

            // 2) lock : 분필 든 사람만
            _count = 0;
            RunTwoWorkers(() =>
            {
                lock (_gate)
                {
                    _count++;
                }
            });
            Console.WriteLine("2) lock        : " + _count);

            // 3) Interlocked : 번호표 기계
            _count = 0;
            RunTwoWorkers(() => { Interlocked.Increment(ref _count); });
            Console.WriteLine("3) Interlocked : " + _count);
        }

        // 같은 일(addOne)을 두 일꾼에게 동시에 시킨다
        private static void RunTwoWorkers(Action addOne)
        {
            Task a = Task.Run(() => Repeat(addOne));
            Task b = Task.Run(() => Repeat(addOne));
            Task.WaitAll(a, b);
        }

        private static void Repeat(Action addOne)
        {
            for (int i = 0; i < 1000000; i++)
            {
                addOne();
            }
        }
    }
}
```
B03의 `Action`이 여기서 쓰였다. "무엇을 더할지"를 쪽지로 넘겨서, 반복하는 코드는 한 벌만 쓴다.

## 5. 실행 결과 (검증함, 1번 숫자는 매번 다름)
```text
1) 그냥 ++     : 1092289 (기대값 2000000)
2) lock        : 2000000
3) Interlocked : 2000000
```
👀 1번은 **약 90만 번을 잃어버렸다.** 에러도, 예외도 없이 **조용히 틀린다.** 검사 프로그램이라면 "양품 개수"나 "프레임 번호"가 틀리는 것이다.

## 6. 바꿔 보기 (1개)
1번을 **5번 실행**해서 숫자를 적어 본다. 매번 다르다. 운이 좋으면 가끔 200만이 나올 수도 있다.
→ "테스트 한 번 통과 = 안전"이 **아니다.** 멀티스레드 버그가 무서운 이유다. (Release 빌드로 바꾸면 결과가 또 달라질 수 있다.)

## 7. 어디서 다시 나오나
| Day | 코드 | 뜻 |
|---|---|---|
| Day03 | `lock (_sync) { 상태 확인 + 바꾸기 }` | 확인하고 바꾸는 사이에 끼어들지 못하게 (check-then-act) |
| Day04 | `long id = Interlocked.Increment(ref _frameId);` | 타이머가 겹쳐 불려도 **같은 번호를 두 번 주지 않는다** |
| Day09 | race, lock, Monitor, Interlocked, volatile | 깊게 |

## 8. 셀프 체크
<details><summary>Q1. _count++가 한 줄인데 왜 두 스레드에서 틀리나?</summary>

읽기 → +1 → 쓰기 3단계로 나뉘어 실행된다. 두 스레드가 같은 값을 읽고 각자 +1을 써서, 한 번 더한 것이 사라진다.
</details>

<details><summary>Q2. lock 대신 Interlocked를 쓰는 때는?</summary>

숫자 하나를 더하거나 빼거나 바꾸는 것처럼 **딱 한 동작**일 때다. 더 가볍다. 여러 변수를 함께 바꾸거나 "확인하고 바꾸기"처럼 여러 단계면 lock을 쓴다.
</details>

## 9. 체크
- [ ] 1번째: 보면서 타이핑 → 실행
- [ ] 2번째: 실행 결과 비교
- [ ] 3번째: 지우고 뼈대(할 일 주석)만 보고 채우기
- [ ] 바꿔 보기
