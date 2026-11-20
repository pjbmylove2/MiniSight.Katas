using System;

namespace Katas.L0
{
    public static class D002
    {
        public static void Run()
        {
            Chunk0();
            Chunk1();
            Chunk2();
            Chunk3();
            Chunk4();
            Chunk5();
            Chunk6();
        }

        public static void Chunk0()
        {
            int? a = 5;
            int? b = a;
            b = 9;
            Console.WriteLine("a=" + a + " b=" + b);
            Console.WriteLine("int? 값 형식? " + typeof(int?).IsValueType);

            int[] x = { 5 };
            int[] y = x;
            y[0] = 9;
            Console.WriteLine("x[0]=" + x[0] + " y[0]=" + y[0]);
            Console.WriteLine("int[] 값 형식? " + typeof(int[]).IsValueType);
        }

        // 조각 1: 상속 = 부모가 가진 것을 물려받는다
        public static void Chunk1()
        {
            AreaCam cam = new AreaCam("CAM1", 1920);
            Console.WriteLine(cam.Name);
            Console.WriteLine(cam.Describe());
            Console.WriteLine("가로 픽셀: " + cam.Width);
        }

        // 조각 2: virtual/override = 같은 이름, 자식마다 다른 동작
        public static void Chunk2()
        {
            CamBase[] cams = { new AreaCam("CAM1", 1920), new LineCam("CAM2"), new CamBase("CAM3") };
            foreach (CamBase cam in cams)
            {
                Console.WriteLine(cam.Grab());
            }
        }

        // 조각 3: abstract = 순서는 부모가, 빈칸은 자식이
        public static void Chunk3()
        {
            DeviceBase ok = new LightDevice("LIGHT1", true);
            DeviceBase ng = new LightDevice("LIGHT2", false);
            Console.WriteLine(ok.Start());
            Console.WriteLine(ng.Start());
        }

        // 조각 4: interface = "이것만 할 줄 알면 된다"는 약속
        public static void Chunk4()
        {
            ICam[] cams = { new FakeCam("SIM1"), new AreaCam("CAM1", 1920) };
            foreach (ICam cam in cams)
            {
                Console.WriteLine(cam.Grab());
            }
            Console.WriteLine(cams[0].Grab());
        }

        // 조각 5: sealed = 여기서 상속 끝
        public static void Chunk5()
        {
            CamBase cam = new SimAreaCam("SIM2");
            Console.WriteLine(cam.Grab());
            // ⑪: "이 객체를 AreaCam으로 볼 수 있나?"를 true/false로 묻는 키워드
            Console.WriteLine("AreaCam인가? " + (cam is AreaCam));
            Console.WriteLine("ICam인가? " + (cam is ICam));
        }

        // 조각 6: 예외도 상속이다 + 부모 변수도 같은 객체를 가리킨다
        public static void Chunk6()
        {
            try
            {
                CheckName(null);
            }
            // ⑫: ArgumentNullException의 부모 예외 형식 (부모로 자식 예외를 잡는다)
            catch (ArgumentException ex)
            {
                Console.WriteLine("부모로 잡음: " + ex.GetType().Name);
            }

            AreaCam a = new AreaCam("CAM1", 1920);
            CamBase b = a;
            // ⑬: a와 b가 같은 객체를 가리키는지 true/false로 알려 주는 메서드 호출
            Console.WriteLine("같은 객체? " + ReferenceEquals(a, b));
        }

        private static void CheckName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
        }
    }

        // ⑦: 구현 없이 "이것만 할 줄 알면 된다"는 약속(멤버 목록)만 담는 형식
        public interface ICam
        {
            string Name { get; }
            string Grab();
        }

    // ⑧: CamBase가 ICam 약속을 지킨다고 선언한다
    public class CamBase : ICam
    {
        public string Name { get; }

        public CamBase(string name)
        {
            Name = name;
        }

        public string Describe() => "카메라 " + Name;

        // ③: "자식이 이 메서드를 바꿔도 된다"고 부모가 허락하는 키워드
        public virtual string Grab() => Name + ": 기본 촬영";
    }

    // ①: AreaCam이 CamBase를 물려받는다고 선언하는 기호와 부모 이름
    public class AreaCam : CamBase
    {
        public int Width { get; }

        // ②: 자식 생성자가 먼저 부모 생성자를 불러 name을 넘긴다
        public AreaCam(string name, int width) : base(name)
        {
            Width = width;
        }

        // ④: 부모의 Grab을 자식 것으로 바꿔 끼운다는 키워드 (빠지면 CS0114 경고)
        public override string Grab() => Name + ": 한 장 촬영 " + Width + "px";
    }

    public class LineCam : CamBase
    {
        public LineCam(string name) : base(name)
        {
        }

        public override string Grab() => Name + ": 한 줄씩 스캔";
    }

// ⑨: 이 클래스를 더는 아무도 상속하지 못하게 막는 키워드
    public sealed class SimAreaCam : AreaCam
    {
        // ⑩: 부모(AreaCam) 생성자에 이름과 가로 640을 넘긴다
        public SimAreaCam(string name) : base(name, 640)
        {
        }

        public override string Grab() => Name + ": 시뮬레이션 " + Width + "px";
    }

    // ⑤: new로 직접 만들 수 없고, 상속해서만 쓰는 미완성 클래스
    public abstract class DeviceBase
    {
        public string Name { get; }

        protected DeviceBase(string name)
        {
            Name = name;
        }

        // ⑥: 몸통 없이 이름만 정해 두고 자식이 반드시 채우게 하는 메서드
        protected abstract bool Open();

        public string Start()
        {
            if (Open())
            {
                return Name + " 시작";
            }
            return Name + " 열기 실패";
        }
    }

    public class LightDevice : DeviceBase
    {
        private bool Connected { get; }

        public LightDevice(string name, bool connected) : base(name)
        {
            Connected = connected;
        }

        protected override bool Open() => Connected;
    }

    public class FakeCam : ICam
    {
        private int _count;

        public FakeCam(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public string Grab()
        {
            _count++;
            return Name + ": 가짜 사진 #" + _count;
        }
    }
}