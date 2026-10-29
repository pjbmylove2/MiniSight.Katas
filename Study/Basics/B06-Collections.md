# B06 · 목록과 사물함 (`List<T>` `Dictionary<K,V>` 제네릭 `<T>` `out var`) · 30분

## 1. 오늘 목표
카메라 목록, 카메라별 NG 개수처럼 **여러 개를 담는 그릇**을 직접 쓴다. OmniSight에서 `List<`는 263번, `Dictionary<`는 81번 나온다. 가장 많이 쓰는 그릇이다.

## 2. 🧒 비유
- **List = 줄 선 아이들**: 순서가 있다. "0번째 아이", "1번째 아이"로 부른다. 줄 끝에 세우고(`Add`) 빼낼(`Remove`) 수 있다.
- **Dictionary = 이름표 붙은 사물함**: 순서가 아니라 **이름표(키)**로 찾는다. "Cam1 사물함 열어 줘." 같은 이름표는 하나만 있다.
- **제네릭 `<T>` = 칸에 넣을 물건 종류 정하기**: `List<string>`은 "글자만 넣는 줄", `List<int>`는 "숫자만 넣는 줄"이다. 다른 걸 넣으려 하면 컴파일러가 막는다.

## 3. 키워드 표

| 키워드 | 역할 | 사용법 | 기억할 점 |
|---|---|---|---|
| `List<T>` | 순서 있는 목록. 크기가 알아서 늘어남 | `List<string> cameras = new List<string>();` | `using System.Collections.Generic;` |
| `.Add(x)` / `.Remove(x)` | 끝에 넣기 / 찾아서 빼기 | `cameras.Add("Cam1");` | |
| `.Count` | 몇 개인지 | `cameras.Count` | 배열은 `.Length`, List는 `.Count` |
| `list[i]` | i번째 꺼내기 (0부터) | `cameras[0]` | 없는 번호면 `ArgumentOutOfRangeException` |
| `new List<int> { 1, 2 }` | 만들면서 바로 채우기 (컬렉션 초기화) | | |
| `Dictionary<TKey, TValue>` | 키로 값을 찾는 사물함 | `Dictionary<string, int> ngCount` | 키는 중복 불가 |
| `dict[key] = value` | 넣기 (있으면 **덮어쓰기**) | `ngCount["Cam1"] = 0;` | |
| `dict[key]` (읽기) | 꺼내기 | `ngCount["Cam1"]` | ⚠️ **없는 키면 `KeyNotFoundException`** |
| `.TryGetValue(key, out v)` | 있으면 true + v에 값, 없으면 false | `if (ngCount.TryGetValue("Cam1", out int cam1)) { }` | 예외 없이 안전하게 꺼내기 |
| `.ContainsKey(key)` | 그 키가 있나? | | |
| `KeyValuePair<K,V>` | foreach로 꺼낸 한 칸 (`.Key`, `.Value`) | `foreach (KeyValuePair<string, int> pair in ngCount)` | |
| `out int x` (out var) | 메서드가 **결과를 채워 주는** 변수를 그 자리에서 선언 (C# 7.0+) | `TryGetValue("A", out int x)` | 옛날엔 `int x; TryGetValue("A", out x);` |
| `<T>` (제네릭) | 타입을 **나중에 정하는** 칸 | `static void PrintAll<T>(List<T> items)` | 호출할 때 `T`가 정해짐 |
| `typeof(T).Name` | T가 무슨 타입인지 이름 | `Int32`, `String` | |
| `string.Join(", ", list)` | 목록을 글자 하나로 이어 붙이기 | | |

## 4. 코드: `Basics/KataB06.cs`

```csharp
using System;
using System.Collections.Generic;

namespace Katas.Basics
{
    public static class KataB06
    {
        public static void Run()
        {
            // 1) List<T>: 순서 있는 목록
            List<string> cameras = new List<string>();
            cameras.Add("Cam1");
            cameras.Add("Cam2");
            cameras.Add("Cam3");
            Console.WriteLine("카메라 수: " + cameras.Count);
            Console.WriteLine("첫 번째: " + cameras[0]);

            cameras.Remove("Cam2");
            foreach (string name in cameras)
            {
                Console.WriteLine("  " + name);
            }

            // 2) Dictionary<TKey, TValue>: 이름표로 찾는 사물함
            Dictionary<string, int> ngCount = new Dictionary<string, int>();
            ngCount["Cam1"] = 0;
            ngCount["Cam3"] = 0;
            ngCount["Cam1"] = ngCount["Cam1"] + 1;
            ngCount["Cam1"] = ngCount["Cam1"] + 1;

            if (ngCount.TryGetValue("Cam1", out int cam1))
            {
                Console.WriteLine("Cam1 NG: " + cam1);
            }
            if (!ngCount.ContainsKey("Cam9"))
            {
                Console.WriteLine("Cam9 사물함은 없음");
            }
            foreach (KeyValuePair<string, int> pair in ngCount)
            {
                Console.WriteLine("  " + pair.Key + " = " + pair.Value);
            }

            // 3) 제네릭 메서드: 어떤 타입의 List든 출력
            List<int> ids = new List<int> { 10, 20, 30 };
            PrintAll(ids);
            PrintAll(cameras);
        }

        private static void PrintAll<T>(List<T> items)
        {
            Console.WriteLine("목록(" + typeof(T).Name + "): " + string.Join(", ", items));
        }
    }
}
```

## 5. 실행 결과 (검증함)
```text
카메라 수: 3
첫 번째: Cam1
  Cam1
  Cam3
Cam1 NG: 2
Cam9 사물함은 없음
  Cam1 = 2
  Cam3 = 0
목록(Int32): 10, 20, 30
목록(String): Cam1, Cam3
```

## 6. 바꿔 보기 (1개)
`Run()` 끝에 `Console.WriteLine(ngCount["Cam9"]);`를 추가해 실행한다.
→ `KeyNotFoundException`(지정한 키가 사전에 없습니다)이 난다. 설정 파일에서 읽은 카메라 이름에 오타가 있으면 **현장에서 이렇게 죽는다.** 그래서 밖에서 들어온 키는 `TryGetValue`로 꺼낸다. 확인했으면 지운다.

## 7. 어디서 다시 나오나
| Day | 코드 | 뜻 |
|---|---|---|
| Day05 | 장치 목록 `List<IDevice>` | 조립한 장치를 줄 세워 두고 역순으로 정리 |
| Day06 | 카메라 이름 → 장치 `Dictionary` | 이름으로 장치 찾기 |
| Day13 | 입력별 작업자 | 카메라마다 일꾼 하나 |
| ⚠️ Day09 | `List`/`Dictionary`는 **여러 스레드가 동시에 고치면 망가진다** | lock 또는 `ConcurrentDictionary` |

## 8. 셀프 체크
<details><summary>Q1. dict["없는키"]와 TryGetValue("없는키", out v)의 차이는?</summary>

앞은 `KeyNotFoundException`으로 넘어진다. 뒤는 false를 돌려주고 넘어지지 않는다.
</details>

<details><summary>Q2. List&lt;string&gt;의 &lt;string&gt;은 무슨 뜻인가?</summary>

이 목록에는 string만 넣을 수 있다는 뜻이다(제네릭 타입 인자). 다른 타입을 넣으면 컴파일 단계에서 막힌다.
</details>

## 9. 체크
- [ ] 1번째: 보면서 타이핑 → 실행
- [ ] 2번째: 실행 결과 비교
- [ ] 3번째: 지우고 안 보고 다시 타이핑
- [ ] 바꿔 보기
