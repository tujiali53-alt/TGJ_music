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
            Sprite sprite = SpriteFor(definition, instance == null ? CutShape.Whole : instance.Shape);
            if (definition == null || instance == null || sprite == null)
                return Blank();

            return new FoodVisual
            {
                Sprite = sprite,
                Color = Color.white
            };
        }

        public static Sprite SpriteFor(IngredientDefinition definition, CutShape shape)
        {
            if (definition == null)
                return null;

            if (shape == CutShape.Cut && definition.spriteCut != null)
                return definition.spriteCut;

            return definition.spriteWhole;
        }

        static FoodVisual Blank()
        {
            return new FoodVisual { Sprite = null, Color = Color.white };
        }
    }
}
