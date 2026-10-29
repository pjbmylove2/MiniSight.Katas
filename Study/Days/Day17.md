# Day 17 (수, 2h) · 자로 재기: 캘리퍼, 라인 피팅(PCA), 블랍

## 1. 오늘 목표

- **완료 기준**
  - **캘리퍼(Point Gauge)**: 선 하나를 따라 픽셀을 읽어 "가장 큰 밝기 변화" 위치를 찾는 원리를 그림으로 설명할 수 있다.
  - **라인 게이지**: 캘리퍼 여러 개의 점으로 직선을 맞추는 공식 `θ = ½·atan2(2Sxy, Sxx − Syy)`가 무엇을 의미하는지(점들이 가장 길게 퍼진 방향) 설명할 수 있다.
  - **오브젝트 게이지**: `ConnectedComponentsWithStats`로 덩어리 개수·면적을 세고 면적 범위로 거르는 흐름을 설명할 수 있다.
  - 합성 이미지(Day15)에서 사각형 왼쪽 에지 x를 재서 **정답(200 + i×2)과 ±1px 이내**로 맞혔다.
  - MiniSight `RuleEngine`에 Point/Line/Blob 게이지를 넣었다.
- **최소분량**: 2, 3, 4.1~4.2, 5의 Point·Line
- **확장분량**: Blob, 6.3 AI 실행 게이트, 7번

## 2. 🧒 초1 비유

- **캘리퍼 = 손가락으로 줄 따라가기**. 그림 위에 자를 대고 왼쪽부터 손가락으로 쭉 간다. "밝은 데서 어두운 데로 확 바뀌는 곳"에서 멈춘다. 거기가 물체의 테두리.
- **라인 게이지 = 손가락을 여러 줄에서 해 보고 점들을 이어 직선 긋기**. 점이 조금씩 삐뚤어도 **제일 잘 맞는 직선**을 긋는다. 점이 너무 적으면(대부분 실패) "선을 못 찾았다".
- **PCA = 점들이 제일 길쭉하게 퍼진 방향 찾기**. 콩알들을 책상에 뿌렸을 때 길쭉한 모양이면, 그 긴 방향이 선의 방향.
- **블랍 = 색종이 조각 세기**. 붙어 있는 같은 색 픽셀을 한 덩어리로 보고 번호를 매긴다. 너무 작은 건 먼지, 너무 큰 건 배경이니 크기로 거른다.

## 3. OmniSight 지도

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 게이지 종류 | `OmniSight/Configuration/Recipe/RecipeRuleConfig.cs` | `GaugeType { POINT, LINE, OBJECT }`, 방향 L2R/R2L/T2B/B2T/IN2OUT/OUT2IN |
| Point 게이지 | `OmniSight/Application/Inspection/RecipeRuleInspectionEngine.cs:245-295` | 원점에서 각도 방향 ±Tolerance 선분, 이진 → 실패 시 회색조 재시도 |
| 전이점 찾기 | `OmniSight/Application/Inspection/RecipeRuleInspectionEngine.cs:614-668` | 선분 위 정수 좌표 샘플링(중복 제거), 이웃 차이 최댓값, 최소 차이 기준 |
| Line 게이지 | `OmniSight/Application/Inspection/RecipeRuleInspectionEngine.cs:337-432` | 축 방향으로 Sampling+1개 캘리퍼(법선 방향 스캔), 최소 성공 개수, 공분산 각도, 투영으로 끝점 |
| 최소 점 개수 | `OmniSight/Application/Inspection/RecipeRuleInspectionEngine.cs:379-386` | `max(2, (Length/Sampling)/2 + 1)` — 구 프로그램 규칙 유지 |
| Object 게이지 | `OmniSight/Application/Inspection/RecipeRuleInspectionEngine.cs:434-599` | ROI → InRange 또는 Threshold → `ConnectedComponentsWithStats` → 면적 필터 |
| AI 실행 게이트 | `OmniSight/Application/Inspection/RecipeRuleInspectionEngine.cs:86-125` | 블랍 개수가 최소 이상일 때만 AI 실행 → `ShouldRunAi` |
| PLC로 라인 위치 | `OmniSight/Application/Inspection/RecipeRuleInspectionEngine.cs:58-84` | 특정 설비만, 못 찾으면 0 |
| 라이브 선 그리기 | `OmniSight/Application/Inspection/RecipeRuleInspectionEngine.cs:313-335` | BGR 사본에 AA 라인 |
| 안 쓰는 코드 | `OmniSight/Application/Inspection/RecipeRuleInspectionEngine.cs:670-703` | `FindStrongestEdge` — 호출처 없음(정리 후보) |

**집에서 Claude에게:**
```text
RecipeRuleInspectionEngine.cs 245~295(Point), 337~432(Line), 614~668(FindTransitionPoint)를 보여주고
손가락 비유로 설명해줘. Line에서 normal(법선) 방향으로 스캔하는 이유, 공분산 각도 공식을
그림으로(텍스트 그림) 설명하고, 최소 성공 개수 규칙이 왜 필요한지 알려줘.
```

## 4. 왜 이렇게 썼나

### 4.1 그림

```text
Point 게이지 (Angle=0°, L2R)                 Line 게이지 (축=세로, 스캔=가로)
                                              axis ↓   scan →
 start ●───────────────●──────● end              ●──────●(점1)──●
       밝음 밝음 밝음 | 어둠 어둠                   ●──────●(점2)──●
                    ↑ 가장 큰 차이                  ●───────●(점3)─●
                    = 측정점                        ...  → 점들로 직선 맞춤
```

### 4.2 결정과 이유

| 결정 | 이유 |
|---|---|
| 먼저 이진 영상에서 차이 ≥10 | 0/255 영상에선 에지가 분명하고 잡음에 강함 |
| 실패하면 회색조에서 차이 ≥ Threshold | 전처리 결과가 완전 이진이 아니거나 확대 화면에서도 동작 |
| 이웃 차이 "최댓값" 하나 | 가장 강한 에지 = 가장 확실한 경계. 잡음 에지보다 우선 |
| 방향(L2R/R2L 등) | "밝→어"인지 "어→밝"인지로 같은 선 위의 다른 에지를 구분 |
| 최소 성공 캘리퍼 수 | 1~2개만 성공한 선은 잡음일 확률이 높음. 거짓 측정을 PLC에 보내지 않기 |
| 공분산(PCA) 직선 | 최소제곱(y=ax+b)은 세로선에서 기울기가 무한대가 되어 깨짐. PCA는 **어떤 방향이든** 안정 |
| ConnectedComponentsWithStats | 한 번에 라벨·면적·외접 사각형·중심을 줌. findContours보다 개수·면적 계산이 단순 |
| 블랍 → AI 게이트 | 문자가 있을 것 같은 프레임만 무거운 AI에 보냄 → 처리량 증가 |

### 4.3 수학 한 장 (면접에서 화이트보드로)

점 (xᵢ, yᵢ), 평균 (x̄, ȳ), dx = x − x̄, dy = y − ȳ

- Sxx = Σdx², Syy = Σdy², Sxy = Σdx·dy
- 직선 방향 θ = ½·atan2(2Sxy, Sxx − Syy)
- 끝점 = 평균점 + 축 방향 × (투영 최소/최대)

### 4.4 시니어의 눈

- 서브픽셀 정밀도가 없다(정수 좌표). 미크론 단위 계측이면 보간(포물선 피팅 등)이 필요. 현재는 구 프로그램과 같은 결과가 목표.
- 이상치(튀는 점)에 약하다. RANSAC 같은 강건 피팅이 대안.
- 픽셀마다 `At<byte>`는 느리다. 캘리퍼 몇 개는 괜찮지만 전체 영상 루프엔 `GetGenericIndexer`/포인터/`Cv2.Reduce` 같은 방법을 쓴다.

## 5. Kata: 손가락 자

```csharp
// KATA: Day17 (needs OpenCvSharp)
using System;
using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;

namespace Katas.Day17
{
    public static class Gauges
    {
        // 선분 위에서 이웃 픽셀 차이가 가장 큰(음수: 밝→어) 지점
        public static Point? FindEdge(Mat gray, Point2d start, Point2d end, bool brightToDark, int minDiff)
        {
            int steps = Math.Max(Math.Abs((int)Math.Round(end.X - start.X)), Math.Abs((int)Math.Round(end.Y - start.Y)));
            if (steps < 1) return null;
            var points = new List<Point>();
            var values = new List<int>();
            for (int s = 0; s <= steps; s++)
            {
                double r = (double)s / steps;
                int x = (int)Math.Round(start.X + (end.X - start.X) * r);
                int y = (int)Math.Round(start.Y + (end.Y - start.Y) * r);
                if (x < 0 || y < 0 || x >= gray.Width || y >= gray.Height) continue;
                if (points.Count > 0 && points[points.Count - 1].X == x && points[points.Count - 1].Y == y) continue;
                points.Add(new Point(x, y));
                values.Add(gray.At<byte>(y, x));
            }
            int best = -1, bestDiff = 0;
            for (int i = 0; i < values.Count - 1; i++)
            {
                int d = values[i + 1] - values[i];
                int score = brightToDark ? -d : d;
                if (score >= minDiff && score > bestDiff) { bestDiff = score; best = i + 1; }
            }
            return best < 0 ? (Point?)null : points[best];
        }

        // 세로 에지: y를 따라 캘리퍼 여러 개 → PCA 직선
        public static bool FitVerticalEdge(Mat gray, int x0, int yFrom, int yTo, int samples, int tolerance,
            out double angleDeg, out double centerX)
        {
            var pts = new List<Point>();
            for (int i = 0; i <= samples; i++)
            {
                int y = yFrom + (yTo - yFrom) * i / samples;
                Point? p = FindEdge(gray, new Point2d(x0 - tolerance, y), new Point2d(x0 + tolerance, y), true, 30);
                if (p.HasValue) pts.Add(p.Value);
            }
            angleDeg = 0; centerX = 0;
            if (pts.Count < Math.Max(2, samples / 2 + 1)) return false;   // 최소 성공 개수
            double mx = pts.Average(p => p.X), my = pts.Average(p => p.Y);
            double sxx = 0, syy = 0, sxy = 0;
            foreach (Point p in pts) { double dx = p.X - mx, dy = p.Y - my; sxx += dx * dx; syy += dy * dy; sxy += dx * dy; }
            angleDeg = 0.5 * Math.Atan2(2 * sxy, sxx - syy) * 180 / Math.PI;
            centerX = mx;
            return true;
        }

        public static int CountBlobs(Mat gray, int threshold, int minArea, int maxArea)
        {
            using (var bin = new Mat())
            using (var labels = new Mat())
            using (var stats = new Mat())
            using (var centroids = new Mat())
            {
                Cv2.Threshold(gray, bin, threshold, 255, ThresholdTypes.BinaryInv);   // 어두운 물체를 흰색으로
                int n = Cv2.ConnectedComponentsWithStats(bin, labels, stats, centroids, PixelConnectivity.Connectivity8);
                int count = 0;
                for (int label = 1; label < n; label++)                              // 0은 배경
                {
                    int area = stats.At<int>(label, (int)ConnectedComponentsTypes.Area);
                    if (area >= minArea && area <= maxArea) count++;
                }
                return count;
            }
        }
    }

    public static class KataDay17
    {
        public static void Run()
        {
            int pass = 0;
            for (int i = 0; i < 20; i++)
            {
                using (Mat img = Cv2.ImRead(@"C:\MiniSight\Images\Cam1\" + i.ToString("D3") + ".png", ImreadModes.Grayscale))
                {
                    int expected = 200 + i * 2;
                    Point? edge = Gauges.FindEdge(img, new Point2d(150, 240), new Point2d(300, 240), true, 30);
                    double angle, cx;
                    bool line = Gauges.FitVerticalEdge(img, 230, 170, 310, 10, 60, out angle, out cx);
                    int blobs = Gauges.CountBlobs(img, 128, 500, 100000);
                    bool ok = edge.HasValue && Math.Abs(edge.Value.X - expected) <= 1;
                    if (ok) pass++;
                    Console.WriteLine(i + ": 정답 " + expected + " / 캘리퍼 " + (edge.HasValue ? edge.Value.X.ToString() : "없음")
                        + " / 라인 " + (line ? cx.ToString("0.0") + " @" + angle.ToString("0.0") + "°" : "실패") + " / 블랍 " + blobs);
                }
            }
            Console.WriteLine("정확도 " + pass + "/20");
        }
    }
}
```

생각해 볼 것: 라인 각도가 90°(또는 −90°) 근처로 나온다. 왜? (세로선이니까 점들이 y 방향으로 퍼져 있다.) 각도를 "세로 기준 기울기"로 바꾸려면?

## 6. MiniSight 오늘 단계

1. `Inspection/RuleEngine.cs : IRuleEngine` — `Func<RecipeConfig>`로 게이지 목록(`PointGauge`, `LineGauge`, `BlobGauge` 설정 DTO)을 읽어 측정.
2. 결과 `RuleResult`: `Ok`(모든 게이지가 허용 범위 안), `Message`(게이지별 값), `Measurements` 목록, `ShouldRunAi`.
3. 판정: 레시피에 `ExpectedX ± ToleranceX`를 두고 벗어나면 NG. 합성 이미지의 "구멍" 불량(i%7==6)은 블랍 면적으로 잡히는지 확인(구멍 때문에 면적이 줄어듦).
4. **AI 게이트**: 블랍이 1개 이상일 때만 `ShouldRunAi = true`.
5. 결과 이미지에 측정선을 그리는 `DrawMeasurements(Mat bgr, RuleResult r)` — **입력과 별도의 BGR 사본**에 그린다.

## 7. VS2022 디버깅 실습

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | `FindEdge`의 차이 계산 줄에 **조건부 중단점** `Math.Abs(d) > 100` | 강한 에지에서만 멈춤 |
| 2 | 중단점 → **적중 횟수(Hit Count)** "= 3" | 세 번째 강한 에지에서만 |
| 3 | `values` 리스트에 마우스 → 확대(+) → 인덱스별 값 | 밝기 프로파일을 눈으로 |
| 4 | 직접 실행 창에서 `string.Join(",", values)` 복사 → 엑셀에 붙여 그래프 | **밝기 프로파일 그래프**: 계측 디버깅의 기본기 |
| 5 | 틀린 프레임에서 `Cv2.ImWrite`로 이진 영상 저장 | 임계값이 문제인지 위치가 문제인지 |

## 8. 셀프 체크

1. 캘리퍼에서 "이웃 차이 최댓값"을 쓰는 이유와 단점은?
<details><summary>답</summary>가장 강한 밝기 변화가 실제 경계일 가능성이 높고 계산이 단순하다. 단점은 잡음 하나가 더 크게 튀면 거기를 고르고, 정수 픽셀 정밀도에 머문다. 최소 차이 기준·평활화·서브픽셀 보간으로 보완한다.</details>

2. 직선 피팅에 y = ax + b 최소제곱 대신 PCA(공분산)를 쓰는 이유는?
<details><summary>답</summary>최소제곱은 세로에 가까운 선에서 기울기가 발산하고 y 방향 오차만 최소화한다. PCA는 점까지의 수직 거리를 최소화하는 방향을 주므로 각도와 무관하게 안정적이다.</details>

3. 라인 게이지에 최소 성공 캘리퍼 수가 필요한 이유는?
<details><summary>답</summary>대부분 캘리퍼가 실패하고 몇 개만 잡음에서 성공하면 엉뚱한 선이 나온다. 그 값이 PLC 위치 제어에 쓰이면 설비 오동작이 된다. 충분한 근거가 없으면 "못 찾음"이 안전하다.</details>

4. `ConnectedComponentsWithStats` 결과에서 label 0은?
<details><summary>답</summary>배경(0인 픽셀)이다. 실제 객체는 1부터 n−1까지다. 그래서 반복은 1부터 한다.</details>

5. 블랍 결과로 AI 실행을 건너뛰는 설계의 장점과 위험은?
<details><summary>답</summary>장점: 빈 프레임에 무거운 AI를 돌리지 않아 처리량과 GPU 부하가 줄어든다. 위험: 블랍 조건이 너무 엄격하면 실제 대상이 있는 프레임을 AI가 못 본다(미검). 그래서 생략한 프레임도 "AI SKIP" 결과로 기록·보고한다.</details>

## 9. 블로그 포인트

- 제목 예: `[머신비전 기초] 캘리퍼와 PCA 라인 피팅을 C#으로 직접 구현해 보기`
- 밝기 프로파일 그래프, 정확도 20/20 결과, 공분산 각도 공식 그림. 합성 이미지라 공개해도 안전.
