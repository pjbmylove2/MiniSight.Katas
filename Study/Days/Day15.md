# Day 15 (월, 2h) · 사진 상자 Mat: 채널, stride, 래퍼 vs 복사, ROI, 소유권

## 1. 오늘 목표

- **완료 기준**
  - `Mat`이 "관리 객체(작은 껍데기) + 네이티브 메모리(큰 픽셀)"라서 **Dispose가 필수**인 이유를 설명할 수 있다.
  - `width × channels`와 `step(stride)`이 다를 수 있는 이유(행 패딩)를 그림으로 설명할 수 있다.
  - `Mat.FromPixelData`(래퍼, 복사 없음)와 `Clone`(독립 사본)의 차이, **ROI sub-Mat은 원본 메모리를 공유**한다는 것을 실험으로 확인했다.
  - OmniSight의 Mat 소유권 표(SDK 버퍼 → 앱 사본 → 큐 → 파이프라인 → 저장 사본 → 화면 사본)를 백지에 그릴 수 있다.
  - MiniSight 테스트용 **합성 이미지 40장**을 만들었다.
  - VS2022 직접 실행 창에서 `Cv2.ImWrite`로 디버깅 중 이미지를 파일로 떠 봤다.
- **최소분량**: 2, 3, 4.1~4.2, 5의 1~4
- **확장분량**: 5의 5~6, 7번 4~5

## 2. 🧒 초1 비유

- **Mat = 사진 앨범 표지 + 창고에 있는 사진들**. 표지(C# 객체)는 가볍지만 사진(픽셀)은 창고(네이티브 메모리)에 있다. 표지를 버려도 GC 청소부는 창고 사정을 모른다. 그래서 **"창고 비워 주세요"(Dispose)**를 직접 말해야 한다.
- **stride = 공책 한 줄의 칸 수**. 글자(픽셀)가 10칸만 필요해도 공책은 한 줄에 12칸이 있을 수 있다(정렬용 여백). 다음 줄로 가려면 10칸이 아니라 **12칸**을 건너뛰어야 한다.
- **래퍼(FromPixelData) = 남의 앨범을 보는 돋보기**. 사진을 복사하지 않고 그 자리에서 본다. 주인이 앨범을 치우면 돋보기로는 아무것도 못 본다.
- **Clone = 사진 복사본**. 내 것이라 주인이 치워도 괜찮다. 대신 창고가 두 배 든다.
- **ROI = 사진의 일부분에 액자 틀 대기**. 틀 안을 칠하면 **원래 사진도 칠해진다**(같은 사진이니까).

## 3. OmniSight 지도

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 소유권 문서 | `OmniSight/docs/onboarding/11-MemoryThreadsAndPerformance.ko.md` §1, §3 | 소유권 표, 메모리 계산법 |
| SDK 버퍼 → 앱 사본 | `OmniSight/Devices/Cameras/OwnedCameraImage.cs:11-26` | stride 검증 → 래퍼 → `Clone()` (패딩 제거된 연속 Mat) |
| managed 배열 고정 | `OmniSight/Devices/Cameras/OwnedCameraImage.cs:27-36` | `GCHandle.Alloc(Pinned)` → 주소 → 복사 → `Free` |
| 프레임 래퍼 | `OmniSight/Application/Inspection/RecipeImagePreprocessor.cs:38-57` | `FromPixelData(h, w, type, frame.Data, frame.Step)` + `using` |
| 처리 입력 보호 | `OmniSight/Vision/Processing/RecipeImageProcessor.cs:43` | 시작하자마자 `source.Clone()` → 원본은 절대 안 바꿈 |
| 단계마다 교체 | `OmniSight/Vision/Processing/RecipeImageProcessor.cs:51-60` | `next` 만들고 `current.Dispose(); current = next;` |
| 실패 시 정리 | `OmniSight/Vision/Processing/RecipeImageProcessor.cs:65-73` | `owned = current; current = null;` 소유권 넘김, catch에서 `current?.Dispose()` |
| 저장용 사본 | `OmniSight/Application/Inspection/InspectionOutputDispatcher.cs:125-148` | Clone 2개 → request에 넘기면 로컬 null |
| 화면 사본 | `OmniSight/UI/Controls/LiveCameraDisplayControl.cs:82` | 콜백에서는 Clone만 하고 처리는 다른 스레드 |

**집에서 Claude에게:**
```text
OwnedCameraImage.cs 전체와 RecipeImageProcessor.cs 27~74줄을 보여주고
앨범/돋보기/복사본 비유로 Mat이 어디서 만들어져서 어디서 Dispose되는지 따라가 줘.
특히 "owned = current; current = null;" 두 줄이 왜 필요한지.
```

## 4. 왜 이렇게 썼나

### 4.1 메모리 그림

```text
width=5, channels=1, 실제 데이터 5바이트/행, step=8 (8바이트 정렬)

행0: [p p p p p][x x x]   ← x = 패딩 (쓰레기 값)
행1: [p p p p p][x x x]
       └ 5 ┘    └ 3 ┘
       └────── step 8 ──────┘
픽셀(r, c) 주소 = data + r * step + c * channels
```

SDK가 주는 버퍼는 패딩이 있을 수 있다. `width*channels`로 다음 줄을 계산하면 영상이 **비스듬히 찢어진다**. `OwnedCameraImage.Copy`는 stride를 받아 래퍼를 만들고 `Clone()`으로 **패딩 없는 연속 Mat**을 만든다.

### 4.2 결정과 이유

| 결정 | 이유 |
|---|---|
| SDK 콜백 안에서 즉시 복사 | 콜백이 끝나면 SDK가 버퍼를 다음 사진에 재사용 |
| 프레임은 래퍼로 감싸서 처리 | 복사 없음 = 빠름. 원본 소유자(ImageFrame)가 처리 내내 살아 있음이 보장되는 구간에서만 |
| 처리기는 입력을 Clone 후 작업 | 룰·AI·저장이 원본을 공유. 처리기가 원본을 고치면 다른 쪽 결과가 틀어짐 |
| `next`/`current` 교체 | 단계마다 중간 결과 하나만 살아 있음 → 메모리 최소 |
| 실패 경로 Dispose | 예외 시 중간 Mat이 새면 하루 수십만 장에 GB 단위 누수 |
| `GCHandle.Pinned` | managed 배열은 GC가 옮길 수 있음. 네이티브가 주소를 쓰는 동안 고정 |

### 4.3 메모리 계산 습관

`폭 × 높이 × 채널 × 동시에 살아 있는 사본 수`. 2000×1000 BGR = 6MB. 카메라 큐 32장 = 192MB. 여기에 처리 사본, 저장 사본(큐 4), 화면 사본이 더해진다. 카메라 4대면 ×4. **설계할 때 이 계산을 먼저** 한다.

### 4.4 시니어 함정

- `Mat`은 `IDisposable`이지만 finalizer도 있어 "언젠가는" 해제된다. 그래서 테스트에선 멀쩡하다가 **현장 24시간 운전에서만** 메모리가 터진다. GC는 네이티브 메모리 압박을 모른다.
- `GC.Collect()`로 증상을 감추는 건 해결이 아니다 (onboarding 11 §5).
- `Mat` 연산자(`a + b`, `~a`)는 임시 객체를 만든다. 반복문 안에서는 `Cv2.Add(a, b, dst)`처럼 출력 Mat을 지정하는 형태가 안전.

## 5. Kata: Mat 실험 6종 + 합성 이미지 공장

`MiniSight.Katas`에 OpenCvSharp4 NuGet 3종 설치, 플랫폼 x64.

```csharp
// KATA: Day15 (needs OpenCvSharp)
using System;
using System.IO;
using System.Runtime.InteropServices;
using OpenCvSharp;

namespace Katas.Day15
{
    public static class KataDay15
    {
        public static void Run()
        {
            // 1. Mat 기본 정보
            using (var gray = new Mat(480, 640, MatType.CV_8UC1, Scalar.All(128)))
            using (var color = new Mat(480, 640, MatType.CV_8UC3, new Scalar(255, 0, 0)))   // BGR 순서! 파랑
            {
                Console.WriteLine("gray: " + gray.Width + "x" + gray.Height + " ch=" + gray.Channels() + " step=" + gray.Step() + " cont=" + gray.IsContinuous());
                Console.WriteLine("color: ch=" + color.Channels() + " step=" + color.Step() + " (640*3=1920)");
            }

            // 2. stride 있는 외부 버퍼를 래퍼로 감싸기
            int width = 5, height = 3, stride = 8;
            byte[] buffer = new byte[stride * height];
            for (int r = 0; r < height; r++)
                for (int c = 0; c < width; c++) buffer[r * stride + c] = (byte)(r * 10 + c);
            GCHandle pin = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                using (Mat wrapper = Mat.FromPixelData(height, width, MatType.CV_8UC1, pin.AddrOfPinnedObject(), stride))
                using (Mat owned = wrapper.Clone())
                {
                    Console.WriteLine("wrapper step=" + wrapper.Step() + " cont=" + wrapper.IsContinuous());
                    Console.WriteLine("clone   step=" + owned.Step() + " cont=" + owned.IsContinuous());
                    buffer[0] = 99;                                   // 원본 버퍼 수정
                    Console.WriteLine("wrapper(0,0)=" + wrapper.At<byte>(0, 0) + " clone(0,0)=" + owned.At<byte>(0, 0));
                }
            }
            finally { pin.Free(); }

            // 3. ROI는 메모리를 공유한다
            using (var image = new Mat(100, 100, MatType.CV_8UC1, Scalar.All(0)))
            {
                using (var roi = new Mat(image, new Rect(10, 10, 20, 20)))
                {
                    roi.SetTo(Scalar.All(255));                      // 틀 안을 칠하면
                }
                Console.WriteLine("원본(15,15)=" + image.At<byte>(15, 15) + " (255면 공유 확인)");
            }

            // 4. 합성 이미지 공장: 흰 바탕, 검은 사각형이 오른쪽으로 2px씩 이동
            string root = @"C:\MiniSight\Images";
            for (int cam = 1; cam <= 2; cam++)
            {
                string dir = Path.Combine(root, "Cam" + cam);
                Directory.CreateDirectory(dir);
                for (int i = 0; i < 20; i++)
                {
                    using (var img = new Mat(480, 640, MatType.CV_8UC1, Scalar.All(230)))
                    {
                        int x = 200 + i * 2;
                        Cv2.Rectangle(img, new Rect(x, 150, 120, 180), Scalar.All(30), -1);
                        if (i % 7 == 6) Cv2.Circle(img, new Point(x + 60, 240), 15, Scalar.All(230), -1); // 가끔 불량(구멍)
                        Cv2.ImWrite(Path.Combine(dir, i.ToString("D3") + ".png"), img);
                    }
                }
            }
            Console.WriteLine("합성 이미지 40장 생성: " + root + " (사각형 왼쪽 에지 x = 200 + i*2 가 정답)");

            // 5. (확장) 누수 체감: Dispose 없이 10000번
            // for (int i = 0; i < 10000; i++) { var m = new Mat(1000, 1000, MatType.CV_8UC3); }
            // 작업 관리자에서 메모리 확인 → using으로 바꿔서 비교

            // 6. (확장) 연산자 임시 객체: (a + b) * 2 는 Mat이 몇 개 생길까?
        }
    }
}
```

## 6. MiniSight 오늘 단계

1. Kata 4번으로 이미지 생성 → `FolderCamera` 설정 폴더를 `C:\MiniSight\Images\Cam1`, `Cam2`로.
2. `FolderCamera`에서 `Cv2.ImRead(path, ImreadModes.Grayscale)` 결과를 `ImageFrame`에 넘길 때 소유권 이동 확인(로컬 `mat = null`).
3. `Docs/Ownership.md`를 MiniSight 저장소에 만들고 **소유권 표**를 직접 작성:

| 단계 | 누가 만드나 | 누가 Dispose | 어디서 |
|---|---|---|---|
| 드라이버/폴더에서 읽은 Mat | | | |
| ImageFrame | | | |
| 파이프라인 래퍼(쓴다면) | | | |
| ProcessedImage | | | |
| 저장 요청 사본 | | | |
| 화면 사본 | | | |

## 7. VS2022 디버깅 실습: Mat을 눈으로 보기

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | Kata 3번 `roi.SetTo` 다음 줄에 중단점 | |
| 2 | **직접 실행 창(Ctrl+Alt+I)** 에 `Cv2.ImWrite(@"C:\MiniSight\debug_roi.png", image)` 입력 → Enter | 디버깅 중에 이미지 파일로 떠서 그림판으로 확인. **가장 확실한 Mat 시각화 방법** |
| 3 | 조사식에 `image.At<byte>(15, 15)`, `image.Step()`, `image.IsDisposed` | 값 확인 |
| 4 | 조사식에 `image.Dump()` (작은 Mat만!) | 픽셀 값 문자열 |
| 5 | 확장: `Debug/MatDebug.cs`에 `public static string Save(Mat m, string name)` 헬퍼를 만들어 두고 직접 실행 창에서 `MatDebug.Save(processed.Image, "after_threshold")`로 호출 | Day16~17에서 단계별 결과 확인에 계속 씀 |

## 8. 셀프 체크

1. Mat을 Dispose하지 않으면 왜 GC가 늦게 치우나?
<details><summary>답</summary>GC는 관리 힙 크기만 보고 수집 시점을 정한다. Mat의 관리 객체는 수십 바이트라 압박이 거의 없지만 네이티브 픽셀은 수 MB다. 관리 힙이 한가하면 finalizer가 오래 실행되지 않아 네이티브 메모리가 계속 쌓인다.</details>

2. stride가 width×channels보다 클 수 있는 이유와, 무시하면 생기는 증상은?
<details><summary>답</summary>메모리 정렬(4/8/16바이트)이나 하드웨어 DMA 요구로 행 끝에 패딩이 붙는다. stride를 무시하고 width×channels로 행을 넘기면 행마다 패딩만큼 밀려 영상이 사선으로 찢어지고 오른쪽에 쓰레기 픽셀이 보인다.</details>

3. `Mat.FromPixelData` 래퍼를 다른 스레드에 넘기면 안 되는 이유는?
<details><summary>답</summary>래퍼는 메모리를 소유하지 않는다. 원래 소유자가 그 메모리를 해제하거나 재사용하면 래퍼는 무효 메모리를 가리킨다. 수명이 보장되는 같은 범위 안에서만 쓰고, 넘기려면 Clone한다.</details>

4. ROI sub-Mat을 수정하면 원본이 바뀌는 이유와, 원본을 지키려면?
<details><summary>답</summary>ROI는 원본 데이터 포인터에 오프셋과 step만 바꾼 헤더라 같은 메모리를 공유한다. 독립 결과가 필요하면 `new Mat(src, rect).Clone()`처럼 복사한다(OmniSight Crop도 Clone).</details>

5. 2000×1000 컬러 영상, 카메라 2대, 카메라 큐 32 + 처리 사본 1 + 저장 큐 4 + 화면 1이면 대략 메모리는?
<details><summary>답</summary>장당 6MB. 카메라당 (32+1+1) = 34장 → 2대 68장, 저장 큐 4장(공유) = 72장 × 6MB ≈ 432MB. SDK 내부 버퍼·AI GPU 메모리·관리 객체는 별도.</details>

## 9. 블로그 포인트

- 제목 예: `[OpenCvSharp] Mat은 왜 꼭 Dispose해야 할까: stride, 래퍼, ROI 실험`
- Kata 2·3번 출력, stride 그림, 직접 실행 창에서 `Cv2.ImWrite` 하는 디버깅 팁(의외로 사람들이 모름).
