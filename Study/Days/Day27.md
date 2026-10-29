# Day 27 (토, 4h) · 다른 현장에 옮기기: 코어는 그대로, 부품만 새로

## 1. 오늘 목표

- **완료 기준**
  - "새 현장"을 받았을 때 **무엇을 먼저 확보하고, 어떤 파일을 새로 만들고, 어떤 파일은 건드리지 않는지** 15분 안에 설명할 수 있다 (4주차 졸업 시험).
  - MiniSight에 **새 카메라 종류**와 **새 결과 보고 방식**을 추가하면서 `InspectionPipeline`, `InspectionRuntime`, `OutputDispatcher`, `VisionSystem` 코드를 **한 줄도 고치지 않았다**(git diff로 증명).
  - 4주 전체 회귀 테스트 통과, 30분 연속 운전에서 메모리·핸들·스레드 수가 안정.
  - 백지에 MiniSight 전체 구조도를 그렸다.
- **최소분량**: 3, 4, 5.1~5.3
- **확장분량**: 5.4 30분 안정성, 6번

## 2. 🧒 초1 비유

**레고 성을 다른 친구 집에 옮겨 짓기**. 성의 뼈대(코어: 파이프라인, 큐, 상태 머신)는 그대로 가져간다. 그 집에 맞게 **문(카메라)과 우편함(보고 방식)만** 바꿔 끼운다. 뼈대를 부숴서 다시 만들면 무너진다.

## 3. OmniSight 지도

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 현장별 변경 순서 | `OmniSight/docs/reference/OMNISIGHT_ARCHITECTURE.ko.md` §6 | 샘플 확보 → 설정 → 어댑터 → 검사 엔진 → 보고 → 1장 대조 → 속도·장애 시험 |
| 지켜야 할 규칙 | 같은 문서 §7 | CSV·로그·경로·소켓 필드 변경은 원본과 비교 |
| 조립 교체 지점 | `OmniSight/Application/Inspection/LegacyOcrInspectionFactory.cs:15-17` 주석, `:19-90` | "다른 검사기는 이 조립 코드와 준비 조건을 별도로" |
| 준비 조건 정책 | `OmniSight/Application/Inspection/LegacyOcrInspectionFactory.cs:103` | `IInspectionReadinessPolicy` 현장별 |
| 장비 타입 정책 | `OmniSight/Configuration/SystemEnums.cs` | `EquipmentType`, 카메라/공유폴더 모드 |
| 출력 분할 | `OmniSight/Application/Inspection/PartitionedInspectionOutputDispatcher.cs` | 입력 2개짜리 설비용 Composite. 런타임은 모름 |
| 현장 준비 계획 | `OmniSight/docs/reference/FIELD_READINESS_PLAN.ko.md` | |
| 운영 문서 | `OmniSight/docs/onboarding/13-MaintenancePlaybook.ko.md` | 유지보수 절차 |

**집에서 Claude에게:**
```text
OMNISIGHT_ARCHITECTURE.ko.md 6장과 7장을 읽고, "새 현장: 카메라 공급사가 다르고 PLC 대신 MES에 HTTP로
결과를 보내야 함" 시나리오로 OmniSight에서 새로 만들 클래스, 수정할 조립 코드, 절대 수정하면 안 되는 파일을
목록으로 만들어줘. 그리고 PartitionedInspectionOutputDispatcher가 왜 런타임 수정 없이 들어갈 수 있었는지 설명해줘.
```

## 4. 새 현장 체크리스트 (암기용)

| 순서 | 확보/결정 | 산출물 |
|---|---|---|
| 1 | 현장 샘플: OK/NG 영상, 실제 패킷 캡처, 기존 로그·CSV·이미지 경로 | 골든 데이터 |
| 2 | 입력 방식: 카메라(벤더·트리거·선속) / 공유폴더 / 기타 | `IImageSource` 어댑터 |
| 3 | 검사 규칙: 룰·AI·준비 조건 | `IRuleEngine`/`IAiEngine`/준비 정책 |
| 4 | 상위 통신: PLC 소켓 / MES / 파일 | `IResultReporter` |
| 5 | 저장 정책: 무엇을, 어디에, 포화 시 | `IImageStore` + 큐 정책 |
| 6 | 장애 정책: 끊김, 디스크 가득, 처리 지연 | 알람·정지 조건 |
| 7 | 조립: **현장 전용 Factory**(Composition Root) | `SiteXInspectionFactory` |
| 8 | 검증: 1장 대조 → 최대 속도 → 장애 시험 → 장시간 | 시험 기록 |

**하면 안 되는 것**: 현장 조건을 UI 이벤트나 공통 파이프라인에 `if (site == X)`로 넣기. 공통 계약과 현장별 구현을 구분한다 (아키텍처 문서 §6).

## 5. MiniSight 오늘 단계 (4시간)

### 5.1 시작 전 기준점
```bash
git add -A
git commit -m "Day27 기준점: 이식 전"
```
(MiniSight 저장소에서. Git을 아직 안 썼다면 오늘 `git init`.)

### 5.2 새 카메라: `SpinningDiskCamera` (가상 "벤더 C")
- 새 `ICameraDriver` 구현: 원판이 돌아가는 영상(합성: 원 + 회전하는 표식)을 25fps로 생성, **프레임 번호가 가끔 건너뜀**(드롭 시뮬레이션).
- `CameraKind.SpinningDisk` 추가 + `CameraFactory`에 case 한 줄.
- 어댑터는 기존 `DriverCameraAdapter` 재사용(드라이버 팩토리만 다름) → 12줄짜리 OmniSight 어댑터와 같은 모양.

### 5.3 새 보고 방식: `HttpJsonReporter` (가상 MES)
- `IResultReporter` 구현: 결과를 JSON으로 `HttpClient.PostAsync` (로컬 `HttpListener` 콘솔을 MES로 흉내, 또는 파일로 대체).
- 실패 시 재시도 3회 + 지수 백오프, 그래도 실패면 로컬 `outbox` 폴더에 원자적 저장(Day23) → 나중에 재전송.
- **동기 계약 주의**: `IResultReporter.Report`가 동기면 내부에서 `GetAwaiter().GetResult()`(보고 작업자는 전용 스레드라 안전, Day12). 계약을 `ReportAsync`로 바꾸고 싶다면? → 코어 수정이 필요하니 ADR로.
- 새 조립: `SiteBFactory.Create(...)` — 카메라 종류와 리포터만 다르고 나머지는 같은 부품.

### 5.4 증명
```bash
git diff --stat HEAD
```
→ 바뀐 파일이 **새 파일 + CameraFactory + 설정 + 새 조립 Factory**뿐이어야 한다. `Inspection/InspectionPipeline.cs`, `InspectionRuntime.cs`, `OutputDispatcher.cs`, `App/VisionSystem.cs`가 목록에 있으면 설계를 다시 본다.

### 5.5 30분 안정성 (확장)
- 카메라 2대 30분 운전, HealthMonitor 값을 1분마다 CSV로.
- 기준: 워밍업 후 PrivateMB 변동 ±5% 이내, 핸들·스레드·GDI 수 일정, 실패 0.
- 중간에 FakePlcServer 끊기/다시 연결, MES 서버 끄기/켜기 → 복구되는지.

## 6. 백지 구조도 (확장이지만 꼭 하기)

아무것도 보지 않고 A4 한 장에:
1. 객체 조립 순서(부트스트래퍼부터)
2. 스레드·큐·용량
3. 소유권 이동 지점
4. 상태 머신
5. 현장별로 바뀌는 부품(색칠)과 안 바뀌는 코어

다 그리면 Claude에게 말로 설명하고 피드백 받기.

## 7. VS2022 디버깅 실습

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | 보기 → **Git 변경 내용** / Git 리포지토리 창 | 오늘 변경 파일 목록을 IDE에서 |
| 2 | `CameraFactory` 새 case에 중단점 → 설정으로 SpinningDisk 선택 | 새 부품만 경로에 들어오는지 |
| 3 | MES 서버를 끈 상태에서 Tracepoint: `재시도 {attempt}/3` | 백오프 동작 |
| 4 | 30분 운전 중 진단 도구 메모리 그래프 | 톱니 모양(정상 GC) vs 우상향(누수) |

## 8. 셀프 체크 (4주차 졸업 시험의 일부)

1. 새 현장에서 가장 먼저 확보해야 하는 것은? 왜?
<details><summary>답</summary>기존 현장의 실제 샘플(영상, 패킷 캡처, 로그·CSV·이미지 경로와 파일명, 원본 프로그램 버전). 동등성 비교 기준이 없으면 무엇을 바꿔도 맞는지 증명할 수 없다.</details>

2. 현장 조건을 공통 파이프라인에 `if`로 넣으면 왜 나쁜가?
<details><summary>답</summary>현장이 늘수록 공통 코드가 분기로 뒤덮여 한 현장 수정이 다른 현장을 깨뜨린다. 수백 대 유지보수가 불가능해진다. 현장 차이는 인터페이스 구현과 현장 전용 조립 코드로 격리한다.</details>

3. 보고 계약이 동기(`Report`)인데 HTTP는 비동기다. 어떻게 하나?
<details><summary>답</summary>보고 작업자가 SynchronizationContext 없는 전용 스레드라면 내부에서 GetAwaiter().GetResult()로 동기 대기해도 데드락이 없다. 장기적으로 계약을 ReportAsync로 바꾸는 것은 코어 변경이므로 ADR과 회귀 테스트를 동반한다(OmniSight 계약은 이미 ReportAsync).</details>

4. 외부 서버(MES)가 죽었을 때 결과를 잃지 않는 방법은?
<details><summary>답</summary>재시도 + 백오프, 그래도 실패하면 로컬에 원자적으로 저장(outbox)하고 복구 후 재전송. 중복 전송에 대비해 결과에 고유 ID를 넣어 수신 측이 멱등 처리하게 한다.</details>

5. "코어를 안 고쳤다"를 어떻게 증명하나?
<details><summary>답</summary>이식 전 기준 커밋과의 diff로 변경 파일을 보여주고, 코어 회귀 테스트(파이프라인 drain, 롤백, 골든)가 그대로 통과함을 보인다.</details>

## 9. 블로그 포인트

- 제목 예: `[설계] 영상 검사 프로그램을 다른 현장으로 옮길 때 코어를 한 줄도 안 고치는 법`
- `git diff --stat` 캡처, 새 부품 목록, 30분 안정성 그래프. 이직 포트폴리오의 **대표 글**로 좋다.
