# 특강 1일차 (토, 8h) · 일꾼: 컴퓨터 안에는 일꾼이 여러 명 있다

> **2026-11-11, 1~8교시 완료.** PipelineLab에서 시작, 사과 표시, 정지 시 화면 비우기를 확인했다. MSTest 3개 통과와 셀프 체크 5문제를 확인했다. 확인 범위는 사용자 실습 결과와 저장 소스다.
> 전체 계획: [README.md](README.md) · 소개문: [P00-소개.md](P00-소개.md)

## 1. 오늘 목표

- **완료 기준**
  - 스레드를 만들고(`new Thread`, `Start`), 기다리고(`Join`), 멈추는(`ManualResetEvent`) 코드를 손으로 쳤다.
  - 화면은 UI 스레드만 그린다는 걸 예외로 직접 보고, `BeginInvoke`로 고쳤다.
  - 작품 PipelineLab 뼈대에서 가짜 사진기 일꾼이 만든 사과 사진이 화면에 나온다.
  - MSTest 첫 테스트 3개가 녹색이다.

## 2. 🧒 초1 비유

- **두뇌(코어)** = 생각하는 자리. **일꾼(스레드)** = 그 자리에 앉아 일하는 사람.
- **공장 건물(프로세스)** 안에는 내가 안 불러도 .NET이 데려온 도우미 일꾼(청소 담당 GC·종료자 등)이 처음부터 있다.

## 3. 조각별 실습

포트폴리오 `Katas` 콘솔 프로젝트의 `Katas/Concurrency/KataP01.cs`(namespace `Katas.Concurrency`). `Program.cs`는 `KataP01.Run();`만 부르고 다른 Kata는 주석으로 둔다. `Run()` 안에서 `Chunk1(); Chunk2(); ...`를 차례로 부른다.

### 1교시 조각 1 · 일꾼이 몇 명일까?

```csharp
using System;
using System.Diagnostics;
using System.Threading;

namespace Katas.Concurrency
{
    public static class KataP01
    {
        public static void Run()
        {
            Chunk1();
        }

        // 1교시 조각 1: 컴퓨터 안의 일꾼 세기
        public static void Chunk1()
        {
            Console.WriteLine("두뇌(코어) 수: " + Environment.ProcessorCount);
            Console.WriteLine("지금 내 번호: " + Thread.CurrentThread.ManagedThreadId);
            Console.WriteLine("이 프로그램의 일꾼 수: " + Process.GetCurrentProcess().Threads.Count);
        }
    }
}
```

| 코드 | 뜻 | 쓰는 법 |
|---|---|---|
| `Environment.ProcessorCount` | 두뇌 수 (논리 프로세서 수) | 읽기만 하면 숫자 |
| `Thread.CurrentThread` | 지금 이 줄을 실행하는 일꾼 자신 | `.ManagedThreadId`로 번호 |
| `ManagedThreadId` | 일꾼 번호표. `Main` 일꾼은 보통 1번 | 로그에 찍으면 "누가 했나"가 보인다 |
| `Process.GetCurrentProcess()` | 지금 이 프로그램 전체 (공장 건물) | `.Threads.Count` = 건물 안 일꾼 수 |

예상 출력 (Claude PC, 3회 실행 확인. 두뇌 수·일꾼 수는 PC마다 다르다):

```text
두뇌(코어) 수: 28
지금 내 번호: 1
이 프로그램의 일꾼 수: 6
```

- 실험: Ctrl+Shift+Esc → 성능 → CPU → "논리 프로세서" 숫자와 두뇌 수 비교.
- 질문: "내 PC의 두뇌 수는 __, 일꾼 수는 __. 일꾼을 안 만들었는데 1명보다 많은 이유는 __."

### 1교시 조각 2 · 일꾼 1명이 사과 3개를 씻으면?

`Run()`에 `Chunk2();` 추가, `Chunk1()` 아래에 붙인다.

```csharp
        // 1교시 조각 2: 일꾼 1명이 사과 3개를 차례로 씻기
        public static void Chunk2()
        {
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 1; i <= 3; i++)
            {
                Console.WriteLine("사과 " + i + " 씻는 중 (일꾼 " + Thread.CurrentThread.ManagedThreadId + "번)");
                Thread.Sleep(500);
            }
            sw.Stop();
            Console.WriteLine("다 씻음: " + sw.ElapsedMilliseconds + "ms");
        }
```

| 코드 | 뜻 | 쓰는 법 |
|---|---|---|
| `Stopwatch.StartNew()` | 초시계 만들고 바로 누르기 | `new Stopwatch(); sw.Start();`와 같다 |
| `sw.Stop()` | 초시계 멈추기 | 잴 일이 끝난 바로 뒤 |
| `sw.ElapsedMilliseconds` | 잰 시간(ms, `long`) | |
| `Thread.Sleep(500)` | 지금 이 일꾼이 0.5초 쉬기 (씻는 척) | ms 단위 |

예상 출력 (3회 실행 확인: 1502, 1502, 1503ms. 1500~1510 사이로 조금씩 다르다):

```text
사과 1 씻는 중 (일꾼 1번)
사과 2 씻는 중 (일꾼 1번)
사과 3 씻는 중 (일꾼 1번)
다 씻음: 1502ms
```

- 공백: 숫자와 `ms` 사이 공백 없음.
- 실험: `Sleep(500)` → `Sleep(1000)`으로 바꿔 보고 원래대로 돌린다 (약 3000ms 예상, 실행 확인 안 함).
- 질문: "일꾼이 1명이라 사과를 __ 씻는다. 그래서 걸린 시간은 대략 __ ms이다. 1000으로 바꾸면 대략 __ ms."
- 의도: 이 1500ms가 2교시(일꾼 3명)와 비교할 **기준점**이다.

조각 2 답변 ✅. `Sleep(1000)`을 세 번 실행하므로 대기 시간은 약 3000ms라고 설명했다.

### 1교시 조각 3 · 작업 관리자에서 스레드 수 보기

`KataP01.Chunk3()`에서 `Process.GetCurrentProcess()`로 PID와 `process.Threads.Count`를 출력한다. `Console.ReadLine()`으로 유지한 뒤 작업 관리자 → 세부 정보 → 열 선택에서 스레드를 켠다. 콘솔 PID와 같은 행을 비교한다.
사용자는 콘솔 7개와 작업 관리자 5개를 관찰했다. 같은 PID라면 서로 다른 시점의 관측값이다. 조각 1의 코어 수와 처음 출력한 스레드 수는 추가 기록 대기다.

### 2교시 조각 1 · 일꾼 시작과 종료 기다리기

`KataP02.Run()`에서 `new Thread(WashApple)`, `Name`, `Start()`, `Join()`을 사용했다. `Join()`을 주석 처리하니 사용자 실행에서 메인 다음 작업 문구가 민수의 완료 문구보다 먼저 나왔다 ✅.

### 2교시 조각 2 · 시작을 먼저 모아 호출하기

`KataP02.Chunk2()`에서 500ms씩 대기하는 세 작업자를 시작한 뒤 모두 `Join()`한다. 첫 `Join()`을 첫 `Start()` 직후로 옮기면 약 500ms 뒤에 나머지 둘이 시작한다. 사용자가 1000ms를 조금 넘는 시간과 그 이유를 확인했다 ✅.

### 2교시 조각 3 · 호출 스택에서 기다리는 위치 찾기

`WashApple()`의 출력 줄에 F9를 걸어 F5로 실행한다. 디버그 → 창 → 스레드에서 메인과 작업자를 전환해 호출 스택을 비교한다. 사용자 관측은 민수가 `Console.WriteLine()`에, 메인이 `worker.Join()`에 멈춘 상태였다 ✅.

### 3교시 조각 1 · 포그라운드와 백그라운드 종료

`KataP03`에서 메인은 작업자를 시작하고 종료한다. `IsBackground`를 `true`로 바꾼 사용자 실행에서는 작업자의 완료 줄이 나오지 않았다. 메인이 기다린다고 설명한 부분 △는 포그라운드 작업자가 프로세스를 살려 둔다는 의미로 보충했다. 메인 자체의 대기는 `Join()`으로 한다.

### 3교시 조각 2 · 시작과 완료 순서 비교

`KataP03.Chunk2()`에서 민수와 영희를 차례로 시작한 뒤 둘을 `Join()`한다. 민수 700ms와 영희 200ms를 주고, 다음에는 민수만 100ms로 바꾼다. 변형에서 민수 완료 → 영희 완료 → 모두 완료를 사용자 출력으로 확인했다 ✅.

### 4교시 조각 1 · 정지 신호와 제한 시간

`KataP04.Chunk1()`은 `WaitOne(시간)`이 `false`인 동안 작업을 반복한다. 메인은 Enter를 받으면 `Set()` → `Join()` → 이벤트 해제 순서로 정리한다. `WaitOne(2000)`으로 바꾼 사용자 실행에서 Enter를 누르자 바로 종료했다 ✅.

### 4교시 조각 2 · 신호 유지와 Reset

`KataP04.Chunk2()`는 `WaitOne(0)`으로 현재 신호를 읽는다. `Set()` 이후에는 여러 번 읽어도 `true`가 유지된다. `Reset()`을 주석 처리한 사용자 실행에서 마지막 값 `True`를 확인했다 ✅.

### 4교시 조각 3 · 신호 객체 하나를 두 작업자에게 전달

`KataP04.Chunk3()`은 작업자 둘이 동일한 `ManualResetEvent`를 기다리게 한다. Enter 한 번에 `Set()`을 한 번 호출한다. 사용자 실행에서 영희와 민수의 정지 신호 수신이 두 줄 출력됐다 ✅.

### 5교시 조각 1~3 · PipelineLab 가짜 카메라

`PipelineLab/Form1.cs`에서 시작과 정지 버튼 상태를 연결한다. `CaptureLoop()`는 500ms마다 160×120 `Bitmap`에 원을 그린다. `Graphics`와 `Bitmap`은 각각 `using`으로 관리한다. `StopCamera()`에서 작업자를 종료하고 창 닫기에도 같은 정리를 적용한다.
사용자는 버튼 상태 변화와 카메라 종료 → 메인 정리 완료 순서를 확인했다 ✅. 파란 원의 중앙을 빨강과 비교하면 `False`라는 답도 확인했다 ✅.

### 6교시 조각 1~2 · UI 스레드에서 사진 표시

작업자에서 `picPreview.Handle`에 접근하자 `InvalidOperationException`이 발생했다. Visual Studio의 예외 발생 알림과 실습 `catch`가 같은 예외를 각각 출력했다.
`QueuePreview()`에서 `Clone()`으로 표시용 사진을 만들고 `Interlocked.Exchange()`로 최신 대기 사진을 보관한다. `BeginInvoke()`가 `ShowPendingPreview()`를 UI에 예약한다. 교체된 사진을 해제하고, 정지할 때는 `Join()` 후 대기 사진과 표시 사진을 정리한다.
사용자 실행에서 시작하면 사과가 보이고 정지하면 사라지는 것을 확인했다 ✅.

### 7교시 조각 1~2 · 멍 픽셀을 세는 테스트

`PipelineLab/AppleInspector.cs`의 `CountBruisePixels()`는 실습에서 검정 픽셀을 멍으로 센다. `PipelineLab.Tests/AppleInspectorTests.cs`는 0개, 1개, 두 행에 하나씩 총 2개인 입력을 검사한다.
두 번째 행의 검정을 빨강으로 바꿔 기대값 2와 실제값 1의 차이로 테스트가 실패하는 것을 확인했다. 복원 후 사용자 실행에서 3개 모두 통과했다 ✅.

## 4. 셀프 체크 (8교시)

1. `StopCamera()`에서 `Join()`을 먼저 호출하면 어떻게 되나요?
2. 포그라운드 작업자가 남아 있을 때 `Main()`이 끝나면 프로그램은 언제 종료되나요?
3. `BeginInvoke()`로 표시하기 전에 사진을 복사하는 이유는 무엇인가요?
4. 첫 행만 검사하는 오류를 어떤 MSTest가 잡나요?
5. 작업자가 동기식 `Invoke()`에 있고 UI가 `Join()`에 있으면 각각 무엇을 기다리나요?

<details><summary>답과 확인 결과</summary>

1. UI가 `Join()`에서 기다리고 작업자는 정지 신호 없이 루프를 계속 돈다. UI는 뒤에 있는 `Set()`을 실행할 수 없다. 사용자 답 ✅.
2. 마지막 포그라운드 작업자가 종료한 뒤 프로세스가 끝난다. 메인에서 `Join()`을 호출하면 작업자의 종료를 메인도 기다린다. 사용자 답 ✅.
3. UI 콜백이 실행되기 전에 작업자가 `using`을 벗어나 원본을 해제할 수 있다. 표시용 복사본은 교체나 정지 때 정리한다. 사용자 답 ✅.
4. `CountBruisePixels_BlackPixelOnEachRow_ReturnsTwo()`가 잡는다. 한 행만 읽으면 기대값 2와 실제값 1이 다르다. 사용자 답 ✅.
5. `Invoke()`에서는 작업자가 UI의 표시를 기다리고, `Join()`에서는 UI가 작업자의 종료를 기다린다. `BeginInvoke()`는 UI에 예약한 뒤 반환하므로 작업자가 정지 절차를 계속할 수 있다. 처음 답변 보류 △, 설명 후 사용자가 양쪽 대기 대상을 정확히 구분 ✅.

</details>
