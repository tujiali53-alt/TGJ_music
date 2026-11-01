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
                Debug.Log("第 1 天计分通过：做对 125，一份焦炭 45。");
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
            Sprite cut = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            presentation.charcoalSprite = charcoal;
            hotpot.id = "recipe_day7";
            var definitions = new List<IngredientDefinition> { mushroom, onion, potato, beef };
            var created = new Object[] { mushroom, onion, potato, beef, soup, hotpot, presentation, texture, charcoal, slice, cut };

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

                error = CheckTwoCharcoalComments(definitions);
                if (error != null)
                    return error;

                error = CheckSeasoningOrder(soup, presentation, definitions);
                if (error != null)
                    return error;

                error = CheckSeasoningRepeat(soup, presentation, definitions);
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

                error = CheckOptimalWindow(definitions);
                if (error != null)
                    return error;

                error = CheckSynergy(definitions);
                if (error != null)
                    return error;

                mushroom.spriteWhole = slice;
                mushroom.spriteCut = cut;
                return CheckVisual(mushroom, slice, cut);
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
                Portion("ing_mushroom", CutShape.Cut, CookMethod.BoiledAndFried, 120f, "season_oil", "season_salt", "season_pepper"),
                Portion("ing_onion", CutShape.Cut, CookMethod.BoiledAndFried, 110f, "season_oil", "season_salt", "season_pepper"),
                Portion("ing_potato", CutShape.Cut, CookMethod.Boiled, 130f, "season_salt", "season_pepper")
            };

            DishResult result = DeliciousnessScore.Evaluate(plate, definitions, soup, presentation);
            if (!Near(result.Score, 125f))
                return "第 1 天做对应为 125，实际 " + result.Score;

            if (result.Picture != DishPictureKind.Recipe)
                return "第 1 天做对时应使用成品图。";

            if (result.Comment != "白蘑菇、洋葱、星土豆的火候刚刚好！\n这一味调料看来是放对了。\n完美的复现。")
                return "第 1 天做对时评语不符，实际「" + result.Comment + "」。";

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
                Portion("ing_potato", CutShape.Cut, CookMethod.Boiled, 130f, "season_salt", "season_pepper")
            };

            DishResult result = DeliciousnessScore.Evaluate(plate, definitions, soup, presentation);
            if (!Near(result.Score, 75f) || result.Picture == DishPictureKind.Recipe)
                return "切过与否要和食谱一致才算复现。原形不应是成品，分数 75，实际 " + result.Score;

            return null;
        }

        static string CheckCharcoal(
            RecipeDefinition soup,
            DishPresentation presentation,
            List<IngredientDefinition> definitions,
            IngredientDefinition mushroom)
        {
            IngredientInstance burnt = Portion("ing_mushroom", CutShape.Cut, CookMethod.BoiledAndFried, 180f, "season_oil", "season_salt", "season_pepper");
            var plate = new List<IngredientInstance>
            {
                burnt,
                Portion("ing_onion", CutShape.Cut, CookMethod.BoiledAndFried, 110f, "season_oil", "season_salt", "season_pepper"),
                Portion("ing_potato", CutShape.Cut, CookMethod.Boiled, 130f, "season_salt", "season_pepper")
            };

            DishResult result = DeliciousnessScore.Evaluate(plate, definitions, soup, presentation);
            if (!Near(result.Score, 45f))
                return "一份蘑菇焦炭时应为 45，实际 " + result.Score;

            if (!burnt.IsCharcoal)
                return "熟度达到最大熟度时要标成焦炭。";

            if (result.Picture != DishPictureKind.Charcoal || result.SummarySprite != presentation.charcoalSprite)
                return "有焦炭时总结图要用共用焦炭图。";

            if (result.Comment != "我把白蘑菇烧过头了……\n洋葱、星土豆的火候刚刚好！\n这一味调料看来是放对了。")
                return "焦炭评语不符，实际「" + result.Comment + "」。";

            if (mushroom == null)
                return "缺少白蘑菇定义。";

            return null;
        }

        static string CheckTwoCharcoalComments(List<IngredientDefinition> definitions)
        {
            var plate = new List<IngredientInstance>
            {
                Portion("ing_mushroom", CutShape.Cut, CookMethod.Boiled, 180f),
                Portion("ing_onion", CutShape.Cut, CookMethod.Fried, 170f)
            };

            DishResult result = DeliciousnessScore.Evaluate(plate, definitions, null, null);
            if (result.Comments.Count != 2)
                return "两份焦炭应有两句评语，实际 " + result.Comments.Count + "。";

            if (result.Comment != "我把白蘑菇烧过头了……\n我把洋葱烧过头了……")
                return "两份焦炭评语应按盘子顺序各写一句，实际「" + result.Comment + "」。";

            DishResult raw = DeliciousnessScore.Evaluate(
                new List<IngredientInstance>
                {
                    Portion("ing_mushroom", CutShape.Whole, CookMethod.Raw, 0f),
                    Portion("ing_beef", CutShape.Cut, CookMethod.Fried, 90f)
                },
                definitions,
                null,
                null);
            if (raw.Comment != "荤素搭配的饮食能给我带来健康的身体——虽然我好像用不着它了。\n虹彩牛肉的火候刚刚好！\n哎呀，白蘑菇是生的！希望不要拉肚子。\n哎呀，虹彩牛肉是生的！希望不要拉肚子。")
                return "未熟和荤素评语不符，实际「" + raw.Comment + "」。";

            return null;
        }

        static string CheckSeasoningOrder(RecipeDefinition soup, DishPresentation presentation, List<IngredientDefinition> definitions)
        {
            var plate = new List<IngredientInstance>
            {
                Portion("ing_mushroom", CutShape.Cut, CookMethod.BoiledAndFried, 120f, "season_pepper", "season_oil", "season_salt"),
                Portion("ing_onion", CutShape.Cut, CookMethod.BoiledAndFried, 110f, "season_pepper", "season_salt", "season_oil"),
                Portion("ing_potato", CutShape.Cut, CookMethod.Boiled, 130f, "season_pepper", "season_salt")
            };

            DishResult result = DeliciousnessScore.Evaluate(plate, definitions, soup, presentation);
            if (!Near(result.Score, 125f) || result.Picture != DishPictureKind.Recipe)
                return "调味不看顺序，颠倒后仍应是成品，分数 125，实际 " + result.Score;

            return null;
        }

        static string CheckSeasoningRepeat(RecipeDefinition soup, DishPresentation presentation, List<IngredientDefinition> definitions)
        {
            var plate = new List<IngredientInstance>
            {
                Portion("ing_mushroom", CutShape.Cut, CookMethod.BoiledAndFried, 120f, "season_oil", "season_oil", "season_salt", "season_pepper"),
                Portion("ing_onion", CutShape.Cut, CookMethod.BoiledAndFried, 110f, "season_oil", "season_salt", "season_salt", "season_pepper"),
                Portion("ing_potato", CutShape.Cut, CookMethod.Boiled, 130f, "season_salt", "season_pepper", "season_pepper")
            };

            DishResult result = DeliciousnessScore.Evaluate(plate, definitions, soup, presentation);
            if (!Near(result.Score, 125f) || result.Picture != DishPictureKind.Recipe)
                return "同一种调味多倒几次仍应是成品，分数 125，实际 " + result.Score;

            return null;
        }

        static string CheckWrongSeasoning(RecipeDefinition soup, DishPresentation presentation, List<IngredientDefinition> definitions)
        {
            var plate = new List<IngredientInstance>
            {
                Portion("ing_mushroom", CutShape.Cut, CookMethod.BoiledAndFried, 120f, "season_oil", "season_salt"),
                Portion("ing_onion", CutShape.Cut, CookMethod.BoiledAndFried, 110f, "season_oil", "season_salt", "season_pepper"),
                Portion("ing_potato", CutShape.Cut, CookMethod.Boiled, 130f, "season_salt", "season_pepper")
            };

            DishResult result = DeliciousnessScore.Evaluate(plate, definitions, soup, presentation);
            if (!Near(result.Score, 70f))
                return "少放一种调味时不加这 5 分，也不加复现的 50，应为 70，实际 " + result.Score;

            if (result.Picture != DishPictureKind.Loose || result.LooseFoods.Count != 3)
                return "少放一种调味时不算成品。";

            return null;
        }

        static string CheckHotpotHasNoSeasoningBonus(RecipeDefinition hotpot, List<IngredientDefinition> definitions)
        {
            var plate = new List<IngredientInstance>
            {
                Portion("ing_mushroom", CutShape.Cut, CookMethod.BoiledAndFried, 120f, "season_oil", "season_salt", "season_pepper"),
                Portion("ing_onion", CutShape.Cut, CookMethod.BoiledAndFried, 110f),
                Portion("ing_potato", CutShape.Cut, CookMethod.Boiled, 130f)
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
                Portion("ing_beef", CutShape.Cut, CookMethod.Fried, 90f),
                Portion("ing_onion", CutShape.Cut, CookMethod.BoiledAndFried, 110f)
            };

            DishResult result = DeliciousnessScore.Evaluate(plate, definitions, null, null);
            if (!Near(result.Score, 45f))
                return "荤素搭配加未熟扣分应为 45，实际 " + result.Score;

            if (beef.kind != IngredientKind.Meat || onion.kind != IngredientKind.Vegetable)
                return "荤素测试用的食材种类不对。";

            return null;
        }

        static string CheckOptimalWindow(List<IngredientDefinition> definitions)
        {
            float low = ScoreOf(definitions, 110f);
            float high = ScoreOf(definitions, 130f);
            float under = ScoreOf(definitions, 109f);
            float over = ScoreOf(definitions, 131f);
            if (!Near(low, 20f) || !Near(high, 20f))
                return "最佳熟度上下 10 应按最佳熟度加分，实际 " + low + " / " + high;

            if (!Near(under, 10f) || !Near(over, 10f))
                return "离开最佳熟度上下 10 后不再加这 10 分，实际 " + under + " / " + over;

            return null;
        }

        static string CheckSynergy(List<IngredientDefinition> definitions)
        {
            IngredientInstance mushroom = Portion("ing_mushroom", CutShape.Cut, CookMethod.Boiled, 120f);
            IngredientInstance onion = Portion("ing_onion", CutShape.Whole, CookMethod.Boiled, 110f);
            mushroom.OptimalHitTime = 1f;
            onion.OptimalHitTime = 1.8f;
            DishResult together = DeliciousnessScore.Evaluate(
                new List<IngredientInstance> { mushroom, onion },
                definitions,
                null,
                null);
            if (!Near(together.Score, 60f) || together.Comment.IndexOf("食材的相性很好，味道完美地融合在一起。") < 0)
                return "1 秒内先后达到最佳熟度应加 20 并写出相性评语，实际 " + together.Score + "「" + together.Comment + "」。";

            mushroom.OptimalHitTime = 1f;
            onion.OptimalHitTime = 2.2f;
            DishResult apart = DeliciousnessScore.Evaluate(
                new List<IngredientInstance> { mushroom, onion },
                definitions,
                null,
                null);
            if (!Near(apart.Score, 40f) || apart.Comment.IndexOf("食材的相性很好") >= 0)
                return "超过 1 秒不算同时达到最佳熟度，实际 " + apart.Score + "「" + apart.Comment + "」。";

            return null;
        }

        static float ScoreOf(List<IngredientDefinition> definitions, float doneness)
        {
            DishResult result = DeliciousnessScore.Evaluate(
                new List<IngredientInstance>
                {
                    Portion("ing_mushroom", CutShape.Cut, CookMethod.Boiled, doneness)
                },
                definitions,
                null,
                null);
            return result.Score;
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

        static string CheckVisual(IngredientDefinition mushroom, Sprite whole, Sprite cut)
        {
            mushroom.spriteWhole = null;
            mushroom.spriteCut = null;
            FoodVisual missing = FoodVisualResolver.Evaluate(mushroom, new IngredientInstance
            {
                Shape = CutShape.Whole,
                CookMethod = CookMethod.Raw,
                Doneness = 0f
            });
            if (missing.Sprite != null || missing.Color != Color.white)
                return "没有整颗图时应为白色。";

            mushroom.spriteWhole = whole;
            mushroom.spriteCut = cut;
            FoodVisual fried = FoodVisualResolver.Evaluate(mushroom, new IngredientInstance
            {
                Shape = CutShape.Whole,
                CookMethod = CookMethod.Fried,
                Doneness = 180f
            });
            if (fried.Sprite != whole)
                return "煎煮不换图，原形仍用整颗图。";

            FoodVisual sliced = FoodVisualResolver.Evaluate(mushroom, new IngredientInstance
            {
                Shape = CutShape.Cut,
                CookMethod = CookMethod.Boiled,
                Doneness = 180f
            });
            if (sliced.Sprite != cut)
                return "切开后用切开的图，煎煮不再另换一张。";

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
                shape = CutShape.Cut,
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
