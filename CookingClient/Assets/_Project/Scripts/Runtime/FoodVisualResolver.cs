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
        public static readonly Color RawGreen = new Color(0.55f, 0.78f, 0.35f, 1f);

        public static FoodVisual Evaluate(IngredientDefinition definition, IngredientInstance instance)
        {
            if (definition == null || instance == null)
                return Blank();

            Sprite sprite = FindSprite(definition, instance.Shape, instance.CookMethod);
            if (sprite == null)
                return Blank();

            return new FoodVisual
            {
                Sprite = sprite,
                Color = DonenessColor(definition, instance.Doneness)
            };
        }

        public static Sprite FindSprite(IngredientDefinition definition, CutShape shape, CookMethod method)
        {
            if (definition == null)
                return null;

            Sprite exact = FindExact(definition, shape, method);
            if (exact != null)
                return exact;

            if (shape != CutShape.Whole)
            {
                Sprite cut = FindAnyCut(definition, method);
                if (cut != null)
                    return cut;
            }

            if (shape == CutShape.Whole && method == CookMethod.Raw)
                return definition.spriteWhole;

            return null;
        }

        static Sprite FindExact(IngredientDefinition definition, CutShape shape, CookMethod method)
        {
            if (definition.stateSprites == null)
                return null;

            for (int i = 0; i < definition.stateSprites.Count; i++)
            {
                IngredientVisualState state = definition.stateSprites[i];
                if (state.sprite != null && state.shape == shape && state.cookMethod == method)
                    return state.sprite;
            }

            return null;
        }

        static Sprite FindAnyCut(IngredientDefinition definition, CookMethod method)
        {
            if (definition.stateSprites == null)
                return null;

            for (int i = 0; i < definition.stateSprites.Count; i++)
            {
                IngredientVisualState state = definition.stateSprites[i];
                if (state.sprite == null || state.shape == CutShape.Whole || state.cookMethod != method)
                    continue;

                return state.sprite;
            }

            return null;
        }

        public static Color DonenessColor(IngredientDefinition definition, float doneness)
        {
            float optimal = definition.optimalDoneness;
            if (doneness <= optimal)
            {
                float t = optimal <= 0f ? 1f : Mathf.Clamp01(doneness / optimal);
                return Color.Lerp(RawGreen, definition.baseColor, t);
            }

            float max = definition.maxDoneness;
            if (max <= optimal)
                return Color.black;

            float burn = Mathf.Clamp01((doneness - optimal) / (max - optimal));
            return Color.Lerp(definition.baseColor, Color.black, burn);
        }

        static FoodVisual Blank()
        {
            return new FoodVisual { Sprite = null, Color = Color.white };
        }
    }
}
