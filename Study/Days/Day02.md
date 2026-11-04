# Day 02 (화, 2h) · 인터페이스와 계약, SOLID, 소유권

## 1. 오늘 목표

- **완료 기준**
  - "인터페이스에 의존한다"는 말을 OmniSight 예로 설명할 수 있다 (`InspectionPipeline`은 카메라 종류를 모른다).
  - SOLID 5개 중 **DIP, ISP, SRP**를 OmniSight 코드로 하나씩 가리킬 수 있다.
  - MiniSight `Devices/Contracts.cs`(IDevice, IImageSource, DeviceState, ImageFrame)를 손으로 작성했다.
  - `ImageFrame.Dispose()`를 두 번 불러도 안전하다는 걸 디버거로 확인했다.
- **최소분량**: 2, 3, 4, 6
- **확장분량**: 5 Kata 전부, 7번 Watch 실험

## 2. 🧒 초1 비유

- **인터페이스 = 콘센트 모양 약속**. 벽 콘센트(검사기)는 "구멍 두 개" 모양만 약속한다. 드라이기든 선풍기든(카메라든 공유폴더든) 플러그 모양만 맞으면 꽂힌다. 콘센트는 뭐가 꽂혔는지 몰라도 된다.
- **ISP(인터페이스 분리) = 리모컨을 기능별로 나누기**. TV 리모컨에 에어컨 버튼까지 다 있으면 헷갈린다. "영상 주는 장치"와 "미리보기 되는 장치"를 다른 리모컨(인터페이스)으로 나눈다.
- **ImageFrame = 도시락 상자 + 이름표**. 상자 안 밥(픽셀 메모리)은 누가 먹고 누가 설거지(Dispose)하는지 정해야 한다. 설거지를 두 번 해도 깨지면 안 된다.

## 2-1. 2회차 조각별 실습 (2026-11-04 시작)

코드부터 실행하고 한 조각씩 진행한다. 오늘의 순서는 인터페이스 → 구현 교체 → 기능별 계약 분리 → 소유권 → 한 번만 Dispose → MiniSight 계약 작성 → 디버깅·셀프 체크다. 아래 기존 지도와 예제는 뒤에서 비교할 참고 자료이며, 진행에 맞춰 조각으로 옮긴다.

### 조각 1 · 같은 약속으로 선풍기 받기

**지금 만드는 것:** 기기의 실제 종류 대신 `IPowerSource`라는 약속으로 이름을 읽는 프로그램.

비유: 콘센트는 연결한 기기의 내부 구조를 몰라도 플러그 모양이 맞으면 받는다. 오늘 약속은 전기 규격 대신 `Name` 속성이 있다는 것이다.

`Katas` 프로젝트에 `Day02/KataDay02.cs`를 만들어 손으로 친다.

```csharp
using System;

namespace Katas.Day02
{
    public interface IPowerSource
    {
        string Name { get; }
    }

    public sealed class Fan : IPowerSource
    {
        public string Name
        {
            get { return "선풍기"; }
        }
    }

    public static class KataDay02
    {
        public static void Run()
        {
            IPowerSource device = new Fan();
            Console.WriteLine("연결한 기기: " + device.Name);
        }
    }
}
```

| 키워드·기호 | 역할 / 사용법 | 풀어서 읽기 |
|---|---|---|
| `interface` | 사용할 수 있는 멤버의 약속 | 이 타입으로는 Name을 읽을 수 있다 |
| `string Name { get; }` | 문자열을 읽는 속성이 필요 | 인터페이스에는 구현 없이 약속만 적는다 |
| `: IPowerSource` | Fan이 약속을 구현 | Fan에 public Name을 제공한다 |
| `sealed` | 자식 클래스 금지 | Fan을 부모로 하는 클래스는 만들 수 없다 |
| `get { return "선풍기"; }` | 속성 읽기의 실제 구현 | device.Name을 읽으면 이 문자열을 반환한다 |
| `IPowerSource device = new Fan();` | 실제 객체는 Fan, 변수 타입은 약속 | Fan fan = new Fan(); IPowerSource device = fan; |

**실험:** Katas를 시작 프로젝트로 설정한다. Program.cs의 `KataDay01.RunAbandoned();` 등 기존 실행 호출을 주석 처리하고 `Day02.KataDay02.Run();`으로 바꾼다. 마지막 Console.ReadKey는 그대로 둔다. Ctrl+F5로 실행한다.

**확인된 출력:** Roslyn csc /langversion:7.3, 설치된 .NET Framework 기본 참조로 컴파일·실행했다. 이 조각은 4.7.2 전용 참조를 별도로 지정하지 않았다.

```text
연결한 기기: 선풍기
```

**질문 1개:** `device`의 선언 타입은 `IPowerSource`인데 `device.Name`에서 왜 "선풍기"가 나올까?

<details><summary>답</summary>실제로 만든 객체가 Fan이기 때문이다. 인터페이스는 Name을 읽을 수 있다는 약속이고, 실제 속성 구현은 Fan의 getter가 제공한다.</details>

> **Q. 인터페이스는 부모이고 구현 클래스는 자식인가?**
> 실제 객체가 Fan이라 Fan의 Name이 실행된다는 설명은 맞다. 정확한 용어는 "Fan이 IPowerSource 계약을 구현한다"이다. 인터페이스 구현과 클래스 상속을 구별한다. 사용자 파일의 getter는 "Fan"을 반환하므로 첫 출력은 "선풍기"가 아니라 "Fan"이다.

### 조각 2 · 같은 계약으로 기기 교체하기

**지금 만드는 것:** 출력 메서드는 그대로 두고, 전달하는 기기를 선풍기에서 드라이기로 교체한다. 비유: 같은 콘센트에 다른 플러그를 꽂는다.

조각 1의 인터페이스와 Fan은 유지한다. Fan.Name은 비교를 위해 "선풍기"를 반환하도록 맞춘다. namespace 안에서 Fan과 같은 높이에 다음 클래스를 추가한다.

```csharp
public sealed class HairDryer : IPowerSource
{
    public string Name
    {
        get { return "드라이기"; }
    }
}
```

KataDay02 클래스는 다음처럼 작성한다.

```csharp
public static class KataDay02
{
    public static void Run()
    {
        IPowerSource device = new Fan();
        ShowName(device);

        device = new HairDryer();
        ShowName(device);
    }

    private static void ShowName(IPowerSource device)
    {
        Console.WriteLine("연결한 기기: " + device.Name);
    }
}
```

| 키워드·코드 | 역할 / 사용법 | 풀어서 읽기 |
|---|---|---|
| `ShowName(IPowerSource device)` | 계약을 구현한 객체를 인자로 받는다 | Fan과 HairDryer 모두 전달 가능 |
| `private static void` | 클래스 내부에서 객체 생성 없이 호출, 반환값 없음 | Run에서 ShowName(device)로 호출 |
| `device = new HairDryer();` | 기존 변수에 새 객체의 참조를 대입 | 변수의 선언 타입은 IPowerSource 그대로, 새 변수 선언 아님 |

**실험:** Ctrl+F5로 실행. Roslyn /langversion:7.3과 설치된 Framework 기본 참조로 컴파일·실행 확인(4.7.2 전용 참조를 별도로 지정하지 않음).

```text
연결한 기기: 선풍기
연결한 기기: 드라이기
```

**질문 1개:** 나중에 Heater도 IPowerSource를 구현하면, 이름을 출력하기 위해 ShowName의 몸통을 수정해야 할까? 이유도 한 줄로 답한다.

<details><summary>답</summary>수정할 필요 없다. ShowName은 특정 클래스 대신 IPowerSource의 Name 계약만 사용하므로 Heater가 그 계약을 구현하면 그대로 받을 수 있다.</details>

> **Q. 인터페이스의 Name 앞에 public이 없으면 private인가? 구현 클래스에서 고를 수 있나?**
>
> C# 7.3에서 인터페이스에 선언한 멤버는 public 계약이다. 클래스 멤버의 기본 접근성(private)과 구별한다. 인터페이스 자체의 접근성과 내부 멤버의 접근성도 별개다. namespace 바로 아래 `interface IPowerSource`는 기본 internal이고, `public interface IPowerSource`는 public이다. `Name`은 함수가 아니라 속성이다.

```csharp
public interface IPowerSource
{
    string Name { get; }   // public 계약. C# 7.3에서는 public을 직접 적지 않는다.
}

public sealed class Fan : IPowerSource
{
    public string Name     // 일반적인 구현에는 public이 필요하다.
    {
        get { return "선풍기"; }
    }
}
```

| 위치 | 접근 제한자를 생략하면? | 이번 코드에서의 작성법 |
|---|---|---|
| 인터페이스 안의 `Name` 계약 | public | `string Name { get; }` |
| 클래스 안의 일반 `Name` 속성 | private | 계약을 구현하려면 `public string Name` |

비유: 인터페이스는 "누구나 누를 수 있는 이름 확인 버튼이 있어야 한다"는 약속이다. 구현 클래스가 그 버튼을 private으로 숨기면 약속을 지키지 못한다.

**확인 실험:** Roslyn `csc /langversion:7.3`로 일반 public 구현은 컴파일 성공, 일반 private 구현은 CS0737, 인터페이스 계약 선언에 public을 직접 적으면 CS8703(현재 설치된 컴파일러)으로 확인했다. 기본 Framework 참조를 사용했으며 4.7.2 전용 참조를 따로 지정하지 않았다.

**참고: 명시적 인터페이스 구현**은 다른 작성 방법이다. private/public 중 하나를 선택하는 방식이 아니며 접근 제한자를 적지 않는다.

```csharp
public sealed class Fan : IPowerSource
{
    string IPowerSource.Name
    {
        get { return "선풍기"; }
    }
}
```

이때 `IPowerSource device = new Fan();`을 통해 `device.Name`은 읽을 수 있다. `Fan` 타입 변수로는 그 속성을 직접 읽을 수 없다. 명시적 구현은 C# 7.3으로 컴파일·실행 성공을 확인했다. 지금 Kata는 일반 public 구현을 유지한다.

공식 참고: [interface 접근 제한자](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/interface#access-modifiers), [명시적 인터페이스 구현](https://learn.microsoft.com/dotnet/csharp/programming-guide/interfaces/explicit-interface-implementation). 최신 문서의 C# 8 이상 예제를 현재 7.3 코드에 그대로 적용하지 않는다.

> **Q. 구현 클래스의 `private void Show()`는 C# 7.3에서 안 돼도 8 이후에는 가능한가?**
>
> 질문은 `IPowerSource`의 `void Show();`를 Fan의 `private void Show()`로 구현할 수 있는지이다. C# 8에서도 불가능하다. HairDryer의 public Show는 계약을 지키지만, Fan의 private Show는 계약을 지키지 못한다.

```csharp
public interface IPowerSource
{
    void Show();    // public 계약
}

public sealed class Fan : IPowerSource
{
    private void Show()    // CS0737: public 계약의 일반 구현이 될 수 없음
    {
    }
}
```

**비교 실험:** 사용자가 제시한 Name 속성과 Fan·HairDryer를 포함한 코드를 `csc /langversion:7.3`과 `/langversion:8.0`으로 컴파일했다. 둘 다 Fan.Show에서 CS0737이다. 기본 Framework 참조를 사용했으며 4.7.2 전용 참조는 별도로 지정하지 않았다.

C# 8에서 바뀐 것은 인터페이스 **내부**의 작성 규칙이다. 클래스의 일반 private 메서드가 public 계약을 구현할 수 있게 된 것은 아니다. 앞선 설명의 "8 이후"는 인터페이스 선언 안에 public을 직접 적을 수 있다는 부분을 가리킨다.

일반 구현은 `public void Show()`로 작성한다. 명시적 구현은 `void IPowerSource.Show()`로 작성하며 접근 제한자를 붙이지 않는다. 명시적 구현은 7.3에서도 가능하고 인터페이스 타입 변수로 호출한다.

### 조각 3 · 타이머 기능은 별도 계약으로 나누기 (ISP)

**지금 만드는 것:** 타이머가 있는 선풍기만 타이머를 설정하고, 없는 드라이기는 없다고 출력한다. 오늘의 HairDryer 모델에는 타이머가 없다고 가정한다.

비유: 모든 가전 리모컨에 타이머 버튼을 강제로 넣지 않는다. 이름 계약(IPowerSource)과 타이머 계약(IHasTimer)을 나누고, 필요한 기기만 구현한다. 이를 인터페이스 분리 원칙(ISP)이라고 한다.

기존 `IPowerSource`는 유지한다. 그 아래, Fan과 같은 높이에 추가한다.

```csharp
public interface IHasTimer
{
    void SetTimer(int minutes);
}
```

Fan 선언에 계약을 하나 추가하고 Fan 클래스 안에 SetTimer를 넣는다. 기존 Name은 유지한다.

```csharp
public sealed class Fan : IPowerSource, IHasTimer
{
    public string Name
    {
        get { return "선풍기"; }
    }

    public void SetTimer(int minutes)
    {
        Console.WriteLine(Name + " 타이머 " + minutes + "분");
    }
}
```

HairDryer는 IPowerSource만 구현한 채 그대로 둔다. KataDay02의 Run을 다음으로 바꾸고 TrySetTimer를 같은 클래스 안에 추가한다. 기존 ShowName은 남겨도 된다.

```csharp
public static void Run()
{
    TrySetTimer(new Fan());
    TrySetTimer(new HairDryer());
}

private static void TrySetTimer(IPowerSource device)
{
    IHasTimer timer = device as IHasTimer;
    if (timer != null)
    {
        timer.SetTimer(30);
    }
    else
    {
        Console.WriteLine(device.Name + ": 타이머 없음");
    }
}
```

| 키워드·기호 | 역할 / 사용법 | 풀어서 읽기 |
|---|---|---|
| `: IPowerSource, IHasTimer` | 쉼표로 여러 계약을 구현 | Fan은 이름과 타이머 두 약속을 지킨다 |
| `void SetTimer(int minutes)` | int 인자 1개, 반환값 없는 메서드 계약 | SetTimer(30)은 30분을 전달한다 |
| `device as IHasTimer` | 해당 계약으로 볼 수 있으면 참조 반환, 아니면 null | 새 객체를 만들지 않음. 아래 긴 코드 참고 |
| `!= null` | null이 아니면 | 타이머 계약이 있는 경우에만 호출 |

`as`를 길게 쓴 같은 의미의 코드(읽기용):

```csharp
IHasTimer timer;
if (device is IHasTimer)
{
    timer = (IHasTimer)device;
}
else
{
    timer = null;
}
```

`is`는 해당 타입으로 사용할 수 있는지 검사한다. `(IHasTimer)`는 타입 변환이다. 위 코드에서는 검사에 성공한 경우에만 변환한다.

**실험:** Ctrl+F5로 실행한다. 로컬 사용자 Kata를 읽어 HairDryer.Name이 "헤어드라이기"인 것을 확인했다. 사용자 파일은 수정하지 않고, 임시 파일에서 조각 3을 조립했다. Roslyn `csc /langversion:7.3` + .NET Framework 4.7.2 참조(mscorlib, System, System.Core)를 명시하여 컴파일·실행 성공.

```text
선풍기 타이머 30분
헤어드라이기: 타이머 없음
```

**질문 1개:** 왜 SetTimer를 IPowerSource에 넣지 않고 IHasTimer로 따로 나눴을까? HairDryer 입장에서 한 줄로 답한다.

<details><summary>답</summary>타이머가 없는 HairDryer가 필요 없는 SetTimer까지 구현하도록 강요받지 않게 하려고 나눈다. 기능별 계약을 분리하면 해당 기능이 있는 기기만 구현한다.</details>

> **학습 확인:** 사용자는 "그러면 HairDryer에도 SetTimer를 구현해야 한다. 필요 없는 함수를 구현해야 하는 격이다"라고 답했다. ISP의 핵심 이유를 정확히 설명했다(✅). 실행 출력은 별도로 아직 확인하지 않았다.

### 조각 4 · 빌려 쓰는 동안 주인이 정리하면? (소유권)

**지금 만드는 것:** 같은 자원을 두 변수로 바라본 뒤, 한쪽에서 Dispose하면 다른 쪽도 사용할 수 없다는 실험.

비유: 도시락 상자 하나를 친구에게 잠깐 빌려 줬다. 친구가 다 먹기 전에 주인이 상자를 치우면 친구는 더 사용할 수 없다. 변수를 하나 더 만든다고 새 상자가 생기지 않는다.

이 실험에서는 original을 정리 책임이 있는 소유자, borrowed를 잠깐 사용하는 쪽으로 약속한다. 소유권은 C#이 변수 이름이나 대입만 보고 자동으로 정하는 것이 아니라, 코드와 문서로 정하는 사용·정리 책임이다. 소유권을 넘기는 경우에는 받은 쪽이 정리 책임까지 받는다.

`Katas/Day02/KataDay02.cs` 맨 위에 `using System.IO;`를 추가한다. 이전 조각의 클래스와 메서드는 남기고 KataDay02.Run의 내용만 다음으로 교체한다.

```csharp
public static void Run()
{
    MemoryStream original = new MemoryStream();
    MemoryStream borrowed = original;

    borrowed.WriteByte(7);
    Console.WriteLine("저장한 바이트 수: " + original.Length);

    original.Dispose();
    Console.WriteLine("빌린 변수는 null인가? " + (borrowed == null));

    try
    {
        borrowed.WriteByte(8);
    }
    catch (ObjectDisposedException)
    {
        Console.WriteLine("이미 정리된 자원은 사용할 수 없음");
    }
}
```

| 키워드·코드 | 역할 / 사용법 | 풀어서 읽기 |
|---|---|---|
| `MemoryStream` | 메모리에 바이트를 읽고 쓰는 스트림, System.IO 소속 | 이 실험의 도시락 상자. 실제 파일은 만들지 않는다 |
| `borrowed = original` | 같은 객체의 참조를 복사 | 새 객체를 만들거나 소유권을 자동 이전하지 않는다 |
| `WriteByte(7)` | 값 7인 바이트 하나 기록 | 바이트 값 7과 바이트 개수 1은 다르다 |
| `Length` | 기록한 데이터 길이, 바이트 단위 | WriteByte 1번 후에는 1 |
| `Dispose()` | 다 쓴 자원을 정리 | 참조 변수들을 null로 바꾸지 않는다 |
| `ObjectDisposedException` | 정리된 객체에 금지된 작업을 했다는 런타임 예외 | 여기서는 Dispose 뒤 WriteByte 호출 시 발생 |
| `try` / `catch` | 실행 중 예외를 받아 처리 | WriteByte가 던진 예외를 catch에서 받아 출력 |

MemoryStream.Dispose는 스트림을 닫아 더 이상 쓸 수 없게 한다. 내부 배열 메모리가 이 순간 즉시 해제된다는 뜻은 아니다. 자원마다 Dispose의 구체적 동작은 다르다.

**실험:** Ctrl+F5로 실행. Roslyn `csc /langversion:7.3 /platform:x64`와 .NET Framework 4.7.2 참조(mscorlib, System)를 명시하여 임시 파일에서 컴파일·실행 확인. 사용자 Kata 파일은 직접 수정하지 않았다.

```text
저장한 바이트 수: 1
빌린 변수는 null인가? False
이미 정리된 자원은 사용할 수 없음
```

중요한 관찰: borrowed가 null이 아니어도 자원이 사용 가능한 것은 아니다. 원본과 빌린 변수는 같은 스트림을 가리킨다. 예외는 실제로 WriteByte를 시도했기 때문에 발생한다.

**질문 1개:** borrowed 쪽에서 아직 쓸 일이 남아 있다면 original.Dispose()는 언제 해야 할까? 정리 책임자는 original 쪽이라는 약속을 유지한다.

<details><summary>답</summary>빌린 쪽의 사용이 모두 끝난 뒤, 소유자인 original 쪽에서 Dispose해야 한다. 아직 쓰는 사람이 있는데 먼저 정리하면 이미 정리된 자원에 접근하게 된다.</details>

> **Q. borrowed를 쓰고 borrowed = null을 준 뒤 original을 Dispose해야 하나?**
>
> 사용이 끝난 뒤 소유자가 Dispose한다는 순서는 맞다. 하지만 borrowed = null은 필수가 아니다. null 대입은 그 변수의 참조만 끊으며, 스트림을 닫거나 다른 사용자의 작업 완료를 확인하지 않는다. 이 단일 스레드 실험에서는 마지막 사용 다음에 original.Dispose()만 해도 된다. 여러 스레드라면 실제 작업 완료를 기다려야 하며 null 대입이 동기화를 대신하지 못한다.

```csharp
borrowed.WriteByte(7);  // 이 예제에서는 빌린 쪽의 마지막 사용
original.Dispose();    // 사용이 끝났으므로 소유자가 정리 (null 대입 불필요)
```

| 동작 | 의미 |
|---|---|
| `borrowed = null;` | borrowed 변수만 객체를 가리키지 않게 함. original은 같은 객체를 계속 가리킴 |
| `original.Dispose();` | 해당 MemoryStream을 닫음. borrowed를 자동으로 null로 만들지 않음 |

비유: 상자를 가리키던 친구의 손을 내리는 것(null 대입)과, 주인이 사용이 끝난 상자를 정리하는 것(Dispose)은 별개의 행동이다.

**확인 실험:** borrowed = null 뒤에도 original.WriteByte(8)은 성공했다. original.CanWrite는 True, 두 번 쓴 뒤 Length는 2, original.Dispose 뒤 CanWrite는 False였다. C# 7.3 / x64 / .NET Framework 4.7.2 참조로 임시 파일을 컴파일·실행해 확인했다. 사용자 파일은 수정하지 않았다.

### 조각 5 · Dispose를 두 번 불러도 정리는 한 번만

**지금 만드는 것:** 같은 도시락 상자에 Dispose를 두 번 호출해도 정리 본문은 한 번만 실행되게 만든다.

비유: 설거지를 시작할 때 상자에 "정리 시작" 도장을 찍는다. 이미 도장이 있으면 두 번째 사람은 손대지 않는다. 이 예제의 설거지는 출력으로 모의한 정리이며, 실제 외부 자원을 해제하지 않는다. 뒤에서 ImageFrame의 실제 이미지 정리로 연결한다.

`KataDay02.cs` 위에 `using System.Threading;`을 추가한다. 기존 클래스는 남기고 namespace 안에서 Fan과 같은 높이에 LunchBox를 추가한다.

```csharp
public sealed class LunchBox : IDisposable
{
    private int _disposed;

    public void Dispose()
    {
        int previous = Interlocked.Exchange(ref _disposed, 1);
        if (previous != 0)
        {
            return;
        }

        Console.WriteLine("설거지 완료 (딱 한 번)");
    }
}
```

`KataDay02.Run()`만 다음으로 바꾼다. 다른 메서드는 남긴다.

```csharp
public static void Run()
{
    LunchBox box = new LunchBox();

    Console.WriteLine("1) Dispose 호출");
    box.Dispose();

    Console.WriteLine("2) Dispose 호출");
    box.Dispose();

    Console.WriteLine("끝");
}
```

| 키워드·코드 | 역할 / 사용법 | 풀어서 읽기 |
|---|---|---|
| `: IDisposable` | 정리 메서드를 제공하는 계약을 구현 | `public void Dispose()`를 작성한다 |
| `private int _disposed;` | 정리 진입 표시, 처음에는 0 | 숫자 필드 기본값은 0 |
| `Interlocked.Exchange(ref _disposed, 1)` | 필드를 1로 바꾸고 **바꾸기 전 값**을 반환 | 교환 전체가 원자적 연산. 읽기와 쓰기를 분리하지 않는다 |
| `ref` | 변수 자체를 전달 | 복사본이 아니라 _disposed를 바꾼다 |
| `previous != 0` | 이미 정리 진입 표시가 있었는지 확인 | 처음은 0, 이후는 1 |
| `return;` | 현재 메서드를 즉시 끝냄 | 두 번째 호출은 출력 줄까지 가지 않는다 |

의미를 순서로 읽으면 "이전 값 받기 → 1로 바꾸기"이지만, 실제 Exchange는 둘을 분리하지 않고 원자적으로 수행한다. 일반 읽기·대입 두 줄로 바꾸면 동시 호출에서 같은 보장을 할 수 없다.

| 호출 | 이전 값(previous) | 교환 후 _disposed | 결과 |
|---|---|---|---|
| 첫 번째 | 0 | 1 | 정리 본문 실행 |
| 두 번째 | 1 | 1 | return으로 종료 |

단순히 `if (_disposed == 0)` 검사 뒤 별도로 1을 대입하면, 두 스레드가 모두 0을 읽어 정리를 중복 실행할 수 있다(B13). Exchange는 한 호출만 이전 값 0을 받게 한다. 이 패턴은 정리 진입을 한 번으로 제한하며, 다른 사용 메서드와 Dispose 사이의 모든 동시성까지 해결하는 것은 아니다. 다른 호출이 return했다고 첫 정리가 반드시 완료됐다는 뜻도 아니다.

**실험:** Ctrl+F5로 실행한다. C# 7.3 / x64 / .NET Framework 4.7.2 참조(mscorlib, System)로 임시 파일 컴파일·실행 성공. 사용자 파일은 수정하지 않았다. 이 실행은 순차 두 번 호출 확인이며 동시 호출 실행 시험은 하지 않았다.

```text
1) Dispose 호출
설거지 완료 (딱 한 번)
2) Dispose 호출
끝
```

**질문 1개:** 두 번째 Dispose에서 previous는 몇이고, 왜 "설거지 완료"가 다시 출력되지 않을까?

<details><summary>답</summary>previous는 1이다. 첫 호출이 이미 _disposed를 1로 바꿨기 때문에, 두 번째 교환의 이전 값도 1이다. previous != 0 조건이 참이어서 return으로 끝나고 정리 출력까지 도달하지 않는다.</details>

> **학습 확인:** 사용자는 "두 번째 Dispose에서 previous는 1이므로 설거지 완료가 출력되지 않는다"고 답했다(✅). previous != 0 조건에서 return하므로 출력 줄까지 가지 않는다는 실행 흐름을 덧붙였다. 사용자 실행 출력은 아직 별도로 확인하지 않았다.

### 조각 6 · MiniSight의 장치 계약으로 가짜 장치 받기

**지금 만드는 것:** WinForms 버튼에서 IDevice 타입으로 가짜 장치의 이름과 상태를 읽는다. 콘센트(IPowerSource)에서 실무 장치 계약(IDevice)으로 연결한다.

로컬 MiniSight에 1회차의 Devices/Contracts.cs, DeviceBase.cs, TestDevice.cs가 이미 있다. 중복 클래스를 만들지 않는다. 기존 파일의 DeviceState와 ImageFrame 등은 유지하고 IDevice 블록만 다음 내용을 보며 다시 친다. System과 System.Threading using은 이미 있는지 확인한다.

```csharp
public interface IDevice : IDisposable
{
    string Name { get; }
    DeviceState State { get; }
    void Initialize(CancellationToken cancellationToken);
    void Start(CancellationToken cancellationToken);
    void Stop();
}
```

Name은 이름, State는 상태를 읽는 계약이다. DeviceState는 기존 enum(Created, Initializing, Ready, Running, Stopped, Faulted, Disposed)이다. Initialize/Start/Stop의 구현과 상태 변화는 Day03에서 자세히 다룬다. 여기서는 TestDevice를 만들기만 하므로 상태는 Created이다.

IDevice : IDisposable은 인터페이스가 다른 인터페이스의 계약을 상속하는 것이다. 따라서 IDevice를 구현한 클래스는 Dispose도 제공해야 한다. 실제 TestDevice는 DeviceBase에서 IDevice 구현을 상속받는다. 클래스를 상속하는 것과 인터페이스의 계약을 구현하는 것을 구별한다.

**실험 준비:** 솔루션 탐색기에서 MiniSight를 시작 프로젝트로 설정하고 Debug/x64를 선택한다. Form1 디자이너에서 Button을 하나 추가(button4), Text를 "장치 계약 확인"으로 바꾼 뒤 더블클릭한다. Form1.cs 위에 `using MiniSight.Devices;`를 추가하고 핸들러를 아래처럼 작성한다. 기존 Day01 버튼 핸들러들은 유지한다.

```csharp
private void button4_Click(object sender, EventArgs e)
{
    using (IDevice device = new TestDevice("연습 장치"))
    {
        MessageBox.Show("장치: " + device.Name
            + Environment.NewLine + "상태: " + device.State);
    }
}
```

| 키워드·코드 | 역할 / 사용법 | 풀어서 읽기 |
|---|---|---|
| `IDevice : IDisposable` | 인터페이스 사이의 계약 상속 | IDevice에는 Dispose 계약도 포함 |
| `DeviceState` / `enum` | 정해진 상태 이름 중 하나 | 생성 직후 Created, 연결 전 상태 |
| `CancellationToken` | 취소 요청을 확인하는 토큰 | Initialize/Start가 받을 인자, 지금은 호출하지 않음 |
| `using (IDevice device = ...)` | 블록을 빠져나갈 때 Dispose 호출 | 이름만 읽는 계약에 정리 계약까지 포함된 예 |
| `new TestDevice("연습 장치")` | 하드웨어 대신 가짜 장치 생성 | 문자열은 생성자에 전달할 이름 |
| `Environment.NewLine` | 운영체제의 줄바꿈 문자열 | Windows에서는 \r\n |

**확인된 결과:** 사용자 MiniSight의 Contracts.cs, DeviceBase.cs, TestDevice.cs를 읽어 임시 콘솔 하네스에 함께 컴파일했다. C# 7.3 / x64 / .NET Framework 4.7.2 참조와 설치된 OpenCvSharp4 4.13.0.20260627의 관리 DLL을 사용했다. 이름·상태 문자열과 using 뒤 정리 출력이 아래처럼 나왔다. 이 검증에서는 Mat을 생성하거나 실제 장비에 연결하지 않았다.

```text
장치: 연습 장치
상태: Created
 [연습 장치] 자원 해제
연습 장치: Created -> Disposed
```

사용자가 Ctrl+F5로 실행하고 button4를 누르면 메시지 창에 앞의 두 줄이 표시될 것으로 예상한다. 확인을 눌러 메시지 창을 닫은 뒤 using이 끝나 장치를 Dispose한다. 마지막 두 줄은 기존 가짜 장치의 Console 출력이며 메시지 창에 포함되지 않는다. WinForms 버튼 추가·클릭은 이 턴에 직접 실행 확인하지 않았다. 사용자 포트폴리오 파일도 직접 수정하지 않았다.

**질문 1개:** IDevice 안에는 Dispose()를 직접 적지 않았는데, 왜 IDevice 변수도 using으로 정리할 수 있을까?

<details><summary>답</summary>IDevice가 IDisposable의 계약을 상속하기 때문에 Dispose 계약도 포함한다. TestDevice는 DeviceBase를 통해 그 계약을 구현하므로 using이 끝나면 실제 객체의 Dispose가 호출된다.</details>

> **학습 확인:** 사용자는 IDevice에 IDisposable이 연결되어 있으므로 IDevice를 구현하는 클래스는 Dispose도 구현해야 한다고 설명했다(✅). 용어는 "IDevice가 IDisposable 계약을 상속하고, 클래스가 그 계약을 구현한다"로 다듬었다. 부모 클래스가 이미 Dispose를 구현했다면 자식이 새로 작성할 필요는 없다. 현재 DeviceBase가 Dispose를 제공하고 TestDevice는 그 구현을 상속한다. 인터페이스 선언 자체에 Dispose의 실행 코드가 들어 있는 것은 아니다. 사용자 WinForms 실행 결과는 아직 별도로 받지 않았다.

### 조각 7 · 실제 이미지 한 장의 소유권과 정리

**지금 만드는 것:** 검은 이미지 한 장을 ImageFrame에 넘기고, 두 번 Dispose한 뒤 원래 Mat도 정리되었는지 확인한다.

비유: Mat은 음식, ImageFrame은 음식과 이름표를 담은 도시락 상자다. 생성자에 Mat을 넘기면 상자가 그 이미지의 정리 책임을 넘겨받는다는 약속이다. 이름표(FrameId, SourceName, Timestamp)와 실제 이미지(Image)를 한 객체로 묶는다. 생성자가 이미지 픽셀을 복사하는 것은 아니다.

**기존 코드 확인:** MiniSight/Devices/Contracts.cs에 ImageFrame이 이미 있다. _image = image 대입으로 같은 Mat을 보관하고, null 이미지는 ArgumentNullException으로 거부한다. Timestamp는 기존 코드의 DateTime.UtcNow를 유지한다. 생성자를 호출해 보면서 계약을 확인하는 조각이므로 기존 클래스를 중복 작성하지 않는다.

Form1.cs 위에 `using OpenCvSharp;`를 추가한다. MiniSight가 시작 프로젝트, Debug/x64인지 확인한다. Form1 디자이너에 button5를 추가하고 Text를 "이미지 정리 확인"으로 바꾼다. 더블클릭해서 생긴 핸들러에 작성한다.

```csharp
private void button5_Click(object sender, EventArgs e)
{
    Mat image = new Mat(100, 200, MatType.CV_8UC1, Scalar.All(0));

    using (ImageFrame frame = new ImageFrame(1, "Fake", image))
    {
        string before = "폭: " + frame.Image.Width;
        frame.Dispose();
        frame.Dispose();

        string after = "정리 후 Image는 null인가? " + (frame.Image == null);
        string original = "처음 Mat도 정리됐나? " + image.IsDisposed;
        MessageBox.Show(before + Environment.NewLine + after
            + Environment.NewLine + original);
    }
}
```

| 키워드·코드 | 역할 / 사용법 | 풀어서 읽기 |
|---|---|---|
| `Mat` | OpenCvSharp의 이미지 객체 | 픽셀 등 네이티브 자원을 관리하므로 Dispose 필요 |
| `new Mat(100, 200, ...)` | 세로 100행, 가로 200열 | Width는 두 번째 숫자인 200 |
| `MatType.CV_8UC1` | 8비트 부호 없는 값, 채널 하나 | 이번 예제는 흑백 이미지 |
| `Scalar.All(0)` | 모든 성분을 0으로 채움 | 이번 흑백 이미지에서는 검은색 |
| `new ImageFrame(1, "Fake", image)` | 번호·출처·이미지를 묶음 | 이 생성자 계약상 이미지의 정리 책임도 Frame으로 이동 |
| `image.IsDisposed` | 이 Mat 객체가 Dispose되었는지 확인 | 객체 참조가 null인지와 다른 질문 |
| `using (ImageFrame frame = ...)` | 블록을 빠져나갈 때 Frame.Dispose 호출 | 실험의 직접 두 번 호출 뒤 블록 끝에서도 한 번 더 호출되지만 같은 가드에서 return |

Mat의 소유권을 Frame으로 넘겼으므로 호출자는 image를 별도 using으로 감싸거나 Frame이 쓰는 동안 image.Dispose를 먼저 호출하지 않는다. 위 image 변수는 실험 관찰용 참조이며 Dispose 책임은 Frame에 있다.

기존 ImageFrame.Dispose의 핵심은 다음과 같다(읽기용).

```csharp
if (Interlocked.Exchange(ref _disposed, 1) != 0)
{
    return;
}

Mat image = Interlocked.Exchange(ref _image, null);
if (image != null)
{
    image.Dispose();
}
```

첫 Exchange는 조각 5처럼 중복 진입을 막는다. 두 번째 Exchange는 _image를 null로 바꾸고, **그 전에 들어 있던 Mat 참조**를 돌려준다. 그 Mat을 Dispose한다. 정수에만 Exchange를 쓰는 것이 아니라 참조형에도 쓸 수 있다. null 대입 자체가 자원 해제인 것은 아니다. `Volatile.Read(ref _image)` getter와 Exchange가 있어도 Image를 받은 직후 다른 스레드가 Dispose하는 경쟁까지 막는 것은 아니므로, 사용 완료 후 소유자가 정리한다는 조각 4 규칙은 유지된다.

**실험:** Ctrl+F5로 실행하고 button5를 누른다. 설치된 OpenCvSharp4 4.13.0.20260627, 실제 Contracts.cs와 네이티브 OpenCvSharpExtern DLL로 임시 콘솔 하네스에서 검증했다. C# 7.3 / x64 / .NET Framework 4.7.2 전용 참조로 컴파일·실행했고, using 종료 시 추가 Dispose도 예외 없이 끝났다.

```text
폭: 200
정리 후 Image는 null인가? True
처음 Mat도 정리됐나? True
```

이는 소프트웨어가 만든 이미지로 수행한 테스트다. 실제 카메라 촬영·WinForms 버튼 클릭은 이 턴에 확인하지 않았다. 사용자 포트폴리오 파일도 수정하지 않았다. 사용 환경에서 네이티브 DLL 로딩 오류가 나면 그 메시지로 원인을 확인하고, 패키지 버전을 임의로 바꾸지 않는다.

**질문 1개:** Frame만 Dispose했는데 왜 처음 만든 image의 IsDisposed도 True가 될까?

<details><summary>답</summary>처음 image와 Frame 내부의 _image가 같은 Mat 객체를 가리키기 때문이다. Frame이 그 객체의 Dispose를 호출했으므로 원래 참조로 확인해도 이미 정리된 상태다. 생성자 대입은 이미지 복사가 아니다.</details>

> **학습 확인:** 사용자는 ImageFrame.Dispose 내부에 _image를 Dispose하는 부분이 있어서 원래 이미지도 정리된다고 설명했다(✅). 생성자의 `_image = image;`는 같은 Mat의 참조를 보관하는 것이며, 이미지 복사가 아니므로 원래 image로 확인해도 IsDisposed가 True라는 점을 보충했다. 실제 Dispose에서는 Exchange로 꺼낸 로컬 image에 Dispose를 호출한다. 사용자 버튼 실행 결과는 아직 별도로 확인하지 않았다.

### 조각 8 · 디버거로 첫 번째와 두 번째 Dispose 비교

**지금 하는 것:** 기존 button5 코드를 F5/F10으로 따라가며 정리 표시와 이미지 참조가 바뀌는 순간을 본다. 새 코드는 추가하지 않는다. 조각 7의 이미지 생성·정리 로직은 임시 콘솔에서 실행 확인했으며, 이 조각의 VS2022 UI 조작은 사용자가 직접 수행한다.

비유: 상자에 정리 시작 도장이 찍히고, 주인이 상자에서 이미지를 꺼내 정리하는 과정을 확대경으로 본다.

로컬 Form1.cs에서 button5 구현과 두 번의 frame.Dispose 호출을 확인했다. Contracts.cs의 실제 Dispose는 다음 코드다.

```csharp
public void Dispose()
{
    if (Interlocked.Exchange(ref _disposed, 1) != 0)
    {
        return;
    }

    Mat image = Interlocked.Exchange(ref _image, null);
    if (image != null)
    {
        image.Dispose();
    }
}
```

| 순서 | VS2022에서 할 일 | 볼 것 |
|---|---|---|
| 1 | MiniSight를 시작 프로젝트로, Debug/x64 선택 | 기존 인스턴스가 있으면 닫고 시작 |
| 2 | Contracts.cs의 위 if(Interlocked.Exchange...) 줄에 커서를 두고 F9 | 중단점은 메서드 호출 버튼 쪽이 아니라 ImageFrame.Dispose 안의 조건 줄 |
| 3 | F5로 실행, Form1의 "이미지 정리 확인" 버튼 클릭 | 첫 번째 Dispose 조건 줄에서 멈춤 |
| 4 | 디버그 → 창 → 조사식 → 조사식 1에서 `_disposed`, `_image == null` 추가 | 아직 조건 줄 실행 전이므로 0 / False |
| 5 | F10으로 조건 줄 실행 | _disposed=1, 다음 노란 줄은 Mat image=Exchange... / _image는 아직 null 아님 |
| 6 | F10으로 Mat 교환 줄 실행 | _image는 null, 로컬 image에는 기존 Mat. 변수 이름이 비슷해도 필드와 지역 변수는 별개 |
| 7 | F10으로 if와 image.Dispose를 지나가 보기 | 실제 Mat 정리. 호출 후 로컬 image.IsDisposed는 True |
| 8 | F5로 계속 | 두 번째 Dispose가 같은 중단점에서 멈춤. _disposed=1 / _image==null은 True |
| 9 | F10으로 조건 실행 | Exchange가 이전 값 1 반환 → 조건 참 → return으로 감 |
| 10 | F5로 계속해 메시지 확인 후 닫기 | using 끝에서 세 번째 Dispose에도 중단점이 걸릴 수 있음. 값은 두 번째와 같고 F5로 계속하면 됨 |

| 키워드·도구 | 역할 / 사용법 | 풀어서 읽기 |
|---|---|---|
| F9 | 중단점 설정/해제 | 실행을 멈추고 싶은 줄 표시 |
| F5 | 디버깅 시작/계속 | 다음 중단점 또는 종료까지 실행 |
| F10 | 한 줄 실행 | 현재 노란 줄의 일을 실행한 뒤 다음 위치에서 멈춤 |
| 조사식(Watch) | 현재 변수와 식의 값을 관찰 | `_disposed`, `_image == null`처럼 읽기만 하는 식을 넣음 |
| `_image`와 `image` | 필드와 지역 변수 | 필드는 null이 되어도 지역 변수는 기존 Mat을 정리할 때 사용 |

노란 줄은 일반적으로 **이제 실행할 줄**이다. 멈춘 줄을 이미 실행했다고 생각하면 값이 한 단계씩 어긋난다. 조사식에는 Exchange나 Dispose 같은 상태를 바꾸는 호출을 넣지 않는다. _image가 null이 된 뒤 _image.Width처럼 읽으면 조사식 평가 오류가 나므로 null 여부를 본다.

예상 관찰:

| 멈춘 위치 | _disposed | _image == null |
|---|---|---|
| 첫 번째 조건 줄, 실행 전 | 0 | False |
| 첫 번째 Mat 교환 줄, 실행 후 | 1 | True |
| 두 번째 조건 줄, 실행 전 | 1 | True |

이 표는 앞서 실행 검증한 코드의 동작에 따른 예상이다. 이번 VS2022 조사식 결과는 사용자 관찰을 받아 확인한다. 끝나면 F9로 중단점을 해제한다.

**질문 1개:** 두 번째 Dispose의 조건 줄에서 멈췄을 때 `_disposed`와 `_image == null`은 각각 무엇이고, F10을 누르면 어느 줄로 갔는가? 직접 본 값과 줄을 답한다.

<details><summary>답</summary>_disposed는 1, _image == null은 True다. Exchange가 이전 값 1을 반환하여 조건이 참이므로 return 줄로 간다. 실제 정리 본문은 다시 실행하지 않는다.</details>

> **학습 확인:** 사용자가 두 번째 호출의 조사식에서 `_disposed: 1`, `_image == null: true`를 직접 확인했다고 답했다(✅). 조건 실행 시 이전 값 1을 반환하여 return으로 간다는 흐름을 보충했다. F10 뒤 실제 이동 줄은 사용자 답에 포함되지 않았으므로 별도로 관찰했다고 기록하지 않는다.

### 마무리 실습 · 3번째 타이핑은 뼈대 채우기

원래 답을 가리고 의도에서 키워드로 바꿔 쓰는 연습이다. 파일 전체를 지우지 않는다. Katas/Day02/KataDay02.cs의 LunchBox 필드·Dispose 내용, KataDay02.Run 내용만 비운 뒤 다음 뼈대를 채운다. IPowerSource, IHasTimer, Fan, HairDryer와 기존 ShowName/TrySetTimer는 유지한다. using System과 System.Threading도 유지한다. MiniSight 파일은 바꾸지 않는다.

```csharp
public sealed class LunchBox : IDisposable
{
    // [private int] 정리 표시 _disposed 필드 만들기 (처음 0)

    public void Dispose()
    {
        // [Interlocked.Exchange + ref] 표시를 1로 바꾸고 이전 값을 previous에 받기

        // [if + return] 이전 값이 0이 아니면 바로 끝내기 (중괄호 필수)

        // [Console.WriteLine] "설거지 완료 (딱 한 번)" 출력
    }
}
```

아래 메서드는 기존 KataDay02 클래스 안의 Run을 채울 때 쓴다.

```csharp
public static void Run()
{
    // [IDisposable + new LunchBox] 정리 계약 타입의 변수 box로 새 상자 받기

    // [Console.WriteLine] "1) Dispose 호출" 출력
    // [Dispose] box의 정리 메서드 호출

    // [Console.WriteLine] "2) Dispose 호출" 출력
    // [Dispose] 같은 box의 정리 메서드 다시 호출

    // [Console.WriteLine] "끝" 출력
}
```

막히면 먼저 키워드 표를 보고, 그래도 안 되면 원래 코드의 필요한 한 줄만 본다. 본 줄은 이후 PROGRESS에 적는다. 완료 후에는 Katas를 시작 프로젝트로, Debug/x64로 선택하고 Program.cs의 Day02.KataDay02.Run 호출을 확인해 Ctrl+F5로 실행한다.

**기대 출력:** 현재 로컬 Kata를 임시 복사하고 Run의 변수 선언을 IDisposable로 바꾼 구현으로 C# 7.3 / x64 / .NET Framework 4.7.2 참조를 지정해 컴파일·실행 확인했다. 이는 제공할 연습 정답의 확인이며 사용자의 뼈대 작성 결과가 아니다. 사용자 파일은 직접 수정하지 않았다.

```text
1) Dispose 호출
설거지 완료 (딱 한 번)
2) Dispose 호출
끝
```

**제출:** 실행 결과, 원래 코드를 본 줄(없으면 "없음")을 알려 준다. 회사 PC에 작성한 코드가 있으면 직접 읽어 리뷰하며, 집 PC라면 수정한 부분을 붙여 준다. 이 결과와 마지막 셀프 체크를 확인하기 전에는 Day02를 완료 처리하지 않는다.

## 3. OmniSight 지도

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 장치 상태 | `OmniSight/Devices/DeviceContracts.cs:8-17` | Created → … → Disposed 7단계 |
| IDevice | `OmniSight/Devices/DeviceContracts.cs:19-26` | `IDisposable` 상속, Initialize/Start/Stop |
| IImageSourceDevice | `OmniSight/Devices/DeviceContracts.cs:31-38` | 미리보기 이벤트 + 검사용 `WaitFrameAsync` |
| 인터페이스 분리 | `OmniSight/Devices/DeviceContracts.cs:40-51` | ICameraDevice(표식), ICameraPreviewDevice |
| ImageFrame 소유권 | `OmniSight/Devices/DeviceContracts.cs:53-102` | `_owner`, `Interlocked.Exchange`로 한 번만 Dispose |
| 검사 계약들 | `OmniSight/Application/Inspection/InspectionContracts.cs:10-80` | IImagePreprocessor, IRuleInspectionEngine, IAiInspectionEngine, IInspectionResultReporter, IInspectionImageStore |
| 파이프라인은 인터페이스만 앎 | `OmniSight/Application/Inspection/InspectionPipeline.cs:23-27,44-52` | 생성자 주입 + null 검사 |

**집에서 Claude에게:**
```text
OmniSight/Devices/DeviceContracts.cs 전체와 InspectionPipeline.cs 23~52줄을 보여줘.
InspectionPipeline이 Basler 카메라인지 공유폴더인지 모르는데도 동작하는 이유를 콘센트 비유로 설명해줘.
그리고 ICameraDevice가 멤버 없이 비어 있는 이유도.
```

## 4. 왜 이렇게 썼나

### 4.0 2회차 마무리 비교 · 내가 친 코드의 회사 버전

실습 조각 1~8 뒤에 읽는다. 회사 코드를 새로 타이핑하지 않는다. 2026-11-04 현재 파일을 읽어 심볼·사용 위치를 확인했다. 회사 전체 프로그램 빌드나 장비 실행은 하지 않았다.

조각 2에서 사용한 계약:

```csharp
private static void ShowName(IPowerSource device)
{
    Console.WriteLine("연결한 기기: " + device.Name);
}
```

회사 InspectionPipeline.cs의 해당 부분(서로 떨어진 멤버 두 개를 발췌):

```csharp
private readonly IImageSourceDevice _source;
public string SourceName => _source.Name;
```

SourceName 속성은 길게 쓰면 `public string SourceName { get { return _source.Name; } }`과 같다. readonly 필드는 생성자 등 허용된 초기화 위치에서 정하고 다른 객체로 바꿔 대입하지 못하도록 한다. 객체 내부 상태까지 불변으로 만드는 것은 아니다.

ShowName이 Fan/HairDryer 대신 IPowerSource로 Name을 읽었듯, InspectionPipeline은 특정 카메라 클래스 대신 IImageSourceDevice로 Name과 영상 입력 계약을 사용한다. 생성자에서 실제 구현체를 받아 _source에 보관한다. 영상 입력은 Worker의 `_source.WaitFrameAsync(...)` 호출로 사용한다. 새 구현이 기존 계약을 지킨다면 해당 이름 읽기·영상 받기 호출부는 유지할 수 있다. 새 구현 클래스·팩토리·설정 조립은 별도로 필요할 수 있다.

MiniSight Contracts.cs에는 다음 작은 영상 입력 계약이 이미 있다(읽기용).

```csharp
public interface IImageSource : IDevice
{
    event EventHandler<ImageFrame> FrameCaptured;
    Task<ImageFrame> WaitFrameAsync(CancellationToken cancellationToken);
}
```

| 표현 | 역할 | 기존 실습과 연결 |
|---|---|---|
| `IImageSource : IDevice` | 영상 입력 장치는 기본 장치 계약도 포함 | 조각 6의 인터페이스 계약 상속 |
| `event EventHandler<ImageFrame>` | 사진 상자를 전달하는 알림 계약 | B14의 번호 알림에서 전달값이 ImageFrame으로 바뀜 |
| `Task<ImageFrame>` | 나중에 받을 사진 한 장의 결과를 담는 Task | B11의 Task<T>. 대기는 Day11~12에서 자세히 구현 |
| `=>` 속성 | 읽기 getter의 짧은 작성법 | 위 SourceName의 긴 코드와 같은 뜻 |

미리보기·검사용 프레임 전달 시점과 소유권은 각각의 구현 계약을 확인해야 한다. 이 짧은 선언만으로 모든 이벤트가 복사본을 넘긴다고 단정하지 않는다.

| 원칙 | 이번에 직접 친 예 | 회사 코드에서 확인한 예 |
|---|---|---|
| DIP(의존 역전) | ShowName은 IPowerSource를 받음 | InspectionPipeline은 IImageSourceDevice와 검사 부품 인터페이스를 받음 |
| ISP(인터페이스 분리) | 타이머만 IHasTimer로 분리 | 미리보기 기능이 ICameraPreviewDevice에 따로 있음 |
| SRP(단일 책임) | LunchBox는 정리, ImageFrame은 프레임 데이터·이미지 수명 관리 | Pipeline이 전처리·룰·AI·배포 부품을 호출하고 실제 작업은 각각의 부품에 맡김 |

SRP는 "메서드 하나"가 아니라 관련 책임의 묶음과 바뀌는 이유를 구분하는 기준이다. 이름만 인터페이스로 바꾼다고 모든 설계 문제가 자동으로 해결되는 것도 아니다.

**질문 1개:** 기존 계약을 지키는 공유폴더 입력으로 교체할 때, 위 `_source.Name` 속성 읽기 코드를 공유폴더 전용으로 바꿔야 할까? 이유를 한 줄로 답한다.

<details><summary>답</summary>바꾸지 않아도 된다. 공유폴더 입력도 IImageSourceDevice 계약의 Name을 제공한다면 같은 코드로 읽는다. 구체적인 입력 객체를 만드는 조립 부분은 별도로 교체한다.</details>

> **학습 확인:** 사용자가 "아니, 그냥 써도 된다"고 답했다(✅). 공유폴더 입력도 같은 계약의 Name을 제공하므로 이름 읽기 코드는 유지하며, 실제 입력 객체를 만드는 조립 부분에서 교체한다는 이유를 보충했다. Day02 전체 완료는 아직 표시하지 않았다.

### 4.1 SOLID를 OmniSight에서 찾기

| 원칙 | 한 줄 뜻 | OmniSight 예 |
|---|---|---|
| **S** 단일 책임 | 바뀌는 이유는 하나 | `InspectionPipeline`은 순서만 관리. 전처리/룰/AI/저장은 각자 클래스 |
| **O** 개방-폐쇄 | 확장엔 열리고 수정엔 닫힘 | 새 카메라 = 새 어댑터 + 팩토리 한 줄. 파이프라인은 수정 안 함 |
| **L** 리스코프 치환 | 자식은 부모 자리에 그대로 | 어떤 `IImageSourceDevice`든 `WaitFrameAsync`는 "프레임 또는 null(완료)" 약속을 지켜야 함 |
| **I** 인터페이스 분리 | 안 쓰는 메서드에 의존 안 함 | 미리보기 기능은 `ICameraPreviewDevice`로 분리. 공유폴더는 미리보기 없음 |
| **D** 의존 역전 | 구체 말고 추상에 의존 | 파이프라인 생성자가 인터페이스 5개를 받음 |

### 4.2 이벤트와 WaitFrameAsync를 왜 둘 다 두었나

- `FrameCaptured` 이벤트: **화면 미리보기용**. 여러 구독자가 볼 수 있지만, **핸들러 안에서만 유효**하다(핸들러가 끝나면 상자가 큐로 넘어감). 오래 붙잡으면 안 되고 필요하면 복사(Clone)한다.
- `WaitFrameAsync`: **검사용**. 한 명(파이프라인)만 받고, **받은 사람이 Dispose할 책임**이 생긴다.
- 두 용도를 하나로 합치면 "누가 설거지하나?"가 모호해진다. 그래서 갈랐다.

### 4.3 ImageFrame.Dispose가 Interlocked를 쓰는 이유

`Dispose()`가 정지 스레드와 작업자 스레드에서 **동시에** 불릴 수 있다. `if (_disposed) return; _disposed = true;`는 두 스레드가 동시에 if를 통과할 수 있다. `Interlocked.Exchange(ref _disposed, 1)`은 "바꾸면서 이전 값을 돌려주기"를 **한 번에** 하므로 딱 한 명만 0을 받는다. (Day09에서 깊게.)

### 4.4 약점

- `ImageFrame.Data`는 그냥 `IntPtr`이라 Dispose 후에도 다른 곳에 복사된 포인터 값은 살아 있다. 규칙(소유권 문서)으로 지키는 구조다.
- `ICameraDevice`처럼 **빈 표식 인터페이스(marker)**는 의미가 약해질 수 있다. "카메라 종류인지"를 `is`로 확인할 때만 쓴다.

## 5. Kata: 콘센트와 플러그 (DIP + ISP)

```csharp
// KATA: Day02
using System;
using System.Collections.Generic;

namespace Katas.Day02
{
    public interface IPowerSource { string Name { get; } int Watt { get; } }
    public interface IHasTimer { void SetTimer(int minutes); }   // ISP: 타이머 있는 기기만

    public sealed class Fan : IPowerSource, IHasTimer
    {
        public string Name { get { return "선풍기"; } }
        public int Watt { get { return 40; } }
        public void SetTimer(int minutes) { Console.WriteLine(Name + " 타이머 " + minutes + "분"); }
    }

    public sealed class HairDryer : IPowerSource
    {
        public string Name { get { return "드라이기"; } }
        public int Watt { get { return 1500; } }
    }

    // DIP: 멀티탭은 구체 클래스를 모르고 약속(IPowerSource)만 안다
    public sealed class PowerStrip
    {
        private readonly List<IPowerSource> _plugged = new List<IPowerSource>();
        private readonly int _maxWatt;
        public PowerStrip(int maxWatt) { _maxWatt = maxWatt; }

        public bool Plug(IPowerSource device)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));
            int total = device.Watt;
            foreach (IPowerSource d in _plugged) total += d.Watt;
            if (total > _maxWatt) { Console.WriteLine(device.Name + " 과부하! 거부"); return false; }
            _plugged.Add(device);
            Console.WriteLine(device.Name + " 연결 (합계 " + total + "W)");
            return true;
        }

        public void SetAllTimers(int minutes)
        {
            foreach (IPowerSource d in _plugged)
            {
                var timer = d as IHasTimer;          // 기능이 있는 기기만 골라 쓴다
                if (timer != null) timer.SetTimer(minutes);
            }
        }
    }

    // 소유권 연습: 두 번 Dispose해도 안전한 상자
    public sealed class LunchBox : IDisposable
    {
        private int _disposed;
        public void Dispose()
        {
            if (System.Threading.Interlocked.Exchange(ref _disposed, 1) != 0) return;
            Console.WriteLine("설거지 완료 (딱 한 번)");
        }
    }

    public static class KataDay02
    {
        public static void Run()
        {
            var strip = new PowerStrip(1600);
            strip.Plug(new Fan());
            strip.Plug(new HairDryer());   // 40 + 1500 = 1540 OK
            strip.Plug(new Fan());         // 1580 OK
            strip.Plug(new HairDryer());   // 과부하
            strip.SetAllTimers(30);

            var box = new LunchBox();
            box.Dispose();
            box.Dispose();                  // 아무 일 없음
        }
    }
}
```

생각해 볼 것: `PowerStrip`에 `if (device is Fan)` 같은 분기를 넣고 싶어지면 그건 DIP 위반 신호다.

## 6. MiniSight 오늘 단계

1. NuGet: `OpenCvSharp4`, `OpenCvSharp4.runtime.win`, `OpenCvSharp4.Extensions` 설치 (MiniSight 프로젝트).
2. `Devices/Contracts.cs`에 [설계서 4장](../01-MiniSight-Spec.md) 코드를 타이핑.
3. 같은 파일에 `ImageFrame`을 직접 작성. 요구사항:
   - 생성자: `ImageFrame(long frameId, string sourceName, Mat image)` — **image의 소유권을 넘겨받는다**.
   - 속성: `FrameId`, `SourceName`, `Timestamp`(DateTime.Now), `Image`(Mat, Dispose 후 null).
   - `Dispose()`는 `Interlocked.Exchange`로 한 번만, `Image`를 Dispose하고 null로.
4. 콘솔 확인 코드(Katas 프로젝트에서 MiniSight 참조 또는 임시로 Form1_Load):

```csharp
var frame = new ImageFrame(1, "Cam1", new Mat(100, 200, MatType.CV_8UC1, Scalar.All(0)));
Console.WriteLine(frame.Image.Width);   // 200
frame.Dispose();
frame.Dispose();                         // 예외 없어야 함
Console.WriteLine(frame.Image == null);  // True
```

## 7. VS2022 디버깅 실습

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | `frame.Dispose()` 첫 줄에 중단점 | |
| 2 | 디버그 → 창 → **지역(Locals)**, **자동(Autos)** | Autos는 "지금 줄 근처 변수"만, Locals는 현재 메서드 전부 |
| 3 | **조사식(Watch) 1**에 `frame.Image.IsDisposed`, `frame.Image.Width` 입력 | Dispose 전/후 값 변화 |
| 4 | Dispose 안에서 `_disposed` 값을 Watch로 보며 F10 | 0 → 1 |
| 5 | 두 번째 Dispose 호출에서 F11 → `return`으로 바로 빠지는지 | |
| 6 | Watch에서 `frame.Image` 오른쪽 돋보기 → **Text 시각화 도우미** | (Mat은 시각화가 없다. Day15에서 Cv2.ImWrite 요령) |

## 8. 셀프 체크

### 8-1. 2회차 마무리 확인

뼈대 후 로컬 KataDay02.cs를 직접 확인했다. LunchBox의 Exchange/조건/return 및 두 번의 호출은 올바르다. 사용자 파일의 임시 복사본에 진입점만 추가하여 C# 7.3 / x64 / .NET Framework 4.7.2 참조로 컴파일·실행했고, "1) Dispose 호출 → 설거지 완료 (딱 한 번) → 2) Dispose 호출 → 끝" 출력을 확인했다. 원본 파일은 직접 수정하지 않았다.

호출부는 `LunchBox lunchBox = new LunchBox();`다. 동작은 맞지만 뼈대의 "IDisposable 계약 타입으로 받기" 요구는 아직 반영되지 않았다. 힌트: 변수 이름과 오른쪽 생성식은 유지하고 **왼쪽 선언 타입만 정리 계약으로 바꾼다**. 원래 코드를 본 줄 여부는 아직 사용자에게서 받지 않았으므로 "없음"으로 기록하지 않는다. 완전한 뼈대 무참조 통과를 아직 확정하지 않는다.

아래 다섯 질문은 2회차 실습에서 사용한 계약·소유권·정리에 맞춰 답한다. 각 한두 문장, 모르면 "모름"으로 답한다.

1. 이름을 읽는 메서드가 Fan 대신 IPowerSource를 받으면 무엇이 좋아지는가?
<details><summary>답</summary>계약을 지키는 HairDryer나 Heater를 전달해도 이름 읽기 코드는 유지할 수 있다. 특정 기기 대신 계약에 의존하므로 다른 구현으로 교체하기 쉽다.</details>

2. ImageFrame이 Mat의 소유권을 넘겨받았다. 호출자가 그 Mat을 먼저 Dispose해도 되는가? 이유는?
<details><summary>답</summary>안 된다. Frame이 보관한 것과 같은 Mat 객체가 정리되어 Frame이 더 이상 안전하게 사용할 수 없다. 소유권을 받은 Frame이 사용 완료 후 정리하도록 한다.</details>

3. 타이머 기능을 IPowerSource에 넣으면 HairDryer에는 어떤 불편이 생기는가?
<details><summary>답</summary>필요 없는 SetTimer까지 구현해야 한다. 기능별 IHasTimer로 나누면 해당 기능이 있는 기기만 구현할 수 있다(ISP).</details>

4. 정리 여부를 일반 if로 확인하고 따로 1을 대입하는 것보다 Interlocked.Exchange를 쓰는 이유는?
<details><summary>답</summary>일반 검사와 대입 사이에 다른 스레드가 끼면 둘 다 정리에 들어갈 수 있다. Exchange는 교환과 이전 값 반환을 원자적으로 처리해 하나의 호출만 이전 값 0을 받게 한다.</details>

5. borrowed = null과 original.Dispose()는 각각 무엇을 하는가?
<details><summary>답</summary>null 대입은 borrowed 변수의 참조만 끊는다. Dispose는 이 예제의 스트림을 닫아 사용할 수 없게 한다. 다른 참조가 null로 바뀌는 것도 아니고, null 대입으로 자원이 정리되는 것도 아니다.</details>

#### 2회차 셀프 체크 1차 답변과 보충

| 번호 | 사용자 답의 핵심 | 판정과 보충 |
|---|---|---|
| 1 | IPowerSource를 구현한 클래스들을 공통 계약으로 사용할 수 있음 | △ 방향은 맞음. 소비자인 ShowName 코드를 수정하지 않고 구현을 교체할 수 있다는 이점을 보충 |
| 2 | 소유권이 넘어가 가리키고 있으므로 호출자가 먼저 Dispose해도 됨 | ❌ 같은 Mat을 가리키므로 호출자가 먼저 정리하면 Frame도 그 Mat을 사용할 수 없음. Frame에 정리 책임을 맡기는 계약이지 이미지 복사가 아님 |
| 3 | HairDryer는 필요 없는 SetTimer도 구현해야 함 | ✅ ISP 핵심 설명 |
| 4 | if의 동시 접근으로 null 객체를 Dispose하다 오류가 날 수 있음 | △ 동시 접근 위험은 짚음. 핵심은 검사와 대입 사이에 끼어들어 둘 다 미정리 상태로 판단하고 정리 본문을 중복 실행하는 것. null 예외가 반드시 나는 것은 아님 |
| 5 | borrowed는 원본 참조를 끊고 Dispose는 원본을 해제 | ✅ 핵심 구별 맞음. 정확히는 borrowed 변수의 참조만 끊으며 객체 자체를 끊는 것은 아님. MemoryStream.Dispose는 스트림을 닫고, 관리 메모리 즉시 해제를 보장하지 않음 |

2번 보충: 생성자의 `_image = image;`는 같은 Mat의 참조를 보관한다. 호출자의 image와 Frame 내부 _image는 모두 같은 객체를 본다. 호출자가 먼저 image.Dispose를 하면 Frame이 쓸 자원도 정리된다. 정리 책임을 받은 Frame이 사용 완료 후 정리해야 한다. 코드가 소유권 계약 위반을 자동으로 막는 것은 아니다.

4번 보충: 일반 검사/대입이면 A가 0 확인 → B도 0 확인 → A와 B가 모두 1 대입 → 둘 다 정리할 수 있다. Exchange는 교환과 이전 값 반환을 원자적으로 수행하여 한 호출만 0을 받게 한다.

**짧은 재확인:** Frame이 아직 이미지를 사용해야 하는데 호출자가 image.Dispose를 먼저 부르면, Frame이 가리키는 Mat은 사용 가능한 상태일까, 이미 정리된 상태일까? 이유도 한 줄로 답한다.

<details><summary>답</summary>이미 정리된 상태다. 호출자의 image와 Frame의 _image가 같은 Mat 객체를 가리키기 때문이다.</details>

**소유권 재확인 결과(2026-11-04):** 사용자는 "이미 정리된 상태다. Frame이 가리키는 image와 호출자가 Dispose한 image는 같은 것을 가리킨다"고 답했다(✅). 같은 객체 참조와 정리 책임의 관계를 자기 말로 설명했다.

**Day02 완료 판정:** Kata 코드 실행, 실제 Mat 생성·정리 실험, 사용자 조사식 관찰, 계약 교체·ISP 질문, 소유권 재확인을 합쳐 약 70% 기준으로 완료 처리한다. 일반 검사/대입 사이의 경쟁은 Day03·Day09에서 다시 확인한다. 뼈대의 IDisposable 선언 타입 변경과 본 줄 여부는 아직 확인하지 못했으며, 이를 모두 완료했다거나 무참조로 작성했다고 기록하지 않는다. 코드 동작을 막는 오류가 아닌 추가 복습 항목으로 남긴다.

**완료 시 빌드 확인:** 기존 MiniSight 프로젝트는 Debug/x64, Katas는 기존 Debug/AnyCPU 구성에서 PlatformTarget=x64를 지정하고 둘 다 LangVersion=7.3으로 MSBuild 성공. Katas에는 Debug/x64 구성 자체가 없어 그 이름으로 직접 빌드하는 첫 시도는 실패했으며, 프로젝트 설정을 수정하지 않고 기존 구성에 x64 출력 대상을 명시해 재검증했다. 최종 WinForms UI 클릭 및 실제 장비 연동은 별도 수행하지 않았다.

### 8-2. 기존 교재의 확장 질문

1. `InspectionPipeline`이 구체 카메라 클래스를 모르면 무엇이 좋아지나? (2가지)
<details><summary>답</summary>① 새 카메라/입력 방식을 추가해도 파이프라인을 수정하지 않는다(개방-폐쇄). ② 가짜 입력(시뮬레이션, 테스트 더블)을 넣어 하드웨어 없이 테스트할 수 있다.</details>

2. `FrameCaptured` 이벤트로 받은 프레임을 다른 스레드에 넘겨 나중에 쓰면 어떤 문제가 생기나?
<details><summary>답</summary>이벤트가 끝나면 그 프레임은 큐로 넘어가 파이프라인이 처리 후 Dispose한다. 나중에 접근하면 이미 해제된 메모리(또는 다른 프레임으로 재사용된 SDK 버퍼)를 읽게 된다. 오래 써야 하면 핸들러 안에서 Clone한다.</details>

3. ISP를 어긴 인터페이스의 증상은?
<details><summary>답</summary>구현 클래스가 쓸모없는 메서드를 `throw new NotSupportedException()`으로 채우거나 빈 몸통으로 둔다. 호출하는 쪽은 그 메서드가 동작하는지 믿을 수 없다.</details>

4. `if (_disposed) return; _disposed = true;`가 멀티스레드에서 위험한 이유는?
<details><summary>답</summary>검사와 대입 사이에 다른 스레드가 끼어들 수 있다. 두 스레드가 동시에 false를 읽으면 둘 다 해제 코드를 실행해 이중 해제가 된다. 원자적 연산(Interlocked.Exchange)이나 lock이 필요하다.</details>

5. 생성자에서 `?? throw new ArgumentNullException(...)`을 하는 이유는?
<details><summary>답</summary>잘못된 조립을 **만드는 순간** 발견하기 위해서다. 안 하면 운전 중 첫 프레임에서 NullReferenceException이 나고 원인 추적이 어렵다(Fail fast).</details>
