# 면접 질문 60 (주차별)

> 사용법: Claude에게 "Interview.md에서 N주차 문제 10개 섞어서 면접관처럼 내줘, 꼬리 질문 포함"이라고 한다.
> 답할 때는 "OmniSight" 대신 **"FA 영상 검사 프로그램"**, 회사 고유 내용 대신 **MiniSight 경험**으로 말한다.
> 각 질문 옆 (Day) = 복습할 문서.

## 1주차: 설계와 디자인 패턴

1. SOLID 중 실무에서 가장 체감한 원칙과 예는? (02)
2. 인터페이스를 "언제" 만드나? 구현이 하나뿐이어도 만드나? (02)
3. Template Method와 Strategy 차이, 각각 언제? (03, 16)
4. 템플릿 메서드를 virtual로 두면 안 되는 이유? (03)
5. Simple Factory / Factory Method / Abstract Factory 차이. (04)
6. Adapter에 로직을 넣으면 안 되는 이유? (04)
7. 생성자에 객체 대신 `Func<T>`를 받는 경우는? (04, 05)
8. Composition Root란? DI 컨테이너를 안 쓴 이유를 설명해 보라. (05)
9. Singleton의 문제점과 완화 방법. (05)
10. `Lazy<T>`의 스레드 안전성. (05)
11. Facade와 Adapter의 차이. (06)
12. 상태를 enum 여러 개로 나눈 이유, State 패턴(클래스)과 비교. (06)
13. 이벤트 구독자 하나가 예외를 던지면? 어떻게 막나? (06)
14. 시작 실패 롤백(보상)을 어떻게 구현했나? (06)
15. 이벤트 구독 해제를 안 하면? (06, 20)

## 2주차: 멀티스레드와 병렬처리

16. `Thread`, `ThreadPool`, `Task.Run`, `LongRunning` 비교. (08)
17. 스레드풀 고갈이란, 어떻게 확인하고 피하나? (08)
18. `LongRunning`에 async 람다를 넣으면? (08)
19. `Thread.Sleep` 대신 무엇을 쓰고 왜? (08)
20. race condition을 실제로 재현해 본 방법. (09)
21. `lock`, `Interlocked`, `volatile` 차이. (09)
22. CAS 루프를 설명하고 써 본 예. (09)
23. 여러 필드 상태를 lock 없이 공유하는 방법. (09)
24. Producer/Consumer와 유한 큐가 필요한 이유. (10)
25. 큐가 가득 찼을 때 정책 3가지와 선택 기준. (10)
26. `BlockingCollection`을 async로 소비하고 싶다면? (11)
27. SemaphoreSlim 두 개로 만든 큐의 동작. (11)
28. await는 스레드를 멈추나? (12)
29. UI에서 `.Result`를 부르면 왜 멈추나? (12, 14)
30. `ConfigureAwait(false)`는 언제, 왜? (12)
31. `async void`의 위험. (12)
32. 타임아웃 구현과 진 Task 처리. (12)
33. `TaskCompletionSource`와 `RunContinuationsAsynchronously`. (12)
34. 카메라별 파이프라인 1개로 한 이유와 한계. (13)
35. 정지 순서와 Drain vs Cancel. (13)
36. 데드락 4조건과 실제로 깨 본 방법. (14)
37. 병렬 스택으로 멈춤 원인을 찾는 순서. (14)

## 3주차: 영상처리, Native, UI

38. `Mat`을 Dispose해야 하는 이유, GC가 늦게 치우는 이유. (15)
39. stride란, 무시하면 생기는 증상. (15)
40. 래퍼와 Clone, ROI의 메모리 관계. (15)
41. 영상 메모리 사용량을 어떻게 추정하나? (15)
42. 전처리 순서가 중요한 예. (16)
43. 적응형 이진화를 쓰는 상황. (16)
44. JSON 다형 역직렬화의 보안 위험과 대책. (16)
45. 캘리퍼 원리와 한계, 서브픽셀은? (17)
46. 직선 피팅에 최소제곱 대신 PCA를 쓰는 이유. (17)
47. Dispose(bool) 패턴과 finalizer에서 하면 안 되는 일. (18)
48. SafeHandle을 쓰는 이유. (18, 21)
49. Object Pool이 필요한 경우와 크기 결정. (18)
50. 모델 교체 중 사용 중인 핸들을 해제하지 않게 하는 방법. (18)
51. `BadImageFormatException`, `DllNotFoundException` 원인. (18)
52. WinForms 크로스 스레드 규칙과 `Invoke` vs `BeginInvoke`. (19)
53. 카메라 30fps를 화면 10fps로 lock 없이 제한하는 방법. (19)
54. GDI 개체 누수를 어떻게 찾나? (19)

## 4주차: 통신, 안정성, 시니어

55. TCP 프레이밍 방식과 재동기화. (22)
56. 하트비트·응답 타임아웃·재연결·세대 번호의 역할. (22)
57. 정전에도 설정이 안 깨지게 저장하는 방법. (23)
58. 로그는 버리고 결과는 안 버리는 이유, 비동기 로거 설계. (24)
59. 멈춘 현장 프로그램을 어떻게 분석하나? (덤프, 로그) (24)
60. 레거시 장비 코드를 안전하게 개선하는 절차와, 새 현장 이식 시 코어를 안 고치는 방법. (25, 26, 27)

## 답변 체크 키워드 (자기 채점용)

| # | 꼭 들어가야 할 단어 |
|---|---|
| 18 | 첫 await, 스레드 반납, Unwrap |
| 21 | 상호 배제 / 원자적 연산 / 가시성, 복합 연산 |
| 25 | 생산자 대기(손실 0), 새 것 버림, 옛 것 버림, 카운트·알람, 현장 계약 |
| 29 | SynchronizationContext, 계속(continuation), UI 스레드 블로킹 |
| 35 | CompleteAcquisition, null까지 처리, _acceptResults, 출력 drain, 역순 |
| 38 | 네이티브 메모리, GC는 관리 힙만 압박으로 봄, finalizer 지연 |
| 47 | disposing true/false, SuppressFinalize, 네이티브만, 예외 금지 |
| 50 | 읽기 잠금 추론, 쓰기 잠금 교체, 밖에서 조립, copy-on-write |
| 53 | Stopwatch 타임스탬프, CompareExchange, 바쁨 플래그, 검사 경로 분리 |
| 57 | .tmp, WriteThrough, Flush(true), File.Replace, .bak, 같은 볼륨 |
| 60 | 샘플 확보, 골든 테스트, 최소 변경, 인터페이스 구현 + 현장 조립, diff 증명 |
