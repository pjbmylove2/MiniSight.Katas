using System;
using System.IO;
using System.Text;

namespace Katas.Basics
{
    public static class KataB10
    {
        public static void Run()
        {
            string dir = Path.Combine(Path.GetTempPath(), "MiniSightKata");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            string file = Path.Combine(dir, "result.csv");
            Console.WriteLine("파일 이름만: " + Path.GetFileName(file));
            Console.WriteLine("확장자:    " + Path.GetExtension(file));

            File.WriteAllText(file, "Id,Judge\r\n", Encoding.UTF8);
            File.AppendAllText(file, "1,OK\r\n", Encoding.UTF8);
            File.AppendAllText(file, "2,NG\r\n", Encoding.UTF8);
            string[] lines = File.ReadAllLines(file, Encoding.UTF8);
            Console.WriteLine("줄 수: " + lines.Length);
            foreach (var l in lines)
            {
                Console.WriteLine("  " + l);
            }
            Console.WriteLine("파일 있나? " + File.Exists(file));

            int value = 1000;
            Byte[] little = BitConverter.GetBytes(value);
            Console.WriteLine("little 출력 -> " + BitConverter.ToString(little));
            Byte[] big = BitConverter.GetBytes(value);
            Array.Reverse(big);
            Console.WriteLine("big 출력 -> " + BitConverter.ToString(big));
            Console.WriteLine("출력 -> " + BitConverter.IsLittleEndian);

            Byte[] head = Encoding.ASCII.GetBytes("MS");
            Byte[] packet = new Byte[head.Length + big.Length];
            Buffer.BlockCopy(head, 0, packet, 0, head.Length);
            Buffer.BlockCopy(big, 0, packet, head.Length, big.Length);
            Console.WriteLine("보낼 패킷: " + BitConverter.ToString(packet));

            Byte[] valuePart = new Byte[4];
            Buffer.BlockCopy(packet, head.Length, valuePart, 0, valuePart.Length);
            Array.Reverse(valuePart);
            int back = BitConverter.ToInt32(valuePart, 0);
            Console.WriteLine("되읽은 값: " + back);

            File.Delete(file);
        }
    }
}
