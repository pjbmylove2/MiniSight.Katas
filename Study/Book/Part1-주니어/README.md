# Part 1 · L1 주니어 (J01~J18)

> 목표: 장치·파일·스레드·영상·통신을 갖춘 작은 검사 프로그램 **MiniSight2**를 처음부터 손으로 만들고, 왜 그렇게 만들었는지 설명한다.
> 장 하나 = 2~4일차. 장마다 MiniSight2에 기능이 하나씩 붙는다. 블록 시험은 J06·J12·J18 뒤에 있다.

## MiniSight2 한 장 그림 (L1이 끝났을 때)

```text
[가짜 카메라] --프레임--> [프레임 큐] --> [검사 파이프라인: 전처리 → 측정 → 판정] --> [결과 큐] --> [TCP 송신 → 가짜 PLC]
      |                                              |                                         |
      +--최신 1장--> [WinForms 화면]                  +--> [로그]                               +--> [결과 파일(JSON/CSV)]
[설정·레시피·모델 파일(JSON, 버전)] → 조립 지점(Bootstrapper) → 모든 부품
```

## 장 목록

| 장 | 일차 수 | 주제 | 핵심 내용 | MiniSight2 결과물 | 짝 수학 |
|---|---|---|---|---|---|
| J01 | 3 | 통신의 언어: 비트·바이트 | 2진·16진, 비트 마스크, 엔디언, `BitConverter`, 체크섬, 패킷을 바이트로 만들고 풀기 | `ByteTools` + 단위 테스트 | M01 |
| J02 | 2 | 디버거와 친해지기 | VS 중단점·조건 중단점·조사식·호출 스택·예외 설정, 전역 예외 처리, 단일 실행(Mutex) | 솔루션 뼈대 + `Program` 안전망 | |
| J03 | 3 | 인터페이스와 계약 | SOLID(DIP·ISP), `IDisposable` 소유권, "누가 만들고 누가 버리나" | `Contracts`, `ImageFrame` | |
| J04 | 3 | 장치 수명과 상태 머신 | 템플릿 메서드, 상태 enum과 전이표, 실패 시 롤백 | `DeviceBase`, 상태 전이표 | |
| J05 | 3 | 장치 바꿔 끼우기와 조립 | Simple Factory, Adapter, Composition Root, `Func` 주입 | `FakeCamera`, `CameraFactory`, `Bootstrapper` | |
| J06 | 3 | 파일 입출력 | 경로·`Path`, 스트림·버퍼, 텍스트/바이너리, 원자적 저장(임시 파일 → 교체), 파일 잠금, `FileSystemWatcher`와 파일 준비 확인 | `SafeFile`, 결과 CSV 저장 | |
| **블록 시험 1** | 1 | J01~J06 | 백지 구조도 + 바이트 문제 + 원자적 저장 실기 | | |
| J07 | 3 | JSON 설정·레시피·모델 파일 관리 | 직렬화, 스키마 버전과 마이그레이션, 검증, 백업·롤백, 레시피 폴더 구조, 모델 파일 메타데이터(버전·해시·입력 크기) | `ConfigStore`, `RecipeStore`, `ModelCatalog` | |
| J08 | 3 | 스레드 기초와 종료 순서 | `Thread`·`Join`·`IsBackground`, 정지 신호(`ManualResetEvent`), Stop → Join → Dispose 순서 | 카메라 스레드, Stop 순서 | |
| J09 | 3 | 공유 자원과 데드락 | race, `lock`, `Interlocked`, `CompareExchange` 팻말, 데드락 만들기·순서로 고치기 | 통계 카운터, Dispose-once | |
| J10 | 3 | 줄 서기와 파이프라인 | 생산자/소비자, `BlockingCollection`, 꽉 찼을 때 정책(기다리기·새것/옛것 버리기), `CompleteAdding`, 단계 파이프라인 | `FrameQueue`, `InspectionPipeline` | M08 |
| J11 | 3 | Task·async·병렬·취소 | `Task.Run`·`WhenAll`, `async/await`(UI에서 `.Result` 금지), `CancellationToken`·타임아웃, `Parallel.For`와 `localInit/localFinally`, 암달의 법칙 | 타임아웃 연결, 병렬 전처리 | |
| J12 | 3 | 영상 기초 | 픽셀·채널·stride·패딩, `Bitmap`과 `Mat`, `LockBits`, 원본·ROI·복사본의 소유권, 해제 표 | `ImageFrame` ↔ `Mat` 변환, 소유권 표 | M02 |
| **블록 시험 2** | 1 | J07~J12 | 스레드·큐 그림 설명 + Mat 소유권 표 + 데드락 실기 | | |
| J13 | 4 | 영상 전처리 | 그레이·히스토그램, 고정·Otsu·Adaptive 이진화, Blur·가우시안·미디언, 모폴로지(침식·팽창·열기·닫기), 단계별 중간 영상 저장 | `IFilter` 체인 + 레시피 | M03, M05 |
| J14 | 3 | 측정 | 엣지(Sobel·Canny), 캘리퍼, 최소제곱 라인/원 피팅, 블롭(ConnectedComponents), px→mm | `LineGauge`, `CircleGauge`, `BlobCounter` | M04 |
| J15 | 3 | UI 스레드와 화면 | `InvokeRequired`·`BeginInvoke`, 최신 1장만 그리기, 더블 버퍼, async 버튼, 결과 그리드 | WinForms 화면, 라이브 프리뷰 | |
| J16 | 4 | 이더넷 TCP/UDP와 패킷 | 소켓 클라이언트/서버, 프레이밍(길이 헤더·구분자), 패킷 설계·체크섬, 하트비트·타임아웃·재연결, UDP와 차이, 루프백 Wireshark | `FakePlcServer`, `PlcClient`, 결과 송신 | M01 |
| J17 | 3 | 로그·종료·테스트·Git | 비동기 로거(시각·스레드·레벨·CycleId), 역순 정지·멱등 Dispose, MSTest(가짜 장치·골든 입력·`[Timeout]`), Git 브랜치·태그·diff | `AsyncLogger`, `Shutdown`, `MiniSight2.Tests` | |
| J18 | 3 | 주니어 졸업 | 구조 설명, 데드락·누수 덤프 실기, "새 현장" 이식 계획, 면접 60문항 | README, 졸업 시험 | |

## 장 문서가 담을 것 (본문 소제목 기준)

각 장 문서는 시작할 때 쓴다. 소제목은 이렇게 고정한다.

1. 이 장에서 할 수 있게 되는 것
2. 🧒 비유
3. 본문 — 개념 / 왜 필요한가 / 컴퓨터 안에서 일어나는 일 / 현장 사례 / 흔한 실수
4. 수학 노트
5. 손코딩 조각 (일차마다 4~8개)
6. MiniSight2 단계
7. 현장 연결 — 장비 SW / TS / 반도체·디스플레이·2차전지
8. 셀프 체크와 면접 질문
9. 블로그 요약

## 블록 시험과 L1 졸업 기준

| 시험 | 내용 | 통과 |
|---|---|---|
| 블록 1 (J06 뒤) | 백지에 Program → Bootstrapper → 장치 구조도와 패턴 이름, 패킷 16바이트 손으로 풀기, 원자적 저장 실기 | 구조도 빠짐 없음, 바이트 문제 전부, 실기 20분 |
| 블록 2 (J12 뒤) | 카메라 콜백 → 저장까지 스레드·큐 수와 꽉 찼을 때 동작 설명, Mat 하나의 생성~해제 표, 데드락 재현·덤프로 원인 줄 찾기 | 설명 10분, 표 정확, 실기 30분 |
| 블록 3 = J18 | 촬영 → 전처리·측정 → TCP 송신 → 로그 → 안전한 종료가 가짜 장치로 한 번에 돈다. "다른 카메라·다른 PLC 패킷" 이식 계획 15분. 면접 60문항 중 45 이상 | 모두 |
