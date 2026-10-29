# MiniSight 설계서 (집에서 만드는 축소판 OmniSight)

> MiniSight는 OmniSight의 **구조와 이유**를 그대로 따르되, 코드는 처음부터 내 손으로 쓴다.
> 카메라·PLC·AI SDK가 없으니 전부 **가짜(Fake)/시뮬레이션**으로 만든다. 그래서 오히려 패턴이 더 잘 보인다.

## 1. 환경

| 항목 | 값 |
|---|---|
| IDE | Visual Studio 2022 (Community로 충분) |
| 워크로드 | ".NET 데스크톱 개발" + 개별 구성 요소 ".NET Framework 4.7.2 targeting pack" |
| 프로젝트 템플릿 | **Windows Forms 앱(.NET Framework)** — "(.NET Framework)"가 붙은 것을 고른다. 그냥 "Windows Forms 앱"은 .NET 8이다 |
| 대상 프레임워크 | .NET Framework 4.7.2 |
| 언어 버전 | C# 7.3 (csproj에 `<LangVersion>7.3</LangVersion>` 명시) |
| 플랫폼 | x64 (구성 관리자에서 Any CPU → x64 새로 만들기). OpenCV 네이티브 DLL 때문 |
| NuGet | `OpenCvSharp4`, `OpenCvSharp4.runtime.win`, `OpenCvSharp4.Extensions`, `Newtonsoft.Json` (회사와 같은 계열. 버전은 최신 안정판) |
| 테스트 | MSTest 테스트 프로젝트(.NET Framework) — Day26 |

### C# 7.3에서 **못 쓰는** 문법 (집에서 습관적으로 쓰면 컴파일 에러)

| 금지 | 대신 |
|---|---|
| `using var x = ...;` | `using (var x = ...) { }` |
| `switch` 식 `x switch { ... }` | `switch` 문 |
| `record`, `init` | 생성자 + `get;` 전용 속성 |
| `x ??= y` | `if (x == null) x = y;` |
| 범위 `arr[1..3]`, `^1` | `Array.Copy`, `arr[arr.Length - 1]` |
| `new()` (대상 형식 new) | `new Foo()` |
| nullable 참조형 `string?` | 그냥 `string` |
| `await foreach`, `IAsyncEnumerable` | 루프 + `await` |
| `Channel<T>` | `BlockingCollection<T>` 또는 SemaphoreSlim 큐 (Day10~11) |

사용 **가능**: 튜플 `(bool ok, string msg)`, `out var`, 패턴 매칭 `is Foo f`, `when` 필터, 로컬 함수, `=>` 속성, 문자열 보간, `nameof`, `?.`, `??`, `throw` 식.

## 2. 솔루션 구조 (최종 모습)

```text
MiniSight.sln
├─ MiniSight/                       WinForms exe, x64
│  ├─ Program.cs                    Day01 단일 실행, 전역 예외
│  ├─ App/
│  │  ├─ Bootstrapper.cs            Day05 조립 지점 (new는 여기서만)
│  │  ├─ VisionSystem.cs            Day06 Facade + 상태 머신 + 이벤트
│  │  └─ HealthMonitor.cs           Day08 전용 스레드 + 취소 가능한 sleep
│  ├─ Devices/
│  │  ├─ Contracts.cs               Day02 IDevice, IImageSource, ImageFrame, DeviceState
│  │  ├─ DeviceBase.cs              Day03 Template Method
│  │  ├─ DeviceManager.cs           Day06 등록 순서 시작, 역순 정지, 롤백
│  │  ├─ Cameras/
│  │  │  ├─ ICameraDriver.cs        Day04 가짜 SDK 계약
│  │  │  ├─ FakeCameraDriver.cs     Day04 타이머로 프레임 콜백을 쏘는 가짜 SDK
│  │  │  ├─ DriverCameraAdapter.cs  Day04 ICameraDriver → IImageSource 어댑터
│  │  │  ├─ FolderCamera.cs         Day04 폴더 이미지 순환 시뮬레이션 카메라
│  │  │  ├─ CameraFactory.cs        Day04 설정 → 카메라 생성
│  │  │  └─ FrameQueue.cs           Day11 SemaphoreSlim 두 개 유한 큐
│  │  ├─ ShareFolderSource.cs       Day23 폴링 + 파일 준비 확인
│  │  └─ Plc/
│  │     ├─ PlcPacket.cs            Day22 MiniSight 자체 패킷 (CRLF 프레이밍)
│  │     └─ PlcClient.cs            Day22 송수신 루프, 하트비트, 재연결
│  ├─ Inspection/
│  │  ├─ Contracts.cs               Day13 IPreprocessor, IRuleEngine, IAiEngine, IResultReporter, IImageStore
│  │  ├─ InspectionPipeline.cs      Day13 입력 1개 = 작업자 1개
│  │  ├─ InspectionRuntime.cs       Day13 파이프라인 N개 시작/정지
│  │  ├─ OutputDispatcher.cs        Day10 보고 큐 + 이미지 큐 + 작업자 2개
│  │  ├─ CsvReporter.cs             Day10
│  │  ├─ PngImageStore.cs           Day10
│  │  ├─ RuleEngine.cs              Day17 Point/Line 게이지, 블랍
│  │  └─ FakeAiEngine.cs, AiEnginePool.cs   Day18
│  ├─ Vision/
│  │  ├─ Filters/IFilter.cs …       Day16 Strategy 필터들
│  │  └─ FilterChain.cs             Day16
│  ├─ Config/
│  │  ├─ JsonStore.cs               Day05(기본), Day23(원자적 저장)
│  │  ├─ SystemConfig.cs, RecipeConfig.cs
│  ├─ Logging/AsyncLogger.cs        Day24
│  └─ UI/
│     ├─ MainForm.cs                Day20
│     ├─ ImageCanvas.cs             Day19 줌·더블버퍼
│     └─ ControlExtensions.cs       Day19 RunOnUi(BeginInvoke)
├─ MiniSight.Katas/                 콘솔 exe. 매일 Kata를 KataDayNN.cs로 쌓는다
├─ MiniSight.Tests/                 MSTest (.NET Framework) Day26
└─ Tools/FakePlcServer/             콘솔 exe. TcpListener 가짜 PLC Day22
```

## 3. MiniSight ↔ OmniSight 대응표 (집에서 Claude에게 "이거 회사 코드에선 어디야?" 물어볼 때)

| MiniSight | OmniSight | 핵심 아이디어 |
|---|---|---|
| `Program.cs` | `OmniSight/Program.cs` | Mutex 단일 실행, 예외 처리기 3종 |
| `Bootstrapper` | `OmniSight/Application/ApplicationBootstrapper.cs`, `Application/Inspection/LegacyOcrInspectionFactory.cs` | 조립 지점, 실패 시 부분 정리 |
| `VisionSystem` | `OmniSight/Application/VisionSystem.cs` | Facade, 상태 enum 3개, `ChangeState` |
| `Contracts.cs` | `OmniSight/Devices/DeviceContracts.cs` | IDevice, IImageSourceDevice, ImageFrame |
| `DeviceBase` | `OmniSight/Devices/DeviceBase.cs` | Template Method + 상태 |
| `DeviceManager` | `OmniSight/Devices/DeviceManager.cs` | 순서 시작, 역순 정지, 롤백 |
| `CameraFactory` | `OmniSight/Devices/DeviceFactory.cs`, `Devices/Cameras/CameraAdapterFactory.cs` | Simple Factory |
| `ICameraDriver`, `DriverCameraAdapter` | `OmniSight/Devices/Cameras/ICameraDriver.cs`, `CallbackCameraAdapterBase.cs` | Adapter + driver factory |
| `FolderCamera` | `OmniSight/Devices/Cameras/SimulationCameraAdapter.cs` | 시뮬레이션 입력 |
| `FrameQueue` | `OmniSight/Devices/Cameras/CameraFrameQueue.cs` | SemaphoreSlim 2개 + Complete |
| `InspectionPipeline` | `OmniSight/Application/Inspection/InspectionPipeline.cs` | 입력별 작업자, finally Dispose |
| `InspectionRuntime` | `OmniSight/Application/Inspection/InspectionRuntime.cs` | 파이프라인 N개, 시작 롤백 |
| `OutputDispatcher` | `OmniSight/Application/Inspection/InspectionOutputDispatcher.cs` | 보고 64 / 이미지 4 큐 |
| `FilterChain` | `OmniSight/Vision/Processing/RecipeImageProcessor.cs` | 필터 → 변환 순서 (회사는 switch, 우리는 Strategy) |
| `RuleEngine` | `OmniSight/Application/Inspection/RecipeRuleInspectionEngine.cs` | 캘리퍼, PCA 라인, 블랍 |
| `AiEnginePool` | `OmniSight/Vision/AiEngine/AiEngineRegistry.cs` | Object Pool, RWLock 교체 |
| `FakeAiEngine` | `OmniSight/Vision/AiEngine/AiEngineBase.cs` | Dispose(bool) + finalizer |
| `PlcClient` | `OmniSight/Devices/LegacyOcrSocketDevice.cs`, `Devices/LegacySocketProtocol.cs` | 송수신 루프, 하트비트, 연결 세대 |
| `ShareFolderSource` | `OmniSight/Devices/SharedFolderAdapter.cs` | 폴링, 파일 준비 확인 |
| `JsonStore` | `OmniSight/Configuration/JsonConfigProvider.cs` | .tmp + Replace + .bak, Clone |
| `AsyncLogger` | `OmniSight/Logging/LogManager.cs` | 유한 큐, drop 카운트, flush 장벽 |
| `ImageCanvas` | `OmniSight/UI/Controls/ZoomableImageCanvas.cs` | 줌, 더블버퍼 |
| `ControlExtensions` | `OmniSight/UI/MainForm.cs` `RunOnUiThread` | BeginInvoke |

## 4. 핵심 계약 (Day02에 직접 타이핑. 이후 모든 Day가 이걸 쓴다)

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiniSight.Devices
{
    public enum DeviceState { Created, Initializing, Ready, Running, Stopped, Faulted, Disposed }

    public interface IDevice : IDisposable
    {
        string Name { get; }
        DeviceState State { get; }
        void Initialize(CancellationToken cancellationToken);
        void Start(CancellationToken cancellationToken);
        void Stop();
    }

    public interface IImageSource : IDevice
    {
        event EventHandler<ImageFrame> FrameCaptured;          // 미리보기용 (핸들러 안에서만 유효)
        Task<ImageFrame> WaitFrameAsync(CancellationToken cancellationToken); // 검사용 (받은 쪽이 Dispose)
    }
}
```

`ImageFrame`은 Day02에서 OpenCvSharp `Mat`을 소유하는 버전으로 만든다.

## 5. 단계별 완성도 (각 주말에 이 상태가 되어야 한다)

| 시점 | 실행하면 보이는 것 |
|---|---|
| 1주차 토요일 | 콘솔: `Initialize → Ready → Start → Running → Stop → Ready` 상태 로그, FolderCamera가 이미지 이름 출력 |
| 2주차 토요일 | 콘솔: 카메라 2대 × 파이프라인 2개, 결과 CSV와 PNG 저장, Stop 시 큐가 비워지는 로그. 메모리가 일정 |
| 3주차 토요일 | WinForms: Start/Stop 버튼(화면 안 멈춤), 라이브 화면 10fps, 필터 적용 결과, 게이지 선 표시, 결과 그리드 |
| 4주차 토요일 | FakePlcServer에서 Start 명령 → MiniSight 운전 → 결과 패킷 수신. 설정 저장 중 강제 종료해도 설정 안 깨짐. 테스트 전부 통과. 새 카메라 종류를 코어 수정 없이 추가 |

## 6. 테스트용 이미지 준비

- `C:\MiniSight\Images\Cam1`, `Cam2` 폴더에 PNG 20장씩 넣는다.
- 인터넷 이미지 대신 Day15 Kata에서 **OpenCV로 합성 이미지(흰 바탕에 검은 사각형, 위치를 조금씩 이동)** 를 만들어 쓴다. 게이지 결과를 정답과 비교할 수 있어서 더 좋다.
