# OpenCvSharp / 영상처리 치트시트

## 1. Mat 기본

| 항목 | 코드 | 메모 |
|---|---|---|
| 생성 | `new Mat(rows, cols, MatType.CV_8UC1, Scalar.All(0))` | rows=높이, cols=너비 |
| 컬러 순서 | BGR | `new Scalar(255,0,0)` = 파랑 |
| 정보 | `Width, Height, Channels(), Step(), IsContinuous(), Type()` | Step ≥ Width×Channels |
| 픽셀 | `m.At<byte>(y, x)`, `m.Set<byte>(y, x, v)` | 느림. 캘리퍼 정도만 |
| 래퍼 | `Mat.FromPixelData(h, w, type, IntPtr, step)` | 복사 없음, **소유 안 함** |
| 사본 | `m.Clone()` | 독립, 연속 메모리 |
| ROI | `new Mat(m, rect)` | **메모리 공유**. 독립 필요하면 `.Clone()` |
| 해제 | `using` / `Dispose()` | 네이티브 메모리. GC 믿지 않기 |
| 저장/읽기 | `Cv2.ImWrite(path, m)`, `Cv2.ImRead(path, ImreadModes.Grayscale)` | |
| Bitmap | `BitmapConverter.ToBitmap(m)` (OpenCvSharp.Extensions) | 전체 복사, Bitmap도 Dispose |
| managed 배열 고정 | `GCHandle.Alloc(arr, GCHandleType.Pinned)` → `AddrOfPinnedObject()` → `Free()` | |

메모리 계산: `폭 × 높이 × 채널 × 동시 사본 수`. 2000×1000 BGR = 6MB.

## 2. OmniSight 전처리 순서

```text
원본 Clone → 필터 체인(Step 오름차순) → 반전 → 좌우(Flip Y) → 상하(Flip X) → 회전 → Crop → Resize
```
`OmniSight/Vision/Processing/RecipeImageProcessor.cs:26-67`, `OmniSight/Vision/Processing/InputImageTransformer.cs:28-119`

## 3. 필터 요약

| 필터 | OpenCvSharp | 핵심 파라미터 | 용도 |
|---|---|---|---|
| 고정 이진화 | `Cv2.Threshold(src, dst, th, 255, Binary)` | th | 조명 균일 |
| 적응형 이진화 | `Cv2.AdaptiveThreshold(src, dst, 255, MeanC/GaussianC, Binary, block, C)` | block 홀수 ≥3 | 조명 불균일 |
| Sobel | `Cv2.Sobel(src, dst, CV_8U, dx, dy, ksize)` | ksize 홀수 1~31 | 방향 에지 (8U면 음수 잘림) |
| Laplacian | `Cv2.Laplacian(src, dst, CV_8U, ksize)` | | 점·작은 결함 |
| Canny | `Cv2.Canny(src, dst, th1, th2, 3)` | 두 임계 | 얇은 윤곽 |
| Scharr | `Cv2.Scharr(src, dst, CV_8U, dx, dy)` | dx+dy=1 | 정밀 3×3 미분 |
| Dilate/Erode | `Cv2.Dilate/Erode(src, dst, kernel, null, iter, BorderTypes.Replicate)` | 3×3 Rect, 반복 | 끊김 연결 / 잡음 제거 |
| 산술 | `Cv2.Add/Subtract/Multiply/Divide/Compare/BitwiseAnd...` | 상수 또는 원본 | 배경 빼기, 마스크 |
| 회전 | `Cv2.Rotate(src, dst, Rotate90Clockwise)` / `GetRotationMatrix2D` + `WarpAffine` | | 임의 각도는 모서리 잘림 |
| 크기 | `Cv2.Resize(src, dst, new Size(w, h))` | | |
| 색 변환 | `Cv2.CvtColor(src, dst, ColorConversionCodes.BGR2GRAY)` | | |

## 4. 게이지 알고리즘

**Point(캘리퍼)** — `OmniSight/Application/Inspection/RecipeRuleInspectionEngine.cs:245-295,614-668`
1. 원점에서 각도 방향으로 ±Tolerance 선분.
2. 선분 위 정수 좌표를 순서대로 샘플(중복 좌표 제거).
3. 이웃 밝기 차이 최댓값(방향에 따라 +/−), 최소 차이 이상.
4. 이진 영상에서 실패하면 회색조로 재시도.

**Line(다중 캘리퍼 + PCA)** — 같은 파일 `:337-432`
1. 축 방향으로 Sampling+1개 위치, 각 위치에서 **법선 방향** 캘리퍼.
2. 성공 점 수 ≥ max(2, (Length/Sampling)/2 + 1).
3. 평균 (x̄, ȳ), Sxx, Syy, Sxy → θ = ½·atan2(2Sxy, Sxx − Syy).
4. 투영 최소/최대로 끝점.

**Object(블랍)** — 같은 파일 `:434-599`
1. ROI → `InRange(low, high)` 또는 `Threshold(Binary/BinaryInv)`.
2. `Cv2.ConnectedComponentsWithStats(bin, labels, stats, centroids, Connectivity8)`; label 0 = 배경.
3. `stats.At<int>(label, (int)ConnectedComponentsTypes.Area)`로 면적 필터.
4. 개수 ≥ 최소면 AI 실행(`ShouldRunAi`).

## 5. Mat 소유권 체크리스트

- [ ] SDK 버퍼는 콜백 안에서 복사했나?
- [ ] 래퍼가 원본보다 오래 살지 않나?
- [ ] 단계마다 이전 중간 결과를 Dispose했나?
- [ ] 실패·취소·큐 포화 경로에서도 Dispose하나?
- [ ] 다른 스레드/큐로 넘길 땐 Clone했나? 넘긴 뒤 로컬 null?
- [ ] 이벤트로 받은 Mat을 핸들러 밖에서 쓰지 않나?
- [ ] 룰과 AI가 같은 Mat을 읽기만 하나? (그리기는 사본에)
- [ ] Bitmap 교체 시 새 것 먼저 넣고 옛것 Dispose?

## 6. 디버깅 요령

- 직접 실행 창: `Cv2.ImWrite(@"C:\MiniSight\dbg.png", m)`.
- 밝기 프로파일: 캘리퍼 values를 `string.Join(",", values)`로 복사 → 엑셀 그래프.
- 결과가 다르면: 입력 동일? 순서 동일? 타입(8U vs 16S) 동일? 경계 처리(BorderTypes) 동일? 커널 크기 보정 동일?
- 성능: Release + 프로파일러. 픽셀 루프는 `At<T>` 대신 인덱서/포인터, 가능하면 Cv2 함수 하나로.
