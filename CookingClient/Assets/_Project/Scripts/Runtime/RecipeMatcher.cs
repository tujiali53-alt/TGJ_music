using System.Collections.Generic;
using Cooking.Data;

namespace Cooking.Runtime
{
    public static class RecipeMatcher
    {
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

                if (!SameSequence(item.SeasoningIds, ExpectedSeasonings(recipe, item.DefinitionId)))
                    return false;
            }

            return true;
        }

        public static List<string> ExpectedSeasonings(RecipeDefinition recipe, string ingredientId)
        {
            var expected = new List<string>();
            if (recipe == null || recipe.seasonings == null || string.IsNullOrEmpty(ingredientId))
                return expected;

            for (int i = 0; i < recipe.seasonings.Count; i++)
            {
                RecipeSeasoning seasoning = recipe.seasonings[i];
                if (seasoning.targetIngredientId != ingredientId || string.IsNullOrEmpty(seasoning.seasoningId))
                    continue;

                int count = RequirementCount(seasoning.count);
                for (int n = 0; n < count; n++)
                    expected.Add(seasoning.seasoningId);
            }

            return expected;
        }

        public static bool SameSequence(IList<string> actual, IList<string> expected)
        {
            int actualCount = actual == null ? 0 : actual.Count;
            int expectedCount = expected == null ? 0 : expected.Count;
            if (actualCount != expectedCount)
                return false;

            for (int i = 0; i < actualCount; i++)
            {
                if (actual[i] != expected[i])
                    return false;
            }

            return true;
        }

        static bool SameBody(IngredientInstance item, RecipeIngredient requirement)
        {
            return item.DefinitionId == requirement.ingredientId
                && item.Shape == requirement.shape
                && item.CookMethod == requirement.cookMethod;
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
