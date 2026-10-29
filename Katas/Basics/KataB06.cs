using System;
using System.Collections.Generic;

namespace Katas.Basics
{
    public static class KataB06
    {
        public static void Run()
        {
            List<string> cameras = new List<string>();
            cameras.Add("Cam1");
            cameras.Add("Cam2");
            cameras.Add("Cam3");

            Console.WriteLine(cameras.Count);
            Console.WriteLine(cameras[0]);
            cameras.Remove("Cam2");

            foreach (string list in cameras)
            {
                Console.WriteLine(list);
            }

            Dictionary<string, int> dic = new Dictionary<string, int>();
            dic["Cam1"] = 0;
            dic["Cam3"] = 0;
            dic["Cam1"] = dic["Cam1"] + 1;
            dic["Cam1"] = dic["Cam1"] + 1;

            if (dic.TryGetValue("Cam1", out int cam1))
            {
                Console.WriteLine(cam1);
            }

            if (!dic.ContainsKey("Cam9"))
            {
                Console.WriteLine("키없음");
            }

            foreach (KeyValuePair<string, int> pair in dic)
            {
                Console.WriteLine(pair.Key + " " + pair.Value);
            }

            List<int> ids = new List<int>() { 10, 20, 30 };
            PrintAll(ids);
            PrintAll(cameras);
        }

        public static void PrintAll<T>(List<T> list)
        {
            foreach (T item in list)
            {

                Console.WriteLine(item);
            }
        }
    }
}