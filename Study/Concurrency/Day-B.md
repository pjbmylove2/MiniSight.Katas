# 특강 2일차 (일, 8h) · 같이 쓰기: 일꾼끼리 싸우지 않게

> **작성 중.** 교시가 끝날 때마다 이 문서에 기록한다(2026-11-13 사용자 지시, Codex 인계용). 2일차(9~16교시)가 끝나면 Day-A.md 형식으로 다듬고 셀프 체크를 붙인다.
> 전체 계획: [README.md](README.md) · 1일차: [Day-A.md](Day-A.md)

## 1. 오늘 목표

- **완료 기준**
  - 여러 일꾼이 같은 숫자를 고치면 숫자가 사라지는 것(경쟁 상태)을 직접 봤다.
  - `lock`, `Interlocked`, `CompareExchange`를 언제 쓰는지 "무엇을 → 그래서"로 말할 수 있다.
  - PipelineLab에서 사진기 → 바구니(`BlockingCollection`) → 검사 일꾼으로 사과가 흐르고, OK/NG 숫자가 올라간다.
  - 카운터·바구니 순서 테스트가 녹색이다.

## 2. 🧒 초1 비유

- **칠판 싸움** = 일꾼 넷이 칠판 숫자를 동시에 지우고 다시 쓰면 하나가 사라진다.
- **열쇠(`lock`)** = 열쇠 가진 사람만 칠판에 쓴다. 나머지는 줄 서서 기다린다.
- **번호표 기계(`Interlocked`)** = 버튼 한 번에 "읽고 +1하고 쓰기"가 끝난다.
- **사용 중 팻말(`CompareExchange`)** = 비어 있을 때만 들어가고, 사용 중이면 그냥 지나간다.

## 3. 교시별 기록

### 9교시 · 칠판 싸움 → 촬영 수 카운터 (2026-11-12, Codex로 진행, 조각 1~5)

- 9교시는 별도 `KataP09.cs` 없이 **PipelineLab `Form1.cs`에 바로** 쳤다. 원래 계획의 11교시 내용(번호표 기계 카운터)을 당겨서 같이 했다.
- 조각별 질문·답은 Codex 세션에 남아 있고 이 문서에는 없다. 아래는 2026-11-13 Claude가 **저장된 코드(포트폴리오 저장소 미커밋 변경)를 읽고** 정리한 것이다.

| 넣은 것 | 위치 |
|---|---|
| `private int _capturedCount;` 촬영 수 카운터 | 필드 |
| 시작할 때 `Interlocked.Exchange(ref _capturedCount, 0);` + `lblFrameCount.Text = "촬영 수: 0";` | `btnStart_Click`, `worker.Start()` 앞 |
| `frameNumber++` → `frameNumber = Interlocked.Increment(ref _capturedCount);` | `CaptureLoop` |
| `lblFrameCount.Text = "촬영 수: " + Volatile.Read(ref _capturedCount);` | `ShowPendingPreview` (UI 스레드) |
| 라벨 `lblFrameCount` | `Form1.Designer.cs` |

핵심:
- `Increment`의 **반환값**을 받는다. 그래서 "+1한 바로 그 번호"를 틈 없이 얻는다(다시 읽으면 그 사이 다른 일꾼이 또 올릴 수 있다).
- 숫자가 **하나**라서 쓰는 쪽 `Interlocked` + 읽는 쪽 `Volatile.Read`로 충분하다. 두 숫자(OK/NG)가 서로 맞아야 하면 `lock`이 필요하다(10교시, Day09 조각 2).

**마무리 질문 (2026-11-13, Claude):** "`CaptureLoop`에서 `QueuePreview`가 __번 불린다. 그래서 사진 한 장마다 __이 생긴다."
- 사용자 답: "2번 불린다. 그래서 사진 한 장마다 2가 생긴다." → 횟수 ✅, "그래서" △ (무엇이 2개인지 빠짐).
- 보충: 원래 있던 `using (Graphics ...)` 안의 호출과 새로 넣은 `Increment` 아래 호출이 겹쳤다. 그래서 사진 한 장마다 **복사본(`Clone`)이 2개** 생기고, 첫 번째는 두 번째에 밀려 바로 `Dispose`된다(낭비). 첫 번째 호출 때는 카운터가 아직 이전 값이다.
- 할 일: 사용자가 둘 중 **하나를 지운다**(어느 쪽을 남길지는 사용자가 정한다. 힌트: 카운터를 올린 뒤에 화면에 보내야 숫자와 사진이 맞는다). 10교시 시작 전에 지웠는지 코드로 확인한다.
- 2026-11-13 사용자: "중복 호출을 지웠다". 단, 이 PC 포트폴리오 저장소의 `Form1.cs`에는 **아직 두 호출이 다 있다**(71줄·75줄). 저장을 안 했거나 다른 PC에서 고쳤을 수 있다. 다음 기록 때 다시 확인한다.

### 10교시 · 열쇠 (`lock`) — 진행 중 (2026-11-13 시작)

계획(README): `lock`, 오래 붙잡으면 생기는 일, "lock 안에서 하지 말 것"(Cheatsheets/Threading.md §8). 코드는 `Katas/Concurrency/KataP10.cs`, `Program.cs`는 `KataP10.Run();`.

#### 조각 1 · 열쇠 없이 vs 열쇠 들고 칠판 고치기

일꾼 4명이 칠판 숫자 `_count`를 10만 번씩 +1한다. 9교시 "칠판 싸움" race 실험(기대값 vs 실제값)을 여기서 함께 본다.

```csharp
using System;
using System.Threading;

namespace Katas.Concurrency
{
    public static class KataP10
    {
        private static int _count;                          // 칠판 숫자
        private static readonly object _key = new object(); // 칠판 열쇠

        public static void Run()
        {
            Chunk1();
        }

        // 10교시 조각 1: 열쇠 없이 vs 열쇠 들고 칠판 고치기
        public static void Chunk1()
        {
            _count = 0;
            RunFour(AddNoKey);
            Console.WriteLine("열쇠 없이: " + _count + " (기대 400000)");

            _count = 0;
            RunFour(AddWithKey);
            Console.WriteLine("열쇠 들고: " + _count + " (기대 400000)");
        }

        private static void AddNoKey()
        {
            for (int i = 0; i < 100000; i++)
            {
                _count++;
            }
        }

        private static void AddWithKey()
        {
            for (int i = 0; i < 100000; i++)
            {
                lock (_key)
                {
                    _count++;
                }
            }
        }

        // 일꾼 4명에게 같은 일을 시키고 다 끝날 때까지 기다린다
        private static void RunFour(ThreadStart work)
        {
            Thread[] workers = new Thread[4];
            for (int i = 0; i < 4; i++)
            {
                workers[i] = new Thread(work);
                workers[i].Start();
            }
            foreach (Thread t in workers)
            {
                t.Join();
            }
        }
    }
}
```

| 코드 | 뜻 | 같은 뜻의 긴 코드 / 쓰는 법 |
|---|---|---|
| `_count++` | 읽고 → +1 → 쓰기, **세 걸음** | `int t = _count; t = t + 1; _count = t;` |
| `object _key = new object()` | 열쇠로만 쓰는 빈 물건 | `readonly`로 바뀌지 않게 한다 |
| `lock (_key) { }` | 열쇠를 잡은 일꾼만 `{ }` 안에 들어간다. 나오면 열쇠 반납 | `Monitor.Enter(_key); try { } finally { Monitor.Exit(_key); }` |
| `ThreadStart` | "매개변수 없고 돌려주는 값 없는 메서드"를 담는 그릇 | `RunFour(AddNoKey)`처럼 메서드 이름을 넘긴다 |

예상 출력 (Claude PC, Debug 5회·Release 3회 확인):

```text
열쇠 없이: 156085 (기대 400000)
열쇠 들고: 400000 (기대 400000)
```

- "열쇠 없이" 숫자는 매번 다르다(확인한 값: 102030~234003). "열쇠 들고"는 항상 400000이다.
- 실험: 같은 exe를 3번 실행해 "열쇠 없이" 숫자 세 개를 적는다.
- 질문: "열쇠 없이 하면 숫자가 400000보다 __. 왜냐하면 `_count++`가 __ 걸음이라 __. 그래서 `lock`으로 __."
- 사용자 답 (2026-11-13): "작다. 읽고 → +1 → 쓰기 세 걸음이라 그 사이에 다른 스레드가 +1 안 된 원래 값을 읽고 +1 하고 쓴다. 그래서 값이 예상보다 작고, lock으로 잠가서 한 스레드씩 접근하게 막아야 한다." ✅ ("그래서"까지 완성)
- 실험 숫자 (집 PC, 3회): 321234, 400000, 400000. 열쇠 없이도 400000이 나온 회차가 있다 → 보충: 10만 번은 너무 빨리 끝나서, 다음 일꾼이 출발하기 전에 앞 일꾼이 끝나 버리면(겹치지 않으면) 싸움이 안 일어난다. 그래서 race는 "가끔"만 보이고, 한 번 맞았다고 안전한 게 아니다(15·28교시 테스트로 이어짐).
#### 조각 2 · 열쇠를 오래 붙잡으면?

`using System.Diagnostics;` 추가, `Run()`은 `Chunk1();` 주석 + `Chunk2();`. 기존 `RunFour`, `_key`, `_count` 재사용.

```csharp
        // 10교시 조각 2: 열쇠를 오래 붙잡으면?
        public static void Chunk2()
        {
            Stopwatch sw = Stopwatch.StartNew();
            RunFour(WriteSlow);
            Console.WriteLine("열쇠 안에서 생각: " + sw.ElapsedMilliseconds + "ms");

            sw.Restart();
            RunFour(WriteFast);
            Console.WriteLine("열쇠 밖에서 생각: " + sw.ElapsedMilliseconds + "ms");
        }

        private static void WriteSlow()
        {
            lock (_key)
            {
                Thread.Sleep(200);   // 생각(검사)을 열쇠 들고 한다
                _count++;
            }
        }

        private static void WriteFast()
        {
            Thread.Sleep(200);       // 생각(검사)은 열쇠 없이 한다
            lock (_key)
            {
                _count++;            // 칠판에 쓰는 순간만 열쇠
            }
        }
```

| 코드 | 뜻 |
|---|---|
| `sw.Restart()` | 초시계를 0으로 돌리고 다시 누르기 (`Reset()` + `Start()`) |

예상 출력 (Claude PC 3회 확인: 802/200, 803/201, 803/200):

```text
열쇠 안에서 생각: 802ms
열쇠 밖에서 생각: 200ms
```

- 실험: `WriteSlow`의 `Sleep(200)`을 `Sleep(500)`으로 바꾸면 첫 줄이 약 2000ms(4 × 500)가 되는지 본다 (실행 확인 안 함).
- 질문: "열쇠 안에서 생각하면 약 __ms, 밖에서 하면 약 __ms. 왜냐하면 열쇠 안에서는 일꾼들이 __. 그래서 lock 안에는 __만 넣는다."
- 사용자 답 (2026-11-13): "약 800ms, 약 200ms. 열쇠 안에서는 일꾼들이 줄 서서 기다리기 때문. 그래서 lock 안에는 간단한 작업만 넣는다." ✅ → 다듬기: "간단한 작업" 대신 **"공유 값을 읽고 바꾸는 짧은 작업"**. 기준은 쉬운지가 아니라 공유 값을 건드리는지와 짧은지다(검사·Sleep·파일·통신은 밖).
#### 조각 3 · 열쇠를 든 채 다른 열쇠를 기다리면? (lock 안에서 하지 말 것)

필드에 `_keyA`(칠판), `_keyB`(분필 상자) 추가, `Run()`은 `Chunk3();`만. 데드락은 25교시에 본격적으로 하고, 여기서는 `Monitor.TryEnter(열쇠, 1000)`로 **1초만 기다리게 해서** 프로그램이 멈추지 않게 본다.

```csharp
        private static readonly object _keyA = new object(); // 칠판 열쇠
        private static readonly object _keyB = new object(); // 분필 상자 열쇠

        // 10교시 조각 3: 열쇠를 든 채 다른 열쇠를 기다리면?
        public static void Chunk3()
        {
            Thread minsu = new Thread(() => TakeTwo("민수", _keyA, _keyB));
            Thread younghee = new Thread(() => TakeTwo("영희", _keyB, _keyA));
            minsu.Start();
            younghee.Start();
            minsu.Join();
            younghee.Join();
            Console.WriteLine("메인: 끝");
        }

        private static void TakeTwo(string name, object first, object second)
        {
            lock (first)
            {
                Console.WriteLine(name + ": 첫 열쇠 잡음");
                Thread.Sleep(100);   // 상대도 첫 열쇠를 잡을 시간

                if (Monitor.TryEnter(second, 1000))
                {
                    try
                    {
                        Console.WriteLine(name + ": 두 열쇠 다 잡음");
                    }
                    finally
                    {
                        Monitor.Exit(second);
                    }
                }
                else
                {
                    Console.WriteLine(name + ": 1초 기다려도 두 번째 열쇠 못 받음");
                }
            }
        }
```

| 코드 | 뜻 |
|---|---|
| `Monitor.TryEnter(열쇠, 1000)` | 열쇠를 최대 1초만 기다린다. 받으면 `true`, 못 받으면 `false` |
| `Monitor.Exit(열쇠)` | 열쇠 반납. `TryEnter`로 받았으면 `finally`에서 꼭 반납 (`lock`이 대신 해 주던 일) |

예상 출력 (Claude PC 5회 확인, 두 가지 모양이 섞여 나온다):

```text
민수: 첫 열쇠 잡음
영희: 첫 열쇠 잡음
민수: 1초 기다려도 두 번째 열쇠 못 받음
영희: 두 열쇠 다 잡음
메인: 끝
```

```text
민수: 첫 열쇠 잡음
영희: 첫 열쇠 잡음
민수: 1초 기다려도 두 번째 열쇠 못 받음
영희: 1초 기다려도 두 번째 열쇠 못 받음
메인: 끝
```

- 앞 두 줄·이름 순서는 바뀔 수 있다. 먼저 포기한 쪽이 첫 열쇠를 돌려주면 상대가 두 번째 열쇠를 받기도 한다(5회 중 2회).
- 실험: `if (Monitor.TryEnter(second, 1000))`를 잠깐 `lock (second)`로 바꿔 보면 "첫 열쇠 잡음" 두 줄 뒤 **영원히 멈춘다**(데드락). Ctrl+C로 끄고 원래대로 돌린다. (실험 모양은 분석으로 예상, 실행 확인 안 함)
- 질문: "민수는 __ 열쇠를 든 채 __ 열쇠를 기다리고, 영희는 반대다. 그래서 `lock`으로 기다리면 __. 그래서 lock 안에서는 __를 하지 않는다."
- 사용자 답 (2026-11-13): "민수는 A열쇠를 든 채 B열쇠를 기다리고, 영희는 반대다. 그래서 lock으로 기다리면 안 된다. 그래서 lock 안에서는 다른 키를 기다리면 안 된다." → 빈칸 1·2 ✅, 3 △ ("안 된다"는 규칙이고 **일어나는 일**이 빠짐 → 둘 다 상대 열쇠를 영원히 기다려 **프로그램이 멈춘다(데드락)**), 4 ✅.
- 보충: 꼭 두 열쇠가 필요하면 ① 모두 **같은 순서**(A → B)로 잡거나 ② `TryEnter`에 시간 제한을 두고 못 받으면 로그를 남긴다. 25교시에서 다시 본다.

**10교시 완료 (2026-11-13).** 조각 1 ✅, 조각 2 ✅, 조각 3 △(결과 "멈춘다"를 말하기). 다음은 11교시 번호표 기계 (9교시에 작품 카운터를 이미 해서 짧게).

### 11교시 · 번호표 기계 (`Interlocked`) — 진행 중 (2026-11-13 시작)

9교시에 PipelineLab 촬영 수 카운터를 `Interlocked.Increment`로 이미 만들었다. 그래서 11교시는 조각 2개로 짧게 한다: ① 열쇠와 속도 비교 ② 작품 카운터 다시 보기(숫자 하나 vs 둘). 코드는 `Katas/Concurrency/KataP11.cs`, `Program.cs`는 `KataP11.Run();`.

#### 조각 1 · 열쇠 vs 번호표 기계, 누가 빠를까?

```csharp
using System;
using System.Diagnostics;
using System.Threading;

namespace Katas.Concurrency
{
    public static class KataP11
    {
        private static int _count;
        private static readonly object _key = new object();

        public static void Run()
        {
            Chunk1();
        }

        // 11교시 조각 1: 열쇠 vs 번호표 기계, 누가 빠를까?
        public static void Chunk1()
        {
            _count = 0;
            Stopwatch sw = Stopwatch.StartNew();
            RunFour(AddWithKey);
            Console.WriteLine("열쇠: " + _count + ", " + sw.ElapsedMilliseconds + "ms");

            _count = 0;
            sw.Restart();
            RunFour(AddWithTicket);
            Console.WriteLine("번호표: " + _count + ", " + sw.ElapsedMilliseconds + "ms");
        }

        private static void AddWithKey()
        {
            for (int i = 0; i < 1000000; i++)
            {
                lock (_key)
                {
                    _count++;
                }
            }
        }

        private static void AddWithTicket()
        {
            for (int i = 0; i < 1000000; i++)
            {
                Interlocked.Increment(ref _count);
            }
        }

        private static void RunFour(ThreadStart work)
        {
            Thread[] workers = new Thread[4];
            for (int i = 0; i < 4; i++)
            {
                workers[i] = new Thread(work);
                workers[i].Start();
            }
            foreach (Thread t in workers)
            {
                t.Join();
            }
        }
    }
}
```

| 코드 | 뜻 |
|---|---|
| `Interlocked.Increment(ref _count)` | 읽고 +1 하고 쓰기를 **한 걸음**에 (CPU 명령 하나). 올린 뒤의 값을 돌려준다 |
| `ref` | 값을 복사하지 않고 **그 변수 자리**를 넘긴다. 그래야 진짜 `_count`가 바뀐다 |

예상 출력 (Claude PC, Debug 4회·Release 3회 확인. 열쇠 51~55ms, 번호표 30~36ms):

```text
열쇠: 4000000, 53ms
번호표: 4000000, 35ms
```

- 두 줄 다 항상 4000000. ms는 PC마다 다르지만 번호표가 더 빠를 것으로 예상(집 PC 실행 확인 안 함).
- `RunFour`는 KataP10과 같은 코드를 복사했다(Kata 파일마다 혼자 돌게).
- 실험: `AddWithTicket` 안을 `_count++;`로 바꾸면 두 번째 줄 숫자가 4000000보다 작아지는 걸 본다 (10교시 조각 1과 같은 모양, 실행 확인 안 함).
- 질문: "두 방법 모두 결과는 __이다. 번호표가 더 빠른 이유는 __. 그래서 숫자 __개만 올릴 때는 `Interlocked`를 쓴다."
- 사용자 답 (2026-11-13): "결과는 다르다. 번호표가 빠른 이유는 CPU가 그 자리에서 한 번에 끝내기 때문. 그래서 숫자 1개만 올릴 때는 Interlocked." 집 PC 출력: 열쇠 4000000, 51ms / 번호표 4000000, 33ms. → 빈칸 1 ❌ (자기 출력에서 두 숫자 모두 4000000 → **결과는 같다**, 다른 건 시간), 2 ✅ (+ "줄 서서 기다리지 않아서"), 3 ✅.
#### 조각 2 · 번호표 두 장 vs 열쇠 하나 (두 숫자가 서로 맞아야 할 때)

조각 1 코드 아래에 이어 붙인다. 필드 `_total`, `_ok`, `_bad` 추가(`_key`는 조각 1 것 재사용), `Run()`은 `Chunk1();` 주석 + `Chunk2();`. 쓰는 일꾼은 "전체 +1 → OK +1" 순서라 어느 순간에도 OK ≤ 전체다. 그런데 읽는 일꾼이 두 숫자를 **따로** 읽으면 그 사이에 쓰기가 끼어들어 "OK가 전체보다 많은" 있을 수 없는 칠판을 본다.

```csharp
        private static int _total; // 들어온 사과 수
        private static int _ok;    // 그중 OK 수
        private static int _bad;   // "OK가 전체보다 많은" 이상한 칠판을 본 횟수

        // 11교시 조각 2: 번호표 두 장 vs 열쇠 하나
        public static void Chunk2()
        {
            _total = 0;
            _ok = 0;
            _bad = 0;
            RunPair(WriteTwoTickets, ReadTwoTickets);
            Console.WriteLine("번호표 두 장: 이상한 칠판 " + _bad + "번");

            _total = 0;
            _ok = 0;
            _bad = 0;
            RunPair(WriteWithKey, ReadWithKey);
            Console.WriteLine("열쇠 하나: 이상한 칠판 " + _bad + "번");
        }

        private static void WriteTwoTickets()
        {
            for (int i = 0; i < 1000000; i++)
            {
                Interlocked.Increment(ref _total); // 사과 하나 들어옴
                Interlocked.Increment(ref _ok);    // 검사해 보니 OK
            }
        }

        private static void ReadTwoTickets()
        {
            for (int i = 0; i < 1000000; i++)
            {
                int total = Volatile.Read(ref _total);
                int ok = Volatile.Read(ref _ok);
                if (ok > total)
                {
                    _bad++; // OK가 전체보다 많을 수는 없다
                }
            }
        }

        private static void WriteWithKey()
        {
            for (int i = 0; i < 1000000; i++)
            {
                lock (_key)
                {
                    _total++;
                    _ok++;
                }
            }
        }

        private static void ReadWithKey()
        {
            for (int i = 0; i < 1000000; i++)
            {
                int total;
                int ok;
                lock (_key)
                {
                    total = _total;
                    ok = _ok;
                }
                if (ok > total)
                {
                    _bad++;
                }
            }
        }

        // 쓰는 일꾼 1명과 읽는 일꾼 1명을 동시에 돌린다
        private static void RunPair(ThreadStart writer, ThreadStart reader)
        {
            Thread w = new Thread(writer);
            Thread r = new Thread(reader);
            w.Start();
            r.Start();
            w.Join();
            r.Join();
        }
```

| 코드 | 뜻 |
|---|---|
| `Volatile.Read(ref _total)` | 다른 일꾼이 바꾼 **최신 값**을 읽는다(9교시 UI 라벨에서 쓴 것). 보이게만 할 뿐, 두 값을 함께 읽어 주지는 않는다 |
| `lock` 안에서 두 값 복사 → 밖에서 비교 | **스냅샷**: 같은 순간의 두 값을 한꺼번에 가져온다. 비교는 열쇠 밖에서 (짧게) |

예상 출력 (Claude PC 확인. 번호표 두 장: Debug 5회 10407~60842번, Release 3회 108~1693번. 열쇠 하나: 항상 0번):

```text
번호표 두 장: 이상한 칠판 30491번
열쇠 하나: 이상한 칠판 0번
```

- 첫 줄 숫자는 매번·PC마다 다르다. 집 PC에서는 0이 나오는 회차도 있을 수 있다(10교시 조각 1처럼 안 겹치면). 둘째 줄은 항상 0.
- `_bad`는 읽는 일꾼 혼자만 바꾸므로 `++`로 충분하다.
- 실험: `ReadWithKey`의 `lock (_key)`를 지우고(두 대입은 남김) 다시 실행하면 둘째 줄도 0이 아니게 되는지 본다 → **쓰는 쪽만 잠그면 소용없다** (실행 확인 안 함).
- 질문: "번호표 두 장으로 따로 읽으면 두 번 읽는 __에 쓰기가 끼어든다. 그래서 OK가 전체보다 __ 칠판을 본다. 그래서 두 숫자가 서로 맞아야 하면 쓰는 쪽과 __ 쪽 모두 같은 열쇠로 잠근다."
- 사용자 답 (2026-11-13): "두 번 읽는 사이에 쓰기가 끼어든다. 그래서 OK가 전체보다 쓰는 칠판을 본다. 그래서 쓰는 쪽과 읽는 쪽 모두 같은 열쇠로 잠근다." → 빈칸 1 ✅, 2 △ ("쓰는" → **많은**: 있을 수 없는 숫자를 본다는 결과가 핵심), 3 ✅. 집 PC 첫 줄 숫자는 안 보냄.

**11교시 완료 (2026-11-13).** 조각 1 빈칸 1 ❌(결과는 같고 시간이 다름)·2·3 ✅, 조각 2 △(빈칸 2). 다음은 12교시 사용 중 팻말(`CompareExchange`). 참고: PipelineLab `QueuePreview`가 이미 `Interlocked.Exchange`로 "최신 사진 1장" 자리를 쓰고 있다(6교시). 12교시는 Day09 조각 5의 `CompareExchange` 팻말(사용 중이면 건너뛰기)을 KataP12로 짧게 치고, 작품의 Exchange와 무엇이 다른지 비교하는 쪽으로 한다.

### 12교시 · 사용 중 팻말 (`CompareExchange`) — 진행 중 (2026-11-13 시작)

코드는 `Katas/Concurrency/KataP12.cs`, `Program.cs`는 `KataP12.Run();`. 조각 2개 예정: ① 팻말로 건너뛰기 ② 작품 `QueuePreview`의 `Exchange`(최신 1장으로 바꿔 끼우기)와 비교.

#### 조각 1 · 그리는 중이면 새 사진은 건너뛴다

사진기가 40ms마다 사진을 주는데 그리기는 100ms 걸린다. 팻말이 "사용 중"이면 새 사진은 줄 서지 않고 그냥 지나간다.

```csharp
using System;
using System.Threading;

namespace Katas.Concurrency
{
    public static class KataP12
    {
        private static int _busy;    // 팻말: 0 = 비어 있음, 1 = 사용 중
        private static int _drawn;   // 그린 사진 수
        private static int _skipped; // 건너뛴 사진 수

        public static void Run()
        {
            Chunk1();
        }

        // 12교시 조각 1: 사용 중 팻말 - 그리는 중이면 새 사진은 건너뛴다
        public static void Chunk1()
        {
            Thread[] painters = new Thread[10];
            for (int i = 0; i < 10; i++)
            {
                int frame = i + 1;
                painters[i] = new Thread(() => TryDraw(frame));
                painters[i].Start();
                Thread.Sleep(40);   // 사진기: 40ms마다 한 장
            }
            foreach (Thread t in painters)
            {
                t.Join();
            }
            Console.WriteLine("그림: " + _drawn + ", 건너뜀: " + _skipped);
        }

        private static void TryDraw(int frame)
        {
            if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
            {
                Interlocked.Increment(ref _skipped);
                return;
            }
            try
            {
                Console.WriteLine("사진 " + frame + " 그리는 중");
                Thread.Sleep(100);  // 그리기: 100ms
                Interlocked.Increment(ref _drawn);
            }
            finally
            {
                Volatile.Write(ref _busy, 0); // 팻말 내리기
            }
        }
    }
}
```

| 코드 | 뜻 |
|---|---|
| `Interlocked.CompareExchange(ref _busy, 1, 0)` | "팻말이 **0이면** 1로 바꿔" 를 한 걸음에. 바꾸기 **전** 값을 돌려준다 → 0이면 내가 차지, 1이면 이미 누가 사용 중 |
| `!= 0` → `return` | 사용 중이면 기다리지 않고 지나간다 (lock은 줄 서서 기다림) |
| `Volatile.Write(ref _busy, 0)` | 팻말 내리기. 다른 일꾼에게 바로 보이게 쓴다 |
| `int frame = i + 1;` | 람다가 `i`를 직접 잡으면 일꾼이 시작할 때 `i`가 이미 바뀌어 있을 수 있어 **복사본**을 만든다 |

예상 출력 (Claude PC 6회 확인, 6번 모두 같음):

```text
사진 1 그리는 중
사진 4 그리는 중
사진 7 그리는 중
사진 10 그리는 중
그림: 4, 건너뜀: 6
```

- 0ms 사진 1 시작(~100ms까지 사용 중) → 40·80ms 건너뜀 → 120ms 사진 4 … 3장마다 1장 그린다.
- 실험: `finally`의 `Volatile.Write(ref _busy, 0);`를 주석 처리 → `사진 1 그리는 중` / `그림: 1, 건너뜀: 9` (확인함). 팻말을 안 내리면 **아무 오류 없이** 그 뒤로 영원히 안 그린다(조용한 고장, Day09 조각 5).
- 질문: "`CompareExchange`는 팻말이 __일 때만 1로 바꾼다. 사용 중이면 기다리지 않고 __. 그래서 `finally`에서 팻말을 __ 않으면 __."
- 사용자 답 (2026-11-13): "팻말이 0일 때만 1로 바꾼다. 사용 중이면 기다리지 않고 넘어간다. 그래서 finally에서 팻말을 내리지 않으면 계속 넘어간다." ✅ → 덧붙임: 계속 넘어간다 → **그래서 오류 없이 다시는 안 그려진다(화면 멈춤, 조용한 고장)**.
#### 조각 2 · 우편함(`Exchange`) — 늘 최신 사진 1장만 남긴다

작품 `QueuePreview`와 같은 생각을 숫자로 친다. 사진기는 우편함에 새 사진을 **바꿔 넣고**(있던 건 밀려남), 그리는 일꾼은 다 그리면 우편함을 **비우며 꺼낸다**. 필드 `_box`, `_replaced` 추가, `Run()`은 `Chunk1();` 주석 + `Chunk2();`.

```csharp
        private static int _box;      // 우편함: 0 = 비어 있음, 숫자 = 사진 번호
        private static int _replaced; // 그려지기 전에 새 사진에 밀려난 수

        // 12교시 조각 2: 우편함(Exchange) - 늘 최신 사진 1장만 남긴다
        public static void Chunk2()
        {
            ManualResetEvent cameraDone = new ManualResetEvent(false);
            Thread painter = new Thread(() => PaintLoop(cameraDone));
            painter.Start();

            for (int frame = 1; frame <= 10; frame++)
            {
                int old = Interlocked.Exchange(ref _box, frame); // 새 사진 넣고, 있던 것 꺼내기
                if (old != 0)
                {
                    Interlocked.Increment(ref _replaced);
                }
                Thread.Sleep(40);   // 사진기: 40ms마다 한 장
            }

            cameraDone.Set();
            painter.Join();
            cameraDone.Dispose();
            Console.WriteLine("밀려남: " + _replaced);
        }

        private static void PaintLoop(ManualResetEvent cameraDone)
        {
            while (true)
            {
                int frame = Interlocked.Exchange(ref _box, 0); // 우편함 비우며 꺼내기
                if (frame != 0)
                {
                    Console.WriteLine("사진 " + frame + " 그리는 중");
                    Thread.Sleep(100);  // 그리기: 100ms
                }
                else if (cameraDone.WaitOne(10))
                {
                    return;
                }
            }
        }
```

| 코드 | 뜻 |
|---|---|
| `Interlocked.Exchange(ref _box, frame)` | 조건 없이 **바꿔 넣고** 원래 있던 값을 돌려준다 (`CompareExchange`는 "0일 때만") |
| `Interlocked.Exchange(ref _box, 0)` | 비우면서 꺼내기. 꺼내는 사람이 둘이어도 한 명만 사진을 받는다 |
| `ManualResetEvent cameraDone` / `WaitOne(10)` | 4교시 "그만! 벨". 우편함이 비었을 때 10ms만 벨을 확인한다 |

예상 출력 (Claude PC 8회 확인. 셋째 줄만 `사진 5` 7회 / `사진 6` 1회):

```text
사진 1 그리는 중
사진 3 그리는 중
사진 5 그리는 중
사진 8 그리는 중
사진 10 그리는 중
밀려남: 5
```

- 조각 1(팻말)은 1·4·7·10 → **4장**, 우편함은 **5장**. 팻말은 바쁠 때 온 사진을 버리고 **다음 사진이 올 때까지** 쉰다. 우편함은 다 그리자마자 그동안 온 **최신 사진**을 바로 그린다.
- 마지막 사진(10)은 우편함에 남아 있으니 반드시 그려진다. 팻말은 마지막 사진이 바쁠 때 오면 버려서 화면이 옛 사진에 멈출 수 있다.
- 실험: 사진기 `Sleep(40)` → `Sleep(150)`(그리기보다 느린 사진기)으로 바꾸면 사진 1~10을 다 그리고 `밀려남: 0` (확인함). 빠른 사진기일 때만 밀려난다.
- 작품 `QueuePreview`: 사진기 일꾼이 `Exchange(ref _pendingPreview, copy)`로 바꿔 넣고, 밀려난 이전 사진은 `Dispose`한다. UI의 `ShowPendingPreview`가 `Exchange(ref _pendingPreview, null)`로 비우며 꺼낸다. 숫자 대신 `Bitmap`이라 밀려난 것을 **해제**까지 해야 한다는 점만 다르다.
- 질문: "팻말은 바쁠 때 온 사진을 __. 우편함은 새 사진으로 __ 넣는다. 그래서 다 그린 뒤 화면에 나오는 건 늘 __ 사진이다."
- 사용자 답: 빈칸 질문에는 답하지 않고 "다음"으로 넘어감 → 미답. "다시 물어볼 질문"으로 넘김 (16교시 정리 퀴즈에서 다시). 정답: 버린다 / 바꿔(덮어) / 최신(마지막).
- 사용자 Kata 확인 (포트폴리오 `KataP12.cs`): 조각 1은 `Run()` 안에 그대로, 조각 2는 `Chunk2()`로 쳤고 `Program.cs`가 `KataP12.Chunk2();`를 부름. 코드는 원본과 같음.

> **Q. 값이 변하는 과정을 말로 풀어 주세요.**
> 사진기는 0·40·…·360ms에 1~10을 넣고, 일꾼은 꺼낼 때마다 100ms 그린다. 일꾼이 우편함을 여는 시각은 0·100·200·300·400ms → 그때 들어 있던 1·3·5·8·10을 꺼낸다. 사진기가 넣을 때 `old != 0`이면(일꾼이 아직 안 가져간 사진을 덮으면) 밀려남 +1 → 2·4·6·7·9로 5장. 200ms는 일꾼이 꺼내는 순간과 사진기가 6을 넣는 순간이 겹쳐서 가끔 5 대신 6을 그린다(그래도 밀려남은 5). 우편함이 비었을 때 `WaitOne(10)`은 벨이 안 울렸으면 10ms 쉬고 `false` → 다시 확인, 울렸으면 `true` → `return`.

> **Q. `painter.Start()`가 먼저인데 `for`가 먼저 시작하나요?**
> `Start()`는 운영체제에 "출발시켜 달라"고 부탁만 하고 바로 돌아온다. 새 스레드가 첫 줄을 실행하기까지 준비 시간이 있어서 보통 메인의 `for`가 먼저 움직인다. Claude PC에서 첫 줄 시각을 5번 재 보니 5번 다 `for`가 먼저(차이 0.002~0.1ms). 순서는 보장되지 않는다. 일꾼이 먼저 가도 `frame == 0` → `WaitOne(10)` → 다시 꺼내기라서 결과는 같다(최대 10ms 늦을 뿐). 한 스레드 안에서만 "적힌 순서 = 실행 순서"다.

> **Q. 무조건 1 → 3인가요? 1 → 4는 안 되나요?**
> 보장은 안 된다. 일꾼이 100ms쯤 우편함을 여는데 3은 80ms, 4는 120ms에 들어와서 양쪽으로 20ms 여유가 있을 뿐이다. 일꾼이 20ms 넘게 늦으면(PC가 바쁨, `Sleep`이 길게 잠, 디버거) 1 → 4, 사진기가 늦으면 1 → 2. 확인: 그대로 30번 → 1 → 3이 30/30(`1,3,5,8,10` 26번, `1,3,6,8,10` 4번). 그리기를 `Sleep(130)`으로 → `1,4,7,10` 10/10. 이 코드가 보장하는 것은 "꺼낸 건 그 순간의 최신 사진", "마지막 사진은 반드시 그려진다"이다. 몇 번이 그려지는지는 타이밍이라 테스트로 검사하지 않는다(15교시).

**12교시 완료 (2026-11-13).** 조각 1 ✅, 조각 2 빈칸 미답(곁가지 질문 3개로 동작은 이해함). 다음은 13교시 바구니에 줄 서기(`BlockingCollection`). 작품 `Form1.cs`의 `QueuePreview(frame)` 중복 호출(71·75줄)은 아직 그대로다.

### 13교시 · 바구니에 줄 서기 (`BlockingCollection`) — 진행 중 (2026-11-13 시작)

코드는 `Katas/Concurrency/KataP13.cs`, `Program.cs`는 `KataP13.Run();`. 조각 예정: ① 바구니 기본(넣기·기다렸다 꺼내기·끝 알림) ② 작품에 사진기 → 바구니 → 검사 일꾼 연결.

#### 조각 1 · 넣은 사과는 하나도 안 빠지고 순서대로 검사된다

🧒 우편함은 한 장만 들어가서 덮어썼다. 바구니는 여러 개가 들어가서 **줄을 선다**. 검사 일꾼은 바구니가 비어 있으면 **기다렸다가** 꺼내고, 사진기가 "이제 더 안 넣어요"라고 말하면 남은 걸 다 검사하고 퇴근한다.

```csharp
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Katas.Concurrency
{
    public static class KataP13
    {
        public static void Run()
        {
            Chunk1();
        }

        // 13교시 조각 1: 바구니(BlockingCollection) - 넣은 사과는 하나도 안 빠지고 순서대로 검사
        public static void Chunk1()
        {
            BlockingCollection<int> basket = new BlockingCollection<int>();
            Thread inspector = new Thread(() => InspectLoop(basket));
            inspector.Start();

            for (int apple = 1; apple <= 5; apple++)
            {
                basket.Add(apple);              // 바구니에 넣기
                Console.WriteLine("넣음: 사과 " + apple);
                Thread.Sleep(40);               // 사진기: 40ms마다 한 개
            }

            basket.CompleteAdding();            // "이제 더 안 넣어요" 알림
            Console.WriteLine("사진기: 다 넣었음");
            inspector.Join();
            basket.Dispose();
        }

        private static void InspectLoop(BlockingCollection<int> basket)
        {
            foreach (int apple in basket.GetConsumingEnumerable())  // 비면 기다렸다가 꺼내기
            {
                Console.WriteLine("    검사: 사과 " + apple);
                Thread.Sleep(100);              // 검사: 100ms
            }
            Console.WriteLine("    검사 일꾼: 바구니 끝, 퇴근");
        }
    }
}
```

| 코드 | 뜻 |
|---|---|
| `using System.Collections.Concurrent;` | 여러 스레드가 같이 써도 되는 모음 가족이 사는 곳 |
| `BlockingCollection<int>` | 바구니. 안쪽은 줄(먼저 넣은 것 먼저, FIFO). 비어 있으면 꺼내는 쪽을 **기다리게(Blocking)** 한다 |
| `basket.Add(apple)` | 바구니 끝에 넣기. 안에서 알아서 잠그니 `lock` 필요 없음 |
| `GetConsumingEnumerable()` | `foreach`로 하나씩 **꺼내면서(지우면서)** 돈다. 비면 기다린다. `CompleteAdding` + 빈 바구니가 되면 `foreach`가 끝난다 |
| `basket.CompleteAdding()` | "이제 더 안 넣어요" 알림. 이후 `Add`는 예외 |
| `basket.Dispose()` | 바구니가 안에서 쓰는 신호 객체 해제. 일꾼 `Join` **뒤에** |

예상 출력 (Claude PC 8회 확인, 8회 모두 같음):

```text
넣음: 사과 1
    검사: 사과 1
넣음: 사과 2
넣음: 사과 3
    검사: 사과 2
넣음: 사과 4
넣음: 사과 5
    검사: 사과 3
사진기: 다 넣었음
    검사: 사과 4
    검사: 사과 5
    검사 일꾼: 바구니 끝, 퇴근
```

- 우편함(12교시)은 10장 중 5장을 버렸지만 바구니는 **5개 다** 순서대로 검사한다. 대신 사진기가 빠르면 바구니에 쌓인다(사진기가 끝난 뒤에도 4·5가 남아 있다). 쌓이는 문제는 14교시.
- 첫 두 줄(`넣음 1` / `검사 1`)은 거의 동시라 순서가 바뀔 수 있다(확인 범위에선 안 바뀜).
- 실험: `basket.CompleteAdding();`을 주석 처리 → 사과 5까지 검사한 뒤 "퇴근" 줄이 안 나오고 프로그램이 **안 끝난다**(일꾼이 빈 바구니 앞에서 영원히 기다림, `Join`도 영원히 기다림). 3초 제한으로 확인함. 확인 후 되돌린다.
- 질문: "`GetConsumingEnumerable`은 바구니가 비면 __. 그래서 사진기가 끝났다고 알려 주는 __이 없으면 검사 일꾼은 __."
- 사용자 답: "바구니가 비면 기다렸다가 꺼냈다. 그래서 … 신호가 없으면 검사 일꾼은 무한대기한다." ✅ (빈칸 1·2·3). 보충: 신호의 이름은 `CompleteAdding`. 일꾼만 무한대기하는 게 아니라 메인의 `Join`도 같이 기다려서 프로그램 전체가 안 끝난다.

#### 조각 2 · 작품에 연결: 사진기 → 바구니 → 검사 일꾼 → OK/NG

작품 `PipelineLab/Form1.cs`에 바로 친다(Kata 파일 없음). 바꾸는 곳:

1. 디자이너: 사진 오른쪽에 Label 추가, `Name = lblResult`, `Text = OK 0 / NG 0`.
2. `using System.Collections.Concurrent;` + 필드 4개: `BlockingCollection<Bitmap> _basket`, `Thread _inspectorThread`, `int _okCount`, `int _ngCount`.
3. `btnStart_Click`: 지역 변수 `basket`, `inspector`(= null로 시작) 만들기 → 카운터 3개 `Interlocked.Exchange`로 0 → **검사 일꾼 먼저 `Start`**, 사진기 다음. `catch`에서는 `basket.CompleteAdding()` → 출발한 검사 일꾼이 있으면(`IsAlive`) `Join` → `basket.Dispose()` → `stop.Dispose()`. 성공하면 필드에 넣기.
4. `CaptureLoop(stop, basket)`: 바구니도 `stop`처럼 **매개변수**로 받는다(필드는 `Start` 뒤에 채워지니까). `frameNumber = Interlocked.Increment(...)`를 루프 맨 앞으로 올리고, `frameNumber % 3 == 0`이면 검은 원(멍) 그리기. `QueuePreview(frame)`은 **한 번만**(9교시 중복 호출 정리). 그다음 `basket.Add(new Bitmap(frame))`.
5. `InspectLoop(basket)`: `foreach (Bitmap frame in basket.GetConsumingEnumerable())` → `using (frame)` 안에서 `AppleInspector.CountBruisePixels` → 0보다 크면 `_ngCount`, 아니면 `_okCount`를 `Interlocked.Increment`.
6. `StopCamera`: `_stop.Set()` → `_cameraThread.Join()` → `_basket.CompleteAdding()` → `_inspectorThread.Join()` → `_basket.Dispose()` → `ShowResult()`. 끝에 `_inspectorThread = null; _basket = null;`.
7. `ShowResult()` 새 메서드(`lblResult.Text = "OK " + Volatile.Read(ref _okCount) + " / NG " + Volatile.Read(ref _ngCount)`), `ShowPendingPreview`에서도 호출.

전체 코드는 Claude 확인본: 스크래치 `p13b/Form1.cs` (채팅으로 전달).

확인 (Claude PC, 폼을 띄우고 시작 → 3.4초 뒤 정지를 코드로 누르기, 5회 같음):

```text
  1.7s: 촬영 수: 3 | OK 2 / NG 0
  3.4s: 촬영 수: 6 | OK 4 / NG 1
정지 후: 촬영 수: 6 | OK 4 / NG 2 | 정지
```

- 실행 중에는 OK+NG가 촬영 수보다 1 작다. 미리보기는 사진기가 바로 보내고, 검사는 그 뒤에 끝나기 때문이다. 정지 후에는 남은 것까지 다 검사돼서 6 = 4 + 2로 맞는다.
- **함정 (확인함):** 처음에 `basket.Add((Bitmap)frame.Clone())`으로 했더니 검사 일꾼의 `GetPixel`에서 `InvalidOperationException: 개체가 이미 다른 곳에서 사용 중입니다`가 나고 프로그램이 죽었다(3/3). `Bitmap.Clone()`은 픽셀 데이터를 원본과 **같이 쓰는** 얕은 복사라서, 사진기의 `frame.GetPixel`과 검사 일꾼의 `GetPixel`이 같은 데이터를 동시에 잠그려다 부딪힌다. `new Bitmap(frame)`은 픽셀을 **새로 복사**해서 문제없다. 다른 스레드로 넘길 사진은 `new Bitmap(...)`으로 복사한다.
- 실험: 검사에 `Thread.Sleep(1200)`을 넣으면(검사가 사진기보다 느림) 3.4초에 `촬영 수: 6 | OK 2 / NG 0`. 바구니에 쌓인다. 정지를 누르면 남은 4개를 다 검사할 때까지 **4.3초** 걸리고(그동안 창이 멈춤) 최종 `OK 4 / NG 2` (확인함). 하나도 안 버리는 대신 쌓이고, 정지가 느려진다 → 14교시.
- 질문: "`StopCamera`에서 `_cameraThread.Join()`보다 `_basket.CompleteAdding()`을 먼저 하면, 사진기가 아직 __ 중일 때 `Add`가 __. 그래서 사진기를 먼저 __ 뒤에 끝을 알린다."
- 사용자 답: "사진기가 아직 촬영 중일 때 ADD가 에러가 난다. 그래서 사진이를 먼저 작업자 뒤에 끝을 알린다." 빈칸 1 촬영 ✅, 2 에러 ✅(정확히는 `InvalidOperationException`, 사진기 스레드에서 안 잡혀서 프로그램이 죽음), 3 △ 문장이 꼬임 → "사진기를 먼저 멈추고(`Join`으로 퇴근 확인한) 뒤에". 보충: 정지 신호를 받으면 사진기는 대개 `WaitOne(500)` 안에서 자고 있다가 바로 나가므로 이 사고는 **가끔**만 난다(한 장 만드는 몇 ms 사이에 눌렀을 때). 가끔 나는 사고일수록 순서로 막는다.
- 사용자 코드 확인 (포트폴리오 `PipelineLab/Form1.cs`, 미커밋): ①~⑥ 원본과 같음, 9교시 `QueuePreview` 중복 호출도 정리됨. 빠진 곳 1개: `ShowPendingPreview` 안의 `ShowResult();` → 실행 중에는 OK/NG가 0에 머물고 정지할 때만 바뀐다(힌트로 전달). `InspectLoop` 끝 `Debug.WriteLine("검사 스레드 종료")` 없음(동작 영향 없음). `KataP13.cs`도 있음.

**13교시 완료 (2026-11-13).** 조각 1 ✅, 조각 2 빈칸 1·2 ✅ 3 △. 다음은 14교시 바구니가 꽉 차면?(`boundedCapacity`, `TryAdd`, 정책 선택, 버린 수 카운터).

### 14교시 · 바구니가 꽉 차면? (`boundedCapacity`) — 진행 중 (2026-11-13 시작)

코드는 `Katas/Concurrency/KataP14.cs`, `Program.cs`는 `KataP14.Run();`. 조각 예정: ① 작은 바구니 = 꽉 차면 기다리기 ② `TryAdd` = 꽉 차면 새것 버리기 ③ 옛것 버리고 새것 넣기 ④ 작품에 정책 선택 + 버린 수.
시작 전 확인: 작품 `ShowPendingPreview`에 `ShowResult();` 아직 없음(13교시 힌트 미반영).

#### 조각 1 · 작은 바구니: 꽉 차면 사진기가 기다린다

🧒 바구니에 사과가 3개까지만 들어간다. 꽉 차 있으면 사진기는 검사 일꾼이 하나 꺼내 갈 때까지 손에 사과를 든 채 **기다린다**.

```csharp
        public static void Run()
        {
            Chunk1();
        }

        // 14교시 조각 1: 작은 바구니(boundedCapacity) - 꽉 차면 사진기가 기다린다
        public static void Chunk1()
        {
            BlockingCollection<int> basket = new BlockingCollection<int>(3); // 최대 3개
            Stopwatch watch = Stopwatch.StartNew();
            Thread inspector = new Thread(() => InspectLoop(basket));
            inspector.Start();

            for (int apple = 1; apple <= 8; apple++)
            {
                basket.Add(apple);   // 꽉 차 있으면 자리가 날 때까지 기다린다
                Console.WriteLine("넣음: 사과 " + apple + " (바구니 " + basket.Count + "개)");
                Thread.Sleep(10);    // 사진기: 10ms마다 한 개 (아주 빠름)
            }
            Console.WriteLine("사진기 끝: " + watch.ElapsedMilliseconds + "ms");

            basket.CompleteAdding();
            inspector.Join();
            Console.WriteLine("검사 끝: " + watch.ElapsedMilliseconds + "ms");
            basket.Dispose();
        }

        private static void InspectLoop(BlockingCollection<int> basket)
        {
            foreach (int apple in basket.GetConsumingEnumerable())
            {
                Thread.Sleep(100);   // 검사: 100ms
            }
        }
```

| 코드 | 뜻 |
|---|---|
| `new BlockingCollection<int>(3)` | 바구니 한도(boundedCapacity) 3개. 숫자를 안 주면 한도 없음 |
| `basket.Add(apple)` (한도 있을 때) | 꽉 차 있으면 자리가 날 때까지 **기다린다**(Blocking). 비었을 때 꺼내는 쪽이 기다린 것과 반대 방향 |
| `basket.Count` | 지금 바구니에 든 개수. 다른 스레드가 바로 꺼낼 수 있어서 "그 순간"의 값일 뿐 |
| `Stopwatch` | 2교시에서 쓴 초시계 |

예상 출력 (Claude PC 5회, 숫자 줄은 5회 같음, ms만 ±2):

```text
넣음: 사과 1 (바구니 1개)
넣음: 사과 2 (바구니 1개)
넣음: 사과 3 (바구니 2개)
넣음: 사과 4 (바구니 3개)
넣음: 사과 5 (바구니 3개)
넣음: 사과 6 (바구니 3개)
넣음: 사과 7 (바구니 3개)
넣음: 사과 8 (바구니 3개)
사진기 끝: 417ms
검사 끝: 809ms
```

- 사과 1은 검사 일꾼이 바로 꺼내서 바구니에 안 남는다. 4부터는 늘 3개 → 사진기는 100ms(검사 한 개)마다 한 개씩만 넣을 수 있다. 10ms 사진기가 검사 속도로 **느려진다**(사진기 끝 417ms).
- 실험: `(3)`을 지워 한도를 없애면 바구니가 1·1·2·3·4·5·6·7개로 커지고 사진기 끝 **88ms**, 검사 끝은 같은 809ms (3회 확인). 한도 없음 = 사진기는 빨리 끝나지만 쌓인다. 한도 있음 = 쌓이지 않지만 사진기가 기다린다.
- 질문: "한도 3인 바구니가 꽉 차면 `Add`는 __. 그래서 사진기가 아무리 빨라도 실제로는 __ 속도로 찍게 된다. 진짜 카메라는 기다려 주지 않으니 꽉 찼을 때 __ 방법이 필요하다."
- 사용자 답: "기다린다 / 느린 속도 / 버리는" 빈칸 1 ✅, 2 △ "느린" → 핵심 단어는 **검사(일꾼) 속도**(뒤에서 꺼내 가는 속도에 맞춰진다, 출력 100ms마다 한 개), 3 ✅. 약점 "왜?를 핵심 단어로" 한 번 더.

#### 조각 2 · `TryAdd`: 꽉 차면 새 사과를 버린다

🧒 바구니가 꽉 차 있으면 사진기는 기다리지 않고 방금 찍은 사과를 **바닥에 버리고** 다음 사과를 찍는다.

`KataP14.cs`에 `Chunk2()`와 `InspectAndPrint()`를 추가하고 `Run()`에서 `Chunk1();`을 주석, `Chunk2();` 추가.

```csharp
        // 14교시 조각 2: TryAdd - 꽉 차면 새 사과를 버린다
        public static void Chunk2()
        {
            BlockingCollection<int> basket = new BlockingCollection<int>(3); // 최대 3개
            int dropped = 0;                       // 버린 수 (사진기 혼자 씀)
            Stopwatch watch = Stopwatch.StartNew();
            Thread inspector = new Thread(() => InspectAndPrint(basket));
            inspector.Start();

            for (int apple = 1; apple <= 8; apple++)
            {
                if (!basket.TryAdd(apple))         // 자리가 없으면 기다리지 않고 false
                {
                    dropped++;
                    Console.WriteLine("버림: 사과 " + apple);
                }
                Thread.Sleep(10);                  // 사진기: 10ms마다 한 개
            }
            Console.WriteLine("사진기 끝: " + watch.ElapsedMilliseconds + "ms, 버림 " + dropped + "개");

            basket.CompleteAdding();
            inspector.Join();
            basket.Dispose();
        }

        private static void InspectAndPrint(BlockingCollection<int> basket)
        {
            foreach (int apple in basket.GetConsumingEnumerable())
            {
                Console.WriteLine("    검사: 사과 " + apple);
                Thread.Sleep(100);   // 검사: 100ms
            }
        }
```

| 코드 | 뜻 |
|---|---|
| `basket.TryAdd(apple)` | 자리가 있으면 넣고 `true`, 꽉 차 있으면 **기다리지 않고** `false` |
| `!` | 거꾸로(`true` ↔ `false`). "못 넣었으면" |
| `int dropped` (그냥 `++`) | 사진기 스레드 **혼자만** 쓰고, 읽는 것도 사진기가 `Join` 전에 한다 → `Interlocked` 필요 없음 |

예상 출력 (Claude PC 8회, ms만 85~93 차이):

```text
    검사: 사과 1
버림: 사과 5
버림: 사과 6
버림: 사과 7
버림: 사과 8
사진기 끝: 90ms, 버림 4개
    검사: 사과 2
    검사: 사과 3
    검사: 사과 4
```

- 사과 1은 바로 검사, 2·3·4가 바구니를 채움 → 5~8은 들어갈 자리가 없어서 버림. 사진기는 기다리지 않아서 **90ms**에 끝(조각 1은 417ms).
- 버리는 건 **새 사과**다. 바구니에는 오래된 2·3·4가 남아 검사된다. 화면·결과가 "지금"보다 늦다.
- 실험: 한도를 `(10)`으로 → `버림 0개`, 1~8 모두 검사 (확인함). 한도가 크면 안 버리지만 그만큼 쌓인다.
- 질문: "`TryAdd`는 꽉 차 있으면 __ 않고 `false`를 돌려준다. 그래서 사진기는 늦어지지 않지만 버려지는 건 __ 사과이고, 검사되는 건 바구니에 남은 __ 사과다."
- 사용자 답: "add 하지 않고 / add가 안 된 사과 / 바구니에 남은 add된 사과" 빈칸 1 △ 사실은 맞지만 핵심 단어는 **기다리지**(조각 1 `Add`와의 차이), 2 △ → **새(방금 찍은)** 사과, 3 △ → **오래된** 사과. 동작은 이해했고 "새것 vs 오래된 것"이라는 핵심 단어가 빠짐(약점 "핵심 단어" 반복) → 조각 3(옛것 버리기)에서 다시 확인.

**여기서 멈춤 (2026-11-13 퇴근).** 14교시 조각 1·2 완료. 이어서 할 것: 조각 3 옛것 버리고 새것 넣기(`TryTake` 후 `TryAdd`) → 조각 4 작품에 정책 선택(기다리기/새것 버리기/옛것 버리기) + 버린 수 → 15교시 테스트 +3 → 16교시 2일차 정리. 포트폴리오에 `KataP14.cs`가 없으면 조각 1·2 코드를 먼저 친다. 작품 `ShowPendingPreview`의 `ShowResult();` 힌트도 확인.
