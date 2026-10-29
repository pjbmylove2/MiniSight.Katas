# B09 · 갈림길과 꼬리표 (`enum` `switch` `is` 패턴 `catch when` `static` `readonly` `const`) · 35분

## 1. 오늘 목표
OK/NG/측정 실패를 **enum + switch**로 나누고, 예외를 **종류별로** 다르게 처리한다. 그리고 자주 보이는 수식어 3개(`static`, `readonly`, `const`)의 차이를 안다.

## 2. 🧒 비유
- **enum = 정해진 도장 3개**: OK 도장, NG 도장, 에러 도장. 다른 도장은 없다. 오타 낼 일이 없다.
- **switch = 갈림길 표지판**: 도장이 OK면 왼쪽, NG면 오른쪽, 나머지는 직진.
- **is 패턴 = 상자 열기 전에 라벨 보기**: "이 상자가 글자 상자면 꺼내서 s라고 부르자."
- **catch when = 조건 붙은 그물**: "IOException 중에서도 **디스크** 얘기만 이 그물로."
- **static = 반 전체 칠판**(모두가 하나를 같이 봄), **readonly = 이름표**(처음 한 번 붙이면 못 바꿈), **const = 교실 문에 새긴 숫자**(만들 때부터 영원히 고정).

## 3. 키워드 표

| 키워드 | 역할 | 사용법 | 기억할 점 |
|---|---|---|---|
| `enum` | 정해진 이름 목록 | `public enum Judge { Ok, Ng, Error }` | 내부는 숫자 0,1,2. 문자열 비교보다 안전 |
| `switch (x) { case A: ... break; }` | 값에 따라 갈림 | | 각 case 끝에 `break;` 필수 |
| `default:` | 어느 case에도 안 맞을 때 | | **항상 넣는다** (새 값이 추가될 때 대비) |
| `&&` / `\|\|` / `!` | 그리고 / 또는 / 아니다 | `w >= Min && w <= Max` | |
| `o is string s` | o가 string이면 true + s에 담기 (C# 7.0+) | `if (o is string s) { ... }` | 확인과 변환을 한 번에 |
| `o.GetType().Name` | 실제 타입 이름 | `Double` | |
| `catch (A ex)` 여러 개 | 위에서부터 **처음 맞는 것** 하나만 | | 구체적인 것을 위에, `Exception`은 맨 아래 |
| `catch (A ex) when (조건)` | 타입이 맞고 **조건도 true일 때만** 잡음 (C# 6+) | `when (ex.Message.Contains("디스크"))` | 조건이 false면 잡지 않고 밖으로 보냄 |
| `static` 필드 | 객체마다가 아니라 **클래스에 하나** | `public static int TotalCreated;` | ⚠️ 여러 스레드가 같이 보면 lock 필요 (B13) |
| `readonly` 필드 | 생성자에서만 정할 수 있음 | `public readonly string Name;` | 실행 중 바뀌면 안 되는 값 |
| `const` | **컴파일할 때** 고정되는 상수 | `private const double MinWidth = 9.8;` | 숫자·문자열만. 진짜로 안 바뀌는 값에만 |
| `partial class` | 한 클래스를 **여러 파일에** 나눠 씀 | `Form1.cs` + `Form1.Designer.cs` | WinForms 화면이 이렇게 되어 있다 (OmniSight 81곳) |

## 4. 코드: `Basics/KataB09.cs`

```csharp
using System;
using System.IO;

namespace Katas.Basics
{
    public enum Judge
    {
        Ok,
        Ng,
        Error
    }

    public class Counter
    {
        public static int TotalCreated;       // 모든 Counter가 같이 쓰는 칠판
        public readonly string Name;          // 생성자에서 한 번만 정함

        public Counter(string name)
        {
            Name = name;
            TotalCreated = TotalCreated + 1;
        }
    }

    public static class KataB09
    {
        private const double MinWidth = 9.8;
        private const double MaxWidth = 10.2;

        public static void Run()
        {
            // 1) enum + switch
            double[] widths = { 10.0, 10.3, -1 };
            foreach (double w in widths)
            {
                Judge j = Check(w);
                switch (j)
                {
                    case Judge.Ok:
                        Console.WriteLine(w + " -> OK");
                        break;
                    case Judge.Ng:
                        Console.WriteLine(w + " -> NG (PLC에 NG 신호)");
                        break;
                    default:
                        Console.WriteLine(w + " -> 측정 실패 (" + j + ")");
                        break;
                }
            }

            // 2) is 패턴: 타입 확인 + 변수 만들기를 한 번에
            object[] things = { "cam1", 42, 3.14, null };
            foreach (object o in things)
            {
                if (o is string s)
                {
                    Console.WriteLine("문자열: " + s.ToUpper());
                }
                else if (o is int n)
                {
                    Console.WriteLine("정수 + 1: " + (n + 1));
                }
                else if (o == null)
                {
                    Console.WriteLine("null");
                }
                else
                {
                    Console.WriteLine("기타 타입: " + o.GetType().Name);
                }
            }

            // 3) catch 순서와 when 필터
            string[] kinds = { "timeout", "disk" };
            foreach (string kind in kinds)
            {
                try
                {
                    Fail(kind);
                }
                catch (TimeoutException ex)
                {
                    Console.WriteLine("재시도할 오류: " + ex.Message);
                }
                catch (IOException ex) when (ex.Message.Contains("디스크"))
                {
                    Console.WriteLine("오래된 이미지 정리 필요: " + ex.Message);
                }
            }

            // 4) static / readonly / const
            Counter a = new Counter("A");
            Counter b = new Counter("B");
            Console.WriteLine("만든 개수(static): " + Counter.TotalCreated);
            Console.WriteLine("이름(readonly): " + a.Name + ", " + b.Name);
            Console.WriteLine("규격(const): " + MinWidth + " ~ " + MaxWidth);
        }

        private static Judge Check(double width)
        {
            if (width < 0)
            {
                return Judge.Error;
            }
            if (width >= MinWidth && width <= MaxWidth)
            {
                return Judge.Ok;
            }
            return Judge.Ng;
        }

        private static void Fail(string kind)
        {
            if (kind == "timeout")
            {
                throw new TimeoutException("PLC 응답 없음");
            }
            throw new IOException("디스크 공간 부족");
        }
    }
}
```

## 5. 실행 결과 (검증함)
```text
10 -> OK
10.3 -> NG (PLC에 NG 신호)
-1 -> 측정 실패 (Error)
문자열: CAM1
정수 + 1: 43
기타 타입: Double
null
재시도할 오류: PLC 응답 없음
오래된 이미지 정리 필요: 디스크 공간 부족
만든 개수(static): 2
이름(readonly): A, B
규격(const): 9.8 ~ 10.2
```
👀 `-1`은 NG가 아니라 **측정 실패**다. 검사 프로그램에서 "측정을 못 했다"를 "불량"과 섞으면 안 된다. 그래서 도장을 3개로 나눴다.

## 6. 바꿔 보기 (1개)
`a.Name = "C";`를 `Run()`의 4) 부분에 추가하고 빌드한다.
→ **CS0191** 에러: 읽기 전용 필드에는 생성자 안에서만 할당할 수 있다. `readonly`가 "실행 중에 바꾸지 마"를 컴파일러로 지켜 준다. 확인했으면 지운다.

## 7. 어디서 다시 나오나
| Day | 코드 | 뜻 |
|---|---|---|
| Day03 | `enum DeviceState { Created, ... }` + 상태 검사 | 장치 상태 도장 |
| Day04 | `switch (kind)` 팩토리, `default: throw new NotSupportedException` | 모르는 카메라 종류는 거절 |
| Day06 | State Machine | enum + switch에 붙은 이름 |
| Day22 | `catch (SocketException ex) when (...)` | 통신 오류 종류별 처리 |

## 8. 셀프 체크
<details><summary>Q1. switch에 default를 꼭 넣는 이유는?</summary>

나중에 enum에 새 값이 추가되거나 이상한 값이 들어와도 조용히 아무것도 안 하고 지나가지 않게 하려는 것이다. 장비에서는 "모르는 값 → 안전한 쪽(에러/정지)"으로 처리한다.
</details>

<details><summary>Q2. const와 readonly의 차이는?</summary>

const는 컴파일할 때 값이 박혀서 절대 안 바뀌고 숫자·문자열만 된다. readonly는 실행 중 생성자에서 한 번 정할 수 있고 어떤 타입이든 된다. 설정에서 읽는 값은 readonly, 수학 상수처럼 진짜 고정은 const.
</details>

## 9. 체크
- [ ] 1번째: 보면서 타이핑 → 실행
- [ ] 2번째: 실행 결과 비교
- [ ] 3번째: 지우고 안 보고 다시 타이핑
- [ ] 바꿔 보기
