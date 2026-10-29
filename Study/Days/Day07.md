# Day 07 (일, 4h) · 1주차 복습, 백지 구조도, 블로그

## 1. 오늘 목표

- **완료 기준**
  - 밀린 Day의 최소분량을 끝냈다.
  - **백지 시험**을 통과했다: 아무것도 안 보고 1주차 구조도를 그리고 화살표마다 패턴 이름을 적는다.
  - 셀프 체크 Day01~06 중 틀린 문항을 Claude와 말로 다시 풀었다.
  - 블로그 2편 초안을 썼다.
- **최소분량**: 2(밀린 것), 3(백지 시험)
- **확장분량**: 5 리팩토링 과제, 블로그 2편째

## 2. 시간표

| 시간 | 할 일 |
|---|---|
| 0:00~1:30 | PROGRESS.md의 "일요일 버퍼"에 쌓인 확장분량 처리 |
| 1:30~2:10 | 백지 시험 (3번) |
| 2:10~2:50 | Claude 구술 면접 (4번) |
| 2:50~4:00 | 블로그 (Templates/BlogPost.md) |

## 3. 백지 시험

종이(또는 그림판)에 **아무것도 보지 않고** 그린다. 20분 제한.

1. `Program.Main`부터 `Application.Run`까지 객체가 만들어지는 순서
2. 각 화살표 옆에 패턴 이름: Composition Root / Factory / Adapter / Template Method / Facade / Observer / Singleton
3. `DeviceState` 상태 그림 (전이 조건 포함)
4. Start 실패 시 롤백 순서, Stop 순서, Dispose 순서
5. ImageFrame의 소유권: 누가 만들고, 이벤트로 받은 사람과 `WaitFrameAsync`로 받은 사람의 책임 차이

다 그렸으면 Claude에게:
```text
1주차 백지 시험 그림을 말로 설명할게. OmniSight 실제 코드와 비교해서 틀린 부분이나
빠진 부분을 짚어줘. 정답을 바로 말하지 말고 "어디가 이상한지"만 먼저 힌트로 줘.
```

**합격 기준**: 패턴 7개 중 6개 이상 정확한 위치, 상태 그림 전이 오류 0개, 소유권 설명에서 "이벤트 핸들러 안에서만 유효"를 말했다.

## 4. 구술 면접 연습

Claude에게:
```text
면접관 역할 해줘. 주제는 "디자인 패턴을 실제 장비 프로그램에 어떻게 썼나".
내 MiniSight 기준으로 질문 5개를 하나씩 하고, 내 답에 꼬리 질문 2개씩 해줘.
답이 부족하면 마지막에 OmniSight의 어느 코드를 다시 보면 되는지 알려줘.
```

자주 나오는 꼬리 질문 예시:
- "Factory를 switch로 만들었는데 벤더가 30개면?"
- "싱글톤을 썼다는데 테스트는 어떻게 했나?"
- "상태 enum을 쓰면 State 패턴(클래스)과 뭐가 다른가? 언제 클래스로 바꾸나?"
- "이벤트 구독자가 UI라면 어떤 스레드에서 불리나?" (→ 2주차 복선)

## 5. 리팩토링 과제 (확장)

OmniSight에서 본 약점 하나를 MiniSight에서 **다르게** 해 본다. 하나만 고른다.

| 과제 | 내용 |
|---|---|
| A. 팩토리 등록 방식 | `CameraFactory`의 `switch`를 `Dictionary<CameraKind, Func<CameraSettings, IImageSource>>` 등록 방식으로 바꾸고 장단점 정리 |
| B. 상태 읽기 안전 | `DeviceBase.State`를 `volatile` 백킹 필드로 바꾸거나 lock으로 읽게 하고, 왜 필요한지 주석으로 |
| C. 방송을 lock 밖으로 | `VisionSystem.ChangeState`에서 lock 안에서는 상태와 구독자 스냅샷만, 방송은 lock 밖. 순서 역전 문제를 어떻게 막을지 고민 |

## 6. 1주차 용어 카드 (말로 30초씩 설명)

| 용어 | 30초 설명 키워드 |
|---|---|
| DIP | 고수준(파이프라인)이 저수준(카메라)에 의존하지 않고 둘 다 추상(인터페이스)에 의존 |
| ISP | 쓰지 않는 기능에 의존하지 않게 인터페이스를 쪼갬 (Preview 분리) |
| Template Method | 순서 고정(non-virtual) + 빈칸(abstract/virtual hook) |
| Simple Factory | 조건 → 구체 객체, new를 한 곳에 |
| Adapter | SDK API를 우리 계약 모양으로. 로직 넣지 않기 |
| Composition Root | 객체 그래프 조립은 시작 지점 한 곳 |
| Singleton(Lazy) | 프로세스당 하나, 스레드 안전 지연 생성, 숨은 의존성 주의 |
| Facade | 여러 하위 시스템을 하나의 단순 API로 |
| Observer | 이벤트 방송, 구독자 격리(try/catch), 해제 필수 |
| 보상 롤백 | 실패 시 이미 한 일을 역순으로 되돌림 |

## 7. VS2022 복습 (10분)

- F9, F5, F10, F11, Shift+F11, Ctrl+Alt+C(호출 스택), Ctrl+Alt+I(직접 실행), Ctrl+Alt+E(예외 설정), Ctrl+Alt+B(중단점 창)
- 조건부 중단점과 Tracepoint를 하나씩 다시 만들어 본다.

## 8. 셀프 체크 (1주차 종합)

1. OmniSight에서 "부팅 실패해도 설정 화면은 열린다"는 어떻게 구현되어 있나?
<details><summary>답</summary>Bootstrapper가 단계별 예외를 이슈 목록에 쌓고, 장치 생성이 실패하면 빈 DeviceManager(Null Object)로 VisionSystem을 만든다. VisionSystem은 이슈가 있으면 StartInternal에서 시작을 거부한다.</details>

2. 1주차 패턴 중 "상속"을 쓰는 것과 "합성"을 쓰는 것을 나눠 보라.
<details><summary>답</summary>상속: Template Method(DeviceBase, CallbackCameraAdapterBase, AiEngineBase). 합성: Adapter(드라이버를 필드로), Facade(DeviceManager·Inspection을 필드로), Strategy/DI(파이프라인이 인터페이스를 필드로), Factory는 생성 책임 분리.</details>

3. 소유권 이전을 코드에서 표시하는 관용구 두 가지는?
<details><summary>답</summary>① 넘긴 뒤 로컬 변수를 null로 만들어 finally/catch의 Dispose 대상에서 빼기(`devices = null`, `frame = null`). ② `Interlocked.Exchange(ref _owner, null)`로 꺼내서 한 번만 Dispose.</details>

4. 시작 실패 롤백과 정지 실패 처리는 왜 다르게(예외를 삼키느냐 던지느냐) 했나?
<details><summary>답</summary>시작 롤백 중의 정지 실패는 원래 시작 실패 예외를 가리지 않도록 기록만 하고 삼킨다(throwOnFailure:false). 정상 정지에서의 실패는 호출자가 Faulted로 판단해야 하므로 AggregateException으로 던진다.</details>

5. 다음 주(멀티스레드)를 위해 1주차 코드에서 "다른 스레드가 접근하는 곳"을 3군데 찾아라.
<details><summary>답</summary>① FakeCameraDriver 타이머 콜백(작업자 스레드) → 어댑터 OnFrame. ② ImageFrame.Dispose(정지 스레드 vs 작업자). ③ VisionSystem 이벤트 → UI 구독자(UI 스레드가 아닌 곳에서 호출될 수 있음). 그 밖에 Bootstrapper.InitializeAsync(Task.Run).</details>

## 9. 블로그 포인트

- 이번 주 2편 추천: "Adapter + Factory" 1편, "상태 머신 + Observer + 롤백" 1편.
- 백지 시험 그림을 깔끔하게 다시 그려 **일반화된 구조도**로 올리면 좋다 (회사 클래스명 대신 MiniSight 이름).
