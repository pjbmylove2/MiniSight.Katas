# Day 23 (화, 2h) · 파일은 조심히: 공유폴더 입력, 원자적 저장, Clone 후 Commit

## 1. 오늘 목표

- **완료 기준**
  - 네트워크 공유폴더(SMB)에서 `FileSystemWatcher` 대신 **폴링**을 쓰는 이유를 설명할 수 있다.
  - "쓰는 중인 파일"을 읽지 않는 방법(배타적 열기 시도, 크기 안정 확인)과 `.copying` → `Move` **원자적 이름 바꾸기**를 설명할 수 있다.
  - 설정 저장을 **`.tmp`에 쓰고 디스크까지 flush → `File.Replace`로 교체 + `.bak` 보관**하는 이유(정전 중에도 원본 또는 새본 중 하나는 온전)를 설명할 수 있다.
  - UI는 **복제본을 편집하고 저장 시 통째로 교체**(Prototype + copy-on-write)하는 이유를 설명할 수 있다.
  - MiniSight: `JsonStore<T>`(원자적 저장, .bak 복구, GetClone/ApplyAndSave)와 `ShareFolderSource`(폴링, 준비 확인, backpressure) 작성.
  - 저장 도중 **프로세스 강제 종료 실험**을 했고 설정이 깨지지 않았다.
- **최소분량**: 2, 3, 4, 5, 6.1
- **확장분량**: 6.2 ShareFolderSource, 7번 3~4

## 2. 🧒 초1 비유

- **FileSystemWatcher = 초인종**. 집(로컬 디스크)에선 잘 울리는데, 멀리 있는 친구 집(네트워크 폴더)에선 초인종이 가끔 안 울린다. 그래서 **50ms마다 창밖을 직접 본다**(폴링).
- **쓰는 중인 파일 = 아직 굽는 중인 케이크**. 오븐 문을 열어 보려 했는데 잠겨 있으면(공유 위반) 아직 굽는 중이다. 다음에 다시 본다.
- **.copying → Move = 포장지 씌워서 옮긴 뒤 이름표 붙이기**. 반쯤 옮긴 상자를 누가 먼저 가져가지 않게, 다 옮긴 뒤에 진짜 이름을 붙인다.
- **원자적 저장 = 새 일기장에 다 쓰고 나서 바꿔치기**. 헌 일기장을 지우고 쓰다가 정전되면 일기가 다 날아간다. 새 공책(.tmp)에 다 쓰고, 꼭꼭 눌러 쓴 뒤(flush), 한 번에 바꾼다. 헌 공책은 서랍(.bak)에 보관.
- **Clone 후 Commit = 숙제를 연습장에서 하고 다 맞으면 공책에 옮기기**. 편집 중인 반쯤 된 값을 검사 스레드가 보지 않는다.

## 3. OmniSight 지도

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 폴링 이유 주석 | `OmniSight/Devices/SharedFolderAdapter.cs:23-26` | SMB에서 Watcher 이벤트 누락 |
| 감시 루프 | `OmniSight/Devices/SharedFolderAdapter.cs:248-322` | 50ms 폴링, 날짜 폴더, 정규식·확장자 필터, 오류 시 1초 백오프, 실패 기록 4096개 상한 |
| 준비 확인 | `OmniSight/Devices/SharedFolderAdapter.cs:329-356` | Win32 `CreateFile(FILE_SHARE_NONE)`, 오류 32/33이면 "아직 쓰는 중" → 건너뜀 |
| 가져오기 | `OmniSight/Devices/SharedFolderAdapter.cs:358-468` | 로컬 `.copying`에 1MB 버퍼 복사 + `Flush(true)` → Move → `ImRead` → 큐(5) Add(대기) → 원격 삭제 |
| 파일명 해석 | `OmniSight/Devices/SharedFolderAdapter.cs:470-514` | `_`로 나눠 트리거·카운트·카메라 번호 |
| 큐 대기 | `OmniSight/Devices/SharedFolderAdapter.cs` (`WaitFrameAsync`) | `Task.Run(() => Take)` — Day11과 비교 |
| 설정 제공자 | `OmniSight/Configuration/JsonConfigProvider.cs:18-35` | `ReaderWriterLockSlim`, `Current` |
| 로드 + 복구 | `OmniSight/Configuration/JsonConfigProvider.cs:40-95` 부근 | 파일 없으면 기본값 생성·저장, 깨졌으면 `.bak` |
| 원자적 저장 | `OmniSight/Configuration/JsonConfigProvider.cs:118-188` | `FileOptions.WriteThrough` + `Flush(true)` → `File.Replace(tmp, file, bak)` → finally에서 tmp 정리 |
| 변경 내용 로그 | `OmniSight/Configuration/JsonConfigProvider.cs:190-213` | 바뀐 키만 기록, password/secret은 값 숨김 |
| 복제 | `OmniSight/Configuration/JsonConfigProvider.cs:218-227` | JSON 왕복 깊은 복사 |
| 교체 저장 | `OmniSight/Configuration/JsonConfigProvider.cs:232-251` | 쓰기 잠금 안에서 교체 → 저장 실패면 **되돌리고** IOException |
| 읽기 전용 참조 | `OmniSight/Configuration/JsonConfigProvider.cs:253-263` | "반환 객체를 읽기 전용으로 취급" 규칙 |

**집에서 Claude에게:**
```text
JsonConfigProvider.cs 118~263줄을 일기장 바꿔치기·연습장 비유로 설명해줘. WriteThrough와 Flush(true)가
각각 무엇을 보장하는지, File.Replace가 같은 볼륨이어야 하는 이유, ApplyAndSave에서 저장 실패 시
Current를 되돌리는 이유를 알려줘. 그다음 SharedFolderAdapter.cs 329~468줄로 "굽는 중인 케이크" 처리를 보여줘.
```

## 4. 왜 이렇게 썼나

### 4.1 저장 단계별 정전 시나리오

| 정전 시점 | 남는 것 | 결과 |
|---|---|---|
| `.tmp` 쓰는 중 | 원본 + 깨진 tmp | 원본 그대로. 다음 저장이 tmp 덮어씀 |
| `Flush(true)` 후, Replace 전 | 원본 + 완성된 tmp | 원본 그대로 |
| Replace 중 | OS가 이름 교체를 원자적으로 처리 | 원본 또는 새본 |
| Replace 후 | 새본 + `.bak`(옛 원본) | 새본. 새본이 깨졌다면 Load가 .bak 사용 |

직접 덮어쓰기(`File.WriteAllText(path, json)`)는 "파일을 0바이트로 자르고 쓰는" 중간 상태가 있어서 정전 시 **빈 설정 파일**이 남는다. 장비가 다음 부팅에 기본값으로 돌아가 버린다.

### 4.2 결정과 이유

| 결정 | 이유 |
|---|---|
| `FileOptions.WriteThrough` + `Flush(true)` | OS 캐시에만 있고 디스크엔 없는 상태를 없앰. `Flush()`만으론 .NET 버퍼만 비움 |
| 쓰기 잠금 안에서 교체+저장 | 저장 도중 다른 스레드가 반쯤 바뀐 상태를 못 봄 |
| 실패 시 Current 되돌림 | "메모리엔 새 값, 디스크엔 옛 값" 불일치 방지. 재부팅하면 설정이 바뀌는 귀신 현상 |
| UI는 GetClone으로 편집 | 사용자가 숫자를 입력하는 도중 검사 스레드가 그 값을 쓰지 않게 |
| 런타임은 `GetCurrentReference` + 읽기 전용 규칙 | 매 프레임 복제는 비쌈. 대신 **절대 수정하지 않는다**는 규칙 |
| 변경 로그에서 비밀 값 숨김 | 로그가 외부로 나가도 비밀번호 노출 없음 |
| 공유폴더 폴링 50ms | Watcher 누락 대비. 대신 디렉터리 나열 비용 → 날짜 폴더로 범위 축소 |
| 배타적 열기 실패 = 쓰는 중 | 크기 비교(2번 읽기)보다 빠르고 확실 |
| 로컬에 `.copying`으로 복사 후 Move | 반쯤 복사된 파일을 다른 코드가 정상 파일로 오인하지 않게 |
| 큐 Add가 대기 | 처리가 느리면 원격 폴더에 파일이 **남아 있게**(backpressure). 원격이 버퍼 역할 |
| 실패 파일 기록 4096 상한 | 같은 깨진 파일을 무한 재시도하지 않되, 기록이 무한히 커지지도 않게 → 상한 도달 시 입력 정지 + 알람 |

### 4.3 시니어의 눈

- 큐에 넣은 직후 **원격 원본을 삭제**한다. 그 뒤 취소 정지(drain 아님)로 큐의 5장이 버려지면 **다시 가져올 수 없다**. "삭제는 처리 완료 후"가 더 안전하지만 중복 처리 방지 설계가 필요하다 → 트레이드오프.
- 크기 안정 확인 함수(`IsStableFile`)가 있지만 호출처가 없다 → 죽은 코드 후보.
- 클래스 주석(`OmniSight/Devices/SharedFolderAdapter.cs:25-26`)은 "150ms 폴링 + 크기 안정성 검증"이라고 하지만 실제 코드는 `PollIntervalMs = 50`(`:46`)이고 준비 확인은 배타적 열기다. Day13의 룰/AI "병렬" 주석과 같은 유형 → **주석보다 코드를 믿고, 불일치는 기록해서 보고**.
- `GetCurrentReference`의 "수정 금지"는 컴파일러가 막아 주지 않는다. 불변 클래스(get만 있는 속성)로 만들면 강제된다. 하지만 JSON·UI 바인딩 편의와 충돌 → 현실적 절충.

## 5. Kata: 일기장 바꿔치기

```csharp
// KATA: Day23
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Katas.Day23
{
    public static class AtomicFile
    {
        public static void Save(string path, string content)
        {
            string tmp = path + ".tmp";
            string bak = path + ".bak";
            try
            {
                using (var stream = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(content);
                    writer.Flush();
                    stream.Flush(true);                    // OS 캐시 → 디스크
                }
                if (File.Exists(path)) File.Replace(tmp, path, bak, true);
                else File.Move(tmp, path);
            }
            finally
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch (IOException) { }
            }
        }

        public static string Load(string path)
        {
            try { return Validate(File.ReadAllText(path)); }
            catch (Exception ex) when (ex is IOException || ex is FormatException)
            {
                Console.WriteLine("원본 손상(" + ex.GetType().Name + ") → .bak 사용");
                return Validate(File.ReadAllText(path + ".bak"));
            }
        }

        private static string Validate(string text)
        {
            if (!text.StartsWith("{") || !text.TrimEnd().EndsWith("}")) throw new FormatException("JSON 아님");
            return text;
        }
    }

    public static class KataDay23
    {
        public static void Run()
        {
            string dir = @"C:\MiniSight\Kata23";
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "config.json");

            AtomicFile.Save(path, "{ \"threshold\": 100 }");
            AtomicFile.Save(path, "{ \"threshold\": 120 }");
            Console.WriteLine("현재: " + AtomicFile.Load(path));
            Console.WriteLine("백업: " + File.ReadAllText(path + ".bak"));

            // 나쁜 저장 흉내: 직접 덮어쓰다 중간에 죽음
            using (var s = new FileStream(path, FileMode.Create)) { s.WriteByte((byte)'{'); }   // 반쪽 파일
            Console.WriteLine("손상 후 로드: " + AtomicFile.Load(path));

            // 강제 종료 실험 안내
            Console.WriteLine("확장: 큰 문자열(50MB)을 Save 하는 동안 작업 관리자에서 프로세스를 끝내 보고, 다시 Load 해서 깨졌는지 확인.");
        }
    }
}
```

## 6. MiniSight 오늘 단계

### 6.1 `Config/JsonStore<T> where T : class, new()` (Day05 ConfigStore를 교체)
- `Load(path)`: 없으면 기본값 저장, 역직렬화 실패면 `.bak`, 둘 다 실패면 기본값 + 알람.
- `SaveInternal`: Kata의 `AtomicFile.Save` 방식, Newtonsoft `Formatting.Indented`, Day16의 허용 목록 바인더.
- `GetClone()`: JSON 왕복. `ApplyAndSave(T edited)`: 쓰기 잠금, 교체, 실패 시 되돌리고 `IOException`.
- `GetCurrentReference()`: 읽기 잠금, 문서 주석에 "읽기 전용".
- Bootstrapper/Preprocessor/RuleEngine은 `() => store.GetCurrentReference()`.

### 6.2 `Devices/ShareFolderSource.cs : DeviceBase, IImageSource, IDrainableSource` (확장)
- 원격 폴더(실험은 로컬 `C:\MiniSight\Inbox`) 50ms 폴링, 오래된 순 정렬.
- 준비 확인: `new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None)` 시도 → `IOException`이면 건너뜀 (Win32 호출 대신 .NET 방식).
- 로컬 `TEMP\name.copying`으로 복사 → Move → `ImRead` → `FrameQueue.Add`(대기) → 원본 삭제(정책 주석으로 트레이드오프 기록).
- 처리 실패 파일은 `HashSet` 기록(상한 1000, 초과 시 Faulted).
- 테스트: 다른 콘솔에서 PNG를 **천천히 쓰는** 프로그램(1KB씩 100ms 간격)을 만들어 반쪽 파일을 안 읽는지 확인.

## 7. VS2022 디버깅 실습

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | Kata `File.Replace` 줄에 중단점 → 탐색기에서 폴더 열어 두기 | tmp와 원본이 동시에 있는 순간 |
| 2 | 예외 설정에서 `System.IO.IOException` 체크 → 6.2 천천히 쓰기 실험 | 준비 확인에서 던져지는 공유 위반 예외를 첫 기회에 잡음(그 후 F5로 계속 — 정상 흐름) |
| 3 | `ApplyAndSave`에서 저장 경로를 읽기 전용 폴더로 바꿔 실패 유도 | Current가 되돌려지는지 조사식으로 |
| 4 | (확장) Sysinternals **Process Monitor**로 MiniSight의 파일 I/O 필터링 | CreateFile/WriteFile/SetRenameInformation 순서를 OS 수준에서 확인 |

## 8. 셀프 체크

1. `Flush()`와 `Flush(true)`의 차이는?
<details><summary>답</summary>`Flush()`는 .NET FileStream 내부 버퍼를 OS로 넘긴다(OS 캐시에 머물 수 있음). `Flush(true)`는 OS 파일 버퍼까지 디스크로 내리라고 요청한다(FlushFileBuffers). 정전 내구성에는 후자가 필요하다.</details>

2. `File.Replace`가 원자적이려면 어떤 조건이 필요한가?
<details><summary>답</summary>원본·임시·백업 파일이 같은 볼륨(파티션)에 있어야 한다. 다른 볼륨이면 복사가 필요해 원자성이 깨지거나 예외가 난다. 그래서 tmp를 대상 파일 옆에 만든다.</details>

3. UI가 Current 객체를 직접 편집하면 어떤 일이 생기나?
<details><summary>답</summary>사용자가 입력하는 도중의 값(예: 임계값 1 → 12 → 120 중간)을 검사 스레드가 읽어 엉뚱한 판정을 한다. 여러 필드가 일부만 바뀐 상태도 보인다. 저장 취소도 불가능하다.</details>

4. 네트워크 공유폴더에서 FileSystemWatcher를 믿기 어려운 이유는?
<details><summary>답</summary>SMB 서버 구현·버퍼 크기·연결 끊김에 따라 변경 알림이 누락되거나 몰려서 온다. 재연결 후 놓친 이벤트를 복구할 방법도 없다. 폴링은 비용이 있지만 결국 모든 파일을 발견한다.</details>

5. 원격 파일을 "큐에 넣은 직후" 삭제하는 것과 "처리 완료 후" 삭제하는 것의 트레이드오프는?
<details><summary>답</summary>즉시 삭제: 다음 폴링에서 중복으로 가져오지 않아 단순하지만, 정지·크래시 시 큐 안의 프레임이 사라진다. 완료 후 삭제: 손실이 없지만 처리 중인 파일을 다시 가져오지 않도록 "처리 중" 목록 관리와 재시작 시 중복 처리 대책이 필요하다.</details>

## 9. 블로그 포인트

- 제목 예: `[C# 파일 I/O] 정전에도 설정 파일이 안 깨지게: .tmp + Flush(true) + File.Replace`
- 정전 시나리오 표, Kata 손상 복구 출력, "반쪽 파일 안 읽기" 실험.
