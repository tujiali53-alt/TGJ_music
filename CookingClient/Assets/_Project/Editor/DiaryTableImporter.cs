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
    public static class DiaryTableImporter
    {
        const string TableAssetPath = "Assets/_Project/Data/Tables/DiaryTables.xlsx";
        const string CatalogAssetPath = "Assets/_Project/Data/Diary/DiaryCatalog.asset";
        const string RecipesFolder = "Assets/_Project/Data/Recipes";

        [MenuItem("Cooking/Import Diary Tables")]
        public static void ImportMenu()
        {
            ImportResult result = Import();
            if (result.ErrorCount > 0)
                Debug.LogError(
                    "Diary tables import finished with errors. Created "
                    + result.Created + ", updated " + result.Updated + ", errors "
                    + result.ErrorCount + ".");
            else
                Debug.Log(
                    "Diary tables import OK. Created " + result.Created
                    + ", updated " + result.Updated + ".");
        }

        public static ImportResult Import()
        {
            var result = new ImportResult();
            string absolute = ToAbsolutePath(TableAssetPath);
            if (!File.Exists(absolute))
            {
                Debug.LogError("缺少表文件：" + TableAssetPath);
                result.ErrorCount++;
                return result;
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            DataSet dataSet;
            try
            {
                using (FileStream stream = File.Open(absolute, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream))
                {
                    dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
                    {
                        ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("读取 Excel 失败：" + ex.Message);
                result.ErrorCount++;
                return result;
            }

            DataTable entryTable = RequireSheet(dataSet, "DiaryEntries", result);
            DataTable paragraphTable = RequireSheet(dataSet, "DiaryParagraphs", result);
            DataTable stringTable = RequireSheet(dataSet, "DiaryStrings", result);
            if (result.ErrorCount > 0)
                return result;

            List<DiaryStringRow> strings = ReadStrings(stringTable, result);
            List<DiaryEntry> entries = ReadEntries(entryTable, strings, result);
            ReadParagraphs(paragraphTable, entries, result);
            if (result.ErrorCount > 0)
                return result;

            WriteCatalog(entries, strings, result);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return result;
        }

        static List<DiaryStringRow> ReadStrings(DataTable table, ImportResult result)
        {
            var rows = new List<DiaryStringRow>();
            var seen = new HashSet<string>();
            foreach (DataRow row in table.Rows)
            {
                string id = Cell(row, "id");
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                id = id.Trim();
                if (!seen.Add(id))
                {
                    Debug.LogError("DiaryStrings id 重复：" + id);
                    result.ErrorCount++;
                    continue;
                }

                rows.Add(new DiaryStringRow
                {
                    id = id,
                    textZh = Cell(row, "textZh"),
                    textEn = Cell(row, "textEn")
                });
            }

            RequireString(rows, "title_past", result);
            RequireString(rows, "title_present", result);
            RequireString(rows, "day_format", result);
            return rows;
        }

        static void RequireString(List<DiaryStringRow> rows, string id, ImportResult result)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].id == id)
                    return;
            }

            Debug.LogError("DiaryStrings 缺少 " + id);
            result.ErrorCount++;
        }

        static List<DiaryEntry> ReadEntries(DataTable table, List<DiaryStringRow> strings, ImportResult result)
        {
            var entries = new List<DiaryEntry>();
            var seen = new HashSet<string>();
            foreach (DataRow row in table.Rows)
            {
                string id = Cell(row, "id");
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                id = id.Trim();
                if (!seen.Add(id))
                {
                    Debug.LogError("DiaryEntries id 重复：" + id);
                    result.ErrorCount++;
                    continue;
                }

                string recipeId = Cell(row, "recipeId");
                if (FindRecipe(recipeId) == null)
                {
                    Debug.LogError("DiaryEntries 找不到食谱：" + recipeId + " @ " + id);
                    result.ErrorCount++;
                    continue;
                }

                if (!TryParseEnum(Cell(row, "kind"), out DiaryKind kind, "DiaryEntries/" + id + ".kind", result))
                    continue;
                if (!TryParseInt(Cell(row, "storyDay"), out int storyDay, "DiaryEntries/" + id + ".storyDay", result))
                    continue;
                if (!TryParseInt(Cell(row, "unlockDay"), out int unlockDay, "DiaryEntries/" + id + ".unlockDay", result))
                    continue;
                if (!TryParseEnum(Cell(row, "unlockPhase"), out DayPhase phase, "DiaryEntries/" + id + ".unlockPhase", result))
                    continue;
                if (phase != DayPhase.Day && phase != DayPhase.Night)
                {
                    Debug.LogError("unlockPhase 只能是 Day 或 Night @ " + id);
                    result.ErrorCount++;
                    continue;
                }

                if (!TryParseInt(Cell(row, "sortOrder"), out int sortOrder, "DiaryEntries/" + id + ".sortOrder", result))
                    continue;
                if (!TryParseFlag(Cell(row, "inBook"), out bool inBook, "DiaryEntries/" + id + ".inBook", result))
                    continue;

                string titleId = Cell(row, "titleId");
                if (!string.IsNullOrEmpty(titleId) && !HasString(strings, titleId))
                {
                    Debug.LogError("DiaryEntries 找不到 titleId：" + titleId + " @ " + id);
                    result.ErrorCount++;
                    continue;
                }

                entries.Add(new DiaryEntry
                {
                    id = id,
                    recipeId = recipeId,
                    storyDay = storyDay,
                    kind = kind,
                    titleId = titleId,
                    unlockDay = unlockDay,
                    unlockPhase = phase,
                    sortOrder = sortOrder,
                    inBook = inBook,
                    paragraphs = new List<DiaryParagraph>()
                });
            }

            return entries;
        }

        static void ReadParagraphs(DataTable table, List<DiaryEntry> entries, ImportResult result)
        {
            var orders = new Dictionary<string, HashSet<int>>();
            foreach (DataRow row in table.Rows)
            {
                string entryId = Cell(row, "entryId");
                if (string.IsNullOrWhiteSpace(entryId))
                    continue;

                entryId = entryId.Trim();
                DiaryEntry entry = FindEntry(entries, entryId);
                if (entry == null)
                {
                    Debug.LogError("DiaryParagraphs 找不到日记：" + entryId);
                    result.ErrorCount++;
                    continue;
                }

                if (!TryParseInt(Cell(row, "order"), out int order, "DiaryParagraphs/" + entryId + ".order", result))
                    continue;
                if (!TryParseEnum(Cell(row, "side"), out DiarySide side, "DiaryParagraphs/" + entryId + ".side", result))
                    continue;

                if (!orders.TryGetValue(entryId, out HashSet<int> used))
                {
                    used = new HashSet<int>();
                    orders[entryId] = used;
                }

                if (!used.Add(order))
                {
                    Debug.LogError("DiaryParagraphs order 重复：" + entryId + " " + order);
                    result.ErrorCount++;
                    continue;
                }

                entry.paragraphs.Add(new DiaryParagraph
                {
                    order = order,
                    side = side,
                    textZh = Cell(row, "textZh"),
                    textEn = Cell(row, "textEn")
                });
            }

            for (int i = 0; i < entries.Count; i++)
            {
                DiaryEntry entry = entries[i];
                if (entry.paragraphs == null || entry.paragraphs.Count == 0)
                {
                    Debug.LogError("日记没有段落：" + entry.id);
                    result.ErrorCount++;
                    continue;
                }

                entry.paragraphs.Sort((a, b) => a.order.CompareTo(b.order));
            }
        }

        static void WriteCatalog(List<DiaryEntry> entries, List<DiaryStringRow> strings, ImportResult result)
        {
            EnsureFolder("Assets/_Project/Data");
            EnsureFolder("Assets/_Project/Data/Diary");

            DiaryCatalog catalog = AssetDatabase.LoadAssetAtPath<DiaryCatalog>(CatalogAssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<DiaryCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
                result.Created++;
            }
            else
                result.Updated++;

            catalog.entries = entries;
            catalog.strings = strings;
            EditorUtility.SetDirty(catalog);
        }

        static bool HasString(List<DiaryStringRow> rows, string id)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].id == id)
                    return true;
            }

            return false;
        }

        static DiaryEntry FindEntry(List<DiaryEntry> entries, string id)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].id == id)
                    return entries[i];
            }

            return null;
        }

        static RecipeDefinition FindRecipe(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            string[] guids = AssetDatabase.FindAssets("t:RecipeDefinition", new[] { RecipesFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                RecipeDefinition recipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>(
                    AssetDatabase.GUIDToAssetPath(guids[i]));
                if (recipe != null && recipe.id == id.Trim())
                    return recipe;
            }

            return null;
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
            if (row == null || row.Table == null || !row.Table.Columns.Contains(column))
                return "";

            object value = row[column];
            if (value == null || value == DBNull.Value)
                return "";

            return Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? "";
        }

        static bool TryParseFlag(string text, out bool value, string context, ImportResult result)
        {
            value = false;
            if (text == "1" || string.Equals(text, "true", StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }

            if (text == "0" || string.Equals(text, "false", StringComparison.OrdinalIgnoreCase))
                return true;

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
            {
                if (number == 1d)
                {
                    value = true;
                    return true;
                }

                if (number == 0d)
                    return true;
            }

            Debug.LogError("inBook 只能是 0 或 1，实际 '" + text + "' @ " + context);
            result.ErrorCount++;
            return false;
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

        public sealed class ImportResult
        {
            public int Created;
            public int Updated;
            public int ErrorCount;
        }
    }
}
