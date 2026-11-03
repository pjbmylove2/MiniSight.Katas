# Day 01 (월, 2h) · 단일 실행(Mutex)과 전역 예외 그물 3개

> 2회차는 **코드 먼저**: 조각마다 "지금 만드는 것 한 줄 → 코드 → 실험 → 질문 1개". 회사 코드는 다 친 뒤에 "내가 친 것의 회사 버전"으로 짧게 본다.

## 1. 오늘 목표

- **완료 기준**
  - 두 번 실행하면 두 번째는 "이미 실행 중"으로 막히는 MiniSight `Program.cs`를 손으로 작성했다.
  - 예외가 **어느 스레드에서** 터졌느냐에 따라 받는 그물이 다르다는 걸 직접 실행해서 봤다 (UI / 일꾼 Thread / 버려진 Task).
  - VS2022에서 F5 / F9 / F10 / F11 / Shift+F11 / Call Stack 창을 써 봤다.
- **최소분량**: 3번 조각 1~8, 5번 디버깅 1~5
- **확장분량**: 5번 디버깅 6~7, 6번 셀프 체크

## 2. 🧒 초1 비유

- **Mutex = 놀이공원 정문 열쇠 하나**. 열쇠가 하나뿐이라 두 번째 사람은 문을 못 연다. 카메라는 한 프로그램만 잡을 수 있어서 두 개가 뜨면 큰일 난다.
- **전역 예외 그물 = 안전요원 3명**. 정문(UI 스레드), 뒷마당(일꾼 스레드), 택배 창고(Task)를 각각 지킨다. 누가 넘어지면(예외) **기록부터** 한다.

## 3. 조각별 실습

조각 1~3은 `Katas` 콘솔 프로젝트의 `Katas/Day01/KataDay01.cs`(namespace `Katas.Day01`)에서, 조각 4~8은 `MiniSight` WinForms 프로젝트에서 한다. `Program.cs`에서 `Day01.KataDay01.Run();`을 부른다.

### 조각 1 · 프로그램 두 번 못 켜게 하기

```csharp
using System;
using System.Threading;

namespace Katas.Day01
{
    public static class KataDay01
    {
        public static void Run()
        {
            using (Mutex key = new Mutex(false, @"Local\MiniSight_Kata01"))   // 이름 붙은 열쇠
            {
                bool gotKey = key.WaitOne(TimeSpan.Zero);                      // 0초만 잡아 봄
                if (!gotKey)
                {
                    Console.WriteLine("이미 실행 중입니다.");
                    return;
                }

                Console.WriteLine("열쇠 잡음. 실행 중... (Enter로 종료)");
                Console.ReadLine();
                key.ReleaseMutex();                                            // 열쇠 반납
            }
        }
    }
}
```

| 키워드 | 역할 | 비슷한 기초 |
|---|---|---|
| `Mutex` | **프로그램끼리** 쓰는 열쇠. 이름이 같으면 다른 프로그램도 같은 열쇠를 본다 | B13 `lock`은 한 프로그램 **안의** 일꾼끼리 |
| `new Mutex(false, 이름)` | 열쇠를 만들거나 찾는다. `false` = 아직 안 잡음 | |
| `key.WaitOne(TimeSpan.Zero)` | **0초만** 잡아 본다. 잡으면 `true`, 남이 들고 있으면 `false` | B14 `WaitOne()` |
| `key.ReleaseMutex()` | 열쇠 반납 | B13 `lock`의 `}` |
| `@"..."` | 문자열 안의 `\`를 그대로 쓴다 | `"Local\\..."`과 같음 |

**실험**: 빌드 → `bin\Debug\Katas.exe`를 더블클릭(창 ①) → ①을 둔 채 한 번 더(창 ②).

```text
창 ①: 열쇠 잡음. 실행 중... (Enter로 종료)
창 ②: 이미 실행 중입니다.
```

**질문**: ①에서 Enter로 끝낸 뒤 다시 실행하면?
<details><summary>답</summary>정상 실행된다. `ReleaseMutex()`로 열쇠를 반납했기 때문. X로 닫아도 다시 실행되는데, 프로세스가 죽으면 Windows가 열쇠를 회수하기 때문이다.</details>

### 조각 2 · 터질 때 마지막 그물 치기 (콘솔)

`Run()` 맨 위에 그물을 달고, `Console.ReadLine();` 다음 줄에 `throw new InvalidOperationException("일부러 터뜨림");`을 넣는다 (`ReleaseMutex` 줄은 닿지 않으니 주석).

```csharp
AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    Exception ex = (Exception)e.ExceptionObject;
    Console.WriteLine("[마지막 그물] 잡힘: " + ex.Message);
    Console.WriteLine("[마지막 그물] 곧 죽음? " + e.IsTerminating);
};
```

| 키워드 | 역할 | 비슷한 기초 |
|---|---|---|
| `AppDomain.CurrentDomain` | 지금 돌고 있는 프로그램 전체 | |
| `.UnhandledException +=` | "아무도 안 잡은 예외" 이벤트에 핸들러를 단다 | B04 `event +=` |
| `e.ExceptionObject` | 터진 예외. `object`라서 `(Exception)`으로 바꿔 쓴다 | B09 형변환 |
| `e.IsTerminating` | `true`면 핸들러가 끝난 뒤 **프로그램이 죽는다** | |

**실험**: Ctrl+F5 → Enter.

```text
[마지막 그물] 잡힘: 일부러 터뜨림
[마지막 그물] 곧 죽음? True

처리되지 않은 예외: System.InvalidOperationException: 일부러 터뜨림
```

**질문**: 잡았는데도 죽는다. 그럼 그물은 왜 다나?
<details><summary>답</summary>현장 PC 앞에는 사람이 없다. 죽기 직전에 **무슨 예외가 어디서 났는지 기록**을 남겨야 나중에 원인을 찾을 수 있다. 모든 예외를 미리 try/catch로 막을 수는 없다.</details>

### 조각 3 · 그물에 걸린 걸 파일로 남기기

`using System.IO;`를 추가하고, 그물 안에 한 줄 더 넣는다.

```csharp
File.AppendAllText("crash.log",
    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + ex + Environment.NewLine);
```

| 키워드 | 역할 | 비슷한 기초 |
|---|---|---|
| `File.AppendAllText` | 파일 **끝에 이어서** 쓴다. 없으면 만든다 | B10 파일 |
| `"crash.log"` | 이름만 쓰면 **exe 폴더**(`bin\Debug`)에 생긴다 | |
| `+ ex` | 예외를 글자로 바꾸면 **메시지 + 터진 위치(스택)** 가 들어간다 | `ex.Message`는 메시지만 |

**실험**: 두 번 실행하면 `crash.log`에 덩어리가 2개(이어 쓰기).

```text
2026-11-03 17:05:12.345 System.InvalidOperationException: 일부러 터뜨림
   위치: Katas.Day01.KataDay01.Run() 파일 ...\KataDay01.cs:줄 25
```

**질문**: 줄 번호가 왜 중요한가?
<details><summary>답</summary>`ex.Message`만 남기면 "일부러 터뜨림"만 남아서 어느 코드에서 죽었는지 모른다. 줄 번호는 exe 옆에 `.pdb` 파일이 있어야 찍힌다 → 현장 배포 때 pdb도 같이 넣는다 (S07).</details>

### 조각 4 · WinForms 기본 동작 보기 (그물 없음)

`MiniSight`를 시작 프로젝트로 설정 → `Form1`에 `button1` → 더블클릭해서 생긴 핸들러에:

```csharp
private void button1_Click(object sender, EventArgs e)
{
    throw new InvalidOperationException("버튼에서 터뜨림");
}
```

**실험**: Ctrl+F5 → 버튼 → **WinForms 기본 오류 창**([계속] / [끝내기]). [계속]을 누르면 프로그램이 **살아 있다** (콘솔과 다름).

**질문**: 검사 중 이 창이 떴는데 아무도 [계속]을 안 누르면?
<details><summary>답</summary>창이 UI 스레드를 붙잡아 화면 갱신·PLC 처리가 멈춘다. 그런데 프로세스는 살아 있어서 밖에서는 정상처럼 보인다 → **좀비 상태**. 그래서 잡아서 기록하고 확실히 끝내야 한다.</details>

### 조각 5 · UI 그물 달기

```csharp
using System;
using System.IO;
using System.Windows.Forms;

namespace MiniSight
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            RegisterUnhandledExceptionHandlers();          // ← 폼 만들기 전에!
            Application.Run(new Form1());
        }

        private static void RegisterUnhandledExceptionHandlers()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.ThreadException += (sender, args) =>
            {
                WriteCrashLog("[UI 예외] " + args.Exception);
                Environment.Exit(1);
            };
        }

        private static void WriteCrashLog(string text)
        {
            File.AppendAllText("crash.log",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + text + Environment.NewLine);
        }
    }
}
```

| 키워드 | 역할 |
|---|---|
| `SetUnhandledExceptionMode(CatchException)` | UI 예외를 기본 오류 창 말고 **내 그물로**. **폼을 만들기 전에** 불러야 한다 |
| `Application.ThreadException` | **UI 스레드**(버튼 클릭 등)의 예외 그물 |
| `Environment.Exit(1)` | 즉시 종료. `1` = 오류 종료 코드 (`0`은 정상) |

**실험**: Ctrl+F5 → 버튼 → 오류 창 없이 사라지고 `crash.log`에 `[UI 예외] ... button1_Click ... 줄 NN`.

**질문**: `RegisterUnhandledExceptionHandlers();`를 `Application.Run(...)` 아래로 옮기면?
<details><summary>답</summary>`Application.Run`은 폼이 닫힐 때까지 돌아오지 않아서, 그동안 버튼 예외는 기본 오류 창으로 간다. 폼을 닫고 그 줄에 도착하면 `InvalidOperationException: 스레드에서 컨트롤을 만든 이후에는 처리되지 않은 예외 모드를 설정할 수 없습니다.`까지 난다.</details>

### 조각 6 · 일꾼 스레드에서 터지면

`button2` 추가 (`using System.Threading;`):

```csharp
private void button2_Click(object sender, EventArgs e)
{
    Thread worker = new Thread(() =>
    {
        throw new InvalidOperationException("일꾼에서 터뜨림");
    });
    worker.Start();
}
```

1단계: 그대로 실행 → `crash.log`에 `[UI 예외]`가 **안 생긴다**.
2단계: `ThreadException` 아래에 두 번째 그물:

```csharp
AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
{
    Exception ex = (Exception)args.ExceptionObject;
    WriteCrashLog("[백그라운드 예외] " + ex);
    Environment.Exit(1);
};
```

**질문**: 1단계에서 왜 UI 그물이 못 잡았나?
<details><summary>답</summary>`Application.ThreadException`은 UI 스레드의 예외만 받는다. 일꾼 스레드의 예외는 `AppDomain` 그물로 간다. B14처럼 **코드를 실행한 스레드가 누구냐**로 갈린다.</details>

### 조각 7 · Task에서 터지고 아무도 안 보면

`button3` 추가 (`using System.Threading.Tasks;`):

```csharp
private void button3_Click(object sender, EventArgs e)
{
    StartLostTask();
    Thread.Sleep(200);               // Task가 터질 시간
    GC.Collect();                    // 버려진 Task를 청소부가 수거 (실험용)
    GC.WaitForPendingFinalizers();
    MessageBox.Show("버튼3 끝. 프로그램 살아 있음");
}

private void StartLostTask()
{
    Task.Run(() =>
    {
        throw new InvalidOperationException("Task에서 터뜨림");
    });                              // 결과를 아무도 안 봄
}
```

1단계: 메시지 창은 뜨고 `crash.log`는 **그대로** → 예외가 조용히 사라진다.
2단계: 세 번째 그물:

```csharp
TaskScheduler.UnobservedTaskException += (sender, args) =>
{
    WriteCrashLog("[Task 예외] " + args.Exception);
    args.SetObserved();
};
```

| 키워드 | 역할 |
|---|---|
| `TaskScheduler.UnobservedTaskException` | **결과를 아무도 안 본 Task**의 예외. Task가 GC될 때 불린다 (늦게 온다) |
| `args.SetObserved()` | "봤다" 표시. 프로그램을 죽이지 않는다 |
| `StartLostTask()`로 뺀 이유 | 같은 메서드에 두면 디버그 빌드가 Task를 끝까지 붙잡아서 청소가 안 된다 |

**질문**: 조용히 사라지는 것과 죽는 것 중 무엇이 더 무섭나?
<details><summary>답</summary>조용히 사라지는 쪽. 어디서 무슨 문제가 났는지 찾을 수 없다 (예: 이미지 저장 Task가 실패했는데 며칠 뒤에야 "사진이 왜 없지?"). 죽으면 적어도 바로 눈에 띈다.</details>

| 터진 곳 | 받는 그물 | 그물이 하는 일 |
|---|---|---|
| UI 스레드 | `Application.ThreadException` | 기록 → 종료 |
| `new Thread` 일꾼 | `AppDomain.CurrentDomain.UnhandledException` | 기록 → 종료 |
| 버려진 Task | `TaskScheduler.UnobservedTaskException` | 기록 → **계속 실행** |

### 조각 8 · MiniSight에 중복 실행 막기 (조각 1 + 그물 합치기)

`using System.Threading;` 추가, `Main`을 바꾼다.

```csharp
[STAThread]
static void Main()
{
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    RegisterUnhandledExceptionHandlers();

    using (Mutex mutex = new Mutex(false, @"Global\MiniSight_SingleInstance"))
    {
        if (!TryAcquireSingleInstanceLock(mutex))
        {
            MessageBox.Show("이미 실행 중입니다.");
            return;
        }

        try
        {
            Application.Run(new Form1());
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }
}

private static bool TryAcquireSingleInstanceLock(Mutex mutex)
{
    try
    {
        return mutex.WaitOne(TimeSpan.Zero, false);
    }
    catch (AbandonedMutexException)
    {
        return true;     // 앞 주인이 반납 없이 죽음 → 열쇠는 내 손에 들어옴
    }
}
```

| 키워드 | 역할 | 조각 1과 다른 점 |
|---|---|---|
| `Global\` | PC 전체에서 하나 (다른 로그인 세션끼리도 막음) | 조각 1은 `Local\` |
| `try { Run } finally { ReleaseMutex }` | 폼이 닫히든 예외가 나든 열쇠 반납 | 조각 1은 그냥 다음 줄 |
| `catch (AbandonedMutexException)` | 앞 주인이 반납 없이 죽었을 때. 이때도 열쇠는 **내가 잡은 상태** → `true` | |
| `Try...` 이름 | 실패하면 예외 대신 `false`를 준다는 약속 | B05 `TryParse` |

> **Q. 앞에 아무것도 안 붙이면(`"MiniSight_SingleInstance"`)?** 동작은 하지만 **`Local\`과 같은 열쇠**다. 실험: 이름 없는 쪽이 잡은 상태에서 `Local\같은이름` → "이미 실행 중", `Global\같은이름` → "잡음"(다른 열쇠함). 그래서 의도를 드러내려고 `Global\`을 **명시**한다.

**실험**: `MiniSight.exe` 두 번 더블클릭 → 두 번째는 "이미 실행 중입니다." 첫 번째를 닫으면 다시 뜬다.

**질문**: `button1` → 그물이 `Environment.Exit(1)`. 그러면 `finally`의 `ReleaseMutex()`는 실행될까?

> 흔한 오답: "finally는 무조건 실행된다." → 실험으로 확인한다.

**실험**: `finally` 안 `mutex.ReleaseMutex();` 위에 `WriteCrashLog("[finally] 열쇠 반납");`을 넣고 Ctrl+F5 → `button1`.

```text
2026-11-03 ... [UI 예외] System.InvalidOperationException: 버튼에서 터뜨림 ...
```

`[finally]` 줄이 **없다**.

<details><summary>답</summary>`Environment.Exit`는 프로세스를 그 자리에서 끝낸다. 그물은 `Application.Run` **안**(메시지 루프)에서 불리므로 `Main`의 `finally`까지 돌아오지 못한다. "finally는 무조건"은 **예외·return으로 블록을 빠져나갈 때** 이야기다. 프로세스가 끝나면 열쇠는 Windows가 회수한다 (조각 1에서 X로 닫아도 다시 실행된 이유). 다른 프로세스가 같은 열쇠를 열어 두고 있었다면 그쪽은 `AbandonedMutexException`을 받는다 → 그래서 `TryAcquire`가 그 예외를 "잡은 걸로" 친다. 회사 `ShutdownSafely`가 `Exit` **전에** 로그부터 정리하는 이유도 이것이다 (`finally`가 안 돈다).</details>

### 조각 9 · AbandonedMutexException 직접 보기 (확장)

`KataDay01`에 메서드를 하나 더 만들고 `Program.cs`에서 `Day01.KataDay01.RunAbandoned();`를 부른다.

```csharp
public static void RunAbandoned()
{
    Mutex key = new Mutex(false, @"Local\MiniSight_Kata01b");
    try
    {
        if (key.WaitOne(TimeSpan.FromSeconds(30)))        // 최대 30초 기다림
        {
            Console.WriteLine("열쇠 잡음. 5초 뒤 반납 없이 죽음");
            Thread.Sleep(5000);
            Environment.Exit(1);                           // ReleaseMutex 없이 종료
        }
    }
    catch (AbandonedMutexException)
    {
        Console.WriteLine("AbandonedMutexException: 앞 주인이 반납 없이 죽음. 열쇠는 내 것");
        key.ReleaseMutex();
        Console.ReadLine();
    }
}
```

| 키워드 | 역할 |
|---|---|
| `WaitOne(TimeSpan.FromSeconds(30))` | 0초가 아니라 **30초까지 줄 서서** 기다린다 |
| `Environment.Exit(1)` | 반납 없이 죽는 장면을 일부러 만든다 |

**실험**: `Katas.exe`를 더블클릭하고 5초 안에 **한 번 더** 더블클릭.

```text
창 ①: 열쇠 잡음. 5초 뒤 반납 없이 죽음      (5초 뒤 사라짐)
창 ②: AbandonedMutexException: 앞 주인이 반납 없이 죽음. 열쇠는 내 것
```

①이 죽은 뒤 **혼자** 실행하면 예외 없이 "열쇠 잡음"이 나온다.

<details><summary>왜?</summary>열쇠를 돌려주는 건 **Windows**다 (프로세스가 끝나면 그 프로세스의 열쇠를 회수). 그때 그 열쇠를 **기다리던(열어 둔) 다른 프로세스가 있으면** 그쪽이 `AbandonedMutexException`으로 "앞 주인이 반납 안 하고 죽었다"는 소식을 받는다. 아무도 열어 두지 않았으면 열쇠 자체가 사라지고, 다음 실행은 새 열쇠를 만들어서 예외가 없다.</details>

## 4. 회사 코드와 비교 (다 친 뒤에)

| 내가 친 것 | OmniSight | 다른 점 |
|---|---|---|
| 조각 8 `Main` | `OmniSight/Program.cs:23-106` | PHASE 1 부팅 → 2 `Application.Run` → 3 종료. using이 여러 겹 |
| `new Mutex(false, @"Global\...")` | `Program.cs:33-41` | World SID 권한(`MutexSecurity`)을 직접 줌 → 다른 계정에서도 같은 열쇠를 열 수 있게 |
| `TryAcquireSingleInstanceLock` | `Program.cs:112-130` | 같은 구조. Abandoned일 때 로그를 남김 |
| 그물 3개 | `Program.cs:135-161` | 첫 줄 `SetUnhandledExceptionMode`까지 같음. 기록은 `LogManager` |
| 그물 안의 `Environment.Exit(1)` | `ShutdownSafely()` `Program.cs:167-178` | 로그를 먼저 정리(Dispose)한 뒤 `finally`에서 `Exit(1)` |
| `finally { ReleaseMutex }` | `Program.cs:98-104` | 다른 정리를 다 끝낸 뒤 **마지막에** 반납 |

| 결정 | 이유 | 트레이드오프 |
|---|---|---|
| `WaitOne(TimeSpan.Zero)` | 기다리지 않고 바로 판단 → 시작이 멈추지 않음 | 0초면 바로 실패 → 사용자에게 메시지 |
| Abandoned를 성공으로 | 이전 프로그램이 크래시한 상황이니 새 프로그램은 떠야 함 | 왜 버려졌는지 로그로 남겨야 함 |
| 예외 시 `Environment.Exit(1)` | 메시지박스 좀비 방지. 끝나야 감시 프로그램이 재시작 | 작업 중 데이터 손실 가능 → 로그 flush 먼저 |
| Task 그물은 `SetObserved()` | 놓친 Task 오류를 기록만 하고 계속 | GC 때 늦게 와서 "언제" 터졌는지 실제와 다름 → Task는 항상 await하거나 결과를 본다 (Day12) |

## 5. VS2022 디버깅 실습

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | `Main` 첫 줄에 **F9**(중단점) → **F5** | 노란 화살표가 멈춘 줄 |
| 2 | **F10**(프로시저 단위 실행)으로 한 줄씩 | `using` 줄을 지날 때 Mutex 생성 |
| 3 | `TryAcquireSingleInstanceLock(mutex)` 줄에서 **F11**(한 단계씩 코드 실행) | 메서드 안으로 들어감 |
| 4 | **Shift+F11**(프로시저 나가기) | 호출한 곳으로 복귀 |
| 5 | 디버그 → 창 → **호출 스택(Ctrl+Alt+C)** | Main → TryAcquire 쌓인 모습 |
| 6 | `button1`을 **F5**(디버거 붙임)로 실행해서 누르기 | 디버거가 `throw` 줄에서 먼저 멈춘다. F5로 계속하면 `crash.log`에 `[UI 예외]`만 남고 `[finally]` 줄은 없다 (2026-11-03 확인) |
| 7 | 같은 실험을 **Ctrl+F5**(디버거 없이) | 결과는 6번과 같다. 다른 점은 **디버거가 먼저 멈춰서 보여 주느냐**뿐 → 디버깅 중 멈춤 ≠ 프로그램이 죽음 |

## 6. 셀프 체크

1. Mutex 이름에 `Global\`을 붙이면 무엇이 달라지나?
<details><summary>답</summary>Windows는 **로그인 한 번 = 세션 하나**로 칸막이를 친다. 장비 PC 앞 `Operator` 로그인이 세션 1이면, 원격 데스크톱 `Admin` 접속은 세션 2다. `Local\`은 **세션마다 따로 있는 열쇠함**이라 세션 2에서 또 켜진다 → 카메라 충돌. `Global\`은 **PC 전체에 하나뿐인 열쇠함**이라 세션 2에서 막힌다. 회사 코드가 World SID 권한(`MutexSecurity`)을 따로 주는 것도 이 때문이다. `Admin`이 만든 Global 열쇠를 `Operator`가 권한 문제로 못 여는 일을 막는다.</details>

2. `AbandonedMutexException`은 언제 발생하고 왜 성공으로 처리했나?
<details><summary>답</summary>Mutex를 가진 스레드/프로세스가 `ReleaseMutex` 없이 끝났을 때 다음 대기자가 받는다. 받은 쪽은 Mutex를 실제로 획득한 상태다. 이전 프로세스가 크래시한 상황이므로 새 프로그램은 실행되어야 하고, 대신 로그로 남긴다.</details>

3. 전역 예외 그물 3개는 각각 어느 스레드의 예외를 잡나?
<details><summary>답</summary>`Application.ThreadException`: WinForms UI 스레드. `AppDomain.UnhandledException`: 그 외 모든 스레드(Thread, 스레드풀 콜백), 이후 프로세스 종료. `TaskScheduler.UnobservedTaskException`: 결과를 아무도 안 본 Task의 예외가 GC될 때.</details>

4. 장비 프로그램에서 예외 시 메시지박스를 띄우고 기다리면 왜 위험한가?
<details><summary>답</summary>현장 PC 앞에 사람이 없다. 메시지박스가 UI 스레드를 잡고 있는 동안 검사·통신이 멈추거나 반쯤 살아 있는 좀비가 되어 PLC는 정상으로 착각할 수 있다. 로그를 남기고 종료해야 감시/재시작이 가능하다.</details>

5. `finally`에서 `ReleaseMutex`를 가장 마지막에 하는 이유는?
<details><summary>답</summary>자원 정리가 끝나기 전에 Mutex를 놓으면 새 인스턴스가 떠서 같은 파일/장치를 동시에 잡을 수 있다. 정리가 완전히 끝난 뒤 열쇠를 반납한다.</details>

6. 그물 안에서 `Environment.Exit(0)`(정상 코드)으로 바꾸면 `Main`의 `finally`는 돌까?
<details><summary>답</summary>안 돈다. 흔한 오답은 "Exit(1)은 비정상이라 안 돌고 Exit(0)이면 돈다"이다. `0`/`1`은 밖에 알려 주는 **종료 번호**일 뿐이고, `Exit`는 숫자와 상관없이 그 자리에서 프로세스를 끝낸다. finally는 **블록을 빠져나갈 때**(정상 진행·return·예외) 도는데, Exit는 빠져나가는 게 아니라 프로그램 자체를 끝낸다.

```text
try { Console.WriteLine("try 안"); Environment.Exit(0); }
finally { Console.WriteLine("finally 돎"); }
→ 출력: try 안   (exit=0, "finally 돎" 없음)
```
</details>

## 7. 블로그 포인트

- 제목 예: `[C#] 장비 프로그램은 왜 두 번 실행되면 안 될까: Mutex와 전역 예외 그물 3개`
- 쓸 내용: 조각 4의 기본 오류 창(좀비), 조각 6·7에서 그물이 못 잡던 순간, 디버거 유무에 따라 흐름이 달라 보인 경험.
- ❌ 회사 Mutex 이름, 회사 로그 문구 금지.
