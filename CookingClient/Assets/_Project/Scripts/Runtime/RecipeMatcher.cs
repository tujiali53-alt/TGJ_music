using System.Collections.Generic;
using Cooking.Data;

namespace Cooking.Runtime
{
    public static class RecipeMatcher
    {
        public static bool IsFreeform(RecipeDefinition recipe)
        {
            return recipe != null
                && (recipe.ingredients == null || recipe.ingredients.Count == 0)
                && recipe.finishedSprite != null;
        }

        public static bool FreeformPicture(
            IList<IngredientInstance> plate,
            IList<IngredientDefinition> definitions,
            RecipeDefinition recipe)
        {
            if (!IsFreeform(recipe) || plate == null)
                return false;

            for (int i = 0; i < plate.Count; i++)
            {
                IngredientInstance item = plate[i];
                if (item != null && item.Quantity > 0)
                    return true;
            }

            return false;
        }

        public static bool Matches(
            IList<IngredientInstance> plate,
            IList<IngredientDefinition> definitions,
            RecipeDefinition recipe)
        {
            if (recipe == null || recipe.ingredients == null || recipe.ingredients.Count == 0)
                return false;

            if (plate == null)
                return false;

            var pool = new List<PoolEntry>();
            for (int i = 0; i < plate.Count; i++)
            {
                IngredientInstance item = plate[i];
                if (item == null || item.Quantity <= 0)
                    continue;

                IngredientDefinition definition = Find(definitions, item.DefinitionId);
                if (definition == null)
                    return false;

                if (item.Doneness >= definition.maxDoneness)
                    return false;

                pool.Add(new PoolEntry
                {
                    Item = item,
                    Remaining = item.Quantity
                });
            }

            for (int r = 0; r < recipe.ingredients.Count; r++)
            {
                RecipeIngredient requirement = recipe.ingredients[r];
                int need = RequirementCount(requirement.count);
                for (int p = 0; p < pool.Count && need > 0; p++)
                {
                    PoolEntry entry = pool[p];
                    if (entry.Remaining <= 0 || !SameBody(entry.Item, requirement))
                        continue;

                    int take = entry.Remaining < need ? entry.Remaining : need;
                    entry.Remaining -= take;
                    need -= take;
                }

                if (need > 0)
                    return false;
            }

            for (int p = 0; p < pool.Count; p++)
            {
                if (pool[p].Remaining > 0)
                    return false;
            }

            for (int i = 0; i < plate.Count; i++)
            {
                IngredientInstance item = plate[i];
                if (item == null || item.Quantity <= 0)
                    continue;

                if (!SameSeasonings(
                    item.SeasoningIds,
                    ExpectedSeasonings(recipe, item.DefinitionId),
                    OptionalSeasonings(recipe, item.DefinitionId)))
                    return false;
            }

            if (!AlternativeSeasoningsSatisfied(plate, recipe))
                return false;

            return true;
        }

        // count > 0：必需调料
        public static List<string> ExpectedSeasonings(RecipeDefinition recipe, string ingredientId)
        {
            var result = new List<string>();
            if (recipe == null || recipe.seasonings == null || string.IsNullOrEmpty(ingredientId))
                return result;

            for (int i = 0; i < recipe.seasonings.Count; i++)
            {
                RecipeSeasoning seasoning = recipe.seasonings[i];
                if (seasoning.targetIngredientId != ingredientId || string.IsNullOrEmpty(seasoning.seasoningId))
                    continue;
                if (seasoning.count <= 0)
                    continue;

                for (int n = 0; n < seasoning.count; n++)
                    result.Add(seasoning.seasoningId);
            }

            return result;
        }

        // count == 0：可选；count < 0：互斥组候选项（允许出现，是否满足由组判定）
        public static List<string> OptionalSeasonings(RecipeDefinition recipe, string ingredientId)
        {
            var result = new List<string>();
            if (recipe == null || recipe.seasonings == null || string.IsNullOrEmpty(ingredientId))
                return result;

            for (int i = 0; i < recipe.seasonings.Count; i++)
            {
                RecipeSeasoning seasoning = recipe.seasonings[i];
                if (seasoning.targetIngredientId != ingredientId || string.IsNullOrEmpty(seasoning.seasoningId))
                    continue;
                if (seasoning.count > 0)
                    continue;

                result.Add(seasoning.seasoningId);
            }

            return result;
        }

        // 同一食谱内，count 为相同负数的调料至少满足其一
        static bool AlternativeSeasoningsSatisfied(IList<IngredientInstance> plate, RecipeDefinition recipe)
        {
            if (recipe == null || recipe.seasonings == null)
                return true;

            var groups = new Dictionary<int, List<RecipeSeasoning>>();
            for (int i = 0; i < recipe.seasonings.Count; i++)
            {
                RecipeSeasoning seasoning = recipe.seasonings[i];
                if (seasoning.count >= 0 || string.IsNullOrEmpty(seasoning.seasoningId))
                    continue;

                if (!groups.TryGetValue(seasoning.count, out List<RecipeSeasoning> list))
                {
                    list = new List<RecipeSeasoning>();
                    groups[seasoning.count] = list;
                }

                list.Add(seasoning);
            }

            foreach (KeyValuePair<int, List<RecipeSeasoning>> pair in groups)
            {
                bool any = false;
                List<RecipeSeasoning> options = pair.Value;
                for (int i = 0; i < options.Count; i++)
                {
                    if (SeasoningPresent(plate, options[i].targetIngredientId, options[i].seasoningId))
                    {
                        any = true;
                        break;
                    }
                }

                if (!any)
                    return false;
            }

            return true;
        }

        static bool SeasoningPresent(IList<IngredientInstance> plate, string ingredientId, string seasoningId)
        {
            if (plate == null || string.IsNullOrEmpty(ingredientId) || string.IsNullOrEmpty(seasoningId))
                return false;

            for (int i = 0; i < plate.Count; i++)
            {
                IngredientInstance item = plate[i];
                if (item == null || item.Quantity <= 0 || item.DefinitionId != ingredientId)
                    continue;
                if (item.SeasoningIds == null)
                    continue;

                for (int s = 0; s < item.SeasoningIds.Count; s++)
                {
                    if (item.SeasoningIds[s] == seasoningId)
                        return true;
                }
            }

            return false;
        }

        public static bool SameSeasonings(IList<string> actual, IList<string> expected)
        {
            return SameSeasonings(actual, expected, null);
        }

        public static bool SameSeasonings(IList<string> actual, IList<string> expected, IList<string> optional)
        {
            var expectedIds = new HashSet<string>();
            var actualIds = new HashSet<string>();
            var optionalIds = new HashSet<string>();
            Collect(expectedIds, expected);
            Collect(actualIds, actual);
            Collect(optionalIds, optional);

            foreach (string id in expectedIds)
            {
                if (!actualIds.Contains(id))
                    return false;
            }

            foreach (string id in actualIds)
            {
                if (!expectedIds.Contains(id) && !optionalIds.Contains(id))
                    return false;
            }

            return true;
        }

        static void Collect(HashSet<string> ids, IList<string> source)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                string id = source[i];
                if (!string.IsNullOrEmpty(id))
                    ids.Add(id);
            }
        }

        static bool SameBody(IngredientInstance item, RecipeIngredient requirement)
        {
            return item.DefinitionId == requirement.ingredientId
                && item.CookMethod == requirement.cookMethod
                && item.Shape == requirement.shape;
        }

        static int RequirementCount(int count)
        {
            return count <= 0 ? 1 : count;
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

        sealed class PoolEntry
        {
            public IngredientInstance Item;
            public int Remaining;
        }
    }
}
