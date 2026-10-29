# B01 · null 다루기와 짧은 기호 (`?.` `??` `throw` 식 `nameof` `=>`) · 25분

## 1. 오늘 목표
Day02~04 코드에 계속 나온 **짧은 기호 5개**를 읽고 쓸 수 있다.

## 2. 🧒 비유: 도시락 확인
- 친구가 도시락을 **안 가져왔을 수도** 있다 (`null` = 아무것도 없음).
- 없는 도시락 뚜껑을 열려고 하면 넘어진다 → `NullReferenceException`.
- 그래서 "**있으면** 열어 보고(`?.`), **없으면** 매점 빵 먹어(`??`)"라고 미리 정한다.

## 3. 키워드 표

| 기호 | 역할 | 사용법 | 같은 뜻의 긴 코드 |
|---|---|---|---|
| `?.` | 왼쪽이 null이면 **멈추고 null**을 준다. null이 아닐 때만 `.` 뒤를 실행 | `friend?.Menu` | `friend == null ? null : friend.Menu` |
| `??` | 왼쪽이 null이면 **오른쪽 값**을 쓴다 | `a ?? "기본값"` | `if (a != null) { x = a; } else { x = "기본값"; }` |
| `?? throw` | 왼쪽이 null이면 **예외를 던진다** (C# 7.0+) | `_name = name ?? throw new ArgumentNullException(nameof(name));` | `if (name == null) { throw new ArgumentNullException("name"); } _name = name;` |
| `nameof(x)` | 변수·속성 **이름을 문자열**로 준다. 이름을 바꾸면 같이 바뀜 | `nameof(name)` → `"name"` | `"name"` (오타 나도 컴파일러가 모름) |
| `=>` (식 본문) | 한 줄짜리 속성·메서드를 짧게 쓴다 | `public string Name => _name;` | `public string Name { get { return _name; } }` |

⚠️ `=>`는 **람다**(B03)에서도 쓰인다. 오늘은 "속성·메서드를 짧게 쓰는 것"만 본다.

## 4. 코드: `Basics/KataB01.cs`

```csharp
using System;

namespace Katas.Basics
{
    public class Lunchbox
    {
        public string Menu { get; set; }       // 도시락 메뉴
    }

    public class Student
    {
        private readonly string _name;

        public Student(string name)
        {
            // name이 null이면 바로 예외. 아니면 _name에 저장
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string Name => _name;                       // 읽기 전용 속성 (짧게)

        public string Hello() => "안녕, 나는 " + _name;     // 한 줄 메서드 (짧게)
    }

    public static class KataB01
    {
        public static void Run()
        {
            Lunchbox mine = new Lunchbox();
            mine.Menu = "김밥";
            Lunchbox friend = null;                 // 도시락을 안 가져옴

            string a = mine?.Menu;                  // 있으니까 "김밥"
            string b = friend?.Menu;                // 없으니까 null (넘어지지 않음!)
            Console.WriteLine("내 도시락: " + a);
            Console.WriteLine("친구 도시락: " + b);   // 문자열 + null = 빈 문자열

            string c = friend?.Menu ?? "매점 빵";    // null이면 오른쪽 값
            Console.WriteLine("친구가 먹을 것: " + c);

            Student s = new Student("철수");
            Console.WriteLine(s.Name);
            Console.WriteLine(s.Hello());
        }
    }
}
```

## 5. 실행 결과 (검증함)
```text
내 도시락: 김밥
친구 도시락: 
친구가 먹을 것: 매점 빵
철수
안녕, 나는 철수
```
👀 둘째 줄이 비어 있다. `friend?.Menu`가 null을 돌려줬고, `"..." + null`은 그냥 빈 문자열이 된다. **에러가 아니다.**

## 6. 중괄호 규칙 (goto fail)
2014년 Apple의 보안 연결 코드에서 실제로 난 버그다 (CVE-2014-1266).
```c
if ((err = SSLHashSHA1.update(&hashCtx, &signedParams)) != 0)
    goto fail;
    goto fail;      // 실수로 한 줄 복사됨 → 조건과 상관없이 항상 실행
```
들여쓰기 때문에 두 줄 모두 `if` 안에 있는 것처럼 **보이지만**, 중괄호가 없으면 `if`는 **바로 다음 한 줄만** 책임진다. 그 결과 서명 검증을 건너뛰고 "성공"을 반환했다.

```csharp
// ❌ 우리 분야라면: 양품에도 NG 신호가 PLC로 간다
if (result == Ng)
    SaveNgImage();
    SendNgToPlc();

// ✅ 그래서 한 줄이어도 무조건 중괄호
if (result == Ng)
{
    SaveNgImage();
    SendNgToPlc();
}
```
VS2022에서는 **도구 → 옵션 → 텍스트 편집기 → C# → 코드 스타일 → 일반 → "중괄호 기본 설정"**을 "예"로 두면, 빠진 곳에 경고 줄이 생긴다.

## 7. 바꿔 보기 (1개)
`new Student("철수")`를 `new Student(null)`로 바꿔 실행한다.
```text
처리되지 않은 예외: System.ArgumentNullException: 값은 null일 수 없습니다.
매개 변수 이름: name
```
`nameof(name)`이 `"name"`이 되어 **어느 값이 문제였는지** 메시지에 찍혔다. 확인했으면 되돌린다.

## 8. 어디서 다시 나오나
| Day | 코드 | 뜻 |
|---|---|---|
| Day04 | `_timer?.Change(...)` | 타이머가 있을 때만 멈춘다 |
| Day04 | `frame?.Dispose(); mat?.Dispose();` | 아직 손에 남아 있으면 버린다 (이미 넘겼으면 null이라 건너뜀) |
| Day04 | `public override string Name => _name;` | 식 본문 속성 |
| Day04 | `_name = name ?? throw new ArgumentNullException(nameof(name));` | 생성자 인자 검사 |

## 9. 셀프 체크
<details><summary>Q1. `friend.Menu`와 `friend?.Menu`는 friend가 null일 때 각각 어떻게 되나?</summary>

`friend.Menu`는 `NullReferenceException`으로 넘어진다. `friend?.Menu`는 넘어지지 않고 null을 준다.
</details>

<details><summary>Q2. 왜 `"name"` 대신 `nameof(name)`을 쓰나?</summary>

나중에 변수 이름을 바꾸면(이름 바꾸기 F2) `nameof`도 같이 바뀐다. 문자열 `"name"`은 그대로 남아서 메시지가 틀린 이름을 가리킨다.
</details>

## 10. 체크
- [ ] 1번째: 보면서 타이핑 → 실행
- [ ] 2번째: 실행 결과 비교
- [ ] 3번째: 지우고 안 보고 다시 타이핑
- [ ] 바꿔 보기
