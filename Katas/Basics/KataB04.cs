using System;


namespace Katas.Basics
{
    public class DoorBell
    {
        public event EventHandler<string> Rang;

        public void Push(string who)
        {
            Console.WriteLine("딩동! (" + who + ")");
            EventHandler<string> handler = Rang;
            if (handler != null)
            {
                handler.Invoke(this, who);
            }
        }
    }


    public static class KataB04
    {
        public static void Run()
        {
            DoorBell bell = new DoorBell();
            bell.Rang += Mom;
            bell.Rang += Dog;
            bell.Push("택배");

            bell.Rang -= Dog;
            bell.Push("친구");

            bell.Rang -= Mom;
            bell.Push("아무도 없음");
        }

        private static void Mom(object sender, string who)
        {
            Console.WriteLine("  엄마: " + who + " 왔네, 나가볼게");
        }

        private static void Dog(object sender, string who)
        {
            Console.WriteLine("  강아지: 멍멍!");
        }
    }


}