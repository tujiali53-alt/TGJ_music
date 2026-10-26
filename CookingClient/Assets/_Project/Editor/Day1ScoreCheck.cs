using System.Collections.Generic;
using Cooking.Data;
using Cooking.Runtime;
using UnityEditor;
using UnityEngine;

namespace Cooking.EditorTools
{
    public static class Day1ScoreCheck
    {
        [MenuItem("Cooking/Check Day 1 Score")]
        public static void RunMenu()
        {
            string error = Run();
            if (error == null)
                Debug.Log("第 1 天计分通过：做对 75，一份焦炭 45。");
            else
                Debug.LogError(error);
        }

        [InitializeOnLoadMethod]
        static void Schedule()
        {
            EditorApplication.delayCall += RunWhenReady;
        }

        static void RunWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += RunWhenReady;
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            string error = Run();
            if (error != null)
                Debug.LogError(error);
        }

        public static string Run()
        {
            IngredientDefinition mushroom = Definition("ing_mushroom", "白蘑菇", IngredientKind.Vegetable, false, 120f, 180f);
            IngredientDefinition onion = Definition("ing_onion", "洋葱", IngredientKind.Vegetable, true, 110f, 170f);
            IngredientDefinition potato = Definition("ing_potato", "星土豆", IngredientKind.Vegetable, false, 130f, 190f);
            IngredientDefinition beef = Definition("ing_beef", "虹彩牛肉", IngredientKind.Meat, false, 90f, 160f);
            RecipeDefinition soup = Day1Soup();
            RecipeDefinition hotpot = ScriptableObject.CreateInstance<RecipeDefinition>();
            DishPresentation presentation = ScriptableObject.CreateInstance<DishPresentation>();
            hotpot.hideFlags = HideFlags.HideAndDontSave;
            presentation.hideFlags = HideFlags.HideAndDontSave;
            Texture2D texture = new Texture2D(1, 1);
            Sprite charcoal = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            Sprite slice = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            Sprite boiled = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            presentation.charcoalSprite = charcoal;
            hotpot.id = "recipe_day7";
            var definitions = new List<IngredientDefinition> { mushroom, onion, potato, beef };
            var created = new Object[] { mushroom, onion, potato, beef, soup, hotpot, presentation, texture, charcoal, slice, boiled };

            try
            {
                string                 error = CheckPerfect(soup, presentation, definitions);
                if (error != null)
                    return error;

                error = CheckShapeIsIgnored(soup, presentation, definitions);
                if (error != null)
                    return error;

                error = CheckCharcoal(soup, presentation, definitions, mushroom);
                if (error != null)
                    return error;

                error = CheckWrongSeasoning(soup, presentation, definitions);
                if (error != null)
                    return error;

                error = CheckHotpotHasNoSeasoningBonus(hotpot, definitions);
                if (error != null)
                    return error;

                error = CheckMeatAndVegetable(definitions, beef, onion);
                if (error != null)
                    return error;

                error = CheckAverage();
                if (error != null)
                    return error;

                mushroom.stateSprites.Add(new IngredientVisualState
                {
                    shape = CutShape.Slice,
                    cookMethod = CookMethod.Raw,
                    sprite = slice
                });
                mushroom.stateSprites.Add(new IngredientVisualState
                {
                    shape = CutShape.Slice,
                    cookMethod = CookMethod.Boiled,
                    sprite = boiled
                });
                return CheckVisual(mushroom, slice, boiled);
            }
            finally
            {
                for (int i = created.Length - 1; i >= 0; i--)
                {
                    if (created[i] != null)
                        Object.DestroyImmediate(created[i]);
                }
            }
        }

        static string CheckPerfect(
            RecipeDefinition soup,
            DishPresentation presentation,
            List<IngredientDefinition> definitions)
        {
            var plate = new List<IngredientInstance>
            {
                Portion("ing_mushroom", CutShape.Slice, CookMethod.BoiledAndFried, 120f, "season_oil", "season_salt", "season_pepper"),
                Portion("ing_onion", CutShape.Dice, CookMethod.BoiledAndFried, 110f, "season_oil", "season_salt", "season_pepper"),
                Portion("ing_potato", CutShape.Dice, CookMethod.Boiled, 130f, "season_salt", "season_pepper")
            };

            DishResult result = DeliciousnessScore.Evaluate(plate, definitions, soup, presentation);
            if (!Near(result.Score, 75f))
                return "第 1 天做对应为 75，实际 " + result.Score;

            if (result.Picture != DishPictureKind.Recipe)
                return "第 1 天做对时应使用成品图。";

            if (result.Comment != "")
                return "第 1 天做对时没有焦炭评语。";

            return null;
        }

        static string CheckShapeIsIgnored(
            RecipeDefinition soup,
            DishPresentation presentation,
            List<IngredientDefinition> definitions)
        {
            var plate = new List<IngredientInstance>
            {
                Portion("ing_mushroom", CutShape.Whole, CookMethod.BoiledAndFried, 120f, "season_oil", "season_salt", "season_pepper"),
                Portion("ing_onion", CutShape.Whole, CookMethod.BoiledAndFried, 110f, "season_oil", "season_salt", "season_pepper"),
                Portion("ing_potato", CutShape.Shred, CookMethod.Boiled, 130f, "season_salt", "season_pepper")
            };

            DishResult result = DeliciousnessScore.Evaluate(plate, definitions, soup, presentation);
            if (!Near(result.Score, 75f) || result.Picture != DishPictureKind.Recipe)
                return "上盘不看形状。原形或切丝仍应是成品，分数 75，实际 " + result.Score;

            return null;
        }

        static string CheckCharcoal(
            RecipeDefinition soup,
            DishPresentation presentation,
            List<IngredientDefinition> definitions,
            IngredientDefinition mushroom)
        {
            IngredientInstance burnt = Portion("ing_mushroom", CutShape.Slice, CookMethod.BoiledAndFried, 180f, "season_oil", "season_salt", "season_pepper");
            var plate = new List<IngredientInstance>
            {
                burnt,
                Portion("ing_onion", CutShape.Dice, CookMethod.BoiledAndFried, 110f, "season_oil", "season_salt", "season_pepper"),
                Portion("ing_potato", CutShape.Dice, CookMethod.Boiled, 130f, "season_salt", "season_pepper")
            };

            DishResult result = DeliciousnessScore.Evaluate(plate, definitions, soup, presentation);
            if (!Near(result.Score, 45f))
                return "一份蘑菇焦炭时应为 45，实际 " + result.Score;

            if (!burnt.IsCharcoal)
                return "熟度达到最大熟度时要标成焦炭。";

            if (result.Picture != DishPictureKind.Charcoal || result.SummarySprite != presentation.charcoalSprite)
                return "有焦炭时总结图要用共用焦炭图。";

            if (result.Comment != "我把白蘑菇烧过头了")
                return "焦炭评语应为「我把白蘑菇烧过头了」，实际「" + result.Comment + "」。";

            if (mushroom == null)
                return "缺少白蘑菇定义。";

            return null;
        }

        static string CheckWrongSeasoning(RecipeDefinition soup, DishPresentation presentation, List<IngredientDefinition> definitions)
        {
            var plate = new List<IngredientInstance>
            {
                Portion("ing_mushroom", CutShape.Slice, CookMethod.BoiledAndFried, 120f, "season_oil", "season_pepper", "season_salt"),
                Portion("ing_onion", CutShape.Dice, CookMethod.BoiledAndFried, 110f, "season_oil", "season_salt", "season_pepper"),
                Portion("ing_potato", CutShape.Dice, CookMethod.Boiled, 130f, "season_salt", "season_pepper")
            };

            DishResult result = DeliciousnessScore.Evaluate(plate, definitions, soup, presentation);
            if (!Near(result.Score, 70f))
                return "调味顺序不对时应为 70，实际 " + result.Score;

            if (result.Picture != DishPictureKind.Loose || result.LooseFoods.Count != 3)
                return "调味顺序不对时总结图是三份散装食材。";

            return null;
        }

        static string CheckHotpotHasNoSeasoningBonus(RecipeDefinition hotpot, List<IngredientDefinition> definitions)
        {
            var plate = new List<IngredientInstance>
            {
                Portion("ing_mushroom", CutShape.Slice, CookMethod.BoiledAndFried, 120f, "season_oil", "season_salt", "season_pepper"),
                Portion("ing_onion", CutShape.Dice, CookMethod.BoiledAndFried, 110f),
                Portion("ing_potato", CutShape.Dice, CookMethod.Boiled, 130f)
            };

            DishResult result = DeliciousnessScore.Evaluate(plate, definitions, hotpot, null);
            if (!Near(result.Score, 60f))
                return "没有成菜要求的火锅不加调味分，应为 60，实际 " + result.Score;

            if (result.Picture != DishPictureKind.Loose)
                return "火锅不使用成品图。";

            return null;
        }

        static string CheckMeatAndVegetable(List<IngredientDefinition> definitions, IngredientDefinition beef, IngredientDefinition onion)
        {
            var plate = new List<IngredientInstance>
            {
                Portion("ing_beef", CutShape.Dice, CookMethod.Fried, 90f),
                Portion("ing_onion", CutShape.Dice, CookMethod.BoiledAndFried, 110f)
            };

            DishResult result = DeliciousnessScore.Evaluate(plate, definitions, null, null);
            if (!Near(result.Score, 45f))
                return "荤素搭配加未熟扣分应为 45，实际 " + result.Score;

            if (beef.kind != IngredientKind.Meat || onion.kind != IngredientKind.Vegetable)
                return "荤素测试用的食材种类不对。";

            return null;
        }

        static string CheckAverage()
        {
            float average = DeliciousnessScore.Average(new[] { 30f, 20f, 0f }, 2);
            if (!Near(average, 25f) || DeliciousnessScore.FormatScore(average) != "25")
                return "两次 30 和 20 的平均值应为 25。";

            if (DeliciousnessScore.FormatScore(17.5f) != "17.5")
                return "半分要保留一位小数。";

            if (DeliciousnessScore.FormatScore(75f) != "75")
                return "整数不带小数。";

            return null;
        }

        static string CheckVisual(IngredientDefinition mushroom, Sprite slice, Sprite boiled)
        {
            FoodVisual missing = FoodVisualResolver.Evaluate(mushroom, new IngredientInstance
            {
                Shape = CutShape.Dice,
                CookMethod = CookMethod.Fried,
                Doneness = 120f
            });
            if (missing.Sprite != null || missing.Color != Color.white)
                return "没绑到的状态应为白色。";

            FoodVisual fallback = FoodVisualResolver.Evaluate(mushroom, new IngredientInstance
            {
                Shape = CutShape.Dice,
                CookMethod = CookMethod.Raw,
                Doneness = 0f
            });
            if (fallback.Sprite != slice)
                return "切过的形状没有专用图时，应使用同一烹饪方法下的切图。";

            FoodVisual exact = FoodVisualResolver.Evaluate(mushroom, new IngredientInstance
            {
                Shape = CutShape.Slice,
                CookMethod = CookMethod.Boiled,
                Doneness = 120f
            });
            if (exact.Sprite != boiled || exact.Color != mushroom.baseColor)
                return "形状和烹饪方法都对上时用那一张图，最佳熟度用本色。";

            FoodVisual burnt = FoodVisualResolver.Evaluate(mushroom, new IngredientInstance
            {
                Shape = CutShape.Slice,
                CookMethod = CookMethod.Boiled,
                Doneness = 180f
            });
            if (burnt.Sprite != boiled || burnt.Color != Color.black)
                return "有精灵时，最大熟度的颜色是黑色。";

            return null;
        }

        static IngredientDefinition Definition(
            string id,
            string displayName,
            IngredientKind kind,
            bool canEatRaw,
            float optimal,
            float max)
        {
            IngredientDefinition definition = ScriptableObject.CreateInstance<IngredientDefinition>();
            definition.hideFlags = HideFlags.HideAndDontSave;
            definition.id = id;
            definition.displayName = displayName;
            definition.kind = kind;
            definition.canEatRaw = canEatRaw;
            definition.canCut = true;
            definition.optimalDoneness = optimal;
            definition.maxDoneness = max;
            definition.baseColor = Color.white;
            return definition;
        }

        static RecipeDefinition Day1Soup()
        {
            RecipeDefinition recipe = ScriptableObject.CreateInstance<RecipeDefinition>();
            recipe.hideFlags = HideFlags.HideAndDontSave;
            recipe.id = "recipe_day1";
            recipe.displayName = "什锦蘑菇浓汤";
            recipe.unlockDay = 1;
            recipe.ingredients.Add(Requirement("ing_mushroom", CookMethod.BoiledAndFried));
            recipe.ingredients.Add(Requirement("ing_onion", CookMethod.BoiledAndFried));
            recipe.ingredients.Add(Requirement("ing_potato", CookMethod.Boiled));
            recipe.seasonings.Add(Seasoning("season_oil", "ing_mushroom"));
            recipe.seasonings.Add(Seasoning("season_salt", "ing_mushroom"));
            recipe.seasonings.Add(Seasoning("season_pepper", "ing_mushroom"));
            recipe.seasonings.Add(Seasoning("season_oil", "ing_onion"));
            recipe.seasonings.Add(Seasoning("season_salt", "ing_onion"));
            recipe.seasonings.Add(Seasoning("season_pepper", "ing_onion"));
            recipe.seasonings.Add(Seasoning("season_salt", "ing_potato"));
            recipe.seasonings.Add(Seasoning("season_pepper", "ing_potato"));
            return recipe;
        }

        static RecipeIngredient Requirement(string id, CookMethod method)
        {
            return new RecipeIngredient
            {
                ingredientId = id,
                cookMethod = method,
                count = 1
            };
        }

        static RecipeSeasoning Seasoning(string seasoningId, string ingredientId)
        {
            return new RecipeSeasoning
            {
                seasoningId = seasoningId,
                targetIngredientId = ingredientId,
                count = 1
            };
        }

        static IngredientInstance Portion(
            string id,
            CutShape shape,
            CookMethod method,
            float doneness,
            params string[] seasonings)
        {
            return new IngredientInstance
            {
                DefinitionId = id,
                Quantity = 1,
                Shape = shape,
                CookMethod = method,
                Doneness = doneness,
                SeasoningIds = new List<string>(seasonings)
            };
        }

        static bool Near(float value, float expected)
        {
            return Mathf.Abs(value - expected) < 0.001f;
        }
    }
}
