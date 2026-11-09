# Day 06 (토, 4h) · Facade + State Machine + Observer, 장치 묶음과 창구

## 1. 오늘 목표와 이번 회차 완료 범위

- UI가 창구(`VisionSystem`) 하나만 보고 운전하는 구조(Facade)를 설명한다.
- 반장(`DeviceManager`)이 장치를 순서대로 켜고, 거꾸로 끄고, 켜다 실패하면 켠 것을 거꾸로 되돌리는(롤백) 이유를 말한다.
- 끄다 실패해도 나머지를 끄고 오류를 모아 알리는 것(`AggregateException`), `Dispose`는 던지지 않고 한 번만 하는 것을 구분한다.
- 창구가 예외를 잡아 쪽지(튜플)로 돌려주고, 상태 변화를 안전하게 방송(`GetInvocationList`)하고, 동시 호출을 `lock`으로 막는 이유를 말한다.

2026-11-06 시작(1차, MiniSight 코드부터), 2026-11-09 "감이 안 온다"로 **처음부터 다시** 했다. 2차는 큰 그림 한 장 → 콘솔 Kata 조각 0~7(각 조각을 A 사고 → B 고침 순서로)로 진행했고 약 70% 기준으로 마무리했다. MiniSight 적용은 일부 남았다(6절).

## 2. 초1 비유 · 그림 한 장

```text
 Form1 (손님)          "시작해 주세요"  →  결과 쪽지만 받음
    ↓
 VisionSystem (창구)    지금 상태 기억, 잘못된 순서면 거절, 결과를 쪽지로 돌려줌, 상태 방송
    ↓
 DeviceManager (반장)   장치 여러 개를 순서대로 켜고, 거꾸로 끄고, 실패하면 되돌림
    ↓
 조명 / 카메라1 / 카메라2 (학생들)
```

| 누구 | 하는 일 | 모르는 것 |
|---|---|---|
| Form1 | 버튼 누르면 창구에 부탁 | 장치가 몇 개인지, 순서 |
| VisionSystem | 상태 기억, 쪽지로 결과 전달, 방송 | 장치 하나하나 켜는 법 |
| DeviceManager | 장치 목록을 순서대로 다룸 | 지금 자동 운전인지 수동인지 |

**한 사람이 한 가지씩만 안다.** 패턴 이름으로는 창구 = Facade, 상태 규칙 = State Machine, 방송 = Observer.

## 2-1. 조각별 실습

콘솔 앱(.NET Framework 4.7.2) `Day06Kata` 하나의 `Program.cs`를 조각마다 고쳐 가며 진행한다. 조각마다 **A(사고 내기) → B(고치기)** 순서로 두 번 실행해 차이를 눈으로 본다. 모든 예상 출력은 `csc -langversion:7.3` + net472 + x64로 컴파일·실행해서 확인했다.

### 조각 0 · 반장: 순서대로 켜고, 거꾸로 끄기

**지금 만드는 것:** 장치 3개를 반장 하나가 한꺼번에 켜고 끈다. 예외도 상태도 아직 없다.

**비유:** 사진관. 조명을 먼저 켜야 카메라가 밝은 사진을 찍는다. 끌 때는 카메라를 먼저 멈춘다. 조명을 먼저 끄면 카메라가 깜깜한 사진을 찍는다.

```csharp
using System;

class Device
{
    private readonly string _name;

    public Device(string name)
    {
        _name = name;
    }

    public void Start()
    {
        Console.WriteLine(_name + " 켜짐");
    }

    public void Stop()
    {
        Console.WriteLine(_name + " 꺼짐");
    }
}

class Manager
{
    private readonly Device[] _devices;

    public Manager(Device[] devices)
    {
        _devices = devices;
    }

    public void StartAll()
    {
        foreach (Device device in _devices)
        {
            device.Start();
        }
    }

    public void StopAll()
    {
        for (int i = _devices.Length - 1; i >= 0; i--)
        {
            _devices[i].Stop();
        }
    }
}

class Program
{
    static void Main()
    {
        Manager manager = new Manager(new Device[] { new Device("조명"), new Device("카메라1"), new Device("카메라2") });
        manager.StartAll();
        Console.WriteLine("---");
        manager.StopAll();
    }
}
```

| 코드 | 역할 | 같은 뜻의 긴 코드 |
|---|---|---|
| `new Device[] { a, b, c }` | 배열을 만들면서 채움 | `Device[] arr = new Device[3]; arr[0] = a; ...` |
| `foreach (Device device in _devices)` | 앞에서부터 하나씩 | `for (int i = 0; i < _devices.Length; i++)` |
| `_devices.Length - 1` | 마지막 칸 번호 (3개면 2) | — |
| `i--`, `i >= 0` | 뒤에서 앞으로, 0번까지 포함 | `i = i - 1` |

```text
조명 켜짐
카메라1 켜짐
카메라2 켜짐
---
카메라2 꺼짐
카메라1 꺼짐
조명 꺼짐
```

실험: 배열 맨 뒤에 `new Device("조명2")`를 넣으면 `StopAll`을 안 고쳐도 `조명2 꺼짐`이 맨 처음 나온다. 반장이 `Length`로 개수를 스스로 센다.

**질문:** 끌 때 `foreach`(앞에서부터)를 쓰면 사진관에서 무슨 일이 생기나?

<details><summary>답과 사용자 확인</summary>

조명이 먼저 꺼져서, 아직 켜져 있는 카메라가 깜깜한 사진(불량 영상)을 찍는다. 그래서 켠 순서의 거꾸로 끈다. 규칙: 나중에 켠 것이 먼저 켠 것에 기대고 있으니 나중에 켠 것부터 끈다(접시 쌓기, 중첩 `using`과 같은 규칙).

사용자 △: "조명 → 카메라1 → 카메라2 순서로 꺼진다"(순서는 정확). 그래서 무슨 일이 생기는지(깜깜한 사진)가 빠짐 → 보충.
</details>

### 조각 1 · 반장이 목록을 자기 배열로 베끼기 (`ToArray`)

**지금 만드는 것:** 받은 목록을 그대로 들고 있으면 생기는 사고를 먼저 내고, `ToArray()` 한 줄로 고친다.

**비유:** 출석부 원본을 같이 보면 선생님이 원본에서 지운 학생을 반장이 하교 때 안 챙긴다. 반장이 자기 공책에 베껴 두면 그대로 다 챙긴다.

A(사고): `Manager`가 `List<Device>`를 그대로 보관하고, `Main`이 반장을 만든 뒤 `list.RemoveAt(1)`로 카메라1을 뺀다.

```csharp
List<Device> list = new List<Device> { new Device("조명"), new Device("카메라1"), new Device("카메라2") };
Manager manager = new Manager(list);
manager.StartAll();

list.RemoveAt(1);   // 바깥에서 누가 목록의 1번(카메라1)을 뺌
Console.WriteLine("--- 바깥에서 카메라1을 목록에서 뺐다");

manager.StopAll();
```

```text
조명 켜짐
카메라1 켜짐
카메라2 켜짐
--- 바깥에서 카메라1을 목록에서 뺐다
카메라2 꺼짐
조명 꺼짐
```

B(고침): `Manager`의 4군데만 바꾸고 `Main`은 그대로 둔다.

```csharp
private readonly Device[] _devices;                  // ① List → 배열

public Manager(IEnumerable<Device> devices)          // ② List → IEnumerable
{
    _devices = devices.ToArray();                    // ③ 자기 공책에 베끼기 (using System.Linq;)
}

// StopAll의 _devices.Count → _devices.Length         // ④
```

```text
조명 켜짐
카메라1 켜짐
카메라2 켜짐
--- 바깥에서 카메라1을 목록에서 뺐다
카메라2 꺼짐
카메라1 꺼짐
조명 꺼짐
```

| 코드 | 역할 |
|---|---|
| `list.RemoveAt(1)` | 1번 칸(두 번째)을 빼고 뒤를 당김 |
| `.Count` / `.Length` | 개수. List는 `Count`, 배열은 `Length` |
| `IEnumerable<Device>` | 하나씩 꺼낼 수 있는 것이면 뭐든 받음 (List, 배열 모두) |
| `devices.ToArray()` | 내용을 새 배열에 복사 |

**질문:** A에서 `list.RemoveAt(1)`만 했는데 왜 반장의 `StopAll`에서 카메라1이 빠졌나?

<details><summary>답과 사용자 확인</summary>

`_devices = devices;`는 복사가 아니라 같은 목록을 가리키게 한다. `list`와 `manager._devices`가 하나뿐인 목록을 같이 보므로, 바깥에서 빼면 반장 명단에서도 빠지고 카메라1은 켜진 채 남는다. `readonly`는 변수가 다른 목록으로 바뀌는 것만 막고 목록 내용 변경은 못 막는다. `ToArray()`도 목록만 새로 만들고 안의 `Device` 객체는 같은 것을 가리킨다(반장이 끄면 진짜 카메라가 꺼짐).

사용자 ✅: "list와 manager가 같은 곳을 가리킨다. 그래서 RemoveAt(1)이 manager에도 똑같이 적용된다." → 같은 곳을 가리키는 건 `manager` 자체가 아니라 `manager._devices`라고 다듬음.

1차(2026-11-06) 기록: 처음에 "서로 다른 목록에서 삭제·에러" △ → 같은 공유 목록으로 정정, 재확인에서 개수 정답·이유 빠짐 → 핵심 단어(복사, 자기 배열, 바깥 변경과 무관) 보충.
</details>

### 조각 2 · 켜다가 실패하면 켠 것들을 거꾸로 끄기 (롤백)

**지금 만드는 것:** 카메라2가 켜지다 실패할 때, 이미 켜진 조명과 카메라1을 되돌린다.

**비유:** 소풍 버스 3대 중 3번 버스 시동이 안 걸리면, 출발한 버스를 마지막 것부터 차고로 되돌린 뒤 선생님께 보고한다.

`Device`에 고장 스위치를 넣는다.

```csharp
public bool FailOnStart { get; set; }       // true면 켜질 때 고장

public void Start()
{
    if (FailOnStart)
    {
        throw new InvalidOperationException(_name + " 켜기 실패");
    }
    Console.WriteLine(_name + " 켜짐");
}
```

`Main`은 `camera2.FailOnStart = true;`로 만든 뒤 `manager.StartAll()`을 `try/catch`로 부르고 `"Main이 받음: " + ex.Message`를 찍는다.

A(롤백 없음, 조각 0의 `StartAll`):

```text
조명 켜짐
카메라1 켜짐
Main이 받음: 카메라2 켜기 실패
```

B(롤백):

```csharp
public void StartAll()
{
    List<Device> attempted = new List<Device>();     // 시동 걸어 본 버스 명단
    try
    {
        foreach (Device device in _devices)
        {
            attempted.Add(device);                   // ① 먼저 명단에 적고
            device.Start();                          // ② 그다음 시동
        }
    }
    catch (Exception)
    {
        Console.WriteLine("--- 실패! 켠 것들을 거꾸로 끈다");
        for (int i = attempted.Count - 1; i >= 0; i--)
        {
            attempted[i].Stop();
        }
        throw;                                       // ③ 원래 예외를 그대로 다시 던짐
    }
}
```

```text
조명 켜짐
카메라1 켜짐
--- 실패! 켠 것들을 거꾸로 끈다
카메라2 꺼짐
카메라1 꺼짐
조명 꺼짐
Main이 받음: 카메라2 켜기 실패
```

| 코드 | 역할 | 같은 뜻의 긴 코드 |
|---|---|---|
| `public bool FailOnStart { get; set; }` | 자동 속성 | `private bool _fail;` + get/set |
| `catch (Exception)` | 변수 이름 없이 잡기 | `catch (Exception ex)` |
| `throw;` | 잡은 예외를 그대로 다시 던짐 | `throw ex;`는 처음 터진 위치(스택)를 지움 |

**질문:** 카메라2는 `켜짐`이 한 번도 안 찍혔는데 왜 `카메라2 꺼짐`이 나왔나? 그래서 무엇을 막나?

<details><summary>답과 사용자 확인</summary>

`attempted.Add`가 `Start`보다 먼저라서, `Start`가 터져도 카메라2가 이미 명단에 있다. 진짜 카메라는 `Start` 안에서 그래버를 연 뒤 트리거 루프에서 실패하는 식으로 **반쯤 열린 채** 실패할 수 있다. 그래서 실패한 장치도 `Stop`을 받아 반쯤 열린 자원이 남는 것을 막고, 다음 시작이 "이미 열려 있음"으로 충돌하지 않는다. 대가로 `Stop`은 켜진 적 없는 장치에도 안전해야 한다(MiniSight `DeviceBase.Stop`이 Created/Disposed면 넘어가는 이유). 셀프 체크 4번과 같은 내용.

사용자: 동작 설명 ✅("Add에 카메라2까지 들어가 있어서 예외와 상관없이 꺼짐"), "그래서 무엇을 막나" △ → 보충.
</details>

### 조각 3 · 끄다가 실패해도 나머지는 끄고, 오류는 모으기

**지금 만드는 것:** 끌 때 카메라1이 고장 나도 조명까지 끄고, 실패를 모아 한 번에 알린다.

**비유:** 하교 때 2번 스위치가 고장 나면 메모만 하고 남은 스위치를 다 끈 뒤 메모장을 통째로 드린다. 고장 나자마자 뛰어나가면 1번 불이 밤새 켜져 있다.

`Device`에 `FailOnStop` 속성과 `Stop` 안 `if (FailOnStop) { throw new InvalidOperationException(_name + " 끄기 실패"); }`를 추가한다. `Main`은 `camera1.FailOnStop = true;`, `StartAll()` 뒤 `StopAll()`을 부르고 `catch (AggregateException ex)`에서 `ex.Message`와 `ex.InnerExceptions`를 찍고, 그 아래 `catch (Exception ex)`에서 `"Main이 받음(하나만): "`을 찍는다.

A(조각 0의 `StopAll`):

```text
조명 켜짐
카메라1 켜짐
카메라2 켜짐
---
카메라2 꺼짐
Main이 받음(하나만): 카메라1 끄기 실패
```

B:

```csharp
public void StopAll()
{
    List<Exception> errors = new List<Exception>();           // 메모장
    for (int i = _devices.Length - 1; i >= 0; i--)
    {
        try
        {
            _devices[i].Stop();
        }
        catch (Exception stopError)
        {
            Console.WriteLine("  (실패 기록하고 계속): " + stopError.Message);
            errors.Add(stopError);                            // 적고 다음 장치로
        }
    }

    if (errors.Count > 0)
    {
        throw new AggregateException("일부 장치 끄기 실패", errors);
    }
}
```

```text
조명 켜짐
카메라1 켜짐
카메라2 켜짐
---
카메라2 꺼짐
  (실패 기록하고 계속): 카메라1 끄기 실패
조명 꺼짐
Main이 받음: 일부 장치 끄기 실패
  - 카메라1 끄기 실패
```

| 코드 | 역할 |
|---|---|
| `try/catch`를 `for` **안에** | 장치 하나마다 따로 잡음 → 반복은 계속. 바깥이면 A처럼 반복 전체가 끝남 |
| `new AggregateException("메시지", errors)` | 예외 여러 개를 하나로 묶어 던짐 |
| `ex.InnerExceptions` | 묶인 예외를 하나씩 꺼냄 |
| `catch (AggregateException)` 다음 `catch (Exception)` | 위에서부터 맞는 첫 catch만 실행, 좁은 것을 먼저 (순서 바꾸면 CS0160) |

**질문:** 첫 실패만 기억했다가 던져도 조명은 꺼진다. 그런데도 전부 모으는 이유는?

<details><summary>답과 사용자 확인</summary>

첫 실패만 알리면 뒤에 추가로 실패한 장치를 알 수 없다. 현장에서 카메라1을 고치고 재시작 → 이번엔 카메라2 실패 → 또 재시작으로 라인 정지 시간이 늘어난다. 모아서 던지면 한 번에 전부 확인한다. 롤백할 때 처음 원인(켜기 실패)도 같은 메모장에 넣는 이유도 같다(빼면 왜 롤백했는지 모름). .NET Framework의 `AggregateException.Message`에는 안쪽 내용이 붙지 않으므로 로그는 `InnerExceptions`를 돌며 남긴다.

사용자 ✅: "첫 실패만 기억하면 뒤에 실패한 에러를 파악할 수 없다. 그래서 모아서 던진다. 그래야 어디서 터졌는지 모두 확인할 수 있다." (Kata 1차의 "startError를 왜 포함하나" △도 같은 이유로 연결)
</details>

### 조각 4 · `Dispose`는 예외를 던지지 않고, 한 번만 한다

**지금 만드는 것:** 반장에 `Dispose`를 붙이고 `using`으로 쓴다. 검사 중 진짜 오류가 났을 때 뒷정리에서 또 오류가 나면 진짜 오류가 사라지는지 본다.

**비유:** 교실에 불이 났는데(①) 대피 중 스위치 하나가 고장(②). 반장이 "스위치 고장"만 보고하면 선생님은 불이 난 줄 모른다.

`Main`:

```csharp
Device camera1 = new Device("카메라1");
camera1.FailOnStop = true;

try
{
    using (Manager manager = new Manager(new Device[] { new Device("조명"), camera1, new Device("카메라2") }))
    {
        manager.StartAll();
        Console.WriteLine("--- 검사 중");
        throw new InvalidOperationException("① 검사 중 진짜 오류");
    }
}
catch (Exception ex)
{
    Console.WriteLine("Main이 받음: " + ex.Message);
}
```

A(`class Manager : IDisposable`, `Dispose()`가 `StopAll()`을 그냥 부름):

```text
조명 켜짐
카메라1 켜짐
카메라2 켜짐
--- 검사 중
카메라2 꺼짐
  (실패 기록하고 계속): 카메라1 끄기 실패
조명 꺼짐
Main이 받음: 일부 장치 끄기 실패
```

B:

```csharp
private bool _disposed;                       // 이미 정리했나?

public void Dispose()
{
    if (_disposed)
    {
        return;                               // 두 번째 호출이면 아무것도 안 함
    }
    _disposed = true;

    for (int i = _devices.Length - 1; i >= 0; i--)
    {
        try
        {
            _devices[i].Stop();
        }
        catch (Exception disposeError)
        {
            Console.WriteLine("  정리 오류(계속 진행): " + disposeError.Message);   // 적기만, 안 던짐
        }
    }
}
```

```text
조명 켜짐
카메라1 켜짐
카메라2 켜짐
--- 검사 중
카메라2 꺼짐
  정리 오류(계속 진행): 카메라1 끄기 실패
조명 꺼짐
Main이 받음: ① 검사 중 진짜 오류
```

| 코드 | 역할 | 같은 뜻의 긴 코드 |
|---|---|---|
| `class Manager : IDisposable` | 뒷정리가 있다는 약속 | — |
| `using (Manager manager = ...) { }` | `{ }`를 나갈 때 무조건 `Dispose()` | `try { ... } finally { manager.Dispose(); }` |
| `private bool _disposed;` + `if (_disposed) { return; }` | 정리는 딱 한 번 | — |

`Stop`은 던진다(호출한 쪽이 실패를 알아야 함). `Dispose`는 안 던진다(이미 다른 사고가 진행 중일 수 있음). 두 메서드는 마지막 `throw` 한 줄만 다르다.

> **Q. `using(카메라1)`을 쓸 때 Dispose가 에러를 던지면 받아 줄 곳을 구현해야 하나?** (1차 2026-11-06)
> 받아 줄 곳을 만들어도 이미 늦다. `using` = `try/finally`라서 ①이 날아가는 중에 `finally`의 `Dispose`가 ②를 던지면 ②가 ①을 덮어쓴다(C#은 예외를 한 번에 하나만 들고 다님). 그래서 `Dispose`가 안 던지게 만드는 게 답이다. 사용자 재확인 ✅: "사라진 쪽은 ①이고, using은 try/finally와 같아서 try의 1번 에러 뒤 finally의 Dispose에서 2번 에러가 나 2번만 잡히는 구조라 1번이 사라진다."

**질문:** `_disposed` 깃발이 없으면 `Dispose()`가 두 번 불릴 때(예: `using` 한 번 + 폼 닫을 때 한 번) 무슨 일이 생기나?

<details><summary>답과 사용자 확인</summary>

정리 출력(`카메라2 꺼짐` / `정리 오류: 카메라1 끄기 실패` / `조명 꺼짐`)이 두 번 찍힌다. 진짜 장비라면 이미 닫힌 자원을 또 닫으려 해서 `ObjectDisposedException`, 네이티브 SDK면 이미 닫힌 핸들을 또 닫아 접근 위반(강제 종료, `try/catch`로도 못 잡을 수 있음), 로그에는 고장이 두 번 난 것처럼 남는다. `Dispose`는 여러 번 불려도 안전해야 한다(.NET 지침).

사용자 ✅: 출력 두 번 + "꺼진 상태에서 한 번 더 끄려 하니 알 수 없는 에러가 발생할 수 있다."
</details>

### 조각 5 · 창구: 상태를 기억하고, 결과를 쪽지(튜플)로 돌려주기

**지금 만드는 것:** 반장 앞에 창구(`Window`)를 세운다. 창구는 지금 상태를 기억하고, 결과를 예외 대신 `(성공했나, 이유)` 쪽지로 돌려준다.

**비유:** 은행 창구. 번호표 없이 출금하려 하면 "번호표부터" 쪽지를 준다. 금고(반장)에서 문제가 나면 직원이 대신 받아서 "처리 실패: 금고 고장" 쪽지만 준다.

```csharp
enum SystemState { Created, Ready, Running }

class Window
{
    private readonly Manager _manager;

    public Window(Manager manager)
    {
        _manager = manager;
        State = SystemState.Created;
    }

    public SystemState State { get; private set; }

    public (bool ok, string message) Prepare()
    {
        if (State != SystemState.Created)
        {
            return (false, "처음 상태에서만 준비할 수 있습니다. 현재: " + State);
        }
        State = SystemState.Ready;
        return (true, "준비 완료");
    }

    public (bool ok, string message) Start()
    {
        if (State != SystemState.Ready)
        {
            return (false, "준비 상태에서만 시작할 수 있습니다. 현재: " + State);
        }

        try
        {
            _manager.StartAll();                    // 반장은 실패하면 던짐
            State = SystemState.Running;            // 성공했을 때만 상태 변경
            return (true, "운전 시작");
        }
        catch (Exception ex)                        // 창구가 대신 받음
        {
            return (false, "시작 실패: " + ex.Message);
        }
    }
}
```

`Main`(**`try/catch` 없음**): 카메라2 `FailOnStart = true`로 창구를 만들고 ① 준비 전 `Start` ② `Prepare` ③ `Start`(고장) + `window.State` ④ `FailOnStart = false` 후 `Start` + `window.State`를 차례로 찍는다.

```text
① False / 준비 상태에서만 시작할 수 있습니다. 현재: Created
② True / 준비 완료
조명 켜짐
카메라1 켜짐
--- 실패! 켠 것들을 거꾸로 끈다
카메라2 꺼짐
카메라1 꺼짐
조명 꺼짐
③ False / 시작 실패: 카메라2 켜기 실패
   지금 상태: Ready
조명 켜짐
카메라1 켜짐
카메라2 켜짐
④ True / 운전 시작
   지금 상태: Running
```

| 코드 | 역할 | 같은 뜻의 긴 코드 |
|---|---|---|
| `enum SystemState { ... }` | 정해진 이름 중 하나만 갖는 타입 | `const int Created = 0, ...` |
| `{ get; private set; }` | 밖에서는 읽기만 | — |
| `(bool ok, string message)` | 값 2개를 묶어 돌려주는 튜플 (C# 7.0+, net472 기본 포함) | 결과 클래스 또는 `out string message` |
| `result.ok`, `result.message` | 이름으로 꺼냄 | `result.Item1`, `result.Item2` |

`State = Running`이 `StartAll()` 다음 줄이라, 실패하면 이 줄까지 오지 않아 상태가 저절로 `Ready`로 남는다. 줄 순서가 곧 규칙이다.

> **Q. 창구가 던지지 않고 결과로 돌려주면 UI 쪽 코드는 무엇이 편해지나?** (1차)
> 던지는 창구라면 버튼마다 `try/catch`(글러브)가 필요하고 하나만 빠뜨려도 앱이 죽는다. 돌려주는 창구면 `if (!result.ok) { MessageBox.Show(result.message); }` 하나다. 예외를 잡는 곳이 창구 한 곳으로 모이고, 잡는 김에 `Faulted` 같은 상태도 그 자리에서 맞춘다. 단, 생성자에 null을 넣는 것 같은 프로그래머 실수는 그대로 던진다(`ArgumentNullException`). 사용자: 1차 "VisionSystem 상태만 확인하면 된다?" △ → "모르겠어" → 식당 비유(접시 던지기 vs 쪽지) 재설명 → 빈칸 ② try/catch ✅ ③ 죽지 ✅ ① "튜플" △(정답: 잡아서).

> **Q. 일부러 잡지 않고 던지는 `button13`은 그냥 뻗나?** (1차)
> MiniSight는 Day01에서 `Application.ThreadException`에 crash.log 기록 + `Environment.Exit(1)`을 걸어 두었다. 그래서 exe 실행 시 창 없이 기록하고 바로 꺼진다(`Environment.Exit`는 `Main`의 `finally`를 건너뜀). VS F5에서는 디버거가 먼저 그 줄에서 멈춘다. 안전망은 최후의 수단이고, 예상되는 실패는 창구에서 잡아 쪽지로 돌려준다. 층은 ① 창구 catch → 쪽지(계속 운전) ② 버튼 try/catch(필요할 때만) ③ ThreadException(기록하고 종료). crash.log 실제 내용은 확인 안 함.

**질문:** 이 `Main`에는 `try/catch`가 하나도 없는데, ③에서 카메라2가 터졌을 때 왜 죽지 않았나?

<details><summary>답과 사용자 확인</summary>

카메라2 throw → 반장 `StartAll`의 catch에서 시도한 장치를 역순으로 끄고 `throw;` → 창구 `Start`의 catch가 받아 `(false, "시작 실패: ...")`로 바꿈(여기서 예외가 멈춤) → `Main`은 쪽지를 표시만 함. 각 층의 역할: 반장 = 정리, 창구 = 번역(예외 → 쪽지), Main = 표시. `button13`은 가운데 층(창구 catch)이 없어서 안전망까지 떨어졌다.

사용자 ✅: 세 층 흐름을 정확히 설명(1차 △ 해결). 용어 정정: 롤백은 "해제(Dispose)"가 아니라 "끄기(Stop)", "초기화 역순"이 아니라 "켜려고 시도한 순서의 역순".
</details>

### 조각 6 · 창구가 상태 변화를 방송하기 (`event`)

**지금 만드는 것:** 상태가 바뀔 때마다 창구가 방송하고, 화면·로그 파일이 구독한다. 고장 난 스피커(구독자) 하나가 무슨 일을 일으키는지 본다.

**비유:** 학교 방송. 한 줄로 이어진 방송이면 2번 교실 스피커가 고장 날 때 3번 교실은 못 듣고, 방송실이 "방송 실패"를 외쳐 교장은 행사가 취소된 줄 안다. 교실마다 따로 방송하면 고장은 메모만 하고 계속 간다.

`Window`에 `public event Action<SystemState> StateChanged;`를 추가하고, `Prepare`·`Start`의 `State = ...`를 `ChangeState(...)`로 바꾼다.

A:

```csharp
private void ChangeState(SystemState next)
{
    SystemState previous = State;
    State = next;                                       // 1. 상태 먼저
    Console.WriteLine("[창구] " + previous + " -> " + next);   // 2. 기록
    StateChanged?.Invoke(next);                         // 3. 방송 (한 번에)
}
```

`Main`: `화면 수신` 구독 → `Prepare` → 고장 스피커(`throw new InvalidOperationException("화면 갱신 실패")`) 구독 → `로그파일 수신` 구독 → `Start` → 결과와 `window.State` 출력. 고장 스피커를 `Prepare` 앞에 붙이면 `Prepare`(try 없음)에서 터져 프로그램이 죽으므로 뒤에 붙인다.

```text
[창구] Created -> Ready
  화면 수신: Ready
② 준비 완료
조명 켜짐
카메라1 켜짐
[창구] Ready -> Running
  화면 수신: Running
③ False / 시작 실패: 화면 갱신 실패
   진짜 상태: Running
```

B(3번 줄을 교체):

```csharp
    Action<SystemState> handlers = StateChanged;        // 구독자 명단 복사
    if (handlers == null)
    {
        return;                                         // 듣는 사람 없음
    }

    foreach (Action<SystemState> handler in handlers.GetInvocationList())
    {
        try
        {
            handler(next);                              // 교실 하나씩 방송
        }
        catch (Exception ex)
        {
            Console.WriteLine("[창구] 구독자 오류(무시하고 계속): " + ex.Message);
        }
    }
```

```text
[창구] Created -> Ready
  화면 수신: Ready
② 준비 완료
조명 켜짐
카메라1 켜짐
[창구] Ready -> Running
  화면 수신: Running
[창구] 구독자 오류(무시하고 계속): 화면 갱신 실패
  로그파일 수신: Running
③ True / 운전 시작
   진짜 상태: Running
```

| 코드 | 역할 | 같은 뜻의 긴 코드 |
|---|---|---|
| `public event Action<SystemState> StateChanged;` | 방송 채널, 밖에서는 `+=`/`-=`만 | — |
| `window.StateChanged += state => { ... };` | 구독 | 메서드를 만들고 `+= OnChanged;` |
| `StateChanged?.Invoke(next)` | 구독자 전원을 한 줄로 이어서 부름, 하나 터지면 멈춤 | `if (StateChanged != null) { StateChanged(next); }` |
| `handlers.GetInvocationList()` | 구독자를 하나씩 따로 꺼냄 | — |

회사 코드는 `Action<T>` 대신 .NET 표준 모양 `EventHandler<StateChangedEventArgs>`를 쓴다. 안전 방송 방식은 같다.

**질문:** A에서 쪽지는 `False / 시작 실패`인데 진짜 상태는 `Running`이었다. 왜 어긋났나?

<details><summary>답과 사용자 확인</summary>

`ChangeState`가 `Start`의 `try` 안에서 불린다. `State = next`(Running)가 방송보다 먼저 실행됐고, 방송에서 터진 예외가 `Start`의 catch로 떨어져 `(false, "시작 실패")`가 됐다. catch는 상태를 되돌리지 않으므로 상태는 Running, 쪽지는 실패. 그래서 화면·PLC는 멈춘 줄 아는데 카메라는 돌고 있고, 작업자가 다시 시작하면 엉뚱한 쪽지만 받는다. 방송(덤)에서 난 예외는 방송 안에서 끝내야 본업(결과)을 오염시키지 않는다. "상태 먼저 → 기록 → 방송" 순서 자체는 옳다(구독자가 읽는 `State`가 새 값이어야 함).

사용자 ✅: 방송 예외가 Start의 catch로 잡혀 false가 되고, 그 순간 로그파일 구독자는 실행되지 않는다. 상태가 Running으로 남은 이유(`State = next`가 먼저)는 보충.
</details>

### 조각 7 · 화면 버튼과 PLC가 동시에 시작할 때 (`lock`)

**지금 만드는 것:** 스레드 두 개가 동시에 `window.Start()`를 불러 장치가 두 번 켜지는 사고를 보고, `lock`으로 막는다.

**비유:** 줄을 안 서는 은행에서 두 손님이 동시에 출금하면, 잔액 확인 순간 둘 다 "있음"이라 둘 다 출금된다. 창구에 번호표 기계(열쇠 하나)를 둔다.

`Device.Start`에 `Thread.Sleep(100);`(켜는 데 0.1초)을 넣어 틈을 벌린다. `Main`:

```csharp
Window window = new Window(new Manager(new Device[] { new Device("조명"), new Device("카메라1") }));
window.Prepare();

Thread button = new Thread(() =>
{
    (bool ok, string message) result = window.Start();
    Console.WriteLine("화면 버튼: " + result.ok + " / " + result.message);
});
Thread plc = new Thread(() =>
{
    (bool ok, string message) result = window.Start();
    Console.WriteLine("PLC 신호: " + result.ok + " / " + result.message);
});

button.Start();
plc.Start();
button.Join();
plc.Join();
```

A(lock 없음, 3번 실행 모두 두 번씩 켜짐 · 줄 순서는 매번 조금 다름):

```text
[창구] Created -> Ready
조명 켜짐
조명 켜짐
카메라1 켜짐
카메라1 켜짐
[창구] Ready -> Running
[창구] Running -> Running
화면 버튼: True / 운전 시작
PLC 신호: True / 운전 시작
```

B:

```csharp
private readonly object _sync = new object();       // 열쇠

public (bool ok, string message) Start()
{
    lock (_sync)
    {
        if (State != SystemState.Ready)
        {
            return (false, "준비 상태에서만 시작할 수 있습니다. 현재: " + State);
        }

        try
        {
            _manager.StartAll();
            ChangeState(SystemState.Running);
            return (true, "운전 시작");
        }
        catch (Exception ex)
        {
            return (false, "시작 실패: " + ex.Message);
        }
    }
}
```

```text
[창구] Created -> Ready
조명 켜짐
카메라1 켜짐
[창구] Ready -> Running
화면 버튼: True / 운전 시작
PLC 신호: False / 준비 상태에서만 시작할 수 있습니다. 현재: Running
```

(3번 모두 장치 한 번씩, True 하나·False 하나. 누가 이기는지는 매번 다를 수 있음)

| 코드 | 역할 | 같은 뜻의 긴 코드 |
|---|---|---|
| `new Thread(() => { ... })`, `.Start()`, `.Join()` | 일꾼 만들기, 출발, 끝날 때까지 기다리기 | — |
| `lock (_sync) { }` | 한 스레드만 안에 들어감, `return`·예외로 나가도 자동 반납 | `Monitor.Enter(_sync); try { ... } finally { Monitor.Exit(_sync); }` |

검사(`if State != Ready`)와 변경(`ChangeState(Running)`) 사이에 `StartAll`(0.2초)이 끼어 있어서 두 번째 스레드도 `Ready`를 봤다(check-then-act 경쟁, B13 `count++`와 같은 종류). `lock`은 검사부터 변경까지를 한 덩어리로 묶는다. `Sleep`을 빼면 A도 대부분 멀쩡해 보이지만 틈이 짧을 뿐 없어진 게 아니다. 실제로는 `Prepare`, `Stop`도 같은 열쇠로 감싼다.

> **Q. lock이 없으면 화면 버튼과 PLC가 거의 동시에 Start를 부를 때 무슨 일이 생기나?** (1차, 이 조각 전에 말로 먼저 답함)
> 사용자 ✅: "`Status == Running`이 되기 전 상태로 접근하면 `_devices.Start`를 두 번 할 수 있다." → 검사와 변경 사이의 틈, 둘 다 검사 통과, lock = 검사+변경을 한 덩어리로 보충. 실제 장비면 카메라 SDK가 "이미 열려 있음" 예외를 던지거나 트리거 루프가 두 개 돌아 영상이 두 번 들어온다.

**질문:** A의 `[창구] Running -> Running` 줄은 무엇을 뜻하고, 현장 로그에서 보면 무엇을 판단하나?

<details><summary>답과 사용자 확인</summary>

두 스레드가 같이 `Ready`를 보고 들어갔고, 한쪽이 먼저 Running으로 바꾼 걸 모른 채 다른 쪽이 또 Running으로 바꿨다. 현장에서 `A -> A`(같은 값으로 전환) 줄은 동시 호출 경쟁(동기화 문제)의 단서이고, 장치도 두 번 켜졌다는 신호이므로 그 시각 근처에서 `켜짐` 두 번, 카메라 "이미 열려 있음" 오류를 같이 찾는다. 순서: 이상한 로그 → 가설(동시 호출) → `Sleep`으로 틈 벌려 재현 → `lock` 수정 → 같은 실험으로 확인.

사용자 ✅: "같이 Ready를 보고 들어갔는데 누군가 먼저 Running으로 바꾸고, 그걸 모르는 다른 하나가 또 Running으로 바꿨다. 현장에서는 스레드 경쟁을 의심하고 동기화 문제를 봐야 한다."
</details>

### Kata ↔ MiniSight 연결

| Kata | MiniSight | 조각 |
|---|---|---|
| `Manager` | `Devices/DeviceManager.cs` | 0~4 |
| `devices.ToArray()` | 생성자 `_devices = devices.ToArray();` | 1 |
| `StartAll`의 `attempted` + `throw;` | `Start`의 `attempted` + `AggregateException`(원인 + 정리 실패 함께) | 2, 3 |
| `StopAll` 오류 모으기 | `Stop()` | 3 |
| `Dispose` + `_disposed` | `Dispose()`(장치의 `Dispose` 호출) | 4 |
| `Window` | `App/VisionSystem.cs` | 5~7 |
| `SystemState` 1개 | `State` / `Mode` / `Status` 3개 | 5 |
| `Action<SystemState>` | `EventHandler<StateChangedEventArgs>` | 6 |
| `GetInvocationList` + 하나씩 `try/catch` | `ChangeState` 안 같은 코드 | 6 |
| `lock (_sync)` | 모든 공개 메서드 | 7 |

## 3. OmniSight 지도

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 상태 enum 3개 | `OmniSight/Application/VisionSystem.cs:16-35` | State(준비도), Mode(수동/자동), Status(정지/운전) |
| Facade 필드 | `OmniSight/Application/VisionSystem.cs:55-79` | DeviceManager, IInspectionController, ManualCameraController, `_sync` 공유 |
| 시작 규칙 | `OmniSight/Application/VisionSystem.cs:380-427` | lock, Ready 아니면 거부, 중복 시작 무시, **실패 시 StopInternal로 롤백** |
| 정지 순서 | `OmniSight/Application/VisionSystem.cs:430-493` | 취득 완료(drain) → 검사 정지 → 장치 정지. 실패 시 Faulted 유지 |
| 안전한 방송 | `OmniSight/Application/VisionSystem.cs:495-528` | 상태 먼저 확정 → `GetInvocationList()` 하나씩 try/catch |
| 장치 시작/롤백 | `OmniSight/Devices/DeviceManager.cs:184-204` | `started.Add(device)`를 **Start 호출 전에** (L192) |
| 장치 역순 정지 | `OmniSight/Devices/DeviceManager.cs:207-248` | 실패를 모아 `AggregateException` |
| 장치 역순 해제 | `OmniSight/Devices/DeviceManager.cs:250-266` | 하나 실패해도 다음 장치 계속 |
| UI 구독/해제 | `OmniSight/UI/MainForm.cs:585-617`, `:879-938` | 폼 종료 시 반드시 `-=` |

## 4. 왜 이렇게 썼나

### 4.1 상태 3개로 나눈 이유

| 축 | 값 | 질문 |
|---|---|---|
| `VisionSystemState` | Created, Initializing, Ready, Faulted, Disposed | 시스템이 **쓸 수 있는 상태**인가? |
| `VisionMode` | Manual, Auto | 사람이 조작 중인가, 자동 운전 중인가? |
| `VisionStatus` | Stop, Running | 지금 검사가 돌고 있나? |

하나의 enum(예: `ManualStopped, AutoRunning, FaultedWhileRunning…`)으로 합치면 조합 폭발이 난다. 축을 나누면 규칙이 단순해진다: "Start는 `State == Ready && Status == Stop`일 때만".

### 4.2 결정과 이유

| 결정 | 이유 |
|---|---|
| 시작 실패 → 즉시 롤백 | 카메라 2대 중 1대만 켜진 상태로 남지 않게 (**보상 트랜잭션**) |
| `started.Add`를 `Start` 전에 | Start가 **중간까지 진행하다** 실패한 장치도 롤백 대상에 넣어 반쯤 열린 자원을 닫는다 |
| 정지 실패 시 `Faulted` 유지 | "멈췄다"고 거짓말하지 않기. 재시작을 막는다 |
| 상태를 먼저 바꾸고 방송 | 구독자가 `system.State`를 읽으면 새 값이 보여야 한다 |
| 구독자별 try/catch | 화면 하나 버그로 상태 전환·결과·다른 화면이 멈추거나 오염되지 않게 |
| 이벤트 `-=` 해제 | 폼이 닫혔는데 VisionSystem이 폼을 참조하면 **메모리 누수 + 해제된 컨트롤 접근 예외** |

### 4.3 시니어의 눈 (Day25에서 고칠 것)

- `ChangeState`는 `lock(_sync)` **안에서** 이벤트를 부른다. 구독자가 lock을 기다리는 다른 일을 하면 **교착 상태**가 날 수 있다. 지금은 UI가 `BeginInvoke`(비동기 전달)만 해서 괜찮다. "lock 안에서 외부 코드 호출 금지"는 일반 원칙.
- `Stop`은 UI 스레드에서 동기로 호출되고, 내부에서 파이프라인 종료를 기다린다 → 화면 멈춤 위험.

## 5. MiniSight 적용

### 5-1. 완료 (사용자 작성, 2026-11-06 ~ 11-09)

- `Devices/DeviceManager.cs`: `ToArray` 복사, 순서 Initialize, `attempted.Add` 후 Start + 실패 시 역순 Stop과 `AggregateException`(startError + stopErrors), 역순 Stop 오류 모으기, 역순 Dispose(던지지 않음, `_disposed`).
- `App/VisionSystem.cs`: enum 3개, `StateChangedEventArgs`, 생성자 null 검사, `Initialize`/`Start`/`Stop` 튜플 반환, 모든 공개 메서드 `lock (_sync)`.
- Form1 확인 버튼: `button10` 시작 실패 롤백, `button11` 정지 실패 + Dispose 계속, `button12` 방송·두 번째 Initialize 거절, `button13` 잡지 않은 예외, `button14` Start/Stop 순서 규칙, `button15` FailOnStart 롤백 후 재시작.

`button10`(카메라2 FailOnStart) 확인: 카메라2 `Ready -> Faulted` 후 카메라2·카메라1·조명 역순 정지, `오류 개수: 1`, 최종 `Stopped / Stopped / Stopped`. `button11`(카메라1 FailOnStop) 확인: 정지 오류 1개, Dispose 단계에서 `정리 오류(계속 진행): 카메라1 정지 실패(테스트)` 후 조명까지 해제, 최종 `Disposed / Disposed / 해제 True`.

### 5-2. 남은 것 (다음 학습 때)

1. `VisionSystem.ChangeState`의 방송 코드가 **주석 처리**되어 있다. 주석을 풀기 전에 `handlers` 검사 줄의 비교 연산자가 조각 6 B와 반대인지 확인한다(그대로 풀면 방송이 절대 안 나감). 확인은 `button12`의 `로그 수신:` 줄.
2. `VisionSystem.cs` 맨 위 자동 완성으로 들어온 `using static OpenCvSharp.Stitcher;`, `using static System.Windows.Forms.AxHost;` 삭제(창구는 화면·영상 라이브러리를 몰라야 함).
3. `VisionSystem : IDisposable` + `Dispose()`(`lock` 안에서 `Disposed`면 return, `_devices.Dispose()`, `ChangeState(Disposed, Manual, Stop)`).
4. 6.3 `Bootstrapper`에서 VisionSystem 조립(카메라 + 조명 → DeviceManager → VisionSystem), Form1 준비/시작/정지 버튼, 폼 닫을 때 `_system?.Dispose()`.

## 6. 확인한 범위와 다음 단원으로 넘긴 범위

- 확인: Kata 조각 0~7 A/B 출력은 강사 PC에서 C# 7.3 / net472 / x64 컴파일·실행으로 확인. 조각 7은 A·B 각 3회 실행.
- 사용자 실행: MiniSight `button10`~`button15`는 1차에서 출력·메시지 창 확인. 2차 콘솔 Kata(`Day06Kata`)를 집에서 직접 쳐서 실행했는지는 **미보고**.
- 넘긴 것: 5-2의 1~4, 7절 디버깅(Tracepoint, Diagnostic Tools), Day06 3번 타이핑의 뼈대 채우기.
- 설계 메모: MiniSight `DeviceBase.Stop`은 Stopped/Faulted에서도 `OnStop`을 다시 부른다(`Faulted -> Faulted`, `Stopped -> Stopped` 로그). `DeviceBase.Dispose`는 `Stop`이 던지면 같이 던질 수 있어 단독 `using`에서는 조각 4의 덮어쓰기 위험이 있다. DeviceManager가 감싸므로 지금은 안전, Day09 자원 정리 때 다시 본다.

## 7. VS2022 디버깅 실습 (선택, 아직 안 함)

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | `ChangeState`에 Tracepoint: `{previous} -> {state}/{mode}/{status}` | **출력(Output) 창**에 상태 이력이 쭉 찍힘 |
| 2 | 구독자 하나에서 예외를 던지게 하고 실행 | 예외 설정이 켜져 있으면 거기서 멈춤 → F5로 계속 → 다른 구독자 출력 확인 |
| 3 | 디버그 → 창 → **진단 도구(Ctrl+Alt+F2)** | 이벤트 탭: 예외·중단점 타임라인. 메모리 그래프 |
| 4 | Start/Stop을 20번 반복하는 버튼을 만들고 메모리 그래프 관찰 | 계속 올라가면 누수 의심 (Day13에서 본격적으로) |
| 5 | `button15` 실험에서 롤백 중 **호출 스택** 확인 | VisionSystem.Start → DeviceManager.Start → catch → 역순 Stop |

## 8. 셀프 체크

1. Facade와 Adapter의 차이는?
<details><summary>답</summary>Adapter는 이미 있는 인터페이스 하나를 다른 모양으로 바꾼다(1:1 변환). Facade는 여러 하위 시스템을 묶어 단순한 새 진입점을 제공한다(N:1 단순화).</details>

2. 이벤트를 `handler?.Invoke(...)`로 한 번에 부를 때의 문제는?
<details><summary>답</summary>멀티캐스트 델리게이트는 구독자를 순서대로 호출하다가 하나가 예외를 던지면 나머지 구독자는 호출되지 않고 예외가 발행자에게 전파된다. 상태 전환 코드가 중간에 끊기고, 발행자의 catch로 떨어지면 결과까지 오염된다(조각 6 A: 쪽지는 실패, 상태는 Running).</details>

3. lock 안에서 이벤트를 발생시키면 생길 수 있는 문제는?
<details><summary>답</summary>구독자가 같은 lock이 필요한 메서드를 다른 스레드로 부르고 그 결과를 동기로 기다리면 교착 상태가 된다. 또 구독자가 오래 걸리면 lock 보유 시간이 길어져 다른 호출이 모두 대기한다.</details>

4. 장치 시작 롤백에서 "시작에 실패한 그 장치"도 Stop해야 하는 이유는?
<details><summary>답</summary>Start 중간에 일부 자원(스트림 그래버, 트리거 루프)만 열렸을 수 있다. 상태가 Faulted여도 열린 자원은 닫아야 다음 시작에서 충돌하지 않는다(조각 2).</details>

5. `Stop`은 예외를 던지는데 `Dispose`는 던지지 않는 이유는?
<details><summary>답</summary>`Stop`을 부른 쪽은 실패를 알아야 다음 판단(Faulted, 재시도)을 한다. `Dispose`는 `using`/`finally`에서 다른 예외가 이미 날아가는 중에 불릴 수 있어서, 던지면 진짜 원인을 덮어쓴다(조각 4). 그래서 기록만 하고 계속 정리하며, 여러 번 불려도 한 번만 정리한다.</details>
