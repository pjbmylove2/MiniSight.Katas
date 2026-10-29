# Day 05 (금, 2h) · Composition Root, Singleton, Func 설정 주입

## 1. 오늘 목표

- **완료 기준**
  - "`new`는 조립 지점에서만"이라는 규칙과 그 이유를 말할 수 있다.
  - OmniSight 부팅 6단계를 순서대로 말할 수 있다 (경로 → 설정 → AI → 장치 → 검사 조립 → VisionSystem 초기화).
  - `Lazy<T>` 싱글톤이 스레드 안전한 이유와 싱글톤의 단점(숨은 의존성)을 설명할 수 있다.
  - 설정을 값이 아니라 `Func<T>`로 넘기는 이유를 설명할 수 있다.
  - MiniSight: `ConfigStore`(Lazy 싱글톤, JSON 로드)와 `Bootstrapper` 작성.
- **최소분량**: 2, 3, 4, 6
- **확장분량**: Kata의 "싱글톤 vs 주입" 비교, 7번 예외 설정 실습

## 2. 🧒 초1 비유

- **Composition Root = 레고 조립 설명서 펼친 책상**. 블록(클래스)들은 서로를 어떻게 만드는지 모른다. 책상 위에서 한 사람(부트스트래퍼)만 블록을 끼운다. 다른 곳에서 몰래 블록을 만들면 설명서와 실제 모양이 달라진다.
- **Singleton = 학교 방송실 마이크 하나**. 학교 전체에 하나만 있어야 한다(로그 파일 쓰는 사람이 둘이면 글자가 섞임). 하지만 누구나 몰래 마이크를 쓸 수 있으면 누가 썼는지 추적이 어렵다(숨은 의존성).
- **Func<T> 설정 = 냉장고 문 열어 보기**. "어제 우유 있었어"(값 복사)라고 기억하는 대신, 필요할 때마다 냉장고를 열어 본다(`() => 현재 설정`). 엄마가 새 우유를 넣으면(설정 저장) 다음 번에 바로 보인다.

## 3. OmniSight 지도

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 비동기 부팅 | `OmniSight/Application/ApplicationBootstrapper.cs:45-54` | `Task.Run(() => Initialize(...))` — 스플래시가 멈추지 않게 |
| 부팅 본체 | `OmniSight/Application/ApplicationBootstrapper.cs:56-261` | 단계별 `Report(progress, "n/6 ...")` |
| AI 동시성 설정 | `OmniSight/Application/ApplicationBootstrapper.cs:151-152` | 입력 수만큼 엔진 풀 (Day18 복선) |
| 장치 생성 | `OmniSight/Application/ApplicationBootstrapper.cs:177` | `DeviceFactory.Create` |
| 실패해도 계속 | `OmniSight/Application/ApplicationBootstrapper.cs:179-185` | 문제를 목록에 쌓고 빈 DeviceManager로 **설정 화면은 열리게** (Null Object) |
| 검사 조립 | `OmniSight/Application/ApplicationBootstrapper.cs:200` | `LegacyOcrInspectionFactory.Create` |
| 소유권 이전 | `OmniSight/Application/ApplicationBootstrapper.cs:210-218` | `devices = null;` "이제 정리는 VisionSystem이" |
| 실패 정리 | `OmniSight/Application/ApplicationBootstrapper.cs:253-260` | `_initialized`가 false면 만든 것 해제 |
| 검사 조립 공장 | `OmniSight/Application/Inspection/LegacyOcrInspectionFactory.cs:19-90` | 부품 생성 → `new InspectionRuntime(...)`, 실패 시 부분 정리 |
| Func 설정 주입 | `OmniSight/Application/Inspection/LegacyOcrInspectionFactory.cs:58` | `() => DataManager.Instance.RecipeRule.GetCurrentReference()` |
| Lazy 싱글톤 | `OmniSight/Configuration/DataManager.cs:18-23` | private 생성자 + `Lazy<DataManager>` |
| 설정 제공자 목록 | `OmniSight/Configuration/DataManager.cs:28-38` | 파일 종류마다 `JsonConfigProvider<T>` |

**집에서 Claude에게:**
```text
ApplicationBootstrapper.cs 56~261줄을 단계별로 잘라서 레고 책상 비유로 설명해줘.
특히 210~218줄 "devices = null" 과 253~260줄 finally가 어떻게 짝을 이루는지,
LegacyOcrInspectionFactory.cs 58줄에서 설정을 Func로 넘기는 이유를 알려줘.
```

## 4. 왜 이렇게 썼나

### 4.1 부팅 흐름

```text
StartupForm(스플래시) ── await ──▶ Bootstrapper.InitializeAsync  (Task.Run: 백그라운드)
  1/6 경로·로그          PathManager.Initialize
  2/6 설정 로드          DataManager.InitializeFixedData...
  3/6 AI 엔진            AiEngineRegistry.Shared.Configure...   (실패해도 기록 후 계속)
  4/6 장치 객체 생성      DeviceFactory.Create                     (실패하면 빈 DeviceManager)
      검사 조립          LegacyOcrInspectionFactory.Create
      총괄 객체          new VisionSystem(devices, runtime)  → devices = null (소유권 이전)
  5/6 장치 초기화         visionSystem.InitializeForConfiguration
  6/6 완료               문제가 있으면 "검사 시작 차단" 상태로 설정 화면만 허용
```

### 4.2 결정과 이유

| 결정 | 이유 |
|---|---|
| 조립 지점 한 곳 | 구체 타입을 아는 곳이 한 곳. 다른 현장은 **조립 코드만** 바꾸면 된다 (`LegacyOcrInspectionFactory` 주석: 다른 검사기는 별도 조립) |
| 부분 실패를 "이슈 목록"으로 | 장비 현장에서는 카메라 하나가 죽어도 **설정 화면은 열려야** 고칠 수 있다. 대신 검사 시작은 막는다 |
| 생성 후 `devices = null` | finally의 실패 정리 코드가 **이미 VisionSystem에 넘긴 장치를 또 Dispose**하지 않게. "로컬 변수를 null로 = 소유권을 넘겼다"는 관용구 |
| `Lazy<T>` 싱글톤 | 기본 모드(`ExecutionAndPublication`)가 스레드 안전 + 처음 쓸 때 생성 |
| `Func<T>`로 설정 전달 | 운전 중 레시피를 저장하면 **다음 프레임부터** 새 값. 클래스는 `DataManager`를 몰라도 됨 → 테스트에서 `() => 테스트설정` 주입 가능 |

### 4.3 싱글톤의 그림자 (면접 단골)

- `DataManager.Instance`를 아무 데서나 부르면 클래스 생성자만 봐서는 의존성을 알 수 없다(숨은 의존성).
- 테스트에서 상태가 전역으로 남아 테스트끼리 간섭한다.
- OmniSight는 그래서 **조립 지점에서만 싱글톤을 읽고**, 부품에는 `Func<T>`로 넘긴다. 실제 코드엔 아직 UI 등에서 직접 `Instance`를 부르는 곳이 남아 있다 → 리팩토링 후보.
- `AiEngineRegistry`는 `Shared` 싱글톤이지만 **생성자가 public**이라 테스트용 인스턴스를 따로 만들 수 있다. 좋은 절충안.

### 4.4 왜 DI 컨테이너(Autofac 등)를 안 썼나

장치 수가 적고 수명 규칙(시작 순서, 역순 정지, 실패 정리)이 까다로워서 **손으로 조립(Pure DI)** 하는 게 더 읽기 쉽고 디버깅이 쉽다. 컨테이너는 등록이 수백 개가 되거나 웹처럼 요청 범위 수명이 필요할 때 효과가 크다.

## 5. Kata: 레고 책상과 냉장고

```csharp
// KATA: Day05
using System;

namespace Katas.Day05
{
    public sealed class Settings { public int Threshold { get; set; } }

    // 싱글톤 (Lazy: 스레드 안전 + 지연 생성)
    public sealed class SettingsStore
    {
        private static readonly Lazy<SettingsStore> _instance =
            new Lazy<SettingsStore>(() => new SettingsStore());
        public static SettingsStore Instance { get { return _instance.Value; } }

        private Settings _current = new Settings { Threshold = 100 };
        private readonly object _sync = new object();
        private SettingsStore() { Console.WriteLine("SettingsStore 생성 (딱 한 번)"); }

        public Settings Current { get { lock (_sync) { return _current; } } }
        public void Replace(Settings next) { lock (_sync) { _current = next; } }
    }

    // 부품: 싱글톤을 모른다. "냉장고 여는 방법"만 받는다.
    public sealed class Inspector
    {
        private readonly Func<Settings> _settings;
        public Inspector(Func<Settings> settings) { _settings = settings ?? throw new ArgumentNullException(nameof(settings)); }
        public string Inspect(int brightness)
        {
            int th = _settings().Threshold;     // 매번 최신 값
            return brightness >= th ? "OK" : "NG(th=" + th + ")";
        }
    }

    // 나쁜 예: 생성 시점 값을 복사해 버림
    public sealed class StaleInspector
    {
        private readonly int _threshold;
        public StaleInspector(Settings settings) { _threshold = settings.Threshold; }
        public string Inspect(int brightness) { return brightness >= _threshold ? "OK" : "NG(th=" + _threshold + ")"; }
    }

    // Composition Root: 여기서만 new
    public static class KataDay05
    {
        public static void Run()
        {
            var good = new Inspector(() => SettingsStore.Instance.Current);
            var stale = new StaleInspector(SettingsStore.Instance.Current);
            var test = new Inspector(() => new Settings { Threshold = 10 });   // 테스트 주입

            Console.WriteLine(good.Inspect(120) + " / " + stale.Inspect(120));
            SettingsStore.Instance.Replace(new Settings { Threshold = 150 }); // 운전 중 레시피 저장
            Console.WriteLine(good.Inspect(120) + " / " + stale.Inspect(120)); // good만 새 값 반영
            Console.WriteLine("테스트용: " + test.Inspect(20));
        }
    }
}
```

생각해 볼 것: `Replace`가 객체를 **통째로 바꾸고** 기존 객체를 고치지 않는 이유는? (힌트: 다른 스레드가 반쯤 바뀐 설정을 읽지 않게. Day23의 Clone 후 Commit과 연결)

## 6. MiniSight 오늘 단계

1. NuGet `Newtonsoft.Json` 설치.
2. `Config/SystemConfig.cs`: 카메라 목록(`Name`, `Kind`, `Folder`, `IntervalMs`), `ImageSaveRoot`.
3. `Config/ConfigStore.cs`: Lazy 싱글톤. `Load(path)`(없으면 기본값으로 생성 후 저장), `Current`(lock), `Replace(next)`. 원자적 저장은 Day23에서.
4. `App/Bootstrapper.cs : IDisposable`
   - `Initialize(IProgress<string> progress)`: 설정 로드 → `CameraFactory`로 카메라 목록 생성 → (Day06의 `DeviceManager`, `VisionSystem` 자리) → 실패하면 만든 것 역순 Dispose 후 rethrow.
   - `InitializeAsync`는 `Task.Run(() => Initialize(progress))`.
   - 이슈 목록 `List<string> Issues` — 카메라 하나 실패 시 이슈로 남기고 계속.
5. `Program.cs`의 PHASE 1에서 `using (var boot = new Bootstrapper()) { boot.Initialize(...); ... }`.

## 7. VS2022 디버깅 실습

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | 설정 JSON의 카메라 폴더를 없는 경로로 바꾸고 실행 | 이슈 목록에 쌓이는지 |
| 2 | **예외 설정(Ctrl+Alt+E)** → `Common Language Runtime Exceptions` 전체 체크 | `try` 안에서 잡히는 예외도 throw 순간에 멈춤(**first-chance**). 출력 창에 "예외 발생: ..." 줄 |
| 3 | 다시 체크 해제하고, `DirectoryNotFoundException`만 체크 | 원하는 예외만 |
| 4 | `Lazy` 생성자 람다에 중단점 → 두 스레드에서 `Instance`를 동시에 호출하는 테스트 코드 작성 | 중단점이 **한 번만** 걸리는지 |
| 5 | 조사식에 `SettingsStore.Instance` 대신 `_instance.IsValueCreated` | 생성 전/후 |

## 8. 셀프 체크

1. Composition Root가 없으면 생기는 문제 3가지.
<details><summary>답</summary>① 구체 타입이 여러 곳에 흩어져 교체가 어렵다. ② 생성 순서·실패 정리가 제각각이라 부분 생성된 자원이 샌다. ③ 테스트에서 가짜 부품을 주입할 수 없다.</details>

2. `Lazy<T>` 기본 생성자의 스레드 안전 모드는?
<details><summary>답</summary>`LazyThreadSafetyMode.ExecutionAndPublication`. 여러 스레드가 동시에 Value를 요청해도 팩토리는 한 번만 실행되고 모두 같은 인스턴스를 받는다.</details>

3. 설정을 `Func<T>`로 넘길 때의 주의점은?
<details><summary>답</summary>호출할 때마다 최신 참조를 읽으므로 한 프레임 안에서 여러 번 부르면 중간에 설정이 바뀌어 값이 섞일 수 있다. 프레임 시작 시 한 번 읽어 지역 변수로 쓰는 것이 안전하다. 또 반환된 객체를 수정하면 안 된다(공유 참조).</details>

4. 부팅 중 장치 하나가 실패해도 프로그램을 계속 띄우는 이유는? 위험은?
<details><summary>답</summary>현장에서 설정 화면으로 원인을 고칠 수 있어야 하기 때문이다. 위험은 반쯤 초기화된 상태로 검사가 시작되는 것이므로, 이슈가 있으면 Start를 차단하는 규칙이 반드시 함께 있어야 한다.</details>

5. 왜 이 프로젝트는 DI 컨테이너 대신 손 조립을 택했다고 보나?
<details><summary>답</summary>객체 수가 적고, 시작/정지 순서와 실패 롤백 같은 수명 규칙을 명시적으로 보여주는 것이 중요하다. 컨테이너는 이 순서를 숨기고 런타임 오류로 바꾼다. .NET Framework WinForms 환경에서 의존성 추가도 부담이다.</details>

## 9. 블로그 포인트

- 제목 예: `[설계] new는 한 곳에서만: Composition Root와 Func<T> 설정 주입`
- Kata의 good vs stale 결과 비교가 좋은 글감.
- ❌ 회사 기본 데이터 경로(드라이브·폴더명)는 고객사명이 들어가 있으니 절대 쓰지 않는다.
