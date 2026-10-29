# Day 01 (월, 2h) · 전체 지도, 단일 실행, 전역 예외

## 1. 오늘 목표

- **완료 기준**
  - OmniSight가 켜져서 꺼질 때까지 큰 흐름 4단계를 말로 설명할 수 있다.
  - MiniSight 솔루션을 만들고, **두 번 실행하면 두 번째는 "이미 실행 중"으로 막히는** Program.cs를 손으로 작성했다.
  - VS2022에서 F5 / F9 / F10 / F11 / Shift+F11 / Call Stack 창을 써 봤다.
- **최소분량**: 2, 3, 6, 7번
- **확장분량**: 5번 Kata의 AbandonedMutex 실험, 8번 셀프 체크

## 2. 🧒 초1 비유

- **프로그램 = 놀이공원**. 아침에 문 열기(부팅) → 놀이기구 운전(검사) → 저녁에 문 닫기(종료).
- **Mutex = 놀이공원 정문 열쇠 하나**. 열쇠는 한 개뿐이라 두 번째 사람이 와도 문을 못 연다. 그래서 프로그램이 두 개 동시에 뜨지 않는다. (카메라는 한 프로그램만 잡을 수 있어서 두 개 뜨면 큰일 난다.)
- **전역 예외 처리기 = 놀이공원 안전요원 3명**. 정문(UI 스레드), 뒷마당(백그라운드 스레드), 택배 창고(Task)를 각각 지킨다. 누가 넘어지면(예외) 기록하고 안전하게 문을 닫는다.

## 3. OmniSight 지도

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 큰 흐름 문서 | `OmniSight/docs/reference/OMNISIGHT_ARCHITECTURE.ko.md` §2, §3 | 시작부터 종료까지 그림 |
| Main 전체 | `OmniSight/Program.cs:23-106` | PHASE 1 부팅 → PHASE 2 Application.Run → PHASE 3 종료 |
| Mutex 생성 | `OmniSight/Program.cs:33-41` | `Global\` 이름, World SID 권한 |
| 단일 실행 확인 | `OmniSight/Program.cs:112-130` | `WaitOne(TimeSpan.Zero)`, `AbandonedMutexException` |
| 예외 처리기 3종 | `OmniSight/Program.cs:135-161` | ThreadException / AppDomain.UnhandledException / UnobservedTaskException |
| 강제 종료 | `OmniSight/Program.cs:167-178` | 메시지박스 대신 로그 + `Environment.Exit(1)` |
| finally 정리 | `OmniSight/Program.cs:98-104` | `ReleaseMutex`는 마지막 |

**집에서 Claude에게:**
```text
OmniSight/Program.cs 23~178줄을 보여주고, PHASE 1/2/3을 놀이공원 비유로 설명해줘.
그리고 using 블록이 몇 겹인지, 각 using이 끝날 때 무엇이 Dispose되는지 순서대로 알려줘.
```

## 4. 왜 이렇게 썼나

| 결정 | 이유 | 다른 방법 | 트레이드오프 |
|---|---|---|---|
| `Global\` 이름 Mutex | 원격 데스크톱 등 **다른 세션**에서 실행해도 막는다. 카메라/소켓 중복 점유 방지 | `Local\` Mutex, 프로세스 이름 검사 | Global은 권한 문제가 생길 수 있어 World SID 권한을 직접 줌 |
| `WaitOne(TimeSpan.Zero)` | 기다리지 않고 바로 판단 → 시작이 멈추지 않음 | `WaitOne()` 무한 대기 | 0초면 바로 실패, 사용자에게 메시지 |
| `AbandonedMutexException`을 성공으로 | 이전 프로그램이 작업관리자로 죽으면 열쇠가 "버려진" 상태. 넘겨받아 실행 | 실패로 처리 | 버려진 이유(크래시)를 로그로 남겨야 함 |
| 예외 시 `Environment.Exit(1)` | 장비 PC에서 메시지박스가 떠 있으면 아무도 안 누른다 → **좀비 상태**. 로그 남기고 확실히 종료해 감시 프로그램이 재시작하게 | 메시지박스 + 계속 실행 | 작업 중 데이터 손실 가능 → 로그 flush가 먼저 |
| `UnobservedTaskException`은 `SetObserved()` | .NET 4.5 이상은 기본적으로 프로세스를 죽이지 않지만, 설정에 따라 죽을 수 있어 방어 | 무시 | 놓친 Task 오류를 반드시 로그로 남김 |

**지금 코드의 약점(시니어 시각)**: `UnobservedTaskException`은 GC가 Task를 회수할 때 늦게 발생한다. 즉 "언제 터졌는지"가 실제 시점과 다르다. 그래서 Task는 **항상 await하거나 결과를 관찰**하는 게 원칙이다 (Day12).

## 5. Kata: Mutex로 단일 실행 + 예외 처리기 (콘솔)

`MiniSight.Katas` 콘솔 프로젝트(.NET Framework 4.7.2)를 만들고 `KataDay01.cs`를 쓴다.

```csharp
// KATA: Day01
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Katas.Day01
{
    public static class KataDay01
    {
        public static void Run()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                Console.WriteLine("[AppDomain] " + e.ExceptionObject);
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Console.WriteLine("[Task] " + e.Exception.InnerException?.Message);
                e.SetObserved();
            };

            bool createdNew;
            using (var mutex = new Mutex(false, @"Local\MiniSight_Kata01", out createdNew))
            {
                bool acquired;
                try
                {
                    acquired = mutex.WaitOne(TimeSpan.Zero, false);
                }
                catch (AbandonedMutexException)
                {
                    Console.WriteLine("이전 프로세스가 비정상 종료됨. 열쇠를 넘겨받음.");
                    acquired = true;
                }

                if (!acquired)
                {
                    Console.WriteLine("이미 실행 중입니다.");
                    return;
                }

                try
                {
                    Console.WriteLine("실행 중... createdNew=" + createdNew + " (Enter로 종료)");

                    // 관찰되지 않은 Task 예외 실험
                    Task.Run(() => { throw new InvalidOperationException("아무도 안 본 Task 오류"); });
                    Thread.Sleep(200);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    Console.ReadLine();
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }
    }
}
```

실험:
1. 빌드 후 `bin\Debug\MiniSight.Katas.exe`를 **두 번** 더블클릭 → 두 번째는 "이미 실행 중".
2. (확장) 첫 번째를 작업관리자에서 강제 종료하지 말고, `ReleaseMutex` 줄을 주석 처리한 뒤 프로세스 하나는 Mutex를 잡은 스레드가 끝나게 만들어 `AbandonedMutexException`을 재현해 본다. 어려우면 Claude에게 "Abandoned 재현 코드 힌트 줘"라고 한다.

## 6. MiniSight 오늘 단계

1. VS2022 → 새 프로젝트 → **Windows Forms 앱(.NET Framework)** → 이름 `MiniSight`, 솔루션 `MiniSight`, 프레임워크 4.7.2.
2. 구성 관리자 → 플랫폼 `x64` 추가. csproj에 `<LangVersion>7.3</LangVersion>`.
3. 같은 솔루션에 콘솔 앱(.NET Framework) `MiniSight.Katas` 추가 → Kata 위 코드.
4. `MiniSight/Program.cs` 작성 (구조만 OmniSight와 같게, 코드는 내 손으로):

```csharp
[STAThread]
private static void Main()
{
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    RegisterUnhandledExceptionHandlers();          // 안전요원 3명

    using (var mutex = new Mutex(false, @"Global\MiniSight_V1"))
    {
        if (!TryAcquire(mutex)) { MessageBox.Show("MiniSight가 이미 실행 중입니다."); return; }
        try
        {
            // PHASE 1: 부팅 (Day05에서 Bootstrapper로 교체)
            // PHASE 2: Application.Run(new MainForm());
            // PHASE 3: 종료
            Application.Run(new Form1());
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }
}
```

`RegisterUnhandledExceptionHandlers`와 `TryAcquire`는 3번 표를 보고 직접 채운다. `Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException)`을 **이벤트 등록 전에** 호출하는 것도 잊지 않는다.

## 7. VS2022 디버깅 실습

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | `Main` 첫 줄에 **F9**(중단점) → **F5** | 노란 화살표가 멈춘 줄 |
| 2 | **F10**(프로시저 단위 실행)으로 한 줄씩 | `using` 줄을 지날 때 Mutex 생성 |
| 3 | `TryAcquire(mutex)` 줄에서 **F11**(한 단계씩 코드 실행) | 메서드 안으로 들어감 |
| 4 | **Shift+F11**(프로시저 나가기) | 호출한 곳으로 복귀 |
| 5 | 디버그 → 창 → **호출 스택(Ctrl+Alt+C)** | Main → TryAcquire 쌓인 모습 |
| 6 | Form1의 버튼 클릭 이벤트에 `throw new Exception("test");` 넣고 실행 | 디버거가 먼저 잡는다(첫 번째 예외). F5로 계속하면 ThreadException 처리기로 감 |
| 7 | 같은 실험을 **디버거 없이(Ctrl+F5)** | 처리기만 동작. 디버거 유무로 동작이 다르다는 걸 기억 |

## 8. 셀프 체크

1. Mutex 이름에 `Global\`을 붙이면 무엇이 달라지나?
<details><summary>답</summary>Windows 세션 전체(다른 로그인 사용자, 원격 데스크톱 세션 포함)에서 공유된다. `Local\`은 현재 세션 안에서만 유일하다. 장비 PC에서 다른 계정으로 두 번 띄우는 것까지 막으려면 Global이 필요하다.</details>

2. `AbandonedMutexException`은 언제 발생하고 왜 성공으로 처리했나?
<details><summary>답</summary>Mutex를 가진 스레드/프로세스가 ReleaseMutex 없이 끝났을 때 다음 대기자가 받는다. 이 예외를 받은 쪽은 Mutex를 실제로 획득한 상태다. 이전 프로세스가 크래시한 상황이므로 새 프로그램은 실행되어야 하고, 대신 로그로 남긴다.</details>

3. 전역 예외 처리기 3개는 각각 어느 스레드의 예외를 잡나?
<details><summary>답</summary>`Application.ThreadException`: WinForms UI 스레드의 메시지 루프 안 예외. `AppDomain.UnhandledException`: 그 외 모든 스레드(Thread, 스레드풀 콜백). 이 이벤트 후 프로세스는 종료된다. `TaskScheduler.UnobservedTaskException`: 결과를 아무도 관찰하지 않은 Task의 예외가 GC될 때.</details>

4. 장비 프로그램에서 예외 시 메시지박스를 띄우고 기다리면 왜 위험한가?
<details><summary>답</summary>현장 PC 앞에 사람이 없다. 메시지박스가 UI 스레드를 잡고 있는 동안 검사·통신이 멈추거나 반쯤 살아 있는 좀비가 되어 PLC는 정상으로 착각할 수 있다. 로그를 남기고 종료해야 감시/재시작이 가능하다.</details>

5. `finally`에서 `ReleaseMutex`를 가장 마지막에 하는 이유는?
<details><summary>답</summary>폰트·로그 등 자원 정리가 끝나기 전에 Mutex를 놓으면 새 인스턴스가 떠서 같은 파일/장치를 동시에 잡을 수 있다. 정리가 완전히 끝난 뒤 열쇠를 반납한다.</details>

## 9. 블로그 포인트

- 제목 예: `[C#] 장비 프로그램은 왜 두 번 실행되면 안 될까: Global Mutex와 AbandonedMutexException`
- 쓸 내용: 놀이공원 열쇠 비유, Kata 코드, 디버거 유무에 따라 예외 처리 흐름이 달라 보였던 경험.
- ❌ 회사 Mutex 이름, 회사 로그 문구 금지.
