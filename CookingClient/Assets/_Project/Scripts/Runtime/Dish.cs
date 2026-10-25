using System.Collections.Generic;

namespace Cooking.Runtime
{
    public sealed class Dish
    {
        public int DishId;
        public List<IngredientInstance> Items = new List<IngredientInstance>();
        public string RecipeId = "";
        public float ExecutionScore = 1f;
    }
}
