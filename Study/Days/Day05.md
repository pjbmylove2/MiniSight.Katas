# Day 05 (금, 2h) · Composition Root, Singleton, Func 설정 주입

## 1. 오늘 목표와 이번 회차 완료 범위

- 구체 장치의 생성·연결을 조립 지점에 모으는 이유를 설명한다.
- 정상·실패·return 경로의 finally와, 성공 후 호출자에게 넘긴 자원의 정리 책임을 구분한다.
- Lazy로 설정 창고 한 객체를 지연 생성한다.
- 숫자 보관과 Func<int> 보관을 구별하고, 검사 때 읽을 기준값 제공 함수를 주입한다.
- 기존 MiniSight의 CameraFactory와 button9를 재사용해 단일 카메라 설정, 공유 ConfigStore, JSON 저장·읽기, 준비 오류 시 중단을 연결한다.

2026-11-05 시작, 2026-11-06 핵심 기본 목표를 약 70% 기준으로 마무리했다. 원래 계획의 모든 확장 구현을 끝냈다는 뜻은 아니다. 실제 확인과 미구현·미검증 범위는 6절에 구분한다. 다음 학습은 Day06이다.

## 2. 초1 비유

- Composition Root는 레고 조립 책상이다. 부품을 골라 연결하는 위치를 모아 장치 교체 때 수정할 곳을 줄인다. 모든 DTO·배열의 new를 한 파일에 몰라는 규칙은 아니다.
- Singleton은 함께 찾아가는 설정 창고 하나다. Lazy는 첫 방문 때 창고를 만든다. 생성 보호와 창고 내용 변경의 동기화는 별개다.
- Func는 기준값을 물어보는 방법이다. 숫자를 적어 두는 대신 호출할 때 지정한 곳에서 읽는다. 고정값을 돌려주는 함수도 만들 수 있다.

## 2-1. 조각별 실습

한 번에 조각 하나씩 코드 입력 → 실행 결과 비교 → 뼈대 재작성으로 진행한다. 예제마다 Run 본문을 교체하며, 같은 이름의 Run을 여러 개 추가하지 않는다. 콘솔 실험은 Katas, 실제 적용은 MiniSight 프로젝트다.

조각 1의 교재 코드는 namespace 안의 두 클래스로 설명했다. 학생의 실제 파일은 PowerDisplay 안에 Bootstrapper와 KataDay05가 중첩되고, Program에서 using static으로 접근한다. 유효한 구조이며 이번 학습에서는 그 위치를 유지했다. SettingsStore와 Inspector는 namespace 수준이다. 조각 1 아래의 당시 기록과 마지막 실제 파일 상태를 구분한다.

### 조각 1 · 쓰는 부품과 조립하는 곳 구분하기

**지금 만드는 것:** 장치를 스스로 만들지 않고 생성자로 전달받는 PowerDisplay와, 장치를 골라 연결하는 Run.

**비유:** 레고 자동차의 바퀴는 엔진을 사 오지 않는다. 조립 책상에서 사람이 엔진과 바퀴를 골라 연결한다. 이번엔 PowerDisplay가 쓰는 부품이고 Run이 조립 책상이다.
Day04 Factory는 “어떤 부품을 만들까”, 오늘 Composition Root는 “만든 부품들을 어디서 연결할까”에 초점을 둔다.

**재사용:** 사용자의 Katas.Day02에 공개 IPowerSource/Fan/HairDryer와 각각 선풍기/헤어드라이기 Name이 있음을 현재 소스로 확인했다. 다시 만들지 않는다.
Katas 콘솔 프로젝트에 Day05 폴더와 KataDay05.cs를 추가해 아래 전체 코드를 직접 입력한다. 포트폴리오 원본 .cs는 강사가 수정하지 않는다.

```csharp
using System; // Console과 예외
using Katas.Day02; // 이미 작성한 계약과 장치

namespace Katas.Day05
{
    public sealed class PowerDisplay // 전달받은 장치의 이름을 표시하는 부품
    {
        private readonly IPowerSource _source; // 사용할 장치 참조를 보관

        public PowerDisplay(IPowerSource source) // 장치를 밖에서 받는 생성자
        {
            if (source == null) // 잘못된 입력을 먼저 확인
            {
                throw new ArgumentNullException(nameof(source)); // 호출자에게 오류 전달
            }

            _source = source; // 받은 장치를 필드에 보관
        }

        public void Show() // 장치를 만들지 않고 공통 계약으로 사용
        {
            Console.WriteLine("연결한 기기: " + _source.Name);
        }
    }

    public static class KataDay05
    {
        public static void Run() // 이번 작은 예제의 조립 지점
        {
            IPowerSource source = new Fan(); // 사용할 구체 장치를 선택
            PowerDisplay display = new PowerDisplay(source); // 두 부품을 연결
            display.Show(); // 조립된 부품을 사용
        }
    }
}
```

| 키워드/항목 | 타입·역할 | 읽는 방법 |
|---|---|---|
| source / _source | IPowerSource | 공통 계약으로 장치를 받음/보관 |
| display | PowerDisplay | 장치를 사용하는 표시 부품 |
| 생성자 PowerDisplay(...) | 객체 생성 시 호출, 반환 타입 없음 | new PowerDisplay(source)가 이 생성자로 source를 전달 |
| private readonly | 생성 시 정한 필드 참조를 이후 다시 대입하지 않음 | 참조 대상 객체 전체가 불변이 되거나 스레드 안전해지는 것은 아님 |
| _source = source | 매개변수의 참조를 인스턴스 필드에 보관 | 객체를 복제하지 않음 |
| public sealed class | 외부에서 쓸 수 있으며 상속을 허용하지 않는 클래스 | 생성자 전달 자체에 sealed가 필수인 것은 아님 |
| void / static | 반환값 없음 / 클래스 이름으로 호출 | KataDay05.Run()은 실행용 진입 메서드 |
| nameof(source) | 매개변수 이름 문자열 | null을 잘못 전달하면 인자 이름과 함께 예외로 알림 |

**VS2022**
1. 솔루션 탐색기에서 Katas 프로젝트 우클릭→추가→새 폴더→Day05.
2. Day05 우클릭→추가→클래스→KataDay05.cs. 생성된 틀을 위 전체 코드로 작성한다.
3. Katas/Program.cs의 기존 `KataDay04.Run();` 호출만 `Day05.KataDay05.Run();`으로 교체한다. 기존 Console.ReadKey()와 이전 학습 코드는 유지한다.
4. Katas 프로젝트 우클릭→시작 프로젝트로 설정, Debug/x64 선택, Ctrl+Shift+B로 빌드 후 Ctrl+F5로 실행한다. VS에서 추가한 파일의 Compile Include 편입도 자동 저장됐는지 확인한다.

예상 출력:

```text
연결한 기기: 선풍기
```

**줄별 흐름:** Run에서 Fan을 생성→IPowerSource로 보관→PowerDisplay 생성자로 같은 참조 전달→생성자에서 null 확인 후 _source에 보관→Show가 _source.Name을 읽어 출력한다.
PowerDisplay는 Fan/Factory를 직접 참조하지 않는다. 새로운 장치가 같은 계약을 구현하면 연결하는 부분을 바꾸고 표시 코드는 유지할 수 있다.
의존성 주입은 필요한 부품을 바깥에서 전달한다는 뜻이고, Composition Root는 이 연결을 모아 두는 위치다. DI 컨테이너가 반드시 필요한 것은 아니다.
서비스/장비의 연결을 한곳에서 관리한다는 원칙이며 모든 DTO/배열/영상 생성까지 한 파일에 몰아야 한다는 뜻은 아니다.
이번 Fan/HairDryer에는 외부 자원이나 IDisposable이 없어 추가 Dispose가 없다. 공유 필드 변경/Task도 없고, 장치 수명/정리 책임은 후속 조각에서 연결한다.

**바꿔 보기 하나:** `new Fan()`만 `new HairDryer()`로 바꿔 같은 프로그램을 실행한다. Show나 PowerDisplay의 본문은 변경하지 않는다.

```text
연결한 기기: 헤어드라이기
```

**디버깅:** Run의 new PowerDisplay(source) 줄에 F9→F5→F11로 생성자 진입→F10으로 _source 대입 실행→지역/조사식에서 source와 _source의 Name 확인→F5로 계속한다. 매개변수 source는 생성자 범위, _source는 객체 필드다. 생성자 밖에 나오면 같은 이름의 지역 변수와 구분한다.

**회사 연결(손코딩 뒤):** ApplicationBootstrapper.Initialize에서 DeviceFactory로 장치 묶음을 만들고 검사 런타임과 함께 new VisionSystem(devices, inspectionRuntime)으로 연결한다. 현재 확인 위치는 ApplicationBootstrapper.cs:177/200/210이다. 회사 프로그램 실행/장비 연결은 하지 않았다.
Factory를 부품 생성 담당, Bootstrapper를 생성 순서/연결 담당으로 구분한다. 실제 코드의 실패 정리/소유권 이전은 조각 3에서 확인한다.

**검증:** 2026-11-05 교재 예제와 한 줄 변형을 C#7.3/.NET Framework4.7.2 참조/x64로 임시 경로에서 각각 컴파일·실행했다. 선풍기/헤어드라이기 출력 문자열이 예상과 정확히 일치했다. 기존 Day02 타입 파일은 읽어서 재사용했고 학생/회사 소스는 수정하지 않았다. 학생의 직접 작성/실행 완료와는 별개다.

**면접 한 줄:** 객체를 사용하는 부품은 공통 계약에 의존하게 하고, 구체 객체의 생성과 연결은 조립 지점에 모아 교체 범위를 줄였습니다.

**질문 하나:** new Fan()을 new HairDryer()로 바꿔도 PowerDisplay.Show()를 고치지 않아도 되는 이유를 한 문장으로 설명해 주세요.

<details><summary>답과 사용자 확인</summary>
Fan과 HairDryer가 모두 IPowerSource를 구현하므로 PowerDisplay는 구체 종류 대신 공통 계약의 Name을 사용한다. 생성자는 Run에서 이미 만든 객체를 받아 필드에 보관한다. 이 구조라 장치 교체 때 조립하는 줄만 바꾸고 Show는 유지할 수 있다.
사용자 답은 공통 인터페이스·멤버 필드·생성자 전달·Show 사용을 연결해 설명해 정답 ✅. “인터페이스를 가지고 있다”는 “인터페이스를 구현한다”로 용어만 보충했다.
조각 1 당시 로컬 KataDay05.cs는 HairDryer를 생성해 전달하고 null 검사/readonly 필드/Show 출력을 구현했다. 다만 실행용 KataDay05가 PowerDisplay의 중첩 클래스로 작성됐고 Program에서 using static Katas.Day05.PowerDisplay로 호출한다. 유효한 중첩 구조이며 교재의 같은 namespace 안 두 클래스 구조와 구분한다. 사용자가 직접 실행한 “연결한 기기: 헤어드라이기”를 제출해 예상과 일치 확인 ✅. 설명과 실행 기준으로 조각 1 기본 목표 통과. 3번째 뼈대 타이핑·독립 재구현 완료로 확대하지 않으며 조각 1 당시의 기록이며 이후 완료 범위는 6절을 따른다.
</details>

### 조각 2 · Bootstrapper로 조립 순서 묶기

**지금 만드는 것:** 구체 장치의 생성과 연결을 `Build()`에 모으고, `Run()`은 조립된 표시기를 받아 사용한다.

레고 조립 담당자가 부품을 골라 연결한 뒤 완성품을 건네는 모습이다. 실제 학생 파일에서는 아래 두 클래스가 `PowerDisplay` 안에 중첩되어 있다. 그 위치를 유지한다.

```csharp
public static class Bootstrapper
{
    public static PowerDisplay Build()
    {
        IPowerSource source = new HairDryer();
        PowerDisplay display = new PowerDisplay(source);
        return display;
    }
}

public static class KataDay05
{
    public static void Run()
    {
        PowerDisplay display = Bootstrapper.Build();
        display.Show();
    }
}
```

| 코드 | 역할 / 사용법 |
|---|---|
| `Bootstrapper` | 우리가 붙인 조립 담당 클래스 이름 |
| `static` | 객체를 따로 만들지 않고 클래스 이름으로 호출 |
| `PowerDisplay Build()` | `PowerDisplay`를 반환하는 메서드 |
| `return display;` | 같은 객체의 참조를 호출자에게 반환 |

**실험:** `new HairDryer()` 한 줄만 `new Fan()`으로 바꾸고 빌드·실행한다.

```text
연결한 기기: 선풍기
```

**질문 하나:** 기기를 교체할 때 어디를 바꾸면 될까?

<details><summary>답과 실제 확인</summary>
구체 장치를 고르는 `Build()`의 생성 코드를 바꾼다. 사용자는 `Build` 안에 `new HairDryer()`가 있기 때문이라고 답했고, 선풍기 출력을 직접 제출했다. 뼈대 작성에서는 `IPowerSource source = new Fan();` 다음에 `return new PowerDisplay(source);`로 바로 반환했다. 변수에 담은 뒤 반환하는 두 줄과 같은 동작이다. 원본을 다시 본 줄은 없다고 답했다. 설명·실행·뼈대 작성 통과 ✅.
</details>

### 조각 3 · 조립 도중 실패해도 임시 자원 정리하기

**지금 만드는 것:** 실패를 일부러 발생시키고 `finally`와 호출자의 `catch` 실행 순서를 확인한다.

조립하다 실패해도 꺼내 놓은 도구는 정리한다. 파일 맨 위에 `using System.IO;`를 추가하고, 기존 `Build()`와 `Run()`을 아래 내용으로 교체한다. `MemoryStream`은 메모리 안에서 데이터를 읽고 쓰는 연습용 자원이다.

```csharp
public static PowerDisplay Build()
{
    MemoryStream buffer = new MemoryStream();
    try
    {
        Console.WriteLine("임시 버퍼 생성");
        throw new InvalidOperationException("조립 실패");
    }
    finally
    {
        buffer.Dispose();
        Console.WriteLine("임시 버퍼 정리");
    }
}
```

이번 메서드는 실패만 재현하므로 정상 결과를 반환하는 경로가 없다.

```csharp
public static void Run()
{
    try
    {
        PowerDisplay display = Bootstrapper.Build();
        display.Show();
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine(ex.Message);
    }
}
```

| 코드 | 역할 / 사용법 |
|---|---|
| `try` | 실패할 수 있는 작업을 감싸는 구역 |
| `throw new InvalidOperationException(...)` | 예외 객체를 만들어 던짐 |
| `finally` | 정상 종료·예외·`return`으로 블록을 빠져나갈 때 실행하는 정리 구역 |
| `Dispose()` | 자원의 사용을 끝냄. 변수를 `null`로 만드는 작업은 별개 |
| `catch (... ex)` | 해당 예외를 받아 `ex`로 사용 |
| `ex.Message` | 예외의 설명 문자열 |

```text
임시 버퍼 생성
임시 버퍼 정리
조립 실패
```

**질문 하나:** 오류 메시지보다 정리 문구가 먼저 출력되는 이유는?

<details><summary>답과 실제 확인</summary>
예외가 호출자에게 전달되는 과정에서 `Build()`의 `finally`가 먼저 실행되고, 그 뒤 `Run()`의 `catch`가 처리한다. 사용자는 순서를 맞게 설명했다. “메시지를 보낸다”는 표현은 “예외 객체를 던진다”로 보충했다. 정상 반환하지 않았으므로 `display.Show()`는 실행되지 않는다. 사용자 실행 출력 일치 ✅.
</details>

### 조각 4 · 성공해도 finally는 실행된다

**지금 만드는 것:** 예외 대신 표시기를 반환해 정상 경로의 정리를 확인한다. `Run()`은 조각 3의 것을 유지한다.

```csharp
public static PowerDisplay Build()
{
    MemoryStream buffer = new MemoryStream();
    try
    {
        Console.WriteLine("임시 버퍼 생성");
        IPowerSource source = new Fan();
        return new PowerDisplay(source);
    }
    finally
    {
        buffer.Dispose();
        Console.WriteLine("임시 버퍼 정리");
    }
}
```

`return` 식으로 표시기를 만든 뒤, `finally`가 실행되고, 표시기가 호출자에게 반환된다. 버퍼와 표시기는 서로 다른 객체이고 버퍼는 표시기에 전달하지 않았다.

```text
임시 버퍼 생성
임시 버퍼 정리
연결한 기기: 선풍기
```

**질문 하나:** 예외가 없어도 정리 문구가 나오는 이유는?

<details><summary>답과 실제 확인</summary>
`finally`는 정상 종료와 예외 경로에서 모두 실행된다. 사용자는 성공·실패와 관계없이 실행된다고 답했고, 실행 결과도 같다고 확인했다 ✅. 다만 프로세스 강제 종료, 전원 차단, `Environment.Exit` 등까지 포함해 “무조건” 실행되는 것은 아니다. Day01의 프로세스 종료 실험과 구분한다.
</details>

### 조각 5 · 성공하면 받은 쪽이 정리하기

**지금 만드는 것:** 버퍼를 반환한 뒤 호출자가 사용·정리한다. 기존 `Bootstrapper`에 아래 메서드를 추가한다.

```csharp
public static MemoryStream BuildBuffer()
{
    MemoryStream buffer = new MemoryStream();
    try
    {
        MemoryStream result = buffer;
        buffer = null;
        return result;
    }
    finally
    {
        buffer?.Dispose();
    }
}
```

상자 하나에 이름표 두 개를 붙였다가 한쪽 이름표만 떼는 비유다. 객체를 복제하거나 C#이 소유권을 자동 이전하는 기능이 아니다. 성공 시 호출자가 정리한다는 규칙을 참조 대입과 정리 코드로 표현했다. 이후 실패할 수 있는 준비 단계가 추가되면, 넘기기 전 실패는 이 메서드의 정리 대상이다.

```csharp
public static void Run()
{
    MemoryStream buffer = Bootstrapper.BuildBuffer();
    using (buffer)
    {
        Console.WriteLine("사용 중: " + buffer.CanRead);
    }

    Console.WriteLine("정리 후: " + buffer.CanRead);
}
```

| 코드 | 역할 / 긴 형태 |
|---|---|
| `result = buffer` | 같은 객체의 참조를 변수 하나에 더 보관 |
| `buffer = null` | 이 메서드의 `buffer` 변수만 비움 |
| `buffer?.Dispose()` | `if (buffer != null) { buffer.Dispose(); }` |
| `using (buffer)` | `try`에서 사용하고 `finally`에서 `Dispose()`하는 정리 형태 |
| `CanRead` | 이 스트림에서 현재 읽기를 할 수 있는지 확인 |

```text
사용 중: True
정리 후: False
```

**질문 하나:** `buffer = null` 뒤에도 반환된 버퍼를 사용할 수 있는 이유는?

<details><summary>답과 실제 확인</summary>
사용자는 `result`와 `buffer`가 같은 객체를 가리키며 `buffer`만 비워도 `result`는 생성된 버퍼를 계속 가리킨다고 설명했다 ✅. 호출자의 같은 이름 `buffer`는 별개의 지역 변수다. 반환받은 객체는 호출자의 `using`이 정리한다. 사용자 실행 True/False 확인 ✅.
</details>

### 조각 6 · Lazy로 공유 설정 창고를 처음 필요할 때 만들기

**지금 만드는 것:** 설정 창고 한 객체를 공유하고 첫 접근 때 생성한다. 아래 클래스는 `namespace Katas.Day05` 안, `PowerDisplay` 바깥에 추가한다.

```csharp
public sealed class SettingsStore
{
    private static readonly Lazy<SettingsStore> _instance =
        new Lazy<SettingsStore>(() => new SettingsStore());

    public static SettingsStore Instance
    {
        get
        {
            return _instance.Value;
        }
    }

    private SettingsStore()
    {
        Console.WriteLine("설정 창고 생성");
    }
}
```

```csharp
public static void Run()
{
    Console.WriteLine("아직 접근 전");
    SettingsStore first = SettingsStore.Instance;
    SettingsStore second = SettingsStore.Instance;
    Console.WriteLine("같은 창고: " + object.ReferenceEquals(first, second));
}
```

| 코드 | 역할 / 긴 형태 |
|---|---|
| `Lazy<SettingsStore>` | 처음 `Value`를 읽을 때 만들고 결과를 보관하는 도구 |
| `static readonly` | 공유 필드이며 초기화 후 다른 `Lazy` 상자로 재대입하지 못함 |
| `() => new SettingsStore()` | 인수 없는 함수. `() => { return new SettingsStore(); }` |
| `private SettingsStore()` | 외부에서 직접 생성하는 호출을 막는 생성자 |
| `object.ReferenceEquals(...)` | 두 변수가 같은 객체를 가리키는지 확인. `System.Object.ReferenceEquals(...)` |

`Instance`는 속성이므로 뒤에 `()`를 붙이지 않는다. 기본 `Lazy<T>`의 생성·게시 보호와 설정 객체의 변경 안전성은 별개다. 공유 설정의 동시 읽기·변경까지 자동으로 보호하는 것은 아니다.

```text
아직 접근 전
설정 창고 생성
같은 창고: True
```

**질문 하나:** 두 번 읽었는데 생성 문구가 한 번만 나오는 이유는?

<details><summary>답과 실제 확인</summary>
처음 `Value`를 읽을 때 생성하고, 이후에는 보관한 같은 객체를 반환한다. 사용자 설명과 직접 실행 출력 확인 ✅. 여러 스레드 동시 접근 실험은 이번 회차에 하지 않았다.
</details>

### 조각 7 · 숫자 복사와 읽는 함수 보관의 차이

**지금 만드는 것:** 기준값을 바꾸고, 복사한 숫자와 호출 때 읽는 숫자를 비교한다. `SettingsStore` 생성자 바깥에 속성을 추가한다.

```csharp
public int Threshold { get; set; }
```

```csharp
public static void Run()
{
    SettingsStore store = SettingsStore.Instance;
    store.Threshold = 100;

    int saved = store.Threshold;
    Func<int> read = () => store.Threshold;

    Console.WriteLine("변경 전: " + saved + " / " + read());
    store.Threshold = 150;
    Console.WriteLine("변경 후: " + saved + " / " + read());
}
```

| 코드 | 역할 / 긴 형태 |
|---|---|
| `Threshold` | 검사 기준값을 보관하는 속성 |
| `saved` | 대입 당시 숫자의 복사본 |
| `Func<int>` | Function. 인수 없이 `int`를 반환하는 함수를 담는 타입 |
| `() => store.Threshold` | `() => { return store.Threshold; }` |
| `read()` | 보관한 함수를 지금 실행 |

```text
설정 창고 생성
변경 전: 100 / 100
변경 후: 100 / 150
```

**질문 하나:** 변경 후 두 값이 다른 이유는?

<details><summary>답과 실제 확인</summary>
사용자는 `saved`에 처음 숫자 100을 보관한 뒤 다시 대입하지 않았고, `read()`는 변경된 `store.Threshold`를 읽는다고 설명했다 ✅. 직접 실행 출력 일치 ✅. 함수에 넣는 줄이 아니라 호출하는 시점에 속성값을 읽는다.
</details>

### 조각 8 · 읽는 방법을 검사 부품에 전달하기

**지금 만드는 것:** 설정 창고를 직접 모르는 검사 부품에 기준값 제공 함수를 전달한다. `Inspector`는 `SettingsStore`와 같은 namespace 수준에 추가한다.

```csharp
public sealed class Inspector
{
    private readonly Func<int> _getThreshold;

    public Inspector(Func<int> getThreshold)
    {
        if (getThreshold == null)
        {
            throw new ArgumentNullException(nameof(getThreshold));
        }

        _getThreshold = getThreshold;
    }

    public bool Inspect(int brightness)
    {
        int threshold = _getThreshold();
        return brightness >= threshold;
    }
}
```

`Bootstrapper`에 아래 메서드를 추가한다.

```csharp
public static Inspector BuildInspector()
{
    return new Inspector(() => SettingsStore.Instance.Threshold);
}
```

```csharp
public static void Run()
{
    SettingsStore store = SettingsStore.Instance;
    store.Threshold = 100;
    Inspector inspector = Bootstrapper.BuildInspector();

    Console.WriteLine("변경 전: " + inspector.Inspect(120));
    store.Threshold = 150;
    Console.WriteLine("변경 후: " + inspector.Inspect(120));

    Inspector test = new Inspector(() => 10);
    Console.WriteLine("테스트: " + test.Inspect(20));
}
```

| 코드 | 역할 |
|---|---|
| `brightness` | 검사할 밝기 |
| `_getThreshold()` | 전달받은 함수를 호출해 기준값 읽기 |
| 지역 변수 `threshold` | 이 검사에서 사용할 기준값을 한 번 읽어 보관 |
| `bool` | 참/거짓을 나타내는 타입 |
| `>=` | 같을 때도 참인 “이상” 비교 |

```text
설정 창고 생성
변경 전: True
변경 후: False
테스트: True
```

**질문 하나:** 검사 부품이 창고를 직접 읽는 대신 함수를 받으면 무엇이 좋은가?

<details><summary>답과 실제 확인</summary>
검사 코드를 바꾸지 않고 기준값을 가져오는 곳을 바꿀 수 있다. 처음에는 설명을 요청했고, 고정값 200을 주는 `new Inspector(() => 200)`을 직접 답했다. 이어 숫자 보관과 함수 보관의 차이를 자기 말로 재설명했다. 직접 실행한 네 줄이 예상과 일치 ✅. 뼈대 작성에서는 `>`를 사용해 경계값에서 달라지는 점을 힌트로 짚었고, 사용자가 `>=`로 수정했다 ✅. 조각 8 작성 중 원본을 다시 본 줄 여부는 별도 보고가 없어 미확인으로 둔다.
</details>

> **Q. `new Inspector(() => store.Threshold)`로 만들면 Bootstrapper와 같은 구조 아닌가?**
>
> 맞다. 현재 예제에서는 같은 설정 창고의 속성을 읽는 함수를 외부에서 전달한다. `store.Threshold = 100`에서 밝기 20은 False, 120은 True다. `() => 10`은 실제 설정 창고 값을 바꾸지 않고 고정값으로 검사할 수 있는 다른 제공 함수다.

> **Q. setter로 기준값을 바꿔도 되나?**
>
> 가능하다. 숫자를 필드에 저장하는 설계라면 외부 설정 변경 후 검사 객체의 setter나 변경 메서드로 새 숫자를 다시 전달해야 한다. 함수를 받는 설계에서는 검사 때 제공 함수를 호출한다. 선택 기준은 줄 수가 아니라 변경 전달 책임과 갱신 누락 가능성이다. 두 설계 모두 목적에 따라 쓸 수 있다.

> **Q. `test.SetThreshold = store.Threshold = 200`을 한 줄 더 써야 해서 Func가 나은가?**
>
> 현재 작성한 `Inspector`에는 `SetThreshold` 속성이 없어 그대로는 컴파일되지 않는다. 현재 함수 주입형 객체들은 `store.Threshold = 200` 뒤 다음 검사에서 200을 읽는다. setter 기반 객체를 가정하면 검사 객체에도 새 숫자를 넣는 작업이 필요하다. 그 가정과 현재 클래스의 실제 API를 구분한다.

> **Q. 결국 `Inspector(int threshold)`와 `Inspector(Func<int> getThreshold)`의 차이인가?**
>
> 맞다. 전자는 대입 당시 숫자를 받아 필드에 저장할 수 있고, 후자는 숫자를 읽는 함수를 받아 보관한다. 비교를 위한 `int` 생성자는 현재 코드에 실제로 추가하지 않았다.

> **Q. Func가 기준값을 계속 감시해 자동 갱신하나?**
>
> `_getThreshold()`를 호출할 때 함수 본문을 실행한다. `() => store.Threshold`라서 그 객체의 현재 속성값을 읽는 것이다. `() => 200`이면 항상 200이다. 함수를 생성했다는 사실만으로 자동 감시나 임의의 최신 설정 조회가 보장되지는 않는다.

> **Q. 지역 변수에 기준값을 보관했다면 setter가 필요한가?**
>
> 검사 객체가 숫자를 “멤버 필드”에 보관한 설계와 구분한다. 현재 `Inspect()`의 지역 변수 `threshold`는 호출 때마다 새로 읽은 숫자다. 필드에 숫자를 오래 보관하는 설계라면 별도의 갱신이 필요하다.

**마무리 뼈대 실습:** 필드와 메서드 껍데기는 유지한 채 생성자·`Inspect()` 본문만 지우고 채운다. 막히면 키워드 표, 해당 원본 한 줄 순서로 확인한다.

```csharp
public sealed class Inspector
{
    private readonly Func<int> _getThreshold;

    public Inspector(Func<int> getThreshold)
    {
        // [if, throw, nameof] null 검사 후 함수를 필드에 보관
    }

    public bool Inspect(int brightness)
    {
        // [함수 호출, >=, return] 기준값을 한 번 읽고 밝기가 이상인지 반환
    }
}
```

이 뼈대는 빈칸 연습용이므로 그대로는 완성 코드가 아니다. 앞의 완성 예제를 먼저 실행한 뒤 사용한다.

> 아래 회사 지도와 설계 참고는 기존 교재에서 유지한 내용이다. 줄 번호는 과거 기준이 포함되어 있고, 이번 완료 검증은 학생의 Kata와 MiniSight를 대상으로 했다. 회사 프로그램 부팅·실장비 연결을 이번 회차에 검증했다고 해석하지 않는다.

## 3. OmniSight 지도

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 비동기 부팅 | `OmniSight/Application/ApplicationBootstrapper.cs:45-54` | `Task.Run(() => Initialize(...))` — 스플래시가 멈추지 않게 |
| 부팅 본체 | `OmniSight/Application/ApplicationBootstrapper.cs:56-261` | 단계별 `Report(progress, "n/6 ...")` |
| AI 동시성 설정 | `OmniSight/Application/ApplicationBootstrapper.cs:151-152` | 입력 수만큼 엔진 풀 (Day18 복선) |
| 장치 생성 | `OmniSight/Application/ApplicationBootstrapper.cs:177` | `DeviceFactory.Create` |
| 실패해도 계속 | `OmniSight/Application/ApplicationBootstrapper.cs:179-185` | 문제를 목록에 쌓고 빈 DeviceManager로 **설정 화면은 열리게** (Null Object) |
| 검사 조립 | `OmniSight/Application/ApplicationBootstrapper.cs:200` | `LegacyOcrInspectionFactory.Create` |
| 소유권 이전 | `OmniSight/Application/ApplicationBootstrapper.cs:210-218` | `devices = null;` "이제 정리는 VisionSystem이" |
| 실패 정리 | `OmniSight/Application/ApplicationBootstrapper.cs:253-260` | `_initialized`가 false면 만든 것 해제 |
| 검사 조립 공장 | `OmniSight/Application/Inspection/LegacyOcrInspectionFactory.cs:19-90` | 부품 생성 → `new InspectionRuntime(...)`, 실패 시 부분 정리 |
| Func 설정 주입 | `OmniSight/Application/Inspection/LegacyOcrInspectionFactory.cs:58` | `() => DataManager.Instance.RecipeRule.GetCurrentReference()` |
| Lazy 싱글톤 | `OmniSight/Configuration/DataManager.cs:18-23` | private 생성자 + `Lazy<DataManager>` |
| 설정 제공자 목록 | `OmniSight/Configuration/DataManager.cs:28-38` | 파일 종류마다 `JsonConfigProvider<T>` |

**집에서 Claude에게:**
```text
ApplicationBootstrapper.cs 56~261줄을 단계별로 잘라서 레고 책상 비유로 설명해줘.
특히 210~218줄 "devices = null" 과 253~260줄 finally가 어떻게 짝을 이루는지,
LegacyOcrInspectionFactory.cs 58줄에서 설정을 Func로 넘기는 이유를 알려줘.
```

## 4. 왜 이렇게 썼나

### 4.1 부팅 흐름

```text
StartupForm(스플래시) ── await ──▶ Bootstrapper.InitializeAsync  (Task.Run: 백그라운드)
  1/6 경로·로그          PathManager.Initialize
  2/6 설정 로드          DataManager.InitializeFixedData...
  3/6 AI 엔진            AiEngineRegistry.Shared.Configure...   (실패해도 기록 후 계속)
  4/6 장치 객체 생성      DeviceFactory.Create                     (실패하면 빈 DeviceManager)
      검사 조립          LegacyOcrInspectionFactory.Create
      총괄 객체          new VisionSystem(devices, runtime)  → devices = null (소유권 이전)
  5/6 장치 초기화         visionSystem.InitializeForConfiguration
  6/6 완료               문제가 있으면 "검사 시작 차단" 상태로 설정 화면만 허용
```

### 4.2 결정과 이유

| 결정 | 이유 |
|---|---|
| 조립 지점 한 곳 | 구체 타입을 아는 곳이 한 곳. 다른 현장은 **조립 코드만** 바꾸면 된다 (`LegacyOcrInspectionFactory` 주석: 다른 검사기는 별도 조립) |
| 부분 실패를 "이슈 목록"으로 | 장비 현장에서는 카메라 하나가 죽어도 **설정 화면은 열려야** 고칠 수 있다. 대신 검사 시작은 막는다 |
| 생성 후 `devices = null` | finally의 실패 정리 코드가 **이미 VisionSystem에 넘긴 장치를 또 Dispose**하지 않게. "로컬 변수를 null로 = 소유권을 넘겼다"는 관용구 |
| `Lazy<T>` 싱글톤 | 기본 모드(`ExecutionAndPublication`)가 스레드 안전 + 처음 쓸 때 생성 |
| `Func<T>`로 설정 전달 | 운전 중 레시피를 저장하면 **다음 프레임부터** 새 값. 클래스는 `DataManager`를 몰라도 됨 → 테스트에서 `() => 테스트설정` 주입 가능 |

### 4.3 싱글톤의 그림자 (면접 단골)

- `DataManager.Instance`를 아무 데서나 부르면 클래스 생성자만 봐서는 의존성을 알 수 없다(숨은 의존성).
- 테스트에서 상태가 전역으로 남아 테스트끼리 간섭한다.
- OmniSight는 그래서 **조립 지점에서만 싱글톤을 읽고**, 부품에는 `Func<T>`로 넘긴다. 실제 코드엔 아직 UI 등에서 직접 `Instance`를 부르는 곳이 남아 있다 → 리팩토링 후보.
- `AiEngineRegistry`는 `Shared` 싱글톤이지만 **생성자가 public**이라 테스트용 인스턴스를 따로 만들 수 있다. 좋은 절충안.

### 4.4 왜 DI 컨테이너(Autofac 등)를 안 썼나

장치 수가 적고 수명 규칙(시작 순서, 역순 정지, 실패 정리)이 까다로워서 **손으로 조립(Pure DI)** 하는 게 더 읽기 쉽고 디버깅이 쉽다. 컨테이너는 등록이 수백 개가 되거나 웹처럼 요청 범위 수명이 필요할 때 효과가 크다.

## 5. MiniSight 적용 · 단일 카메라 설정과 조립

이번 회차는 `.NET Framework 4.7.2 / C# 7.3 / WinForms`의 기존 코드를 재사용했다. 학생이 VS2022에서 직접 입력했고, 강사는 학생 소스를 대신 수정하지 않았다. 각 단계마다 Debug/x64로 빌드하고 button9를 실행했다. 아래는 실습을 마친 최종 형태다.

### 5-1. 생성 위치를 Bootstrapper로 옮기기

MiniSight 프로젝트에 `App` 폴더와 `Bootstrapper.cs` 클래스를 추가했다. 처음에는 기존 `CameraFactory.Create(CameraKind.Fake, "연습 카메라", null, 300)` 호출만 옮겼다. 버튼은 반환된 `IImageSource`를 사용하고 기존 `using`으로 정리했다. 이후 설정 읽기까지 이 조립 지점에 모았다. `Program.Main`의 시작·단일 실행·전역 예외 처리 코드는 변경하지 않았다.

### 5-2. 카메라 설정 상자 CameraConfig

MiniSight 프로젝트에 `Config` 폴더와 `CameraConfig.cs` 클래스를 추가한다.

```csharp
using MiniSight.Devices.Cameras;

namespace MiniSight.Config
{
    public sealed class CameraConfig
    {
        public CameraKind Kind { get; set; }
        public string Name { get; set; }
        public string Folder { get; set; }
        public int IntervalMs { get; set; }
    }
}
```

| 속성 | 설정 |
|---|---|
| `Kind` | 카메라 종류 |
| `Name` | 카메라 이름 |
| `Folder` | 폴더 카메라의 입력 경로. 이번 Fake에서는 null |
| `IntervalMs` | 밀리초 단위 촬영 간격. 500ms는 0.5초 |

처음에는 `BuildCamera()`에서 이 객체에 직접 값을 담고 기존 공장에 전달했다. 사용자는 촬영 간격 500ms로 바꾸는 줄을 `config.IntervalMs = 500;`으로 답했고, 직접 실행한 세 결과가 모두 True임을 확인했다.

### 5-3. 공유 설정 창고 ConfigStore

`Config/ConfigStore.cs`의 최종 코드다. `Lazy` 생성, 현재 설정 보관, JSON 저장·읽기를 순서대로 추가했다.

VS2022에서 MiniSight 프로젝트의 **참조 우클릭 → 참조 추가 → 어셈블리 → 프레임워크 → System.Runtime.Serialization 체크**로 기존 Framework 어셈블리를 참조한다. 외부 NuGet 패키지 설치나 기존 패키지·Framework 업그레이드는 하지 않았다.

```csharp
using System;
using System.IO;
using System.Runtime.Serialization.Json;
using MiniSight.Devices.Cameras;

namespace MiniSight.Config
{
    public sealed class ConfigStore
    {
        private static readonly Lazy<ConfigStore> _instance =
            new Lazy<ConfigStore>(() => new ConfigStore());

        public static ConfigStore Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        public CameraConfig Current { get; private set; }

        private ConfigStore()
        {
            Current = new CameraConfig();
            Current.Kind = CameraKind.Fake;
            Current.Name = "연습 카메라";
            Current.Folder = null;
            Current.IntervalMs = 500;
        }

        public void Save(string path)
        {
            DataContractJsonSerializer serializer =
                new DataContractJsonSerializer(typeof(CameraConfig));

            using (FileStream stream = File.Create(path))
            {
                serializer.WriteObject(stream, Current);
            }
        }

        public void Load(string path)
        {
            if (!File.Exists(path))
            {
                Save(path);
                return;
            }

            DataContractJsonSerializer serializer =
                new DataContractJsonSerializer(typeof(CameraConfig));

            using (FileStream stream = File.OpenRead(path))
            {
                CameraConfig loaded = (CameraConfig)serializer.ReadObject(stream);
                if (loaded == null || loaded.IntervalMs <= 0)
                {
                    throw new InvalidDataException("카메라 설정과 촬영 간격을 확인하세요.");
                }

                Current = loaded;
            }
        }
    }
}
```

| 코드 | 역할 / 사용법 |
|---|---|
| `Instance` | 공유 설정 창고 객체. 첫 접근 때 만들고 재사용 |
| `Current` | 창고가 보관한 카메라 설정 객체 |
| `private set` | Current에 다른 객체를 넣는 대입은 ConfigStore 안에서만 허용. 설정 객체의 속성은 읽고 쓸 수 있음 |
| `DataContractJsonSerializer` | 설정 객체와 JSON을 서로 변환하는 내장 도구 |
| `typeof(CameraConfig)` | 객체가 아니라 변환 대상 타입 정보를 전달 |
| `File.Create(path)` | 파일 생성. 이미 있으면 기존 내용을 비움 |
| `WriteObject(stream, Current)` | 현재 설정을 JSON으로 직렬화해 기록 |
| `ReadObject(stream)` | JSON을 읽어 객체로 역직렬화 |
| `(CameraConfig)` | 반환된 객체를 카메라 설정 타입으로 다룸 |
| `!File.Exists(path)` | 파일이 없으면 참. 현재 설정을 저장하고 종료 |
| `||` | 둘 중 하나가 참이면 참. 앞이 참이면 뒤의 속성 검사를 생략 |

**실습 순서**

1. 저장 메서드를 먼저 만들었다. 당시 button9에서 `Save(path)` 후 `File.ReadAllText(path)`를 표시해 실제 파일 내용을 확인했다.
2. 실행 파일 폴더의 `camera-study.json`에 다음 내용이 저장되는 것을 사용자가 확인했다.

```json
{"Folder":null,"IntervalMs":500,"Kind":0,"Name":"연습 카메라"}
```

3. JSON의 `IntervalMs`만 700으로 수정·저장하고 `Load(path)`를 호출했다. 사용자는 `읽어온 촬영 간격: 700ms`를 확인했다.
4. 연습 파일의 간격을 0으로 바꾸자 설정 오류 안내 후 촬영 결과 창이 뜨지 않는 것을 사용자가 확인했다. 관찰 후 700으로 복구하도록 안내했다.

`Kind`의 0은 `CameraKind.Fake`다. 저장 후 읽기로 바꿀 때 버튼의 기존 `Save(path)` 호출을 제거했다. 매번 읽기 전에 저장하면 파일에서 수정한 값을 덮어쓰게 된다.

> **Q. private set이면 설정값을 바꿀 수 없나?**
>
> `Current = 다른설정객체`라는 대입을 ConfigStore 내부로 제한한다. 반환된 설정 객체 전체를 불변으로 만들거나 동시 변경을 자동 보호하는 기능은 아니다. 이번 회차에서는 파일 읽기·검증을 마친 뒤 내부에서 Current를 교체한다.

JSON 변환 참고: [Microsoft의 Framework 직렬화·역직렬화 예제](https://learn.microsoft.com/dotnet/framework/wcf/feature-details/how-to-serialize-and-deserialize-json-data).

### 5-4. 설정 읽기 → 카메라 생성 순서 모으기

`App/Bootstrapper.cs`의 최종 코드다.

```csharp
using MiniSight.Config;
using MiniSight.Devices;
using MiniSight.Devices.Cameras;

namespace MiniSight.App
{
    public static class Bootstrapper
    {
        public static IImageSource BuildCamera(string configPath)
        {
            ConfigStore.Instance.Load(configPath);
            CameraConfig config = ConfigStore.Instance.Current;

            return CameraFactory.Create(
                config.Kind, config.Name, config.Folder, config.IntervalMs);
        }
    }
}
```

설정을 읽고 구체 장치를 고르는 순서를 조립 지점에서 관리한다. 사용자는 카메라에 들어갈 설정 데이터가 파일에서 관리되므로 먼저 읽어야 한다고 설명했다 ✅.

**중요한 구분:** 이 카메라 공장은 생성할 때 설정값을 인수로 받는다. `Current`를 나중에 바꾸는 것만으로 이미 만든 카메라의 주기가 자동 변경되지는 않는다. 조각 8의 `Func<int>`로 검사 때마다 읽는 구조와 구분한다.

### 5-5. 버튼은 반환된 카메라를 사용하고 정리하기

폼 디자이너에서 button9를 더블클릭한다. 코드 파일의 기존 using에 `MiniSight.App`, `MiniSight.Config`, `System.IO`가 있어야 한다. 기존 `MiniSight.Devices`, `OpenCvSharp`, `System`, `System.Threading`, `System.Windows.Forms`도 사용한다.

```csharp
private async void button9_Click(object sender, EventArgs e)
{
    string path = Path.Combine(Application.StartupPath, "camera-study.json");
    IImageSource camera;

    try
    {
        camera = Bootstrapper.BuildCamera(path);
    }
    catch (Exception ex)
    {
        MessageBox.Show("카메라 준비 실패: " + ex.Message);
        return;
    }

    using (camera)
    {
        MessageBox.Show("읽어온 촬영 간격: "
            + ConfigStore.Instance.Current.IntervalMs + "ms");

        camera.Initialize(CancellationToken.None);
        camera.Start(CancellationToken.None);

        ImageFrame frame = await camera.WaitFrameAsync(CancellationToken.None);
        Mat borrowed = frame.Image;
        string inside;

        using (frame)
        {
            camera.Stop();
            inside = "블록 안 이미지 있음: " + (frame.Image != null);
        }

        MessageBox.Show(inside
            + Environment.NewLine + "블록 밖 이미지 없음: " + (frame.Image == null)
            + Environment.NewLine + "빌린 Mat 정리됨: " + borrowed.IsDisposed);
    }
}
```

| 코드 | 역할 |
|---|---|
| `Application.StartupPath` | 실행 파일이 있는 폴더 |
| `Path.Combine(...)` | 폴더와 파일 이름을 연결 |
| `IImageSource camera;` | try 뒤에서도 사용할 변수를 먼저 선언 |
| `catch`의 `return` | 준비 실패 시 뒤의 생성 결과 사용·촬영을 건너뜀 |
| `using (camera)` | 성공해서 받은 카메라의 정리 책임을 버튼 범위가 가짐 |

파일을 700으로 복구한 상태에서 **MiniSight를 시작 프로젝트로 설정 → Debug/x64 → Ctrl+Shift+B → Ctrl+F5 → button9**로 실행한다.

먼저 표시되는 메시지:

```text
읽어온 촬영 간격: 700ms
```

촬영 뒤 표시되는 결과:

```text
블록 안 이미지 있음: True
블록 밖 이미지 없음: True
빌린 Mat 정리됨: True
```

사용자가 마지막 구조로 바꾼 뒤 700ms 안내와 세 줄 True를 직접 확인했다 ✅. 이 메시지는 설정값 전달·프레임 정리의 관찰 결과이며 실제 촬영 간격의 정밀도, UI 연속 클릭, 실카메라 연결까지 검증한 것은 아니다.

### 5-6. 한곳에 모으는 이유 재확인

> **Q. 한 시스템에서 관리하니까 편한 것 아닌가?**
>
> 구체적으로는 장치를 교체할 때 수정할 위치가 줄어든다. 처음에는 이유 설명을 보충했고, 사용자는 “구체적인 장치의 생성과 연결을 한곳에 모으면 장치 교체 때 수정할 곳이 줄어든다”고 다시 설명했다. 선풍기를 헤어드라이기로 바꿀 때 Show가 아니라 Build만 바꾸면 된다는 재답도 맞았다 ✅.

> **Q. 설정 읽기에 실패해도 이전 설정이나 기본값으로 계속 촬영하면?**
>
> 사용자는 검사 결과가 이상해지거나 전반적인 알 수 없는 문제가 생길 수 있다고 답했다 △. 사용자가 의도한 설정과 다른 조건으로 촬영해 겉으로 정상처럼 보여도 결과를 신뢰하기 어렵다는 구체적인 이유를 보충했다. 준비를 중단하는 코드와 실제 오류 경로 관찰은 확인했고, 이 한 문장 이유는 Day06에서 재확인한다.

## 6. 확인한 범위와 다음 단원으로 넘긴 범위

2026-11-06, 약 70% 기준으로 Day05의 핵심 기본 목표를 완료했다. 조각 2는 원본을 보지 않고 뼈대 작성했다고 확인했다. Inspector 뼈대는 `>`를 `>=`로 고쳐 완성했다. 다른 모든 조각의 세 번째 타이핑이나 원본을 본 줄 여부까지 완료했다고 확대하지 않는다.

**실제 학생 코드 검증**

- Katas와 MiniSight를 별도 검증 출력 폴더에 C# 7.3 기준으로 빌드했다. MiniSight는 Debug/x64, Katas는 기존 AnyCPU 구성을 유지한 채 검증 빌드의 PlatformTarget을 x64로 지정했다. 학생 프로젝트 설정을 바꾸지 않았다.
- 실제 `Katas.Day05.PowerDisplay.KataDay05.Run()`을 호출해 최종 콘솔 출력 네 줄이 정확히 일치하는 것을 확인했다.
- 실제 `ConfigStore`에서 파일 없음 시 현재 설정 저장, 700 읽기, JSON null·촬영 간격 0 거부, 잘못된 값일 때 기존 설정 객체 보존, 현재 700 재저장을 확인했다. 검증 파일은 임시 폴더에만 만들었다.
- WinForms의 정상 출력과 오류 시 촬영 중단은 사용자 직접 실행 보고다. 강사가 UI를 직접 클릭한 검증이나 실장비 시험과 구분한다.

**미구현·미검증으로 남김**

- 이번 MiniSight 구현은 단일 카메라와 static Bootstrapper다. 원래 계획의 `SystemConfig` 카메라 목록·ImageSaveRoot, DeviceManager·VisionSystem, IDisposable Bootstrapper의 다중 자원 역순 정리·이슈 목록은 완료하지 않았다. Day06 장치 묶음·총괄 조립과 연결한다.
- `InitializeAsync`, `Task.Run`, `IProgress<string>` 진행 안내, 느린 초기화·취소·동시 실행 보호는 이번 코드에 추가하지 않았다. 비동기·취소 단원에서 연결한다.
- `Lazy` 기본 모드는 ExecutionAndPublication이지만 동시 접근 시험, 공유 Current 변경의 동기화, 원자적 파일 교체는 이번 회차에서 검증하지 않았다. 동시성·설정 저장 단원에서 다룬다.
- 현재 Load의 데이터 검사는 null과 양수 촬영 간격까지다. 이름·종류·폴더·장비별 범위의 전체 검증, 파일 잠김·권한·디스크 장애, 실제 장비 초기화 실패 정리, 반복 클릭, 정확한 촬영 주기는 별도 확인이 필요하다.
- `Save`는 기존 파일을 직접 덮어쓰는 학습 코드다. 임시 파일 후 교체하는 원자적 저장·복구는 Day23으로 넘긴다.
- 회사 부팅 6단계의 암기·스플래시 실행, 아래 선택 디버깅, DI 컨테이너 비교 실습은 완료 처리하지 않았다.

## 7. 선택 디버깅 · 아직 실행하지 않은 것

1. `BuildCamera` 첫 줄에 F9 → F5 → F11로 Load에 들어가 JSON의 700이 Current로 들어오는 순서를 본다.
2. 잘못된 간격 0에서 throw 순간에 멈추고, catch의 return 뒤 카메라 생성으로 가지 않는지 본다. 관찰 뒤 700으로 복구한다.
3. ConfigStore 생성자에 중단점을 두고 button9를 여러 번 눌렀을 때 생성이 한 번인지 본다.
4. 생성 전 상태를 보려면 `_instance.IsValueCreated`를 확인한다. `Instance`를 조사식에서 읽으면 그 관찰 자체가 생성할 수 있음을 구분한다.

위 목록은 안내용이며 실제 수행 보고를 받지 않았다.

## 8. 셀프 체크

1. 생성과 연결을 Bootstrapper에 모으면 장치 교체가 왜 편한가?

<details><summary>답</summary>
구체 장치 선택과 연결을 바꾸는 위치가 모이고, 공통 계약으로 사용하는 코드는 유지할 수 있기 때문이다. 모든 객체 생성을 한 파일에 강제로 모으라는 뜻은 아니다. 사용자 재설명 통과 ✅.
</details>

2. 숫자를 받는 생성자와 Func를 받는 생성자는 무엇이 다른가?

<details><summary>답</summary>
숫자는 대입 당시 값을 보관한다. 함수는 호출할 때 본문을 실행한다. 현재의 `() => store.Threshold`라서 호출 시 그 속성값을 읽으며, `() => 200`이면 항상 200이다. 사용자가 setter와 필드 갱신까지 질문하고 자기 말로 구분했다 ✅.
</details>

3. `buffer = null` 뒤 반환된 객체가 살아 있는 이유는?

<details><summary>답</summary>
result가 같은 객체의 참조를 보관하고 있고, buffer라는 지역 변수만 비웠기 때문이다. 정리는 호출자가 using으로 맡는다. 사용자 설명·실행 통과 ✅.
</details>

4. 밝기가 기준값과 같을 때 통과해야 한다면 비교 기호는?

<details><summary>답</summary>
`>=`. 뼈대에서 `>`를 썼다가 사용자가 직접 고쳤다 ✅.
</details>

5. 설정 파일을 먼저 읽고, 실패하면 준비를 중단하는 이유는?

<details><summary>답</summary>
파일에서 읽은 의도한 값으로 장치를 생성해야 한다. 실패 뒤 이전 설정이나 기본값으로 계속 진행하면 사용자가 의도한 조건과 실제 조건이 달라질 수 있다. 읽기 순서 설명과 실패 경로 실행은 통과했고, 오류 시 중단 이유의 구체적인 한 문장 설명은 보충 후 Day06에서 재확인한다 △.
</details>
