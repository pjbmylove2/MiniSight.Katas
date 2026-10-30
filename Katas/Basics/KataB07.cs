using System;
using System.Collections.Generic;
using System.Linq;

namespace Katas.Basics
{
    public class InspectResult
    {
        public int Id { get; set; }
        public string Camera { get; set; }
        public bool IsOk { get; set; }
        public double Width { get; set; }
    }


    public static class KataB07
    {
        public static void Run()
        {
            List<InspectResult> results = new List<InspectResult>
            {
                new InspectResult { Id = 1, Camera = "Cam1", IsOk = true,  Width = 10.02 },
                new InspectResult { Id = 2, Camera = "Cam1", IsOk = false, Width = 10.31 },
                new InspectResult { Id = 3, Camera = "Cam2", IsOk = true,  Width = 9.98 },
                new InspectResult { Id = 4, Camera = "Cam2", IsOk = false, Width = 9.62 },
                new InspectResult { Id = 5, Camera = "Cam1", IsOk = true,  Width = 10.05 }
            };

            List<InspectResult> ngList = results.Where(x => !x.IsOk).ToList();
            Console.WriteLine("NG 개수: " + ngList.Count);

            List<int> ngId = new List<int>();
            ngId = ngList.Select(x => x.Id).ToList();
            Console.WriteLine("NG 번호: " + string.Join(", ", ngId));

            InspectResult cam2 = results.FirstOrDefault(x => x.Camera == "Cam2");
            Console.WriteLine("Cam2 첫 결과: #" + cam2.Id);

            InspectResult cam9 = results.FirstOrDefault(x => x.Camera == "Cam9");
            if (cam9 == null)
            {
                Console.WriteLine("Cam9 결과: 없음");
            }
                
            Console.WriteLine("NG가 하나라도 있나?" + results.Any(x => !x.IsOk));
            Console.WriteLine("Cam1 검사 수: " + results.Count(x => x.Camera == "Cam1"));
            Console.WriteLine("평균 폭: " + Math.Round(results.Average(r => r.Width), 3));
            Console.WriteLine("최대 폭: " + results.Max(x => x.Width));

            foreach (InspectResult result in results.OrderByDescending(x => x.Width)) 
            {
                Console.WriteLine("#" + result.Id + " " + result.Width);
            }
        }
    }
}
