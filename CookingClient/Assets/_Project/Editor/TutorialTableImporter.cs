using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using Cooking.Data;
using ExcelDataReader;
using UnityEditor;
using UnityEngine;

namespace Cooking.EditorTools
{
    public static class TutorialTableImporter
    {
        const string TableAssetPath = "Assets/_Project/Data/Tables/TutorialTables.xlsx";
        const string CatalogAssetPath = "Assets/_Project/Data/TutorialCatalog.asset";

        static readonly string[] PortraitFolders =
        {
            "Assets/_Project/Arts/player/emotion",
            "Assets/_Project/Arts/player/Walking"
        };

        [MenuItem("Cooking/Seed Tutorial Tables")]
        public static void SeedMenu()
        {
            SeedWorkbook();
            ImportResult result = Import();
            LogResult("seed/import", result);
        }

        [MenuItem("Cooking/Import Tutorial Tables")]
        public static void ImportMenu()
        {
            if (!File.Exists(ToAbsolutePath(TableAssetPath)))
                SeedWorkbook();

            LogResult("import", Import());
        }

        [MenuItem("Cooking/Validate Tutorial Tables")]
        public static void ValidateMenu()
        {
            TutorialCatalog catalog = AssetDatabase.LoadAssetAtPath<TutorialCatalog>(CatalogAssetPath);
            if (catalog == null)
            {
                Debug.LogError("缺少 TutorialCatalog。");
                return;
            }

            string[] required =
            {
                TutorialIds.Night0Intro,
                TutorialIds.Night0PostForage,
                TutorialIds.Day1Wakeup,
                TutorialIds.Day1AfterRecipe,
                TutorialIds.Day1AfterCut,
                TutorialIds.Day1Optimal,
                TutorialIds.Day1Plate,
                TutorialIds.Day1AfterEat
            };

            int errors = 0;
            for (int i = 0; i < required.Length; i++)
            {
                if (catalog.Find(required[i]) == null)
                {
                    Debug.LogError("缺少教程段 " + required[i]);
                    errors++;
                }
            }

            if (errors == 0)
                Debug.Log("Tutorial tables validate OK.");
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

            if (dataSet == null || !dataSet.Tables.Contains("TutorialLines"))
            {
                Debug.LogError("缺少 sheet：TutorialLines");
                result.ErrorCount++;
                return result;
            }

            var byId = new Dictionary<string, List<RowLine>>();
            int rowIndex = 1;
            foreach (DataRow row in dataSet.Tables["TutorialLines"].Rows)
            {
                rowIndex++;
                string segmentId = Cell(row, "segmentId");
                string textZh = Cell(row, "textZh");
                if (string.IsNullOrWhiteSpace(segmentId) && string.IsNullOrWhiteSpace(textZh))
                    continue;

                segmentId = segmentId.Trim();
                if (!TryParseInt(Cell(row, "order"), out int order, "TutorialLines 行 " + rowIndex, result))
                    continue;

                if (!byId.TryGetValue(segmentId, out List<RowLine> list))
                {
                    list = new List<RowLine>();
                    byId[segmentId] = list;
                }

                list.Add(new RowLine
                {
                    order = order,
                    textZh = textZh,
                    textEn = Cell(row, "textEn"),
                    expressionId = FirstCell(row, "expressionId", "profileExpression", "PlayerProfileImage")
                });
            }

            var segments = new List<TutorialSegment>();
            foreach (KeyValuePair<string, List<RowLine>> pair in byId)
            {
                pair.Value.Sort((a, b) => a.order.CompareTo(b.order));
                var segment = new TutorialSegment
                {
                    id = pair.Key,
                    lines = new List<TutorialLine>()
                };

                for (int i = 0; i < pair.Value.Count; i++)
                {
                    RowLine row = pair.Value[i];
                    Sprite expression = null;
                    if (!string.IsNullOrWhiteSpace(row.expressionId))
                        TryLoadPortrait(row.expressionId.Trim(), out expression, pair.Key, result);

                    segment.lines.Add(new TutorialLine
                    {
                        textZh = row.textZh ?? "",
                        textEn = row.textEn ?? "",
                        expressionId = row.expressionId ?? "",
                        expression = expression
                    });
                }

                segments.Add(segment);
            }

            WriteCatalog(segments, result);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return result;
        }

        static void WriteCatalog(List<TutorialSegment> segments, ImportResult result)
        {
            TutorialCatalog catalog = AssetDatabase.LoadAssetAtPath<TutorialCatalog>(CatalogAssetPath);
            bool created = false;
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<TutorialCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
                created = true;
            }

            catalog.segments = segments;
            EditorUtility.SetDirty(catalog);
            if (created)
                result.Created++;
            else
                result.Updated++;
        }

        static void SeedWorkbook()
        {
            EnsureFolder("Assets/_Project/Data/Tables");
            string absolute = ToAbsolutePath(TableAssetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ?? ".");

            var rows = new List<string[]>
            {
                // expressionId → PlayerProfileImage（Arts/player/emotion 或 Walking 下的表情名，空则沿用上一句）
                new[] { "segmentId", "order", "textZh", "textEn", "expressionId" }
            };

            Add(rows, TutorialIds.Night0Intro,
                ("这里是哪……我是谁？", "Where is this... Who am I?", "Main_Normal"),
                ("肚子好饿，有什么能吃的吗。", "I'm so hungry. Is there anything to eat?", "Main_Worry"),
                ("闻到一股熟悉的香味，好像是……蘑菇？过去看看吧，说不定真能找到点东西。",
                    "I smell a familiar aroma. It seems to be... mushrooms? Let me go take a look. Maybe I can really find something.", "Main_Smile"),
                ("真倒霉，又冷又饿，还很困。先回去睡会，起床再想想吃什么。",
                    "How unlucky. I'm cold, hungry, and sleepy too. I'll go back and sleep for a while first, then think about what to eat after I wake up.", "Main_Defeat"));

            Add(rows, TutorialIds.Night0PostForage,
                ("这里的夜晚实在是太冷了，幸运的是，我捡到了不少蘑菇。等明天醒了，或许能煲锅汤暖暖身子。",
                    "The nights here are really too cold. Fortunately, I picked up quite a few mushrooms. After I wake up tomorrow, maybe I can make a pot of soup to warm myself up.", "Main_Smile"),
                ("太晚了先回到帐篷睡觉吧。", "It's too late, let's head back to the tent and sleep.", "Main_Normal"));

            Add(rows, TutorialIds.Day1Wakeup,
                ("先看看食谱吧，我可不想因为吃到没煮熟的蘑菇去世。",
                    "Let me look at the recipe first. I don't want to die from eating undercooked mushrooms.", "Main_Worry"));

            Add(rows, TutorialIds.Day1AfterRecipe,
                ("蘑菇得先切碎。手里的刀意外顺手，好像我曾这样做过无数次。",
                    "The mushrooms need to be chopped first. The knife in my hand is unexpectedly handy, as if I have done this countless times before.", "Main_Normal"));

            Add(rows, TutorialIds.Day1AfterCut,
                ("接下来得想办法把它弄熟，直觉告诉我，用煎更快。",
                    "Next, I have to find a way to cook them. Intuition tells me frying is faster.", "Main_Confidence"),
                ("还差一点油，要把蘑菇本身的香味逼出来，汤才会好喝。",
                    "I still need a little oil to bring out the mushrooms' own aroma, so the soup will taste better.", "Main_Normal"),
                ("注意火候，差不多熟了就可以捞进炖锅里。",
                    "Pay attention to the heat. Once they are almost cooked, I can scoop them into the stew pot.", "Main_Pray"),
                ("记得调味，不然汤会变得寡淡的。",
                    "Remember to season it, otherwise the soup will become bland.", "Main_Normal"));

            Add(rows, TutorialIds.Day1Optimal,
                ("完成了，把它们捞出来备用。", "Done. Scoop them out and set them aside.", "Main_Happy"),
                ("该摆盘了。", "Time to plate it.", "Main_Smile"),
                ("盘子在哪？总不能让我空手吃吧。",
                    "Where is the plate? Can't expect me to eat with my bare hands, right?", "Main_Forced"));

            Add(rows, TutorialIds.Day1Plate,
                ("记得调味", "Remember to season it", "Main_Normal"),
                ("可以品鉴一下了", "I can give it a try now", "Main_Smile"));

            Add(rows, TutorialIds.Day1AfterEat,
                ("味道不错，看来我以前手艺挺好的嘛。只有蘑菇还是太单调了，希望后面能找到更多。",
                    "It tastes good. It seems my cooking skills used to be pretty good. Just mushrooms alone is still too monotonous. I hope I can find more later.", "Main_Happy"),
                ("可以多做几次熟悉一下", "I can do it a few more times to get familiar with it", "Main_Smile"));

            MinimalXlsx.Write(absolute, new Dictionary<string, List<string[]>>
            {
                { "TutorialLines", rows }
            });
            AssetDatabase.ImportAsset(TableAssetPath);
            Debug.Log("已写入 " + TableAssetPath);
        }

        static void Add(List<string[]> rows, string segmentId, params (string zh, string en, string expr)[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                rows.Add(new[]
                {
                    segmentId,
                    (i + 1).ToString(CultureInfo.InvariantCulture),
                    lines[i].zh,
                    lines[i].en,
                    lines[i].expr
                });
            }
        }

        static bool TryLoadPortrait(string name, out Sprite sprite, string context, ImportResult result)
        {
            sprite = null;
            int hits = 0;
            Sprite found = null;
            for (int i = 0; i < PortraitFolders.Length; i++)
            {
                Sprite loaded = AssetDatabase.LoadAssetAtPath<Sprite>(PortraitFolders[i] + "/" + name + ".png");
                if (loaded == null)
                    continue;
                hits++;
                found = loaded;
            }

            if (hits != 1)
            {
                Debug.LogError("表情 " + name + " 找不到或重名 @ " + context);
                result.ErrorCount++;
                return false;
            }

            sprite = found;
            return true;
        }

        static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder))
                return;

            string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
            string name = Path.GetFileName(assetFolder);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
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

        static string FirstCell(DataRow row, params string[] columns)
        {
            for (int i = 0; i < columns.Length; i++)
            {
                string value = Cell(row, columns[i]);
                if (!string.IsNullOrEmpty(value))
                    return value;
            }

            return "";
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

            Debug.LogError("无法解析整数 @ " + context);
            result.ErrorCount++;
            value = 0;
            return false;
        }

        static string ToAbsolutePath(string assetPath)
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.GetFullPath(Path.Combine(root, assetPath));
        }

        static void LogResult(string label, ImportResult result)
        {
            if (result.ErrorCount > 0)
                Debug.LogError("Tutorial " + label + " errors=" + result.ErrorCount);
            else
                Debug.Log("Tutorial " + label + " OK. created=" + result.Created + " updated=" + result.Updated);
        }

        sealed class RowLine
        {
            public int order;
            public string textZh;
            public string textEn;
            public string expressionId;
        }

        public sealed class ImportResult
        {
            public int Created;
            public int Updated;
            public int ErrorCount;
        }

        static class MinimalXlsx
        {
            public static void Write(string absolutePath, Dictionary<string, List<string[]>> sheets)
            {
                if (File.Exists(absolutePath))
                    File.Delete(absolutePath);

                using (FileStream file = File.Create(absolutePath))
                using (var zip = new ZipArchive(file, ZipArchiveMode.Create))
                {
                    var shared = new List<string>();
                    var names = new List<string>();
                    int index = 0;
                    foreach (KeyValuePair<string, List<string[]>> pair in sheets)
                    {
                        index++;
                        names.Add(pair.Key);
                        WriteEntry(zip, "xl/worksheets/sheet" + index + ".xml", BuildSheet(pair.Value, shared));
                    }

                    WriteEntry(zip, "[Content_Types].xml", BuildContentTypes(names.Count));
                    WriteEntry(zip, "_rels/.rels",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"
                        + "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
                        + "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>"
                        + "</Relationships>");
                    WriteEntry(zip, "xl/workbook.xml", BuildWorkbook(names));
                    WriteEntry(zip, "xl/_rels/workbook.xml.rels", BuildWorkbookRels(names.Count));
                    WriteEntry(zip, "xl/sharedStrings.xml", BuildSharedStrings(shared));
                    WriteEntry(zip, "xl/styles.xml",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"
                        + "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">"
                        + "<fonts count=\"1\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>"
                        + "<fills count=\"1\"><fill><patternFill patternType=\"none\"/></fill></fills>"
                        + "<borders count=\"1\"><border/></borders>"
                        + "<cellStyleXfs count=\"1\"><xf/></cellStyleXfs>"
                        + "<cellXfs count=\"1\"><xf/></cellXfs></styleSheet>");
                }
            }

            static string BuildSheet(List<string[]> rows, List<string> shared)
            {
                var sb = new StringBuilder();
                sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
                for (int r = 0; r < rows.Count; r++)
                {
                    sb.Append("<row r=\"").Append(r + 1).Append("\">");
                    for (int c = 0; c < rows[r].Length; c++)
                    {
                        int sharedIndex = shared.Count;
                        shared.Add(rows[r][c] ?? "");
                        sb.Append("<c r=\"").Append(ColumnName(c)).Append(r + 1)
                            .Append("\" t=\"s\"><v>").Append(sharedIndex).Append("</v></c>");
                    }

                    sb.Append("</row>");
                }

                sb.Append("</sheetData></worksheet>");
                return sb.ToString();
            }

            static string BuildSharedStrings(List<string> shared)
            {
                var sb = new StringBuilder();
                sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                sb.Append("<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" count=\"")
                    .Append(shared.Count).Append("\" uniqueCount=\"").Append(shared.Count).Append("\">");
                for (int i = 0; i < shared.Count; i++)
                    sb.Append("<si><t>").Append(System.Security.SecurityElement.Escape(shared[i] ?? "")).Append("</t></si>");
                sb.Append("</sst>");
                return sb.ToString();
            }

            static string BuildWorkbook(List<string> sheetNames)
            {
                var sb = new StringBuilder();
                sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                sb.Append("<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" ");
                sb.Append("xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>");
                for (int i = 0; i < sheetNames.Count; i++)
                {
                    sb.Append("<sheet name=\"").Append(System.Security.SecurityElement.Escape(sheetNames[i]))
                        .Append("\" sheetId=\"").Append(i + 1).Append("\" r:id=\"rId").Append(i + 1).Append("\"/>");
                }

                sb.Append("</sheets></workbook>");
                return sb.ToString();
            }

            static string BuildWorkbookRels(int count)
            {
                var sb = new StringBuilder();
                sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                sb.Append("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
                for (int i = 0; i < count; i++)
                {
                    sb.Append("<Relationship Id=\"rId").Append(i + 1)
                        .Append("\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet")
                        .Append(i + 1).Append(".xml\"/>");
                }

                sb.Append("<Relationship Id=\"rId").Append(count + 1)
                    .Append("\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings\" Target=\"sharedStrings.xml\"/>");
                sb.Append("<Relationship Id=\"rId").Append(count + 2)
                    .Append("\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>");
                sb.Append("</Relationships>");
                return sb.ToString();
            }

            static string BuildContentTypes(int sheetCount)
            {
                var sb = new StringBuilder();
                sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                sb.Append("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
                sb.Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
                sb.Append("<Default Extension=\"xml\" ContentType=\"application/xml\"/>");
                sb.Append("<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
                for (int i = 0; i < sheetCount; i++)
                {
                    sb.Append("<Override PartName=\"/xl/worksheets/sheet").Append(i + 1)
                        .Append(".xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
                }

                sb.Append("<Override PartName=\"/xl/sharedStrings.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml\"/>");
                sb.Append("<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
                sb.Append("</Types>");
                return sb.ToString();
            }

            static void WriteEntry(ZipArchive zip, string path, string content)
            {
                ZipArchiveEntry entry = zip.CreateEntry(path, System.IO.Compression.CompressionLevel.Optimal);
                using (Stream stream = entry.Open())
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                    writer.Write(content);
            }

            static string ColumnName(int index)
            {
                int n = index + 1;
                var sb = new StringBuilder();
                while (n > 0)
                {
                    n--;
                    sb.Insert(0, (char)('A' + n % 26));
                    n /= 26;
                }

                return sb.ToString();
            }
        }
    }
}
