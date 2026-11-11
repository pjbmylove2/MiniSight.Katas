# Day 08 (월, 2h) · 일꾼 만들기: Thread, ThreadPool, Task, LongRunning

> 2주차는 이 스터디의 심장이다. 이번 주는 밀려도 **3주차를 줄이고 2주차를 지킨다.**

## 1. 오늘 목표와 이번 회차 완료 범위

- `new Thread`, `Task.Run`, `Task.Factory.StartNew(LongRunning)`의 차이를 표로 설명한다.
- "프로그램 내내 도는 일꾼은 왜 전용 스레드인가"를 **스레드풀 고갈**로 설명한다.
- `Thread.Sleep` 대신 **깨울 수 있는 잠(`WaitOne(timeout)`)**을 쓰는 이유를 실험으로 확인한다.
- MiniSight `HealthMonitor`(전용 스레드, 1초 주기, Dispose 때 즉시 종료)를 작성한다.
- VS2022 **스레드 창**에서 스레드 이름과 Freeze/Thaw를 써 본다.

2026-11-10 하루에 조각 0~7을 진행하고 약 70% 기준으로 마무리했다. 조각 코드는 포트폴리오 `Katas/Day08/Day08Kata.cs`에 조각마다 메서드 하나(`Chunk0()` ~ `Chunk5()`, `Chunk7Practice()`)로 남겼다. 모든 Kata 출력은 C# 7.3 / .NET Framework 4.7.2 / x64로 직접 컴파일·실행해서 확인했다(스레드 번호와 ms는 PC마다 다르다).

## 2. 초1 비유 · 그림 한 장

```text
식당(프로그램)
 ├ 사장님 = UI 스레드 (혼자서 주문도 받고 설거지까지 하면 → 화면 멈춤)
 ├ 정규직 = new Thread        하루 종일 같은 일 (감시, 로그)
 ├ 알바   = Task.Run (스레드풀)  잠깐 하는 일, 끝나면 돌려보냄
 └ 알바를 하루 종일 붙잡으면 → 다른 일이 알바를 못 구함 (스레드풀 고갈)
```

- **깨울 수 있는 잠 = 호출벨.** `Thread.Sleep(1000)`은 귀마개 끼고 자는 것(퇴근하라고 해도 다 잘 때까지 못 들음). `stop.WaitOne(1000)`은 호출벨이 울리면 바로 깬다.

| 조각 | 내용 |
|---|---|
| 0 | 일꾼 한 명 보내기: `new Thread`, `Start`, `Join` |
| 1 | 이름표와 퇴근 규칙: `Name`, `IsBackground` |
| 2 | 귀마개 잠 vs 호출벨 잠: `Thread.Sleep` vs `WaitOne` |
| 3 | 정규직 클래스: `EggBoiler` + `Dispose` |
| 4 | 알바(`Task.Run`)와 정규직의 차이 |
| 5 | 알바를 붙잡으면: 스레드풀 고갈 + `LongRunning` |
| 6 | MiniSight `HealthMonitor` |
| 7 | VS 스레드 창: 이름 보기, 얼리기 |

## 2-1. 조각별 실습

### 조각 0 · 일꾼 한 명 보내기 (`Start` / `Join`)

사장님(Main)이 일꾼을 설거지하러 보내 놓고 **기다리지 않고** 바로 다른 일을 한다. 마지막에 "다 했어?" 하고 주방 앞에서 기다린다(`Join`).

```csharp
public static void Chunk0()
{
    Console.WriteLine("사장님 스레드 번호: " + Thread.CurrentThread.ManagedThreadId);

    Thread worker = new Thread(WashDishes3);     // 일꾼 뽑기 (아직 일 안 함)
    worker.Start();                              // 일꾼 출발
    Console.WriteLine("사장님: 일꾼 보내 놓고 바로 손님 주문 받는 중");

    worker.Join();                               // 일꾼이 끝날 때까지 기다림
    Console.WriteLine("사장님: 설거지 끝난 것 확인");
}

private static void WashDishes3()
{
    for (int i = 1; i <= 3; i++)
    {
        Thread.Sleep(200);
        Console.WriteLine("  일꾼(스레드 " + Thread.CurrentThread.ManagedThreadId + "): 접시 " + i + "개 씻음");
    }
}
```

| 코드 | 역할 | 같은 뜻의 긴 코드 |
|---|---|---|
| `new Thread(WashDishes3)` | 새 일꾼을 만듦. 아직 출발 안 함 | `new Thread(new ThreadStart(WashDishes3))` |
| `worker.Start()` | 일꾼 출발. 이 줄은 **기다리지 않고** 다음 줄로 감 | — |
| `worker.Join()` | 일꾼이 끝날 때까지 여기서 멈춰서 기다림 | — |
| `Thread.CurrentThread.ManagedThreadId` | 지금 이 줄을 실행하는 일꾼의 번호표 | — |

```text
사장님 스레드 번호: 1
사장님: 일꾼 보내 놓고 바로 손님 주문 받는 중
  일꾼(스레드 3): 접시 1개 씻음
  일꾼(스레드 3): 접시 2개 씻음
  일꾼(스레드 3): 접시 3개 씻음
사장님: 설거지 끝난 것 확인
```

**실험:** `worker.Join();`을 주석 처리하면 `설거지 끝난 것 확인`이 접시보다 먼저 찍히고, 그래도 일꾼은 접시 3개를 끝까지 씻은 뒤 프로그램이 끝난다.

> **Q. 메인 스레드가 끝나려면 작업자 스레드가 완료되어야 하나?**
> 아니다. **메인 스레드는 먼저 끝난다**(실험에서 `설거지 끝난 것 확인`이 먼저 찍힘). 기다리는 것은 **프로세스**다. `new Thread`로 만든 일꾼은 기본이 **전경(foreground) 스레드**라서, 전경 스레드가 하나라도 남아 있으면 프로세스가 꺼지지 않는다. 비유: 사장님은 퇴근했지만 "정규직이 남아 있으면 가게 불을 안 끈다".
>
> | 누가 | 언제 끝나나 |
> |---|---|
> | 메인 스레드 | `Main`의 마지막 줄이 끝나면 바로 |
> | 프로세스 | 전경 스레드가 **모두** 끝나면 |

<details><summary>질문과 사용자 확인</summary>

질문: `Join`을 지웠는데도 Main이 끝난 뒤 일꾼이 접시를 다 씻고서야 콘솔이 끝난 이유는?

답(2026-11-10): "작업자 스레드가 안 끝나서" ✅. 덧붙인 질문의 "메인이 끝나려면 작업자가 완료돼야 하나?"는 위 Q처럼 메인이 아니라 프로세스가 기다린다고 정정.
</details>

### 조각 1 · 이름표와 퇴근 규칙 (`Name` / `IsBackground`)

- **전경 스레드 = 정규직**: 자기 일이 끝날 때까지 가게 불을 못 끄게 한다.
- **백그라운드 스레드 = 알바**: 사장님이 불을 끄면 일하다 말고 바로 퇴근한다.

```csharp
public static void Chunk1()
{
    Thread worker = new Thread(WashDishes5);
    worker.Name = "설거지 일꾼";        // 이름표
    worker.IsBackground = true;        // 알바: 가게 불 꺼지면 같이 퇴근
    worker.Start();

    Thread.Sleep(500);                 // 사장님이 0.5초만 있다가
    Console.WriteLine("사장님: 퇴근! (Main 끝)");
}

private static void WashDishes5()
{
    for (int i = 1; i <= 5; i++)
    {
        Thread.Sleep(200);
        Console.WriteLine("  [" + Thread.CurrentThread.Name + "] 접시 " + i + "개 씻음");
    }
    Console.WriteLine("  [" + Thread.CurrentThread.Name + "] 설거지 완료");
}
```

| 코드 | 역할 | 주의 |
|---|---|---|
| `worker.Name = "..."` | 이름표. VS 스레드 창과 로그에 보임 | `Start` 전에 한 번만 정할 수 있음 |
| `worker.IsBackground = true` | 알바로 지정. 전경 스레드가 다 끝나면 일하던 도중에 같이 끝남 | `new Thread`의 기본값은 `false` |
| `Thread.CurrentThread.Name` | 지금 실행 중인 일꾼의 이름표 | 안 달았으면 `null` |

```text
  [설거지 일꾼] 접시 1개 씻음
  [설거지 일꾼] 접시 2개 씻음
사장님: 퇴근! (Main 끝)
```

접시 5개 중 2개만 씻고 잘렸다. `설거지 완료`는 안 찍힌다.

**실험:** `IsBackground = true`를 주석 처리하면 사장님 퇴근 뒤에도 접시 3~5개와 `설거지 완료`까지 찍힌다(조각 0과 같은 현상).

**결론:** 정규직은 "창은 닫았는데 exe가 안 꺼짐"을, 알바는 "끝나는 순간 일이 잘림"을 만든다. 실무 정답은 **알바(`true`)로 두고, 끝내기 전에 "그만해" 신호를 주고 `Join`으로 정리**(조각 2, 3).

<details><summary>질문과 사용자 확인</summary>

질문: 1초마다 메모리를 재서 로그를 남기는 감시 스레드는 `IsBackground`를 `true`로 할까, `false`로 할까?

1차 답: "false, 프로세스가 종료될 때까지 로그를 남겨야 해서" △. "그래서" 꼴은 좋았지만 반대. 감시는 **무한 루프**라서 정규직이면 스스로 끝나지 않고, **그래서 프로세스가 안 꺼진다**(exe가 남고 다시 켜면 "이미 실행 중"). 마지막 로그 걱정은 `true` + `Dispose`(신호 → `Join`)로 해결한다. 회사 `HardwareMonitorService`도 `IsBackground = true`.

재답: "false는 정규직, true는 비정규직. 무한 루프(감시, 통신 수신, 하트비트)면 알바 + Dispose로 정리" ✅(약 80%). "정규직은 영원히 반복 중"이 아니라 **무한 루프라서** 안 끝난다는 점, 결과인 **"프로세스가 안 꺼진다"**를 보충.
</details>

### 조각 2 · 귀마개 잠(`Sleep`) vs 호출벨 잠(`WaitOne`)

2초마다 순찰하는 경비원 둘에게 2.5초 뒤 "그만해!" 신호를 보내고, 퇴근까지 걸린 시간을 잰다.

```csharp
private static readonly ManualResetEvent _bell = new ManualResetEvent(false);   // 호출벨 (처음엔 안 울림)

public static void Chunk2()
{
    RunGuard("귀마개 경비", EarplugGuard);
    RunGuard("호출벨 경비", BellGuard);
}

private static void RunGuard(string name, ThreadStart work)
{
    _bell.Reset();                         // 벨 끄기
    Thread guard = new Thread(work);
    guard.Name = name;
    guard.IsBackground = true;
    guard.Start();

    Thread.Sleep(2500);
    Stopwatch sw = Stopwatch.StartNew();
    _bell.Set();                           // 벨 울림: "그만해!"
    guard.Join();
    Console.WriteLine(name + ": 그만하라고 한 뒤 " + sw.ElapsedMilliseconds + "ms 만에 퇴근");
    Console.WriteLine();
}

private static void EarplugGuard()
{
    while (!_bell.WaitOne(0))              // 벨 울렸나 힐끗 보기
    {
        Thread.Sleep(2000);                // 귀마개 끼고 2초 잠
        Console.WriteLine("  [" + Thread.CurrentThread.Name + "] 금고 확인");
    }
}

private static void BellGuard()
{
    while (!_bell.WaitOne(2000))           // 벨 들으면서 최대 2초 잠
    {
        Console.WriteLine("  [" + Thread.CurrentThread.Name + "] 금고 확인");
    }
}
```

| 코드 | 역할 | 비유 |
|---|---|---|
| `ManualResetEvent` | 스레드끼리 쓰는 신호. 켜짐/꺼짐 | 호출벨 |
| `new ManualResetEvent(false)` | 처음은 꺼짐 | 벨 설치, 조용함 |
| `.Set()` / `.Reset()` | 켜기(기다리던 스레드 즉시 깨어남) / 끄기 | 벨 누르기 / 소리 끄기 |
| `.WaitOne(2000)` | 최대 2초 기다림. 벨이 울리면 바로 `true`, 시간이 다 되면 `false` | 벨 옆 쪽잠 |
| `.WaitOne(0)` | 기다리지 않고 지금 상태만 확인 | 벨 힐끗 보기 |
| `ThreadStart` | 매개변수·반환 없는 메서드를 담는 타입 | 일거리 쪽지 |
| `Stopwatch.StartNew()` / `.ElapsedMilliseconds` | 초시계 시작 / 지난 ms | 초시계 |

```text
  [귀마개 경비] 금고 확인
  [귀마개 경비] 금고 확인
귀마개 경비: 그만하라고 한 뒤 1503ms 만에 퇴근

  [호출벨 경비] 금고 확인
호출벨 경비: 그만하라고 한 뒤 0ms 만에 퇴근

```

귀마개 경비의 두 번째 `금고 확인`(4초 시점)은 벨을 누른 뒤(2.5초)에 찍힌 것이다. 그만하라고 했는데 한 번 더 일했다.

**실험:** 2000을 둘 다 5000으로 바꾸면 귀마개 약 2500ms, 호출벨 0ms. 간격이 길수록 Sleep 쪽 퇴근이 늦어진다. 현장 증상: "종료 버튼을 누르면 몇 초 멈췄다가 꺼진다."

<details><summary>질문과 사용자 확인</summary>

질문: `WaitOne(2000)`이 `true`/`false`를 돌려주는 경우는? `while (!_stop.WaitOne(2000))`은 어느 쪽일 때 계속 돌고 어느 쪽일 때 빠져나오나?

답: "기다리다가 신호가 켜지면 true, 2초가 다 지나면 false. false면 루프를 돈다" ✅(약 85%). 보충 두 가지: ① 신호가 **꺼질 때**는 아무 일도 없다. 깨어나는 경우는 켜짐(true)과 시간 초과(false)뿐. ② **빠져나오는 경우**가 빠짐.

| 2초 사이에 | `WaitOne(2000)` | `!` 붙이면 | 루프 |
|---|---|---|---|
| 벨 안 울림 | `false` | `true` | 계속 (한 번 일하고 다시 대기) |
| 벨 울림 | `true` | `false` | 빠져나옴 → 퇴근 |
</details>

### 조각 3 · 정규직을 클래스로: `EggBoiler`

조각 0~2를 클래스 하나로 묶는다. 만들면 일을 시작하고, `Dispose`하면 **벨을 누르고 → 퇴근할 때까지(최대 2초) 기다리고 → 벨을 치운다.** 영희(0.3초), 철수(0.7초), 민수(5초)를 고용하고 1.65초 뒤 문을 닫는다.

```csharp
internal sealed class EggBoiler : IDisposable
{
    private readonly ManualResetEvent _stop = new ManualResetEvent(false);
    private readonly Thread _worker;
    private readonly int _boilMs;
    private bool _disposed;

    public EggBoiler(string name, int boilMs)
    {
        _boilMs = boilMs;
        _worker = new Thread(Run);
        _worker.Name = name;
        _worker.IsBackground = true;
        _worker.Priority = ThreadPriority.BelowNormal;
        _worker.Start();
    }

    private void Run()
    {
        int eggs = 0;
        while (!_stop.WaitOne(_boilMs))
        {
            eggs++;
            Console.WriteLine("  " + _worker.Name + ": 계란 " + eggs + "개");
        }
        Console.WriteLine("  " + _worker.Name + ": 퇴근");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        _stop.Set();
        if (!_worker.Join(2000))
        {
            Console.WriteLine("  " + _worker.Name + ": 2초 안에 안 끝남!");
        }
        _stop.Dispose();
    }
}

public static void Chunk3()
{
    Stopwatch sw = Stopwatch.StartNew();
    using (new EggBoiler("영희", 300))
    using (new EggBoiler("철수", 700))
    using (new EggBoiler("민수", 5000))
    {
        Thread.Sleep(1650);
    }
    Console.WriteLine("모두 퇴근까지 " + sw.ElapsedMilliseconds + "ms");
}
```

| 코드 | 역할 | 주의 |
|---|---|---|
| `Priority = ThreadPriority.BelowNormal` | CPU를 나눌 때 조금 뒤로 양보 | 감시처럼 급하지 않은 일. 검사보다 앞서면 안 됨 |
| `_worker.Join(2000)` | 최대 2초만 기다림. 끝나면 `true`, 시간 초과면 `false` | `Join()`(무한)보다 안전. 일꾼이 멈춰도 사장님이 갇히지 않음 |
| `_stop.Dispose()` | 벨(OS 자원) 반납 | **`Join` 다음에**. 일꾼이 아직 벨을 쓸 수 있음 |
| `using` 3줄 | 블록이 끝나면 아래부터 거꾸로 `Dispose` | Day06 "정지는 역순" |
| `if (_disposed) { return; }` | 두 번 불려도 한 번만 정리 | Day06 DeviceManager와 같음 |

```text
  영희: 계란 1개
  영희: 계란 2개
  철수: 계란 1개
  영희: 계란 3개
  영희: 계란 4개
  철수: 계란 2개
  영희: 계란 5개
  민수: 퇴근
  철수: 퇴근
  영희: 퇴근
모두 퇴근까지 1651ms
```

민수는 계란 0개지만 5초를 기다리지 않고 바로 퇴근했다. 퇴근 순서는 만든 순서의 거꾸로다.

**실험(귀마개 방식):** `Run`을 `while (!_stop.WaitOne(0)) { Thread.Sleep(_boilMs); ... }`로 바꾸면 `민수: 2초 안에 안 끝남!`이 찍히고, 그 2초 동안 영희·철수는 계속 삶고, 전체 약 4212ms. `민수: 퇴근`은 끝내 안 찍힌다(프로세스가 끝날 때 백그라운드라서 잘림). 이때 민수가 살아 있는데 `_stop.Dispose()`가 이미 실행됐으므로 민수가 깨어나면 `ObjectDisposedException` 위험이 숨어 있다. **`Join`이 실패하면 정리가 안 된 것**이라 로그를 남긴다.

> **사용자 실행에서 나온 버그 (2026-11-10):** 첫 타이핑에서 `Dispose`의 가드가 `if (!_disposed) { return; }`로 뒤집혀 있었다. 출력에 `퇴근`이 한 줄도 없었고, `Set`/`Join` 순서를 바꿔도 결과가 같았다(둘 다 실행되지 않았으니까). 단서는 **"퇴근 로그가 없다"**. Day07 VisionSystem 방송의 `if (handlers != null) { return; }`와 같은 종류다. 가드 `if` 조건 하나가 뒤집히면 그 아래 전체가 **조용히** 사라진다(예외도 없고 컴파일도 된다). 힌트(디버그 출력 두 줄, 원본과 한 글자씩 비교)로 사용자가 직접 고쳤다.

> **Q. `while (!_stop.WaitOne(_boilMs))`에서 `_boilMs`가 5000이면, 처음에 5초를 기다렸다가 아래로 가나? 아니면 처음엔 false니까 한 번 아래를 타고 그다음 5초를 기다리나?**
> **처음에 5초를 먼저 기다린다.** `while (조건)`은 조건을 먼저 계산하는데, 그 조건 자체가 "최대 5초 기다리기"다. false를 받으려면 먼저 5초를 다 기다려야 한다(바로 상태만 보는 것은 `WaitOne(0)`). 증거: 민수 0개, 영희 첫 계란이 0초가 아니라 0.3초.
> "먼저 일하고 그다음 기다리기"를 원하면 일을 앞으로 옮긴다(Day08 1회차 원본 Kata의 모양):
> ```csharp
> while (true)
> {
>     eggs++;
>     Console.WriteLine("  " + _worker.Name + ": 계란 " + eggs + "개");
>
>     if (_stop.WaitOne(_boilMs))
>     {
>         break;
>     }
> }
> ```
> 현장 예: 하트비트 간격이 5초면 위쪽 모양은 켜고 **첫 하트비트가 5초 뒤**라, "3초 안에 첫 신호 없으면 끊김"으로 보는 상대 장비에서 켜자마자 통신 이상이 뜰 수 있다. 조각 6 `HealthMonitor`는 생성자에서 한 번 먼저 재서 이 문제를 피한다.

<details><summary>질문과 사용자 확인</summary>

질문: `Dispose`에서 `_stop.Set();`과 `_worker.Join(2000)`의 순서를 바꾸면(Join 먼저) 출력과 전체 시간은?

답: "민수는 Join(2000)에서 2초 초과 로그를 남기고 Set으로 죽고, 철수·영희는 로그 없이 Set으로 바로 죽는다" △. 민수 부분 ✅, 철수·영희 ❌. **벨은 객체마다 따로**(`_stop`이 `EggBoiler`마다 하나)라서 철수·영희도 `Join`이 먼저면 아무도 벨을 안 눌렀으므로 각자 2초씩 시간 초과가 난다. 직접 실행 확인:

```text
  ...
  민수: 2초 안에 안 끝남!
  민수: 퇴근
  ...
  철수: 2초 안에 안 끝남!
  철수: 퇴근
  ...
  영희: 2초 안에 안 끝남!
모두 퇴근까지 7651ms
  영희: 퇴근
```

1651ms → 7651ms(2초 × 3명). 마지막 `영희: 퇴근`이 `모두 퇴근까지` **뒤에** 찍힌 것도 볼 것: `Set` 직후 확인(`Join`) 없이 끝나서 정리 확인이 사라졌다. 정리: **신호(`Set`)를 먼저 줘야 `Join`이 바로 끝난다.**
</details>

### 조각 4 · 알바(`Task.Run`)와 정규직(`new Thread`)

```csharp
public static void Chunk4()
{
    Report("사장님");

    Thread worker = new Thread(() => Report("정규직"));
    worker.Start();
    worker.Join();

    Task<int> job = Task.Run(() =>
    {
        Report("알바");
        return 3 + 4;
    });
    Console.WriteLine("알바가 가져온 결과: " + job.Result);
}

private static void Report(string who)
{
    Thread me = Thread.CurrentThread;
    Console.WriteLine(who + ": 스레드 " + me.ManagedThreadId
        + ", 스레드풀? " + me.IsThreadPoolThread
        + ", 백그라운드? " + me.IsBackground);
}
```

| 코드 | 역할 | 같은 뜻 |
|---|---|---|
| `() => Report("정규직")` | 이름 없는 작은 메서드(람다) | 메서드 하나 만들어 `new Thread(X)` |
| `Task.Run(() => { ...; return 3 + 4; })` | 스레드풀 알바에게 맡기고 바로 영수증(`Task`)을 받음 | — |
| `Task<int>` | 나중에 `int` 결과가 나오는 영수증 | — |
| `job.Result` | 결과가 나올 때까지 기다렸다가 꺼냄 | `job.Wait();` 후 결과 읽기 |
| `IsThreadPoolThread` | 스레드풀 출신이면 `true` | — |

```text
사장님: 스레드 1, 스레드풀? False, 백그라운드? False
정규직: 스레드 3, 스레드풀? False, 백그라운드? False
알바: 스레드 4, 스레드풀? True, 백그라운드? True
알바가 가져온 결과: 7
```

스레드풀 스레드는 **처음부터 백그라운드**다(바꾸지 않는다).

**실험(`Chunk4Exception`):** 알바 안에서 `throw new InvalidOperationException("계란 깨뜨림")` → `job.Result`를 `try`/`catch (AggregateException ex)`로 감싸면 `사장님이 받은 예외: 계란 깨뜨림`, `사장님: 가게는 계속 영업 중`. 알바의 예외는 영수증에 담겨 `Result`에서 **`AggregateException`으로 한 겹 포장되어** 다시 나온다(진짜 원인은 `InnerException`, `await`는 포장을 벗겨 줌).

| | 정규직 `new Thread` | 알바 `Task.Run` |
|---|---|---|
| 출신 | 새로 뽑음 (비쌈) | 스레드풀에서 빌림 (쌈, 빠름) |
| 기본 백그라운드 | `false` | `true` |
| 이름·우선순위 | 정할 수 있음 | 정하지 않음 (빌린 사람) |
| 결과 | 없음 (`Join`은 "끝났다"만) | `Task<T>.Result` |
| 예외 | 안에서 안 잡으면 **프로세스가 죽음** (이번 Kata에서는 실행 안 함) | 영수증에 담겨 `Result`/`Wait`/`await`에서 다시 던져짐 |
| 어울리는 일 | 프로그램 내내 도는 일 | 잠깐 하는 일 |

> **Q. FA 현장처럼 24시간, 한 달 이상 도는 프로그램에서는 역할에 따라 Thread / Task 선택 기준이 있어야 하지 않나?**
> 있다. 한 줄 기준: **오래 사는 일 = 전용 스레드, 잠깐 하는 일 = 스레드풀(Task).**
>
> | 순서 | 질문 | 예 → |
> |---|---|---|
> | 1 | 프로그램 내내 도나? (무한 루프) | 전용 `Thread` (또는 `LongRunning`) |
> | 2 | 안에서 오래 막히나? (소켓 수신, 장비 응답 대기) | 전용 `Thread` |
> | 3 | 이름·우선순위·STA가 필요한가? | `Thread` |
> | 4 | 짧게 끝나고 결과·예외를 받아야 하나? | `Task.Run` + `await` |
>
> | 역할 | 보통 선택 | 이유 |
> |---|---|---|
> | PLC 수신 루프 | 전용 Thread | 무한 루프 + 수신 대기에서 막힘 |
> | 하트비트 / 상태 감시 | 전용 Thread + `WaitOne(간격)` | 무한 루프, 종료 신호에 즉시 반응 |
> | 검사 처리 | 전용 Thread가 큐에서 꺼내 처리 | 순서·타이밍 중요, 프레임마다 Task면 폭주 때 풀 고갈 |
> | 이미지 저장 / 로그 기록 | 큐 + 전용 스레드 하나 | 디스크가 느려지면 알바가 줄줄이 붙잡힘, 동시 쓰기 방지 |
> | 버튼 1번 계산, 레시피 로드 | `Task.Run` + `await` | 짧음, 결과·예외 필요, 화면 안 멈춤 |
> | 카메라 콜백 | 드라이버가 정한 스레드 | 빨리 큐에 넣고 나가기만 (Day07 `OnFrame`) |
>
> 24시간 장비라서 추가로: ① 전용 Thread 루프 **안**에 `try/catch` + 로그(조각 6), ② 멈추는 길 `Set → Join(시간제한) → Dispose`(조각 3), ③ 스레드 수가 고정인지 감시(조각 6), ④ 몰려드는 일은 크기 제한 큐로 받기(Day09~11).

<details><summary>질문과 사용자 확인</summary>

질문: 조각 3 `EggBoiler`가 `Task.Run`이 아니라 `new Thread`를 고른 이유 두 가지는?

답: "정규직 Thread는 프로그램 내내 도는 일에 어울린다. 그래서 Thread를 골랐다" △. 표의 결론을 옮겼고 이유는 한 가지, "왜 어울리나"가 빠짐. 보충:
① 생성자에서 **이름·우선순위**를 정한다. 빌린 스레드풀 스레드는 바꾸면 안 된다(다음 사람이 물려받음).
② 무한 루프로 스레드풀 스레드를 **영원히 붙잡으면** 다른 짧은 일(`Task.Run`, 타이머, `await` 뒤 코드)이 사람을 못 구해 기다린다 = **스레드풀 고갈**.
</details>

### 조각 5 · 알바를 붙잡으면: 스레드풀 고갈 + `LongRunning`

스레드풀 알바를 전부 3초씩 붙잡아 놓고, 아무것도 안 하는 일 하나가 시작되기까지 기다린 시간을 잰다.

```csharp
public static void Chunk5()
{
    int min;
    int io;
    ThreadPool.GetMinThreads(out min, out io);
    Console.WriteLine("인력사무소 즉시 대기 인원: " + min + "명");

    Task[] blockers = new Task[min + 4];
    for (int i = 0; i < blockers.Length; i++)
    {
        blockers[i] = Task.Run(() => Thread.Sleep(3000));   // 3초 동안 붙잡기
    }
    Console.WriteLine("알바 " + blockers.Length + "명을 3초씩 붙잡음");

    Stopwatch sw = Stopwatch.StartNew();
    Task.Run(() => { }).Wait();                             // 아무것도 안 하는 일
    Console.WriteLine("아주 짧은 일 대기 시간: " + sw.ElapsedMilliseconds + "ms");

    Task.WaitAll(blockers);
}
```

| 코드 | 역할 | 비유 |
|---|---|---|
| `ThreadPool.GetMinThreads(out min, out io)` | 스레드풀이 바로 내보내는 인원. 보통 CPU 논리 코어 수 | 사무소 대기 인원 |
| `out` | 메서드가 값을 돌려주는 칸 | 빈 바구니를 채워 돌려줌 |
| `Task[]` | 영수증 배열 | 영수증 묶음 |
| `Task.WaitAll(blockers)` | 전부 끝날 때까지 기다림 | 모든 알바 귀가 확인 |

```text
인력사무소 즉시 대기 인원: 28명
알바 32명을 3초씩 붙잡음
아주 짧은 일 대기 시간: 3000ms
```

28명은 이 PC의 코어 수다. 집 PC는 숫자가 다르고 대기 시간도 수백 ms ~ 3000ms로 달라질 수 있다. 핵심은 **빈 일인데 0ms가 아니라는 것.**

**실험:** `Task.Run(...)` 줄을 `Task.Factory.StartNew(() => Thread.Sleep(3000), TaskCreationOptions.LongRunning)`으로 바꾸면 **0ms**. `LongRunning`은 스레드풀에서 빌리지 않고 **전용 스레드를 새로 만들어** 실행한다(Task의 결과·예외·`await` 편의는 그대로).

현장 증상: "가끔 화면 갱신이 몇 초 늦다", "타이머 이벤트가 밀려서 한꺼번에 들어온다", "`await` 뒤 코드가 늦게 실행된다". 느린 줄이 없는데 느리다. 스레드풀 스레드에서 `Sleep`, 동기 소켓 수신, `.Result`, `.Wait()`로 **붙잡는 다른 코드**가 원인인 경우가 많다.

<details><summary>질문과 사용자 확인</summary>

질문: 빈 람다가 3초를 기다린 이유를 "그래서"를 넣어 한 줄로, `LongRunning`으로 0ms가 된 이유도 한 줄로.

1차 답: 강사 설명 문장을 그대로 옮김 → 내용은 맞지만 자기 말 확인을 위해 빈칸 틀(① ○○ 때문에 붙잡혀 → 그래서 ○○, ② ○○에서 안 빌림 → 그래서 ○○)로 재요청.

재답: "빈 람다가 3초를 기다린 건 32명이 Sleep에 붙잡혀 있었다. 그래서 LongRunning 옵션을 주어 전용 스레드를 새로 만든다고 해서 바로 처리했다" ✅(약 75%). 보충: `LongRunning`을 붙인 쪽은 빈 일이 아니라 **붙잡는 32개**다. 빈 일은 전후 모두 스레드풀 알바가 했고, 달라진 것은 "알바가 남아 있느냐"뿐. **고칠 대상은 느려진 쪽이 아니라 붙잡은 쪽.**
</details>

### 조각 6 · MiniSight `HealthMonitor`

조각 3 `EggBoiler` 모양 그대로 1초마다 프로세스 건강 상태(메모리, 스레드 수, 핸들 수)를 재서 **불변 종이(`HealthSnapshot`)**를 게시판(`Latest`)에 갈아 끼운다. 비유: 1초마다 공장 체온을 재서 게시판에 새 종이를 붙이는 경비원.

`MiniSight/App/HealthSnapshot.cs`

```csharp
public sealed class HealthSnapshot
{
    public HealthSnapshot(DateTime time, long workingSetMb, long privateMb, int threads, int handles)
    {
        Time = time;
        WorkingSetMb = workingSetMb;
        PrivateMb = privateMb;
        Threads = threads;
        Handles = handles;
    }

    public DateTime Time { get; }
    public long WorkingSetMb { get; }
    public long PrivateMb { get; }
    public int Threads { get; }
    public int Handles { get; }

    public override string ToString()
    {
        return Time.ToString("HH:mm:ss") + " 메모리 " + WorkingSetMb + "MB / 전용 " + PrivateMb
            + "MB / 스레드 " + Threads + "개 / 핸들 " + Handles + "개";
    }
}
```

`MiniSight/App/HealthMonitor.cs`

```csharp
public sealed class HealthMonitor : IDisposable
{
    private readonly ManualResetEvent _stop = new ManualResetEvent(false);
    private readonly Thread _worker;
    private readonly int _intervalMs;
    private HealthSnapshot _latest;
    private bool _disposed;

    public HealthMonitor(int intervalMs)
    {
        _intervalMs = intervalMs;
        _latest = Measure();                        // 켜자마자 1번 재 두기

        _worker = new Thread(Run);
        _worker.Name = "MiniSight health";
        _worker.IsBackground = true;
        _worker.Priority = ThreadPriority.BelowNormal;
        _worker.Start();
    }

    public HealthSnapshot Latest
    {
        get { return Volatile.Read(ref _latest); }
    }

    private void Run()
    {
        while (!_stop.WaitOne(_intervalMs))
        {
            try
            {
                Volatile.Write(ref _latest, Measure());
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[health] 측정 실패: " + ex.Message);
            }
        }
    }

    private static HealthSnapshot Measure()
    {
        using (Process me = Process.GetCurrentProcess())
        {
            return new HealthSnapshot(
                DateTime.Now,
                me.WorkingSet64 / 1024 / 1024,
                me.PrivateMemorySize64 / 1024 / 1024,
                me.Threads.Count,
                me.HandleCount);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        _stop.Set();
        if (!_worker.Join(1000))
        {
            Debug.WriteLine("[health] 1초 안에 안 끝남");
        }
        _stop.Dispose();
    }
}
```

`Form1`: 필드 `private HealthMonitor _health;`, 생성자에서 `_health = new HealthMonitor(1000);`, **건강** 버튼에서 `MessageBox.Show(_health.Latest.ToString());`, `OnFormClosed`에서 `_system?.Dispose();` 다음 줄에 `_health?.Dispose();`(먼저 만든 경비원이 맨 마지막에 퇴근).

| 코드 | 역할 | 비유 / 주의 |
|---|---|---|
| `public int Threads { get; }` | 읽기 전용 속성. 생성자에서만 값을 넣음 | 한 번 붙이면 못 고치는 종이(불변) |
| `public override string ToString()` | 글자로 바꾸는 방법을 내가 정함 | — |
| `Volatile.Write(ref _latest, 새 값)` / `Volatile.Read(ref _latest)` | 경비원이 새 종이를 확실히 게시 / 다른 스레드가 가장 최근 종이를 읽음 | Day09에서 자세히 |
| `ref` | 변수의 복사본이 아니라 그 칸 자체를 넘김 | 게시판 위치 |
| `using (Process me = ...)` | `Process`도 `IDisposable` | 1초마다 새로 만드니 안 반납하면 핸들이 샘 |
| `Debug.WriteLine` | F5 때 VS 출력 창에 찍힘 | MiniSight는 WinExe라 `Console`이 안 보임 |

Day08.md 1회차 계획은 "Bootstrapper가 만들고 정리"였지만, Day07에서 정리했듯 Bootstrapper는 아무것도 들고 있지 않은 static 조립 공장이다. 그래서 **필드를 가진 Form1이 만들고 Form1이 정리**하도록 했다("누가 만들었나 = 누가 정리하나"는 그대로).

같은 두 클래스를 콘솔 데모로 확인한 출력(100MB 할당 + 스레드 5개 부하):

```text
시작 직후: 16:10:05 메모리 16MB / 전용 12MB / 스레드 6개 / 핸들 230개
1초 뒤:    16:10:06 메모리 17MB / 전용 12MB / 스레드 7개 / 핸들 236개
부하 뒤:   16:10:07 메모리 118MB / 전용 116MB / 스레드 13개 / 핸들 266개
정리 끝
```

**사용자 MiniSight 실행(2026-11-10):** 건강 → 준비 → 시작 → 건강 → 정지 → 건강에서 스레드 수 **12 → 16 → 16**.

> **해석:** 설정이 `Kind: 0`(Fake 카메라)이고 `FakeCameraDriver`는 `System.Threading.Timer`로 0.7초마다 프레임을 만든다. 타이머 콜백은 **스레드풀**에서 돌기 때문에 시작하면 타이머 관리·콜백 처리 스레드(및 OpenCvSharp 내부 스레드 가능성)가 생긴다. `Stop()`은 `_timer.Change(Timeout.Infinite, ...)`로 타이머를 정확히 멈추지만, 스레드풀은 한가해진 스레드를 **한동안 살려 둔다**(대기실의 알바). 그래서 정지 뒤에도 그대로다.
>
> | 모양 | 의미 |
> |---|---|
> | 시작 때 한 번 늘고 그 뒤로 그대로 | 정상 (대기 중인 스레드) |
> | 시작/정지할 때마다 계속 늘어남 | 누수 (정리 안 되는 스레드·타이머) |
>
> 남은 4개의 정확한 정체는 확인하지 않았다(조각 7 참고). 시작/정지 5번 반복 후 스레드 수 확인은 선택 실험으로 남김.

<details><summary>질문과 사용자 확인</summary>

질문: `Run`의 `try/catch`를 `while` **바깥**으로 옮기면, `Measure()`가 딱 한 번 실패했을 때 무슨 일이 생기나?

답: "에러가 난 뒤로 측정을 하지 않는다. 그래서 while 문 안에 있어야 에러가 발생해도 다음 턴에 측정 플로우에 들어간다" ✅. 원인과 해결을 "그래서"로 연결함. 보충: 바깥이면 catch로 점프 → `while` 탈출 → `Run` 끝 → **경비원이 조용히 퇴근**. 예외를 잡았으니 프로그램은 멀쩡하고 `Latest`는 옛날 종이 그대로라 감시가 멈춘 것을 모른다. 그래서 `HealthSnapshot`에 `Time`을 넣었다(시간이 안 바뀌면 경비원이 죽은 것). 기준: **실패하면 어디까지 포기할 건가** — 한 바퀴만이면 루프 안, 전체면 루프 밖. Day06에서 막혔던 try 위치를 이번엔 스스로 답함.
</details>

### 조각 7 · VS 스레드 창: 이름 보기, 얼리기

코드를 새로 치지 않는다. `Chunk7Practice()`(조각 3과 같고 60초 동안 살아 있음)를 **F5**로 실행한다. 천천히 보려면 `Thread.Sleep(60000)`을 300000으로 늘린다(디버거로 멈춰 있는 동안에도 시간이 흐른다).

| 단계 | 할 일 | 볼 것 |
|---|---|---|
| 1 | **디버그 > 모두 중단** (`Ctrl+Alt+Break`) | 전체 일시정지, 주 스레드는 `Thread.Sleep` 줄 |
| 2 | **디버그 > 창 > 스레드** (`Ctrl+Alt+H`) | 영희·철수·민수가 **이름으로** 보임, 이름 없는 건 `<이름 없음>` |
| 3 | 영희 줄 더블클릭 | 코드 창이 영희가 서 있는 `WaitOne` 줄로, 호출 스택도 영희 기준 |
| 4 | 철수 우클릭 > **고정**(Freeze) → F5 | 철수만 계란이 멈춤 |
| 5 | 모두 중단 → 철수 **고정 해제**(Thaw) → F5 | 철수 다시 삶음 |
| (선택) | 철수를 얼린 채 끝까지 | `철수: 2초 안에 안 끝남!` 예상 (확인 안 함) |

**실습 B (MiniSight):** 준비 → 시작 → 정지 → 모두 중단 → 스레드 창. `MiniSight health`는 이름표 덕분에 한 줄로 바로 찾힌다. 이름 없는 스레드를 더블클릭하고 호출 스택을 열었더니 **아무것도 표시되지 않았다**(사용자 관찰).

> **Q. 이름 없는 스레드는 호출 스택이 비어 있는데?**
> VS는 기본으로 관리 코드(.NET)만 디버깅하고, 호출 스택도 C# 메서드만 보여 준다. **빈칸 = 그 스레드가 지금 C# 코드를 하나도 실행하고 있지 않다**는 뜻이다. .NET Framework 스레드풀 알바는 일이 없을 때 런타임 내부(네이티브)에서 대기하고, OpenCvSharp 같은 네이티브 라이브러리 스레드도 C# 프레임이 없다. 그래서 정지 뒤 남은 스레드는 **일하는 중이 아니라 대기 중**이라는 조각 6 해석과 맞는다. 누수라면 정지 뒤에도 C# 코드(타이머 콜백, 루프)에 서 있는 스레드가 보였을 것이다. 끝까지 보려면 MiniSight 속성 > 디버그 > **네이티브 코드 디버깅 사용**(이번엔 안 함).
>
> 현장 판단법: "응답 없음" 신고 → VS 붙이기(디버그 > 프로세스에 연결) → 모두 중단 → 스레드 창 → **C# 호출 스택이 보이는 스레드**, 특히 주 스레드 위치. 주 스레드가 메시지 루프가 아닌 `Join`, `.Result`, `lock`, `Sleep`에 서 있으면 그게 UI 멈춤의 범인이다.

실습 A 결과(영희 위치 열 글자, 철수만 멈췄는지)는 사용자 보고 없이 실습 B로 넘어갔다.

### Kata ↔ MiniSight 연결

| Kata 조각 | MiniSight / 회사 코드 |
|---|---|
| 1 `IsBackground = true`, `Name` | `HealthMonitor` 생성자, 회사 `HardwareMonitorService` |
| 2 `while (!_stop.WaitOne(간격))` | `HealthMonitor.Run`, 회사 `HardwareMonitorService` 루프 |
| 3 `Set → Join(시간제한) → Dispose` | `HealthMonitor.Dispose` |
| 4·5 스레드풀 / LongRunning | Fake 카메라 `Timer` 콜백(스레드풀), 회사 `InspectionOutputDispatcher`(LongRunning) |
| 7 스레드 창 | `MiniSight health` 이름으로 찾기 |

## 3. OmniSight 지도

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 감시 전용 스레드 | `OmniSight/Application/HardwareMonitorService.cs:64-83` | `IsBackground = true`, `Name`, `BelowNormal` |
| 깨울 수 있는 잠 | `OmniSight/Application/HardwareMonitorService.cs:92,122` | `_stop.WaitOne(0)` 확인, `_stop.WaitOne(1000)` 대기 |
| 최신값 게시 | `OmniSight/Application/HardwareMonitorService.cs:73,116` | `Volatile.Read/Write`로 불변 스냅샷 교체 (Day09) |
| 정리 전용 스레드 | `OmniSight/Application/SystemRetentionService.cs:20-34` | `CancellationTokenSource` + Thread |
| 토큰으로 깨우기 | `OmniSight/Application/SystemRetentionService.cs:41,62-68` | `_stop.Token.WaitHandle.WaitOne(...)`, 설정이 바뀌면 일찍 깸 |
| LongRunning 검사 작업자 | `OmniSight/Application/Inspection/InspectionPipeline.cs:93-96` | `StartNew(..., LongRunning).Unwrap()` + 주석 |
| LongRunning 출력 작업자 | `OmniSight/Application/Inspection/InspectionOutputDispatcher.cs:83-86` | 동기 루프라 전용 스레드가 끝까지 쓰임 |
| 부팅 비동기 | `OmniSight/Application/ApplicationBootstrapper.cs:45-54` | `Task.Run` (잠깐 일 → 스레드풀) |

## 4. 왜 이렇게 썼나

### 4.1 일꾼 종류 비교

| 방법 | 누가 실행 | 언제 쓰나 | OmniSight 예 |
|---|---|---|---|
| `new Thread(Run)` | 새 OS 스레드 | 프로그램 내내 도는 루프, 이름·우선순위 지정 필요 | HardwareMonitorService, SystemRetentionService |
| `ThreadPool.QueueUserWorkItem` | 스레드풀 | 짧은 작업 (직접 쓸 일은 거의 없음) | 사용 안 함 |
| `Task.Run(...)` | 스레드풀 | 짧은 CPU 작업, UI에서 무거운 일 떼어내기, await 가능 | 부팅, 룰/AI 검사 1회 |
| `Task.Factory.StartNew(..., LongRunning)` | (보통) 새 전용 스레드 | 오래 도는 루프인데 Task로 관리(대기/예외)하고 싶을 때 | 검사 파이프라인, 출력 작업자, 로그 작성기 |
| `System.Threading.Timer` | 스레드풀 | 주기 작업. 콜백이 겹칠 수 있음 | 10분 헬스 로그 (Day19), MiniSight Fake 카메라 |

### 4.2 결정과 이유

| 결정 | 이유 |
|---|---|
| 감시·정리는 `new Thread` | 이름과 우선순위(`BelowNormal`)를 줘서 **검사보다 덜 중요**하다고 OS에 알림. 디버거 스레드 창에서 이름으로 찾기 쉬움 |
| `IsBackground = true` | 메인 폼이 닫혀도 이 스레드 때문에 프로세스가 안 죽는 일을 막음. 대신 **정리는 Dispose에서 명시적으로** |
| `WaitOne(1000)` | Dispose하면 1초를 다 안 기다리고 즉시 깸 → 종료가 빠름 |
| 설정 교체 감지(`ReferenceEquals`) | 15분 대기 중에도 사용자가 경로를 바꾸면 바로 반영 |
| 검사 루프는 `LongRunning` | 무한 루프가 스레드풀 스레드를 잡으면 `Task.Run`으로 띄운 다른 짧은 작업이 줄 서서 늦어짐 |

### 4.3 LongRunning + async 람다 함정 (확장, 면접 고급 질문, 이번 회차 미진행)

`InspectionPipeline`은 `StartNew(() => RunAsync(token), LongRunning).Unwrap()`이다. `RunAsync`는 `async` 메서드라 **첫 번째 `await`에서 전용 스레드를 반납**한다. 그 뒤의 코드는 `ConfigureAwait(false)` 때문에 **스레드풀**에서 이어서 실행된다. 즉 전용 스레드는 시작 몇 줄만 쓰고 끝난다. 효과는 `Task.Run(() => RunAsync(token))`과 거의 같다. 동기 루프(`InspectionOutputDispatcher.ReportLoop`)에서만 LongRunning이 의미가 있다.

→ 면접에서 "LongRunning에 async 람다를 넣으면?"이라고 물으면 이걸 말한다.

## 5. MiniSight 적용

### 5-1. 완료 (사용자 작성, 2026-11-10)

- Day07 밀린 것: `Bootstrapper.BuildSystem`, Form1 준비/시작/정지(button16~18), `OnSystemStateChanged`, `OnFormClosed`(`-=` 후 `_system?.Dispose()`) 작성 확인.
- `App/HealthSnapshot.cs`, `App/HealthMonitor.cs` 작성, Form1 생성자에서 생성·건강 버튼·`OnFormClosed`에서 `_health?.Dispose()`. 사용자 실행으로 스레드 수 12 → 16 → 16 확인.

### 5-2. 남은 것

- 시작/정지 반복 후 스레드 수가 고정인지(누수 확인) — Day13 메모리 누수 실험 때.
- 건강 값을 UI 타이머로 1초마다 화면에 표시 — 필요 시 Day12(UI 마샬링) 때.

## 6. 확인한 범위와 다음 단원으로 넘긴 범위

- 확인: Kata 조각 0~5 출력, 조각 3 Set/Join 순서 실험, `HealthMonitor` 콘솔 데모(직접 컴파일·실행). MiniSight 스레드 수는 사용자 실행 보고.
- 미확인: Thread 안 예외로 프로세스가 죽는 실행(표의 설명만), 조각 7 실습 A 결과 보고, 이름 없는 스레드의 정확한 정체(네이티브 디버깅), 4.3 LongRunning + async 실습.
- 넘김: `Volatile`·불변 게시 원리 → Day09, 크기 제한 큐 → Day09~11, Task 예외·`await` → Day12.

## 7. VS2022 디버깅 실습

조각 7 표 참고. 추가로 해 볼 것:

| 할 일 | 관찰 |
|---|---|
| 스레드 창 깃발 아이콘으로 **Flag** → "플래그 지정된 스레드만 표시" | 관심 스레드만 보기 |
| `Run` 안 중단점 → 우클릭 > 조건: `Thread.CurrentThread.Name == "민수"` | 특정 스레드에서만 멈춤 |
| 디버그 > 창 > **병렬 스택** | 스레드별 호출 스택을 그림으로. Day14 데드락 찾기에서 주력 |

## 8. 셀프 체크

1. `Task.Run`과 `new Thread`의 차이 3가지.
<details><summary>답</summary>① Task.Run은 스레드풀 스레드를 빌리고, new Thread는 새 OS 스레드를 만든다. ② Task는 결과·예외·완료를 await/Wait로 관찰할 수 있고, Thread는 Join만 가능하며 예외 시 프로세스가 죽는다. ③ Thread는 이름·우선순위·IsBackground·아파트 모델(STA)을 지정할 수 있다.</details>

2. 스레드풀 고갈이란? 증상은?
<details><summary>답</summary>스레드풀 스레드들이 오래 걸리는(블로킹) 작업에 묶여 새 작업이 대기하는 상태. 스레드풀은 최소 개수 이후 천천히 스레드를 늘리므로 짧은 작업, 타이머 콜백, await 이후 코드가 수백 ms~초 단위로 지연된다(조각 5: 빈 일이 3000ms 대기). 고칠 대상은 느려진 쪽이 아니라 붙잡은 쪽이다.</details>

3. `IsBackground = false` 스레드가 살아 있으면?
<details><summary>답</summary>모든 포그라운드 스레드가 끝날 때까지 프로세스가 종료되지 않는다. 메인 폼을 닫아도 프로세스가 남아 다음 실행 시 Mutex 때문에 "이미 실행 중"이 뜬다.</details>

4. `Thread.Sleep(1000)` 루프 대신 `WaitOne(1000)`을 쓰는 이유는?
<details><summary>답</summary>종료 신호가 오면 즉시 깨어나므로 Dispose가 빠르다. Sleep은 신호를 받을 수 없어 최대 주기만큼 종료가 늦어지고, 주기가 길면 Join 타임아웃이 난다(조각 2: 1503ms vs 0ms, 조각 3 실험: 민수 시간 초과).</details>

5. `LongRunning`에 async 람다를 넘기면 어떻게 되나?
<details><summary>답</summary>전용 스레드는 첫 await까지만 쓰이고 반납된다. 이후는 스레드풀에서 실행된다. 또 StartNew는 Task<Task>를 반환하므로 Unwrap()을 하지 않으면 바깥 Task가 첫 await에서 바로 완료된 것으로 보인다.</details>
