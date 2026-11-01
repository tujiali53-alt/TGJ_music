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
        public readonly List<string> Comments = new List<string>();
        public DishPictureKind Picture;
        public Sprite SummarySprite;
        public readonly List<FoodVisual> LooseFoods = new List<FoodVisual>();

        public string Comment
        {
            get { return string.Join("\n", Comments); }
        }
    }

    public static class DeliciousnessScore
    {
        public const float BasePerPortion = 10f;
        public const float OptimalBonus = 10f;
        public const float OptimalWindow = 10f;
        public const float UndercookedPenalty = 5f;
        public const float MeatVegetableBonus = 10f;
        public const float CharcoalPenalty = 5f;
        public const float SeasoningMatchBonus = 5f;
        public const float RecipeMatchBonus = 50f;
        public const float SynergyBonusPerFood = 10f;
        public const float SynergyWindow = 1f;

        public static bool IsOptimal(IngredientDefinition definition, float doneness)
        {
            if (definition == null)
                return false;

            return Mathf.Abs(doneness - definition.optimalDoneness) <= OptimalWindow;
        }

        public static float OptimalStart(IngredientDefinition definition)
        {
            if (definition == null)
                return 0f;

            return definition.optimalDoneness - OptimalWindow;
        }

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
            bool seasoningMatched = false;
            var optimalNames = new List<string>();
            var undercookedNames = new List<string>();
            var hitTimes = new List<float>();

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
                    result.Comments.Add("我把" + DisplayName(definition, item) + "烧过头了……");
                    continue;
                }

                if (definition == null)
                    continue;

                string name = DisplayName(definition, item);
                result.Score += BasePerPortion * item.Quantity;
                if (IsOptimal(definition, item.Doneness))
                {
                    result.Score += OptimalBonus * item.Quantity;
                    optimalNames.Add(name);
                }

                if (!definition.canEatRaw && item.Doneness < 100f)
                {
                    result.Score -= UndercookedPenalty * item.Quantity;
                    undercookedNames.Add(name);
                }

                if (definition.kind == IngredientKind.Meat)
                    anyMeat = true;
                else if (definition.kind == IngredientKind.Vegetable)
                    anyVegetable = true;

                if (SeasoningMatches(recipe, item))
                {
                    result.Score += SeasoningMatchBonus * item.Quantity;
                    seasoningMatched = true;
                }

                if (item.OptimalHitTime >= 0f)
                {
                    for (int n = 0; n < item.Quantity; n++)
                        hitTimes.Add(item.OptimalHitTime);
                }
            }

            if (anyMeat && anyVegetable)
            {
                result.Score += MeatVegetableBonus;
                result.Comments.Add("荤素搭配的饮食能给我带来健康的身体——虽然我好像用不着它了。");
            }

            if (optimalNames.Count > 0)
                result.Comments.Add(string.Join("、", optimalNames) + "的火候刚刚好！");

            if (seasoningMatched)
                result.Comments.Add("这一味调料看来是放对了。");

            for (int i = 0; i < undercookedNames.Count; i++)
                result.Comments.Add("哎呀，" + undercookedNames[i] + "是生的！希望不要拉肚子。");

            int synergy = LargestCluster(hitTimes, SynergyWindow);
            if (synergy > 1)
            {
                result.Score += SynergyBonusPerFood * synergy;
                result.Comments.Add("食材的相性很好，味道完美地融合在一起。");
            }

            bool recipeMatch = RecipeMatcher.Matches(plate, definitions, recipe);
            if (recipeMatch)
            {
                result.Score += RecipeMatchBonus;
                result.Comments.Add("完美的复现。");
            }

            if (anyCharcoal)
            {
                result.Picture = DishPictureKind.Charcoal;
                result.SummarySprite = presentation != null ? presentation.charcoalSprite : null;
            }
            else if (recipeMatch)
            {
                result.Picture = DishPictureKind.Recipe;
                result.SummarySprite = recipe.finishedSprite;
            }
            else
            {
                result.Picture = DishPictureKind.Loose;
                result.SummarySprite = presentation != null ? presentation.looseSprite : null;
                for (int i = 0; i < plate.Count; i++)
                {
                    IngredientInstance item = plate[i];
                    if (item == null || item.Quantity <= 0)
                        continue;

                    result.LooseFoods.Add(FoodVisualResolver.Evaluate(Find(definitions, item.DefinitionId), item));
                }
            }

            return result;
        }

        static bool SeasoningMatches(RecipeDefinition recipe, IngredientInstance item)
        {
            if (recipe == null || item == null)
                return false;

            List<string> expected = RecipeMatcher.ExpectedSeasonings(recipe, item.DefinitionId);
            if (expected.Count == 0)
                return false;

            return RecipeMatcher.SameSeasonings(item.SeasoningIds, expected);
        }

        static int LargestCluster(List<float> times, float window)
        {
            if (times == null || times.Count == 0)
                return 0;

            times.Sort();
            int best = 1;
            int left = 0;
            for (int right = 0; right < times.Count; right++)
            {
                while (times[right] - times[left] > window)
                    left++;

                int size = right - left + 1;
                if (size > best)
                    best = size;
            }

            return best;
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
