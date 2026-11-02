using System;
using System.Threading;
using System.Threading.Tasks;

namespace Katas.Basics
{
    public static class KataB11
    {
        public static void Run()
        {
            Show("주문 전");

            Task<string> order = Task.Run(() => Cook("짜장면", 1000));
            Show("주문 넣고 숙제하는 중");

            string food = order.GetAwaiter().GetResult();
            Show("받음: " + food);

            string two = OrderTwoAsync().GetAwaiter().GetResult();
            Show("받음: " + two);
        }


        private static string Cook(string menu, int ms)
        {
            Show(menu + " 요리 시작");
            Thread.Sleep(ms);
            Show(menu + " 요리 끝");
            return menu;
        }

        private static async Task<string> OrderTwoAsync()
        {
            Task<string> a = Task.Run(() => Cook("탕수육", 800));
            Task<string> b = Task.Run(() => Cook("짬뽕", 500));
            string ra = await a;
            string rb = await b;

            //string ra = await Task.Run(() => Cook("탕수육", 800));
            //string rb = await Task.Run(() => Cook("짬뽕", 500));

            return ra + " + " + rb;
        }

        private static void Show(string text)
        {
            Console.WriteLine("[스레드 " + Thread.CurrentThread.ManagedThreadId + "] " + text);
        }
    }
}
