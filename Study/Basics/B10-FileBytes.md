# B10 · 파일과 바이트 (`Path` `File` `Directory` `byte[]` `BitConverter` 엔디언 `Buffer.BlockCopy`) · 35분

## 1. 오늘 목표
결과 CSV를 쓰고 읽는다. 숫자를 **바이트로 바꿔 패킷을 조립하고 다시 푸는** 기초를 손으로 해 본다. OmniSight에서 `Path.`/`File.`/`Directory.`는 합쳐서 678번, `byte[]`/`BitConverter`/`Buffer.BlockCopy`는 108번 나온다. PLC 통신(Day22)의 바닥이다.

## 2. 🧒 비유
- **Path.Combine = 주소 붙여 쓰기**: "서울시" + "강남구" + "1번지"를 사이에 `\`를 알맞게 넣어 이어 준다.
- **byte = 칸 하나에 0~255 숫자 하나**인 작은 상자. `int` 숫자 하나는 상자 **4개**에 나눠 담는다.
- **엔디언 = 상자를 놓는 순서**: 1000은 16진수로 `00 00 03 E8`이다.
  - **빅 엔디언**: 큰 자리부터 `00 00 03 E8` (사람이 읽는 순서, 네트워크 표준).
  - **리틀 엔디언**: 작은 자리부터 `E8 03 00 00` (Windows PC 내부 순서).
  - 보내는 쪽과 받는 쪽 순서가 다르면 1000이 **-402456576** 같은 엉뚱한 숫자가 된다.

## 3. 키워드 표

| 키워드 | 역할 | 사용법 | 기억할 점 |
|---|---|---|---|
| `Path.Combine(a, b)` | 경로 이어 붙이기 | `Path.Combine(dir, "result.csv")` | `a + "\\" + b`로 직접 붙이지 않는다 |
| `Path.GetFileName` / `GetExtension` / `GetTempPath` | 이름만 / 확장자 / 임시 폴더 | | |
| `Directory.CreateDirectory(dir)` | 폴더 만들기 (있으면 그냥 넘어감) | | 상위 폴더까지 한 번에 |
| `File.WriteAllText(path, text, Encoding.UTF8)` | 파일 새로 쓰기 (있으면 덮어씀) | | |
| `File.AppendAllText` | 끝에 덧붙이기 | | 로그·CSV |
| `File.ReadAllLines(path, Encoding.UTF8)` | 모든 줄을 `string[]`으로 | | |
| `File.Exists(path)` / `File.Delete(path)` | 있나? / 지우기 | | |
| `Encoding.UTF8` / `Encoding.ASCII` | 글자 ↔ 바이트 변환 규칙 | `Encoding.ASCII.GetBytes("MS")` | 한글은 UTF8. 장비 프로토콜은 보통 ASCII |
| `byte[]` | 바이트 배열 | `new byte[6]` | |
| `0x3E8` | 16진수 숫자 표기 (= 1000) | | 패킷 문서는 거의 16진수 |
| `BitConverter.GetBytes(int)` | 숫자 → 바이트 4개 (**PC 순서**) | | |
| `BitConverter.ToInt32(bytes, 시작위치)` | 바이트 4개 → 숫자 | | |
| `BitConverter.ToString(bytes)` | `E8-03-00-00`처럼 보기 좋게 | | 로그·디버깅용 |
| `BitConverter.IsLittleEndian` | 이 PC가 리틀 엔디언인가? | | Windows PC는 true |
| `Array.Reverse(arr)` | 배열 순서 뒤집기 | | 엔디언 바꾸기 |
| `Buffer.BlockCopy(원본, 시작, 대상, 시작, 개수)` | 바이트 배열 일부를 복사 | | 패킷 조립·분해 |

## 4. 코드: `Basics/KataB10.cs`

```csharp
using System;
using System.IO;
using System.Text;

namespace Katas.Basics
{
    public static class KataB10
    {
        public static void Run()
        {
            // 1) 경로 만들기
            string dir = Path.Combine(Path.GetTempPath(), "MiniSightKata");
            Directory.CreateDirectory(dir);
            string file = Path.Combine(dir, "result.csv");
            Console.WriteLine("파일 이름만: " + Path.GetFileName(file));
            Console.WriteLine("확장자:     " + Path.GetExtension(file));

            // 2) 쓰기 / 덧붙이기 / 읽기
            File.WriteAllText(file, "Id,Judge\r\n", Encoding.UTF8);
            File.AppendAllText(file, "1,OK\r\n", Encoding.UTF8);
            File.AppendAllText(file, "2,NG\r\n", Encoding.UTF8);
            string[] lines = File.ReadAllLines(file, Encoding.UTF8);
            Console.WriteLine("줄 수: " + lines.Length);
            foreach (string line in lines)
            {
                Console.WriteLine("  " + line);
            }
            Console.WriteLine("파일 있나? " + File.Exists(file));

            // 3) 숫자 -> byte[] (엔디언)
            int value = 1000;                                   // 16진수 0x000003E8
            byte[] little = BitConverter.GetBytes(value);
            Console.WriteLine("PC 순서(리틀 엔디언):     " + BitConverter.ToString(little));

            byte[] big = BitConverter.GetBytes(value);
            Array.Reverse(big);                                 // 순서 뒤집기
            Console.WriteLine("네트워크 순서(빅 엔디언): " + BitConverter.ToString(big));
            Console.WriteLine("이 PC는 리틀 엔디언? " + BitConverter.IsLittleEndian);

            // 4) 패킷 조립: 머리 "MS"(2바이트) + 값(4바이트)  ← 연습용으로 지어낸 형식
            byte[] head = Encoding.ASCII.GetBytes("MS");
            byte[] packet = new byte[head.Length + big.Length];
            Buffer.BlockCopy(head, 0, packet, 0, head.Length);
            Buffer.BlockCopy(big, 0, packet, head.Length, big.Length);
            Console.WriteLine("보낼 패킷: " + BitConverter.ToString(packet));

            // 5) 받은 쪽: 패킷에서 값 꺼내기
            byte[] valuePart = new byte[4];
            Buffer.BlockCopy(packet, 2, valuePart, 0, 4);       // 2번 칸부터 4개
            Array.Reverse(valuePart);                           // 빅 → 리틀로 되돌림
            int back = BitConverter.ToInt32(valuePart, 0);
            Console.WriteLine("되읽은 값: " + back);

            File.Delete(file);
        }
    }
}
```

## 5. 실행 결과 (검증함)
```text
파일 이름만: result.csv
확장자:     .csv
줄 수: 3
  Id,Judge
  1,OK
  2,NG
파일 있나? True
PC 순서(리틀 엔디언):     E8-03-00-00
네트워크 순서(빅 엔디언): 00-00-03-E8
이 PC는 리틀 엔디언? True
보낼 패킷: 4D-53-00-00-03-E8
되읽은 값: 1000
```
👀 `4D-53`은 글자 `M`, `S`의 ASCII 코드다. 패킷 캡처(Wireshark)로 보면 이렇게 16진수로 보인다.

## 6. 바꿔 보기 (1개)
5)의 `Array.Reverse(valuePart);` 줄을 주석 처리하고 실행한다.
→ 되읽은 값이 1000이 아니라 **-402456576** 같은 엉뚱한 숫자가 된다. **PLC와 값이 안 맞을 때 가장 먼저 의심할 것이 엔디언**이다. 확인했으면 되돌린다.

## 7. 어디서 다시 나오나
| Day | 코드 | 뜻 |
|---|---|---|
| Day04 | `Directory.GetFiles`, `Path.GetFileName` | 폴더 카메라 |
| Day22 | PLC 프레이밍, 엔디언 | 이 Kata의 본편 |
| Day23 | 파일 준비 확인, 원자적 저장 (임시 파일 → 이름 바꾸기) | 쓰다가 꺼져도 파일이 안 깨지게 |
| Day24 | 비동기 로거 | AppendAllText를 매번 하지 않는 이유 |

## 8. 셀프 체크
<details><summary>Q1. PLC에서 받은 4바이트 값이 엉뚱하게 크거나 음수로 나온다. 무엇부터 확인하나?</summary>

엔디언(바이트 순서)이다. 보내는 쪽이 빅 엔디언이면 받는 쪽에서 뒤집어야 한다. 그다음 시작 위치(오프셋)와 길이(2바이트인지 4바이트인지)를 확인한다.
</details>

<details><summary>Q2. 경로를 dir + "\\" + name 대신 Path.Combine으로 만드는 이유는?</summary>

dir 끝에 `\`가 있든 없든 알맞게 이어 준다. 직접 붙이면 `\\`가 두 번 들어가거나 빠지는 실수가 생긴다.
</details>

## 9. 체크
- [ ] 1번째: 보면서 타이핑 → 실행
- [ ] 2번째: 실행 결과 비교
- [ ] 3번째: 지우고 안 보고 다시 타이핑
- [ ] 바꿔 보기
