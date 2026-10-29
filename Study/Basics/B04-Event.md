# B04 · 이벤트 (`event` `EventHandler<T>` `+=` `-=` `Invoke`) · 25분

## 1. 오늘 목표
"무슨 일이 생기면 **등록해 둔 사람들에게 알리는**" 코드를 직접 쓴다. Day04의 `FrameReady += OnFrame`을 읽을 수 있다.

## 2. 🧒 비유: 초인종
- 초인종에 **줄**이 달려 있다. 엄마와 강아지가 줄을 잡는다(`+=`).
- 누가 버튼을 누르면(`Invoke`) 줄을 잡은 **모두에게, 잡은 순서대로** 소리가 간다.
- 강아지가 줄을 놓으면(`-=`) 강아지에게는 더 이상 안 들린다.
- 아무도 줄을 안 잡았는데 소리를 보내려 하면 넘어진다 → 그래서 **"잡은 사람 있나?"를 먼저 확인**한다.

## 3. 키워드 표

| 키워드 | 역할 | 사용법 | 기억할 점 |
|---|---|---|---|
| `event` | 바깥에서는 `+=`/`-=`만 할 수 있는 **알림 줄** | `public event EventHandler<string> Rang;` | 바깥에서 `Rang(...)` 직접 호출이나 `Rang = null` 불가 |
| `EventHandler<T>` | 이벤트 메서드의 **정해진 모양**: `(object sender, T 값)` | `EventHandler<string>` | sender = 누가 울렸나, 값 = 무엇을 들고 왔나 |
| `+=` | 메서드를 줄에 **건다** (구독) | `bell.Rang += Mom;` | 두 번 걸면 두 번 불린다 |
| `-=` | 메서드를 줄에서 **뗀다** (구독 해제) | `bell.Rang -= Dog;` | 안 떼면 버린 객체도 계속 불린다 (Day04) |
| `Invoke` | 줄에 건 메서드를 **차례대로 실행** | `handler.Invoke(this, who);` | `handler(this, who)`와 같다. **부른 스레드에서** 실행됨 |
| `?.Invoke` | 줄이 비어 있으면(null) 그냥 넘어간다 | `Rang?.Invoke(this, who);` | 아래 긴 코드와 같다 |

**`Rang?.Invoke(this, who)`를 길게 풀면** (오늘은 이 긴 버전으로 친다)
```csharp
EventHandler<string> handler = Rang;   // ① 줄을 먼저 손에 쥔다
if (handler != null)                   // ② 잡은 사람이 있나?
{
    handler.Invoke(this, who);         // ③ 모두 부른다
}
```
①에서 변수로 옮기는 이유: ②와 ③ 사이에 다른 스레드가 `-=`로 마지막 사람을 떼면 `Rang`이 그 순간 null이 될 수 있다. 손에 쥔 `handler`는 그대로라 안전하다. `?.`는 이 처리를 자동으로 해 준다.

## 4. 코드: `Basics/KataB04.cs`

```csharp
using System;

namespace Katas.Basics
{
    public class DoorBell
    {
        // 초인종 줄. string(누가 왔는지)을 들고 울린다
        public event EventHandler<string> Rang;

        public void Push(string who)
        {
            Console.WriteLine("딩동! (" + who + ")");
            EventHandler<string> handler = Rang;
            if (handler != null)
            {
                handler.Invoke(this, who);
            }
        }
    }

    public static class KataB04
    {
        public static void Run()
        {
            DoorBell bell = new DoorBell();

            bell.Rang += Mom;       // 엄마가 줄을 잡음
            bell.Rang += Dog;       // 강아지도 잡음
            bell.Push("택배");

            bell.Rang -= Dog;       // 강아지가 놓음
            bell.Push("친구");

            bell.Rang -= Mom;       // 엄마도 놓음 → 아무도 없음
            bell.Push("아무도 없음");
        }

        // EventHandler<string> 모양: (보낸 쪽, 전달값)
        private static void Mom(object sender, string who)
        {
            Console.WriteLine("  엄마: " + who + " 왔네, 나가볼게");
        }

        private static void Dog(object sender, string who)
        {
            Console.WriteLine("  강아지: 멍멍!");
        }
    }
}
```

## 5. 실행 결과 (검증함)
```text
딩동! (택배)
  엄마: 택배 왔네, 나가볼게
  강아지: 멍멍!
딩동! (친구)
  엄마: 친구 왔네, 나가볼게
딩동! (아무도 없음)
```
👀 마지막에는 줄이 비어서 `handler`가 null이고, `if`가 막아 줘서 아무 일도 없다.

## 6. 바꿔 보기 (1개)
`KataB04.Run()` 맨 끝에 `bell.Rang(bell, "몰래");`를 추가하고 빌드한다.
```text
CS0070: 'DoorBell.Rang' 이벤트는 += 또는 -=의 왼쪽에만 사용할 수 있습니다.
```
`event` 키워드 덕분에 **바깥에서는 초인종을 대신 누를 수 없다.** 누르는 건 DoorBell 자신만 한다. 확인했으면 지운다.

## 7. 어디서 다시 나오나
| Day | 코드 | 비유 |
|---|---|---|
| Day02 | `event EventHandler<ImageFrame> FrameCaptured` | "사진 찍혔다" 초인종 |
| Day04 | `old.FrameReady -= OnFrame; old.Dispose();` | **이사 가기 전에 줄부터 놓는다.** 안 놓으면 철거한 집에 편지가 온다 |
| Day04 | `FrameCaptured?.Invoke(this, frame)` | 미리보기 화면에 알림 |
| Day06 | Observer 패턴 | 이 초인종에 붙은 **이름** |

## 8. 셀프 체크
<details><summary>Q1. 줄에 아무도 없을 때 handler.Invoke를 그냥 부르면?</summary>

`handler`가 null이라 `NullReferenceException`이 난다. 그래서 `if (handler != null)` 또는 `?.Invoke`를 쓴다.
</details>

<details><summary>Q2. 객체를 버리기(Dispose) 전에 왜 -=를 먼저 하나?</summary>

줄에 걸려 있으면 버린 뒤에도 알림이 와서, 이미 버린 객체의 메서드가 실행된다. Day04처럼 다른 스레드에서 알림이 오면 언제 올지 몰라 더 위험하다.
</details>

## 9. 체크
- [ ] 1번째: 보면서 타이핑 → 실행
- [ ] 2번째: 실행 결과 비교
- [ ] 3번째: 지우고 안 보고 다시 타이핑 (긴 버전 → `?.Invoke` 짧은 버전으로도 한 번)
- [ ] 바꿔 보기
