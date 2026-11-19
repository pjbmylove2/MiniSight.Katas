using System;

namespace Katas.L0
{
    public static class D001
    {
        public static void Run()
        {
            Chunk1();
            Chunk2();
            Chunk3();
            Chunk4();
            Chunk5();
            Chunk6();
        }

        public static void Chunk1()
        {
            string cameraName = null;
            Console.WriteLine("비었나? " + (cameraName == null));
            try
            {
                Console.WriteLine(cameraName.Length);
            }
            // ①: null인 변수에 .을 찍었을 때 CLR이 던지는 예외의 형식 이름
            catch (NullReferenceException ex)
            {
                Console.WriteLine("터짐: " + ex.GetType().Name);
            }
        }

        public static void Chunk2()
        {
            string cameraName = null;
            // ② ③: ② null도 담을 수 있는 int 형식 / ③ cameraName이 null이면 Length를 부르지 않고 null을 돌려주는 연산자
            int? length = cameraName?.Length;
            // ④: int?에 값이 들어 있는지 true/false로 알려 주는 속성
            Console.WriteLine("길이 있음? " + length.HasValue);

            cameraName = "CAM1";
            length = cameraName?.Length;
            Console.WriteLine("길이: " + length);
        }

        public static void Chunk3()
        {
            string fromConfig = null;
            // ⑤: fromConfig가 null이면 오른쪽 글자를 대신 쓰게 하는 연산자
            string name = fromConfig ?? "기본카메라";
            Console.WriteLine("이름: " + name);

            string model = null;
            // ⑥ ← 다시: 왼쪽 결과가 null이면 -1로 바꿔서 int에 담기게 하는 연산자와 값 (변수 형식은 int 그대로)
            int length = model?.Length ?? -1;
            Console.WriteLine("모델 길이: " + length);
        }

        public static void Chunk4()
        {
            Camera cam = new Camera("CAM1", 1200);
            Console.WriteLine(cam.Info);
            Console.WriteLine("2배 노출: " + cam.Double(cam.ExposureUs));
        }

        public static void Chunk5()
        {
            try
            {
                Camera cam = new Camera(null, 1000);
            }
            catch (ArgumentNullException ex)
            {
                // ⑪: 예외 안에 들어 있는, null이었던 매개변수의 이름
                Console.WriteLine("빠진 값: " + ex.ParamName);
            }
            // ⑫ ← 다시: Camera 클래스의 노출 시간 속성 (클래스이름.속성이름, 출력: ExposureUs)
            Console.WriteLine("속성 이름: " + nameof(Camera.ExposureUs));
        }

        public static void Chunk6()
        {
            Camera[] cameras = { new Camera("CAM1", 1200), null };
            foreach (Camera cam in cameras)
            {
                // ⑬ ← 다시: cam이 null이면 Info를 부르지 않고, 그 null을 "카메라 없음"으로 바꾼다
                string text = cam?.Info ?? "카메라 없음";
                Console.WriteLine(text);
            }
        }
    }

    public class Camera
    {
        // ⑦: 생성자에서만 값을 넣을 수 있는 읽기 전용 자동 속성 (set 없음)
        public string Name { get; }
        public int ExposureUs { get; }

        public Camera(string name, int exposureUs)
        {
            if (name == null)
            {
                // ⑧ ← 다시: "넘겨받은 인수가 null"이라는 뜻의 예외를 만든다. 괄호 안에는 매개변수 name의 이름을 글자로 (오타를 컴파일러가 잡는 방법)
                throw new ArgumentNullException(nameof(name));
            }
            Name = name;
            ExposureUs = exposureUs;
        }

        // ⑨: 읽을 때마다 오른쪽 식을 새로 계산하는 속성을 만드는 기호
        public string Info => Name + " / 노출 " + ExposureUs + "us";

        // ⑩: value를 2배 해서 돌려주는 식 하나짜리 몸통 (줄 끝 ; 까지)
        public int Double(int value) => value * 2;
    }
}