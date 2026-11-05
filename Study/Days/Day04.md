# Day 04 (목, 2h) · Factory와 Adapter

## 1. 오늘 목표

- **완료 기준**
  - "설정값 → 어떤 카메라 객체를 만들지"를 **한 곳**에서 결정하는 이유를 말할 수 있다 (Simple Factory).
  - 벤더 SDK마다 다른 API를 같은 `IImageSourceDevice` 모양으로 맞추는 구조를 그림으로 그렸다 (Adapter).
  - `Func<ICameraDriver>`를 생성자로 받는 이유(재연결 때마다 새 드라이버)를 설명할 수 있다.
  - MiniSight: `ICameraDriver`, `FakeCameraDriver`, `DriverCameraAdapter`, `FolderCamera`, `CameraFactory` 작성.
- **최소분량**: 2, 3, 4, 6의 ICameraDriver·FakeCameraDriver·CameraFactory
- **확장분량**: FolderCamera, 디버깅 실습 4~5

## 2. 🧒 초1 비유

- **Adapter = 해외여행용 돼지코**. 한국 플러그(검사기가 원하는 모양)를 미국 콘센트(VendorA SDK), 일본 콘센트(VendorB SDK)에 꽂으려면 돼지코가 필요하다. 돼지코 안에서 모양만 바꿔 준다. 검사기는 돼지코가 있는지도 모른다.
- **Factory = 주문 받는 빵집 카운터**. 손님(부트스트래퍼)은 "크림빵 주세요"(설정: `VendorType = VendorA`)라고만 말한다. 빵을 어떻게 굽는지는 카운터 뒤 주방이 안다. 새 빵이 생겨도 손님은 메뉴 이름만 알면 된다.

## 2-1. 2회차 조각별 실습 (2026-11-05 시작)

Day03 완료 뒤 Day04를 시작한다. 먼저 작은 코드를 손으로 실행하고 회사 코드는 뒤에 비교한다. 기존 아래 지도·Kata는 참고 자료다. 진행 계획은 ①객체 생성을 Factory로 모으기 ②enum/switch로 종류 선택과 지원하지 않는 값 처리 ③다른 API를 공통 계약에 맞추는 Adapter ④객체와 만드는 방법(Func) 구별 ⑤MiniSight 가짜 드라이버 계약·동작 ⑥어댑터 연결과 프레임 전달 ⑦정리 순서·소유권 확인 ⑧디버깅·뼈대 복습·셀프 체크다. 실제 파일과 이해도에 맞춰 한 번에 하나씩 진행한다.

### 조각 1 · 객체를 만드는 곳을 한 곳으로 모으기

**지금 만드는 것:** true/false 주문에 따라 선풍기 또는 헤어드라이기 객체를 만들어 공통 계약 IPowerSource로 반환하는 Factory.

**비유:** 손님은 카운터에 주문을 하고 물건을 받는다. 어떤 물건을 꺼낼지는 카운터에서 결정한다. 이번에는 Run이 주문하는 쪽, PowerSourceFactory가 만드는 쪽이다. Factory는 새로운 C# 문법이 아니라 객체 생성 코드를 배치하는 설계 방식이다.

사용자 로컬 Day02 파일에서 IPowerSource, Fan, HairDryer가 public으로 선언돼 있고 Name이 각각 선풍기/헤어드라이기를 반환하는 것을 확인했다. 이 클래스를 다시 작성하지 않고 사용한다. Katas 프로젝트에 Day04 폴더와 KataDay04.cs를 새로 추가해 아래 전체 코드를 손으로 친다. 포트폴리오 원본 파일은 직접 수정하지 않는다.

```csharp
using System;
using Katas.Day02;

namespace Katas.Day04
{
    public static class PowerSourceFactory
    {
        public static IPowerSource Create(bool useFan)
        {
            if (useFan)
            {
                return new Fan();
            }

            return new HairDryer();
        }
    }

    public static class KataDay04
    {
        public static void Run()
        {
            IPowerSource device = PowerSourceFactory.Create(true);
            Console.WriteLine("만든 기기: " + device.Name);
        }
    }
}
```

| 키워드·코드 | 역할 / 사용법 | 풀어서 읽기 |
|---|---|---|
| Factory | 객체 생성 위치를 모으는 설계 | Run이 아니라 Create가 종류 선택과 new를 담당 |
| `using Katas.Day02;` | 기존 타입 이름을 짧게 사용 | Fan은 Katas.Day02.Fan을 뜻함; 별도 객체를 만드는 문장은 아님 |
| `static` | Factory 인스턴스 없이 호출 | new PowerSourceFactory 대신 PowerSourceFactory.Create(...) |
| `IPowerSource Create(...)` | 공통 계약 타입으로 객체 반환 | 실제 객체는 Fan 또는 HairDryer이며 둘 다 계약을 구현 |
| `bool useFan` | 두 종류를 선택하는 주문 값 | true면 Fan, false면 HairDryer |
| `return new Fan();` | 생성한 객체를 반환하고 메서드 종료 | Fan fan = new Fan(); return fan; |

true 분기에서 반환하면 Create는 끝나므로 마지막 HairDryer 생성 줄은 실행하지 않는다. Run의 Name 읽기는 두 구현에 공통이다. 이번은 두 종류만 다루는 첫 예제이며 메뉴가 늘어날 때 enum/switch로 바꾸는 것은 다음 조각에서 확인한다.

**실행 준비:** Katas를 시작 프로젝트로 선택한다. Main의 기존 KataDay03Review.Run 호출을 주석 처리하고 다음을 호출한다. 마지막 Console.ReadKey는 유지한다. Visual Studio에서 새 클래스를 추가해 프로젝트에 포함시킨 뒤 Ctrl+F5로 실행한다.

```csharp
// KataDay03Review.Run();
Katas.Day04.KataDay04.Run();
```

```text
만든 기기: 선풍기
```

**바꿔 보기 1개:** Run의 Create(true)를 Create(false)로 바꾸고 실행한다. Name을 읽는 출력 줄은 유지한다.

```text
만든 기기: 헤어드라이기
```

**검증 범위:** 사용자 기존 Day02 소스와 위 Day04 예제를 임시 파일·진입점으로 C# 7.3 / x64 / .NET Framework 4.7.2 참조(mscorlib, System, System.Core)로 컴파일·실행하여 두 출력을 확인했다. 사용자 작성·실행은 아직 확인 전이다.

**질문 1개:** 위 코드에서 선풍기와 헤어드라이기 중 무엇을 만들지 결정하고 new를 실행하는 곳은 Run과 PowerSourceFactory.Create 중 어디인가?

<details><summary>답</summary>PowerSourceFactory.Create다. Run은 선택 값을 전달하고 공통 IPowerSource 계약으로 받은 객체의 Name을 읽는다. 종류 선택과 생성 규칙은 Factory에 모은다.</details>

> **학습 확인 (2026-11-05):** 사용자는 PowerSourceFactory.Create라고 답했다 ✅. 어떤 구현을 만들지 선택하고 new를 실행하는 책임이 Factory에 있다는 핵심을 확인했다. Run은 선택 값을 전달하고 반환받은 객체를 사용한다. 사용자 파일 작성·직접 실행 결과는 아직 별도 확인 전이다.

### 조각 2 · 이름 있는 주문과 지원하지 않는 종류 거부하기

**지금 만드는 것:** bool 주문을 PowerSourceKind enum으로 바꾸고 Factory의 switch가 생성할 구현을 선택한다. 지원하지 않는 값은 임의의 기기로 대신하지 않고 예외로 알린다.

**비유:** 카운터에 true라고 주문하는 대신 “선풍기”라고 적힌 주문표를 준다. 메뉴에 없는 물건을 주문하면 다른 물건을 마음대로 주지 않고 주문을 거절한다.

사용자 로컬 KataDay04.cs에서 조각 1 구현과 Main의 KataDay04.Run 호출을 확인했다. 현재 `Create(useFan: true)`는 이름 있는 인수를 사용한 유효한 호출이다. 이번에는 매개변수 자체를 kind로 바꾸므로 기존 호출 줄도 아래대로 교체한다. 회사 코드나 사용자 원본은 직접 수정하지 않는다.

같은 namespace 안에서 PowerSourceFactory 위에 추가한다.

```csharp
public enum PowerSourceKind
{
    Fan,
    HairDryer
}
```

PowerSourceFactory 클래스는 다음으로 교체한다. 기존 using System과 using Katas.Day02는 유지한다.

```csharp
public static class PowerSourceFactory
{
    public static IPowerSource Create(PowerSourceKind kind)
    {
        switch (kind)
        {
            case PowerSourceKind.Fan:
                return new Fan();
            case PowerSourceKind.HairDryer:
                return new HairDryer();
            default:
                throw new NotSupportedException("지원하지 않는 기기: " + kind);
        }
    }
}
```

KataDay04의 Run만 교체한다. Main의 호출은 유지한다.

```csharp
public static void Run()
{
    try
    {
        IPowerSource device = PowerSourceFactory.Create(PowerSourceKind.Fan);
        Console.WriteLine("만든 기기: " + device.Name);
    }
    catch (NotSupportedException ex)
    {
        Console.WriteLine("생성 실패: " + ex.Message);
    }
}
```

| 키워드·코드 | 역할 / 사용법 | 풀어서 읽기 |
|---|---|---|
| enum PowerSourceKind | 주문 가능한 종류 이름 | Fan/HairDryer는 각각 기본 번호 0/1 |
| switch / case | 종류 값에 따라 실행 경로 선택 | 해당 case의 new와 return 실행; return으로 메서드가 끝나므로 break는 필요 없음 |
| default | 작성된 case에 없는 값을 처리 | 지원하지 않는 종류를 거부 |
| NotSupportedException | 지원하지 않는 요청을 예외로 알림 | Run의 해당 catch가 생성 실패 메시지 출력 |
| (PowerSourceKind)99 | 정수를 enum 타입으로 명시적 변환 | 정의된 이름이 없는 값도 변환 자체는 가능하므로 default 검사 필요 |

**실행:** Katas를 시작 프로젝트로 선택하고 Ctrl+F5.

```text
만든 기기: 선풍기
```

**바꿔 보기 1개:** Run의 주문 값 PowerSourceKind.Fan만 `(PowerSourceKind)99`로 바꾼다. Ctrl+F5로 실행하고 실패 메시지 뒤 프로그램이 정상적으로 마지막 대기로 돌아오는 것을 본다. 확인 후 Fan으로 되돌린다.

```text
생성 실패: 지원하지 않는 기기: 99
```

enum은 이름을 제공하지만 모든 미정의 정수 값을 자동으로 막지는 않는다. 예외는 받은 요청이 지원되지 않음을 표시하며, Run에서 처리하므로 이 예제에서는 전역 예외 처리까지 올라가지 않는다.

**검증 범위:** 실제 Day02 소스와 위 조각 2 코드를 임시 파일·진입점으로 C# 7.3 / x64 / .NET Framework 4.7.2 참조로 컴파일·실행했다. Fan은 만든 기기: 선풍기, HairDryer는 만든 기기: 헤어드라이기, 99는 생성 실패: 지원하지 않는 기기: 99를 출력하고 모두 종료 코드 0으로 끝남을 확인했다. 사용자 원본 파일은 수정하지 않았다. 사용자 작성·실행 결과는 아직 확인 전이다.

**질문 1개:** default에서 예외 대신 무조건 new HairDryer를 반환하면 어떤 문제가 생길까? 주문한 쪽 입장에서 한 문장으로 답한다.

<details><summary>답</summary>지원하지 않는 잘못된 주문인데도 성공한 것처럼 다른 기기를 받아 설정 오류를 알아채기 어렵다. 실제로 요청한 종류를 만들 수 없으면 실패를 분명히 전달해야 한다.</details>

> **학습 확인 (2026-11-05):** 사용자는 “예외 상황에서도 무조건 HairDryer를 생성해 잘못된 것이 생성되고 에러 감지에 실패한다”라고 답했다 ✅. 지원하지 않는 주문을 다른 객체로 대체하면 생성 성공처럼 보여 설정 오류가 숨는다는 핵심을 확인했다. 정확히는 default가 지원하지 않는 값을 발견한 자리이며, 그곳에서 예외를 던져 호출자에게 실패를 알린다. 사용자 조각 2 직접 실행 결과는 아직 별도 확인 전이다.

### 조각 3 · 외부 API를 우리 계약에 맞추는 Adapter

**지금 만드는 것:** 외부 SDK의 ReadModelName() 메서드를 기존 IPowerSource.Name 속성에 연결하는 어댑터.

**비유:** 손님은 “이름”이라는 말로 물어보지만 외부 기기는 ReadModelName이라는 말만 알아듣는다. 중간 통역사가 우리 쪽 Name 요청을 외부 쪽 메서드 호출로 바꿔 준다. 외부 코드를 수정하는 대신 어댑터를 통해 우리 계약을 제공한다.

로컬 KataDay04에서 조각 2 enum/switch/default/로컬 catch 구현을 확인했다. 현재 Run은 미정의 88 실험 값이고 실패 설명 문구는 안내 예제와 다르지만 미지원 값 거부라는 흐름은 같다. 불필요한 using은 이번 학습과 무관해 수정하지 않는다. 사용자 원본은 직접 수정하지 않는다.

같은 Katas.Day04 namespace 안에 다음 두 클래스를 추가한다. ForeignFanSdk는 외부에서 제공해 직접 고칠 수 없다고 가정한 가짜 코드이며 실제 벤더 SDK나 하드웨어가 아니다.

```csharp
public sealed class ForeignFanSdk
{
    public string ReadModelName()
    {
        return "외부 선풍기";
    }
}

public sealed class ForeignFanAdapter : IPowerSource
{
    private readonly ForeignFanSdk _sdk = new ForeignFanSdk();

    public string Name
    {
        get { return _sdk.ReadModelName(); }
    }
}
```

SDK에는 Name 속성이 없고 ReadModelName 메서드가 있다. 어댑터는 SDK를 필드에 보관하고 Name getter에서 SDK를 호출한다. 이번 가짜 SDK는 IDisposable 자원을 가지지 않는다. 객체 생성 방법을 밖에서 받는 실습은 다음 조각에서 다룬다.

기존 enum에는 세 번째 값을 추가한다.

```csharp
public enum PowerSourceKind
{
    Fan,
    HairDryer,
    ForeignFan
}
```

Factory의 switch에 default 앞 새 case를 추가한다. 기존 Fan/HairDryer/default 처리는 유지한다.

```csharp
case PowerSourceKind.ForeignFan:
    return new ForeignFanAdapter();
```

Run의 주문 줄만 다음으로 바꾼다. 기존 try/catch와 Name 출력 줄은 유지한다.

```csharp
IPowerSource device = PowerSourceFactory.Create(PowerSourceKind.ForeignFan);
```

| 키워드·코드 | 역할 / 사용법 | 풀어서 읽기 |
|---|---|---|
| Adapter | 서로 다른 API 모양 연결 | Name 요청을 ReadModelName 호출로 변환 |
| `ForeignFanAdapter : IPowerSource` | 우리 계약 구현 | 호출자는 기존처럼 Name 사용 |
| `private readonly ForeignFanSdk _sdk` | 외부 객체를 필드에 보관 | 이번에는 선언 시 한 번 생성하고 다른 객체로 재대입하지 않음; SDK 내부 상태가 불변이라는 뜻은 아님 |
| `get { return _sdk.ReadModelName(); }` | 속성을 읽을 때 메서드에 위임 | device.Name → Adapter.Name → SDK.ReadModelName() |

Factory는 어떤 객체를 만들지 선택하고, Adapter는 만들어진 객체가 우리 계약으로 사용되도록 모양을 맞춘다. 이번에는 Factory가 SDK를 감싼 ForeignFanAdapter를 반환한다.

**실행:** Katas를 시작 프로젝트로 선택하고 Ctrl+F5.

```text
만든 기기: 외부 선풍기
```

**바꿔 보기 1개:** 실험을 위해 가짜 SDK의 반환 문자열만 “외부 선풍기 v2”로 바꾼다. Run의 Name 출력과 어댑터는 유지한 채 다시 실행하고 출력 변화를 확인한다. 실제 외부 SDK를 직접 고치는 방식의 해결책을 권하는 것이 아니라, 같은 어댑터 경로로 반환값이 전달되는지 보는 모의 실험이다.

```text
만든 기기: 외부 선풍기 v2
```

**검증 범위:** 사용자 기존 Day02/Day04 소스의 임시 복사본에 위 변경을 반영해 C# 7.3 / x64 / .NET Framework 4.7.2 참조(mscorlib, System, System.Core, System.Xml)로 컴파일·실행했다. 외부 선풍기와 외부 선풍기 v2 두 출력, 종료 코드 0을 확인했다. 사용자 파일·실장비는 수정하지 않으며 사용자 직접 실행 결과는 아직 확인 전이다.

**질문 1개:** ForeignFanSdk에는 Name 속성이 없는데도 Run에서 device.Name을 읽을 수 있는 이유는 무엇일까? 어떤 코드가 연결하는지 답한다.

<details><summary>답</summary>ForeignFanAdapter가 IPowerSource를 구현하고 Name getter 안에서 SDK.ReadModelName()을 호출해 반환하기 때문이다. Run은 SDK에 직접 Name을 요구하는 것이 아니라 어댑터의 Name을 읽는다.</details>

> **학습 확인 (2026-11-05):** 사용자는 “ForeignFanAdapter.Name이 연결해 준다. 어댑터가 SDK와 이어 준다”라고 답했다 ✅. Name getter가 `_sdk.ReadModelName()`을 호출하여 외부 API를 우리 계약에 연결한다는 흐름을 보충했다. 사용자 조각 3 작성·직접 실행 결과는 아직 별도 확인 전이다.

### 조각 4 · 객체 하나와 만드는 방법(Func) 구별하기

**지금 만드는 것:** 기존 객체를 돌려주는 Func와 매번 새 객체를 만드는 Func를 두 번씩 호출해 같은 객체인지 비교한다.

**비유:** 이미 만든 빵 하나를 가리키는 쪽지와, 주문할 때마다 새 빵을 만들라는 조리법은 다르다. 둘 다 함수 모양으로 전달할 수 있지만 새 객체가 생기는지는 함수 안에서 new를 실행하는지에 달렸다.

로컬 파일에서 조각 3의 SDK 모형·Adapter.Name 위임·enum/Factory 분기와 Run 호출이 작성된 것을 확인했다. 이번에는 기존 클래스와 Factory를 모두 유지하고 KataDay04.Run 메서드 전체만 다음으로 교체한다. using System도 유지한다. 사용자 파일은 직접 수정하지 않는다.

```csharp
public static void Run()
{
    ForeignFanSdk sdk = new ForeignFanSdk();

    Func<ForeignFanSdk> reuse = () => sdk;
    Func<ForeignFanSdk> create = () => new ForeignFanSdk();

    ForeignFanSdk a = reuse();
    ForeignFanSdk b = reuse();
    Console.WriteLine("기존 객체 두 번: " + object.ReferenceEquals(a, b));

    ForeignFanSdk c = create();
    ForeignFanSdk d = create();
    Console.WriteLine("새 객체 두 번: " + object.ReferenceEquals(c, d));
}
```

| 키워드·코드 | 역할 / 사용법 | 풀어서 읽기 |
|---|---|---|
| Func<ForeignFanSdk> | 입력 없이 SDK 객체를 반환하는 함수 타입 | Func의 마지막 타입은 반환 타입; 여기서는 입력 타입이 없음 |
| `() => sdk` | 이미 있는 참조 반환 | 호출할 때마다 같은 sdk를 돌려줌 |
| `() => new ForeignFanSdk()` | 호출할 때마다 객체 생성 | 함수 호출 시 new 실행 후 새 객체 반환 |
| `create()` | 저장한 함수 실행 | delegate의 Invoke 호출과 같은 역할 |
| `object.ReferenceEquals(a, b)` | 두 참조가 같은 객체인지 검사 | 이름·필드 내용이 같은지 비교하는 것이 아님 |

`create`에 람다를 대입하는 줄에서 오른쪽 new가 즉시 실행되는 것은 아니다. c/d를 만드는 두 함수 호출에서 각각 실행된다. 반면 sdk의 new는 첫 줄에서 한 번 실행되고 reuse는 그 객체만 돌려준다. Func 자체가 새 객체를 보장하는 것은 아니다.

**실행:** 기존 Main의 KataDay04.Run 호출을 유지하고 Katas를 시작 프로젝트로 선택한 뒤 Ctrl+F5.

```text
기존 객체 두 번: True
새 객체 두 번: False
```

a/b는 같은 객체이고 c/d는 별개의 객체다. 모델 이름을 읽으면 둘 다 같은 문자열을 반환하더라도 객체는 다를 수 있다.

**바꿔 보기 1개:** create의 본문만 `() => sdk`로 바꿔 실행한다. 두 번째 비교도 True가 되는 것을 확인하고 다시 `() => new ForeignFanSdk()`로 되돌린다. “새 객체 두 번”은 고정 출력 라벨이며, 이 변경 실험에서는 더 이상 새 객체를 만들지 않는다는 점을 구별한다.

```text
기존 객체 두 번: True
새 객체 두 번: True
```

**장치 연결로 이어 보기:** 연결이 끊긴 SDK를 새 객체로 교체해야 하는 경우, 어댑터가 Func를 보관해 필요할 때 호출하도록 설계할 수 있다. 이미 만든 같은 SDK만 돌려주는 함수는 이 목적을 충족하지 못한다. 다음 장치 실습에서 기존 MiniSight의 드라이버 생성 함수와 연결한다. 이번 모형은 IDisposable 자원이나 실제 재연결을 다루지 않는다.

**검증 범위:** 현재 사용자 Day02/Day04 소스의 임시 복사본에서 Run 전체만 위 코드로 교체하고 C# 7.3 / x64 / .NET Framework 4.7.2 참조로 컴파일·실행했다. 기본 실험 True/False, create도 기존 sdk를 반환하도록 바꾼 실험 True/True와 종료 코드 0을 확인했다. 사용자 원본은 수정하지 않았다. 사용자 직접 실행은 아직 확인 전이다.

**질문 1개:** 재연결할 때마다 새 SDK 객체가 필요한 경우 reuse와 create 중 어느 함수를 넘겨야 할까? 이유도 한 줄로 답한다.

<details><summary>답</summary>create다. 호출할 때마다 new를 실행해 새 객체를 만들기 때문이다. reuse는 처음 만든 sdk를 계속 반환한다.</details>

> **학습 확인 (2026-11-05):** 사용자는 “새 SDK가 필요하면 create이며 호출할 때마다 new로 새 객체를 만든다. reuse는 하나의 객체를 가리킨다”라고 설명했다 ✅. 만드는 함수 내부의 new 실행 여부가 객체 재생성을 결정한다는 핵심을 확인했다. “동시에 가리킨다”는 표현은 같은 객체의 참조를 공유한다는 뜻으로 보충했으며, 이 실험이 여러 스레드의 동시 실행을 보여 주는 것은 아니다. 사용자 조각 4 직접 실행 결과는 아직 별도 확인 전이다.

> **Q. Func<> 이게 뭐하는 거야?** (2026-11-05)

Func는 반환값이 있는 함수를 변수에 담거나 다른 곳에 전달할 때 사용하는 타입이다. int 변수에 숫자를 담듯 Func<int> 변수에는 int를 반환하는 함수를 담는다. 예를 들어 “실행하면 10을 주는 방법”을 보관하고 나중에 호출할 수 있다.

```csharp
Func<int> getNumber = () => 10;
int number = getNumber();
Console.WriteLine(number);
```

```text
10
```

| 부분 | 역할 / 사용법 | 풀어서 읽기 |
|---|---|---|
| Func<int> | 함수 변수의 타입 | 입력 없이 호출하고 int 값을 반환하는 함수 |
| getNumber | 함수를 보관하는 변수 이름 | 여기에는 숫자 10 자체가 아니라 호출할 함수가 담김 |
| () => 10 | 실행할 함수 내용 | 입력 없이 호출되면 10 반환 |
| getNumber() | 보관한 함수 호출 | 반환된 숫자 10을 number에 저장 |

현재 SDK 코드도 같은 구조다.

```csharp
Func<ForeignFanSdk> create = () => new ForeignFanSdk();
ForeignFanSdk sdk = create();
```

Func<ForeignFanSdk>는 함수를 보관하는 타입이고, ForeignFanSdk는 그 함수를 실행했을 때 반환받는 값의 타입이다. create는 만드는 방법을 보관하고 create()는 그 방법을 실행한다. Func를 쓰면 이 방법을 다른 객체에 전달하고 받은 쪽이 필요한 때 호출할 수 있다. new를 호출하는 람다를 전달할 경우 그 호출 시점에 SDK를 생성한다.

**확인 질문 1개:** create와 create() 중 보관한 함수를 실제로 실행하는 것은 무엇인가?

<details><summary>답</summary>create()다. create는 함수를 가리키는 변수이고 괄호를 붙이면 호출한다.</details>

숫자 예제는 C# 7.3 / x64 / .NET Framework 4.7.2 참조로 임시 컴파일·실행하여 10 출력과 종료 코드 0을 확인했다. 사용자 원본 파일은 수정하지 않았다. 사용자는 create()라고 재답했다 ✅. 함수를 가리키는 변수 create와 괄호를 붙인 실제 호출 create()를 구별했다.

### 조각 5 · 카메라 계약과 연결·시작·정지 순서

**지금 만드는 것:** 카메라 드라이버 계약을 구현하는 콘솔 모형을 Func로 생성하고, 연결 → 시작 → 정지 → 정리 순서로 사용한다.

**비유:** 카메라 조작 설명서에는 연결·촬영 시작·촬영 정지가 있다. 어느 회사 카메라인지 몰라도 이 설명서의 버튼을 같은 이름으로 누를 수 있게 계약을 만든다.

현재 사용자 Kata에서 조각 4의 reuse/create와 ReferenceEquals 비교 구현을 확인했다. MiniSight에는 1회차 ICameraDriver/FakeCameraDriver가 이미 있지만 이번에 새로 작성한 것으로 간주하지 않는다. 실제 계약에는 Mat 이벤트도 있으므로, 이번은 Katas에서 이름만 같은 축소 계약으로 먼저 연습한다. 타이머·영상·스레드는 아직 붙이지 않는다.

기존 KataDay04.cs의 같은 Katas.Day04 namespace 안에 다음 두 타입을 추가한다. 다른 클래스 내부가 아니라 같은 높이에 둔다. 기존 using System은 유지한다.

```csharp
public interface ICameraDriver : IDisposable
{
    void Open(string name);
    void Start();
    void Stop();
}

public sealed class ConsoleCameraDriver : ICameraDriver
{
    private bool _disposed;

    public void Open(string name)
    {
        Console.WriteLine(name + " 연결");
    }

    public void Start()
    {
        Console.WriteLine("촬영 시작");
    }

    public void Stop()
    {
        Console.WriteLine("촬영 정지");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Console.WriteLine("드라이버 정리");
    }
}
```

ConsoleCameraDriver는 실제 장비·네이티브 자원을 열지 않는다. 정리 로그는 순차 호출에서 한 번만 찍으며, 상태별 호출 제한·여러 스레드의 동시 Dispose를 보장하는 구현은 아니다. 실제 장치의 상태 관리는 Day03의 DeviceBase와 별도로 연결한다.

KataDay04.Run 전체만 다음으로 교체한다. Main 호출과 기존 다른 클래스는 유지한다.

```csharp
public static void Run()
{
    Func<ICameraDriver> createDriver = () => new ConsoleCameraDriver();

    using (ICameraDriver driver = createDriver())
    {
        driver.Open("연습 카메라");
        driver.Start();
        driver.Stop();
    }
}
```

| 키워드·코드 | 역할 / 사용법 | 풀어서 읽기 |
|---|---|---|
| ICameraDriver : IDisposable | 드라이버 계약에 정리 계약 포함 | 구현 클래스는 Dispose도 제공 |
| ConsoleCameraDriver : ICameraDriver | 공통 계약 구현 | 현재는 실제 촬영 대신 로그 출력 |
| Func<ICameraDriver> | 드라이버를 반환하는 함수 타입 | 실제 반환 객체는 계약을 구현한 ConsoleCameraDriver |
| createDriver() | 필요한 시점에 생성 함수 실행 | 여기서는 new ConsoleCameraDriver 실행 |
| using (...) | 블록을 나갈 때 Dispose 호출 | Stop 호출을 자동으로 삽입하는 문법은 아님 |
| `_disposed` / return | 순차 중복 정리 진입 방지 | 이 bool 검사만으로 다중 스레드 동시성까지 보장하지 않음 |

**실행:** Katas를 시작 프로젝트로 선택하고 Ctrl+F5.

```text
연습 카메라 연결
촬영 시작
촬영 정지
드라이버 정리
```

**바꿔 보기 1개:** Run의 driver.Stop() 한 줄만 잠깐 주석 처리한다. 촬영 정지 출력은 없어지지만 using이 Dispose를 부르므로 드라이버 정리는 출력된다. 확인 후 Stop 호출을 복구한다. 이것은 위 모형의 Dispose가 Stop을 호출하지 않는다는 뜻이며 모든 실제 SDK의 Dispose 동작이 같다는 일반 규칙은 아니다.

```text
연습 카메라 연결
촬영 시작
드라이버 정리
```

**검증 범위:** 현재 사용자 Day02/Day04 소스의 임시 복사본에 위 두 타입과 Run 변경을 반영해 C# 7.3 / x64 / .NET Framework 4.7.2 참조로 컴파일·실행했다. 정상은 연결/시작/정지/정리 네 줄, Stop 생략은 연결/시작/정리 세 줄과 각각 종료 코드 0을 확인했다. 사용자 원본은 수정하지 않고 실제 카메라 연결·촬영은 하지 않는다.

**질문 1개:** Func<ICameraDriver>가 반환할 타입은 ICameraDriver인데, 함수에서 new ConsoleCameraDriver()를 반환해도 되는 이유는 무엇일까?

<details><summary>답</summary>ConsoleCameraDriver가 ICameraDriver 계약을 구현하므로 해당 인터페이스 타입으로 참조할 수 있기 때문이다. 호출자는 공통 계약의 Open/Start/Stop/Dispose를 사용한다.</details>

> **학습 확인 (2026-11-05):** 사용자는 “ConsoleCameraDriver가 인터페이스 클래스로 ICameraDriver를 가지고 있다”라고 답했다 △. 계약과 구현 클래스의 관계를 짚은 핵심은 맞으며, 정확히는 ConsoleCameraDriver가 ICameraDriver 인터페이스를 구현한다고 표현한다. 계약을 구현한 객체이므로 ICameraDriver 타입으로 참조·반환할 수 있다고 보충했다. 실제 객체를 다른 객체로 바꾸는 것이 아니다. 사용자 조각 5 작성·직접 실행 결과는 아직 별도 확인 전이다.

### 조각 6 · 실제 MiniSight Adapter에 생성 함수를 전달하고 한 장 받기

**지금 만드는 것:** 개인 MiniSight에 이미 있는 DriverCameraAdapter에 드라이버 생성 함수를 전달하고, 새 버튼에서 초기화 → 시작 → 한 프레임 받기 → 정지를 실행한다.

**비유:** Adapter에게 카메라 한 대를 미리 주는 대신 “필요할 때 카메라를 만드는 방법”을 적은 쪽지를 준다. Adapter는 쪽지를 보관했다가 초기화할 때 그 방법을 실행한다.

로컬 Katas 조각 5에서 ICameraDriver 구현을 확인했다. 사용자 ConsoleCameraDriver의 `void ICameraDriver.Start()` 등은 명시적 인터페이스 구현이며 인터페이스 타입으로 호출하는 현재 Run에서 유효하다. 이번에는 실제 MiniSight 프로젝트로 이동한다. 앞의 Katas 축소 계약을 MiniSight에 복사하지 않는다. 기존 DriverCameraAdapter/FakeCameraDriver를 재사용하되 이번에 새로 작성 완료한 것으로 표시하지 않는다.

MiniSight를 시작 프로젝트로 선택하고 Debug/x64로 둔다. Form1 디자이너에 button9를 추가하고 Text를 “어댑터 연결”로 바꾼다. Form1의 using 목록에 다음을 추가한다. Devices, Threading, Tasks, Windows.Forms는 기존 선언을 유지한다.

```csharp
using MiniSight.Devices.Cameras;
```

새 버튼을 더블클릭하고 핸들러 전체를 다음으로 작성한다. 자동 생성된 void 앞에 async를 붙이는 것도 포함한다.

```csharp
private async void button9_Click(object sender, EventArgs e)
{
    using (IImageSource camera = new DriverCameraAdapter(
        "연습 카메라", () => new FakeCameraDriver(300)))
    {
        camera.Initialize(CancellationToken.None);
        camera.Start(CancellationToken.None);

        using (ImageFrame frame =
            await camera.WaitFrameAsync(CancellationToken.None))
        {
            camera.Stop();
            MessageBox.Show("카메라: " + frame.SourceName
                + Environment.NewLine + "받은 프레임: " + frame.FrameId
                + Environment.NewLine + "현재 상태: " + camera.State);
        }
    }
}
```

| 키워드·코드 | 역할 / 사용법 | 풀어서 읽기 |
|---|---|---|
| IImageSource | 이미지 제공 장치의 공통 계약 | Day02의 IDevice를 포함하고 WaitFrameAsync 제공 |
| `() => new FakeCameraDriver(300)` | 새 드라이버를 만드는 방법 전달 | Adapter가 Func<ICameraDriver>로 보관; 300은 반복 간격 ms |
| Initialize | 초기화 흐름 실행 | 현재 Adapter의 OnInitialize에서 생성 함수를 호출하고 드라이버 Open |
| async void | await를 사용하는 버튼 이벤트 핸들러 | 일반 비동기 작업 메서드는 Task를 반환; 여기서는 이벤트 형식이 void |
| await WaitFrameAsync(...) | 프레임을 비동기로 기다림 | UI에서 Result/Wait로 막지 않고 반환된 ImageFrame을 받음 |
| using ImageFrame | 받은 한 장의 소유자 정리 | 이 블록을 나갈 때 frame.Dispose, 내부 Mat도 정리 |

FakeCameraDriver.Start의 타이머는 첫 콜백을 지연 없이 예약하고 이후 300ms 간격으로 호출한다. 첫 프레임을 300ms 뒤에 받는다고 단정하지 않는다. 메시지박스 전에 Stop을 호출해 대화상자를 기다리는 동안 반복 촬영을 계속하지 않게 한다. 정리 시 DeviceBase.Dispose가 Stop을 다시 호출할 수 있다.

**실행:** F5 → 새 버튼 클릭. 다음은 메시지 예시이며 프레임 번호는 첫 수신 순서에 따라 달라질 수 있다.

```text
카메라: 연습 카메라
받은 프레임: 1
현재 상태: Stopped
```

**바꿔 보기 1개:** 생성자의 이름 “연습 카메라”만 “복습 카메라”로 바꿔 실행한다. Frame.SourceName을 통해 메시지의 카메라 이름도 바뀌는지 확인한다. 확인 후 원래 이름으로 되돌린다.

**짧은 코드 연결:** 현재 개인 MiniSight의 Adapter 생성자는 `_driverFactory = driverFactory;`로 함수를 보관한다. OnInitialize의 `ICameraDriver driver = _driverFactory();`에서 실제로 실행한다. 따라서 Adapter 생성과 드라이버 생성은 서로 다른 시점이다.

**검증 범위 (2026-11-05):** 기존 MiniSight를 MSBuild / Debug x64 / C# 7.3으로 빌드해 성공했다. 빌드된 실제 개인 프로젝트와 OpenCvSharp 관리/네이티브 DLL을 임시 콘솔 하네스에서 사용해 Adapter 생성 직후 드라이버 생성 수 0, Initialize 뒤 1, 프레임 #1 수신, Stopped 상태, 640x480 이미지 및 정리 후 Disposed를 확인했다. 하네스는 검증 대기를 5초로 제한했다. 사용자 버튼 코드는 직접 수정하지 않았고, 새 핸들러도 임시 Form 파생 클래스에 그대로 넣어 C# 7.3 / x64 / .NET Framework 4.7.2로 컴파일 성공했다. 사용자 실제 버튼 클릭과 이름 변경 실험은 아직 확인 전이다. 실제 카메라 하드웨어는 연결하지 않았다. 타이머 콜백의 완전한 종료 대기·동시성까지 검증한 것은 아니다.

**질문 1개:** `new FakeCameraDriver(300)`이 실제 실행되는 시점은 Adapter 생성자 호출 때일까, `camera.Initialize(...)` 때일까? 보관과 호출을 구별해 답한다.

<details><summary>답</summary>현재 구현에서는 부모 DeviceBase.Initialize가 어댑터에서 override한 OnInitialize를 호출하고, 그 안에서 _driverFactory()를 실행할 때다. Adapter 생성자는 Func를 보관한다.</details>

> **학습 확인 (2026-11-05):** 사용자는 “camera.Initialize(CancellationToken.None) 때이며 ICameraDriver driver = _driverFactory() 부분이 있다”라고 답했다 ✅. 실제 호출 줄을 확인했고, 함수 보관과 실행 시점을 구별했다. 정확한 경로는 부모 Initialize → 어댑터 OnInitialize → _driverFactory() → 람다 안 new FakeCameraDriver다. 사용자 실제 버튼 클릭·프레임 수신 결과는 아직 별도 확인 전이다.

### 조각 7 · 받은 프레임의 주인과 정리 책임 확인하기

**지금 만드는 것:** 실제 가짜 카메라에서 받은 ImageFrame을 using으로 정리하고, 같은 Mat을 가리키던 별칭에서도 정리 상태를 확인한다.

**비유:** 사진이 든 상자를 받았으면 다 쓴 뒤 상자와 사진을 정리할 책임도 받은 쪽에 있다. 옆 사람이 같은 사진을 가리킨다고 사진이 한 장 더 생기는 것은 아니다.

현재 로컬 button9 작성과 “복습 카메라” 이름 변경 실험 코드가 있는 것을 확인했다. 출력의 “받음 프레임”은 안내의 “받은 프레임”과 글자 차이가 있으나 생성·수신 흐름과는 별개다. 이번에도 사용자 원본은 직접 수정하지 않는다.

MiniSight를 시작 프로젝트, Debug/x64로 유지한다. 기존 button9 핸들러 전체를 다음으로 교체한다. 기존 using OpenCvSharp와 Devices/Cameras/Threading/Tasks/Windows.Forms를 유지한다. 새 버튼은 만들지 않는다.

```csharp
private async void button9_Click(object sender, EventArgs e)
{
    using (IImageSource camera = new DriverCameraAdapter(
        "연습 카메라", () => new FakeCameraDriver(300)))
    {
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

| 키워드·코드 | 역할 / 사용법 | 풀어서 읽기 |
|---|---|---|
| `Mat borrowed = frame.Image` | 같은 이미지 객체의 참조를 보관 | 새 이미지 복사가 아니며 별칭을 얻은 것 |
| `using (frame)` | 이미 받은 객체를 블록 끝에서 정리 | frame.Dispose가 내부 Mat을 정리 |
| `frame.Image == null` | 현재 Frame의 이미지 참조가 비워졌는지 확인 | frame 객체 자체가 null이 되었다는 뜻은 아님 |
| `borrowed.IsDisposed` | Mat의 정리 표시를 관찰 | 픽셀 접근이 아니라 관리 객체의 상태 확인 |

**실행:** F5 → 기존 버튼 9 클릭.

```text
블록 안 이미지 있음: True
블록 밖 이미지 없음: True
빌린 Mat 정리됨: True
```

사용한 프레임은 큐에서 꺼내 호출자에게 넘어왔다. 호출자는 그 한 장을 정리하고, Adapter는 큐에 남은 프레임을 정리한다. 같은 Mat을 borrowed에서도 가리켰기 때문에 frame을 정리하면 borrowed.IsDisposed도 True다. borrowed 변수의 null 여부와 실제 이미지 자원 수명은 별개다.

**바꿔 보기 1개:** using(frame) 안의 camera.Stop 다음에 `frame.Dispose();` 한 줄을 잠깐 추가한다. 블록 안에서 이미 정리했으므로 첫 줄이 False가 된다. 블록 끝의 두 번째 Dispose는 ImageFrame의 Interlocked 가드가 건너뛴다. 확인 후 추가한 한 줄은 지운다.

```text
블록 안 이미지 있음: False
블록 밖 이미지 없음: True
빌린 Mat 정리됨: True
```

**어댑터 안의 소유권 이동 읽기:** 현재 개인 MiniSight의 OnFrame은 다음 순서다. 읽기만 하고 기존 코드를 고치지 않는다.

```csharp
frame = new ImageFrame(id, _name, mat);
mat = null;
```

위 줄에서 ImageFrame이 같은 Mat의 정리 책임을 맡았으므로 지역 변수 mat은 비운다. null 대입이 이미지를 Dispose하는 것은 아니다. 이어서 큐에 보관하는 데 성공하면 지역 변수 frame도 비운다.

```csharp
if (_queue.TryAdd(frame))
{
    frame = null;
}
```

그 뒤 finally의 frame?.Dispose와 mat?.Dispose는 아직 넘기지 못한 자원만 정리한다. 큐는 객체를 복사하는 대신 참조를 보관하며, WaitFrameAsync로 꺼낸 쪽이 사용 후 정리한다. 이 계약에서 성공적으로 넘긴 자원을 이전 소유자가 즉시 정리하면 받은 쪽이 사용할 수 없어진다.

**검증 범위:** 현재 MiniSight를 Debug/x64·C# 7.3으로 빌드하고 실제 관리/네이티브 라이브러리를 임시 콘솔 하네스로 실행했다(프레임 대기 5초 제한). 정상 True/True/True, 선행 Dispose False/True/True 및 두 경우 종료 코드 0을 확인했다. 새 버튼 핸들러도 임시 Form 클래스에서 C# 7.3 / x64 / .NET Framework 4.7.2로 컴파일 성공했다. 사용자 실제 UI 클릭·출력은 아직 별도 확인 전이며 타이머 콜백의 완전한 종료 대기는 검증하지 않았다. 사용자 원본·실장비는 수정하지 않는다.

**질문 1개:** ImageFrame에 Mat을 넘긴 뒤 `mat = null;`을 하지 않으면, OnFrame의 finally에서 어떤 문제가 생길 수 있을까?

<details><summary>답</summary>finally의 mat?.Dispose가 이미 Frame에 넘긴 같은 Mat을 정리해 버릴 수 있다. 그러면 큐나 호출자가 받은 Frame의 이미지가 이미 정리된 상태라 사용할 수 없게 된다. null은 자원 해제가 아니라 이전 지역 변수의 정리 책임을 내려놓는 표시다.</details>

> **Q. 널 예외가 나지 않을까?** (2026-11-05)
>
> 사용자 답은 null 예외 예상 ❌. `mat?.Dispose()`는 mat이 null이면 호출을 건너뛴다. 오히려 `mat = null`을 생략하면 mat이 같은 Mat 객체를 계속 가리키므로 Dispose가 실행되는 것이 문제다. ImageFrame 생성자는 같은 참조를 보관하고 원래 지역 변수를 자동으로 비우지 않는다. 따라서 Frame에 넘긴 Mat이 이전 소유자의 finally에서 먼저 정리될 수 있다. 이때 Frame.Image는 null이 아니어도 그 Mat의 IsDisposed는 True일 수 있고, 받은 쪽은 유효한 이미지로 사용할 수 없다. 이후 어떤 예외가 발생할지는 접근한 API에 따라 달라지므로 null 예외가 반드시 발생한다고 설명하지 않는다.

```csharp
// mat?.Dispose()를 풀어서 읽기
if (mat != null)
{
    mat.Dispose();
}
```

**재확인 질문 1개:** Frame에 넘긴 Mat을 finally에서 먼저 Dispose했다면, 받은 쪽의 이미지는 정상 사용 가능한 상태일까, 이미 정리된 상태일까?

<details><summary>답</summary>이미 정리된 상태다. Frame이 보관한 참조가 남아 있어도 같은 Mat의 자원은 먼저 정리되었다.</details>

위 첫 보충에서는 소스의 참조 공유와 ?. 분기 흐름을 설명했고 사용자 파일을 변경하거나 타이머 콜백 순서를 새로 실험하지 않았다. 후속 재답은 “먼저 Dispose했다면 null이므로 정상 사용 불가”였다 △. 사용 불가라는 결론은 맞지만 null이 되는 대상과 Dispose 호출 대상을 더 구별해야 한다.

> **Q. 먼저 Dispose했다면 null이 되는 것 아닌가?** (2026-11-05)

이번 문제의 finally는 `mat?.Dispose()`를 호출한다. 이것은 `frame.Dispose()`와 다르다. ImageFrame.Dispose에는 자신의 `_image` 필드를 null로 바꾸는 코드가 직접 들어 있다.

```csharp
Mat image = Interlocked.Exchange(ref _image, null);
```

반면 Mat.Dispose는 Frame의 `_image` 필드를 바꾸지 않는다. 같은 참조를 보관한 Frame은 남아 있지만 Mat의 자원은 먼저 정리될 수 있다. 따라서 “사용할 수 없음”과 “참조가 null”은 같은 말이 아니다.

각 경우를 새 Mat과 ImageFrame으로 따로 비교한 결과다. 표를 읽는 시점에는 다른 쪽의 Dispose를 아직 호출하지 않았다.

| 먼저 호출한 코드 | frame.Image == null | mat.IsDisposed |
|---|---|---|
| mat.Dispose() | False | True |
| frame.Dispose() | True | True |

두 경우 모두 원래 지역 변수 mat 자체는 null이 아니었다. Frame.Dispose가 내부 참조를 비워도 지역 변수까지 자동으로 바뀌지 않는다.

**검증 범위 (2026-11-05):** 실제 개인 프로젝트의 ImageFrame과 OpenCvSharp 관리/네이티브 DLL로 1x1 Mat을 만든 임시 콘솔 예제를 C# 7.3 / x64 / .NET Framework 4.7.2로 컴파일·실행했다. 위 두 경우의 null/IsDisposed 값과 종료 코드 0을 확인했고, 각 프레임은 using으로 마무리했다. 사용자 파일·실장비는 변경하지 않았으며 타이머 순서를 검증하는 실험도 아니다.

**재확인 질문 1개:** mat.Dispose만 호출했을 때 Frame.Image는 null이 될까, 같은 Mat 참조가 남아 있을까?

<details><summary>답</summary>같은 Mat 참조가 남아 있다. 다만 그 Mat의 자원은 이미 정리됐으므로 정상 이미지로 사용할 수 없다.</details>

> **후속 답 (2026-11-05):** 사용자는 “null이고 참조는 끊긴다”라고 재답했다 ❌. Dispose 호출과 참조 대입의 차이가 아직 혼동되어, 새 내용을 진행하지 않고 이미 작성한 ConsoleCameraDriver로 관찰 범위를 줄인다. 현재 해당 Dispose 몸통은 _disposed=true와 정리 로그만 실행하며 외부 driver 변수를 null로 바꾸는 코드는 없다.

**보충 실험 · Dispose 호출과 null 대입을 따로 실행하기**

Katas를 시작 프로젝트로 선택한다. MiniSight의 버튼 실험은 보존하고, Katas.Day04.KataDay04의 Run 전체만 다음으로 교체한다. 기존 ICameraDriver/ConsoleCameraDriver 구현을 재사용한다.

```csharp
public static void Run()
{
    ICameraDriver driver = new ConsoleCameraDriver();
    ICameraDriver borrowed = driver;

    driver.Dispose();
    Console.WriteLine("Dispose 후 driver null: " + (driver == null));
    Console.WriteLine("Dispose 후 borrowed null: " + (borrowed == null));

    driver = null;
    Console.WriteLine("대입 후 driver null: " + (driver == null));
    Console.WriteLine("대입 후 borrowed null: " + (borrowed == null));
}
```

Main의 KataDay04.Run 호출과 마지막 ReadKey는 유지하고 Ctrl+F5로 실행한다.

```text
드라이버 정리
Dispose 후 driver null: False
Dispose 후 borrowed null: False
대입 후 driver null: True
대입 후 borrowed null: False
```

Dispose는 객체의 정리 메서드를 호출한다. driver=null은 그 변수에 저장한 참조를 바꾸는 별도 대입이다. 같은 객체를 가리키던 borrowed 변수까지 자동으로 바뀌지 않는다. 이 콘솔 모형은 정리 호출과 참조 값만 비교하며 정리 후 실제 SDK를 사용할 수 있다는 뜻은 아니다.

**검증 범위:** 현재 사용자 Katas 타입을 참조하는 임시 실행 진입점에서 위 메서드 몸통을 C# 7.3 / x64 / .NET Framework 4.7.2로 컴파일·실행했다. 정리 로그 뒤 False/False/True/False와 종료 코드 0을 확인했다. 사용자 원본은 직접 수정하지 않는다.

**확인 요청 1개:** 이번에는 예상이 아니라 실제 실행에서 “Dispose 후 driver null”에 표시된 값을 알려 준다.

> **사용자 실행 확인 (2026-11-05):** 사용자가 정리 로그와 False/False/True/False의 전체 다섯 줄을 제출했고 예상 출력과 일치했다 ✅. Dispose 호출과 null 대입을 분리한 실행 관찰을 확인했다. 원래 문제에 연결하면 mat=null을 생략할 때 mat과 Frame은 같은 Mat을 계속 가리키고, finally의 mat.Dispose가 넘겨준 이미지 자원까지 먼저 정리할 수 있다. mat=null은 Frame의 이미지나 자원을 없애는 것이 아니라 이전 지역 변수만 비워 그 정리를 건너뛰게 한다. 이미지 소유권에 대한 자기 말 설명과 사용자 WinForms 실험 출력까지 새로 확인한 것으로 확대하지 않는다.

### 조각 8 · 실제 CameraFactory로 조립하고 버튼에서 사용하기

**지금 만드는 것:** 개인 MiniSight의 CameraFactory가 종류별 조립을 맡고, 버튼은 IImageSource로 받은 객체를 사용한다. 기존 프레임 정리 실험은 유지한다.

**비유:** 손님이 카운터 뒤로 들어가 부품을 조립하지 않고 “가짜 카메라 하나”를 주문한다. 카운터는 알맞은 Adapter와 드라이버 생성 방법을 조립해 건넨다.

현재 로컬에서 조각 7의 button9와 Dispose/null 콘솔 보충 Run을 확인했다. CameraFactory/CameraKind/FolderCamera도 1회차 파일로 이미 있으므로 새 파일이나 중복 타입을 추가하지 않는다. CameraFactory.Create 메서드만 아래를 보고 다시 손으로 작성한다. enum과 namespace, 나머지 파일은 유지한다. 사용자 원본은 직접 수정하지 않는다.

```csharp
public static IImageSource Create(
    CameraKind kind, string name, string folderOrNull, int intervalMs)
{
    switch (kind)
    {
        case CameraKind.Fake:
            return new DriverCameraAdapter(name, () => new FakeCameraDriver(intervalMs));
        case CameraKind.Folder:
            return new FolderCamera(name, folderOrNull, intervalMs);
        default:
            throw new NotSupportedException("지원하지 않는 카메라 종류: " + kind);
    }
}
```

실제 버튼에서 사용하기 전에 종류 선택·new의 위치가 위 Factory 안으로 모였다는 것을 본다. Fake 분기는 Adapter를 만들면서 드라이버 생성 함수를 전달한다. 실제 드라이버는 앞 조각에서 본 Initialize 시점에 생성된다.

Form1의 기존 button9 바깥 using 부분만 다음으로 바꾼다. 안의 Initialize/Start/프레임 수신/Stop/프레임 정리/메시지 코드는 그대로 유지한다.

```csharp
using (IImageSource camera = CameraFactory.Create(
    CameraKind.Fake, "연습 카메라", null, 300))
```

이 두 줄은 기존 using 헤더를 교체하는 부분이며 새 중괄호 블록을 덧붙이지 않는다. 기존 코드의 new DriverCameraAdapter 헤더를 남겨 두지 않는다. MiniSight를 시작 프로젝트, Debug/x64로 선택한다.

| 코드 | 역할 / 사용법 | 풀어서 읽기 |
|---|---|---|
| CameraKind.Fake | 만들 카메라 종류 주문 | Factory가 가짜 드라이버용 Adapter 조립 |
| IImageSource 반환 | 버튼이 사용할 공통 계약 | 안의 구체 객체 종류와 별개로 같은 메서드 사용 |
| folderOrNull에 null | Fake에는 폴더 경로가 불필요 | Folder를 선택하면 유효한 폴더 경로가 필요 |
| 람다 안 intervalMs | 바깥 매개변수를 나중에도 사용 | 이 호출에서는 300을 보관해 드라이버 생성 시 사용(클로저) |

**눈으로 볼 실험 1개:** 버튼의 CameraFactory.Create 호출 줄에 F9를 누른다. F5 → 버튼 9 클릭으로 멈추면 F11로 Create 안에 들어가 kind/name/intervalMs가 Fake/연습 카메라/300인지 확인한다. 생성 함수 내부로 아직 들어가지 않았다는 점도 확인한다. F5로 계속해서 메시지를 본다.

```text
블록 안 이미지 있음: True
블록 밖 이미지 없음: True
빌린 Mat 정리됨: True
```

**검증 범위:** 위 Factory 메서드를 별도 이름의 임시 타입으로 C# 7.3 / x64 / .NET Framework 4.7.2로 컴파일하여 현재 MiniSight의 실제 Adapter/가짜 드라이버/Mat과 연결해 실행했다(프레임 대기 5초 제한). 정상 Fake의 프레임 수신·True/True/True 및 정리, 미정의 종류 99의 NotSupportedException을 확인했다. Factory 호출로 바꾼 버튼 코드도 임시 Form 클래스에서 컴파일 성공했고, 현재 개인 MiniSight Debug/x64·C# 7.3 MSBuild도 성공했다. Folder 실제 폴더 로딩과 사용자 UI 클릭·디버거 조작은 아직 확인 전이다. 사용자 원본 파일은 직접 수정하지 않았다. 이 조각 안내만으로 Day04를 완료 처리하지 않으며 실제 출력 확인 뒤 뼈대 복습·회사 코드 비교·셀프 체크를 마무리한다.

**확인 요청 1개:** Factory로 바꾼 뒤 버튼 9에서 실제로 표시된 메시지 세 줄을 알려 준다. 디버거가 중단된 상태라면 F5로 계속한 뒤 확인한다.

> **사용자 실행 확인 (2026-11-05):** 사용자가 블록 안 이미지 있음 True / 블록 밖 이미지 없음 True / 빌린 Mat 정리됨 True의 메시지 세 줄을 제출했다 ✅. 로컬 button9가 CameraFactory.Create(Fake, ...)를 호출하고 기존 프레임 정리 블록을 유지한 것도 확인했다. 두 번째 True는 ImageFrame.Dispose가 내부 이미지 참조를 명시적으로 비우기 때문이며 일반 Dispose 호출이 외부 변수를 자동으로 null로 만든다는 뜻은 아니다. F11 관찰·3번째 타이핑·원본 다시 본 줄 여부는 아직 보고되지 않았다. 실장비 검증으로 확대하지 않는다. 다음은 Factory 몸통의 뼈대 복습·짧은 회사 비교·셀프 체크다.

### 마무리 손코딩 · CameraFactory.Create만 뼈대로 채우기 (3번째 타이핑)

**지금 만드는 것:** 주문 종류에 따라 조립한 카메라를 반환하는 Factory 메서드를 할 일 주석만 보고 재작성한다. 카운터의 주문 처리 순서를 직접 적어 보는 복습이다.

현재 개인 MiniSight CameraFactory.Create는 조각 8의 구현과 일치한다. 버튼을 통한 True/True/True 사용자 실행 출력도 받았다. 이제 해당 메서드의 몸통만 지우고 아래 뼈대를 채운다. CameraFactory 클래스·CameraKind enum·namespace·버튼 코드는 유지한다. 사용자 원본은 직접 수정하지 않는다.

```csharp
public static IImageSource Create(
    CameraKind kind, string name, string folderOrNull, int intervalMs)
{
    // [switch] kind에 따라 생성할 종류 선택

    // [case Fake / return] DriverCameraAdapter에 name과 드라이버 생성 함수 전달
    // [Func / new] 함수가 호출될 때 FakeCameraDriver(intervalMs)를 새로 생성

    // [case Folder / return] FolderCamera에 name, folderOrNull, intervalMs 전달

    // [default / throw] NotSupportedException으로 지원하지 않는 종류 거부
    // 메시지: "지원하지 않는 카메라 종류: " + kind
}
```

새 문법은 없다. 앞의 키워드 표부터 참고하고, 막히면 원본에서 그 줄만 확인한다. 봤다면 어느 줄을 다시 봤는지 알려 준다. 잘못 쓴 부분은 완성 정답 대신 힌트로 리뷰한다.

**실행:** 몸통을 채운 뒤 MiniSight / Debug x64에서 Ctrl+Shift+B로 빌드하고 F5 → 버튼 9를 누른다. 기존 using과 프레임 정리 흐름이 유지되므로 결과는 조각 8과 같아야 한다.

```text
블록 안 이미지 있음: True
블록 밖 이미지 없음: True
빌린 Mat 정리됨: True
```

**검증 범위:** 현재 정답 구현의 Fake 프레임 경로·미정의 종류 거부와 사용자 UI 출력은 조각 8에 검증 결과를 기록했다. 이번 문서 변경만으로 같은 검사를 반복 실행하지 않았다. 사용자의 뼈대 작성·재빌드·실행 결과와 원본을 다시 본 줄은 확인 대기 중이다. Folder의 실제 파일 로딩은 확장으로 남긴다.

**확인 요청 1개:** 채운 Create 메서드를 보여 주고, 원본을 다시 본 줄이 있으면 함께 알려 준다. 회사 PC에 작성돼 있으면 해당 파일을 직접 읽어 리뷰한다.

> **손코딩 리뷰 (2026-11-05):** 사용자는 Folder 분기의 case와 `return new FolderCamera(name, folderOrNull, intervalMs);`를 원본에서 다시 보고 작성했다고 보고했다 △. 이 분기를 참고한 사실을 기록하고, 안 보고 전체를 작성했다고 표시하지 않는다. 현재 로컬 Create의 Fake Adapter/Func 조립, Folder 인수 순서, default 예외 처리 모두 정상이며 MiniSight Debug/x64·C# 7.3 MSBuild 성공을 확인했다. Folder의 인수는 이름 / 폴더 경로 / 반복 간격 순서라고 보충한다. 같은 구현의 사용자 UI 출력은 조각 8에서 확인했지만 이번 재타이핑 뒤 새 UI 실행·실제 폴더 로딩을 별도로 확인한 것은 아니다. 다음은 회사 코드의 짧은 비교와 최종 셀프 체크다.

## 3. OmniSight 지도

### 손코딩 뒤 회사 구현과 연결하기 (2026-11-05)

직접 작성한 구조를 회사 코드에서 짧게 찾는다. 회사 코드는 읽기 전용이며 이번 비교에서 수정하지 않는다. 벤더 목록·실제 설정값·회사 로그 문구는 인용하지 않는다.

| MiniSight 실습 | 회사 구현에서 확인한 곳 | 같은 역할 |
|---|---|---|
| CameraFactory.Create | CameraAdapterFactory.Create | 종류 설정에 따라 알맞은 구체 객체를 생성 |
| DriverCameraAdapter | CallbackCameraAdapterBase | 드라이버를 연결하고 공통 장치·프레임 흐름으로 제공 |
| IImageSource | ICameraDevice / 상위 IImageSourceDevice | 호출자는 공통 계약으로 초기화·시작·프레임 수신 |
| Func<ICameraDriver> 보관과 호출 | 생성자의 _driverFactory 보관 / ConnectCore의 _driverFactory() | 생성 방법을 받아 필요한 연결 시점에 새 드라이버 생성 |

실제 소스에서 회사 Factory의 config.VendorType 분기와 공통 계약 반환, 콜백 기반 Adapter의 생성 함수 보관을 확인했다. 회사 구현은 OnInitialize → Connect → ConnectCore를 거쳐 생성 함수를 호출하고, MiniSight는 OnInitialize 안에서 직접 호출한다. 호출 경로의 길이는 달라도 함수 보관과 실제 실행 시점을 나눈 구조는 같다. 모든 회사 카메라가 같은 베이스를 쓰는 것으로 일반화하지 않는다.

Factory는 “무엇을 만들어 줄까”, Adapter는 “외부 드라이버를 우리 계약으로 어떻게 사용할까”를 맡는다. 종류가 추가돼도 공통 계약이 유지되면 받은 객체를 사용하는 호출 흐름을 유지할 수 있다. 생성 시 선택할 종류·설정은 별도로 변경할 수 있다.

**최종 확인 1개:** MiniSight에서 새 종류를 Factory에 추가하고 그 종류를 선택하도록 바꿨다. 같은 IImageSource 계약으로 반환한다면 버튼의 Initialize/Start/WaitFrameAsync 호출도 바꿔야 할까? 이유를 한 줄로 답한다.

<details><summary>답</summary>공통 호출은 바꾸지 않아도 된다. Factory가 구체 객체를 조립해 같은 IImageSource 계약으로 반환하고, 버튼은 그 계약의 메서드를 사용하기 때문이다. 생성할 종류를 고르는 값·설정의 변경과 공통 사용 코드의 변경은 구별한다.</details>

> **학습 확인 (2026-11-05):** 사용자는 “종류에 맞게 재구성해야 한다”라고 답하면서 Initialize/Start는 IDevice, WaitFrameAsync는 IImageSource 계약에 있음을 설명했다 △. 계약의 선언 위치를 찾은 것은 맞다. 재구성할 수 있는 생성 분기·장치별 내부 구현과 버튼의 공통 사용 호출을 구별하도록 보충했다. 실제 IImageSource는 IDevice를 상속하므로 같은 IImageSource 계약으로 반환된 객체에 대해 버튼은 Initialize/Start/WaitFrameAsync를 그대로 호출한다. 생성할 종류·이름·경로 등 주문 값은 별도로 바뀔 수 있다.

| 바뀔 수 있는 곳 | 역할 |
|---|---|
| Factory의 종류 분기·선택 설정 | 새 카메라를 생성·조립하도록 변경 |
| 장치별 Adapter/드라이버 내부 | 실제 장치 동작을 공통 계약에 맞춰 구현 |
| 버튼의 공통 세 호출 | 같은 IImageSource 계약이면 유지 가능 |

**재확인 1개:** 새 카메라도 같은 IImageSource 계약을 구현한다면, 버튼이 구체 카메라 클래스를 몰라도 세 메서드를 호출할 수 있는 이유는 무엇일까?

<details><summary>답</summary>버튼이 사용하는 메서드가 공통 IImageSource와 그 상위 IDevice 계약에 보장되어 있기 때문이다. 실제 수행 내용은 반환된 구현 객체가 제공한다.</details>

**확인 범위:** 실제 회사 Factory·콜백 Adapter·장치 계약과 개인 MiniSight 코드를 읽어 위 역할을 비교했다. 회사 프로그램 실행·장비 연결은 하지 않았다. 사용자는 Initialize/Start는 IDevice, WaitFrameAsync는 IImageSource 계약에 있다고 재답했다. 선언 위치는 맞으며, 같은 공통 계약으로 사용하므로 버튼의 호출을 유지할 수 있다는 이점은 보충했다 △. 이 이유를 자기 말로 한 문장 설명하는 복습은 Day05 조립 지점에서 이어간다.

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 장치 조립 팩토리 | `OmniSight/Devices/DeviceFactory.cs:15-88` | 설비 타입으로 카메라 모드/공유폴더 모드 결정, 실패 시 **역순 Dispose**(L75-85) |
| `yield return` 생성 | `OmniSight/Devices/DeviceFactory.cs:99-120` | UNUSED 노드 건너뛰고 인덱스 유지 |
| 카메라 팩토리 | `OmniSight/Devices/Cameras/CameraAdapterFactory.cs:9-31` | `switch (config.VendorType)` |
| SDK 계약 | `OmniSight/Devices/Cameras/ICameraDriver.cs` | `FrameReady(Mat, string, long)` — **Mat 소유권을 넘긴다** |
| 12줄짜리 어댑터 | `OmniSight/Devices/Cameras/VendorBCameraAdapter.cs` | 생성자에서 `() => new VendorBCameraDriver()`만 넘김 |
| 어댑터 공통부 | `OmniSight/Devices/Cameras/CallbackCameraAdapterBase.cs:41-47` | `Func<ICameraDriver> driverFactory` |
| 재연결 시 새 드라이버 | `OmniSight/Devices/Cameras/CallbackCameraAdapterBase.cs:78-99` | 이전 드라이버 이벤트 해제 → Dispose → 새로 생성 |
| 콜백 → ImageFrame → 큐 | `OmniSight/Devices/Cameras/CallbackCameraAdapterBase.cs:409-444` | `ownedImage = null`, `frame = null`로 소유권 이동, finally에서 남은 것만 해제 |
| 예외: VendorA | `OmniSight/Devices/Cameras/VendorACameraAdapter.cs` | ICameraDriver를 안 쓰고 DeviceBase 직접 상속 (역사적 이유, 중복 로직) |

**집에서 Claude에게:**
```text
CameraAdapterFactory.cs, ICameraDriver.cs, VendorBCameraAdapter.cs를 보여주고
돼지코(Adapter)와 빵집(Factory) 비유로 설명해줘. 그다음 CallbackCameraAdapterBase.cs 78~99줄과
409~444줄을 보여주면서 "이벤트 구독 해제를 왜 먼저 하는지", "ownedImage = null 이 왜 필요한지" 알려줘.
```

## 4. 왜 이렇게 썼나

### 4.1 구조 그림

```text
설정 JSON ─▶ CameraAdapterFactory.Create(config)
                 │ switch VendorType
                 ├─ VendorA     → VendorACameraAdapter (DeviceBase 직접)
                 ├─ VendorB  → VendorBCameraAdapter ─┐
                 ├─ VendorC     → VendorCCameraAdapter    ─┤ CallbackCameraAdapterBase (공통: 큐, 상태, 미리보기)
                 └─ SIMULATION → SimulationCameraAdapter │        │ Func<ICameraDriver>
                                                         │        ▼
                                                         │  VendorBCameraDriver : ICameraDriver (SDK 직접 호출)
검사기는 여기만 본다 ─▶ IImageSourceDevice.WaitFrameAsync ◀─┘
```

### 4.2 결정과 이유

| 결정 | 이유 | 대안과 비교 |
|---|---|---|
| 팩토리를 `static` + `switch` | 종류가 몇 개 안 되고 설정 enum과 1:1. 가장 단순 | 리플렉션/플러그인 로딩: 유연하지만 디버깅·배포 복잡. 현장 장비엔 과함 |
| 팩토리 **한 곳**에서만 `new` | 새 벤더 추가 시 고칠 곳이 한 곳. Program/UI는 모름 | 여기저기 `new VendorACamera()` → 벤더 바꿀 때 전체 검색 |
| 생성 실패 시 역순 Dispose | 3번째 장치에서 실패하면 1·2번째 장치가 핸들을 잡은 채로 남음 | 정리 안 하면 재시작 시 "카메라 사용 중" 오류 |
| `Func<ICameraDriver>` 주입 | **재연결할 때마다 새 SDK 객체**가 필요. 객체 하나 대신 "만드는 방법"을 받음. 테스트에선 가짜 드라이버를 주입 | 드라이버 인스턴스 하나 주입: 재연결 불가 |
| 드라이버 → 어댑터로 **Mat 소유권 이동** | SDK 버퍼는 다음 콜백에서 재사용됨. 드라이버가 복사한 Mat을 넘기면 어댑터가 책임짐 | SDK 버퍼 포인터 그대로 전달: 검사 중에 다음 사진으로 덮임 |
| 이벤트 해제 후 Dispose | Dispose 도중/이후 SDK 콜백이 와도 이미 죽은 어댑터 메서드가 안 불리게 | 순서 반대면 해제된 객체로 콜백 → 크래시 |

### 4.3 약점

- VendorA는 공통 베이스를 안 써서 **같은 로직이 두 벌**(라인 스피드 게이트, 큐, 트리거 루프). 버그를 한쪽만 고칠 위험. 리팩토링 후보다.
- `switch` 팩토리는 벤더가 20개쯤 되면 커진다. 그때는 `Dictionary<VendorType, Func<...>>` 등록 방식으로 바꿀 수 있다.

## 5. Kata: 돼지코와 빵집

```csharp
// KATA: Day04
using System;
using System.Collections.Generic;

namespace Katas.Day04
{
    // 우리 집(검사기)이 원하는 모양
    public interface IKoreanPlug { void PowerOn(int volt); }

    // 외국 SDK들: 우리가 고칠 수 없는 코드라고 가정
    public sealed class UsaSocketSdk { public void Connect110V() { Console.WriteLine("USA 110V 연결"); } }
    public sealed class JapanSocketSdk { public void Start(double hz) { Console.WriteLine("Japan " + hz + "Hz 시작"); } }

    // 어댑터: 모양만 바꾼다. 로직을 넣지 않는다.
    public sealed class UsaAdapter : IKoreanPlug
    {
        private readonly UsaSocketSdk _sdk;
        public UsaAdapter(UsaSocketSdk sdk) { _sdk = sdk ?? throw new ArgumentNullException(nameof(sdk)); }
        public void PowerOn(int volt) { _sdk.Connect110V(); }
    }

    public sealed class JapanAdapter : IKoreanPlug
    {
        private readonly Func<JapanSocketSdk> _sdkFactory;   // 재연결마다 새 SDK
        private JapanSocketSdk _sdk;
        public JapanAdapter(Func<JapanSocketSdk> sdkFactory) { _sdkFactory = sdkFactory; }
        public void PowerOn(int volt)
        {
            _sdk = _sdkFactory();
            _sdk.Start(60.0);
        }
    }

    public enum Country { Usa, Japan }

    // 빵집 카운터: 메뉴 이름만 받고 어떻게 만드는지는 여기만 안다
    public static class PlugFactory
    {
        public static IKoreanPlug Create(Country country)
        {
            switch (country)
            {
                case Country.Usa: return new UsaAdapter(new UsaSocketSdk());
                case Country.Japan: return new JapanAdapter(() => new JapanSocketSdk());
                default: throw new NotSupportedException("지원하지 않는 국가: " + country);
            }
        }

        // 여러 개 만들다 실패하면 이미 만든 것을 역순으로 정리
        public static List<IKoreanPlug> CreateAll(IEnumerable<Country> countries)
        {
            var created = new List<IKoreanPlug>();
            try
            {
                foreach (Country c in countries)
                {
                    created.Add(Create(c));
                }
                return created;
            }
            catch
            {
                for (int i = created.Count - 1; i >= 0; i--)
                {
                    var disposable = created[i] as IDisposable;
                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }
                throw;
            }
        }
    }

    public static class KataDay04
    {
        public static void Run()
        {
            foreach (IKoreanPlug plug in PlugFactory.CreateAll(new[] { Country.Usa, Country.Japan }))
            {
                plug.PowerOn(220);   // 호출하는 쪽은 나라를 모른다
            }
        }
    }
}
```

## 6. MiniSight 오늘 단계

### 6.1 `Devices/Cameras/ICameraDriver.cs` (가짜 SDK의 모양)

```csharp
public interface ICameraDriver : IDisposable
{
    event Action<Mat, long> FrameReady;   // Mat 소유권을 받는 쪽에 넘긴다
    void Open(string cameraName);
    void Start();
    void Stop();
}
```

### 6.2 `FakeCameraDriver.cs` (직접 작성)

- `System.Threading.Timer`로 `intervalMs`마다 콜백.
- 콜백에서 `new Mat(480, 640, MatType.CV_8UC1, Scalar.All(0))` 위에 `Cv2.PutText`로 프레임 번호를 쓰고 `FrameReady?.Invoke(mat, id)`.
- **구독자가 없으면** 만든 Mat을 직접 Dispose (소유권을 받을 사람이 없으니까).
- `Stop()`은 타이머를 멈추고, `Dispose()`는 타이머 Dispose.

### 6.3 `DriverCameraAdapter.cs : DeviceBase, IImageSource`

- 생성자: `(string name, Func<ICameraDriver> driverFactory)`.
- `OnInitialize`: 드라이버 생성 → `FrameReady += OnFrame` → `Open`.
- `OnStart/OnStop`: 드라이버 Start/Stop.
- `OnFrame(Mat mat, long id)`: `ImageFrame` 생성(소유권 이동) → `FrameCaptured` 이벤트 → 내부 큐에 Add. 실패하면 finally에서 Dispose.
- 큐는 오늘은 임시로 `BlockingCollection<ImageFrame>(8)` 사용, `WaitFrameAsync`는 `Task.Run(() => _queue.Take(ct), ct)`로 임시 구현. **Day11에서 FrameQueue로 교체**한다(이 임시 구현이 왜 아쉬운지 Day11에서 배움).
- `OnDispose`: 이벤트 해제 → 드라이버 Dispose → 큐에 남은 프레임 Dispose.

### 6.4 `FolderCamera.cs : DeviceBase, IImageSource`

- 폴더의 PNG 목록을 `Directory.GetFiles`로 읽어 순환하며 `Cv2.ImRead`. 나머지는 6.3과 같은 모양.

### 6.5 `CameraFactory.cs`

```csharp
public enum CameraKind { Fake, Folder }
public static IImageSource Create(CameraKind kind, string name, string folderOrNull, int intervalMs)
// switch로 생성. 알 수 없는 값은 NotSupportedException
```

## 7. VS2022 디버깅 실습

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | `CameraFactory.Create(...)` 호출 줄에서 멈춘 뒤 **직접 실행 창(Immediate, Ctrl+Alt+I)** 열기 | |
| 2 | Immediate에 `CameraFactory.Create(CameraKind.Fake, "T", null, 100).Name` 입력 | 코드를 고치지 않고 메서드를 즉시 실행 |
| 3 | `new DriverCameraAdapter("A", () => new FakeCameraDriver(50))` 처럼 한 줄에 메서드 호출이 여러 개인 줄에서 우클릭 → **특정 항목으로 한 단계씩 코드 실행(Step Into Specific)** | 람다/생성자 중 원하는 것만 골라 들어감 |
| 4 | `OnFrame` 에 중단점 → 디버그 → 창 → **스레드(Ctrl+Alt+H)** | 타이머 콜백은 **작업자 스레드**에서 온다 (UI 스레드 아님) — Day08·19의 복선 |
| 5 | 이벤트 해제(`-=`)를 일부러 빼고, Dispose 후에 FakeCameraDriver 타이머를 1번 더 돌게 해 본다 | 죽은 어댑터로 콜백이 들어오는 걸 확인 → 원래대로 복구 |

## 8. 셀프 체크

2회차에서는 조각별로 확인한 Factory 생성 책임·미지원 값 거부·Adapter 연결·Func 생성 시점·사용자 실행 결과를 함께 본다. 위 회사 비교의 공통 호출 유지 질문과 소유권 복습을 마무리한다. Factory Method/Abstract Factory의 분류 비교는 추가 자료이며, Func로 생성 함수를 전달한다는 사실만으로 상속 기반 Factory Method 패턴과 동일한 것은 아니다.

1. Simple Factory, Factory Method, Abstract Factory 차이를 한 줄씩.
<details><summary>답</summary>Simple Factory: 정적 메서드 하나가 조건에 따라 구체 객체를 만든다(OmniSight CameraAdapterFactory). Factory Method: 부모가 "만드는 메서드"를 정의하고 자식이 어떤 객체를 만들지 결정한다. Abstract Factory: 관련 객체 가족(카메라+조명+PLC)을 한 세트로 만드는 인터페이스.</details>

2. 어댑터 안에 검사 로직을 넣으면 왜 안 되나?
<details><summary>답</summary>어댑터의 책임은 인터페이스 모양 변환이다. 로직이 들어가면 벤더마다 검사 결과가 달라지고, 벤더를 바꿀 때 로직까지 옮겨야 한다. 공통 로직은 베이스 클래스나 파이프라인에 둔다.</details>

3. 생성자에 드라이버 객체 대신 `Func<ICameraDriver>`를 받는 이유는?
<details><summary>답</summary>SDK 객체는 연결이 끊기면 재사용할 수 없는 경우가 많아 재연결 때마다 새로 만들어야 한다. 또한 테스트에서 가짜 드라이버를 주입할 수 있고, 생성 시점을 어댑터가 제어할 수 있다.</details>

4. SDK 콜백에서 받은 버퍼를 복사해야 하는 이유는?
<details><summary>답</summary>SDK가 빌려준 버퍼를 콜백 뒤 재사용하는 경우, 비동기 검사 중 다음 영상으로 덮일 수 있으므로 앱 소유 Mat으로 복사해 독립 수명을 만든다. 회사 ICameraDriver.FrameReady 계약은 SDK 버퍼와 독립된 Mat의 소유권을 전달하는 것이므로, 받은 Mat을 무조건 다시 복사해야 한다는 뜻은 아니다. 이번 FakeCameraDriver도 직접 새 Mat을 만들어 전달한다.</details>

5. DeviceFactory가 실패 시 역순으로 Dispose하는 이유는?
<details><summary>답</summary>나중에 만든 장치가 먼저 만든 장치에 의존할 수 있다(카메라가 소켓 스냅샷을 참조 등). 의존하는 쪽부터 정리하는 것이 안전하다. 또 부분 생성된 장치가 핸들을 잡고 있으면 재시작 시 장치 점유 오류가 난다.</details>

**2회차 완료 (2026-11-05, 약 70% 기준):** 조각 1~8에서 종류별 생성과 잘못된 주문 거부, 외부 API를 Name 계약에 연결하는 Adapter, 호출마다 new를 실행하는 Func, Initialize에서 생성 함수 실행을 코드와 답변으로 확인했다. 실제 사용자 버튼에서 Factory 경로의 프레임 수신·정리 메시지 True/True/True를 받았고, Dispose/null 보충 콘솔에서도 False/False/True/False 출력이 일치했다. Factory 몸통 복습에서는 Folder 분기를 원본에서 다시 본 사실을 기록했다. 로컬 구현 리뷰와 MiniSight Debug/x64·C# 7.3 빌드를 확인했으며 프레임·미지원 종류·소유권의 모의 실행 검증 범위는 각 조각에 남겼다.

추가 복습: 같은 공통 계약이면 버튼의 사용 호출을 유지할 수 있다는 이유 설명(Day05), 참조가 남은 상태와 이미지 자원 정리를 구별하는 소유권 설명(Day09), Folder 생성 인수와 실제 폴더 로딩(확장), Factory 패턴 종류 비교(Day16). F11 관찰은 사용자 별도 보고 전이고, 실제 카메라·SDK 재연결·콜백 완전 종료·회사 장비 동작을 검증한 것은 아니다. 다음 단원은 Day05다.
