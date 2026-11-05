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
    public static class UiSpriteTableImporter
    {
        public const string TableAssetPath = "Assets/_Project/Data/Tables/UiSpriteTables.xlsx";
        public const string CatalogAssetPath = "Assets/_Project/Data/Ui/UiSpriteCatalog.asset";

        [MenuItem("Cooking/Import Ui Sprite Tables")]
        public static void ImportMenu()
        {
            ImportResult result = Import();
            if (result.ErrorCount > 0)
                Debug.LogError(
                    "Ui sprite tables import finished with errors. Created "
                    + result.Created + ", updated " + result.Updated + ", errors "
                    + result.ErrorCount + ".");
            else
                Debug.Log(
                    "Ui sprite tables import OK. Created " + result.Created
                    + ", updated " + result.Updated + ", rows " + result.RowCount + ".");
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

            DataTable sprites = RequireSheet(dataSet, "UiSprites", result);
            RequireSheet(dataSet, "UiSpriteTargets", result);
            if (result.ErrorCount > 0)
                return result;

            List<UiSpriteRow> rows = ReadSprites(sprites, result);
            if (result.ErrorCount > 0)
                return result;

            WriteCatalog(rows, result);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            result.RowCount = rows.Count;
            return result;
        }

        public static List<UiSpriteTargetRow> ReadTargets(ImportResult result)
        {
            var targets = new List<UiSpriteTargetRow>();
            string absolute = ToAbsolutePath(TableAssetPath);
            if (!File.Exists(absolute))
            {
                Debug.LogError("缺少表文件：" + TableAssetPath);
                result.ErrorCount++;
                return targets;
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
                return targets;
            }

            DataTable table = RequireSheet(dataSet, "UiSpriteTargets", result);
            if (table == null)
                return targets;

            for (int i = 0; i < table.Rows.Count; i++)
            {
                DataRow row = table.Rows[i];
                string id = Cell(row, "id");
                string scene = Cell(row, "scene");
                string objectPath = Cell(row, "objectPath");
                if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(scene) && string.IsNullOrWhiteSpace(objectPath))
                    continue;

                id = id.Trim();
                scene = scene.Trim();
                objectPath = objectPath.Trim();
                string context = "UiSpriteTargets 行 " + (i + 2);

                if (string.IsNullOrEmpty(id))
                {
                    Fail(result, "缺少 id @ " + context);
                    continue;
                }

                if (scene != "MeunScene" && scene != "outingScene" && scene != "MainCooking")
                {
                    Fail(result, "scene 只能是 MeunScene、outingScene、MainCooking @ " + context);
                    continue;
                }

                if (string.IsNullOrEmpty(objectPath))
                {
                    Fail(result, "缺少 objectPath @ " + context);
                    continue;
                }

                targets.Add(new UiSpriteTargetRow
                {
                    id = id,
                    scene = scene,
                    objectPath = objectPath
                });
            }

            return targets;
        }

        static List<UiSpriteRow> ReadSprites(DataTable table, ImportResult result)
        {
            var rows = new List<UiSpriteRow>();
            var seen = new HashSet<string>();
            for (int i = 0; i < table.Rows.Count; i++)
            {
                DataRow row = table.Rows[i];
                string id = Cell(row, "id");
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                id = id.Trim();
                string context = "UiSprites 行 " + (i + 2) + " (" + id + ")";
                if (!seen.Add(id))
                {
                    Fail(result, "id 重复：" + id + " @ " + context);
                    continue;
                }

                string zhPath = Cell(row, "spriteZh");
                if (string.IsNullOrWhiteSpace(zhPath))
                {
                    Fail(result, "缺少 spriteZh @ " + context);
                    continue;
                }

                Sprite zh = LoadSprite(zhPath, context, result);
                if (zh == null)
                    continue;

                Sprite en = null;
                string enPath = Cell(row, "spriteEn");
                if (!string.IsNullOrWhiteSpace(enPath))
                    en = LoadSprite(enPath, context, result);

                rows.Add(new UiSpriteRow
                {
                    id = id,
                    spriteZh = zh,
                    spriteEn = en
                });
            }

            return rows;
        }

        static void WriteCatalog(List<UiSpriteRow> rows, ImportResult result)
        {
            EnsureFolder("Assets/_Project/Data/Ui");
            UiSpriteCatalog catalog = AssetDatabase.LoadAssetAtPath<UiSpriteCatalog>(CatalogAssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<UiSpriteCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
                result.Created++;
            }
            else
                result.Updated++;

            catalog.rows = rows.ToArray();
            EditorUtility.SetDirty(catalog);
        }

        static Sprite LoadSprite(string path, string context, ImportResult result)
        {
            path = path.Trim();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Fail(result, "找不到 Sprite：" + path + " @ " + context);
                return null;
            }

            return sprite;
        }

        static DataTable RequireSheet(DataSet dataSet, string name, ImportResult result)
        {
            DataTable table = dataSet.Tables[name];
            if (table == null)
            {
                Fail(result, "Excel 缺少 Sheet：" + name);
                return null;
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

        static void Fail(ImportResult result, string message)
        {
            Debug.LogError(message);
            result.ErrorCount++;
        }

        public sealed class ImportResult
        {
            public int Created;
            public int Updated;
            public int ErrorCount;
            public int RowCount;
        }

        public sealed class UiSpriteTargetRow
        {
            public string id;
            public string scene;
            public string objectPath;
        }
    }
}
