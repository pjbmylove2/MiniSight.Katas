# B08 · 글자·숫자·시간 (`$""` 형식 지정 `Stopwatch` `DateTime` `TimeSpan` `CultureInfo`) · 30분

## 1. 오늘 목표
로그 한 줄, 이미지 파일 이름, **검사 시간(택트)** 측정을 직접 쓴다. 그리고 현장에서 실제로 터지는 **소수점 문제(CultureInfo)**를 눈으로 본다. OmniSight에서 `$"`는 271번, `Stopwatch`는 127번 나온다.

## 2. 🧒 비유
- **`$""` = 빈칸 채우기 문제지**: `"{cam} 카메라의 {id}번 사진"`. 중괄호 칸에 값이 들어간다.
- **형식 지정 = 칸에 쓰는 규칙**: "소수 둘째 자리까지", "네 자리로 맞추고 앞은 0".
- **Stopwatch = 달리기 초시계**: 시작 → 멈춤 → 몇 ms 걸렸나.
- **CultureInfo = 나라마다 다른 숫자 쓰는 법**: 한국은 `10.5`, 독일은 `10,5`. 독일식 PC에서 `"10.5"`를 읽으면 **105**가 된다!

## 3. 키워드 표

| 키워드 | 역할 | 사용법 | 기억할 점 |
|---|---|---|---|
| `$"...{x}..."` | 문자열 보간 (C# 6+) | `$"{cam} #{id}"` | `"..." + x + "..."`와 같은 결과, 읽기 쉬움 |
| `string.Format("{0} {1}", a, b)` | 옛날 방식 빈칸 채우기 | | 번호(`{0}`)로 순서 지정 |
| `{x:F2}` | 소수 2자리 (반올림) | `$"{width:F2}"` → `10.05` | F3이면 3자리 |
| `{x:D4}` | 정수 4자리, 앞은 0 | `$"{id:D4}"` → `0007` | 파일 이름 정렬에 필수 |
| `{t:yyyy-MM-dd HH:mm:ss}` | 날짜·시간 형식 | | **MM=월, mm=분**, HH=24시간 |
| `DateTime` | 날짜+시각 | `DateTime.Now`, `new DateTime(2026, 10, 28, 9, 5, 3)` | 로그 시각, 파일 이름 |
| `TimeSpan` | 시간의 **길이** | `TimeSpan.FromMilliseconds(100)` | `.TotalMilliseconds` |
| `Stopwatch` | 정밀한 초시계 | `Stopwatch sw = Stopwatch.StartNew(); ... sw.Stop();` | `using System.Diagnostics;` |
| `sw.ElapsedMilliseconds` / `sw.Elapsed` | 걸린 ms(long) / 걸린 시간(TimeSpan) | | 처리 시간 측정은 `DateTime.Now` 빼기 말고 Stopwatch |
| `double.Parse(s, CultureInfo.InvariantCulture)` | **나라 상관없이** `.`을 소수점으로 읽기 | | 설정 파일·통신 값은 **항상 Invariant** |
| `double.TryParse(s, 스타일, 문화, out double v)` | 숫자가 아니면 예외 대신 false | | 밖에서 들어온 값은 TryParse |

## 4. 코드: `Basics/KataB08.cs`

```csharp
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace Katas.Basics
{
    public static class KataB08
    {
        public static void Run()
        {
            string cam = "Cam1";
            int id = 7;
            double width = 10.0456;
            DateTime shotAt = new DateTime(2026, 10, 28, 9, 5, 3);

            // 1) 문자열 만들기 3가지 (결과는 같다)
            Console.WriteLine("A: " + cam + " #" + id + " 폭=" + width);
            Console.WriteLine(string.Format("B: {0} #{1} 폭={2}", cam, id, width));
            Console.WriteLine($"C: {cam} #{id} 폭={width}");

            // 2) 형식 지정
            Console.WriteLine($"소수 2자리: {width:F2}");
            Console.WriteLine($"0 채우기:  {id:D4}");
            Console.WriteLine($"시각:      {shotAt:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"파일 이름: {cam}_{shotAt:yyyyMMdd_HHmmss}_{id:D4}.png");

            // 3) Stopwatch: 처리 시간(택트) 재기
            Stopwatch sw = Stopwatch.StartNew();
            Thread.Sleep(120);                       // 검사하는 척
            sw.Stop();
            Console.WriteLine($"검사 시간: {sw.ElapsedMilliseconds} ms");

            TimeSpan limit = TimeSpan.FromMilliseconds(100);
            if (sw.Elapsed > limit)
            {
                Console.WriteLine($"  [경고] 제한 {limit.TotalMilliseconds} ms 초과");
            }

            // 4) 문자열 → 숫자, 그리고 CultureInfo
            string fromFile = "10.5";
            double a = double.Parse(fromFile, CultureInfo.InvariantCulture);
            Console.WriteLine("Invariant로 읽기:   " + a);

            double b = double.Parse(fromFile, new CultureInfo("de-DE"));
            Console.WriteLine("독일어 설정으로 읽기: " + b);

            if (double.TryParse("abc", NumberStyles.Float, CultureInfo.InvariantCulture, out double c))
            {
                Console.WriteLine("숫자: " + c);
            }
            else
            {
                Console.WriteLine("'abc'는 숫자가 아님 (예외 없이 false)");
            }
        }
    }
}
```

## 5. 실행 결과 (검증함, 검사 시간은 120~135 정도로 매번 조금 다름)
```text
A: Cam1 #7 폭=10.0456
B: Cam1 #7 폭=10.0456
C: Cam1 #7 폭=10.0456
소수 2자리: 10.05
0 채우기:  0007
시각:      2026-10-28 09:05:03
파일 이름: Cam1_20261028_090503_0007.png
검사 시간: 120 ms
  [경고] 제한 100 ms 초과
Invariant로 읽기:   10.5
독일어 설정으로 읽기: 105
'abc'는 숫자가 아님 (예외 없이 false)
```
👀 **105**를 보자. 해외 현장 PC(독일·프랑스 등)의 Windows 지역 설정이 다르면, 설정 파일의 `10.5mm` 규격이 **105mm**로 읽힌다. 에러도 안 난다. 실제로 자주 터지는 현장 사고다.

## 6. 바꿔 보기 (1개)
`{shotAt:yyyy-MM-dd HH:mm:ss}`의 `MM`을 `mm`으로 바꿔 실행한다.
→ `2026-05-28`처럼 **월 자리에 분(05)**이 들어간다. 로그 날짜가 틀려 보이는 흔한 실수다. 확인했으면 되돌린다.

## 7. 어디서 다시 나오나
| Day | 코드 | 뜻 |
|---|---|---|
| Day13 | 프레임 처리 시간 측정 | 파이프라인 단계별 Stopwatch |
| Day23 | JSON 설정 읽기 | 숫자는 Invariant로 |
| Day24 | 로거 한 줄 형식 | `{DateTime.Now:HH:mm:ss.fff}` |
| Day22 | PLC 값 표시 | 형식 지정 |

## 8. 셀프 체크
<details><summary>Q1. 처리 시간을 잴 때 DateTime.Now 두 개를 빼지 않고 Stopwatch를 쓰는 이유는?</summary>

DateTime.Now는 정밀도가 낮고(보통 수~15ms 단위), 시계 동기화(NTP)로 중간에 시각이 바뀔 수 있다. Stopwatch는 고정밀 카운터로 "걸린 길이"만 정확히 잰다.
</details>

<details><summary>Q2. 설정 파일의 숫자를 읽을 때 왜 InvariantCulture를 쓰나?</summary>

PC의 지역 설정에 따라 소수점이 `.` 또는 `,`가 되어 같은 문자열이 다른 숫자로 읽힌다. 파일·통신처럼 사람이 아니라 프로그램끼리 주고받는 값은 항상 Invariant로 읽고 쓴다.
</details>

## 9. 체크
- [ ] 1번째: 보면서 타이핑 → 실행
- [ ] 2번째: 실행 결과 비교
- [ ] 3번째: 지우고 안 보고 다시 타이핑
- [ ] 바꿔 보기
