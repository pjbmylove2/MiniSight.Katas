# OmniSight 디자인 패턴 지도

> 줄 번호는 커밋 `93aaca9` 기준. 바뀌었으면 Claude에게 "심볼 이름으로 다시 찾아줘".

| 패턴 | 한 줄 뜻 | OmniSight 위치 | 왜 여기서 | 배운 Day |
|---|---|---|---|---|
| Composition Root (Pure DI) | 객체 조립은 시작 지점 한 곳 | `OmniSight/Application/ApplicationBootstrapper.cs:56-261`, `OmniSight/Application/Inspection/LegacyOcrInspectionFactory.cs:19-90` | 구체 타입을 아는 곳을 한 곳으로. 현장별 조립 교체 | 05 |
| Simple Factory | 조건 → 구체 객체 | `OmniSight/Devices/DeviceFactory.cs:15-88`, `OmniSight/Devices/Cameras/CameraAdapterFactory.cs:9-31` | 벤더·설비 타입 분기를 한 곳에 | 04 |
| Factory Method (Func 주입) | "만드는 방법"을 주입 | `OmniSight/Devices/Cameras/CallbackCameraAdapterBase.cs:41-47,90` | 재연결마다 새 SDK 객체, 테스트용 가짜 | 04 |
| Adapter | SDK API → 우리 계약 | `OmniSight/Devices/Cameras/HikvisionCameraAdapter.cs`, `HikvisionCameraDriver.cs`, `OmniSight/Devices/SharedFolderAdapter.cs` | 파이프라인은 `IImageSourceDevice`만 앎 | 04 |
| Template Method | 순서 고정 + 빈칸 hook | `OmniSight/Devices/DeviceBase.cs:19-115`, `CallbackCameraAdapterBase.cs`, `OmniSight/Vision/AiEngine/AiEngineBase.cs`, `OmniSight/UI/Controls/BaseUserControl.cs:83-110` | 상태 전이·해제 규칙을 한 곳에 | 03 |
| Strategy (DI) | 알고리즘을 인터페이스로 교체 | `OmniSight/Application/Inspection/InspectionContracts.cs`, `InspectionPipeline.cs:23-52` | 전처리·룰·AI·보고·저장이 현장마다 다름 | 02, 13 |
| (대조) switch 분기 | 종류별 분기를 한 함수에 | `OmniSight/Vision/Processing/RecipeImageProcessor.cs:76-91` | 구 프로그램과 1:1 비교 쉬움 | 16 |
| Null Object | "아무것도 안 함" 객체 | `OmniSight/Application/Inspection/AlwaysPassRuleInspectionEngine.cs`, 빈 `DeviceManager` (`ApplicationBootstrapper.cs:185`) | null 검사 제거, 실패해도 설정 화면 | 05 |
| Facade | 여러 하위 시스템 → 단순 API | `OmniSight/Application/VisionSystem.cs:55-79`, `OmniSight/Devices/DeviceManager.cs` | UI는 VisionSystem 하나만 | 06 |
| State Machine (enum) | 상태 + 전이 규칙 | `OmniSight/Application/VisionSystem.cs:16-35,380-528`, `OmniSight/Devices/DeviceContracts.cs:8-17` | 시작 가능 여부가 안전 문제 | 03, 06 |
| Observer | 이벤트 방송 | `VisionSystem.StateChanged`, `InspectionPipeline.InspectionCompleted`, `ICameraDriver.FrameReady` | 상태·결과를 UI로. 구독자 격리 | 06 |
| Composite (분할 라우터) | 여러 자식을 하나처럼 | `OmniSight/Application/Inspection/PartitionedInspectionOutputDispatcher.cs` | 입력별 순서 보장 출력, 런타임 무수정 | 10, 27 |
| Producer/Consumer | 유한 큐로 속도 분리 | `OmniSight/Devices/Cameras/CameraFrameQueue.cs`, `InspectionOutputDispatcher.cs:63-64`, `OmniSight/Logging/LogManager.cs:42-44` | 처리 속도 차이, 메모리 상한 | 10, 11 |
| Pipeline | 단계 순서 처리 | `InspectionPipeline.cs:246-339`, `RecipeImageProcessor.cs:43-67` | 전처리 → 룰 → AI → 후처리 → 발행 | 13, 16 |
| Object Pool | 비싼 객체 N개 재사용 | `OmniSight/Vision/AiEngine/AiEngineRegistry.cs:361-471` | 네이티브 엔진 1개 = 동시 1추론 | 18 |
| Copy-on-write / 스냅샷 교체 | 새로 만들어 참조 교체 | `AiEngineRegistry.cs:98-184`, `OmniSight/Configuration/JsonConfigProvider.cs:232-251`, `OmniSight/Application/HardwareMonitorService.cs:116` | lock 없이 일관된 읽기 | 09, 18, 23 |
| Prototype (깊은 복사) | 복제본 편집 | `JsonConfigProvider.cs:218-227` | UI 편집과 런타임 분리 | 23 |
| Repository/Provider | 저장소 접근 캡슐화 | `JsonConfigProvider<T>`, `OmniSight/Configuration/DataManager.cs:28-38` | 원자적 저장·백업·잠금 일원화 | 23 |
| Singleton (Lazy) | 프로세스당 하나 | `DataManager.cs:18-23`, `LogManager.Instance`, `AiEngineRegistry.Shared` | 단일 로그 작성기, 단일 설정 저장소 | 05 |
| Dispose 패턴 / 소유권 | 해제 책임 명시 | `DeviceContracts.cs:53-102`, `AiEngineBase.cs:149-176`, `OmniSight/Devices/Cameras/OwnedCameraImage.cs` | 네이티브 메모리·핸들 | 02, 15, 18 |
| Lease / Gate | `using`으로 빌리고 반납 | `OmniSight/Application/Inspection/SharedFileWriteGate.cs` | 같은 파일만 직렬화 | 09 |
| 순수 판단 객체 | 시간을 인자로 받는 결정 | `OmniSight/Devices/Cameras/LineSpeedAcquisitionGate.cs` | 하드웨어 없이 테스트 | 26 |
| 보상(롤백) | 실패 시 역순 되돌림 | `DeviceManager.cs:184-204`, `InspectionRuntime.cs:124-171`, `DeviceFactory.cs:71-87` | 반쯤 시작된 장비 방지 | 06 |

## 패턴 고르기 질문 (면접에서 이렇게 말한다)

1. **무엇이 바뀌나?** 생성 방법 → Factory. 외부 API 모양 → Adapter. 알고리즘 → Strategy. 순서는 같고 일부 단계만 → Template Method.
2. **누가 몇 개?** 하나만 → Singleton(조심). 비싸고 여러 개 → Pool.
3. **언제 알려야 하나?** 상태 변화 → Observer(구독자 격리, 해제).
4. **속도가 다른가?** → Producer/Consumer + 유한 큐 + 포화 정책.
5. **실패하면?** → 보상 롤백, 상태 Faulted, 멱등 Dispose.
6. **패턴이 과한가?** 구현 1개짜리 인터페이스, 분기 2개짜리 Strategy는 과할 수 있다. 바뀔 이유가 실제로 있을 때 도입.
