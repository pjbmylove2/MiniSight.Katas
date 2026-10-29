# Day 16 (화, 2h) · 필터 체인: Threshold, Edge, Morphology, Arithmetic + Strategy

## 1. 오늘 목표

- **완료 기준**
  - OmniSight 전처리 순서 "**필터 체인(Step 순) → 반전 → 좌우 → 상하 → 회전 → Crop → Resize**"를 외우고 순서가 바뀌면 결과가 왜 달라지는지 예를 들 수 있다.
  - Threshold(고정/적응형), Sobel/Laplacian/Canny/Scharr, Dilate/Erode, 산술 연산이 각각 **무엇을 위해** 쓰이는지 한 줄씩 말할 수 있다.
  - OmniSight의 `switch(filter.Type)`를 **Strategy(다형성)**로 바꾼 버전을 MiniSight에 만들고, 두 방식의 장단점을 비교할 수 있다.
  - JSON 다형 리스트(`TypeNameHandling.Auto`)로 레시피를 저장·복원했고, 그 **보안 위험**과 Binder 대책을 말할 수 있다.
- **최소분량**: 2, 3, 4.1~4.2, 5, 6.1~6.2
- **확장분량**: 4.3 JSON 보안, 6.3 레시피 JSON, 7번

## 2. 🧒 초1 비유

- **필터 체인 = 사진 꾸미기 앱의 효과 순서**. "흑백 → 선명하게 → 테두리 강조". 순서를 바꾸면(테두리 강조 → 흑백) 결과가 다르다. 그래서 효과마다 번호표(`Step`)가 있다.
- **Threshold = 색칠공부 정답 가르기**. "밝기 128보다 밝으면 흰색, 아니면 검은색". 적응형(Adaptive)은 **동네마다 기준이 다르다**: 그림자 진 곳은 기준을 낮춘다.
- **Sobel/Canny = 경계선 찾기 돋보기**. 밝기가 확 바뀌는 곳(물체 테두리)만 밝게.
- **Dilate/Erode = 색연필 굵게/가늘게**. 끊어진 선을 이어 붙이거나(Dilate), 작은 점 잡음을 지운다(Erode).
- **Strategy = 효과 카드 상자**. 앱이 "흑백 카드", "테두리 카드"를 상자에 순서대로 꽂아 두고 하나씩 꺼내 실행한다. 새 효과는 **카드만 추가**하면 된다(앱 코드 수정 없음).

## 3. OmniSight 지도

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 순서 주석 | `OmniSight/Vision/Processing/RecipeImageProcessor.cs:26` | "구 프로그램 순서: 원본 필터 체인 → 반전/회전 → Crop → Resize" |
| 체인 실행 | `OmniSight/Vision/Processing/RecipeImageProcessor.cs:43-67` | `OrderBy(Step)`, 단계마다 `current` 교체, 결과 비면 예외 |
| 필터 분기 | `OmniSight/Vision/Processing/RecipeImageProcessor.cs:76-91` | `switch (filter.Type)` + `as` 캐스팅 |
| Threshold | `OmniSight/Vision/Processing/RecipeImageProcessor.cs:93-130` 부근 | 적응형 blockSize를 홀수·3 이상으로 보정 |
| Convolution | `OmniSight/Vision/Processing/RecipeImageProcessor.cs` (ApplyConvolution) | Sobel/Gradient/Laplacian/Canny/Scharr, `CV_8U` 포화 (구 프로그램과 같게 `ConvertScaleAbs` 안 씀) |
| Morphology | 같은 파일 (ApplyMorphology) | 3×3 사각 커널, 반복 1~100, `BorderTypes.Replicate` |
| Arithmetic | 같은 파일 (ApplyArithmetic) | 상수 또는 **원본 영상**과 연산 |
| 변환 순서 | `OmniSight/Vision/Processing/InputImageTransformer.cs:28-60` | Invert → Mirror(Flip Y) → Flip(X) → Rotate … (제자리 연산) |
| 설정 기반 클래스 | `OmniSight/Configuration/Processing/ProcessingFilterBase.cs` | `Step`, `Type`(protected set), `SetProperty<T>` + INotifyPropertyChanged |
| 다형 JSON | `OmniSight/Configuration/Processing/ProcessingConfig.cs:8-11` | `ItemTypeNameHandling = TypeNameHandling.Auto` |
| 옛 이름 호환 | `OmniSight/Configuration/Processing/ThresholdFilterConfig.cs` | `[JsonProperty("halfKernelSize")] BlockSize` — JSON 키는 옛 이름 유지 |
| 타입 바인더 | `OmniSight/Configuration/JsonConfigProvider.cs:265-288` (LegacyFilterTypeBinder) | 옛 네임스페이스 타입 이름 → 현재 클래스 매핑. 나머지는 기본 바인더로 넘김(허용 목록 아님) |
| 문서 | `OmniSight/docs/ProcessingOrderChange.ko.md` | 순서 변경 이력 |

**집에서 Claude에게:**
```text
RecipeImageProcessor.cs의 ApplyFilter와 ApplyThreshold, ApplyConvolution, ApplyMorphology를 보여주고
각 필터를 색칠공부/돋보기/색연필 비유로 설명해줘. 그리고 switch 방식이 Strategy보다 나은 점과 못한 점을
OmniSight 사정(구 프로그램 호환, UI 에디터, JSON 저장)에 맞춰 정리해줘. JsonConfigProvider.cs 265~288줄
LegacyFilterTypeBinder도 보여주고, 이게 보안 허용 목록인지 아닌지 판단해줘.
```

## 4. 왜 이렇게 썼나

### 4.1 필터 용도 한 줄 요약

| 필터 | 무엇을 | 언제 |
|---|---|---|
| Threshold SINGLE | 기준값으로 흑/백 | 조명이 고른 경우 |
| Threshold ADAPTIVE | 주변 평균 기준 흑/백 | 그림자·조명 불균일 |
| Sobel X/Y | 가로/세로 방향 밝기 변화 | 특정 방향 에지(필름 끝단) |
| Laplacian | 모든 방향 2차 미분 | 점·작은 결함 강조 |
| Canny | 얇고 연결된 에지 | 윤곽 추출 |
| Scharr | Sobel보다 정확한 3×3 미분 | 미세 에지 |
| Dilate | 흰 영역 굵게 | 끊긴 글자/선 연결 |
| Erode | 흰 영역 가늘게 | 작은 잡음 제거 |
| Arithmetic | 더하기/빼기/비교/논리 | 배경 빼기, 마스크 |

### 4.2 switch vs Strategy

| | OmniSight `switch` | MiniSight Strategy (`IFilter.Apply`) |
|---|---|---|
| 새 필터 추가 | enum + 설정 클래스 + switch case + UI 에디터 | 새 클래스 하나 (+ UI 에디터) |
| 알고리즘 위치 | 처리기 한 파일에 모여 있음 (구 프로그램과 줄 단위 비교 쉬움) | 필터마다 흩어짐 |
| 설정과 알고리즘 | 설정(DTO)과 알고리즘 분리 → JSON이 순수 데이터 | 설정 클래스가 알고리즘까지 가지면 JSON 직렬화 대상이 커짐 → 보통 "설정 DTO + 필터 팩토리"로 분리 |
| 테스트 | 처리기 통째로 | 필터 하나씩 |

**결론**: OmniSight는 "구 프로그램과 결과를 1:1로 맞추는 이식 단계"라 한 파일에 모아 비교하기 쉬운 `switch`가 합리적이다. 필터 종류가 늘고 현장별 커스텀 필터가 생기면 Strategy로 옮길 가치가 생긴다. **패턴은 상황의 답**이다.

### 4.3 TypeNameHandling의 그림자 (보안 면접 단골)

`TypeNameHandling.Auto`는 JSON 안의 `"$type"`을 보고 그 타입을 만든다. 선언 타입이 `object`나 넓은 인터페이스라면 누군가 레시피 파일에 위험한 .NET 타입 이름을 넣어 **역직렬화 중 임의 코드 실행**을 일으킬 수 있다(알려진 공격 기법). 장비 PC라도 USB로 파일이 들어오므로 방심 금지.

OmniSight를 정확히 보면:
- `$type` 처리는 `Filters` 목록 항목에만 켜져 있고(`ItemTypeNameHandling`), 항목 선언 타입이 `ProcessingFilterBase`다. Newtonsoft는 `$type`이 선언 타입과 **호환되지 않으면 예외**를 던지므로 만들 수 있는 타입이 필터 클래스 계열로 좁혀진다. 이것이 실제 방어선이다.
- `LegacyFilterTypeBinder`는 보안용 **허용 목록이 아니다**. 폴더 정리 전 옛 네임스페이스 이름을 현재 클래스로 바꿔 주고, 나머지는 `base.BindToType`(기본 바인더)로 넘긴다.
- 그래서 MiniSight에서는 한 단계 더 나아가 **허용 목록 바인더**(모르는 이름이면 예외)를 직접 만든다(6.3). 면접에서 "선언 타입 제한 + 허용 목록 바인더, 둘 다"라고 말할 수 있으면 된다.

### 4.4 순서가 결과를 바꾸는 예

- Threshold → Dilate: 흰 글자가 굵어짐. Dilate → Threshold: 회색 번짐이 기준을 넘어 모양이 달라짐.
- Crop → Rotate vs Rotate → Crop: ROI 좌표가 회전 전 기준인지 후 기준인지 달라짐. 그래서 **순서를 레시피 규칙으로 고정**하고 문서화한다.

## 5. Kata: 효과 카드 상자 (Strategy + Chain)

```csharp
// KATA: Day16 (needs OpenCvSharp)
using System;
using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;

namespace Katas.Day16
{
    public interface IFilter
    {
        int Step { get; }
        string Name { get; }
        Mat Apply(Mat input);   // 규칙: input은 건드리지 않고 새 Mat을 반환
    }

    public sealed class ThresholdFilter : IFilter
    {
        private readonly double _value;
        public ThresholdFilter(int step, double value) { Step = step; _value = value; }
        public int Step { get; private set; }
        public string Name { get { return "Threshold(" + _value + ")"; } }
        public Mat Apply(Mat input)
        {
            var dst = new Mat();
            Cv2.Threshold(input, dst, _value, 255, ThresholdTypes.Binary);
            return dst;
        }
    }

    public sealed class SobelXFilter : IFilter
    {
        public SobelXFilter(int step) { Step = step; }
        public int Step { get; private set; }
        public string Name { get { return "SobelX"; } }
        public Mat Apply(Mat input)
        {
            var dst = new Mat();
            Cv2.Sobel(input, dst, MatType.CV_8U, 1, 0, 3);   // 8U 포화: 음수 기울기는 0
            return dst;
        }
    }

    public sealed class DilateFilter : IFilter
    {
        private readonly int _iterations;
        public DilateFilter(int step, int iterations) { Step = step; _iterations = iterations; }
        public int Step { get; private set; }
        public string Name { get { return "Dilate x" + _iterations; } }
        public Mat Apply(Mat input)
        {
            var dst = new Mat();
            using (Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3)))
            {
                Cv2.Dilate(input, dst, kernel, null, _iterations, BorderTypes.Replicate);
            }
            return dst;
        }
    }

    public sealed class FilterChain
    {
        private readonly List<IFilter> _filters;
        public FilterChain(IEnumerable<IFilter> filters) { _filters = filters.OrderBy(f => f.Step).ToList(); }

        public Mat Execute(Mat source)
        {
            Mat current = source.Clone();          // 원본 보호
            try
            {
                foreach (IFilter filter in _filters)
                {
                    Mat next = filter.Apply(current);
                    if (next == null || next.Empty()) { if (next != null) next.Dispose(); throw new InvalidOperationException(filter.Name + " 결과 없음"); }
                    current.Dispose();
                    current = next;
                    Console.WriteLine("  " + filter.Step + ". " + filter.Name + " 완료 (평균 " + Cv2.Mean(current).Val0.ToString("0.0") + ")");
                }
                Mat result = current;
                current = null;                     // 소유권을 호출자에게
                return result;
            }
            finally
            {
                if (current != null) current.Dispose();
            }
        }
    }

    public static class KataDay16
    {
        public static void Run()
        {
            using (var img = new Mat(200, 300, MatType.CV_8UC1, Scalar.All(220)))
            {
                Cv2.Rectangle(img, new Rect(100, 50, 80, 100), Scalar.All(40), -1);
                var chain = new FilterChain(new IFilter[] { new DilateFilter(3, 1), new SobelXFilter(2), new ThresholdFilter(1, 128) });
                using (Mat result = chain.Execute(img))
                {
                    Cv2.ImWrite(@"C:\MiniSight\kata16_result.png", result);
                    Console.WriteLine("원본 평균 그대로? " + Cv2.Mean(img).Val0.ToString("0.0"));
                }
            }
        }
    }
}
```

실험: Step을 바꿔 `Sobel → Threshold → Dilate`와 `Threshold → Sobel → Dilate` 결과 이미지를 비교한다.

## 6. MiniSight 오늘 단계

### 6.1 `Vision/Filters/` — Kata의 IFilter 3종 + `ErodeFilter`, `AdaptiveThresholdFilter`(blockSize를 홀수·3 이상으로 보정)
### 6.2 `Vision/FilterChain.cs` + `Inspection/RecipePreprocessor : IPreprocessor`
- `Func<RecipeConfig>`로 매 프레임 최신 레시피를 읽어 체인 생성 → 실행 → `ProcessedImage`.
- 변환 단계 `ImageTransformer.ExecuteOwned(Mat owned, TransformConfig t)`: Invert → FlipY → FlipX → Rotate(90/180/270은 `Cv2.Rotate`) → Crop(`new Mat(m, rect).Clone()`) → Resize. **성공 시 반환값을, 실패 시 입력을 함수가 책임**.
### 6.3 레시피 JSON (확장)
- 설정은 DTO로 분리: `FilterSettingBase { Step, Type }`, `ThresholdSetting`, `SobelSetting` … + `FilterFactory.Create(FilterSettingBase)`(Day04의 Simple Factory 재사용).
- `List<FilterSettingBase>`에 `[JsonProperty(ItemTypeNameHandling = TypeNameHandling.Auto)]`.
- `ISerializationBinder`를 구현해 `MiniSight.Config.*Setting` 타입만 허용. 다른 `$type`이 오면 예외.

## 7. VS2022 디버깅 실습

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | `FilterChain.Execute`의 `current = next;` 줄에 중단점 | |
| 2 | 직접 실행 창: `Cv2.ImWrite(@"C:\MiniSight\step_" + filter.Step + ".png", next)` | 단계별 결과 이미지 |
| 3 | `filter` 변수에 마우스 → **DataTip 고정(핀)** | 루프를 돌아도 화면에 값이 떠 있음. 반복문 디버깅에 편함 |
| 4 | DataTip 우클릭 → 주석 추가 | |
| 5 | 중단점 → 작업: `{filter.Name} 평균={Cv2.Mean(next).Val0}` Tracepoint | 멈추지 않고 단계별 평균 로그 |
| 6 | 레시피 JSON에 허용 안 된 `$type`을 넣고 로드 | Binder가 막는지 확인 |

## 8. 셀프 체크

1. 적응형 Threshold의 blockSize를 홀수로 보정하는 이유는?
<details><summary>답</summary>blockSize는 중심 픽셀 주변 이웃 창의 크기라 중심이 있어야 하므로 홀수(3, 5, …)여야 한다. 짝수면 OpenCV가 예외를 던진다. 영상보다 크면 안 되므로 상한도 보정한다.</details>

2. Sobel 결과를 `CV_8U`로 받으면 무엇이 사라지나?
<details><summary>답</summary>음수 기울기(밝음→어두움)가 0으로 잘리고 255 초과는 255로 포화된다. 한 방향 에지만 남는다. 양쪽 에지가 필요하면 CV_16S로 받고 ConvertScaleAbs를 쓴다(OmniSight는 구 프로그램 결과와 맞추려고 8U 유지).</details>

3. switch 대신 Strategy로 바꾸면 무엇이 좋아지고 무엇이 어려워지나?
<details><summary>답</summary>좋아짐: 새 필터 추가 시 기존 코드 수정 없음(개방-폐쇄), 필터별 단위 테스트. 어려워짐: 알고리즘이 여러 파일에 흩어져 레거시와 비교가 어렵고, 설정 DTO와 알고리즘을 잇는 팩토리가 필요하며, JSON 직렬화 경계 설계가 늘어난다.</details>

4. `TypeNameHandling.Auto`의 위험과 대책은?
<details><summary>답</summary>JSON의 `$type`으로 임의 타입을 생성할 수 있어 가젯 체인을 통한 원격 코드 실행 취약점이 된다. ISerializationBinder로 허용 목록 타입만 바인딩하고, 가능하면 다형 대신 `type` 필드 + 수동 팩토리로 역직렬화한다.</details>

5. 필터가 입력 Mat을 수정하면 안 되는 이유는?
<details><summary>답</summary>입력은 앞 단계 결과이거나 원본이며 다른 소비자(산술 필터의 원본 피연산자, 저장, AI)가 공유할 수 있다. 제자리 수정은 다른 결과를 오염시키고 순서 의존 버그를 만든다. 제자리 연산은 소유권이 확실한 내부 사본에서만 한다(InputImageTransformer).</details>

## 9. 블로그 포인트

- 제목 예: `[머신비전] 전처리 필터 체인을 Strategy로 설계하기 (그리고 switch가 더 나은 경우)`
- 순서 바꾼 결과 이미지 비교(합성 이미지라 안전), JSON 다형성 보안 이야기.
