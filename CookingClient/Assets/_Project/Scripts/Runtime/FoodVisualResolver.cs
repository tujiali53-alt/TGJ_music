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
            Sprite sprite = SpriteFor(
                definition,
                instance == null ? CutShape.Whole : instance.Shape,
                instance == null ? CookMethod.Raw : instance.CookMethod);
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
            return SpriteFor(definition, shape, CookMethod.Raw);
        }

        public static Sprite SpriteFor(IngredientDefinition definition, CutShape shape, CookMethod cookMethod)
        {
            if (definition == null)
                return null;

            if (IsEgg(definition))
                return SpriteForEgg(definition, cookMethod);

            if (shape == CutShape.Cut && definition.spriteCut != null)
                return definition.spriteCut;

            return definition.spriteWhole;
        }

        static bool IsEgg(IngredientDefinition definition)
        {
            return definition.id == "ing_egg" || definition.id == "ing_doneegg";
        }

        // 生/煮：Food_Egg；煎或煎煮（不论切否）：Food_Egg_Sliced。生蛋本身不允许切。
        static Sprite SpriteForEgg(IngredientDefinition definition, CookMethod cookMethod)
        {
            if (cookMethod == CookMethod.Fried || cookMethod == CookMethod.BoiledAndFried)
            {
                if (definition.spriteCut != null)
                    return definition.spriteCut;
            }

            return definition.spriteWhole;
        }

        static FoodVisual Blank()
        {
            return new FoodVisual { Sprite = null, Color = Color.white };
        }
    }
}
