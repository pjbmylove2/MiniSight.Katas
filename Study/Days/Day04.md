# Day 04 (목, 2h) · Factory와 Adapter

## 1. 오늘 목표

- **완료 기준**
  - "설정값 → 어떤 카메라 객체를 만들지"를 **한 곳**에서 결정하는 이유를 말할 수 있다 (Simple Factory).
  - 벤더 SDK마다 다른 API를 같은 `IImageSourceDevice` 모양으로 맞추는 구조를 그림으로 그렸다 (Adapter).
  - `Func<ICameraDriver>`를 생성자로 받는 이유(재연결 때마다 새 드라이버)를 설명할 수 있다.
  - MiniSight: `ICameraDriver`, `FakeCameraDriver`, `DriverCameraAdapter`, `FolderCamera`, `CameraFactory` 작성.
- **최소분량**: 2, 3, 4, 6의 ICameraDriver·FakeCameraDriver·CameraFactory
- **확장분량**: FolderCamera, 디버깅 실습 4~5

## 2. 🧒 초1 비유

- **Adapter = 해외여행용 돼지코**. 한국 플러그(검사기가 원하는 모양)를 미국 콘센트(Basler SDK), 일본 콘센트(Hikvision SDK)에 꽂으려면 돼지코가 필요하다. 돼지코 안에서 모양만 바꿔 준다. 검사기는 돼지코가 있는지도 모른다.
- **Factory = 주문 받는 빵집 카운터**. 손님(부트스트래퍼)은 "크림빵 주세요"(설정: `VendorType = BASLER`)라고만 말한다. 빵을 어떻게 굽는지는 카운터 뒤 주방이 안다. 새 빵이 생겨도 손님은 메뉴 이름만 알면 된다.

## 3. OmniSight 지도

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 장치 조립 팩토리 | `OmniSight/Devices/DeviceFactory.cs:15-88` | 설비 타입으로 카메라 모드/공유폴더 모드 결정, 실패 시 **역순 Dispose**(L75-85) |
| `yield return` 생성 | `OmniSight/Devices/DeviceFactory.cs:99-120` | UNUSED 노드 건너뛰고 인덱스 유지 |
| 카메라 팩토리 | `OmniSight/Devices/Cameras/CameraAdapterFactory.cs:9-31` | `switch (config.VendorType)` |
| SDK 계약 | `OmniSight/Devices/Cameras/ICameraDriver.cs` | `FrameReady(Mat, string, long)` — **Mat 소유권을 넘긴다** |
| 12줄짜리 어댑터 | `OmniSight/Devices/Cameras/HikvisionCameraAdapter.cs` | 생성자에서 `() => new HikvisionCameraDriver()`만 넘김 |
| 어댑터 공통부 | `OmniSight/Devices/Cameras/CallbackCameraAdapterBase.cs:41-47` | `Func<ICameraDriver> driverFactory` |
| 재연결 시 새 드라이버 | `OmniSight/Devices/Cameras/CallbackCameraAdapterBase.cs:78-99` | 이전 드라이버 이벤트 해제 → Dispose → 새로 생성 |
| 콜백 → ImageFrame → 큐 | `OmniSight/Devices/Cameras/CallbackCameraAdapterBase.cs:409-444` | `ownedImage = null`, `frame = null`로 소유권 이동, finally에서 남은 것만 해제 |
| 예외: Basler | `OmniSight/Devices/Cameras/BaslerCameraAdapter.cs` | ICameraDriver를 안 쓰고 DeviceBase 직접 상속 (역사적 이유, 중복 로직) |

**집에서 Claude에게:**
```text
CameraAdapterFactory.cs, ICameraDriver.cs, HikvisionCameraAdapter.cs를 보여주고
돼지코(Adapter)와 빵집(Factory) 비유로 설명해줘. 그다음 CallbackCameraAdapterBase.cs 78~99줄과
409~444줄을 보여주면서 "이벤트 구독 해제를 왜 먼저 하는지", "ownedImage = null 이 왜 필요한지" 알려줘.
```

## 4. 왜 이렇게 썼나

### 4.1 구조 그림

```text
설정 JSON ─▶ CameraAdapterFactory.Create(config)
                 │ switch VendorType
                 ├─ BASLER     → BaslerCameraAdapter (DeviceBase 직접)
                 ├─ HIKVISION  → HikvisionCameraAdapter ─┐
                 ├─ HUARAY     → HuarayCameraAdapter    ─┤ CallbackCameraAdapterBase (공통: 큐, 상태, 미리보기)
                 └─ SIMULATION → SimulationCameraAdapter │        │ Func<ICameraDriver>
                                                         │        ▼
                                                         │  HikvisionCameraDriver : ICameraDriver (SDK 직접 호출)
검사기는 여기만 본다 ─▶ IImageSourceDevice.WaitFrameAsync ◀─┘
```

### 4.2 결정과 이유

| 결정 | 이유 | 대안과 비교 |
|---|---|---|
| 팩토리를 `static` + `switch` | 종류가 몇 개 안 되고 설정 enum과 1:1. 가장 단순 | 리플렉션/플러그인 로딩: 유연하지만 디버깅·배포 복잡. 현장 장비엔 과함 |
| 팩토리 **한 곳**에서만 `new` | 새 벤더 추가 시 고칠 곳이 한 곳. Program/UI는 모름 | 여기저기 `new BaslerCamera()` → 벤더 바꿀 때 전체 검색 |
| 생성 실패 시 역순 Dispose | 3번째 장치에서 실패하면 1·2번째 장치가 핸들을 잡은 채로 남음 | 정리 안 하면 재시작 시 "카메라 사용 중" 오류 |
| `Func<ICameraDriver>` 주입 | **재연결할 때마다 새 SDK 객체**가 필요. 객체 하나 대신 "만드는 방법"을 받음. 테스트에선 가짜 드라이버를 주입 | 드라이버 인스턴스 하나 주입: 재연결 불가 |
| 드라이버 → 어댑터로 **Mat 소유권 이동** | SDK 버퍼는 다음 콜백에서 재사용됨. 드라이버가 복사한 Mat을 넘기면 어댑터가 책임짐 | SDK 버퍼 포인터 그대로 전달: 검사 중에 다음 사진으로 덮임 |
| 이벤트 해제 후 Dispose | Dispose 도중/이후 SDK 콜백이 와도 이미 죽은 어댑터 메서드가 안 불리게 | 순서 반대면 해제된 객체로 콜백 → 크래시 |

### 4.3 약점

- Basler는 공통 베이스를 안 써서 **같은 로직이 두 벌**(라인 스피드 게이트, 큐, 트리거 루프). 버그를 한쪽만 고칠 위험. 리팩토링 후보다.
- `switch` 팩토리는 벤더가 20개쯤 되면 커진다. 그때는 `Dictionary<VendorType, Func<...>>` 등록 방식으로 바꿀 수 있다.

## 5. Kata: 돼지코와 빵집

```csharp
// KATA: Day04
using System;
using System.Collections.Generic;

namespace Katas.Day04
{
    // 우리 집(검사기)이 원하는 모양
    public interface IKoreanPlug { void PowerOn(int volt); }

    // 외국 SDK들: 우리가 고칠 수 없는 코드라고 가정
    public sealed class UsaSocketSdk { public void Connect110V() { Console.WriteLine("USA 110V 연결"); } }
    public sealed class JapanSocketSdk { public void Start(double hz) { Console.WriteLine("Japan " + hz + "Hz 시작"); } }

    // 어댑터: 모양만 바꾼다. 로직을 넣지 않는다.
    public sealed class UsaAdapter : IKoreanPlug
    {
        private readonly UsaSocketSdk _sdk;
        public UsaAdapter(UsaSocketSdk sdk) { _sdk = sdk ?? throw new ArgumentNullException(nameof(sdk)); }
        public void PowerOn(int volt) { _sdk.Connect110V(); }
    }

    public sealed class JapanAdapter : IKoreanPlug
    {
        private readonly Func<JapanSocketSdk> _sdkFactory;   // 재연결마다 새 SDK
        private JapanSocketSdk _sdk;
        public JapanAdapter(Func<JapanSocketSdk> sdkFactory) { _sdkFactory = sdkFactory; }
        public void PowerOn(int volt)
        {
            _sdk = _sdkFactory();
            _sdk.Start(60.0);
        }
    }

    public enum Country { Usa, Japan }

    // 빵집 카운터: 메뉴 이름만 받고 어떻게 만드는지는 여기만 안다
    public static class PlugFactory
    {
        public static IKoreanPlug Create(Country country)
        {
            switch (country)
            {
                case Country.Usa: return new UsaAdapter(new UsaSocketSdk());
                case Country.Japan: return new JapanAdapter(() => new JapanSocketSdk());
                default: throw new NotSupportedException("지원하지 않는 국가: " + country);
            }
        }

        // 여러 개 만들다 실패하면 이미 만든 것을 역순으로 정리
        public static List<IKoreanPlug> CreateAll(IEnumerable<Country> countries)
        {
            var created = new List<IKoreanPlug>();
            try
            {
                foreach (Country c in countries) created.Add(Create(c));
                return created;
            }
            catch
            {
                for (int i = created.Count - 1; i >= 0; i--)
                {
                    var disposable = created[i] as IDisposable;
                    if (disposable != null) disposable.Dispose();
                }
                throw;
            }
        }
    }

    public static class KataDay04
    {
        public static void Run()
        {
            foreach (IKoreanPlug plug in PlugFactory.CreateAll(new[] { Country.Usa, Country.Japan }))
            {
                plug.PowerOn(220);   // 호출하는 쪽은 나라를 모른다
            }
        }
    }
}
```

## 6. MiniSight 오늘 단계

### 6.1 `Devices/Cameras/ICameraDriver.cs` (가짜 SDK의 모양)

```csharp
public interface ICameraDriver : IDisposable
{
    event Action<Mat, long> FrameReady;   // Mat 소유권을 받는 쪽에 넘긴다
    void Open(string cameraName);
    void Start();
    void Stop();
}
```

### 6.2 `FakeCameraDriver.cs` (직접 작성)

- `System.Threading.Timer`로 `intervalMs`마다 콜백.
- 콜백에서 `new Mat(480, 640, MatType.CV_8UC1, Scalar.All(0))` 위에 `Cv2.PutText`로 프레임 번호를 쓰고 `FrameReady?.Invoke(mat, id)`.
- **구독자가 없으면** 만든 Mat을 직접 Dispose (소유권을 받을 사람이 없으니까).
- `Stop()`은 타이머를 멈추고, `Dispose()`는 타이머 Dispose.

### 6.3 `DriverCameraAdapter.cs : DeviceBase, IImageSource`

- 생성자: `(string name, Func<ICameraDriver> driverFactory)`.
- `OnInitialize`: 드라이버 생성 → `FrameReady += OnFrame` → `Open`.
- `OnStart/OnStop`: 드라이버 Start/Stop.
- `OnFrame(Mat mat, long id)`: `ImageFrame` 생성(소유권 이동) → `FrameCaptured` 이벤트 → 내부 큐에 Add. 실패하면 finally에서 Dispose.
- 큐는 오늘은 임시로 `BlockingCollection<ImageFrame>(8)` 사용, `WaitFrameAsync`는 `Task.Run(() => _queue.Take(ct), ct)`로 임시 구현. **Day11에서 FrameQueue로 교체**한다(이 임시 구현이 왜 아쉬운지 Day11에서 배움).
- `OnDispose`: 이벤트 해제 → 드라이버 Dispose → 큐에 남은 프레임 Dispose.

### 6.4 `FolderCamera.cs : DeviceBase, IImageSource`

- 폴더의 PNG 목록을 `Directory.GetFiles`로 읽어 순환하며 `Cv2.ImRead`. 나머지는 6.3과 같은 모양.

### 6.5 `CameraFactory.cs`

```csharp
public enum CameraKind { Fake, Folder }
public static IImageSource Create(CameraKind kind, string name, string folderOrNull, int intervalMs)
// switch로 생성. 알 수 없는 값은 NotSupportedException
```

## 7. VS2022 디버깅 실습

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | `CameraFactory.Create(...)` 호출 줄에서 멈춘 뒤 **직접 실행 창(Immediate, Ctrl+Alt+I)** 열기 | |
| 2 | Immediate에 `CameraFactory.Create(CameraKind.Fake, "T", null, 100).Name` 입력 | 코드를 고치지 않고 메서드를 즉시 실행 |
| 3 | `new DriverCameraAdapter("A", () => new FakeCameraDriver(50))` 처럼 한 줄에 메서드 호출이 여러 개인 줄에서 우클릭 → **특정 항목으로 한 단계씩 코드 실행(Step Into Specific)** | 람다/생성자 중 원하는 것만 골라 들어감 |
| 4 | `OnFrame` 에 중단점 → 디버그 → 창 → **스레드(Ctrl+Alt+H)** | 타이머 콜백은 **작업자 스레드**에서 온다 (UI 스레드 아님) — Day08·19의 복선 |
| 5 | 이벤트 해제(`-=`)를 일부러 빼고, Dispose 후에 FakeCameraDriver 타이머를 1번 더 돌게 해 본다 | 죽은 어댑터로 콜백이 들어오는 걸 확인 → 원래대로 복구 |

## 8. 셀프 체크

1. Simple Factory, Factory Method, Abstract Factory 차이를 한 줄씩.
<details><summary>답</summary>Simple Factory: 정적 메서드 하나가 조건에 따라 구체 객체를 만든다(OmniSight CameraAdapterFactory). Factory Method: 부모가 "만드는 메서드"를 정의하고 자식이 어떤 객체를 만들지 결정(또는 Func로 주입하는 변형 — driverFactory). Abstract Factory: 관련 객체 가족(카메라+조명+PLC)을 한 세트로 만드는 인터페이스.</details>

2. 어댑터 안에 검사 로직을 넣으면 왜 안 되나?
<details><summary>답</summary>어댑터의 책임은 인터페이스 모양 변환이다. 로직이 들어가면 벤더마다 검사 결과가 달라지고, 벤더를 바꿀 때 로직까지 옮겨야 한다. 공통 로직은 베이스 클래스나 파이프라인에 둔다.</details>

3. 생성자에 드라이버 객체 대신 `Func<ICameraDriver>`를 받는 이유는?
<details><summary>답</summary>SDK 객체는 연결이 끊기면 재사용할 수 없는 경우가 많아 재연결 때마다 새로 만들어야 한다. 또한 테스트에서 가짜 드라이버를 주입할 수 있고, 생성 시점을 어댑터가 제어할 수 있다.</details>

4. SDK 콜백에서 받은 버퍼를 복사해야 하는 이유는?
<details><summary>답</summary>SDK는 콜백이 끝나면 같은 버퍼를 다음 영상에 재사용한다. 비동기 검사 중에 버퍼가 덮이면 검사·저장 이미지가 다른 프레임과 섞인다. 앱 소유 Mat으로 복사해 독립 수명을 만든다.</details>

5. DeviceFactory가 실패 시 역순으로 Dispose하는 이유는?
<details><summary>답</summary>나중에 만든 장치가 먼저 만든 장치에 의존할 수 있다(카메라가 소켓 스냅샷을 참조 등). 의존하는 쪽부터 정리하는 것이 안전하다. 또 부분 생성된 장치가 핸들을 잡고 있으면 재시작 시 장치 점유 오류가 난다.</details>

## 9. 블로그 포인트

- 제목 예: `[디자인패턴] 카메라 SDK를 바꿔도 검사 코드는 그대로: Adapter + Factory`
- 돼지코 비유, Kata, MiniSight FakeCameraDriver의 "구독자가 없으면 내가 Dispose" 규칙.
- ❌ 회사가 쓰는 벤더 조합·모델명은 쓰지 않는다 (일반적인 "벤더 A/B"로).
