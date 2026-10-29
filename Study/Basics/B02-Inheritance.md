# B02 · 부모·자식 클래스 (`interface` `abstract` `virtual` `override` `base` `protected`) · 35분

## 1. 오늘 목표
Day02(인터페이스)와 Day03(DeviceBase)이 **이 6개 키워드의 조합일 뿐**이라는 것을 손으로 확인한다.

## 2. 🧒 비유: 동물 소리 장난감 키트
- **interface = 약속 종이**: "이 장난감은 이름이 있고, 버튼을 누르면 말해야 한다." 어떻게 말할지는 안 적혀 있다.
- **abstract class = 반쯤 조립된 키트**: 버튼을 누르면 **"이름 → 소리 → 인사" 순서**로 나오도록 몸통은 다 만들어 놨다. 단, **소리 부품은 비워 둠**(`abstract`) → 사는 사람이 반드시 끼워야 한다.
- **virtual = 기본 부품**: 인사 부품은 "안녕"이 기본으로 들어 있다. 바꾸고 싶으면 바꿔도 된다(`override`).
- **base = 원래 부품도 같이 쓰기**: "기본 인사 + 냥"처럼 원래 것에 덧붙인다.

## 3. 키워드 표

| 키워드 | 역할 | 사용법 | 기억할 점 |
|---|---|---|---|
| `interface` | **무엇을 할 수 있는지**만 약속. 몸통 없음 | `public interface ISpeaker { void Speak(); }` | 이름은 `I`로 시작. 클래스가 `: ISpeaker`로 약속을 지킨다 |
| `abstract class` | 반쯤 만든 클래스. **`new`로 직접 못 만든다** | `public abstract class Animal` | 자식 클래스를 통해서만 쓴다 |
| `abstract` 메서드 | 몸통 없는 메서드. **자식이 반드시** 채운다 | `protected abstract string Sound();` | 안 채우면 컴파일 에러 |
| `virtual` 메서드 | 몸통(기본 동작)이 있는 메서드. 자식이 **원하면** 바꾼다 | `protected virtual string Bye() { return "안녕"; }` | 안 바꾸면 기본 동작 |
| `override` | 부모의 `abstract`/`virtual` 메서드를 **내 것으로 바꾼다** | `protected override string Sound() { ... }` | 부모가 `abstract`/`virtual`이 아니면 못 씀 |
| `base.메서드()` | 부모 클래스의 **원래 버전**을 부른다 | `return base.Bye() + "... 냥";` | |
| `protected` | **나와 내 자식만** 볼 수 있다 | `protected abstract string Sound();` | `public`은 모두, `private`은 나만 |

## 4. 코드: `Basics/KataB02.cs`

```csharp
using System;

namespace Katas.Basics
{
    // 약속 종이: 이름이 있고, 말할 수 있어야 한다
    public interface ISpeaker
    {
        string Name { get; }
        void Speak();
    }

    // 반쯤 만든 키트: 말하는 "순서"는 부모가 고정한다
    public abstract class Animal : ISpeaker
    {
        public abstract string Name { get; }        // 자식이 반드시 채움

        public void Speak()                          // virtual 아님 → 자식이 순서를 못 바꿈
        {
            Console.WriteLine("[" + Name + "]");
            Console.WriteLine("  " + Sound());
            Console.WriteLine("  " + Bye());
        }

        protected abstract string Sound();          // 자식이 반드시 채움

        protected virtual string Bye()              // 기본값 있음. 바꿔도 되고 안 바꿔도 됨
        {
            return "안녕";
        }
    }

    public class Dog : Animal
    {
        public override string Name => "강아지";

        protected override string Sound()
        {
            return "멍멍";
        }
        // Bye는 안 바꿈 → 기본 "안녕"
    }

    public class Cat : Animal
    {
        public override string Name => "고양이";

        protected override string Sound()
        {
            return "야옹";
        }

        protected override string Bye()
        {
            return base.Bye() + "... 냥";            // 부모 것 + 내 것
        }
    }

    public static class KataB02
    {
        public static void Run()
        {
            // 약속(ISpeaker)만 보고 다룬다. 강아지인지 고양이인지 몰라도 된다
            ISpeaker[] list = { new Dog(), new Cat() };
            foreach (ISpeaker s in list)
            {
                s.Speak();
            }
        }
    }
}
```

| 처음 보는 문법 | 뜻 |
|---|---|
| `ISpeaker[] list = { a, b };` | ISpeaker 칸 2개짜리 배열을 만들고 바로 채운다 |
| `foreach (ISpeaker s in list) { }` | 배열에서 하나씩 꺼내 `s`에 담고 반복 |

## 5. 실행 결과 (검증함)
```text
[강아지]
  멍멍
  안녕
[고양이]
  야옹
  안녕... 냥
```

## 6. 바꿔 보기 (1개)
`Dog` 클래스의 `Sound()` 메서드를 통째로 지우고 빌드한다 (Ctrl+Shift+B).
→ 오류 목록에 **CS0534: 'Dog'은(는) 상속된 추상 멤버 'Animal.Sound()'을(를) 구현하지 않습니다.**가 뜬다. `abstract` = "반드시 채워라"를 **컴파일러가 강제**한다. 확인했으면 되돌린다.

## 7. 어디서 다시 나오나 (중요)
이 Kata의 구조가 **Day03 DeviceBase와 똑같다.**

| B02 | Day03 MiniSight | 뜻 |
|---|---|---|
| `ISpeaker` | `IDevice` | 약속 |
| `Animal` (abstract) | `DeviceBase` (abstract) | 순서를 고정한 부모 |
| `Speak()` (virtual 아님) | `Initialize()`, `Start()` (virtual 아님) | 순서는 부모만 정한다 |
| `Sound()` (abstract) | `OnInitialize()`, `OnStart()` (abstract) | 자식이 반드시 채울 칸 |
| `Bye()` (virtual) | `OnDispose()` (virtual) | 기본값 있는 칸 |
| `Dog`, `Cat` | `TestDevice`, `DriverCameraAdapter` | 실제 장치 |

이렇게 "**순서는 부모가, 칸 채우기는 자식이**" 하는 구조에 붙은 이름이 **Template Method 패턴**이다. 패턴은 새 문법이 아니라 **이 6개 키워드를 배치하는 방법**이다.

## 8. 셀프 체크
<details><summary>Q1. abstract 메서드와 virtual 메서드의 차이는?</summary>

abstract는 몸통이 없어서 자식이 **반드시** 채워야 한다. virtual은 기본 몸통이 있어서 자식이 **원할 때만** 바꾼다.
</details>

<details><summary>Q2. 왜 Speak()는 virtual로 만들지 않았나?</summary>

자식이 Speak()를 바꿀 수 있으면 "이름 → 소리 → 인사" 순서를 깰 수 있다. 순서를 지키려고 부모만 정하게 막은 것이다. DeviceBase가 Initialize/Start를 virtual로 만들지 않은 것도 같은 이유다 (상태 전이 순서 보호).
</details>

## 9. 체크
- [ ] 1번째: 보면서 타이핑 → 실행
- [ ] 2번째: 실행 결과 비교
- [ ] 3번째: 지우고 안 보고 다시 타이핑
- [ ] 바꿔 보기
