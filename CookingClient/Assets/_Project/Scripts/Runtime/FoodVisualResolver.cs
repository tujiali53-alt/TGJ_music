using Cooking.Data;
using UnityEngine;

namespace Cooking.Runtime
{
    public struct FoodVisual
    {
        public Sprite Sprite;
        public Color Color;
    }

    public static class FoodVisualResolver
    {
        public static FoodVisual Evaluate(IngredientDefinition definition, IngredientInstance instance)
        {
            if (definition == null || instance == null || definition.spriteWhole == null)
                return Blank();

            return new FoodVisual
            {
                Sprite = definition.spriteWhole,
                Color = Color.white
            };
        }

        static FoodVisual Blank()
        {
            return new FoodVisual { Sprite = null, Color = Color.white };
        }
    }
}
