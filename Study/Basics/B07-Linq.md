# B07 · 목록에서 골라내기 (LINQ: `Where` `Select` `FirstOrDefault` `Any` `Count` `OrderBy`) · 30분

## 1. 오늘 목표
검사 결과 목록에서 "NG만", "Cam2 첫 결과", "평균 폭"을 **한 줄로** 뽑는다. OmniSight에서 `Where`/`Select`/`FirstOrDefault`/`ToList`는 합쳐서 160번 넘게 나온다.

## 2. 🧒 비유: 체에 거르기
- 결과 카드 묶음이 있다.
- **`Where`** = 체에 걸러 **조건에 맞는 카드만** 남기기 ("NG 카드만").
- **`Select`** = 카드에서 **번호만 베껴 적기**.
- **`FirstOrDefault`** = 맨 앞에서부터 찾다가 **첫 번째 하나**. 없으면 빈손(null).
- **`Any`** = "하나라도 있어?" (예/아니오)
- 조건은 B03의 **람다**로 적는다: `r => !r.IsOk` = "카드 r을 받아서, OK가 아니면 true".

## 3. 키워드 표

| 키워드 | 역할 | 사용법 | 결과 타입 |
|---|---|---|---|
| `using System.Linq;` | LINQ 메서드를 쓰겠다 | 파일 맨 위 | 안 쓰면 `.Where`가 안 보인다 |
| `.Where(r => 조건)` | 조건이 true인 것만 | `results.Where(r => !r.IsOk)` | 목록 |
| `.Select(r => 값)` | 각 항목을 다른 값으로 바꾸기 | `.Select(r => r.Id)` | 목록 |
| `.ToList()` | **지금 바로** 계산해서 List로 | `.Where(...).ToList()` | `List<T>` |
| `.FirstOrDefault(r => 조건)` | 첫 번째 하나, 없으면 `null` (숫자면 0) | | 항목 하나 |
| `.First(...)` | 첫 번째 하나, ⚠️ **없으면 예외** | | 항목 하나 |
| `.Any(r => 조건)` | 하나라도 있나? | | bool |
| `.Count(r => 조건)` | 조건에 맞는 개수 | | int |
| `.Average(r => 값)` / `.Max` / `.Min` / `.Sum` | 평균 / 최대 / 최소 / 합 | `.Average(r => r.Width)` | 숫자 |
| `.OrderBy(x => 값)` / `.OrderByDescending` | 작은 순 / 큰 순 정렬 | | 목록 |
| `new X { A = 1, B = 2 }` | 객체 만들면서 속성 채우기 (객체 초기화) | | |

**`Where`를 길게 풀면**
```csharp
List<InspectResult> ngList = new List<InspectResult>();
foreach (InspectResult r in results)
{
    if (!r.IsOk)
    {
        ngList.Add(r);
    }
}
```

## 4. 코드: `Basics/KataB07.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace Katas.Basics
{
    public class InspectResult
    {
        public int Id { get; set; }
        public string Camera { get; set; }
        public bool IsOk { get; set; }
        public double Width { get; set; }
    }

    public static class KataB07
    {
        public static void Run()
        {
            List<InspectResult> results = new List<InspectResult>
            {
                new InspectResult { Id = 1, Camera = "Cam1", IsOk = true,  Width = 10.02 },
                new InspectResult { Id = 2, Camera = "Cam1", IsOk = false, Width = 10.31 },
                new InspectResult { Id = 3, Camera = "Cam2", IsOk = true,  Width = 9.98 },
                new InspectResult { Id = 4, Camera = "Cam2", IsOk = false, Width = 9.62 },
                new InspectResult { Id = 5, Camera = "Cam1", IsOk = true,  Width = 10.05 }
            };

            // Where: 조건에 맞는 것만 거르기
            List<InspectResult> ngList = results.Where(r => !r.IsOk).ToList();
            Console.WriteLine("NG 개수: " + ngList.Count);

            // Select: 필요한 값만 뽑기
            List<int> ngIds = ngList.Select(r => r.Id).ToList();
            Console.WriteLine("NG 번호: " + string.Join(", ", ngIds));

            // FirstOrDefault: 첫 번째 하나, 없으면 null
            InspectResult firstCam2 = results.FirstOrDefault(r => r.Camera == "Cam2");
            Console.WriteLine("Cam2 첫 결과: #" + firstCam2.Id);

            InspectResult cam9 = results.FirstOrDefault(r => r.Camera == "Cam9");
            if (cam9 == null)
            {
                Console.WriteLine("Cam9 결과: 없음");
            }

            // Any / Count / Average / Max
            Console.WriteLine("NG가 하나라도 있나? " + results.Any(r => !r.IsOk));
            Console.WriteLine("Cam1 검사 수: " + results.Count(r => r.Camera == "Cam1"));
            Console.WriteLine("평균 폭: " + Math.Round(results.Average(r => r.Width), 3));
            Console.WriteLine("최대 폭: " + results.Max(r => r.Width));

            // OrderByDescending: 큰 것부터 정렬
            Console.WriteLine("폭 큰 순서:");
            foreach (InspectResult item in results.OrderByDescending(x => x.Width))
            {
                Console.WriteLine("  #" + item.Id + " " + item.Width);
            }
        }
    }
}
```

## 5. 실행 결과 (검증함)
```text
NG 개수: 2
NG 번호: 2, 4
Cam2 첫 결과: #3
Cam9 결과: 없음
NG가 하나라도 있나? True
Cam1 검사 수: 3
평균 폭: 9.996
최대 폭: 10.31
폭 큰 순서:
  #2 10.31
  #5 10.05
  #1 10.02
  #3 9.98
  #4 9.62
```

## 6. 바꿔 보기 (1개)
`if (cam9 == null) { ... }` 블록을 지우고 `Console.WriteLine("Cam9 결과: #" + cam9.Id);`로 바꿔 실행한다.
→ `NullReferenceException`. `FirstOrDefault`는 못 찾으면 **null**을 준다. 받은 쪽이 확인해야 한다 (B01 `?.`도 방법). 확인했으면 되돌린다.

## 7. 현장 주의 (한 줄씩)
- LINQ는 편하지만 **매 프레임 도는 검사 코드(핫 패스)**에서는 새 목록을 계속 만들어 GC 부담이 생길 수 있다. 화면, 설정, 통계에서는 마음껏 쓰고, 프레임마다 도는 곳에서는 `for`가 나을 때가 있다 (Day16·성능 점검 때 확인).
- `.ToList()`를 안 붙이면 **꺼낼 때마다 다시 계산**한다. 그 사이에 원본이 바뀌면 결과도 바뀐다. 결과를 저장하려면 `.ToList()`를 붙인다.

## 8. 셀프 체크
<details><summary>Q1. First와 FirstOrDefault의 차이는?</summary>

못 찾으면 First는 예외(`InvalidOperationException`)를 던지고, FirstOrDefault는 null(숫자 타입이면 0)을 준다.
</details>

<details><summary>Q2. results.Where(r =&gt; !r.IsOk)에서 r은 무엇인가?</summary>

람다의 인자다. 목록의 항목이 하나씩 r로 들어오고, 오른쪽 식이 true인 항목만 남는다.
</details>

## 9. 체크
- [ ] 1번째: 보면서 타이핑 → 실행
- [ ] 2번째: 실행 결과 비교
- [ ] 3번째: 지우고 안 보고 다시 타이핑
- [ ] 바꿔 보기
