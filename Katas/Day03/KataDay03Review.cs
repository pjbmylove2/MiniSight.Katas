using System;

namespace Katas.Day03.Review
{
    public enum CookState
    {
        Idle, Cooking, Done
    }

    public abstract class RamenRecipe
    {
        public CookState State { get; private set; }

        public void Cook()
        {
            State = CookState.Cooking;
            Console.WriteLine("조리 중: " + State);

            Console.WriteLine("물 끓이기");
            AddSoup();
            Console.WriteLine("불 끄기");

            State = CookState.Done;
        }

        protected abstract void AddSoup();
    }

    public sealed class RedRamen : RamenRecipe
    {
        protected override void AddSoup()
        {
            Console.WriteLine("빨간 스프 넣기");
        }
    }

    public static class KataDay03Review
    {
        public static void Run()
        {
            RamenRecipe recipe = new RedRamen();
            Console.WriteLine("시작 전: " + recipe.State);

            recipe.Cook();

            Console.WriteLine("끝난 후: " + recipe.State);
        }
    }
}
