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
    public static class EndingTableImporter
    {
        const string TableAssetPath = "Assets/_Project/Data/Tables/EndingTables.xlsx";
        const string CatalogAssetPath = "Assets/_Project/Data/EndingCatalog.asset";
        const string EmotionFolder = "Assets/_Project/Arts/player/emotion";
        const string WalkingFolder = "Assets/_Project/Arts/player/Walking";

        static readonly string[] PortraitFolders =
        {
            EmotionFolder,
            WalkingFolder
        };

        [MenuItem("Cooking/Seed Ending Tables")]
        public static void SeedMenu()
        {
            SeedWorkbook();
            ImportResult result = Import();
            if (result.ErrorCount > 0)
                Debug.LogError(
                    "Ending tables seed/import finished with errors. Created "
                    + result.Created + ", updated " + result.Updated + ", errors "
                    + result.ErrorCount + ".");
            else
                Debug.Log(
                    "Ending tables seed/import OK. Created " + result.Created
                    + ", updated " + result.Updated + ".");
        }

        [MenuItem("Cooking/Import Ending Tables")]
        public static void ImportMenu()
        {
            if (!File.Exists(ToAbsolutePath(TableAssetPath)))
            {
                Debug.LogWarning("缺少 EndingTables.xlsx，先执行 Seed。");
                SeedWorkbook();
            }

            ImportResult result = Import();
            if (result.ErrorCount > 0)
                Debug.LogError(
                    "Ending tables import finished with errors. Created "
                    + result.Created + ", updated " + result.Updated + ", errors "
                    + result.ErrorCount + ".");
            else
                Debug.Log(
                    "Ending tables import OK. Created " + result.Created
                    + ", updated " + result.Updated + ".");
        }

        [MenuItem("Cooking/Validate Ending Tables")]
        public static void ValidateMenu()
        {
            EndingCatalog catalog = AssetDatabase.LoadAssetAtPath<EndingCatalog>(CatalogAssetPath);
            if (catalog == null)
            {
                Debug.LogError("缺少 EndingCatalog：" + CatalogAssetPath);
                return;
            }

            int errors = 0;
            if (catalog.commonLines == null || catalog.commonLines.Count == 0)
            {
                Debug.LogError("共通台词为空。");
                errors++;
            }

            if (catalog.Find("ending_good") == null)
            {
                Debug.LogError("缺少 ending_good。");
                errors++;
            }

            if (catalog.Find("ending_normal") == null)
            {
                Debug.LogError("缺少 ending_normal。");
                errors++;
            }

            if (errors == 0)
                Debug.Log("Ending tables validate OK.");
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

            DataTable commonTable = RequireSheet(dataSet, "CommonLines", result);
            DataTable endingsTable = RequireSheet(dataSet, "Endings", result);
            DataTable linesTable = RequireSheet(dataSet, "EndingLines", result);
            if (result.ErrorCount > 0)
                return result;

            List<EndingLine> common = ReadLines(commonTable, "CommonLines", result, requireText: true);
            List<EndingDefinition> endings = ReadEndings(endingsTable, result);
            AttachBranchLines(linesTable, endings, result);
            if (result.ErrorCount > 0)
                return result;

            WriteCatalog(common, endings, result);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return result;
        }

        static List<EndingDefinition> ReadEndings(DataTable table, ImportResult result)
        {
            var endings = new List<EndingDefinition>();
            var seen = new HashSet<string>();
            int rowIndex = 1;
            foreach (DataRow row in table.Rows)
            {
                rowIndex++;
                string id = Cell(row, "id");
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                id = id.Trim();
                if (!seen.Add(id))
                {
                    Fail(result, "Endings id 重复：" + id + " 行 " + rowIndex);
                    continue;
                }

                if (!TryParseInt(Cell(row, "priority"), out int priority, "Endings/" + id + ".priority", result))
                    continue;

                if (!TryParseFlag(Cell(row, "requireAllInBookDiaries"), out bool requireDiaries, "Endings/" + id, result))
                    continue;
                if (!TryParseFlag(Cell(row, "forbidAllInBookDiaries"), out bool forbidDiaries, "Endings/" + id, result))
                    continue;
                if (!TryParseFlag(Cell(row, "requireAllNpcProps"), out bool requireProps, "Endings/" + id, result))
                    continue;
                if (!TryParseFlag(Cell(row, "forbidAllNpcProps"), out bool forbidProps, "Endings/" + id, result))
                    continue;

                string cgPath = Cell(row, "cg");
                Sprite cg = null;
                if (!string.IsNullOrWhiteSpace(cgPath))
                {
                    cg = AssetDatabase.LoadAssetAtPath<Sprite>(cgPath.Trim());
                    if (cg == null)
                        Fail(result, "找不到 CG Sprite " + cgPath + " @ " + id);
                }

                Sprite endingImageZh = LoadOptionalSprite(
                    Cell(row, "endingImageZh"),
                    "endingImageZh",
                    id,
                    result);
                Sprite endingImageEn = LoadOptionalSprite(
                    Cell(row, "endingImageEn"),
                    "endingImageEn",
                    id,
                    result);

                endings.Add(new EndingDefinition
                {
                    id = id,
                    priority = priority,
                    requireAllInBookDiaries = requireDiaries,
                    forbidAllInBookDiaries = forbidDiaries,
                    requireAllNpcProps = requireProps,
                    forbidAllNpcProps = forbidProps,
                    cg = cg,
                    endingImageZh = endingImageZh,
                    endingImageEn = endingImageEn,
                    panelTextZh = Cell(row, "panelTextZh"),
                    panelTextEn = Cell(row, "panelTextEn"),
                    clickTextZh = Cell(row, "clickTextZh"),
                    clickTextEn = Cell(row, "clickTextEn"),
                    creditTextZh = Cell(row, "creditTextZh"),
                    creditTextEn = Cell(row, "creditTextEn"),
                    lines = new List<EndingLine>()
                });
            }

            return endings;
        }

        static void AttachBranchLines(DataTable table, List<EndingDefinition> endings, ImportResult result)
        {
            var byId = new Dictionary<string, EndingDefinition>();
            for (int i = 0; i < endings.Count; i++)
                byId[endings[i].id] = endings[i];

            var buckets = new Dictionary<string, List<RowLine>>();
            int rowIndex = 1;
            foreach (DataRow row in table.Rows)
            {
                rowIndex++;
                string endingId = Cell(row, "endingId");
                string textZh = Cell(row, "textZh");
                if (string.IsNullOrWhiteSpace(endingId) && string.IsNullOrWhiteSpace(textZh))
                    continue;

                endingId = endingId.Trim();
                if (!byId.ContainsKey(endingId))
                {
                    Fail(result, "EndingLines 找不到结局 " + endingId + " 行 " + rowIndex);
                    continue;
                }

                if (!TryParseInt(Cell(row, "order"), out int order, "EndingLines/" + endingId + " 行 " + rowIndex, result))
                    continue;

                if (!buckets.TryGetValue(endingId, out List<RowLine> list))
                {
                    list = new List<RowLine>();
                    buckets[endingId] = list;
                }

                list.Add(new RowLine
                {
                    order = order,
                    speaker = Cell(row, "speaker"),
                    textZh = textZh,
                    textEn = Cell(row, "textEn"),
                    expressionId = Cell(row, "expressionId")
                });
            }

            foreach (KeyValuePair<string, List<RowLine>> pair in buckets)
            {
                pair.Value.Sort((a, b) => a.order.CompareTo(b.order));
                EndingDefinition ending = byId[pair.Key];
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    RowLine row = pair.Value[i];
                    ending.lines.Add(MakeLine(row.speaker, row.textZh, row.textEn, row.expressionId, result, ending.id));
                }
            }
        }

        static List<EndingLine> ReadLines(DataTable table, string sheet, ImportResult result, bool requireText)
        {
            var rows = new List<RowLine>();
            int rowIndex = 1;
            foreach (DataRow row in table.Rows)
            {
                rowIndex++;
                string textZh = Cell(row, "textZh");
                string orderText = Cell(row, "order");
                if (string.IsNullOrWhiteSpace(textZh) && string.IsNullOrWhiteSpace(orderText))
                    continue;

                if (!TryParseInt(orderText, out int order, sheet + " 行 " + rowIndex, result))
                    continue;

                if (requireText && string.IsNullOrEmpty(textZh))
                {
                    Fail(result, sheet + " 缺少 textZh 行 " + rowIndex);
                    continue;
                }

                rows.Add(new RowLine
                {
                    order = order,
                    speaker = Cell(row, "speaker"),
                    textZh = textZh,
                    textEn = Cell(row, "textEn"),
                    expressionId = Cell(row, "expressionId")
                });
            }

            rows.Sort((a, b) => a.order.CompareTo(b.order));
            var lines = new List<EndingLine>();
            for (int i = 0; i < rows.Count; i++)
            {
                RowLine row = rows[i];
                lines.Add(MakeLine(row.speaker, row.textZh, row.textEn, row.expressionId, result, sheet));
            }

            return lines;
        }

        static EndingLine MakeLine(
            string speaker,
            string textZh,
            string textEn,
            string expressionId,
            ImportResult result,
            string context)
        {
            Sprite expression = null;
            if (!string.IsNullOrWhiteSpace(expressionId))
            {
                if (!TryLoadPortrait(expressionId.Trim(), out expression, context + "/" + expressionId, result))
                    expression = null;
            }

            return new EndingLine
            {
                speaker = string.IsNullOrWhiteSpace(speaker) ? "主角" : speaker.Trim(),
                textZh = textZh ?? "",
                textEn = textEn ?? "",
                expressionId = expressionId ?? "",
                expression = expression
            };
        }

        static void WriteCatalog(List<EndingLine> common, List<EndingDefinition> endings, ImportResult result)
        {
            EndingCatalog catalog = AssetDatabase.LoadAssetAtPath<EndingCatalog>(CatalogAssetPath);
            bool created = false;
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<EndingCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
                created = true;
            }

            catalog.commonLines = common;
            catalog.endings = endings;
            EditorUtility.SetDirty(catalog);

            if (created)
                result.Created++;
            else
                result.Updated++;
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

            if (hits == 0)
            {
                Fail(result, "找不到表情 " + name + " @ " + context);
                return false;
            }

            if (hits > 1)
            {
                Fail(result, "表情重名 " + name + " @ " + context);
                return false;
            }

            sprite = found;
            return true;
        }

        static DataTable RequireSheet(DataSet dataSet, string name, ImportResult result)
        {
            if (dataSet == null || !dataSet.Tables.Contains(name))
            {
                Fail(result, "缺少 sheet：" + name);
                return null;
            }

            return dataSet.Tables[name];
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
            if (string.IsNullOrWhiteSpace(text))
                return true;

            if (text == "1" || string.Equals(text, "true", StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }

            if (text == "0" || string.Equals(text, "false", StringComparison.OrdinalIgnoreCase))
                return true;

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
            {
                value = Math.Abs(number - 1d) < 0.0001d;
                return true;
            }

            Fail(result, "布尔只能是 0/1 @ " + context + " 实际 '" + text + "'");
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

            Fail(result, "无法解析整数='" + text + "' @ " + context);
            value = 0;
            return false;
        }

        static Sprite LoadOptionalSprite(string path, string field, string endingId, ImportResult result)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            path = path.Trim();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                if (assets != null)
                {
                    for (int i = 0; i < assets.Length; i++)
                    {
                        if (assets[i] is Sprite found)
                        {
                            sprite = found;
                            break;
                        }
                    }
                }
            }

            if (sprite == null)
                Fail(result, "找不到 EndingPanel 图 " + field + " " + path + " @ " + endingId);

            return sprite;
        }

        static void Fail(ImportResult result, string message)
        {
            Debug.LogError(message);
            result.ErrorCount++;
        }

        static string ToAbsolutePath(string assetPath)
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.GetFullPath(Path.Combine(root, assetPath));
        }

        sealed class RowLine
        {
            public int order;
            public string speaker;
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

        #region Seed workbook

        static void SeedWorkbook()
        {
            EnsureFolder("Assets/_Project/Data/Tables");
            string absolute = ToAbsolutePath(TableAssetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ?? ".");

            var common = new List<string[]>
            {
                new[] { "order", "speaker", "textZh", "textEn", "expressionId" },
                Line(1, "这就是末日后的世界吗？总感觉有些不太一样。", "Main_Normal"),
                Line(2, "为什么这些东西怎么也在这里？", ""),
                Line(3, "说起来，我一直没弄清楚它们究竟有什么作用。", ""),
                Line(4, "如今终于不用担心明天吃什么，让我好好瞧瞧……", ""),
                Line(5, "【Main_shocked】、策划案、Bug反馈表、剧情脚本、还有音效合集？", "Main_Shocked"),
                Line(6, "等等！为什么连我每天什么时候出门、遇到了哪些事都有？", "Main_Pray"),
                Line(7, "……", "Main_Pray"),
                Line(8, "原来如此。", ""),
                Line(9, "所谓的世界末日根本不是什么天灾……", ""),
                Line(10, "只是你们这群家伙的截止日到了啊！！！", "Main_Worry")
            };

            var endings = new List<string[]>
            {
                new[]
                {
                    "id", "priority",
                    "requireAllInBookDiaries", "forbidAllInBookDiaries",
                    "requireAllNpcProps", "forbidAllNpcProps",
                    "cg",
                    "endingImageZh", "endingImageEn",
                    "panelTextZh", "panelTextEn",
                    "clickTextZh", "clickTextEn",
                    "creditTextZh", "creditTextEn"
                },
                // 日记决定普通/好结局；NPC 全道具只影响流程。EndingPanel 图按语言各绑一张（共 4 张）。
                new[]
                {
                    "ending_good", "100", "1", "0", "0", "0",
                    "Assets/_Project/Arts/Ending/Ending_Good.PNG",
                    "Assets/_Project/Arts/Ending/chGOOD Ending.png",
                    "Assets/_Project/Arts/Ending/engGOOD Ending.png",
                    "（好结局占位文案）", "",
                    "点击任意处继续", "",
                    "特别演出\n平！ 饰演 章鱼猫\n图图 饰演 鼻嘎图\n隔儿DIGE 饰演 社积食\n沉舟 饰演 吟游诗人\n59 饰演 小说家\n\n游戏策划：林子大了什么后厨都有\n\n主策划：平!\n主程序：图图\n游戏美术：隔儿DIGE、59\n\nUI设计：隔儿DIGE\n人物设计：59\n文案策划：59\n翻译：平！\n\n音乐设计：沉舟",
                    "Special Thanks\nPing! as Octopus Cat\nTutu as Lil Tu\n隔儿DIGE as Sheji Food\nChenzhou as Bard\n59 as Novelist\n\nGame Design: When the forest is big, every kitchen has something\n\nLead Design: Ping!\nLead Programming: Tutu\nArt: 隔儿DIGE, 59\n\nUI: 隔儿DIGE\nCharacter Design: 59\nWriting: 59\nTranslation: Ping!\n\nMusic: Chenzhou"
                },
                new[]
                {
                    "ending_normal", "50", "0", "1", "0", "0",
                    "Assets/_Project/Arts/Ending/Ending_Normal.JPG",
                    "Assets/_Project/Arts/Ending/chNoraml Ending.png",
                    "Assets/_Project/Arts/Ending/engNoraml Ending.png",
                    "（普通结局占位文案）", "",
                    "点击任意处继续", "",
                    "特别演出\n平！ 饰演 章鱼猫\n图图 饰演 鼻嘎图\n隔儿DIGE 饰演 社积食\n沉舟 饰演 吟游诗人\n59 饰演 小说家\n\n游戏策划：林子大了什么后厨都有\n\n主策划：平!\n主程序：图图\n游戏美术：隔儿DIGE、59\n\nUI设计：隔儿DIGE\n人物设计：59\n文案策划：59\n翻译：平！\n\n音乐设计：沉舟",
                    "Special Thanks\nPing! as Octopus Cat\nTutu as Lil Tu\n隔儿DIGE as Sheji Food\nChenzhou as Bard\n59 as Novelist\n\nGame Design: When the forest is big, every kitchen has something\n\nLead Design: Ping!\nLead Programming: Tutu\nArt: 隔儿DIGE, 59\n\nUI: 隔儿DIGE\nCharacter Design: 59\nWriting: 59\nTranslation: Ping!\n\nMusic: Chenzhou"
                }
            };

            var branch = new List<string[]>
            {
                new[] { "endingId", "order", "speaker", "textZh", "textEn", "expressionId" }
            };

            AddBranch(branch, "ending_normal",
                ("这么说来，我这几天的一举一动都被你看了个一清二楚？", "Main_Worry"),
                ("真是见笑，我本来还以为能把那些见不得人的过往一起带进坟墓里呢。", "Main_Forced"),
                ("虽然按照“剧本”的说法，我早已经死过一次了。", ""),
                ("这算什么？截止日期到了来不及开发了吗？", "Main_Defeat"),
                ("至于那些没能得到解答的问题，就让它们随风去吧。", "Main_Smile"),
                ("毕竟要是再让他们写下去，恐怕就要#￥@￥@%——", ""),
                ("【你以为这就是结束了吗？】", "Main_Shadow"),
                ("【呵呵，可不要小瞧DDL的力量啊！】", ""),
                ("【想办法集齐全部日记，解锁完整剧情吧！】", ""),
                ("噗哈……！刚刚那是什么！？我怎么不能说话了？", "Main_Shocked"),
                ("……", "Main_Normal"),
                ("啊，你要离开了吗？", ""),
                ("谢谢你陪我度过这七天，不知道我的故事是否合你胃口呢？", "Main_Smile"),
                ("再见，让我们在完美的时间线再会吧！", "Main_Happy"));

            AddBranch(branch, "ending_good",
                ("真是荒唐。", "Main_Worry"),
                ("没想到，根本不需要等上百年、千年、万年。", "Main_Normal"),
                ("就在这个世界之外，便已经有人见证了这段微不足道的人生。", ""),
                ("事到如今，回去修改日记也毫无意义。", ""),
                ("毕竟我只是一个被你们创造出来的倒霉厨师。即便如此，我依旧热爱着我的事业。", "Main_Confidence"),
                ("啊——真可惜，一点多余的时间都不肯给我。", "Main_Worry"),
                ("我想，你应该也看见了吧？", "Main_Smile"),
                ("即使世界的一切都早已被写下，至少对我而言，那些经历并非虚假。", ""),
                ("如果你有机会见到那些创造我的人，请替我告诉那些家伙——", ""),
                ("虽然你们只给了我短短七天，但也算过了一段快乐的日子。", "Main_Happy"),
                ("还有你。", ""),
                ("谢谢你陪我走完这段旅程。", ""),
                ("要是还有下次……", ""),
                ("至少给我准备一台烤箱啊！", "Main_Worry"));

            var sheets = new Dictionary<string, List<string[]>>
            {
                { "CommonLines", common },
                { "Endings", endings },
                { "EndingLines", branch }
            };

            MinimalXlsx.Write(absolute, sheets);
            AssetDatabase.ImportAsset(TableAssetPath);
            Debug.Log("已写入 " + TableAssetPath);
        }

        static string[] Line(int order, string zh, string expression)
        {
            return new[] { order.ToString(CultureInfo.InvariantCulture), "主角", zh, "", expression };
        }

        static void AddBranch(List<string[]> rows, string endingId, params (string zh, string expression)[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                rows.Add(new[]
                {
                    endingId,
                    (i + 1).ToString(CultureInfo.InvariantCulture),
                    "主角",
                    lines[i].zh,
                    "",
                    lines[i].expression
                });
            }
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
                    var sheetIds = new List<string>();
                    int index = 0;
                    foreach (KeyValuePair<string, List<string[]>> pair in sheets)
                    {
                        index++;
                        string sheetFile = "sheet" + index + ".xml";
                        sheetIds.Add(pair.Key);
                        WriteEntry(zip, "xl/worksheets/" + sheetFile, BuildSheet(pair.Value, shared));
                    }

                    WriteEntry(zip, "[Content_Types].xml", BuildContentTypes(sheetIds.Count));
                    WriteEntry(zip, "_rels/.rels",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"
                        + "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
                        + "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>"
                        + "</Relationships>");
                    WriteEntry(zip, "xl/workbook.xml", BuildWorkbook(sheetIds));
                    WriteEntry(zip, "xl/_rels/workbook.xml.rels", BuildWorkbookRels(sheetIds.Count));
                    WriteEntry(zip, "xl/sharedStrings.xml", BuildSharedStrings(shared));
                    WriteEntry(zip, "xl/styles.xml",
                        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"
                        + "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">"
                        + "<fonts count=\"1\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>"
                        + "<fills count=\"1\"><fill><patternFill patternType=\"none\"/></fill></fills>"
                        + "<borders count=\"1\"><border/></borders>"
                        + "<cellStyleXfs count=\"1\"><xf/></cellStyleXfs>"
                        + "<cellXfs count=\"1\"><xf/></cellXfs>"
                        + "</styleSheet>");
                }
            }

            static string BuildSheet(List<string[]> rows, List<string> shared)
            {
                var sb = new StringBuilder();
                sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
                for (int r = 0; r < rows.Count; r++)
                {
                    string[] row = rows[r];
                    sb.Append("<row r=\"").Append(r + 1).Append("\">");
                    for (int c = 0; c < row.Length; c++)
                    {
                        string value = row[c] ?? "";
                        int sharedIndex = shared.Count;
                        shared.Add(value);
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
                {
                    sb.Append("<si><t>")
                        .Append(System.Security.SecurityElement.Escape(shared[i] ?? ""))
                        .Append("</t></si>");
                }

                sb.Append("</sst>");
                return sb.ToString();
            }

            static string BuildWorkbook(List<string> sheetNames)
            {
                var sb = new StringBuilder();
                sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
                sb.Append("<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" ");
                sb.Append("xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
                sb.Append("<sheets>");
                for (int i = 0; i < sheetNames.Count; i++)
                {
                    sb.Append("<sheet name=\"").Append(System.Security.SecurityElement.Escape(sheetNames[i]))
                        .Append("\" sheetId=\"").Append(i + 1)
                        .Append("\" r:id=\"rId").Append(i + 1).Append("\"/>");
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

        #endregion
    }
}
