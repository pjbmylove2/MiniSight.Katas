# Part 0 · L0 기초 손코딩 (B01~B14 + 보스전)

> 목표: C# 7.3의 핵심 키워드를 **보지 않고 쳐서 돌린다.** 패턴·구조·장비 이야기는 아직 없다. 키워드 하나에 Kata 하나, 손이 먼저 기억하게 만든다.
> 장 하나 = 1일차(약 2시간). 몰아서 하루에 여러 장을 해도 되지만 장마다 통과 기준은 그대로다.

## 왜 기초부터 다시 하나

현장에서 급하게 고칠 때 머리보다 손이 먼저 움직여야 한다. `?.` 하나를 빼먹어서 정지 버튼이 NullReferenceException으로 죽고, `_count++` 하나 때문에 카운터가 틀린다. 이런 사고는 "알고는 있는데 손에 안 붙은" 상태에서 난다. Part 0은 그 키워드들을 손에 붙이는 곳이다.

## 장 목록

| 장 | 일차 | 주제 | 키워드 | 장 끝에 할 수 있는 것 | 지옥 레벨 |
|---|---|---|---|---|---|
| [B01](D001-B01-null과-화살표.md) | D001 | null과 화살표 | `null` `?.` `??` `=>` `nameof` | null 때문에 죽지 않는 코드, 한 줄 속성·메서드 | ★1 |
| [B02](D002-B02-상속과-인터페이스.md) | D002 | 상속과 인터페이스 | `class` `interface` `abstract` `virtual` `override` `sealed` | 카메라 종류를 바꿔 끼우는 뼈대 | ★2 |
| B03 | | 대리자와 람다 | `Action` `Func` `=>` 람다, 캡처 | "나중에 할 일"을 변수에 담기 | ★2 |
| B04 | | 이벤트 | `event` `+=` `-=` `?.Invoke` | 카메라가 사진을 찍으면 알려 주기 | ★2 |
| B05 | | 예외와 정리 | `try/catch/finally` `throw;` `using` `IDisposable` | 예외가 나도 자원을 반납하기 | ★3 |
| B06 | | 컬렉션과 제네릭 | `List<T>` `Dictionary<K,V>` `Queue<T>` `TryGetValue` | 결과 목록·설정 사전 다루기 | ★2 |
| B07 | | LINQ | `Where` `Select` `OrderBy` `First/FirstOrDefault` `Average` | 검사 결과 통계를 한 줄로 | ★2 |
| B08 | | 문자열·시간·문화권 | `$""` `string.Format` `Stopwatch` `DateTime` `CultureInfo.InvariantCulture` | 로그 시각, 처리 시간, 소수점 함정 | ★3 |
| B09 | | enum과 분기 | `enum` `switch` `is` `catch when` `const` `readonly` | 판정 결과·상태를 안전하게 나누기 | ★2 |
| B10 | | 파일과 바이트 | `File` `Path.Combine` `byte[]` `BitConverter` `Buffer.BlockCopy` 엔디언 | 파일 읽고 쓰기, 바이트로 숫자 만들기 | ★3 |
| B11 | | Task 맛보기 | `Task.Run` `async` `await` `Task<T>` | 기다리는 동안 다른 일 하기 | ★3 |
| B12 | | 취소 | `CancellationTokenSource` `CancellationToken` `CancelAfter` | 리모컨과 수신기, 협조적 취소 | ★3 |
| B13 | | 같이 쓰기 | `lock` `Interlocked` `static readonly object` | 여러 일꾼이 같은 숫자를 바꿀 때 | ★4 |
| B14 | | 타이머와 가짜 카메라 | `System.Threading.Timer` `Change` `Dispose` | 주기적으로 찍는 가짜 카메라 | ★4 |
| **보스전** | | L0 졸업 시험 | B01~B14 전부 | 아래 기준 | ★5 |

일차 번호는 실제로 진행한 순서대로 [PROGRESS](../../PROGRESS.md)에서 매긴다. 장 문서 파일 이름은 `D일차-장-제목.md`다.

## 장마다 공통으로 하는 것

- 조각 4~6개: Kata → 키워드 표 → 실험 → 확인된 출력 → 질문
- 3번 타이핑: 보고 치기 → 글자 단위 비교 → 핵심 기능 빈칸 채우기
- 셀프 체크와 면접 질문, 블로그 요약
- Kata 파일 위치: 포트폴리오 `Katas/L0/D일차.cs`, `ChunkN()` 메서드를 `Run()`이 차례로 부른다

## L0 졸업 보스전

| 부 | 내용 | 통과 기준 |
|---|---|---|
| 1부 실기 | 가짜 카메라: 타이머로 주기 촬영, 프레임 번호는 `Interlocked`, 사진 이벤트, `Start/Stop/Dispose`(null 안전), 취소 | 문서 없이 **15분** 안에 쳐서 한 번에 실행 |
| 2부 구술 | 8문장: `?.Invoke`, `Func` 지연 생성, `using`과 예외, `TryGetValue`, `InvariantCulture`, 엔디언, 취소, `_count++` | 8문장 모두 "무엇을 → 그래서"로 |
| 3부 빈칸 | B01~B14 키워드 20개를 코드 빈칸에 채우기 | 18개 이상 |

떨어지면 틀린 장만 다시 하고 이틀 뒤 재도전한다.
