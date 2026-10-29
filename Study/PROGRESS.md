# 진도 기록

> 집에서 "오늘 Day NN 최소분량 끝" 이라고 말하면 Claude가 이 표를 갱신한다. 직접 고쳐도 된다.

## 단계 현황

| 단계 | 범위 | 상태 | 졸업일 |
|---|---|---|---|
| 1 초급 | B01~B14 + 졸업 시험 | 진행 중 (B01·B02 완료, 다음 B03) | |
| 2 주니어 | Day01~28 (2회차) + 주차별 졸업 시험 | 대기 | |
| 3 시니어 | S01~S12 | 대기 | |

## 1단계 · 초급 (B01~B14, 2026-10-28 시작)

| 날짜 | Kata | 3번 타이핑 | 바꿔 보기 | 막힌 곳 / 질문 |
|---|---|---|---|---|
| 2026-10-28 | B01 | ☑ | ☑ | 개념 3조각 Q&A 통과. `set` 없는 속성 대입을 실행 중 예외로 착각 → 컴파일 에러(CS0200)로 정정 후 재확인 통과. 셀프 체크 통과(Q2는 "이름 바꾸기 때 같이 바뀜"을 바로 못 떠올림) |
| 2026-10-29 | B02 | ☑ | ☑ | 개념 3조각 Q&A 통과(용어 정정: 인터페이스는 "상속"이 아니라 "구현"). 셀프 체크 통과(Q2 "virtual 없음 → 자식이 순서를 못 바꿈" 보충) |
| | B03 | ☐ | ☐ | |
| | B04 | ☐ | ☐ | |
| | B05 | ☐ | ☐ | |
| | B06 | ☐ | ☐ | |
| | B07 | ☐ | ☐ | |
| | B08 | ☐ | ☐ | |
| | B09 | ☐ | ☐ | |
| | B10 | ☐ | ☐ | |
| | B11 | ☐ | ☐ | |
| | B12 | ☐ | ☐ | |
| | B13 | ☐ | ☐ | |
| | B14 | ☐ | ☐ | |
| | 졸업 시험 | ☐ | | |

## 2단계 · 주니어 (초급 졸업 후 Day01부터, 2회차)

| 날짜 | Day | 최소분량 | 확장분량 | 걸린 시간 | 막힌 곳 / 질문 | 블로그 |
|---|---|---|---|---|---|---|

## 3단계 · 시니어 (주니어 졸업 후)

| 날짜 | 단원 | 최소분량 | 결과물 | 막힌 곳 / 질문 |
|---|---|---|---|---|
| | S01 Modbus TCP | ☐ | | |
| | S02 시리얼 | ☐ | | |
| | S03 장애 재현 실험실 | ☐ | | |
| | S04 장기 구동 테스트 | ☐ | | |
| | S05 성능·택트 | ☐ | | |
| | S06 카메라·조명 계산 | ☐ | | |
| | S07 배포·운영 | ☐ | | |
| | S08 CI | ☐ | | |
| | S09 레거시 이식·골든 테스트 | ☐ | | |
| | S10 현장 문서 | ☐ | | |
| | S11 ADR·가르치기 | ☐ | | |
| | S12 시니어 졸업 | ☐ | | |

## 1회차 기록 (Day04에서 중단 → 기초 트랙으로)

> 중단 이유: 패턴 구조가 한꺼번에 커졌고, `Task`, `CancellationToken`, `event`/`EventHandler`, `Interlocked`, `?.`, `Invoke` 같은 키워드를 직접 타이핑해 본 적이 없었다 (복사·붙여넣기만 함). Day04 확장분량(FolderCamera, 디버깅 4·5단계)은 2회차 Day04에서 한다.

| 날짜 | Day | 최소분량 | 확장분량 | 걸린 시간 | 막힌 곳 / 질문 | 블로그 |
|---|---|---|---|---|---|---|
| 2026-10-28 | 04 | ☑ | ☐ | | 돼지코 Kata(역순 정리는 UsaAdapter에 IDisposable 추가해서 관찰), MiniSight ICameraDriver·FakeCameraDriver·DriverCameraAdapter·CameraFactory 4단계로 나눠 작성, 호출 스택으로 Func가 Initialize 때 실행됨 확인. 막힌 곳: Kata 파일 위치(교재 §5 = Day04.md, Katas 파일은 집에서 새로 만듦), 셀프 체크 1~3 70% 이해 | ☐ |
| 2026-10-28 | 03 | ☑ | ☑ | | 라면 Kata, MiniSight DeviceBase/TestDevice(FailOnStart) 작성, 디버거로 Faulted 전이 확인. 코드 리뷰 통과(메서드 순서·불필요 using·GC.SuppressFinalize 누락). 막힌 곳: SuppressFinalize를 "강제 해제"로 오해(→ 소멸자 생략 요청), Faulted 복구는 Initialize부터, CancellationToken은 직접 확인해야 동작. 확장: Cook() check-then-act lock, 조건부 중단점/Tracepoint. 참고: CallbackCameraAdapterBase는 Template + Strategy(드라이버 팩토리 주입), 교재 §3 표현 보정 | ☐ |
| 2026-10-27 | 02 | ☑ | ☐ | | 이벤트로 받은 프레임의 수명(핸들러 안에서만 유효 → 나중에 쓰면 Clone). Katas 프로젝트 x64 설정(구성 관리자에서 "새 솔루션 플랫폼 만들기" 해제). 코드 리뷰: IDevice에 IDisposable 누락, 네임스페이스 Device→Devices 수정. ImageFrame 실행 확인 완료 | ☐ |
| 2026-10-27 | 01 | ☑ | ☐ | | 전역 예외 처리기 3종을 스레드 기준으로 구분하는 것(Task↔Thread 혼동). Debug 빌드에서 UnobservedTaskException 미발생 → 별도 메서드(NoInlining)로 해결 | ☐ |

## 일요일 버퍼로 넘긴 것

- Day01 확장: AbandonedMutexException 직접 재현 실험
- Day01 블로그: "장비 프로그램은 왜 두 번 실행되면 안 될까: Global Mutex"
- Day02 블로그: "영상 검사기가 카메라 종류를 몰라도 되는 이유 (DIP와 ISP)"
- Day03 블로그: "장비 클래스의 수명 관리를 Template Method로 고정한 이유" (상태 그림 포함)

## 다시 물어볼 질문

- `nameof(x)`를 문자열 `"x"` 대신 쓰는 이유: F2 이름 바꾸기 때 같이 바뀌고, 오타가 나면 컴파일러가 잡아 줌 (Day04 생성자 인자 검사 때 복습)
- 전역 예외 처리기 3종 재확인 (Day08에서 Thread 예외, Day12에서 Task 예외 다룰 때 복습)
- 취소(OperationCanceledException)도 Faulted로 표시해도 되나? (Day12 Task·취소 때)
- OmniSight `DeviceBase.Initialize`는 Initializing 상태에서 두 번째 호출이 OnInitialize를 또 부를 수 있음 (Day08/Day16 동시성·설계 때 확인)
- Day04 셀프 체크 1~3 (Func 디스펜서, 받을 사람 없으면 던진 쪽이 Dispose, `-=` 먼저) 70% 이해 → Day05 Bootstrapper, Day08 스레드, Day11 FrameQueue 때 다시 확인
- (예) LongRunning에 async 람다를 넣으면 왜 전용 스레드가 낭비되나?

## 졸업 시험 결과

| 시험 | 날짜 | 통과 | 메모 |
|---|---|---|---|
| 초급 졸업 | | ☐ | |
| 주니어 1주차 | | ☐ | |
| 주니어 2주차 | | ☐ | |
| 주니어 3주차 | | ☐ | |
| 주니어 4주차 | | ☐ | |
| 주니어 졸업 | | ☐ | |
| 시니어 졸업 | | ☐ | |
