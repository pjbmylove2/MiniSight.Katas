# Day 25 (목, 2h) · 시니어의 눈: 위험 분석과 ADR, 그리고 개선 하나

## 1. 오늘 목표

- **완료 기준**
  - 지난 3주 동안 모은 OmniSight 위험 목록을 **심각도 × 발생 가능성**으로 정렬하고, 각각 "증상 / 원인 / 개선안 / 검증 방법"을 한 줄씩 쓸 수 있다.
  - **ADR(Architecture Decision Record)** 1편을 썼다: "Stop을 비동기 + 타임아웃으로 바꾼다".
  - MiniSight에서 그 ADR을 실제로 구현하고 Day14 실험 C(정지 중 UI 멈춤)가 **해결됐음을 측정**으로 보였다.
  - "현장 코드는 함부로 고치지 않는다"(구 프로그램 동작 보존, 검증 없는 변경 금지) 원칙을 말로 설명할 수 있다.
- **최소분량**: 3, 4, 5
- **확장분량**: 6 개선 구현 + 측정

## 2. 🧒 초1 비유

**놀이터 안전 점검관**. 그네(코드)가 지금 잘 돌아간다고 끝이 아니다. "줄이 닳았나? 아이 둘이 동시에 타면? 비 오면 미끄럽나?"를 본다. 그리고 바로 그네를 뜯지 않는다. **점검표에 쓰고**, 고칠 순서를 정하고, 고친 뒤 **다시 시험**한다. 점검표가 ADR이다: "무엇을 왜 이렇게 바꾸기로 했는지" 남기는 종이.

## 3. 위험 목록 (스터디 중 발견한 것)

Claude에게:
```text
아래 위험 목록 각각에 대해 OmniSight 실제 코드 위치를 다시 보여주고, 지금도 그런지 확인해줘.
그리고 심각도(안전/생산/편의)와 발생 가능성(높음/중간/낮음)을 같이 매겨줘.
```

| # | 위험 | 위치 | 증상 | 근본 원인 |
|---|---|---|---|---|
| R1 | UI 스레드 동기 Stop + lock 안 무제한 대기 | `OmniSight/UI/MainForm.cs:186`, `OmniSight/Application/Inspection/InspectionPipeline.cs:160-163`, `OmniSight/Application/VisionSystem.cs:430` | 정지 버튼 후 화면 "응답 없음" | 점유 대기(UI 스레드 + `_sync`) + 비선점(Wait 무제한) |
| R2 | AI 엔진 풀 대기에 타임아웃·취소 없음 | `OmniSight/Vision/AiEngine/AiEngineRegistry.cs:447` | SDK 하나가 멈추면 모든 파이프라인·정지 멈춤 | `SemaphoreSlim.Wait()` 무제한 |
| R3 | 운전 중 모델 교체 시 RWLock 쓰기 대기가 모든 추론을 막음 | `OmniSight/Vision/AiEngine/AiEngineRegistry.cs:98-184,219-241` | 레시피 저장 순간 검사 정지 | 쓰기 대기자가 새 읽기 차단 |
| R4 | 주석(룰+AI 병렬)과 코드(순차) 불일치 | `OmniSight/Application/Inspection/InspectionPipeline.cs:14,276-289` | 성능 기대와 실제 차이, 오해 | 문서 미갱신 |
| R5 | 공유폴더: 큐 등록 직후 원격 삭제 | `OmniSight/Devices/SharedFolderAdapter.cs` (Acquire 부분) | 비정상 정지 시 최대 5장 재취득 불가 | 삭제 시점 정책 |
| R6 | 이미지 저장 큐 대기 = 느린 디스크가 검사를 늦춤 | `OmniSight/Application/Inspection/InspectionOutputDispatcher.cs:148` | 디스크 지연 시 카메라 큐 포화 | 포화 정책 미결정 |
| R7 | `DeviceBase.State`가 lock 밖에서 쓰이고 여러 스레드가 읽음 | `OmniSight/Devices/DeviceBase.cs:12,35,55,75` | 드물게 오래된 상태 판단 | 사용 규칙이 문서에 없음 |
| R8 | 이벤트를 lock 안에서 발생 | `OmniSight/Application/VisionSystem.cs:495-528` | 구독자가 블로킹하면 교착 | 외부 코드 호출을 lock 안에서 |
| R9 | 송신 루프 5ms 폴링 | `OmniSight/Devices/LegacyOcrSocketDevice.cs:398` | CPU·스레드풀 상시 점유 | 대기 방식 |
| R10 | `LongRunning` + async 람다 | `OmniSight/Application/Inspection/InspectionPipeline.cs:96` | 전용 스레드 의도와 실제 불일치 | async 특성 |
| R11 | Basler 어댑터가 공통 베이스를 안 씀(로직 중복) | `OmniSight/Devices/Cameras/BaslerCameraAdapter.cs` | 한쪽만 버그 수정될 위험 | 이식 이력 |
| R12 | 네이티브 핸들을 `IntPtr`로 관리(SafeHandle 아님) | `OmniSight/Vision/AiEngine/AiEngineBase.cs:17` | 예외 타이밍에 누수/이중 해제 위험 | 이식 단계 |
| R13 | 호출처 없는 코드 | `RecipeRuleInspectionEngine.FindStrongestEdge`, `SharedFolderAdapter.IsStableFile` | 유지보수 혼란 | 정리 누락 |

### 시니어의 우선순위 판단 예시

1. **R1, R2**: 현장에서 "장비가 멈췄다"로 보이는 증상 → 생산 정지. 높음.
2. **R6, R5**: 데이터 손실·처리량. 현장 계약(모든 결과 보존? 이미지 일부 생략 허용?)을 **먼저 확인**해야 고칠 수 있음.
3. **R3, R8**: 운영 규칙(정지 중에만 모델 교체)으로 막을 수 있으면 규칙 + 가드부터.
4. **R4, R10, R13**: 문서·정리. 위험 낮음, 비용 낮음 → 같이 처리.
5. **R11, R12**: 리팩토링. 동작 보존 검증(골든 테스트, Day26) 없이 손대지 않음.

## 4. 원칙: 현장 코드를 고치는 순서 (legacy-code-refactoring 스킬 요약)

1. 원본(구 프로그램) 동작·현장 샘플(영상, 패킷, 로그, CSV) 확보.
2. **현재 동작을 고정하는 테스트**(골든) 먼저.
3. 가장 작은 변경. 관련 없는 정리 섞지 않기.
4. 같은 입력 → 같은 출력 비교. 성능·장애(케이블 뽑기, 디스크 가득) 시험.
5. 변경 이유를 ADR/커밋에 남김.

"TODO를 지웠다 = 완료"가 아니다. **현장 입력·출력 샘플과 장애 시험 결과**가 완료 기준이다 (아키텍처 문서 §7).

## 5. ADR 쓰기 (MiniSight 저장소 `docs/adr/0001-async-stop.md`)

```markdown
# ADR 0001: 검사 정지를 비동기 + 단계별 타임아웃으로 변경

- 상태: 채택 (2026-11-xx)
- 맥락: 정지 버튼이 UI 스레드에서 동기로 VisionSystem.Stop을 호출한다. 내부에서 파이프라인 작업자를
  무제한 대기하고, 작업자는 보고 큐(가득 참)에서 대기할 수 있다. 디스크가 느리면 화면이 수 초~무한히 멈춘다.
  (재현: Day14 실험 C, 측정값: UI 멈춤 __초)
- 결정:
  1. UI는 `await Task.Run(() => vision.Stop())`으로 호출하고 진행 중 버튼을 잠근다.
  2. 파이프라인 drain 대기는 10초 상한. 초과 시 Cancel로 전환하고 "정지 시 폐기 N장" 알람.
  3. 출력 디스패처는 3초 대기 후 남은 기록을 백그라운드에서 마무리, 다음 Start는 그 완료까지 거부(기존 유지).
  4. 모든 대기에 타임아웃 + 알람. 무제한 Wait 금지.
- 대안:
  - A. 현행 유지: 단순하지만 UI 멈춤.
  - B. 완전 비동기 Stop(StopAsync 전파): 가장 깔끔하지만 변경 범위 큼.
  - C. (채택) UI 경계에서만 비동기 + 내부 타임아웃: 변경 작고 효과 큼.
- 결과(트레이드오프):
  - 좋아짐: UI 응답 유지, 정지 상한 시간 보장.
  - 나빠짐: 타임아웃 시 일부 프레임 폐기 가능 → 알람·카운트로 드러냄. 정지 중 다른 입력 차단 로직 필요.
- 검증: 느린 디스크 시뮬레이션(Sleep 5s)에서 UI 멈춤 0초, 정지 완료 ≤ 13초, 폐기 수 로그 확인.
```

## 6. MiniSight 개선 구현 (확장, 하지만 강력 추천)

1. `InspectionPipeline.Stop(TimeSpan drainTimeout)`:
   - drain 모드면 `worker.Wait(drainTimeout)`, 실패하면 Cancel → `worker.Wait(2s)` → 그래도 안 끝나면 알람 + 상태 Faulted(거짓 정지 금지).
2. `AiEnginePool.Inspect`: `Wait(timeout, ct)` (Day18에서 이미 했다면 OK).
3. MainForm Stop: Day20의 async 버튼 (이미 했다면 OK).
4. **측정**: Day14 실험 C 조건에서 개선 전/후
   - UI 멈춤 시간: `Forms.Timer` 100ms 틱 사이 간격의 최댓값을 기록(멈추면 간격이 커짐).
   - 정지 완료 시간, 폐기 프레임 수.
5. 결과 표를 ADR의 "검증"에 채운다.

## 7. VS2022 디버깅 실습

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | 개선 전 버전으로 실험 C → **병렬 스택** 스크린샷 | UI 스레드가 Wait에 |
| 2 | 개선 후 같은 실험 → 병렬 스택 | UI 스레드는 메시지 루프(`Application.Run`)에 있고, 정지는 스레드풀 스레드에서 진행 |
| 3 | 타임아웃 경로에 Tracepoint: `drain 타임아웃 → cancel, 남은 {queue.Count}` | 출력 창 |
| 4 | 두 스크린샷을 ADR에 첨부 | 근거 있는 의사결정 문서 |

## 8. 셀프 체크

1. 위험을 찾았을 때 바로 고치지 않고 ADR부터 쓰는 이유는?
<details><summary>답</summary>현장 코드는 동작 자체가 계약이다. 왜 그렇게 되어 있는지(호환성, 안전, 현장 요구) 모른 채 바꾸면 새 장애를 만든다. ADR로 맥락·대안·트레이드오프를 정리하면 팀이 같이 판단할 수 있고, 나중에 "왜 이렇게 바꿨지?"에 답할 수 있다.</details>

2. "무제한 Wait 금지" 원칙을 지키되 데이터 손실도 막으려면?
<details><summary>답</summary>상한 시간 내에 drain을 시도하고, 초과 시 손실을 숫자로 기록·알람하고, 가능한 경우 남은 작업을 백그라운드에서 마무리하며 다음 시작을 그 완료에 묶는다. 손실 허용 여부 자체는 현장 계약으로 정한다.</details>

3. R6(이미지 큐 대기)을 고치는 방법 세 가지와 각각의 전제 조건은?
<details><summary>답</summary>① TryAdd 후 생략 + 카운트: 이미지 일부 생략이 허용될 때. ② 큐 용량 증가: 메모리 여유와 순간 지연만 흡수하면 될 때. ③ 저장 실패/지연 시 운전 정지: 모든 이미지 보존이 법적·품질 요구일 때. 현장 계약 확인이 먼저다.</details>

4. 주석과 코드가 다를 때(R4) 어떻게 처리하나?
<details><summary>답</summary>코드가 실제 동작이므로 코드를 기준으로 주석을 고친다. 만약 주석이 원래 의도(병렬)였다면 의도를 확인해 별도 변경으로 다룬다. 성능 수치로 차이를 측정해 근거를 남긴다.</details>

5. 리팩토링(R11, R12) 전에 반드시 있어야 하는 것은?
<details><summary>답</summary>현재 동작을 고정하는 테스트(골든 이미지·패킷·CSV 비교, 가짜 드라이버 수명 테스트)와 비교 기준(원본 구 프로그램 결과). 없으면 "같게 동작한다"를 증명할 수 없다.</details>

## 9. 블로그 포인트

- 제목 예: `[시니어 개발] 동작하는 장비 코드에서 위험 찾기와 ADR 쓰는 법`
- MiniSight의 개선 전/후 병렬 스택과 UI 멈춤 측정 표. 회사 위험 목록 자체는 **올리지 않는다**(일반화한 "흔한 위험 패턴"으로).
