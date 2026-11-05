using System;

namespace Katas.Day03
{
    public enum CookState
    {
        Idle, Cooking, Done, Burnt
    }

    public abstract class RamenRecipe
    {
        public CookState State { get; private set; }
        public void Cook()
        {
            State = CookState.Cooking;
            Console.WriteLine("조리 중: Cooking 출력");

            try
            {
                Console.WriteLine("물 끓이기");
                AddSoup();
                State = CookState.Done;
            }
            catch
            {
                State = CookState.Burnt;
                throw;
            }
            finally
            {
                Console.WriteLine("불 끄기");
            }
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

    public sealed class BrokenRamen : RamenRecipe
    {
        protected override void AddSoup()
        {
            throw new InvalidOperationException("스프가 없습니다.");
        }
    }

    public static class KataDay03
    {
        public static void Run()
        {
            RamenRecipe recipe = new BrokenRamen();
            Console.WriteLine("시작 전: " + recipe.State);

            try
            {
                recipe.Cook();
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("실패: " + ex.Message);
            }

            Console.WriteLine("끝난 후: " + recipe.State);
        }
    }
}
