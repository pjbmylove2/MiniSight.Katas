# 로드맵: 시니어 SW 필드엔지니어 복습 여정 (초급 → 주니어 → 시니어)

> 2026-10-29 재수립. 기존 0주차 기초 트랙과 4주 본편을 그대로 살리고, 그 위에 **시니어 단계**를 이어 붙였다.
> 한 단계의 졸업 기준을 통과해야 다음 단계로 간다. 기간은 목표일 뿐이고, **이해 없이 진도만 나가는 게 제일 나쁘다.**

평일 2시간, 토요일 4시간, 일요일 4시간(복습·밀린 것·블로그) 기준.

## 큰 그림

```text
3단계 시니어   현장을 책임진다     표준 프로토콜 · 장애 재현/덤프 · 장기 구동 · 성능 · 배포/롤백 · CI · 이식 · 현장 문서 · ADR
   ▲  (약 6주, S01~S12)        → "장비가 멈췄을 때 혼자 원인을 찾고, 보고서를 쓰고, 다른 현장에 옮길 수 있다"
2단계 주니어   프로그램을 만든다   인터페이스 · 패턴 · 스레드 · 큐 · async · 영상처리 · UI 스레드 · TCP · 로그 · 테스트
   ▲  (약 5주, Day01~28)       → "작은 검사 프로그램(MiniSight)을 설계 이유까지 설명하며 처음부터 만들 수 있다"
1단계 초급     손이 기억한다       ?. ?? => · 상속 · 람다 · 이벤트 · using · 컬렉션 · LINQ · 파일/바이트 · Task · lock
      (약 1~2주, B01~B14)      → "키워드를 보지 않고 쳐서 돌릴 수 있다"
```

| 단계 | 기간(목표) | 문서 | 졸업하면 할 수 있는 것 | 포트폴리오에 남는 것 |
|---|---|---|---|---|
| 1 초급 | 1~2주 | [Basics/](Basics/README.md) | C# 7.3 핵심 키워드를 안 보고 쓴다 | `Katas/` B01~B14, 진도 기록 |
| 2 주니어 | 약 5주 | [Days/](Days/Day01.md) | 장치 추상화 + 스레드 파이프라인 + UI + 통신을 갖춘 검사 프로그램을 만든다 | `MiniSight/` 동작 버전, 블로그 글 |
| 3 시니어 | 약 6주 | [Senior/](Senior/README.md) | 장애 재현·분석·보고, 배포·롤백, 이식, 품질 자동화를 혼자 한다 | 장애 보고서, CI 배지, v1.0 릴리스, ADR |

현장 역량 키워드(통신·진단·카메라·배포·소통)는 [FieldSkills.md](FieldSkills.md)에 있고, 각 줄이 **어느 단계 어느 칸에서 연습되는지** 적혀 있다.

---

## 1단계 · 초급: 손이 기억한다 (B01~B14)

1회차에서 Day04까지 해 보니 패턴보다 **키워드를 손으로 써 본 경험이 부족**했다. 패턴에 들어가기 전에 **키워드 하나에 Kata 하나**를 직접 친다. 진행 규칙은 [Basics/README.md](Basics/README.md).

| ✔ | 기초 날 | Kata | 키워드 |
|---|---|---|---|
| ☑ | 1 | [B01](Basics/B01-NullAndArrow.md), [B02](Basics/B02-Inheritance.md) | `?.` `??` `=>` `nameof` / `interface` `abstract` `virtual` `override` |
| ☑ | 2 | [B03](Basics/B03-ActionFunc.md), [B04](Basics/B04-Event.md) | `Action` `Func` 람다 / `event` `+=` `-=` `Invoke` |
| ☑ | 3 | [B05](Basics/B05-UsingDispose.md), [B06](Basics/B06-Collections.md) | `using` `try/finally` `throw;` / `List` `Dictionary` `<T>` |
| ☐ | 4 | [B07](Basics/B07-Linq.md), [B08](Basics/B08-StringTime.md) | LINQ / `$""` `Stopwatch` `CultureInfo` |
| ☐ | 5 | [B09](Basics/B09-SwitchPattern.md), [B10](Basics/B10-FileBytes.md) | `enum` `switch` `is` `catch when` / `File` `byte[]` 엔디언 |
| ☐ | 6 | [B11](Basics/B11-Task.md), [B12](Basics/B12-Cancellation.md) | `Task.Run` `async` `await` / `CancellationToken` |
| ☐ | 7 | [B13](Basics/B13-LockInterlocked.md), [B14](Basics/B14-TimerCamera.md) + 졸업 시험 | `lock` `Interlocked` / `Timer` + 합치기 |

**초급 졸업 기준** ([Basics/README.md §4](Basics/README.md#4-졸업-시험-기초-7일째))
- B14 가짜 카메라를 문서를 안 보고 15분 안에 쳐서 실행한다.
- 8문장(`?.Invoke`, `Func` 지연 생성, `using`과 예외, `TryGetValue`, `InvariantCulture`, 엔디언, 취소, `_count++`)을 내 말로 설명한다.

---

## 2단계 · 주니어: 프로그램을 만든다 (Day01~28, 2회차)

초급을 졸업하면 **Day01부터 2회차**로 다시 한다. 1회차에서 본 내용이라 더 빠르다. 패턴은 "이미 친 B Kata의 배치"로 설명한다.

```text
4주차  통신 · 설정 · 로그 · 종료 · 테스트 · 이식      ← 지붕: 현장에서 안 죽는 프로그램
3주차  영상처리 · AI 엔진 풀 · UI 스레드               ← 벽: 실제로 검사하고 보여주기
2주차  스레드 · 락 · 큐 · async · 파이프라인            ← 기둥: 동시에 여러 일 하기 (가장 중요)
1주차  인터페이스 · 템플릿 · 팩토리 · 어댑터 · 상태     ← 바닥: 부품을 갈아끼울 수 있는 구조
```

### 1주차: 뼈대와 디자인 패턴 기초

| ✔ | Day | 요일 | 주제 | 패턴/개념 | MiniSight 결과물 |
|---|---|---|---|---|---|
| ☐ | [01](Days/Day01.md) | 월 | 전체 지도, 단일 실행, 전역 예외 | Mutex, 예외 안전망 | 솔루션 + Program.cs |
| ☐ | [02](Days/Day02.md) | 화 | 인터페이스와 계약 | SOLID(DIP, ISP), IDisposable 소유권 | Contracts.cs, ImageFrame |
| ☐ | [03](Days/Day03.md) | 수 | 장치 수명 | Template Method, 상태 enum | DeviceBase |
| ☐ | [04](Days/Day04.md) | 목 | 장치 만들기와 맞추기 | Simple Factory, Adapter | FolderCamera, FakeCameraDriver, CameraFactory |
| ☐ | [05](Days/Day05.md) | 금 | 조립 지점 | Composition Root, Singleton(Lazy), Func 주입 | Bootstrapper, ConfigStore |
| ☐ | [06](Days/Day06.md) | 토 | 운전 총괄 | Facade, State Machine, Observer | VisionSystem + 콘솔 데모 |
| ☐ | [07](Days/Day07.md) | 일 | 1주 복습 | 백지 구조도 | 블로그 2편 |

### 2주차: 멀티스레드와 병렬처리 (핵심 주간)

| ✔ | Day | 요일 | 주제 | 패턴/개념 | MiniSight 결과물 |
|---|---|---|---|---|---|
| ☐ | [08](Days/Day08.md) | 월 | 일꾼 만들기 | Thread, ThreadPool, Task, LongRunning, 취소 가능한 sleep | HealthMonitor |
| ☐ | [09](Days/Day09.md) | 화 | 같이 쓰면 망가진다 | race, lock, Monitor, Interlocked, volatile | 통계 카운터, Dispose-once |
| ☐ | [10](Days/Day10.md) | 수 | 줄 서기 1 | Producer/Consumer, BlockingCollection, backpressure | OutputDispatcher |
| ☐ | [11](Days/Day11.md) | 목 | 줄 서기 2 | SemaphoreSlim 2개 큐, Complete/Drain | FrameQueue |
| ☐ | [12](Days/Day12.md) | 금 | 기다리는 법 | async/await, 취소, 타임아웃, TCS, ConfigureAwait | 타임아웃 Connect |
| ☐ | [13](Days/Day13.md) | 토 | 컨베이어 벨트 | Pipeline, 입력별 작업자, Stop/Drain 순서, Mat 소유권 | InspectionPipeline + Runtime |
| ☐ | [14](Days/Day14.md) | 일 | 데드락 실험 + 복습 | 교착 상태, UI 스레드 Wait | 블로그 |

### 3주차: 영상처리, Native, UI 스레드

| ✔ | Day | 요일 | 주제 | 패턴/개념 | MiniSight 결과물 |
|---|---|---|---|---|---|
| ☐ | [15](Days/Day15.md) | 월 | 사진 상자 Mat | 채널, stride, FromPixelData vs Clone, ROI | Mat 소유권 실험 |
| ☐ | [16](Days/Day16.md) | 화 | 필터 체인 | Strategy, Pipeline, JSON 다형성 | IFilter 체인 + 레시피 |
| ☐ | [17](Days/Day17.md) | 수 | 자로 재기 | 캘리퍼, 라인 피팅(PCA), 블랍 | PointGauge, LineGauge, BlobCounter |
| ☐ | [18](Days/Day18.md) | 목 | AI 엔진 다루기 | Dispose 패턴, P/Invoke, Object Pool, RWLock | FakeAiEngine + EnginePool |
| ☐ | [19](Days/Day19.md) | 금 | 화면은 한 명만 | UI 스레드, BeginInvoke, CAS 스로틀, 더블버퍼 | ImageCanvas, Invoke 헬퍼 |
| ☐ | [20](Days/Day20.md) | 토 | 화면 통합 | async 버튼, 라이브 프리뷰, 결과 그리드 | MiniSight WinForms |
| ☐ | [21](Days/Day21.md) | 일 | 3주 복습 | | 블로그 |

### 4주차: 통신, 설정, 안정성

| ✔ | Day | 요일 | 주제 | 패턴/개념 | MiniSight 결과물 |
|---|---|---|---|---|---|
| ☐ | [22](Days/Day22.md) | 월 | PLC와 대화 | TCP, 프레이밍, 엔디언, 하트비트, 재연결 | FakePlcServer + PlcClient |
| ☐ | [23](Days/Day23.md) | 화 | 파일은 조심히 | 폴링, 파일 준비 확인, 원자적 저장, Prototype(Clone) | ShareFolderSource, JsonStore |
| ☐ | [24](Days/Day24.md) | 수 | 기록과 마무리 | 비동기 로거, 역순 정지, 멱등 Dispose, 롤백 | AsyncLogger, Shutdown 체인 |
| ☐ | [25](Days/Day25.md) | 목 | 시니어의 눈 (맛보기) | 위험 분석, ADR | async Stop + 타임아웃 개선 |
| ☐ | [26](Days/Day26.md) | 금 | 증명하기 | 가짜 장치 테스트, 골든 이미지, MSTest | MiniSight.Tests |
| ☐ | [27](Days/Day27.md) | 토 | 다른 현장에 옮기기 | Open/Closed, 이식 체크리스트 | 새 카메라 + 새 보고 방식 |
| ☐ | [28](Days/Day28.md) | 일 | 주니어 졸업 | 면접 60문항, 포트폴리오 | GitHub README 정리 |

### 주차별 졸업 시험 (일요일)

- **1주차**: 백지에 `Program → Bootstrapper → VisionSystem → DeviceManager → 장치` 그림을 그리고, 각 화살표에 쓰인 패턴 이름을 적는다.
- **2주차**: 카메라 콜백에서 이미지 저장까지 **스레드가 몇 개, 큐가 몇 개, 각 큐 용량과 꽉 찼을 때 동작**을 말로 설명한다. Stop 순서를 순서대로 말한다.
- **3주차**: Mat 하나가 만들어져서 사라질 때까지 **누가 만들고 누가 Dispose하는지** 표로 그린다. UI 스레드에서 하면 안 되는 일 5가지를 말한다.
- **4주차**: "새 현장에 다른 카메라와 다른 PLC 패킷을 붙여야 한다"를 받고, **어떤 파일을 새로 만들고 어떤 파일은 건드리지 않는지** 15분 안에 설명한다.

**주니어 졸업 기준**
- 4개 주차 졸업 시험을 모두 통과한다.
- MiniSight가 가짜 장치만으로 **촬영 → 검사 → 결과 송신(TCP) → 로그 → 안전한 종료**까지 한 번에 돈다.
- [Interview.md](Cheatsheets/Interview.md) 60문항 중 45문항 이상을 꼬리 질문까지 답한다.
- [FieldSkills.md §9](FieldSkills.md#9-우선순위-단계별) "주니어" 줄이 모두 ②(해 봄) 이상이다.

---

## 3단계 · 시니어: 현장을 책임진다 (S01~S12)

주니어 단계의 MiniSight를 **현장에 실제로 세우고 운영한다는 가정**으로 키운다. 장비가 없으니 시뮬레이터·가상 포트·일부러 만든 장애로 연습한다. 단원별 목표와 결과물은 [Senior/README.md](Senior/README.md)에 있고, 상세 문서는 단원을 시작할 때 만든다.

| ✔ | 단원 | 주제 | 결과물 | FieldSkills |
|---|---|---|---|---|
| ☐ | S01 | 표준 PLC 프로토콜: Modbus TCP 직접 구현 | ModbusClient + 시뮬레이터 + 테스트 | §1 |
| ☐ | S02 | 시리얼 통신: 가상 COM 포트로 조명·바코드 흉내 | SerialLightController | §1 |
| ☐ | S03 | 장애 재현 실험실: 데드락·누수·행을 일부러 만들고 덤프로 찾기 | 장애 보고서 2편 | §4 |
| ☐ | S04 | 장기 구동(soak) 테스트: 메모리·핸들·처리 시간 추세 | 12시간 구동 리포트 | §4 §6 |
| ☐ | S05 | 성능: 프로파일러로 병목 찾고 할당 줄이기, 택트 예산표 | 전후 비교표 | §6 |
| ☐ | S06 | 카메라·조명·광학 계산: 대역폭·FOV·분해능·저장 용량 | 현장 계산기 | §3 |
| ☐ | S07 | 배포·운영: 설치·업데이트·롤백 스크립트, 장비 PC 체크리스트 | deploy 스크립트 + 체크리스트 | §5 |
| ☐ | S08 | 품질 자동화: GitHub Actions로 빌드·테스트 | CI 배지 | §7 |
| ☐ | S09 | 레거시 이식: 골든 테스트로 "결과가 같다"를 증명하며 리팩토링 | 골든 테스트 세트 | §7 |
| ☐ | S10 | 현장 문서: IO 맵, 타이밍 차트, FAT/SAT·이식 체크리스트 | `docs/` 문서 묶음 | §8 |
| ☐ | S11 | 설계 결정과 가르치기: 요구사항 → ADR → 구현 → 리뷰, 후배용 강의 1편 | ADR 3편 + 강의 문서 | §7 §8 |
| ☐ | S12 | 시니어 졸업: MiniSight v1.0 릴리스, 아키텍처 문서, 모의 면접 | GitHub Release | 전체 |

**시니어 졸업 기준**
- 처음 보는 장애(데드락·누수·통신 끊김 중 하나)를 받고 **재현 → 덤프/로그 분석 → 원인 → 조치 → 장애 보고서**를 1시간 안에 혼자 끝낸다.
- "새 현장에 다른 카메라·PLC·리포트 형식" 요구를 받고 **이식 계획(바꿀 파일, 안 바꿀 파일, 테스트, 롤백)**을 15분 안에 설명한다.
- MiniSight CI가 녹색이고, v1.0 릴리스에 설치·롤백 방법이 적혀 있다.
- [FieldSkills.md §9](FieldSkills.md#9-우선순위-단계별) "시니어" 줄이 모두 ③(남에게 설명 가능)이다.
