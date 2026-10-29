# 4주 로드맵

평일 2시간(개념 + Kata + MiniSight 한 조각), 토요일 4시간(MiniSight 통합), 일요일 4시간(복습·밀린 것·블로그).

## 큰 그림: 4주 동안 쌓는 집

```text
4주차  통신 · 설정 · 로그 · 종료 · 테스트 · 이식      ← 지붕: 현장에서 안 죽는 프로그램
3주차  영상처리 · AI 엔진 풀 · UI 스레드               ← 벽: 실제로 검사하고 보여주기
2주차  스레드 · 락 · 큐 · async · 파이프라인            ← 기둥: 동시에 여러 일 하기 (가장 중요)
1주차  인터페이스 · 템플릿 · 팩토리 · 어댑터 · 상태     ← 바닥: 부품을 갈아끼울 수 있는 구조
```

## 0주차: 기초 체력 (2026-10-28 추가)

1회차에서 Day04까지 해 보니 키워드를 손으로 써 본 경험이 부족했다. 패턴에 들어가기 전에 **키워드 하나에 Kata 하나**를 직접 친다. 자세한 내용은 [Basics/README.md](Basics/README.md)에 있다.

| ✔ | 기초 날 | Kata | 키워드 |
|---|---|---|---|
| ☐ | 1 | [B01](Basics/B01-NullAndArrow.md), [B02](Basics/B02-Inheritance.md) | `?.` `??` `=>` `nameof` / `interface` `abstract` `virtual` `override` |
| ☐ | 2 | [B03](Basics/B03-ActionFunc.md), [B04](Basics/B04-Event.md) | `Action` `Func` 람다 / `event` `+=` `-=` `Invoke` |
| ☐ | 3 | [B05](Basics/B05-UsingDispose.md), [B06](Basics/B06-Collections.md) | `using` `try/finally` `throw;` / `List` `Dictionary` `<T>` |
| ☐ | 4 | [B07](Basics/B07-Linq.md), [B08](Basics/B08-StringTime.md) | LINQ / `$""` `Stopwatch` `CultureInfo` |
| ☐ | 5 | [B09](Basics/B09-SwitchPattern.md), [B10](Basics/B10-FileBytes.md) | `enum` `switch` `is` `catch when` / `File` `byte[]` 엔디언 |
| ☐ | 6 | [B11](Basics/B11-Task.md), [B12](Basics/B12-Cancellation.md) | `Task.Run` `async` `await` / `CancellationToken` |
| ☐ | 7 | [B13](Basics/B13-LockInterlocked.md), [B14](Basics/B14-TimerCamera.md) + 졸업 시험 | `lock` `Interlocked` / `Timer` + 합치기 |

현장 SW 필드엔지니어 역량 지도는 [FieldSkills.md](FieldSkills.md)에 있다 (4주 본편과 병행, 4주 이후 이어서).

졸업하면 **Day01부터 2회차**로 다시 한다. 2회차는 이미 본 내용이라 1회차보다 빠르다.

## 1주차: 뼈대와 디자인 패턴 기초

| ✔ | Day | 요일 | 주제 | 패턴/개념 | MiniSight 결과물 |
|---|---|---|---|---|---|
| ☐ | [01](Days/Day01.md) | 월 | 전체 지도, 단일 실행, 전역 예외 | Mutex, 예외 안전망 | 솔루션 + Program.cs |
| ☐ | [02](Days/Day02.md) | 화 | 인터페이스와 계약 | SOLID(DIP, ISP), IDisposable 소유권 | Contracts.cs, ImageFrame |
| ☐ | [03](Days/Day03.md) | 수 | 장치 수명 | Template Method, 상태 enum | DeviceBase |
| ☐ | [04](Days/Day04.md) | 목 | 장치 만들기와 맞추기 | Simple Factory, Adapter | FolderCamera, FakeCameraDriver, CameraFactory |
| ☐ | [05](Days/Day05.md) | 금 | 조립 지점 | Composition Root, Singleton(Lazy), Func 주입 | Bootstrapper, ConfigStore |
| ☐ | [06](Days/Day06.md) | 토 | 운전 총괄 | Facade, State Machine, Observer | VisionSystem + 콘솔 데모 |
| ☐ | [07](Days/Day07.md) | 일 | 1주 복습 | 백지 구조도 | 블로그 2편 |

## 2주차: 멀티스레드와 병렬처리 (핵심 주간)

| ✔ | Day | 요일 | 주제 | 패턴/개념 | MiniSight 결과물 |
|---|---|---|---|---|---|
| ☐ | [08](Days/Day08.md) | 월 | 일꾼 만들기 | Thread, ThreadPool, Task, LongRunning, 취소 가능한 sleep | HealthMonitor |
| ☐ | [09](Days/Day09.md) | 화 | 같이 쓰면 망가진다 | race, lock, Monitor, Interlocked, volatile | 통계 카운터, Dispose-once |
| ☐ | [10](Days/Day10.md) | 수 | 줄 서기 1 | Producer/Consumer, BlockingCollection, backpressure | OutputDispatcher |
| ☐ | [11](Days/Day11.md) | 목 | 줄 서기 2 | SemaphoreSlim 2개 큐, Complete/Drain | FrameQueue |
| ☐ | [12](Days/Day12.md) | 금 | 기다리는 법 | async/await, 취소, 타임아웃, TCS, ConfigureAwait | 타임아웃 Connect |
| ☐ | [13](Days/Day13.md) | 토 | 컨베이어 벨트 | Pipeline, 입력별 작업자, Stop/Drain 순서, Mat 소유권 | InspectionPipeline + Runtime |
| ☐ | [14](Days/Day14.md) | 일 | 데드락 실험 + 복습 | 교착 상태, UI 스레드 Wait | 블로그 |

## 3주차: 영상처리, Native, UI 스레드

| ✔ | Day | 요일 | 주제 | 패턴/개념 | MiniSight 결과물 |
|---|---|---|---|---|---|
| ☐ | [15](Days/Day15.md) | 월 | 사진 상자 Mat | 채널, stride, FromPixelData vs Clone, ROI | Mat 소유권 실험 |
| ☐ | [16](Days/Day16.md) | 화 | 필터 체인 | Strategy, Pipeline, JSON 다형성 | IFilter 체인 + 레시피 |
| ☐ | [17](Days/Day17.md) | 수 | 자로 재기 | 캘리퍼, 라인 피팅(PCA), 블랍 | PointGauge, LineGauge, BlobCounter |
| ☐ | [18](Days/Day18.md) | 목 | AI 엔진 다루기 | Dispose 패턴, P/Invoke, Object Pool, RWLock | FakeAiEngine + EnginePool |
| ☐ | [19](Days/Day19.md) | 금 | 화면은 한 명만 | UI 스레드, BeginInvoke, CAS 스로틀, 더블버퍼 | ImageCanvas, Invoke 헬퍼 |
| ☐ | [20](Days/Day20.md) | 토 | 화면 통합 | async 버튼, 라이브 프리뷰, 결과 그리드 | MiniSight WinForms |
| ☐ | [21](Days/Day21.md) | 일 | 3주 복습 | | 블로그 |

## 4주차: 통신, 설정, 안정성, 시니어 관점

| ✔ | Day | 요일 | 주제 | 패턴/개념 | MiniSight 결과물 |
|---|---|---|---|---|---|
| ☐ | [22](Days/Day22.md) | 월 | PLC와 대화 | TCP, 프레이밍, 엔디언, 하트비트, 재연결 | FakePlcServer + PlcClient |
| ☐ | [23](Days/Day23.md) | 화 | 파일은 조심히 | 폴링, 파일 준비 확인, 원자적 저장, Prototype(Clone) | ShareFolderSource, JsonStore |
| ☐ | [24](Days/Day24.md) | 수 | 기록과 마무리 | 비동기 로거, 역순 정지, 멱등 Dispose, 롤백 | AsyncLogger, Shutdown 체인 |
| ☐ | [25](Days/Day25.md) | 목 | 시니어의 눈 | 위험 분석, ADR | async Stop + 타임아웃 개선 |
| ☐ | [26](Days/Day26.md) | 금 | 증명하기 | 가짜 장치 테스트, 골든 이미지, MSTest | MiniSight.Tests |
| ☐ | [27](Days/Day27.md) | 토 | 다른 현장에 옮기기 | Open/Closed, 이식 체크리스트 | 새 카메라 + 새 보고 방식 |
| ☐ | [28](Days/Day28.md) | 일 | 졸업 | 면접 60문항, 포트폴리오 | GitHub README |

## 주차별 졸업 시험 (일요일에 본다)

- **1주차**: 백지에 `Program → Bootstrapper → VisionSystem → DeviceManager → 장치` 그림을 그리고, 각 화살표에 쓰인 패턴 이름을 적는다.
- **2주차**: 카메라 콜백에서 이미지 저장까지 **스레드가 몇 개, 큐가 몇 개, 각 큐 용량과 꽉 찼을 때 동작**을 말로 설명한다. Stop 순서를 순서대로 말한다.
- **3주차**: Mat 하나가 만들어져서 사라질 때까지 **누가 만들고 누가 Dispose하는지** 표로 그린다. UI 스레드에서 하면 안 되는 일 5가지를 말한다.
- **4주차**: "새 현장에 Hikvision 카메라와 다른 PLC 패킷을 붙여야 한다"를 받고, **어떤 파일을 새로 만들고 어떤 파일은 건드리지 않는지** 15분 안에 설명한다.
