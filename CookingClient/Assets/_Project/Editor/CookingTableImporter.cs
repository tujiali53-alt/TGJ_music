using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using Cooking.Data;
using ExcelDataReader;
using UnityEditor;
using UnityEngine;

namespace Cooking.EditorTools
{
    public static class CookingTableImporter
    {
        const string TableAssetPath = "Assets/_Project/Data/Tables/CookingTables.xlsx";
        const string TableAssetPathFallback = "Assets/_Project/Data/Tables/CookingTables_new.xlsx";
        const string IngredientsFolder = "Assets/_Project/Data/Ingredients";
        const string SeasoningsFolder = "Assets/_Project/Data/Seasonings";
        const string RecipesFolder = "Assets/_Project/Data/Recipes";

        static string ResolveTableAssetPath()
        {
            string primary = ToAbsolutePath(TableAssetPath);
            if (File.Exists(primary))
            {
                // Prefer rewritten workbook if official file is still the locked old copy.
                string fallback = ToAbsolutePath(TableAssetPathFallback);
                if (File.Exists(fallback)
                    && File.GetLastWriteTimeUtc(fallback) > File.GetLastWriteTimeUtc(primary))
                    return TableAssetPathFallback;

                return TableAssetPath;
            }

            if (File.Exists(ToAbsolutePath(TableAssetPathFallback)))
                return TableAssetPathFallback;

            return TableAssetPath;
        }

        [MenuItem("Cooking/Import Cooking Tables")]
        public static void ImportMenu()
        {
            ImportResult result = Import();
            if (result.ErrorCount > 0)
                Debug.LogError(
                    "Cooking tables import finished with errors. Created "
                    + result.Created + ", updated " + result.Updated + ", errors "
                    + result.ErrorCount + ".");
            else
                Debug.Log(
                    "Cooking tables import OK. Created " + result.Created
                    + ", updated " + result.Updated + ".");
        }

        [MenuItem("Cooking/Reveal Cooking Tables")]
        public static void RevealTables()
        {
            string assetPath = ResolveTableAssetPath();
            string absolute = ToAbsolutePath(assetPath);
            if (!File.Exists(absolute))
            {
                Debug.LogError("缺少表文件：" + TableAssetPath);
                return;
            }

            EditorUtility.RevealInFinder(assetPath);
        }

        [MenuItem("Cooking/Validate Imported Day1 Recipe")]
        public static void ValidateImportedDay1Menu()
        {
            string error = ValidateImportedDay1();
            if (error == null)
                Debug.Log("导入的 recipe_day1 与 Day1 期望一致。");
            else
                Debug.LogError(error);
        }

        public static string ValidateImportedDay1()
        {
            RecipeDefinition recipe = FindById<RecipeDefinition>(RecipesFolder, "recipe_day1");
            if (recipe == null)
                return "缺少 recipe_day1，请先执行 Cooking/Import Cooking Tables。";

            if (recipe.displayName != "什锦蘑菇浓汤")
                return "displayName 应为 什锦蘑菇浓汤，实际 " + recipe.displayName;

            if (recipe.unlockDay != 1)
                return "unlockDay 应为 1，实际 " + recipe.unlockDay;

            if (recipe.ingredients == null || recipe.ingredients.Count != 3)
                return "ingredients 应有 3 条，实际 " + (recipe.ingredients == null ? 0 : recipe.ingredients.Count);

            if (!HasIngredient(recipe, "ing_mushroom", CutShape.Slice, CookMethod.BoiledAndFried)
                || !HasIngredient(recipe, "ing_onion", CutShape.Slice, CookMethod.BoiledAndFried)
                || !HasIngredient(recipe, "ing_potato", CutShape.Slice, CookMethod.Boiled))
                return "RecipeIngredients 与 Day1 期望不一致。";

            string[] expectedSeasonings =
            {
                "season_oil:ing_mushroom",
                "season_salt:ing_mushroom",
                "season_pepper:ing_mushroom",
                "season_oil:ing_onion",
                "season_salt:ing_onion",
                "season_pepper:ing_onion",
                "season_salt:ing_potato",
                "season_pepper:ing_potato"
            };

            if (recipe.seasonings == null || recipe.seasonings.Count != expectedSeasonings.Length)
                return "seasonings 数量应为 " + expectedSeasonings.Length + "，实际 "
                    + (recipe.seasonings == null ? 0 : recipe.seasonings.Count);

            for (int i = 0; i < expectedSeasonings.Length; i++)
            {
                RecipeSeasoning s = recipe.seasonings[i];
                string key = s.seasoningId + ":" + s.targetIngredientId;
                if (key != expectedSeasonings[i])
                    return "调味顺序第 " + (i + 1) + " 项应为 " + expectedSeasonings[i] + "，实际 " + key;
            }

            SeasoningDefinition salt = FindById<SeasoningDefinition>(SeasoningsFolder, "season_salt");
            SeasoningDefinition oil = FindById<SeasoningDefinition>(SeasoningsFolder, "season_oil");
            SeasoningDefinition pepper = FindById<SeasoningDefinition>(SeasoningsFolder, "season_pepper");
            if (salt == null || oil == null || pepper == null)
                return "缺少调味品资产 season_oil / season_salt / season_pepper。";

            IngredientDefinition mushroom = FindById<IngredientDefinition>(IngredientsFolder, "ing_mushroom");
            if (mushroom == null || mushroom.optimalDoneness != 120f || mushroom.maxDoneness != 180f)
                return "ing_mushroom 熟度字段与期望不一致。";

            return null;
        }

        static bool HasIngredient(
            RecipeDefinition recipe,
            string ingredientId,
            CutShape shape,
            CookMethod method)
        {
            for (int i = 0; i < recipe.ingredients.Count; i++)
            {
                RecipeIngredient item = recipe.ingredients[i];
                if (item.ingredientId == ingredientId
                    && item.shape == shape
                    && item.cookMethod == method)
                    return true;
            }

            return false;
        }

        public static ImportResult Import()
        {
            var result = new ImportResult();
            string assetPath = ResolveTableAssetPath();
            string absolute = ToAbsolutePath(assetPath);
            if (!File.Exists(absolute))
            {
                Debug.LogError("缺少表文件：" + TableAssetPath);
                result.ErrorCount++;
                return result;
            }

            if (assetPath != TableAssetPath)
                Debug.LogWarning("使用较新的表文件导入：" + assetPath);

            EnsureFolders();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            DataSet dataSet;
            try
            {
                using (FileStream stream = File.Open(absolute, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream))
                {
                    dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
                    {
                        ConfigureDataTable = _ => new ExcelDataTableConfiguration
                        {
                            UseHeaderRow = true
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("读取 Excel 失败：" + ex.Message);
                result.ErrorCount++;
                return result;
            }

            var ingredientIds = new HashSet<string>();
            var seasoningIds = new HashSet<string>();
            var recipeIds = new HashSet<string>();
            var visualByIngredient = new Dictionary<string, List<IngredientVisualState>>();

            ImportIngredients(RequireSheet(dataSet, "Ingredients", result), ingredientIds, result);
            ImportIngredientVisualStates(
                OptionalSheet(dataSet, "IngredientVisualStates"),
                visualByIngredient,
                result);
            ApplyVisualStates(visualByIngredient, result);
            ImportSeasonings(RequireSheet(dataSet, "Seasonings", result), seasoningIds, result);
            ImportRecipes(RequireSheet(dataSet, "Recipes", result), recipeIds, result);
            ImportRecipeIngredients(RequireSheet(dataSet, "RecipeIngredients", result), result);
            ImportRecipeSeasonings(RequireSheet(dataSet, "RecipeSeasonings", result), result);
            // steps 日记功能尚未接入，Sheet 可选
            ImportRecipeSteps(OptionalSheet(dataSet, "RecipeSteps"), result);

            WarnMissing("Ingredients", IngredientsFolder, typeof(IngredientDefinition), ingredientIds);
            WarnMissing("Seasonings", SeasoningsFolder, typeof(SeasoningDefinition), seasoningIds);
            WarnMissing("Recipes", RecipesFolder, typeof(RecipeDefinition), recipeIds);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return result;
        }

        static void ImportIngredients(DataTable table, HashSet<string> ids, ImportResult result)
        {
            if (table == null)
                return;

            foreach (DataRow row in table.Rows)
            {
                string id = Cell(row, "id");
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                id = id.Trim();
                ids.Add(id);

                IngredientDefinition asset = FindOrCreate<IngredientDefinition>(
                    IngredientsFolder, id, result);
                asset.id = id;
                asset.displayName = Cell(row, "displayName");
                asset.description = Cell(row, "description");

                if (!TryParseEnum(Cell(row, "kind"), out IngredientKind kind, "Ingredients/" + id + ".kind", result))
                    continue;
                asset.kind = kind;

                if (!TryParseBool(Cell(row, "canEatRaw"), out bool canEatRaw, "Ingredients/" + id + ".canEatRaw", result))
                    continue;
                asset.canEatRaw = canEatRaw;

                if (!TryParseBool(Cell(row, "canCut"), out bool canCut, "Ingredients/" + id + ".canCut", result))
                    continue;
                asset.canCut = canCut;

                if (!TryParseFloat(Cell(row, "optimalDoneness"), out float optimal, "Ingredients/" + id + ".optimalDoneness", result))
                    continue;
                asset.optimalDoneness = optimal;

                if (!TryParseFloat(Cell(row, "maxDoneness"), out float max, "Ingredients/" + id + ".maxDoneness", result))
                    continue;
                asset.maxDoneness = max;

                ApplySprite(
                    FirstCell(row, "spriteWhole", "spriteWholePath"),
                    ref asset.spriteWhole,
                    "Ingredients/" + id + ".spriteWhole",
                    result);

                string colorText = Cell(row, "baseColor");
                if (!string.IsNullOrWhiteSpace(colorText))
                {
                    if (TryParseColor(colorText, out Color color))
                        asset.baseColor = color;
                    else
                    {
                        Debug.LogError("无法解析颜色：" + colorText + " @ Ingredients/" + id);
                        result.ErrorCount++;
                    }
                }

                EditorUtility.SetDirty(asset);
            }
        }

        static void ImportIngredientVisualStates(
            DataTable table,
            Dictionary<string, List<IngredientVisualState>> visualByIngredient,
            ImportResult result)
        {
            if (table == null)
                return;

            foreach (DataRow row in table.Rows)
            {
                string ingredientId = Cell(row, "ingredientId");
                if (string.IsNullOrWhiteSpace(ingredientId))
                    continue;

                ingredientId = ingredientId.Trim();
                if (!TryParseEnum(Cell(row, "shape"), out CutShape shape, "IngredientVisualStates/" + ingredientId + ".shape", result))
                    continue;
                if (!TryParseEnum(Cell(row, "cookMethod"), out CookMethod cookMethod, "IngredientVisualStates/" + ingredientId + ".cookMethod", result))
                    continue;

                Sprite sprite = null;
                string spritePath = FirstCell(row, "sprite", "spritePath");
                if (!string.IsNullOrWhiteSpace(spritePath))
                {
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath.Trim());
                    if (sprite == null)
                    {
                        Debug.LogError("找不到 Sprite：" + spritePath + " @ IngredientVisualStates/" + ingredientId);
                        result.ErrorCount++;
                        continue;
                    }
                }

                if (!visualByIngredient.TryGetValue(ingredientId, out List<IngredientVisualState> list))
                {
                    list = new List<IngredientVisualState>();
                    visualByIngredient[ingredientId] = list;
                }

                list.Add(new IngredientVisualState
                {
                    shape = shape,
                    cookMethod = cookMethod,
                    sprite = sprite
                });
            }
        }

        static void ApplyVisualStates(
            Dictionary<string, List<IngredientVisualState>> visualByIngredient,
            ImportResult result)
        {
            foreach (KeyValuePair<string, List<IngredientVisualState>> pair in visualByIngredient)
            {
                IngredientDefinition asset = FindById<IngredientDefinition>(IngredientsFolder, pair.Key);
                if (asset == null)
                {
                    Debug.LogError("IngredientVisualStates 找不到食材：" + pair.Key);
                    result.ErrorCount++;
                    continue;
                }

                asset.stateSprites = new List<IngredientVisualState>(pair.Value);
                EditorUtility.SetDirty(asset);
            }
        }

        static void ImportSeasonings(DataTable table, HashSet<string> ids, ImportResult result)
        {
            if (table == null)
                return;

            foreach (DataRow row in table.Rows)
            {
                string id = Cell(row, "id");
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                id = id.Trim();
                ids.Add(id);

                SeasoningDefinition asset = FindOrCreate<SeasoningDefinition>(SeasoningsFolder, id, result);
                asset.id = id;
                asset.displayName = Cell(row, "displayName");
                ApplySprite(
                    FirstCell(row, "iconPath", "icon"),
                    ref asset.icon,
                    "Seasonings/" + id + ".icon",
                    result);
                EditorUtility.SetDirty(asset);
            }
        }

        static void ImportRecipes(DataTable table, HashSet<string> ids, ImportResult result)
        {
            if (table == null)
                return;

            foreach (DataRow row in table.Rows)
            {
                string id = Cell(row, "id");
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                id = id.Trim();
                ids.Add(id);

                RecipeDefinition asset = FindOrCreate<RecipeDefinition>(RecipesFolder, id, result);
                asset.id = id;
                asset.displayName = Cell(row, "displayName");

                string unlockText = Cell(row, "unlockDay");
                if (string.IsNullOrWhiteSpace(unlockText))
                    asset.unlockDay = 1;
                else if (TryParseInt(unlockText, out int unlockDay, "Recipes/" + id + ".unlockDay", result))
                    asset.unlockDay = unlockDay;
                else
                    continue;

                ApplySprite(
                    FirstCell(row, "finishedSpritePath", "finishedSprite"),
                    ref asset.finishedSprite,
                    "Recipes/" + id + ".finishedSprite",
                    result);

                if (asset.ingredients == null)
                    asset.ingredients = new List<RecipeIngredient>();
                else
                    asset.ingredients.Clear();

                if (asset.seasonings == null)
                    asset.seasonings = new List<RecipeSeasoning>();
                else
                    asset.seasonings.Clear();

                if (asset.steps == null)
                    asset.steps = new List<RecipeStep>();
                else
                    asset.steps.Clear();

                EditorUtility.SetDirty(asset);
            }
        }

        static void ImportRecipeIngredients(DataTable table, ImportResult result)
        {
            if (table == null)
                return;

            var grouped = new Dictionary<string, List<RecipeIngredient>>();
            foreach (DataRow row in table.Rows)
            {
                string recipeId = Cell(row, "recipeId");
                if (string.IsNullOrWhiteSpace(recipeId))
                    continue;

                recipeId = recipeId.Trim();
                string ingredientId = FirstCell(row, "ingredientId", "ingredient Id").Trim();
                if (string.IsNullOrEmpty(ingredientId))
                {
                    Debug.LogError("RecipeIngredients 缺少 ingredientId @ " + recipeId);
                    result.ErrorCount++;
                    continue;
                }

                if (!TryParseEnum(
                        FirstCell(row, "shape"),
                        out CutShape shape,
                        "RecipeIngredients/" + recipeId + ".shape",
                        result))
                    continue;

                if (!TryParseEnum(
                        FirstCell(row, "cookMethod", "cook Method"),
                        out CookMethod cookMethod,
                        "RecipeIngredients/" + recipeId + ".cookMethod",
                        result))
                    continue;

                int count = 1;
                string countText = Cell(row, "count");
                if (!string.IsNullOrWhiteSpace(countText)
                    && !TryParseInt(countText, out count, "RecipeIngredients/" + recipeId + ".count", result))
                    continue;
                if (count <= 0)
                    count = 1;

                if (!grouped.TryGetValue(recipeId, out List<RecipeIngredient> list))
                {
                    list = new List<RecipeIngredient>();
                    grouped[recipeId] = list;
                }

                list.Add(new RecipeIngredient
                {
                    ingredientId = ingredientId,
                    shape = shape,
                    cookMethod = cookMethod,
                    count = count
                });
            }

            foreach (KeyValuePair<string, List<RecipeIngredient>> pair in grouped)
            {
                RecipeDefinition recipe = FindById<RecipeDefinition>(RecipesFolder, pair.Key);
                if (recipe == null)
                {
                    Debug.LogError("RecipeIngredients 找不到食谱：" + pair.Key);
                    result.ErrorCount++;
                    continue;
                }

                recipe.ingredients = pair.Value;
                EditorUtility.SetDirty(recipe);
            }
        }

        static void ImportRecipeSeasonings(DataTable table, ImportResult result)
        {
            if (table == null)
                return;

            var grouped = new Dictionary<string, List<RecipeSeasoning>>();
            foreach (DataRow row in table.Rows)
            {
                string recipeId = Cell(row, "recipeId");
                if (string.IsNullOrWhiteSpace(recipeId))
                    continue;

                recipeId = recipeId.Trim();
                string seasoningId = FirstCell(row, "seasoningId", "seasoning Id").Trim();
                string targetIngredientId = FirstCell(
                    row,
                    "targetIngredientId",
                    "target Ingredient Id").Trim();
                if (string.IsNullOrEmpty(seasoningId) || string.IsNullOrEmpty(targetIngredientId))
                {
                    Debug.LogError("RecipeSeasonings 缺少 seasoningId 或 targetIngredientId @ " + recipeId);
                    result.ErrorCount++;
                    continue;
                }

                int count = 1;
                string countText = Cell(row, "count");
                if (!string.IsNullOrWhiteSpace(countText)
                    && !TryParseInt(countText, out count, "RecipeSeasonings/" + recipeId + ".count", result))
                    continue;
                if (count <= 0)
                    count = 1;

                if (!grouped.TryGetValue(recipeId, out List<RecipeSeasoning> list))
                {
                    list = new List<RecipeSeasoning>();
                    grouped[recipeId] = list;
                }

                list.Add(new RecipeSeasoning
                {
                    seasoningId = seasoningId,
                    targetIngredientId = targetIngredientId,
                    count = count
                });
            }

            foreach (KeyValuePair<string, List<RecipeSeasoning>> pair in grouped)
            {
                RecipeDefinition recipe = FindById<RecipeDefinition>(RecipesFolder, pair.Key);
                if (recipe == null)
                {
                    Debug.LogError("RecipeSeasonings 找不到食谱：" + pair.Key);
                    result.ErrorCount++;
                    continue;
                }

                recipe.seasonings = pair.Value;
                EditorUtility.SetDirty(recipe);
            }
        }

        static void ImportRecipeSteps(DataTable table, ImportResult result)
        {
            if (table == null)
                return;

            var grouped = new Dictionary<string, List<RecipeStep>>();
            foreach (DataRow row in table.Rows)
            {
                string recipeId = Cell(row, "recipeId");
                if (string.IsNullOrWhiteSpace(recipeId))
                    continue;

                recipeId = recipeId.Trim();
                if (!TryParseInt(Cell(row, "order"), out int order, "RecipeSteps/" + recipeId + ".order", result))
                    continue;

                CookAction action = CookAction.Cut;
                string actionText = Cell(row, "action");
                if (!string.IsNullOrWhiteSpace(actionText)
                    && !TryParseEnum(actionText, out action, "RecipeSteps/" + recipeId + ".action", result))
                    continue;

                CookMethod cookMethod = CookMethod.Raw;
                string methodText = Cell(row, "cookMethod");
                if (!string.IsNullOrWhiteSpace(methodText)
                    && !TryParseEnum(methodText, out cookMethod, "RecipeSteps/" + recipeId + ".cookMethod", result))
                    continue;

                if (!grouped.TryGetValue(recipeId, out List<RecipeStep> list))
                {
                    list = new List<RecipeStep>();
                    grouped[recipeId] = list;
                }

                list.Add(new RecipeStep
                {
                    order = order,
                    hintText = Cell(row, "hintText"),
                    action = action,
                    ingredientId = Cell(row, "ingredientId").Trim(),
                    cookMethod = cookMethod,
                    seasoningId = Cell(row, "seasoningId").Trim()
                });
            }

            foreach (KeyValuePair<string, List<RecipeStep>> pair in grouped)
            {
                RecipeDefinition recipe = FindById<RecipeDefinition>(RecipesFolder, pair.Key);
                if (recipe == null)
                {
                    Debug.LogError("RecipeSteps 找不到食谱：" + pair.Key);
                    result.ErrorCount++;
                    continue;
                }

                pair.Value.Sort((a, b) => a.order.CompareTo(b.order));
                recipe.steps = pair.Value;
                EditorUtility.SetDirty(recipe);
            }
        }

        static DataTable RequireSheet(DataSet dataSet, string name, ImportResult result)
        {
            DataTable table = dataSet.Tables[name];
            if (table == null)
            {
                Debug.LogError("Excel 缺少 Sheet：" + name);
                result.ErrorCount++;
            }

            return table;
        }

        static DataTable OptionalSheet(DataSet dataSet, string name)
        {
            return dataSet.Tables[name];
        }

        static string FirstCell(DataRow row, params string[] columns)
        {
            for (int i = 0; i < columns.Length; i++)
            {
                string value = Cell(row, columns[i]);
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return "";
        }

        static void ApplySprite(string path, ref Sprite target, string context, ImportResult result)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path.Trim());
            if (sprite == null)
            {
                Debug.LogError("找不到 Sprite：" + path + " @ " + context);
                result.ErrorCount++;
                return;
            }

            target = sprite;
        }

        static T FindOrCreate<T>(string folder, string id, ImportResult result) where T : ScriptableObject
        {
            T existing = FindById<T>(folder, id);
            if (existing != null)
            {
                result.Updated++;
                return existing;
            }

            string path = folder + "/" + id + ".asset";
            T created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            result.Created++;
            return created;
        }

        static T FindById<T>(string folder, string id) where T : ScriptableObject
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset == null)
                    continue;

                string assetId = ReadId(asset);
                if (!string.IsNullOrEmpty(assetId) && assetId.Trim() == id)
                    return asset;

                if (Path.GetFileNameWithoutExtension(path) == id)
                    return asset;
            }

            return null;
        }

        static string ReadId(ScriptableObject asset)
        {
            if (asset is IngredientDefinition ingredient)
                return ingredient.id;
            if (asset is SeasoningDefinition seasoning)
                return seasoning.id;
            if (asset is RecipeDefinition recipe)
                return recipe.id;
            return null;
        }

        static void WarnMissing(string label, string folder, Type type, HashSet<string> presentIds)
        {
            string[] guids = AssetDatabase.FindAssets("t:" + type.Name, new[] { folder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null)
                    continue;

                string id = ReadId(asset);
                if (string.IsNullOrWhiteSpace(id))
                    id = Path.GetFileNameWithoutExtension(path);

                if (!presentIds.Contains(id.Trim()))
                    Debug.LogWarning(label + " 表中未出现，保留资产：" + path + " (id=" + id + ")");
            }
        }

        static void EnsureFolders()
        {
            EnsureFolder("Assets/_Project/Data");
            EnsureFolder(IngredientsFolder);
            EnsureFolder(SeasoningsFolder);
            EnsureFolder(RecipesFolder);
            EnsureFolder("Assets/_Project/Data/Tables");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }

        static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.GetFullPath(Path.Combine(projectRoot ?? "", assetPath));
        }

        static string Cell(DataRow row, string column)
        {
            if (!row.Table.Columns.Contains(column))
                return "";

            object value = row[column];
            if (value == null || value == DBNull.Value)
                return "";

            return Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? "";
        }

        static bool TryParseEnum<T>(string text, out T value, string context, ImportResult result) where T : struct
        {
            value = default;
            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.LogError("缺少枚举值 @ " + context);
                result.ErrorCount++;
                return false;
            }

            if (Enum.TryParse(text.Trim(), true, out value))
                return true;

            Debug.LogError("无法解析枚举 " + typeof(T).Name + "='" + text + "' @ " + context);
            result.ErrorCount++;
            return false;
        }

        static bool TryParseBool(string text, out bool value, string context, ImportResult result)
        {
            value = false;
            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.LogError("缺少布尔值 @ " + context);
                result.ErrorCount++;
                return false;
            }

            text = text.Trim();
            if (text == "1" || text.Equals("true", StringComparison.OrdinalIgnoreCase)
                || text.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || text.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }

            if (text == "0" || text.Equals("false", StringComparison.OrdinalIgnoreCase)
                || text.Equals("no", StringComparison.OrdinalIgnoreCase)
                || text.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
            {
                value = false;
                return true;
            }

            Debug.LogError("无法解析布尔值='" + text + "' @ " + context);
            result.ErrorCount++;
            return false;
        }

        static bool TryParseFloat(string text, out float value, string context, ImportResult result)
        {
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return true;

            Debug.LogError("无法解析数字='" + text + "' @ " + context);
            result.ErrorCount++;
            return false;
        }

        static bool TryParseInt(string text, out int value, string context, ImportResult result)
        {
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                return true;

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double asDouble))
            {
                value = (int)Math.Round(asDouble);
                return true;
            }

            Debug.LogError("无法解析整数='" + text + "' @ " + context);
            result.ErrorCount++;
            value = 0;
            return false;
        }

        static bool TryParseColor(string text, out Color color)
        {
            text = text.Trim();
            if (text.StartsWith("#", StringComparison.Ordinal) && ColorUtility.TryParseHtmlString(text, out color))
                return true;

            string[] parts = text.Split(',');
            if (parts.Length >= 3
                && float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float r)
                && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float g)
                && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float b))
            {
                float a = 1f;
                if (parts.Length >= 4)
                    float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out a);
                color = new Color(r, g, b, a);
                return true;
            }

            color = Color.white;
            return false;
        }

        public sealed class ImportResult
        {
            public int Created;
            public int Updated;
            public int ErrorCount;
        }
    }
}
