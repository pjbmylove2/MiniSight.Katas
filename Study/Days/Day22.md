# Day 22 (월, 2h) · PLC와 대화하기: TCP, 프레이밍, 엔디언, 하트비트, 재연결

> ⚠️ 회사 PLC 패킷의 필드 순서·바이트 구성은 **블로그 금지**. MiniSight는 **자체 프로토콜**을 설계해서 쓴다.

## 1. 오늘 목표

- **완료 기준**
  - "TCP는 바이트 **흐름**이지 메시지가 아니다"를 설명하고, 한 번의 `Read`에 반 개/두 개 반 메시지가 올 수 있음을 실험으로 확인했다.
  - 프레이밍 방식 2가지(**구분자**: CRLF 등 / **길이 접두**: 헤더에 길이)의 장단점, 그리고 **재동기화**(어긋나면 1바이트 버리고 다시 찾기)를 설명할 수 있다.
  - 리틀/빅 엔디언, `BitConverter.IsLittleEndian`, PLC와 바이트 순서가 다를 때 처리법을 말할 수 있다.
  - 연결 루프(연결 → 송신 루프 + 수신 루프 → 끊기면 정리 → 1초 후 재시도), **하트비트**, **응답 타임아웃**, **연결 세대 번호**를 설명할 수 있다.
  - MiniSight: `Tools/FakePlcServer`(TcpListener 콘솔)와 `PlcClient`(재연결, 하트비트, 수신 재동기화)를 만들어 서로 통신시켰다.
  - VS2022에서 **두 프로젝트 동시 디버깅**을 했다.
- **최소분량**: 2, 3, 4.1~4.3, 5, 6.1~6.2
- **확장분량**: 6.3 세대 번호, 7번 Attach to Process

## 2. 🧒 초1 비유

- **TCP = 국수 가락처럼 이어진 편지**. 편지 세 통을 보내도 받는 쪽엔 글자가 **한 줄로 쭉** 온다. 어디서 한 통이 끝나는지는 우리가 약속해야 한다.
- **구분자 프레이밍 = 편지 끝에 "끝." 쓰기**. "끝."을 찾으면 한 통. 편지 안에 "끝."이 들어가면 곤란하다.
- **길이 프레이밍 = 봉투에 "이 편지는 20글자" 쓰기**. 20글자를 다 받을 때까지 기다린다. 길이 숫자가 깨지면 엉뚱하게 읽는다 → 시작 표시(매직 바이트)로 확인.
- **재동기화 = 줄을 잘못 읽었으면 한 글자 건너뛰고 다시 "편지 시작" 찾기**.
- **엔디언 = 숫자 쓰는 방향**. 1234를 "12 34"로 쓰는 나라(빅)와 "34 12"로 쓰는 나라(리틀). PC(x86)는 리틀. PLC는 기종마다 다르다.
- **하트비트 = "나 살아 있어" 손 흔들기**. 일정 시간 손짓이 없으면 전화가 끊긴 걸로 본다.
- **연결 세대 번호 = 전화 번호표**. 옛날에 건 전화가 늦게 연결되면 "이건 지난 번호표네" 하고 끊는다.

## 3. OmniSight 지도

| 볼 것 | 위치 | 볼 포인트 |
|---|---|---|
| 부팅 때 통신 시작 | `OmniSight/Devices/LegacyOcrSocketDevice.cs:80-97` | 검사 Start와 무관하게 Initialize에서 루프 시작, linked token |
| 연결 루프 | `OmniSight/Devices/LegacyOcrSocketDevice.cs:345-463` | 세대 번호 확인 → 수신 Task → 송신/하트비트/타임아웃 루프 → finally 정리 → 1초 후 재시도 |
| 구세대 소켓 폐기 | `OmniSight/Devices/LegacyOcrSocketDevice.cs:361-377` | lock 안에서 세대 비교 후 공개 |
| 하트비트 | `OmniSight/Devices/LegacyOcrSocketDevice.cs:407-415` | 비트 토글 + 최신 결과 재전송 |
| 응답 타임아웃 | `OmniSight/Devices/LegacyOcrSocketDevice.cs:417-424` | 마지막 수신 시각(Interlocked long ticks) |
| 수신 루프 | `OmniSight/Devices/LegacyOcrSocketDevice.cs:465-512` | `ReadAsync` → pending 버퍼 → 길이+CRLF 확인 → 어긋나면 1바이트 제거(재동기화) → 스냅샷 `Interlocked.Exchange` |
| 연결 타임아웃 | `OmniSight/Devices/LegacyOcrSocketDevice.cs:515-614` | Day12 복습 |
| 프로토콜 | `OmniSight/Devices/LegacySocketProtocol.cs` | 활성 필드만 이어 붙이기, `BitConverter`, ASCII 정리 |
| 송신 큐 | `OmniSight/Devices/LegacyOcrSocketDevice.cs` (`_sendQueue`) | `BlockingCollection(256)` + `TryAdd` |
| 스냅샷 공유 | `OmniSight/Devices/LegacyOcrSocketDevice.cs:57` | `Volatile.Read(...).Copy()` |
| 장비 통신 스킬 | `.claude/skills/equipment-communication/SKILL.md` | 사내 분석 체크리스트 |

**집에서 Claude에게:**
```text
LegacyOcrSocketDevice.cs 345~512줄을 국수 편지·번호표 비유로 설명해줘. 패킷 필드 구성은 말하지 말고
구조(연결 루프, 수신 재동기화, 하트비트, 타임아웃, 세대 번호)만. 특히 송신 루프가 TryTake(5ms)로
폴링하는 이유와 그 비용, finally에서 reader를 await하는 이유를 알려줘.
```

## 4. 왜 이렇게 썼나

### 4.1 프레이밍 비교

| | 구분자 (CRLF) | 길이 접두 |
|---|---|---|
| 구현 | 쉬움 | 조금 복잡 |
| 바이너리 값에 구분자 바이트가 섞이면 | **오인식 위험** → 고정 길이와 함께 검사(OmniSight는 "길이 위치에 CRLF가 있나"로 확인) | 문제 없음 |
| 재동기화 | 다음 구분자까지 | 매직 바이트 + 길이 범위 검사 |
| PLC 호환 | PLC 사다리 프로그램에서 만들기 쉬움 | PLC 쪽 구현 부담 |

### 4.2 결정과 이유

| 결정 | 이유 |
|---|---|
| 통신은 부팅 때 시작, 검사 Start와 분리 | PLC가 Start 명령을 보내려면 이미 연결돼 있어야 함 |
| 수신은 별도 Task, 송신은 루프 | 수신은 언제 올지 모르는 블로킹 Read, 송신은 큐·하트비트·타임아웃을 한 곳에서 관리 |
| 받은 바이트를 pending에 쌓기 | Read 한 번 = 패킷 하나가 아님 |
| 어긋나면 1바이트 버리고 알람 | 영원히 틀린 위치로 읽는 것보다 한 패킷 손실이 낫다. 알람으로 PLC 설정 불일치를 알림 |
| 수신 스냅샷을 통째로 교체 | 카메라 스레드가 LOT/카운트를 lock 없이 일관되게 읽음 |
| 응답 타임아웃 → 예외 → 재연결 | 반쯤 죽은 연결(케이블 뽑힘, PLC 정지)은 TCP가 한참 뒤에야 알려줌 |
| 세대 번호 | 설정 변경/Close 중에 늦게 성공한 연결이 새 연결을 덮지 않게 |
| finally에서 Close 후 reader await | 수신 Task가 끝난 걸 확인해야 다음 연결과 섞이지 않고, 미관찰 예외가 안 남음 |

### 4.3 시니어의 눈

- 송신 루프의 `TryTake(…, 5ms)`는 송신할 게 없어도 **5ms마다 깨어나는 폴링**이다. 스레드풀 스레드 하나를 거의 계속 쓴다. 대안: 큐 대기와 하트비트 타이머를 `Task.WhenAny`로 합치거나 `SemaphoreSlim.WaitAsync(timeout)`.
- `NetworkStream.ReadAsync`의 취소 토큰은 .NET Framework에서 **실제로 Read를 깨우지 못하는 경우**가 있다. 그래서 소켓 Close로 깨우는 방식이 같이 필요하다 (OmniSight finally의 Close).
- `List<byte>.RemoveAt(0)`은 O(n). 고속 통신이면 링 버퍼나 인덱스 이동.
- `BitConverter`는 **머신 엔디언**을 따른다. PLC가 빅 엔디언이면 `Array.Reverse` 또는 `IPAddress.HostToNetworkOrder`. 반드시 실제 패킷 캡처로 확인 (equipment-communication 스킬 원칙: 추측 금지).

## 5. Kata: 국수 편지 자르기 (프레이밍 파서)

```csharp
// KATA: Day22
using System;
using System.Collections.Generic;
using System.Text;

namespace Katas.Day22
{
    // MiniSight 자체 프레임: [0xA5][0x5A][type:1][len:2 LE][payload:len][checksum:1 = 앞 모든 바이트 XOR]
    public static class MiniFrame
    {
        public const byte Magic1 = 0xA5, Magic2 = 0x5A;
        public const int HeaderLength = 5;

        public static byte[] Build(byte type, byte[] payload)
        {
            var frame = new byte[HeaderLength + payload.Length + 1];
            frame[0] = Magic1; frame[1] = Magic2; frame[2] = type;
            byte[] len = BitConverter.GetBytes((ushort)payload.Length);
            if (!BitConverter.IsLittleEndian) Array.Reverse(len);      // 항상 리틀 엔디언으로 보냄
            frame[3] = len[0]; frame[4] = len[1];
            Buffer.BlockCopy(payload, 0, frame, HeaderLength, payload.Length);
            byte x = 0;
            for (int i = 0; i < frame.Length - 1; i++) x ^= frame[i];
            frame[frame.Length - 1] = x;
            return frame;
        }
    }

    public sealed class FrameParser
    {
        private readonly List<byte> _pending = new List<byte>();
        public int Resyncs { get; private set; }

        // 받은 조각을 넣으면 완성된 프레임들을 돌려준다
        public List<KeyValuePair<byte, byte[]>> Feed(byte[] chunk, int count)
        {
            var frames = new List<KeyValuePair<byte, byte[]>>();
            for (int i = 0; i < count; i++) _pending.Add(chunk[i]);

            while (_pending.Count >= MiniFrame.HeaderLength + 1)
            {
                if (_pending[0] != MiniFrame.Magic1 || _pending[1] != MiniFrame.Magic2) { Resync(); continue; }
                int len = _pending[3] | (_pending[4] << 8);
                if (len > 1024) { Resync(); continue; }                      // 말이 안 되는 길이
                int total = MiniFrame.HeaderLength + len + 1;
                if (_pending.Count < total) break;                              // 반쪽: 더 기다림
                byte x = 0;
                for (int i = 0; i < total - 1; i++) x ^= _pending[i];
                if (x != _pending[total - 1]) { Resync(); continue; }           // 체크섬 불일치
                byte[] payload = _pending.GetRange(MiniFrame.HeaderLength, len).ToArray();
                frames.Add(new KeyValuePair<byte, byte[]>(_pending[2], payload));
                _pending.RemoveRange(0, total);
            }
            return frames;
        }

        private void Resync() { _pending.RemoveAt(0); Resyncs++; }
    }

    public static class KataDay22
    {
        public static void Run()
        {
            var stream = new List<byte>();
            stream.AddRange(new byte[] { 0x11, 0x22 });                          // 앞에 쓰레기
            stream.AddRange(MiniFrame.Build(1, Encoding.ASCII.GetBytes("START")));
            stream.AddRange(MiniFrame.Build(2, Encoding.ASCII.GetBytes("LOT-0001")));
            stream.AddRange(MiniFrame.Build(3, BitConverter.GetBytes(1234)));
            byte[] all = stream.ToArray();

            var parser = new FrameParser();
            var rnd = new Random(7);
            int offset = 0;
            while (offset < all.Length)                                           // TCP처럼 아무렇게나 잘라서
            {
                int size = Math.Min(rnd.Next(1, 7), all.Length - offset);
                var chunk = new byte[size];
                Array.Copy(all, offset, chunk, 0, size);
                offset += size;
                foreach (var f in parser.Feed(chunk, size))
                {
                    string text = f.Key == 3 ? BitConverter.ToInt32(f.Value, 0).ToString() : Encoding.ASCII.GetString(f.Value);
                    Console.WriteLine("조각 " + size + "B 받음 → 프레임 type=" + f.Key + " : " + text);
                }
            }
            Console.WriteLine("재동기화 " + parser.Resyncs + "회 (앞 쓰레기 2바이트)");
            Console.WriteLine("이 PC는 리틀 엔디언? " + BitConverter.IsLittleEndian + ", 1234 = " + BitConverter.ToString(BitConverter.GetBytes(1234)));
        }
    }
}
```

## 6. MiniSight 오늘 단계

### 6.1 `Tools/FakePlcServer` (콘솔, .NET Framework 4.7.2)
- `TcpListener(IPAddress.Loopback, 5100)`, 클라이언트 하나 수락.
- 키보드: `S` → Start 명령 프레임, `T` → Stop, `L` → LOT 변경, `Q` → 연결 끊기(재연결 시험).
- 1초마다 상태 프레임(type 0x10, 라인 속도 int 등) 송신 = MiniSight 입장에선 "PLC 하트비트".
- 받은 결과 프레임을 콘솔 출력.

### 6.2 `Devices/Plc/PlcClient.cs : DeviceBase`
- `OnInitialize`: linked CTS, `RunAsync` 루프 시작 (검사 Start와 무관).
- `RunAsync`: 연결(Day12 `TimeoutHelper`) → 수신 Task(`FrameParser`) → 송신 루프(`BlockingCollection<byte[]>(256)`, `TryAdd` 실패 시 드롭 카운트) → 마지막 수신 후 3초 지나면 `TimeoutException` → finally Close + 수신 Task await → 1초 후 재시도.
- 이벤트: `CommandReceived(byte type, byte[] payload)`, `ConnectionChanged(bool)`.
- `SendResult(InspectionResult r)`: 결과 프레임(type 0x20)을 큐에.
- VisionSystem 연결: `CommandReceived`에서 Start/Stop을 **UI가 아니라 VisionSystem에** 전달(내부 lock이 직렬화). UI 표시는 상태 이벤트로.

### 6.3 (확장) 연결 세대 번호
- `ReconnectNow()`(설정 변경) 호출 시 `Interlocked.Increment(ref _generation)` → 루프는 자기 세대와 다르면 연결을 공개하지 않고 닫는다.

## 7. VS2022 디버깅 실습: 두 프로세스 동시에

| 단계 | 할 일 | 관찰 |
|---|---|---|
| 1 | 솔루션 우클릭 → 속성 → 공용 속성 → 시작 프로젝트 → **여러 개의 시작 프로젝트**: FakePlcServer, MiniSight 둘 다 "시작" | F5 한 번에 둘 다 디버깅 |
| 2 | 서버의 송신 줄과 클라이언트 `Feed`에 중단점 | 두 프로세스를 오가며 멈춤. 디버그 위치 도구 모음에서 **프로세스 선택** 드롭다운 |
| 3 | 서버를 디버거 없이 Ctrl+F5로 띄우고, 디버그 → **프로세스에 연결(Ctrl+Alt+P)** → FakePlcServer.exe 선택 | 이미 실행 중인 프로그램에 붙기. 현장 장비 PC 원격 디버깅의 기초 |
| 4 | 서버에서 `Q`로 연결 끊기 | 클라이언트 재연결 로그, 병렬 스택에서 수신 Task가 끝나고 재시도 대기 |
| 5 | (확장) 서버가 일부러 매직 바이트를 틀리게 보내게 하고 `Resyncs` 조사식 | 재동기화 동작 |

## 8. 셀프 체크

1. TCP에서 `Read` 한 번이 메시지 하나라고 가정하면 어떤 버그가 생기나?
<details><summary>답</summary>네트워크 상황에 따라 메시지가 쪼개지거나 여러 개가 붙어서 온다. 반쪽 메시지를 해석하거나 두 번째 메시지를 버리게 된다. 테스트(로컬)에서는 잘 되다가 현장(부하·지연)에서만 간헐적으로 깨진다.</details>

2. 구분자 프레이밍에서 바이너리 숫자 안에 CR/LF 바이트가 섞이면?
<details><summary>답</summary>값의 일부를 프레임 끝으로 오인해 잘못 자른다. 고정 길이와 "정해진 위치에 구분자가 있는지"를 함께 검사하거나, 길이 접두 + 체크섬 방식을 쓴다.</details>

3. 하트비트와 응답 타임아웃이 둘 다 필요한 이유는?
<details><summary>답</summary>하트비트는 상대에게 내가 살아 있음을 알리고, 응답 타임아웃은 상대가 죽었음을 내가 감지한다. TCP는 케이블이 뽑혀도 송신을 시도하기 전엔 오래 알아차리지 못하므로 애플리케이션 수준 감시가 필요하다.</details>

4. 연결 세대 번호가 없으면 생길 수 있는 경쟁은?
<details><summary>답</summary>설정을 바꿔 새 연결을 시작했는데, 이전 설정으로 시도하던 연결이 늦게 성공해 공유 필드를 덮어쓴다. 결과적으로 옛 주소로 통신하거나 소켓이 두 개 열린다.</details>

5. PLC와 PC의 엔디언이 다르면 어떻게 처리하나?
<details><summary>답</summary>통신 계층에서만 변환한다. 정수를 바이트로 만들 때 `BitConverter.GetBytes` 후 필요하면 `Array.Reverse`, 또는 시프트 연산으로 직접 조립한다. 실제 패킷 캡처로 바이트 순서를 확인하고 단위 테스트로 고정한다.</details>

## 9. 블로그 포인트

- 제목 예: `[C# 소켓] TCP는 메시지가 아니다: 프레이밍, 재동기화, 하트비트, 재연결 직접 만들기`
- MiniSight 자체 프레임 구조, 랜덤 조각 파싱 결과, 두 프로세스 동시 디버깅 팁. **회사 패킷 구조는 절대 금지.**
