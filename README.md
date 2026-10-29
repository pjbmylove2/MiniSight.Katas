# MiniSight · C# 손코딩 다시 세우기

현장 엔지니어로 일하며 C# 비전 검사 프로그램을 **읽고 고치는 일**은 많이 했지만, 직접 처음부터 써 본 경험은 부족했습니다.
이 저장소는 그 빈자리를 채우려고 **키워드 하나에 작은 Kata 하나씩** 직접 타이핑하고, 그 재료로 작은 검사 프로그램(MiniSight)을 만들어 가는 기록입니다.

## 환경
- C# 7.3 / .NET Framework 4.7.2 / Windows Forms
- Visual Studio 2022, x64
- OpenCvSharp4 (MiniSight 영상 처리)

## 구조

| 폴더 | 내용 |
|---|---|
| [`Katas/`](Katas) | 기초 트랙 Kata (B01~B14). 키워드별 콘솔 연습 |
| [`MiniSight/`](MiniSight) | 작은 비전 검사 프로그램 (장치 추상화, 카메라 어댑터·팩토리) |
| [`MiniSight.Katas/`](MiniSight.Katas) | Day별 Kata (MiniSight 부품을 콘솔에서 시험) |
| [`Study/`](Study) | 학습 계획, Kata 문서, 진도 기록 |

## 진도
- 진도표: [Study/PROGRESS.md](Study/PROGRESS.md)
- 기초 트랙 목차: [Study/Basics/README.md](Study/Basics/README.md)

Kata 하나마다 **3번 타이핑**(보면서 → 결과 비교 → 안 보고)하고, 일부러 코드를 망가뜨려 보는 **바꿔 보기**를 한 번 한 뒤 셀프 체크를 통과하면 기록합니다.

## 빌드
1. `MiniSight.sln`을 Visual Studio 2022로 엽니다.
2. 솔루션 우클릭 → **NuGet 패키지 복원**.
3. 플랫폼을 **x64**로 두고 빌드합니다.
