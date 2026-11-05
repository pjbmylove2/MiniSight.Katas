# Day 03 (수, 2h) · Template Method와 장치 상태

## 1. 오늘 목표

- **완료 기준**
  - Template Method를 "순서는 부모가, 내용은 자식이"라고 설명하고 OmniSight에서 3곳 이상 찾을 수 있다.
  - `DeviceBase`의 `Initialize/Start/Stop/Dispose`가 상태를 어떻게 바꾸는지 **상태 그림**으로 그렸다.
  - MiniSight `DeviceBase`를 작성하고, 일부러 `OnStart`에서 예외를 던져 `Faulted`가 되는 것을 디버거로 봤다.
- **최소분량**: 2, 3, 4, 6
- **확장분량**: Kata 확장 과제, 조건부 중단점/Tracepoint

## 2. 🧒 초1 비유

**라면 끓이기 순서표**를 엄마(부모 클래스)가 정했다.

1. 물 끓이기 → 2. **면 넣기** → 3. **스프 넣기** → 4. 불 끄기

신라면이든 짜파게티든(자식 클래스) 1번과 4번은 똑같다. 2, 3번만 자기 방식으로 한다. 순서를 자식이 바꾸면 안 된다(불 끄고 면 넣으면 안 되니까). 그래서 순서표 자체는 **고칠 수 없게(non-virtual)** 만들고, 2·3번 칸만 비워 둔다(`abstract`/`virtual`).

OmniSight의 `DeviceBase`가 바로 이 순서표다. "상태 확인 → 자식 작업(OnStart) → 상태 바꾸기, 실패하면 Faulted" 순서를 모든 장치가 똑같이 따른다.

## 2-1. 2회차 조각별 실습 (2026-11-04 시작)

코드부터 실행하고 한 조각씩 진행한다. 진행 계획은 ①순서표와 빈칸 ②상태 표시 ③실패와 다시 던지기 ④실패해도 정리하기 ⑤MiniSight 장치 초기화·시작·정지 ⑥시작 실패와 Faulted ⑦정리 순서 ⑧디버깅·뼈대 복습·셀프 체크다. 실제로 진행한 내용과 중간 Q&A를 조각마다 기록한다. 아래 기존 지도·Kata는 비교 참고 자료이며 회사 코드는 손코딩 뒤에 읽는다.

### 조각 1 · 순서는 부모가, 스프는 자식이

**지금 만드는 것:** 부모 클래스가 조리 순서를 정하고 자식 클래스는 스프 넣는 부분만 구현하는 콘솔 예제.

비유: 부모가 "물 끓이기 → 스프 넣기 → 불 끄기" 순서표를 정한다. 어떤 스프를 넣을지만 자식이 채운다. 이 구조를 Template Method라고 부른다. 새로운 문법이 아니라 B02의 abstract/override를 배치하는 방법이다.

Katas 프로젝트에 Day03 폴더와 KataDay03.cs를 만들고 다음 코드를 손으로 친다. MiniSight의 기존 DeviceBase는 아직 수정하지 않는다.

```csharp
using System;

namespace Katas.Day03
{
    public abstract class RamenRecipe
    {
        public void Cook()
        {
            Console.WriteLine("물 끓이기");
            AddSoup();
            Console.WriteLine("불 끄기");
        }

        protected abstract void AddSoup();
    }

    public sealed class RedRamen : RamenRecipe
    {
        protected override void AddSoup()
        {
            Console.WriteLine("빨간 스프 넣기");
        }
    }

    public static class KataDay03
    {
        public static void Run()
        {
            RamenRecipe recipe = new RedRamen();
            recipe.Cook();
        }
    }
}
```

| 키워드·기호 | 역할 / 사용법 | 풀어서 읽기 |
|---|---|---|
| `abstract class` | 미완성 부분을 자식이 채우는 부모 클래스 | RamenRecipe 자체는 new할 수 없음 |
| `protected abstract void AddSoup();` | 부모·자식에서 접근하는 미구현 메서드 계약 | 본문 없이 세미콜론으로 끝냄. 완성된 자식이 구현해야 함 |
| `RedRamen : RamenRecipe` | 클래스 상속 | RedRamen은 부모 Cook 메서드를 물려받음 |
| `protected override void AddSoup()` | 부모의 빈칸 구현 | Cook에서 호출하면 실제 RedRamen 구현이 실행됨 |
| `sealed` | 이 클래스로부터 추가 상속 금지 | RedRamen을 부모로 하는 자식은 만들 수 없음 |
| `public void Cook()` | 외부에서 실행할 순서표 | virtual이 아니므로 자식이 override할 수 없음 |
| `RamenRecipe recipe = new RedRamen();` | 부모 타입 변수로 실제 자식 객체 사용 | RedRamen ramen = new RedRamen(); RamenRecipe recipe = ramen; |

인터페이스는 계약을 선언했던 반면, 이 abstract 부모는 Cook의 공통 실행 코드까지 제공한다. 모든 추상 클래스가 반드시 추상 메서드를 가져야 한다는 뜻은 아니며, 이 예제는 AddSoup가 추상 메서드인 경우다. protected 멤버를 Main/Run 같은 외부 호출부에서 직접 호출하지 않는다.

Cook은 부모에 있고 AddSoup의 실행 내용은 자식에 있다. non-virtual은 override를 막는다는 뜻이며, 상속의 모든 숨김(new)까지 금지하는 것은 아니다. 이번 예제는 부모 타입으로 Cook을 호출하여 부모의 순서를 따른다. 현재는 정상 실행만 다룬다. AddSoup에서 예외가 발생하면 마지막 불 끄기 줄에 도달하지 못하는 문제는 뒤 조각의 finally로 해결한다.

**실험 준비:** Katas를 시작 프로젝트로 설정한다. Katas/Program.cs에서 Day02의 KataDay02.Run 호출을 주석 처리하고 `Day03.KataDay03.Run();`을 호출한다. 기존 `using Katas.Day02;`는 주석 처리해도 된다. 마지막 Console.ReadKey는 유지한다. Ctrl+F5로 실행한다. Katas의 기존 구성은 Debug/AnyCPU이고 이전 검증에서 PlatformTarget=x64를 별도 지정했다. Visual Studio에서 없는 Debug/x64 구성을 선택하라고 안내하지 않는다.

```csharp
// KataDay02.Run();
Day03.KataDay03.Run();
```

**확인된 출력:** 임시 파일에 진입점만 추가하여 Roslyn csc /langversion:7.3 /platform:x64와 .NET Framework 4.7.2 참조(mscorlib, System)를 지정해 컴파일·실행했다. 사용자 코드 파일은 직접 수정하지 않았다.

```text
물 끓이기
빨간 스프 넣기
불 끄기
```

**질문 1개:** 조리 순서는 유지하고 빨간 스프를 하얀 스프로 바꾸려면, Cook 전체를 고쳐야 할까 아니면 자식의 어느 메서드 내용만 바꾸면 될까?

<details><summary>답</summary>Cook은 유지하고 자식의 AddSoup 구현 내용만 바꾸면 된다. 공통 순서는 부모가 제공하고 바뀌는 부분은 자식이 구현한다.</details>

> **학습 확인:** 사용자는 "자식 클래스의 AddSoup만 고치면 된다"고 답했다(✅). 부모 Cook의 순서는 유지하고 자식의 단계 구현만 바꾼다는 Template Method의 핵심을 이해했다. 사용자 실행 결과는 아직 별도로 확인하지 않았다.

### 조각 2 · 조리 전·중·완료 상태 붙이기

**지금 만드는 것:** 부모 RamenRecipe가 조리 순서와 상태를 함께 관리한다. 외부 코드는 상태를 읽지만 직접 바꾸지 못한다.

비유: 라면 냄비에 "대기 / 조리 중 / 완료" 표시판을 붙인다. 조리 담당자가 실제 진행에 맞춰 바꾸고, 구경하는 사람은 읽기만 한다.

로컬 KataDay03.cs에 조각 1 코드가 작성된 것을 확인했다. namespace 안에서 RamenRecipe와 같은 높이에 상태 이름을 추가한다.

```csharp
public enum CookState
{
    Idle,
    Cooking,
    Done
}
```

기존 RamenRecipe 클래스만 다음처럼 바꾼다. RedRamen은 유지한다.

```csharp
public abstract class RamenRecipe
{
    public CookState State { get; private set; }

    public void Cook()
    {
        State = CookState.Cooking;
        Console.WriteLine("조리 중: " + State);

        Console.WriteLine("물 끓이기");
        AddSoup();
        Console.WriteLine("불 끄기");

        State = CookState.Done;
    }

    protected abstract void AddSoup();
}
```

KataDay03.Run은 다음처럼 작성한다.

```csharp
public static void Run()
{
    RamenRecipe recipe = new RedRamen();
    Console.WriteLine("시작 전: " + recipe.State);

    recipe.Cook();

    Console.WriteLine("끝난 후: " + recipe.State);
}
```

| 키워드·코드 | 역할 / 사용법 | 풀어서 읽기 |
|---|---|---|
| `enum CookState` | 정해진 상태 이름의 타입 | 숫자나 문자열을 임의로 적는 대신 이름으로 구별 |
| `Idle`, `Cooking`, `Done` | 대기, 조리 중, 완료 | 명시적 숫자가 없으므로 각각 0, 1, 2 |
| `public CookState State { get; private set; }` | 외부에서 읽고 선언 클래스 안에서만 변경 | 숨은 저장 필드와 읽기/쓰기 접근자를 컴파일러가 만듦 |
| `State = CookState.Cooking;` | 조리를 시작하면서 상태 갱신 | 상태를 바꾸는 코드는 부모 Cook 안에 둠 |

새 객체의 State는 enum 기본값 0이고, 여기서는 Idle이 0이므로 생성 직후 Idle이다. 임의의 enum에서 무조건 첫 이름이 기본 상태라고 외우지 않는다. private set이므로 Run과 자식 RedRamen에서도 직접 대입할 수 없다. 상태를 private set으로 감쌌다는 것만으로 여러 스레드의 동시 실행까지 막는 것은 아니다. 이 조각은 한 스레드의 정상 흐름만 다룬다.

**실험:** Ctrl+F5로 실행한다. C# 7.3 / x64 / .NET Framework 4.7.2 참조(mscorlib, System)로 임시 콘솔 예제를 컴파일·실행하여 다음 출력을 확인했다. 사용자 원본 파일은 수정하지 않았다.

```text
시작 전: Idle
조리 중: Cooking
물 끓이기
빨간 스프 넣기
불 끄기
끝난 후: Done
```

**바꿔 보기 1개(컴파일 에러 확인용):** Run에서 recipe를 만든 다음 줄에 `recipe.State = CookState.Done;`을 잠깐 넣고 빌드한다. private set이므로 CS0272로 거부되는 것을 확인한다. 이 대입을 넣은 임시 예제도 C# 7.3으로 컴파일해 CS0272를 확인했다. 실패한 빌드에서는 실행 결과가 나오지 않으며, 이전 실행 파일을 실행한 것을 새 코드 성공으로 해석하지 않는다. 확인 후 추가한 대입 줄을 지운다.

**질문 1개:** 상태 속성은 public인데, 왜 Run에서 State를 바꾸면 컴파일 에러가 날까? 읽기와 쓰기를 구별해 한 줄로 답한다.

<details><summary>답</summary>get은 public이라 외부에서 읽을 수 있지만 set은 private이라 RamenRecipe 내부에서만 쓸 수 있기 때문이다. 상태 변경은 부모의 Cook 흐름 안에서 관리한다.</details>

> **학습 확인:** 사용자는 "State의 set이 private이므로 쓰기는 내부에서만, 읽기는 외부에서도 가능하다"고 설명했다(✅). 여기서 내부는 State를 선언한 RamenRecipe 클래스 내부이며, 자식 RedRamen에서도 직접 대입할 수 없다는 범위를 보충했다. 사용자 실행 출력과 CS0272 실험 결과는 아직 별도로 받지 않았다.

### 조각 3 · 실패 상태를 남기고 예외 다시 전달하기

**지금 만드는 것:** 스프가 없는 라면을 실행하면 부모가 Burnt 상태를 기록하고, 호출자는 예외를 받아 실패 메시지를 출력한다.

비유: 조리 담당자는 "실패" 표시를 남기고 주문 담당자에게 실패 소식도 전달한다. 상태만 바꾸고 아무 말 없이 돌아오면 호출자는 정상 종료한 것으로 오해할 수 있다.

로컬 코드에 조각 2의 `recipe.State = CookState.Done;` 컴파일 에러 실험 줄이 아직 남아 있었다. 이번 Run으로 교체하면서 제거한다. 컴파일 에러 실험과 실행 중 예외 실험을 구분한다. Cook에는 물 끓이기 출력도 다시 포함한다. 사용자 파일은 직접 수정하지 않는다.

CookState 끝에 Burnt를 추가한다.

```csharp
public enum CookState
{
    Idle, Cooking, Done, Burnt
}
```

RamenRecipe의 Cook만 다음처럼 교체한다. State 속성과 AddSoup 선언은 유지한다.

```csharp
public void Cook()
{
    State = CookState.Cooking;
    Console.WriteLine("조리 중: " + State);

    try
    {
        Console.WriteLine("물 끓이기");
        AddSoup();
        Console.WriteLine("불 끄기");
        State = CookState.Done;
    }
    catch
    {
        State = CookState.Burnt;
        throw;
    }
}
```

RedRamen은 남겨 두고, 같은 높이에 실패하는 자식을 하나 추가한다.

```csharp
public sealed class BrokenRamen : RamenRecipe
{
    protected override void AddSoup()
    {
        throw new InvalidOperationException("스프가 없습니다.");
    }
}
```

KataDay03.Run을 다음으로 교체한다.

```csharp
public static void Run()
{
    RamenRecipe recipe = new BrokenRamen();
    Console.WriteLine("시작 전: " + recipe.State);

    try
    {
        recipe.Cook();
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine("실패: " + ex.Message);
    }

    Console.WriteLine("끝난 후: " + recipe.State);
}
```

| 키워드·코드 | 역할 / 사용법 | 풀어서 읽기 |
|---|---|---|
| `throw new InvalidOperationException(...)` | 새 예외를 만들어 던짐 | 여기서는 스프 없음이라는 실행 중 실패를 표현 |
| `try` | 실패할 수 있는 호출을 실행하는 범위 | 예외가 나면 뒤의 정상 경로는 건너뜀 |
| `catch` | 전달된 예외를 받아 처리 | 부모는 상태를 Burnt로 변경 |
| `throw;` | 현재 받은 예외를 다시 호출자에게 전달 | catch 안에서 사용. 원래 예외의 스택 추적 보존 |
| `catch (InvalidOperationException ex)` | 해당 종류의 예외를 받아 ex로 다룸 | Run은 전달받은 실패를 출력 |
| `ex.Message` | 예외의 설명 문자열 | "스프가 없습니다." |

부모 catch의 "Burnt 기록"과 "throw로 전달"은 별개의 역할이다. throw가 있으면 Cook을 호출한 Run의 catch로 전달되고, Run에서 처리했으므로 마지막 상태 출력까지 실행한다. 예외가 스스로 프로세스를 강제 종료하는 실험이 아니다.

**실험:** Ctrl+F5로 실행한다. C# 7.3 / x64 / .NET Framework 4.7.2 참조(mscorlib, System)로 임시 코드를 컴파일·실행해 다음 출력을 확인했다.

```text
시작 전: Idle
조리 중: Cooking
물 끓이기
실패: 스프가 없습니다.
끝난 후: Burnt
```

"불 끄기"와 Done 설정이 실행되지 않았다는 것도 본다. AddSoup에서 예외가 났기 때문에 정상 경로의 뒤쪽을 건너뛴다. 실패해도 불을 끄게 만드는 finally는 다음 조각에서 추가한다.

**질문 1개:** 부모 Cook의 catch에서 `throw;`만 지우면 Run의 "실패: 스프가 없습니다." 줄이 출력될까? 이유도 한 줄로 답한다.

<details><summary>답</summary>출력되지 않는다. 부모 catch가 예외를 받고 다시 던지지 않으면 Run에는 예외가 전달되지 않아 Run의 catch가 실행되지 않는다. State는 여전히 Burnt지만 호출은 정상 반환한 것처럼 보인다. throw를 제거한 임시 비교 예제도 컴파일·실행하여 실패 메시지는 없고 끝난 후 Burnt만 나오는 것을 확인했다.</details>

> **학습 확인:** 사용자는 "RamenRecipe의 catch에서 에러가 끊기므로 출력되지 않는다"고 답했다(✅). 정확히는 catch에서 받은 예외를 다시 던지지 않으면 호출자에게 전달되지 않는다는 뜻이다. 상태는 Burnt로 남지만 Run의 catch는 실행되지 않고, Cook 호출 다음의 상태 출력은 실행된다는 점을 보충했다. 사용자 실행 출력은 아직 별도로 받지 않았다.

### 조각 4 · 실패해도 불 끄기 (finally)

**지금 만드는 것:** 조리에 성공하거나 일반 예외로 빠져나가더라도 정리 단계가 실행되게 만든다.

비유: 스프가 없어서 조리에 실패해도 켜 둔 불은 꺼야 한다. 다만 불을 껐다고 조리에 성공한 것은 아니다. 성공 표시와 정리 단계는 나눠 둔다.

로컬 Kata에서 BrokenRamen, 부모 catch의 Burnt/throw, 호출자 catch가 작성된 것을 확인했다. RamenRecipe.Cook만 다음처럼 교체하고 나머지는 유지한다. 특히 try 안의 기존 "불 끄기" 줄은 제거하고 finally로 이동한다. 같은 출력을 두 군데에 남겨 두지 않는다.

```csharp
public void Cook()
{
    State = CookState.Cooking;
    Console.WriteLine("조리 중: " + State);

    try
    {
        Console.WriteLine("물 끓이기");
        AddSoup();
        State = CookState.Done;
    }
    catch
    {
        State = CookState.Burnt;
        throw;
    }
    finally
    {
        Console.WriteLine("불 끄기");
    }
}
```

| 키워드·코드 | 역할 / 사용법 | 풀어서 읽기 |
|---|---|---|
| `try` | 조리와 성공 상태 설정 | AddSoup가 성공해야 Done 설정까지 도달 |
| `catch` | 실패 상태 기록 후 예외 전달 | Burnt 설정과 throw를 유지 |
| `finally` | try/catch를 떠나면서 정리 실행 | 이번 예외 재전달 경로에서도 호출자 catch에 도달하기 전에 실행 |
| `State = CookState.Done;` | 조리가 성공했다는 기록 | 실패 경로에서도 실행되는 finally에 넣지 않음 |

실패 시 흐름은 AddSoup throw → Cook catch에서 Burnt/재던지기 → Cook finally의 불 끄기 → Run catch의 실패 메시지다. finally가 실행되어도 예외가 사라지는 것은 아니다. 이 예제의 불 끄기는 출력으로 정리를 모의한다.

**확인된 출력:** 로컬 사용자 코드의 임시 복사본에서 불 끄기 줄을 finally로 옮겨 C# 7.3 / x64 / .NET Framework 4.7.2 참조(mscorlib, System)로 컴파일·실행했다. BrokenRamen과 RedRamen 두 경로를 모두 확인했다. 사용자 원본은 수정하지 않았다.

```text
시작 전: Idle
조리 중: Cooking
물 끓이기
불 끄기
실패: 스프가 없습니다.
끝난 후: Burnt
```

**실험:** Ctrl+F5로 실패 경로를 실행한다. 이전 조각에 없던 "불 끄기"가 실패 메시지보다 먼저 나오는지 본다.

**바꿔 보기 1개:** Run에서 `new BrokenRamen()`을 잠깐 `new RedRamen()`으로 바꿔 실행하고 다시 되돌린다. 성공 경로에서 확인한 출력은 다음과 같다.

```text
시작 전: Idle
조리 중: Cooking
물 끓이기
빨간 스프 넣기
불 끄기
끝난 후: Done
```

Day01 복습: 일반적인 블록 종료·return·예외 전파와 프로세스 강제 종료는 구별한다. Environment.Exit(0 또는 1), 프로세스 강제 종료 같은 경우 finally 실행을 보장하지 않는다. 따라서 "모든 상황에서 무조건 실행"이라고 외우지 않는다.

**질문 1개:** 왜 "불 끄기"는 finally로 옮기면서 `State = CookState.Done;`은 try 안에 남겨 두었을까? 실패하는 BrokenRamen을 기준으로 답한다.

<details><summary>답</summary>정리는 성공·실패 모두 해야 하지만 Done은 조리 성공 때만 기록해야 하기 때문이다. Done을 finally에 넣으면 catch에서 Burnt로 바꿔도 다시 Done으로 덮여 실패를 성공으로 표시하게 된다.</details>

> **학습 확인:** 사용자는 "에러 상태 Burnt가 따로 있는데 Done을 finally에 넣으면 에러여도 Done이 된다"고 답했다(✅). 정리는 성공·실패 경로에서 수행하지만 성공 상태 기록은 성공 경로에만 둬야 한다는 이유를 정확히 설명했다. 질문 통과와 사용자 실행 결과 확인은 구별한다.

> **작성 중 코드 확인:** 실험을 준비하는 동안 로컬 파일에 finally가 추가되고 Done 설정도 finally 안으로 이동한 것을 확인했다. 사용자 파일은 수정하지 않고, Done은 정상 경로 try 안에 두어야 한다는 힌트를 안내했다. 임시 조립 첫 시도는 이미 작성 중이던 finally와 중복되어 CS1513이 났다. 문서의 Cook 전체를 임시 복사본에 적용한 뒤 Broken/Red 두 경로를 다시 컴파일·실행해 위 출력을 확인했다. 사용자 원본이 수정 완료됐다는 뜻은 아니며 후속 답변 때 다시 확인한다.

### 조각 5 · 실제 MiniSight의 장치 상태 흐름

**지금 만드는 것:** 기존 TestDevice 가짜 장치를 생성·초기화·시작·정지하고 각 단계 뒤의 상태를 메시지 창으로 확인한다.

비유: 라면은 조리 전 → 조리 중 → 완료였다. 장치는 만들어짐 → 연결 준비 완료 → 운전 중 → 정지다. 부모 DeviceBase가 공통 상태 순서를 관리하고 자식 TestDevice는 연결·촬영 시작 같은 단계 내용을 채운다.

현재 MiniSight에 DeviceBase와 TestDevice가 이미 있다. 이 조각에서는 새로 중복 작성하거나 실제 장비 코드를 바꾸지 않는다. 로컬 DeviceBase.Start의 OnStart 호출 뒤 Running 설정, TestDevice.OnStart의 가짜 출력 및 기본 FailOnStart=false를 확인했다.

**준비:** MiniSight를 시작 프로젝트로 설정하고 Debug/x64를 선택한다. Form1 디자이너에 button6을 추가하고 Text를 "장치 상태 확인"으로 바꾼다. 더블클릭해 만든 핸들러에 다음을 손으로 친다. Form1.cs의 기존 using MiniSight.Devices, using System.Threading, using System.Windows.Forms가 있는지도 확인한다.

```csharp
private void button6_Click(object sender, EventArgs e)
{
    using (IDevice device = new TestDevice("연습 장치"))
    {
        string states = "생성 직후: " + device.State;

        device.Initialize(CancellationToken.None);
        states += Environment.NewLine + "초기화 후: " + device.State;

        device.Start(CancellationToken.None);
        states += Environment.NewLine + "시작 후: " + device.State;

        device.Stop();
        states += Environment.NewLine + "정지 후: " + device.State;

        MessageBox.Show(states);
    }
}
```

| 키워드·코드 | 역할 / 사용법 | 풀어서 읽기 |
|---|---|---|
| `using (IDevice device = ...)` | 계약으로 장치를 사용하고 블록 끝에서 정리 | Day02에서 사용한 IDisposable 계약 상속 |
| `Initialize(...)` | 초기화 수행 | 부모가 Initializing → 자식 OnInitialize → Ready 순서를 관리 |
| `Start(...)` | 장치 시작 | 부모가 상태 검사 → 자식 OnStart → Running 순서로 수행 |
| `Stop()` | 장치 정지 | 부모가 자식 OnStop 후 Stopped 설정 |
| `CancellationToken.None` | 취소 요청이 발생하지 않는 토큰 | 이번 실험은 취소 동작을 하지 않음. 즉시 실행·비동기를 뜻하는 것은 아님 |
| `states += ...` | 문자열 변수에 뒤의 내용을 이어 붙여 다시 저장 | `states = states + ...`와 같은 뜻. 이번에는 이벤트 구독이 아님 |
| `Environment.NewLine` | 운영체제 줄바꿈 | 네 상태를 다른 줄에 표시 |

**실험:** Ctrl+F5로 실행하고 새 버튼을 누른다. 예상 메시지 본문은 다음과 같다.

```text
생성 직후: Created
초기화 후: Ready
시작 후: Running
정지 후: Stopped
```

메시지의 확인을 누른 뒤 using이 끝나면 Dispose된다. 그 이전에 메시지 문자열을 만들었으므로 메시지에 Disposed까지 찍히지는 않는다. 기존 구현에서는 이미 Stopped여도 Dispose에서 Stop을 다시 호출하므로 콘솔 정지 로그가 한 번 더 나올 수 있다.

**검증 범위:** 현재 Contracts.cs·DeviceBase.cs·TestDevice.cs를 실제 소스로 사용하고 메시지 출력만 Console로 바꾼 임시 하네스를 C# 7.3 / x64 / .NET Framework 4.7.2 참조 및 설치된 OpenCvSharp 관리 DLL로 컴파일·실행했다. 네 상태 문자열, Initializing 중간 전이, using 뒤 Disposed 전이를 확인했다. WinForms 새 버튼 추가·클릭은 직접 하지 않았고 사용자 원본 파일도 수정하지 않았다. TestDevice는 출력만 하는 가짜이며 실제 카메라/PLC와 연결하지 않는다. 실제 연결처럼 오래 걸리는 호출을 UI에서 동기 실행하는 설계는 이 가짜 실험의 범위를 벗어난다.

부모 DeviceBase.Start의 성공 경로(기존 코드 일부, 읽기용):

```csharp
OnStart(cancellationToken);
ChangeState(DeviceState.Running);
```

라면에서 AddSoup가 성공한 뒤 Done으로 바꿨던 것과 같은 배치다. 예외 처리와 실패 상태는 다음 조각에서 실제로 실행해 본다.

**질문 1개:** 왜 부모는 OnStart를 먼저 실행하고, 그다음에 Running 상태로 바꿀까? 순서를 반대로 하면 어떤 거짓 표시가 생길 수 있을까?

<details><summary>답</summary>시작 작업이 성공한 뒤에야 운전 중이라고 표시해야 하기 때문이다. 먼저 Running으로 표시하면 실제 시작이 끝나지 않았거나 실패하는 중에도 다른 코드나 화면에서 운전 중으로 볼 수 있다. 성공 상태는 성공한 다음 기록한다.</details>

> **학습 확인:** 사용자는 "먼저 Running으로 바꾸면 Start에서 에러가 나도 상태가 Running인 상황이 생긴다"고 답했다(✅, 성공 전에 운전 상태를 표시할 위험을 이해). 현재 DeviceBase.Start는 catch에서 Faulted로 바꾸므로, 순서를 반대로 가정해도 실패 후 최종 상태가 계속 Running으로 남는다고 단정하지 않는다. 문제는 실제 시작이 성공하기 전에 Running을 미리 표시하는 구간이다. OnStart 성공 후 Running 설정, 실패 시 Faulted 설정이라는 정확한 흐름을 보충했다. 사용자 버튼 실행 결과는 아직 별도로 받지 않았다.

### 조각 6 · 시작을 실패시켜 Faulted 확인하기

**지금 만드는 것:** TestDevice의 기존 FailOnStart 기능으로 시작 실패를 만들고, 버튼 호출부의 catch에서 Faulted 상태를 확인한다.

비유: 라면의 BrokenRamen은 스프 단계에서 실패했다. 이번에는 가짜 장치의 OnStart가 실패한다. 부모가 실패를 Faulted로 기록한 다음 다시 던지고, 호출자가 받아 보여 준다.

로컬 Form1에 조각 5의 button6이 작성된 것을 확인했다. MiniSight를 시작 프로젝트로, Debug/x64로 선택한다. Form1 디자이너에 button7을 추가하고 Text를 "시작 실패 확인"으로 바꾼 뒤 더블클릭해 다음 코드를 친다. 기존 using MiniSight.Devices와 using System.Threading은 유지한다.

```csharp
private void button7_Click(object sender, EventArgs e)
{
    using (TestDevice device = new TestDevice("연습 장치"))
    {
        device.Initialize(CancellationToken.None);
        device.FailOnStart = true;

        try
        {
            device.Start(CancellationToken.None);
            MessageBox.Show("시작 성공");
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message + Environment.NewLine
                + "현재 상태: " + device.State);
        }
    }
}
```

| 키워드·코드 | 역할 / 사용법 | 풀어서 읽기 |
|---|---|---|
| `using (TestDevice device = ...)` | 이번에는 가짜 클래스 고유 기능도 사용 | IDevice에는 FailOnStart가 없어서 TestDevice 타입으로 받음 |
| `device.FailOnStart = true;` | 시작 실패를 일부러 켜는 테스트 옵션 | 원래 false, true면 TestDevice.OnStart에서 예외를 던짐 |
| `try` / `catch (InvalidOperationException ex)` | 실패한 Start 호출을 가까운 곳에서 처리 | Run에서 라면 예외를 받았던 것과 같음 |
| `device.State` | 부모가 기록한 현재 상태 읽기 | catch 안에서는 Faulted |
| `CancellationToken.None` | 이번에는 취소하지 않음 | 시작 실패와 취소는 다른 실험 |

TestDevice 전용 테스트 옵션을 모든 장치의 공통 계약인 IDevice에 추가할 필요는 없다. 동작 순서는 TestDevice.OnStart에서 throw → DeviceBase.Start catch에서 Faulted/throw → 버튼 핸들러 catch에서 메시지다. Start가 예외를 던졌기 때문에 뒤의 "시작 성공" 메시지는 실행되지 않는다.

**실험:** Ctrl+F5로 실행하고 새 버튼을 누른다. 예상 메시지:

```text
연습 장치 시작 실패(테스트)
현재 상태: Faulted
```

이번 예외는 버튼 내부 catch에서 처리하므로 Day01의 전역 UI 예외 처리기까지 전파되지 않고, 그 전역 처리기의 Environment.Exit(1)도 이 예외 때문에 실행되지 않는다. 메시지를 닫으면 using이 장치를 정리한다. catch에서 이미 문자열을 만들었으므로 Faulted를 관찰할 수 있지만, using이 끝나면 이 구현은 Stop을 거쳐 Disposed로 바뀐다. using 밖에서 상태를 읽는 실험과 혼동하지 않는다.

**검증:** 실제 Contracts.cs/DeviceBase.cs/TestDevice.cs를 사용하고 메시지 출력만 Console로 바꾼 임시 하네스를 C# 7.3 / x64 / .NET Framework 4.7.2 참조와 설치된 OpenCvSharp 관리 DLL로 컴파일·실행했다. true에서 Ready→Faulted 및 위 두 줄을 확인했고, false에서는 Ready→Running과 "시작 성공"을 확인했다. 이후 using 정리로 Disposed가 되는 것도 확인했다. 사용자 파일은 수정하지 않았고, WinForms 새 버튼 클릭·실제 장비는 확인하지 않았다.

**바꿔 보기 1개:** FailOnStart를 false로 바꿔 "시작 성공" 메시지를 확인한 뒤 true로 되돌린다.

**질문 1개:** 이번에도 UI 버튼에서 예외가 났는데 왜 Day01처럼 프로그램이 종료되지 않을까? 어느 catch에서 받는지 설명한다.

<details><summary>답</summary>button7_Click 안의 catch가 예외를 받아 처리하기 때문이다. 처리된 예외는 전역 Application.ThreadException까지 올라가지 않으므로 그 전역 처리기의 Exit가 호출되지 않는다. 가짜 장치의 실패를 확인하는 실험이며 실제 장비에서의 운전 재개 정책을 의미하지는 않는다.</details>

> **학습 확인:** 사용자는 "Day01은 위로 전달된 예외를 Program.cs에서 받았고, 이번에는 button7_Click 안의 catch(InvalidOperationException)에서 받았다"고 설명했다(✅). 정확히는 Day01에서 Program.cs에 등록한 전역 UI 예외 처리기가 받아 Exit를 호출했다. 이번에는 로컬 catch가 처리하므로 그 전역 처리기까지 전달되지 않는다. 두 경우 모두 UI 스레드에서 발생한 예외이며, 차이는 처리 위치다. 사용자 버튼 실행 출력은 별도로 아직 받지 않았다.

### 조각 7 · 정지가 실패해도 정리 단계는 시도하기

**지금 만드는 것:** 가짜 장치의 OnStop에서 예외를 던지고, Dispose 안의 finally가 OnDispose까지 실행하는지 확인한다.

비유: 조리를 멈추는 단계에서 문제가 생겨도, 정리할 수 있는 도구는 정리해야 한다. 정지 성공 여부와 정리 단계 실행 여부는 서로 다른 정보다. 이번 TestDevice는 실제 장비와 자원 해제를 출력으로 모의한다.

MiniSight/Devices/TestDevice.cs에 테스트용 속성 두 개를 추가한다. 기존 FailOnStart는 유지한다. 모든 장치의 공통 IDevice 계약이나 회사 소스는 수정하지 않는다.

```csharp
public bool FailOnStop { get; set; }
public bool CleanupCalled { get; private set; }
```

기존 TestDevice의 OnStop과 OnDispose만 다음으로 교체한다.

```csharp
protected override void OnStop()
{
    if (FailOnStop)
    {
        throw new InvalidOperationException(_name + " 정지 실패(테스트)");
    }

    Console.WriteLine(" [" + _name + "] 촬영 정지");
}

protected override void OnDispose()
{
    CleanupCalled = true;
    Console.WriteLine(" [" + _name + "] 자원 해제");
}
```

MiniSight를 시작 프로젝트, Debug/x64로 선택한다. Form1 디자이너에 button8을 추가하고 Text를 "정지 실패와 정리"로 바꾼 뒤 더블클릭해 작성한다.

```csharp
private void button8_Click(object sender, EventArgs e)
{
    TestDevice device = new TestDevice("연습 장치");
    try
    {
        using (device)
        {
            device.Initialize(CancellationToken.None);
            device.Start(CancellationToken.None);
            device.FailOnStop = true;
        }
    }
    catch (InvalidOperationException ex)
    {
        MessageBox.Show(ex.Message + Environment.NewLine
            + "정리 단계 실행: " + device.CleanupCalled + Environment.NewLine
            + "최종 상태: " + device.State);
    }
}
```

| 표현 | 역할 / 사용법 | 풀어서 읽기 |
|---|---|---|
| `FailOnStop` | 가짜 정지 실패 스위치 | true면 OnStop에서 의도한 예외 발생 |
| `CleanupCalled` | OnDispose 진입 여부 기록 | private set이므로 TestDevice 내부에서만 설정 |
| `using (device)` | 이미 만든 객체를 블록 끝에서 정리 | 새 객체를 만들지 않음. using 종료가 device.Dispose를 호출 |
| using을 감싼 `try/catch` | 블록을 끝내며 Dispose에서 난 예외도 받음 | catch에서 device를 확인하려고 변수를 try 밖에 선언 |
| `State`와 `CleanupCalled` 읽기 | 이 가짜 객체에 남긴 관찰용 정보 | Dispose 후 어떤 자원 접근이든 허용된다는 일반 규칙은 아님 |

DeviceBase.Dispose는 기존 코드를 유지한다. 핵심은 바깥 finally가 Stop 실패 후에도 OnDispose를 시도하고, 안쪽 finally가 OnDispose에서 예외가 나도 정리 표시와 Disposed 상태 설정을 시도하는 것이다. 이번 시험은 OnStop 실패와 OnDispose 정상 반환 조합을 실행했다. OnDispose 실패를 별도로 주입하지는 않았다.

```csharp
try
{
    Stop();
}
finally
{
    try
    {
        OnDispose();
    }
    finally
    {
        _disposed = true;
        ChangeState(DeviceState.Disposed);
    }
}
```

흐름: using 끝 → DeviceBase.Dispose → Stop의 OnStop 실패 → Stop의 catch에서 Faulted/throw → Dispose의 바깥 finally → OnDispose에서 CleanupCalled=true → 안쪽 finally에서 Disposed → 바깥 버튼 catch에 정지 예외 전달. 정리했다고 예외가 사라지는 것은 아니다.

**실험:** Ctrl+F5로 실행하고 새 버튼을 누른다. 예상 메시지는 다음과 같다.

```text
연습 장치 정지 실패(테스트)
정리 단계 실행: True
최종 상태: Disposed
```

Disposed는 이 객체를 정리한 상태 표시이지 실제 장비가 정상 정지했다는 증명이 아니다. CleanupCalled도 이 모의 정리 코드가 실행됐다는 표시이며, 모든 외부 자원이 실제로 해제됐다는 증거는 아니다. Stop의 예외는 그대로 catch에 전달되었으므로 실패를 별도로 알 수 있다.

**바꿔 보기 1개:** FailOnStop을 false로 바꾸고 실행한다. 정지 실패 메시지는 뜨지 않고 정상 정리된 뒤 다시 true로 되돌린다. 이 버튼은 실패 catch에서만 메시지를 보여 주므로 false일 때 팝업이 없는 것은 정상이다.

**검증 범위:** 실제 Contracts/DeviceBase 소스와 위 변경을 반영한 TestDevice의 임시 복사본을 C# 7.3 / x64 / .NET Framework 4.7.2 참조 및 OpenCvSharp 관리 DLL로 컴파일·실행했다. true에서 Running→Faulted→Disposed, 정지 예외 메시지, CleanupCalled=True를 확인했고 false에서 Running→Stopped→Disposed, CleanupCalled=True도 확인했다. WinForms 버튼 클릭·실제 하드웨어는 확인하지 않았고 사용자 원본 파일도 직접 수정하지 않았다.

**질문 1개:** Stop이 예외를 던졌는데도 OnDispose가 실행되어 CleanupCalled가 True가 되는 이유는 무엇일까? DeviceBase.Dispose의 어느 부분이 보장하는지 답한다.

<details><summary>답</summary>OnDispose 호출이 Stop을 감싼 바깥 finally 안에 있기 때문이다. Stop이 일반 예외로 빠져나가도 바깥 finally를 실행하면서 정리를 시도한다. 안쪽 finally는 정리 표시·Disposed 상태 설정을 담당한다.</details>

> **학습 확인 (2026-11-05):** 사용자는 “OnStop에서 에러 → Stop의 catch에서 throw → Dispose의 finally 안에서 정리 실행 → true” 흐름을 설명했다 ✅. 정확히는 바깥 finally가 OnDispose 호출을 보장하고, OnDispose 안의 `CleanupCalled = true`가 값을 바꾼다. 안쪽 finally는 `_disposed`와 Disposed 상태를 기록한다. 사용자 버튼 실행 결과는 별도 보고 전이다.

### 조각 8 · 디버거로 예외와 정리 순서 따라가기

**만드는 것:** 조각 7의 실행 코드를 그대로 사용해 Stop 실패 → OnDispose → Disposed 기록 → 버튼 catch 순서를 눈으로 확인한다. 새 코드는 추가하지 않는다.

**비유:** 청소를 끝낸 순간과 출입문에 “청소 완료” 팻말을 붙이는 순간은 다르다. OnDispose가 끝났어도 다음 상태 변경 줄을 실행하기 전에는 State가 Faulted일 수 있다.

현재 DeviceBase.Dispose에서 관찰할 부분이다. 기존 코드를 수정하지 않는다.

```csharp
finally
{
    try
    {
        OnDispose();
    }
    finally
    {
        _disposed = true;
        ChangeState(DeviceState.Disposed);
    }
}
```

**준비:** MiniSight를 시작 프로젝트로 선택하고 Debug/x64로 둔다. button8의 FailOnStop=true와 TestDevice.OnDispose의 CleanupCalled=true가 작성돼 있는지 확인한다.

1. TestDevice.OnStop의 정지 실패 `throw new InvalidOperationException(...)` 줄에 F9.
2. TestDevice.OnDispose의 `CleanupCalled = true;` 줄에 F9.
3. DeviceBase.Dispose의 `_disposed = true;` 줄에 F9. 이 줄은 안쪽 finally에 있다.
4. F5로 실행하고 “정지 실패와 정리” 버튼을 누른다.
5. 첫 중단점에서 디버그 → 창 → 호출 스택(Ctrl+Alt+C)을 연다. 위에서부터 TestDevice.OnStop → DeviceBase.Stop → DeviceBase.Dispose → Form1.button8_Click 호출 관계를 확인한다.
6. 디버그 → 창 → 조사식 → 조사식 1을 열어 `State`와 `((MiniSight.Devices.TestDevice)this).CleanupCalled`를 추가한다. 이 식은 지금 중단점을 건 장치 인스턴스 메서드에서 사용한다.
7. F5로 두 번째 중단점까지 진행한다. 노란 줄은 아직 실행 전이므로 CleanupCalled는 false다. F10을 한 번 누르면 대입이 실행되어 true가 된다.
8. F5로 세 번째 중단점까지 진행한다. State와 CleanupCalled를 읽는다. 아직 `_disposed = true;`와 그 다음 상태 변경 줄은 실행 전이다.
9. F5로 계속하면 버튼 catch의 메시지에서 정리 True / 최종 Disposed가 표시된다. 버튼 catch에서는 this가 Form1이므로 장치 조사식 대신 지역 변수 device를 확인한다.

| 도구·기호 | 역할 / 사용법 | 풀어서 보면 |
|---|---|---|
| F9 | 현재 줄 중단점 켜기·끄기 | 그 줄을 실행하기 직전에 멈춤 |
| F5 | 다음 중단점까지 계속 | 다음에 설정된 지점에서 관찰 |
| F10 | 현재 줄 실행 후 다음 줄로 | 메서드 내부로 들어가지 않고 한 단계 진행 |
| 호출 스택 | 현재까지 호출된 메서드 관계 | 위가 현재 메서드, 아래가 호출자 |
| `this` | 현재 메서드를 실행 중인 객체 | 이 실험의 장치 인스턴스 |
| `(TestDevice)this` | 자식 타입으로 캐스팅 | DeviceBase 메서드에서도 자식의 CleanupCalled를 읽음; 이 버튼이 TestDevice를 만들었기 때문에 사용 가능 |

예외 설정에서 “throw 시 중단”이 켜져 있으면 별도로 예외 발생 줄에서 한 번 더 멈출 수 있다. 디버거의 중단 자체는 프로그램 종료가 아니다. F5로 계속해 정리 경로를 관찰한다.

**예상 관찰 (노란 줄 실행 전 기준):**

| 멈춘 줄 | State | CleanupCalled |
|---|---|---|
| OnStop의 throw | Running | False |
| OnDispose의 CleanupCalled=true | Faulted | False |
| Dispose의 _disposed=true | Faulted | True |
| 버튼 catch의 메시지 | Disposed | True |

**바꿔 보기 1개:** 코드는 유지하고 세 번째 중단점에서 F10으로 `_disposed=true`, 이어서 F10으로 ChangeState 호출을 실행한다. 호출이 반환된 뒤 State가 Disposed로 바뀌는 것을 확인한다.

**검증 범위 (2026-11-05):** 사용자 로컬 TestDevice와 button8에 조각 7 코드가 작성된 것을 확인했다. MiniSight를 MSBuild / Debug x64 / LangVersion=7.3으로 빌드해 성공했다. 위 표는 현재 소스와 앞선 조각 7 콘솔 실행에 따른 예상이며, 이번 Visual Studio 중단점·조사식·버튼 클릭은 직접 실행하지 않았다. 실제 장비 동작도 미검증이다.

**질문 1개:** 세 번째 중단점인 `_disposed = true;` 줄에서 멈췄을 때 State와 CleanupCalled는 각각 무엇으로 보이는가?

<details><summary>답</summary>State=Faulted, CleanupCalled=True. OnDispose는 이미 실행됐지만, 그 뒤 ChangeState(DeviceState.Disposed)는 아직 실행하지 않았기 때문이다. 노란 줄은 실행하기 전이라는 점도 함께 확인한다.</details>

> **학습 확인 (2026-11-05):** 사용자 답은 Faulted / false △. Faulted는 맞지만 세 번째 중단점에서는 OnDispose의 대입이 이미 끝났으므로 CleanupCalled=True가 예상된다. 현재 로컬 소스에도 해당 대입이 있음을 확인했다. 두 번째 중단점인 `CleanupCalled = true;` 실행 전이라면 False가 맞다. 실제로 멈춘 노란 줄이 어디인지 재확인하도록 안내했으며, 답이 실행 관찰인지 예상인지와 실제 중단 위치는 아직 확인 전이다.

> **Q. 노란 줄은 `_disposed = true;`인데 False가 보인다?** 사용자가 세 번째 중단 위치임을 확인했다. 이 줄의 실행 전 값은 `_disposed=False`, `CleanupCalled=True`, `State=Faulted`가 예상된다. 두 bool은 서로 다른 값이며, `_disposed`는 바로 다음에 대입할 부모 필드이고 CleanupCalled는 앞서 OnDispose에서 대입한 자식 속성이다. False가 어느 항목의 값인지 확인하도록 안내했다. 현재 소스의 OnDispose 대입과 호출 순서는 다시 확인했지만 실제 조사식 화면은 확인하지 않았으므로 원인은 미확정이다.

### 마무리 손코딩 · Cook만 뼈대로 다시 채우기 (3번째 타이핑)

**지금 만드는 것:** 부모가 조리 순서·성공 상태·실패 기록·정리를 맡는 Cook 메서드를 할 일 주석만 보고 다시 작성한다. 레시피의 순서를 적은 종이는 부모가 들고, 자식은 스프 넣기만 맡는 구조를 손으로 복습한다.

사용자 로컬 KataDay03.cs에서 RedRamen/BrokenRamen과 조각 4의 try/catch/finally 구현, Run의 RedRamen 선택을 확인했다. 기존 파일은 직접 수정하지 않았다.

Katas/Day03/KataDay03.cs에서 RamenRecipe.Cook의 몸통만 지운다. enum, State 속성, AddSoup 선언, 두 자식과 Run은 유지한다. 처음부터 빈 뼈대를 채우는 새 과제가 아니라 앞서 작성한 부분의 세 번째 타이핑이다.

```csharp
public void Cook()
{
    // [State / Console.WriteLine] Cooking 기록 → "조리 중: Cooking"

    // [try] "물 끓이기" → 자식의 AddSoup 호출 → 성공하면 Done

    // [catch / throw] 실패하면 Burnt 기록 → 받은 예외를 호출자에게 다시 전달

    // [finally] 성공·일반 예외 모두 "불 끄기"
}
```

새 문법은 없다. State는 상태 표시, try는 성공 경로, catch는 실패 기록과 전달, finally는 정리 단계다. 한 줄 분기도 중괄호를 유지한다. 막히면 먼저 앞의 키워드 표를 보고, 그래도 막히는 줄만 원본에서 확인한다. 다시 본 줄이 있으면 기록을 위해 알려 준다.

**실험:** Katas를 시작 프로젝트로 선택한다. Run의 `new RedRamen()`을 유지해 Ctrl+F5로 실행하고 출력과 공백까지 비교한다.

```text
시작 전: Idle
조리 중: Cooking
물 끓이기
빨간 스프 넣기
불 끄기
끝난 후: Done
```

**바꿔 보기 1개:** Run에서 생성하는 객체만 `new BrokenRamen()`으로 바꾸고 다시 실행한다.

```text
시작 전: Idle
조리 중: Cooking
물 끓이기
불 끄기
실패: 스프가 없습니다.
끝난 후: Burnt
```

**검증 범위 (2026-11-05):** 현재 사용자 Kata 파일의 임시 복사에 실행 진입점만 추가하고, 생성 객체를 RedRamen/BrokenRamen으로 선택한 두 경우를 C# 7.3 / x64 / .NET Framework 4.7.2 참조로 컴파일·실행해 위 출력을 확인했다. 사용자 원본은 수정하지 않았고, 사용자의 이번 뼈대 작성·실행 결과는 아직 확인 전이다.

**확인 요청 1개:** 채운 Cook 메서드와 두 실행 결과를 보여 준다. 원본을 다시 본 줄이 있으면 함께 알려 준다. 리뷰는 정답 대신 힌트로 진행한다.

이 손코딩 안내 당시에는 Day03을 완료 처리하지 않았다. 중단점의 `_disposed`와 CleanupCalled 값 구별은 복습 항목으로 남겼고, 이후 회사 구현 비교·셀프 체크를 진행했다. 최종 결과는 아래 셀프 체크 끝에 기록한다.

## 3. OmniSight 지도

### 직접 친 코드와 회사 코드 연결하기

**손코딩 리뷰 (2026-11-05):** 로컬 Cook에서 Cooking 기록, try의 AddSoup/Done, catch의 Burnt/throw, finally의 불 끄기 구현을 확인했다. 현재 파일의 임시 복사본을 C# 7.3 / x64 / .NET Framework 4.7.2로 컴파일·실행해 RedRamen은 Done, BrokenRamen은 Burnt로 끝나고 두 경우 모두 불 끄기가 실행됨을 확인했다. 다만 문자열이 `조리 중: Cooking 출력`으로 되어 있어 기대 출력보다 “출력” 두 글자가 더 나온다. 안내 주석에서 출력하라는 동작과 따옴표 안 실제 문구를 구별하라는 힌트를 제공한다. 사용자 원본은 수정하지 않았다. 원본을 다시 본 줄과 사용자 직접 실행 결과는 미보고다.

**비유:** 부모는 작업 순서표를 들고, 자식은 순서표 중 자기 장비에 해당하는 작업 칸을 채운다.

| 라면 Kata | 회사 DeviceBase | 역할 |
|---|---|---|
| Cook | Start | 부모가 전체 순서·상태·예외 전달을 관리 |
| AddSoup | OnStart | 자식이 실제 작업을 구현 |
| Done / Burnt | Running / Faulted | 작업 성공·실패 결과를 상태에 기록 |

회사 소스 `OmniSight/Devices/DeviceBase.cs`의 Start에서 다음 부분만 읽는다. 새로 치거나 회사 파일을 수정하는 과제가 아니다.

```csharp
try
{
    OnStart(cancellationToken);
    State = DeviceState.Running;
}
catch
{
    State = DeviceState.Faulted;
    throw;
}
```

그 앞에는 해제 여부 검사와 Ready/Stopped 상태 확인이 있다. 실제 작업 OnStart가 성공해야 Running으로 기록하고, 실패하면 Faulted를 기록한 뒤 호출자에게 예외를 전달한다.

| 선언 | 역할 / 사용법 | 풀어서 보면 |
|---|---|---|
| `public void Start(...)` | 외부에서 부르는 부모 순서표 | virtual이 아니므로 자식이 override로 교체할 수 없음 |
| `protected virtual void OnStart(...)` | 자식이 override할 작업 칸 | 기본 구현은 빈 메서드이며 필요한 자식이 구현 |
| `throw;` | 부모가 받은 예외를 다시 전달 | 상태 기록 후 실패 판단을 호출자에게 맡김 |

이 학습 구조는 호출자가 부모의 public 메서드를 통해 작업한다는 사용 규칙에 따른다. non-virtual만으로 모든 동시성 문제나 자식의 메서드 숨김까지 차단한다는 뜻은 아니다.

**확인 질문 1개:** Start를 virtual로 바꿔 자식이 통째로 override하도록 허용하면 부모의 어떤 공통 처리가 빠질 수 있을까?

<details><summary>답</summary>부모를 호출하지 않는 자식 구현은 해제·시작 가능 상태 검사, 성공 후 Running 기록, 실패 시 Faulted 기록과 예외 전달을 생략할 수 있다. 그래서 이 구조에서는 자식이 OnStart의 실제 작업을 채우고 공통 흐름은 부모 Start가 맡는다.</details>

> **학습 확인 (2026-11-05):** 사용자는 “모르겠습니다”라고 답했다. 부모 Start를 ① 시작 가능 여부 검사 ② OnStart의 실제 작업 ③ 성공·실패 상태 기록이라는 순서표로 다시 설명한다. 자식이 Start 전체를 바꿔 촬영 코드만 쓰면 앞뒤 공통 처리가 빠질 수 있다. 자식에게는 OnStart 작업 칸을 맡기고 Start 순서는 부모가 유지하는 이유를 보충한다. 재확인 질문: 카메라 자식이 촬영 시작 코드만 채울 곳은 Start와 OnStart 중 어디인가? 사용자는 OnStart라고 재답했다 ✅. 자식의 실제 작업과 부모의 공통 순서를 구별하는 핵심을 확인했다.

아래 표는 나중에 회사 코드에서 같은 구조를 찾아볼 때 사용한다.

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 순서표 Initialize | `OmniSight/Devices/DeviceBase.cs:19-42` | lock 안에서 상태 확인, `OnInitialize`, 실패 시 Faulted 후 `throw` |
| 순서표 Start | `OmniSight/Devices/DeviceBase.cs:44-63` | Ready/Stopped에서만 시작 가능 |
| 순서표 Stop | `OmniSight/Devices/DeviceBase.cs:65-83` | 정지 실패를 Stopped로 표시하지 않음 (주석 L79) |
| 빈 칸(hook) | `OmniSight/Devices/DeviceBase.cs:85-88` | `abstract OnInitialize`, `virtual OnStart/OnStop/OnDispose` |
| Dispose 순서 | `OmniSight/Devices/DeviceBase.cs:90-115` | Stop 실패해도 OnDispose는 무조건 (중첩 finally) |
| 2단 템플릿 | `OmniSight/Devices/Cameras/CallbackCameraAdapterBase.cs` (OnInitialize 등 오버라이드 부분) | 카메라 공통 로직을 한 번 더 템플릿화 |
| AI 엔진 템플릿 | `OmniSight/Vision/AiEngine/AiEngineBase.cs` | abstract Initialize/Inspect/ReleaseNativeHandle |
| UI 템플릿 | `OmniSight/UI/Controls/BaseUserControl.cs` | OnHandleCreated에서 공통 폰트·번역 적용 |

**집에서 Claude에게:**
```text
OmniSight/Devices/DeviceBase.cs 전체를 보여주고 라면 순서표 비유로 설명해줘.
CallbackCameraAdapterBase가 DeviceBase를 어떻게 한 번 더 템플릿으로 쓰는지
OnInitialize/OnStart/OnStop/OnDispose 오버라이드 부분만 골라서 보여줘.
```

## 4. 왜 이렇게 썼나

### 4.1 상태 그림 (직접 그려 보기)

```text
           Initialize 성공              Start 성공
Created ───────────────▶ Ready ─────────────────▶ Running
   │                       ▲  ▲                      │
   │ Initialize 실패       │  └──── Start ────┐      │ Stop 성공
   ▼                       │                  │      ▼
Faulted ◀── 어떤 단계든 실패 ┘              Stopped ◀─┘
   (Stop 실패도 Faulted. "멈췄다고 거짓말하지 않기")
모든 상태 ── Dispose ──▶ Disposed
```

### 4.2 결정과 이유

| 결정 | 이유 |
|---|---|
| `Initialize/Start/Stop/Dispose`를 **virtual이 아니게** | 자식이 상태 관리 순서를 망가뜨리지 못하게. 상태 규칙이 한 곳에만 있음 |
| 실패 시 상태를 `Faulted`로 바꾸고 **다시 throw** | 상태는 정확히 기록하고, 판단(롤백)은 호출자(DeviceManager)가 하게 |
| Stop 실패를 Stopped로 표시 안 함 | 카메라가 실제로 안 멈췄는데 "정지"로 보이면 작업자가 재시작 → 장비 충돌. **안전 우선** |
| Dispose는 Stop 실패와 무관하게 OnDispose 실행 | 네이티브 핸들·큐는 무조건 해제해야 메모리/핸들 누수가 없음 |
| `OnInitialize`만 abstract | 모든 장치는 연결 단계가 반드시 있지만, Start/Stop은 없어도 되는 장치가 있다 |

### 4.3 시니어의 눈으로 본 약점

- `State`는 일반 자동 속성이다. `Initialize`의 상태 확인은 lock 안이지만 `State = Ready`(L35)와 `Start/Stop`의 대입은 lock 밖이다. 다른 스레드가 `State`를 읽으면 오래된 값을 볼 수 있다. 현재는 Start/Stop을 한 스레드(VisionSystem의 lock)에서만 부르는 **사용 규칙**으로 지킨다. 규칙이 문서에 없으면 언젠가 깨진다.
- Template Method는 **상속**이라 부모를 바꾸면 모든 자식이 영향을 받는다. 그래서 부모는 작고 안정적이어야 한다. 변하는 부분이 많으면 Strategy(합성)가 더 낫다 (Day16에서 비교).

## 5. Kata: 라면 공장

```csharp
// KATA: Day03
using System;

namespace Katas.Day03
{
    public enum CookState { Idle, Cooking, Done, Burnt }

    public abstract class RamenRecipe
    {
        public CookState State { get; private set; }

        // 템플릿 메서드: virtual 아님. 순서는 여기서만 정한다.
        public void Cook()
        {
            if (State == CookState.Cooking)
            {
                throw new InvalidOperationException("이미 조리 중");
            }
            State = CookState.Cooking;
            try
            {
                BoilWater();
                AddNoodles();          // 자식이 채우는 칸
                AddSoup();             // 자식이 채우는 칸
                if (WantsEgg())
                {
                    AddEgg(); // hook: 기본값이 있는 선택 칸
                }
                State = CookState.Done;
            }
            catch
            {
                State = CookState.Burnt;   // 실패 기록 후
                throw;                     // 판단은 호출자에게
            }
            finally
            {
                TurnOffFire();             // 실패해도 불은 반드시 끈다 (OnDispose 같은 역할)
            }
        }

        private void BoilWater() { Console.WriteLine("물 550ml 끓이기"); }
        private void TurnOffFire() { Console.WriteLine("불 끄기"); }
        protected abstract void AddNoodles();
        protected abstract void AddSoup();
        protected virtual bool WantsEgg() { return false; }
        private void AddEgg() { Console.WriteLine("계란 투하"); }
    }

    public sealed class Shin : RamenRecipe
    {
        protected override void AddNoodles() { Console.WriteLine("신라면 면 4분 30초"); }
        protected override void AddSoup() { Console.WriteLine("빨간 스프"); }
        protected override bool WantsEgg() { return true; }
    }

    public sealed class BrokenRamen : RamenRecipe
    {
        protected override void AddNoodles() { Console.WriteLine("면 넣기"); }
        protected override void AddSoup() { throw new InvalidOperationException("스프가 없다!"); }
    }

    public static class KataDay03
    {
        public static void Run()
        {
            var shin = new Shin();
            shin.Cook();
            Console.WriteLine("상태: " + shin.State);

            var broken = new BrokenRamen();
            try { broken.Cook(); }
            catch (Exception ex) { Console.WriteLine("실패: " + ex.Message); }
            Console.WriteLine("상태: " + broken.State);   // Burnt
        }
    }
}
```

**확장 과제**: `Cook()`을 두 스레드에서 동시에 부르면 `State == Cooking` 확인을 둘 다 통과할 수 있다. 어디에 lock을 걸어야 할까? (`DeviceBase.Initialize`가 lock을 어디까지 잡는지와 비교)

## 6. MiniSight 오늘 단계

`Devices/DeviceBase.cs` 작성. 요구사항(코드는 직접):

- `public abstract class DeviceBase : IDevice`
- `Initialize(ct)`: Disposed면 `ObjectDisposedException`. 이미 Ready/Running이면 return. Initializing → `OnInitialize` → Ready, 실패 시 Faulted + rethrow.
- `Start(ct)`: Ready/Stopped가 아니면 `InvalidOperationException`. 성공 Running, 실패 Faulted.
- `Stop()`: Created/Disposed면 무시. 성공 Stopped, 실패 Faulted + rethrow.
- `Dispose()`: Stop 시도 → 예외와 무관하게 `OnDispose` → Disposed.
- 상태가 바뀔 때마다 `Console.WriteLine(Name + ": " + old + " -> " + new)` (Day06에서 이벤트로 교체).

테스트용 `TestDevice : DeviceBase`를 만들어 `FailOnStart` 속성이 true면 `OnStart`에서 예외를 던지게 한다.

## 7. VS2022 디버깅 실습

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | `DeviceBase.Start`의 `State = DeviceState.Running;` 줄에 중단점 → 우클릭 → **조건** → `Name == "Cam2"` | Cam2일 때만 멈춤 |
| 2 | 중단점 우클릭 → **작업(Actions)** → 메시지 `{Name}: {State} 시작` 입력, "실행 계속" 체크 | **Tracepoint**: 멈추지 않고 출력 창에 로그. 현장 재현이 어려운 타이밍 문제에서 유용 |
| 3 | `FailOnStart = true`로 실행 | `catch` 블록에서 `State`가 Faulted로 바뀌는 순간 확인 |
| 4 | 디버그 → 창 → **예외 설정(Ctrl+Alt+E)** → CLR Exceptions → `InvalidOperationException` 체크 | throw 되는 **바로 그 줄**에서 멈춤 |
| 5 | 중단점 창(Ctrl+Alt+B)에서 중단점 일괄 끄기/켜기 | |

## 8. 셀프 체크

> **2회차 진행 (2026-11-05):** 회사 비교 뒤 한 번에 한 질문씩 확인한다. 먼저 아래 3번을 안내했다: Stop에서 예외가 발생했을 때 Stopped로 표시하면 안 되는 이유를 한 문장으로 설명한다. 사용자 답은 아래 학습 확인에 기록했다. 1번의 Strategy 비교는 Day16에서 실제로 배운 뒤 다시 확인하며, 지금은 부모의 공통 순서와 자식의 작업 칸 구별에 집중한다.

1. Template Method와 Strategy의 차이를 한 문장씩.
<details><summary>답</summary>Template Method: 상속으로 알고리즘 순서를 부모가 고정하고 일부 단계를 자식이 채운다. Strategy: 합성으로 알고리즘 전체(또는 한 단계)를 객체로 갈아끼운다. 전자는 컴파일 시점 고정, 후자는 실행 중 교체 가능.</details>

2. 템플릿 메서드를 virtual로 만들면 무엇이 위험한가?
<details><summary>답</summary>자식이 오버라이드해서 상태 전이·예외 처리 순서를 생략할 수 있다. 상태 규칙이 여러 클래스에 흩어져 장비 안전 규칙이 깨진다.</details>

3. Stop이 실패했을 때 Stopped가 아니라 Faulted로 두는 이유는?
<details><summary>답</summary>실제로 장치가 멈췄는지 확인하지 못했기 때문이다. Stopped로 보이면 운영자나 코드가 재시작을 시도해 이중 취득, 핸들 충돌, 기계 동작 오류가 날 수 있다. 모르면 "고장"이라고 표시하는 게 안전하다.</details>

> **학습 확인 (2026-11-05):** 사용자 답: “Stopped는 정상 상태에서 정지했다는 느낌이고 내부 예외가 발생한 것을 알 수 없기 때문” ✅. 정상 정지로 오해하게 만든다는 핵심을 확인했다. 정지 처리 중 실패했으므로 실제 정지 성공 여부가 확인되지 않았고, Faulted로 실패를 표시해 다음 판단이 필요함을 알리는 것이라고 보충했다. 실제 장치가 계속 움직인다고 단정하는 뜻은 아니다.

4. Dispose 안에서 중첩 try/finally를 쓴 이유는?
<details><summary>답</summary>Stop()이 예외를 던져도 OnDispose()(네이티브 자원 해제)가 반드시 실행되고, OnDispose가 실패해도 _disposed/State 표시는 반드시 되도록 하기 위해서다.</details>

> **2회차 확인 범위:** 조각 7에서 사용자가 OnStop 실패 → Stop의 catch/throw → Dispose의 finally → OnDispose 실행을 설명했다 ✅. 정리 코드가 실패해도 안쪽 finally에서 정리 표시를 남기는 역할은 보충 설명했으며, OnDispose 실패 주입 실험은 하지 않았다.

5. `catch { State = Faulted; throw; }`에서 `throw ex;`가 아니라 `throw;`인 이유는?
<details><summary>답</summary>`throw;`는 원래 스택 트레이스를 보존한다. `throw ex;`는 다시 던진 지점에서 스택 추적을 다시 시작하므로 원래 호출 경로 일부가 사라져 원인 위치를 추적하기 어려워질 수 있다.</details>

**2회차 마지막 확인 (2026-11-05):** 스택 트레이스는 예외가 어떤 메서드를 거쳐 왔는지 남기는 호출 경로 기록이다. 예를 들어 OnStart에서 발생한 예외를 부모 Start가 catch해 다시 던질 때, `throw;`로 원래 경로를 보존하는 것이 현장 원인 분석에 유리하다. 새로운 예외를 만들지 않고 받은 예외를 다시 전달하는 상황이다.

```csharp
catch (InvalidOperationException ex)
{
    State = DeviceState.Faulted;
    throw;
}
```

위 비교용 예시에서 ex 변수는 쓰지 않으므로 실제 부모 코드는 기존처럼 `catch`만 써도 된다. 회사 코드나 사용자 코드를 바꾸는 과제가 아니다.

| 코드 | 역할 / 사용법 | 풀어서 읽기 |
|---|---|---|
| `throw;` | catch에서 받은 예외를 원래 스택 추적과 함께 다시 전달 | “이 실패가 여기까지 왔다”는 원래 경로를 유지 |
| `throw ex;` | 지정한 예외를 현재 지점에서 던짐 | 원래 호출 경로 일부가 사라질 수 있어 단순 재전달에 부적합 |

**질문 1개:** 실제 오류는 OnStart에서 났는데 로그에서 Start의 다시 던진 위치만 찾을 수 있다면, 현장 원인 분석이 왜 어려워질까?

<details><summary>답</summary>실제로 실패한 메서드와 발생 위치를 추적할 단서가 줄어들기 때문이다. 받은 예외를 그대로 전달할 때 throw를 사용해 원래 경로를 유지한다.</details>

> **학습 확인 (2026-11-05):** 사용자 답: “실제 어느 위치에서 에러가 발생했는지 정확히 파악하기 어렵다. throw ex가 되면 ex 호출 자리에서 에러가 작성된다” ✅. 원래 위치 추적이 어려워진다는 핵심을 확인했다. 정확히는 새 오류를 만드는 것이 아니라 다시 던진 지점에서 스택 추적을 다시 시작해 원래 경로 일부를 잃는 것이라고 보충했다. 이번 설명에서는 별도 재던지기 비교 실행이나 디버거 실험을 하지 않았다.

**2회차 완료 (2026-11-05, 약 70% 기준):** 부모의 순서·자식의 작업 칸, 성공 후 상태 기록, 실패 시 Faulted/throw, 실패해도 정리 시도라는 핵심을 코드와 답변으로 확인했다. MiniSight Debug/x64와 Katas Debug/AnyCPU + PlatformTarget=x64를 C# 7.3으로 빌드해 성공했다. 사용자 Kata의 임시 복사본을 실행해 정상 Done/실패 Burnt 및 두 경로의 불 끄기를 확인했고, 장치 시작·정지 실패 모의 실행은 앞 조각에 검증 범위를 기록했다.

추가 복습은 남겨 둔다: `_disposed`와 CleanupCalled 조사식 구별(Day09), Template Method/Strategy 비교(Day16), 뼈대 작성 때 원본을 다시 본 줄 여부, Kata 문자열의 안내어 “출력” 제거. 사용자 원본은 직접 수정하지 않았다. 사용자 WinForms 최종 메시지·디버거 조사식 화면, OnDispose 실패 주입, 실제 장비 동작은 별도 확인 전이다. 다음 단원은 Day04다.
