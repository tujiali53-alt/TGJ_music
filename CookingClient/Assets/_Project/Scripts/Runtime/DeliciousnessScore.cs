using System.Collections.Generic;
using System.Globalization;
using Cooking.Data;
using UnityEngine;

namespace Cooking.Runtime
{
    public enum DishPictureKind
    {
        Loose = 0,
        Recipe = 1,
        Charcoal = 2
    }

    public sealed class DishResult
    {
        public float Score;
        public string Comment = "";
        public DishPictureKind Picture;
        public Sprite SummarySprite;
        public readonly List<FoodVisual> LooseFoods = new List<FoodVisual>();
    }

    public static class DeliciousnessScore
    {
        public const float BasePerPortion = 10f;
        public const float OptimalBonus = 10f;
        public const float OptimalWindow = 10f;
        public const float SeasoningBonus = 5f;
        public const float UndercookedPenalty = 5f;
        public const float MeatVegetableBonus = 10f;
        public const float CharcoalPenalty = 5f;

        public static DishResult Evaluate(
            IList<IngredientInstance> plate,
            IList<IngredientDefinition> definitions,
            RecipeDefinition recipe,
            DishPresentation presentation)
        {
            var result = new DishResult();
            if (plate == null)
                return result;

            bool anyCharcoal = false;
            bool anyMeat = false;
            bool anyVegetable = false;
            var comments = new List<string>();

            for (int i = 0; i < plate.Count; i++)
            {
                IngredientInstance item = plate[i];
                if (item == null)
                    continue;

                IngredientDefinition definition = Find(definitions, item.DefinitionId);
                bool charcoal = definition != null && item.Doneness >= definition.maxDoneness;
                item.IsCharcoal = charcoal;
                if (item.Quantity <= 0)
                    continue;

                if (charcoal)
                {
                    anyCharcoal = true;
                    result.Score -= CharcoalPenalty * item.Quantity;
                    comments.Add("我把" + DisplayName(definition, item) + "烧过头了");
                    continue;
                }

                if (definition == null)
                    continue;

                result.Score += BasePerPortion * item.Quantity;
                if (Mathf.Abs(item.Doneness - definition.optimalDoneness) <= OptimalWindow)
                    result.Score += OptimalBonus * item.Quantity;

                if (!definition.canEatRaw && item.Doneness < 100f)
                    result.Score -= UndercookedPenalty * item.Quantity;

                if (HasSeasoningRule(recipe, item.DefinitionId)
                    && RecipeMatcher.SameSequence(item.SeasoningIds, RecipeMatcher.ExpectedSeasonings(recipe, item.DefinitionId)))
                    result.Score += SeasoningBonus * item.Quantity;

                if (definition.kind == IngredientKind.Meat)
                    anyMeat = true;
                else if (definition.kind == IngredientKind.Vegetable)
                    anyVegetable = true;
            }

            if (anyMeat && anyVegetable)
                result.Score += MeatVegetableBonus;

            if (anyCharcoal)
            {
                result.Picture = DishPictureKind.Charcoal;
                result.SummarySprite = presentation != null ? presentation.charcoalSprite : null;
            }
            else if (RecipeMatcher.Matches(plate, definitions, recipe))
            {
                result.Picture = DishPictureKind.Recipe;
                result.SummarySprite = recipe.finishedSprite;
            }
            else
            {
                result.Picture = DishPictureKind.Loose;
                for (int i = 0; i < plate.Count; i++)
                {
                    IngredientInstance item = plate[i];
                    if (item == null || item.Quantity <= 0)
                        continue;

                    result.LooseFoods.Add(FoodVisualResolver.Evaluate(Find(definitions, item.DefinitionId), item));
                }
            }

            result.Comment = string.Join("\n", comments);
            return result;
        }

        public static float Average(float[] scores, int count)
        {
            if (scores == null || count <= 0)
                return 0f;

            int n = count < scores.Length ? count : scores.Length;
            float sum = 0f;
            for (int i = 0; i < n; i++)
                sum += scores[i];

            return sum / n;
        }

        public static string FormatScore(float value)
        {
            float tenth = Mathf.Round(value * 10f) / 10f;
            float whole = Mathf.Round(tenth);
            if (Mathf.Abs(tenth - whole) < 0.001f)
                return whole.ToString("0", CultureInfo.InvariantCulture);

            return tenth.ToString("0.0", CultureInfo.InvariantCulture);
        }

        static bool HasSeasoningRule(RecipeDefinition recipe, string ingredientId)
        {
            return RecipeMatcher.ExpectedSeasonings(recipe, ingredientId).Count > 0;
        }

        static string DisplayName(IngredientDefinition definition, IngredientInstance item)
        {
            if (definition != null && !string.IsNullOrEmpty(definition.displayName))
                return definition.displayName;

            return item.DefinitionId;
        }

        static IngredientDefinition Find(IList<IngredientDefinition> definitions, string id)
        {
            if (definitions == null || string.IsNullOrEmpty(id))
                return null;

            for (int i = 0; i < definitions.Count; i++)
            {
                IngredientDefinition definition = definitions[i];
                if (definition != null && definition.id == id)
                    return definition;
            }

            return null;
        }
    }
}
