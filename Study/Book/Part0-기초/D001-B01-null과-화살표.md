# D001 · B01 null과 화살표 (`null` `?.` `??` `=>` `nameof`)

> 레벨 L0 · 지옥 레벨 ★1 · 약 2시간 · Kata 파일: 포트폴리오 `Katas/L0/D001.cs`
> 상태: **대기** (사용자가 시작하면 답과 막힌 곳을 아래 "기록"에 적는다)

## 1. 오늘 할 수 있게 되는 것

1. `null`이 무엇인지, 왜 `NullReferenceException`이 나는지 말할 수 있다.
2. `?.`로 "있으면 묻고, 없으면 그냥 null"인 코드를 쓴다.
3. `??`로 "비었으면 대신 이것"을 한 줄로 쓴다.
4. `=>`로 한 줄짜리 속성과 메서드를 쓴다.
5. `nameof`로 이름을 글자로 바꿔서, 이름이 바뀌어도 오타가 안 나게 한다.

## 2. 🧒 비유

- **변수 = 상자, 객체 = 상자 안의 장난감.** `null`은 "상자는 있는데 안이 비었다"는 뜻이다. 빈 상자에 "장난감 길이가 몇 cm야?"라고 물으면 대답할 게 없어서 프로그램이 터진다.
- `?.`는 "상자를 먼저 열어 보고, 비었으면 묻지 말고 '없음'이라고 적어" 하는 조심스러운 친구다.
- `??`는 "비었으면 대신 이걸 줘" 하는 예비 장난감이다.

## 3. 본문

### 3-1. null은 무엇인가

C#의 `string`, `Camera` 같은 **참조 형식** 변수는 객체 자체가 아니라 "객체가 있는 곳의 주소"를 담는다. `null`은 "가리키는 곳이 없다"는 특별한 주소다. 변수는 있지만 객체가 없다.

`int`, `double`, `bool` 같은 **값 형식**은 값 자체를 담기 때문에 원래 `null`이 될 수 없다. 값 형식에 "없음"을 표현하고 싶을 때는 `int?`(= `Nullable<int>`)를 쓴다. `int?`는 `HasValue`(값이 있나)와 `Value`(그 값)를 가진 작은 상자다.

### 3-2. NullReferenceException은 언제 나는가

`null`인 변수에 대고 `.`으로 멤버(속성·메서드)를 부르는 순간 난다. `cameraName.Length`에서 `cameraName`이 `null`이면, CLR은 존재하지 않는 객체의 `Length`를 읽을 수 없어서 예외를 던진다.

현장에서 이 예외는 가장 흔한 장애 원인 중 하나다. 특히 다음 세 곳에서 자주 난다.

1. **정리 코드** — `Start()`를 안 부르고 `Stop()`을 부르면 아직 만들지 않은 스레드나 타이머가 `null`이다. 창을 닫을 때 프로그램이 죽는다.
2. **설정 파일** — JSON에 항목이 빠지면 역직렬화된 속성이 `null`이다.
3. **이벤트** — 아무도 구독하지 않은 이벤트는 `null`이다(B04에서 다룬다).

### 3-3. `?.` 널 조건 연산자

`a?.B`는 이렇게 동작한다: `a`가 `null`이면 `B`를 부르지 않고 전체 결과를 `null`로 한다. `a`가 `null`이 아니면 평소처럼 `a.B`를 부른다.

결과가 값 형식이면 `null`을 담을 수 있게 자동으로 `?`가 붙는다. 그래서 `cameraName?.Length`의 형식은 `int`가 아니라 `int?`다.

`?.`는 이어서 쓸 수 있다. `camera?.Settings?.Exposure`는 중간 어디서든 `null`이면 거기서 멈춘다. 이것을 "단락(short-circuit)"이라고 한다.

### 3-4. `??` 널 병합 연산자

`a ?? b`는 "`a`가 `null`이 아니면 `a`, `null`이면 `b`"다. `?.`와 함께 쓰면 "없으면 기본값"을 한 줄로 쓸 수 있다.

```csharp
int length = model?.Length ?? -1;   // model이 null이면 -1
```

`??=`(C# 8)는 이 책의 범위(C# 7.3) 밖이라 쓰지 않는다. 같은 뜻은 `if (x == null) { x = 기본값; }`로 쓴다.

### 3-5. `=>` 식 본문 멤버

메서드나 읽기 전용 속성의 몸통이 **식 하나**라면 `{ return ...; }` 대신 `=> 식;`으로 쓸 수 있다.

```csharp
public string Info => Name + " / 노출 " + ExposureUs + "us";   // 속성 (get만)
public int Double(int value) => value * 2;                    // 메서드
```

`=>`는 람다(B03)에서도 쓰이지만 여기서는 "몸통이 한 줄"이라는 뜻이다. 몸통이 두 줄 이상이면 쓰지 않는다. 읽기 쉬움이 목적이다.

### 3-6. `nameof`

`nameof(x)`는 컴파일할 때 `x`의 **이름을 문자열**로 바꾼다. `nameof(name)`은 `"name"`이 된다. 왜 그냥 `"name"`이라고 쓰지 않을까? 나중에 변수 이름을 `cameraName`으로 바꾸면 문자열 `"name"`은 그대로 남아 거짓말을 한다. `nameof`는 이름이 바뀌면 같이 바뀌고, 오타가 나면 **컴파일 에러**가 난다. 예외 메시지·로그·속성 변경 알림에 쓴다.

### 3-7. 흔한 실수

| 실수 | 결과 | 바르게 |
|---|---|---|
| `?.`를 붙였는데 결과를 `int`에 바로 담음 | 컴파일 에러(CS0266, `int?`를 `int`로 바꿀 수 없음) | `int?`에 담거나 `?? 기본값` |
| 모든 곳에 `?.`를 남발 | 진짜로 `null`이면 안 되는 곳의 버그를 숨긴다 | `null`이 정상인 곳에만 `?.`, 안 되는 곳은 생성자에서 검사하고 예외 |
| 문자열로 이름을 적음 `"name"` | 이름을 바꾸면 메시지가 틀린다 | `nameof(name)` |

## 4. 손코딩 조각

`Katas/L0/D001.cs`에 아래 조각을 순서대로 추가하고 `Run()`이 차례로 부르게 한다. `Program.cs`의 `Main`에서는 `Katas.L0.D001.Run();`을 부른다. 아래 출력은 C# 7.3 / .NET 4.7.2로 직접 컴파일해서 확인했다.

### 조각 1 · null이면 왜 터지나

```csharp
public static void Chunk1()
{
    string cameraName = null;
    Console.WriteLine("비었나? " + (cameraName == null));
    try
    {
        Console.WriteLine(cameraName.Length);
    }
    catch (NullReferenceException ex)
    {
        Console.WriteLine("터짐: " + ex.GetType().Name);
    }
}
```

| 키워드 | 역할 | 같은 뜻의 긴 코드 |
|---|---|---|
| `null` | 가리키는 객체가 없음 | |
| `try` / `catch (형식 ex)` | 예외를 잡아서 프로그램이 죽지 않게 함 (B05에서 깊게) | |
| `ex.GetType().Name` | 예외 형식 이름 | |

출력:
```text
비었나? True
터짐: NullReferenceException
```

- **실험:** `try/catch`를 지우고 실행해 보라. 프로그램이 어디서 멈추는지, 예외 창의 메시지를 읽어 본다. 다시 되돌린다.
- **질문:** `cameraName.Length`에서 예외가 나는 이유를 "무엇을 → 그래서"로 말하라.

### 조각 2 · `?.` 있으면 묻고, 없으면 null

```csharp
public static void Chunk2()
{
    string cameraName = null;
    int? length = cameraName?.Length;
    Console.WriteLine("길이 있음? " + length.HasValue);

    cameraName = "CAM1";
    length = cameraName?.Length;
    Console.WriteLine("길이: " + length);
}
```

| 키워드 | 역할 | 같은 뜻의 긴 코드 |
|---|---|---|
| `?.` | 앞이 null이면 멈추고 null | `cameraName == null ? (int?)null : cameraName.Length` |
| `int?` | 값이 없을 수도 있는 int | `Nullable<int>` |
| `.HasValue` | 값이 있나 | `length != null` |

출력:
```text
길이 있음? False
길이: 4
```

- **실험:** `int? length`를 `int length`로 바꿔 컴파일해 보라. 어떤 에러 번호가 나오는지 적는다.
- **질문:** `cameraName?.Length`의 결과 형식이 왜 `int`가 아니라 `int?`인가?

### 조각 3 · `??` 비었으면 대신 이것

```csharp
public static void Chunk3()
{
    string fromConfig = null;
    string name = fromConfig ?? "기본카메라";
    Console.WriteLine("이름: " + name);

    string model = null;
    int length = model?.Length ?? -1;
    Console.WriteLine("모델 길이: " + length);
}
```

| 키워드 | 역할 | 같은 뜻의 긴 코드 |
|---|---|---|
| `??` | 앞이 null이면 뒤 값 | `fromConfig != null ? fromConfig : "기본카메라"` |

출력:
```text
이름: 기본카메라
모델 길이: -1
```

- **실험:** `fromConfig = "CAM2";`로 바꾸면 무엇이 찍히나?
- **질문:** `model?.Length ?? -1`에서 `?? -1`이 없으면 왜 `int length`에 담을 수 없나?

### 조각 4 · `=>` 한 줄짜리 몸통

조각 4~6은 아래 `Camera` 클래스를 같이 쓴다. `D001` 클래스 밖(같은 namespace)에 둔다.

```csharp
public class Camera
{
    public string Name { get; }
    public int ExposureUs { get; }

    public Camera(string name, int exposureUs)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }
        Name = name;
        ExposureUs = exposureUs;
    }

    public string Info => Name + " / 노출 " + ExposureUs + "us";

    public int Double(int value) => value * 2;
}
```

```csharp
public static void Chunk4()
{
    Camera cam = new Camera("CAM1", 1200);
    Console.WriteLine(cam.Info);
    Console.WriteLine("2배 노출: " + cam.Double(cam.ExposureUs));
}
```

| 키워드 | 역할 | 같은 뜻의 긴 코드 |
|---|---|---|
| `{ get; }` | 생성자에서만 값을 넣을 수 있는 읽기 전용 속성 | `private readonly` 필드 + get 속성 |
| `=> 식;` (속성) | 읽을 때마다 식을 계산 | `public string Info { get { return Name + ...; } }` |
| `=> 식;` (메서드) | 몸통이 식 하나인 메서드 | `public int Double(int value) { return value * 2; }` |

출력:
```text
CAM1 / 노출 1200us
2배 노출: 2400
```

- **실험:** `Info`를 긴 코드(`get { return ...; }`)로 바꿔 써도 출력이 같은지 확인한다.
- **질문:** `Info`를 필드가 아니라 `=>` 속성으로 만들면, 나중에 `ExposureUs`가 바뀌는 클래스였을 때 무엇이 좋은가?

### 조각 5 · `nameof` 이름을 글자로

```csharp
public static void Chunk5()
{
    try
    {
        Camera cam = new Camera(null, 1000);
    }
    catch (ArgumentNullException ex)
    {
        Console.WriteLine("빠진 값: " + ex.ParamName);
    }
    Console.WriteLine("속성 이름: " + nameof(Camera.ExposureUs));
}
```

| 키워드 | 역할 | 같은 뜻의 긴 코드 |
|---|---|---|
| `nameof(x)` | x의 이름을 문자열로 (컴파일 때) | `"x"` (단, 이름이 바뀌면 틀림) |
| `ArgumentNullException` | "넘어온 인자가 null"이라는 표준 예외 | |
| `ex.ParamName` | 어느 인자가 문제인지 | |

출력:
```text
빠진 값: name
속성 이름: ExposureUs
```

- **실험:** 생성자 매개변수 이름을 `name` → `cameraName`으로 바꾼다(VS에서 F2 이름 바꾸기). `nameof`가 따라 바뀌고 출력이 `빠진 값: cameraName`이 되는지 본다.
- **질문:** 현장 로그에 "빠진 값: name"이 찍혔다. 이것이 문자열 `"name"`이 아니라 `nameof`로 찍혔다는 게 왜 믿을 만한가?

### 조각 6 · 넷을 한 번에

```csharp
public static void Chunk6()
{
    Camera[] cameras = { new Camera("CAM1", 1200), null };
    foreach (Camera cam in cameras)
    {
        string text = cam?.Info ?? "카메라 없음";
        Console.WriteLine(text);
    }
}
```

출력:
```text
CAM1 / 노출 1200us
카메라 없음
```

- **실험:** `cam?.Info`에서 `?`를 지우고 실행하면 두 번째 줄에서 무슨 일이 생기나?
- **질문:** 배열에 `null`이 섞일 수 있는 현장 상황을 하나 들어 보라(예: 설정에서 카메라 2대 중 1대가 빠짐).

### 전체 출력 (조각 1~6을 차례로)

```text
비었나? True
터짐: NullReferenceException
길이 있음? False
길이: 4
이름: 기본카메라
모델 길이: -1
CAM1 / 노출 1200us
2배 노출: 2400
빠진 값: name
속성 이름: ExposureUs
CAM1 / 노출 1200us
카메라 없음
```

## 5. 3번 타이핑

1. 위 코드를 보면서 친다.
2. 출력을 위 "전체 출력"과 글자·공백 단위로 비교한다. (`길이: 4`의 콜론 뒤 공백 1칸, `1200us` 사이 공백 없음)
3. 파일 내용을 지우고 아래 뼈대만 보고 다시 친다. 막히면 키워드 표 → 원본의 그 한 줄만 본다. 본 줄은 기록에 적는다.

```csharp
namespace Katas.L0
{
    public static class D001
    {
        public static void Run() { /* Chunk1~6 차례로 */ }
        // [null, try/catch] 빈 이름의 Length → "비었나? True", "터짐: NullReferenceException"
        // [?., int?] 빈 이름/CAM1 길이 → "길이 있음? False", "길이: 4"
        // [??] 설정 없음 → "이름: 기본카메라", 모델 없음 → "모델 길이: -1"
        // [=>] CAM1 1200 → "CAM1 / 노출 1200us", "2배 노출: 2400"
        // [nameof] null 이름 → "빠진 값: name", "속성 이름: ExposureUs"
        // [?. + ??] 카메라 배열 → "CAM1 / 노출 1200us", "카메라 없음"
    }
    // [{ get; }, =>, nameof] Camera 클래스: Name, ExposureUs, 생성자 null 검사, Info, Double
}
```

## 6. 현장 연결

- **장비 SW:** 정리 메서드는 늘 `_timer?.Dispose();`, `_thread?.Join();`처럼 쓴다. `Start` 전에 `Stop`이 불리거나 `Stop`이 두 번 불려도 죽지 않아야 한다. 반대로 "없으면 안 되는 값"(카메라 이름, 레시피 경로)은 생성자에서 `ArgumentNullException(nameof(...))`로 일찍 터뜨린다. 늦게 터질수록 원인 찾기가 어렵다.
- **TS:** 현장 로그에 `NullReferenceException`이 찍히면 스택 추적의 **맨 위 줄**(어느 메서드 몇 번째 줄)을 먼저 본다. 그 줄에서 `.` 앞에 있는 변수 중 무엇이 `null`일 수 있는지 따져 본다. 설정 누락인지, 순서 문제(초기화 전 호출)인지를 나눈다.
- **산업별:** 산업과 관계없이 같다. 다만 설정 항목이 많은 현장(레시피 수백 개)일수록 "빠진 항목 → null → 늦게 터짐" 사고가 잦다. J07에서 설정 검증으로 이어진다.

## 7. 셀프 체크

1. `string s = null; int? n = s?.Length;` 뒤에 `n.HasValue`는?
2. `a ?? b ?? c`는 어떻게 읽나?
3. `public int Area => W * H;`를 긴 코드로 쓰라.
4. `nameof`를 쓰는 이유 두 가지는?
5. `Stop()` 안에서 `_timer.Dispose();`를 쓰면 어떤 상황에서 죽나? 어떻게 고치나?

<details>
<summary>정답</summary>

1. `False`. `s`가 null이라 `?.`가 멈추고 `n`은 값이 없다.
2. 왼쪽부터: `a`가 null이 아니면 `a`, 아니면 `b`가 null이 아니면 `b`, 둘 다 null이면 `c`.
3. `public int Area { get { return W * H; } }`
4. 이름이 바뀌면 같이 바뀐다(거짓 메시지 방지), 오타가 컴파일 에러로 잡힌다.
5. `Start()`를 부르기 전에 `Stop()`을 부르거나(타이머가 아직 null) `Stop()`을 두 번 부를 때 NullReferenceException. `_timer?.Dispose(); _timer = null;`
</details>

### 면접 질문

1. NullReferenceException이 현장에서 났을 때 어떤 순서로 원인을 찾나요? → 꼬리: 스택 추적이 catch 줄만 가리키면 무엇을 의심하나요? (`throw ex;`로 다시 던짐 — B05)
2. `?.`를 모든 곳에 쓰면 안 되는 이유는? → 꼬리: 그럼 null이면 안 되는 값은 어떻게 다루나요?
3. 값 형식과 참조 형식의 차이와 `int?`가 필요한 이유는?

## 8. 블로그 요약

- 제목 후보: "장비 프로그램이 창을 닫다가 죽는 이유: null과 ?. 하나"
- 핵심 그림: 상자(변수)와 장난감(객체), 빈 상자에 질문하면 터진다
- 핵심 코드: 조각 6 (`cam?.Info ?? "카메라 없음"`)
- 실험 결과: `?` 하나를 지우면 두 번째 카메라에서 NullReferenceException

## 9. 기록

| 항목 | 내용 |
|---|---|
| 진행 날짜 | |
| 조각 1~6 질문 답 | |
| 3번째 타이핑에서 본 줄 | |
| 지옥 대기열로 간 것 | |
| 곁가지 질문 | |
