# 시니어 SW 필드엔지니어 복습 여정: MiniSight 따라 만들기

> 목표: **초급(손코딩) → 주니어(프로그램 설계·구현) → 시니어(현장 책임)** 3단계로 다시 다진다. 주니어를 졸업하면 **백지에서 다른 현장용 검사 프로그램을 설계하고 왜 그렇게 설계했는지 면접에서 설명**할 수 있고, 시니어를 졸업하면 **장애를 혼자 재현·분석·보고하고 다른 현장에 옮길 수** 있어야 한다. 전체 계획은 [00-Roadmap.md](00-Roadmap.md).

## 1. 이 스터디는 이렇게 돌아간다

```text
[회사 PC]  OmniSight 실제 코드 + 이 Study/ 문서 + Claude 세션(Remote Control 켜짐)
     ▲
     │ 집에서 Claude 앱으로 원격 접속 → "Day 09 시작" 이라고 말한다
     ▼
[집 PC]    VS2022에서 MiniSight(축소판 OmniSight)를 손으로 코딩·디버깅
```

- 회사 코드는 **읽기만** 한다. 집에서는 Claude가 회사 PC의 코드를 대신 읽어서 보여주고 설명한다.
- 손으로 치는 건 전부 **MiniSight**다. 회사 코드를 복사하지 않는다. 구조와 이유를 이해한 뒤 내 손으로 다시 만든다.
- 버전은 회사와 같다: **.NET Framework 4.7.2, C# 7.3, WinForms, x64**.

## 1-1. 진행 규칙 보완 (2026-10-28, 2회차부터 적용)

Day01~04를 해 보니 **패턴보다 키워드(`Task`, `CancellationToken`, `event`, `Interlocked`, `?.`, `Invoke` 등)를 손으로 써 본 적이 없는 것**이 진짜 문제였다. 그래서 아래처럼 바꾼다.

1. **0주차 기초 트랙 [Basics/](Basics/README.md) B01~B14를 먼저** 하고, 졸업하면 Day01부터 다시 시작한다 (2회차).
2. **처음 나오는 키워드·기호는 매번 표로 설명한다**: 역할, 사용법, 같은 뜻의 긴 코드. 모아 보기는 [Cheatsheets/Keywords.md](Cheatsheets/Keywords.md).
3. **한 줄짜리 `if`/`for`/`foreach`/`while`도 무조건 `{ }`** (이유: Apple goto fail 버그, [B01 §6](Basics/B01-NullAndArrow.md#6-중괄호-규칙-goto-fail)). Day 문서 안 코드에 중괄호가 빠져 있으면 Claude가 진행할 때 고쳐서 보여준다.
4. **3번 타이핑**: 보면서 → 결과 비교 → 지우고 안 보고. 복사·붙여넣기 금지.
5. **한 번에 작은 조각**: MiniSight 코드는 파일 1개씩, "다음"이라고 하면 다음 조각으로 간다. 조각마다 "지금 뭘 만드는지" 한 줄 → 코드 → 질문 1개.
6. **패턴은 키워드 배치도로 설명한다**: "Template Method = B02 abstract/virtual 배치", "Adapter = B14 카메라를 같은 모양으로 감싼 것"처럼, 이미 친 기초 Kata에 연결한다.
7. **넘어가는 기준**: 코드가 돌아가고 핵심을 한 문장으로 말하면(약 70%) 넘어간다. 남은 건 PROGRESS의 "다시 물어볼 질문"에 적는다.

## 2. 하루 루틴

| 시간 | 평일 (2시간) | 토요일 (4시간) | 일요일 (4시간) |
|---|---|---|---|
| 0:00 | Claude에게 "Day NN 시작" | 같음 | 밀린 Day 처리 |
| 0:10 | 🧒 비유 읽기 → OmniSight 실제 코드 보기 | MiniSight 통합 작업 | 주간 퀴즈 (Interview.md) |
| 0:40 | 패턴/개념 Kata 손코딩 | (계속) | 백지 구조도 그리기 |
| 1:20 | MiniSight 오늘 단계 + 디버깅 실습 | 디버깅 실습 | 블로그 1~2편 |
| 1:50 | 셀프 체크 5문항, PROGRESS.md 기록 | 셀프 체크, 기록 | 다음 주 미리보기 |

### 집에서 Claude에게 보내는 문장 (복사해서 쓰기)

```text
Study/Days/Day09.md 시작하자. 1번부터 순서대로, 초1도 알게 비유로 먼저 설명하고
OmniSight 실제 코드를 보여주면서 "왜 이렇게 썼는지" 설명해줘.
```

```text
이건 내가 집에서 짠 MiniSight 코드야. C# 7.3 / .NET Framework 4.7.2 기준으로
스레드 안전, Dispose, 예외 처리 관점에서 리뷰해줘. 정답 코드는 주지 말고 힌트만 줘.
```

```text
오늘 Day09 최소분량까지만 했어. PROGRESS.md 업데이트하고, 확장분량은 일요일로 넘겨줘.
```

```text
셀프 체크 문제 내줘. 내가 말로 답하면 면접관처럼 꼬리 질문해줘.
```

## 3. 밀림 규칙 (스트레스 받지 않기)

- 모든 Day는 **최소분량**과 **확장분량**으로 나뉜다.
- 최소분량을 끝냈으면 다음 날은 다음 Day로 간다. 확장분량은 **일요일 버퍼**로 보낸다.
- 최소분량도 못 끝냈으면 다음 날 같은 Day를 이어서 한다. 4주가 5주가 되어도 괜찮다. **이해 없이 진도만 나가는 게 제일 나쁘다.**
- 2주차(멀티스레드)는 가장 중요하다. 여기서 밀리면 3주차를 줄이고 2주차를 지킨다.

## 4. 문서 지도

| 파일 | 용도 |
|---|---|
| [00-Roadmap.md](00-Roadmap.md) | 3단계(초급·주니어·시니어) 전체 표, 단계별 졸업 기준 |
| [01-MiniSight-Spec.md](01-MiniSight-Spec.md) | 집 프로젝트의 폴더·인터페이스·NuGet·완성 단계 |
| [PROGRESS.md](PROGRESS.md) | 날짜별 진도, 밀린 것, 질문 |
| [Basics/](Basics/README.md) | 1단계 초급 B01~B14 (키워드 하나에 Kata 하나) |
| [Senior/](Senior/README.md) | 3단계 시니어 S01~S12 (프로토콜·장애 재현·배포·CI·현장 문서) |
| [Cheatsheets/Keywords.md](Cheatsheets/Keywords.md) | 키워드·기호 한 줄 사전 (어느 B에서 배우고 어느 Day에 나오나) |
| [FieldSkills.md](FieldSkills.md) | FA 현장 SW 필드엔지니어 역량 지도 (통신·진단·카메라·배포·소통, 수준 체크) |
| `Days/Day01.md` ~ `Days/Day28.md` | 2단계 주니어 하루치 가이드 |
| [Cheatsheets/Patterns.md](Cheatsheets/Patterns.md) | OmniSight 디자인 패턴 지도 |
| [Cheatsheets/Threading.md](Cheatsheets/Threading.md) | 스레드·큐·동기화 도구 정리 |
| [Cheatsheets/VS2022-Debugging.md](Cheatsheets/VS2022-Debugging.md) | 디버깅 단축키·창·덤프·프로파일러 |
| [Cheatsheets/OpenCV.md](Cheatsheets/OpenCV.md) | Mat 소유권, 필터, 게이지 알고리즘 |
| [Cheatsheets/Interview.md](Cheatsheets/Interview.md) | 면접 질문 60개 |
| [Templates/BlogPost.md](Templates/BlogPost.md) | Velog/Tistory 글 템플릿 |

## 5. Day 문서는 항상 이 순서다

1. **오늘 목표**: 완료 기준, 최소분량, 확장분량
2. **🧒 초1 비유**: 어려운 말 없이 그림처럼 이해
3. **OmniSight 지도**: 볼 파일과 줄 번호, Claude에게 보낼 문장
4. **왜 이렇게 썼나**: 의도, 다른 방법, 장단점, 지금 코드의 약점
5. **Kata**: 회사 코드와 무관한 작은 예제로 패턴을 손에 익힘
6. **MiniSight 오늘 단계**: 집 프로젝트에 붙일 코드
7. **VS2022 디버깅 실습**: 눈으로 확인
8. **셀프 체크**: 면접형 5문항 (답은 접혀 있음)
9. **블로그 포인트**: 오늘 쓸 글감

## 6. 보안 규칙 (반드시 지킨다)

블로그와 GitHub(포트폴리오 저장소)에 올릴 때 지킨다. (2026-10-29 개정: OmniSight는 AI와 함께 리팩토링한 코드라 이 Study 문서 속 인용·파일 경로는 공개해도 된다.)

- ❌ 회사명, 고객사명, 현장명, 설비명(실제 라인 이름)
- ❌ OmniSight 소스 저장소 전체를 그대로 올리기 (Study 문서 속 인용은 괜찮다)
- ❌ 실제 PLC 패킷 필드 순서·바이트 구성, 실제 경로·설정값·IP
- ❌ AI SDK 이름·내부 함수명·사용법, 라이선스 관련 내용
- ❌ 구 프로그램(레거시) 이름과 그 내부 내용
- ✅ "영상 검사 프로그램에서 Producer/Consumer를 이렇게 썼다"처럼 일반화한 설명
- ✅ MiniSight 자체 코드, 직접 그린 일반 구조도, 실험 결과

AI SDK와 구 프로그램 이름은 이 문서에서는 그대로 쓰고(실제 코드를 찾아야 하므로), **포트폴리오 저장소로 복사할 때 `publish.ps1`이 자동으로 가린다.** 가리는 목록은 저장소 밖 `publish-sanitize.ps1`에 있고, 가린 뒤에도 이름이 남아 있으면 올리지 않는다. 블로그 글은 직접 가려서 쓴다.

줄 번호는 커밋 `93aaca9` 기준이다. 회사 코드가 바뀌면 줄 번호가 달라질 수 있으니 Claude에게 "심볼 이름으로 다시 찾아줘"라고 하면 된다.

## 7. Remote Control 주의

- 회사 PC와 이 Claude 세션이 켜져 있어야 집에서 붙을 수 있다.
- 세션이 끊겼다면 이 worktree(`.claude/worktrees/omnisight-study-plan-4bc7dd`)에서 새 세션을 열고 "Study/README.md 읽고 PROGRESS.md 이어서 하자"라고 하면 된다.
