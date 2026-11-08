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
    public static class NpcTableImporter
    {
        const string TableAssetPath = "Assets/_Project/Data/Tables/NpcTables.xlsx";
        const string CatalogAssetPath = "Assets/_Project/Data/NpcCatalog.asset";
        const string UiTextCatalogPath = "Assets/_Project/Data/UiTextCatalog.asset";
        const string IngredientsFolder = "Assets/_Project/Data/Ingredients";

        static readonly string[] SpeakerPrefixes =
        {
            "小说家：", "小说家:", "Novelist:", "Novelist：",
            "吟游诗人：", "吟游诗人:", "Bard:", "Bard：",
            "猫章鱼：", "猫章鱼:", "Ocatopus:", "Ocatopus：",
            "鼻嘎图：", "鼻嘎图:", "Lil Tu:", "Lil Tu：",
            "社积食：", "社积食:", "Dyspegniter:", "Dyspegniter：", "Designer:", "Designer："
        };

        [MenuItem("Cooking/Import Npc Tables")]
        public static void ImportMenu()
        {
            ImportResult result = Import();
            string check = ValidateImported();
            if (!string.IsNullOrEmpty(check))
            {
                Debug.LogError(check);
                result.ErrorCount++;
            }

            if (result.ErrorCount > 0)
                Debug.LogError("Npc tables import finished with errors: " + result.ErrorCount + ".");
            else
                Debug.Log("Npc tables import OK.");
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

            EnsureFolder("Assets/_Project/Data");
            EnsureFolder("Assets/_Project/Data/Tables");
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

            NpcCatalog catalog = FindOrCreateCatalog();
            Dictionary<string, NpcCharacter> characters = ImportCharacters(
                RequireSheet(dataSet, "NpcCharacters", result),
                result);
            Dictionary<string, NpcProp> props = ImportProps(
                RequireSheet(dataSet, "NpcProps", result),
                result);
            ImportOptions(RequireSheet(dataSet, "NpcOptions", result), characters, result);
            ImportRewards(RequireSheet(dataSet, "NpcRewards", result), characters, props, result);
            ImportLines(RequireSheet(dataSet, "NpcLines", result), characters, result);
            ImportUiStrings(RequireSheet(dataSet, "UiStrings", result), result);

            catalog.characters = new List<NpcCharacter>();
            foreach (KeyValuePair<string, NpcCharacter> pair in characters)
                catalog.characters.Add(pair.Value);
            catalog.characters.Sort((a, b) => a.unlockDay.CompareTo(b.unlockDay));

            catalog.props = new List<NpcProp>();
            foreach (KeyValuePair<string, NpcProp> pair in props)
                catalog.props.Add(pair.Value);

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return result;
        }

        public static string ValidateImported()
        {
            NpcCatalog catalog = AssetDatabase.LoadAssetAtPath<NpcCatalog>(CatalogAssetPath);
            if (catalog == null || catalog.characters == null || catalog.characters.Count != 5)
                return "NpcCatalog 应有 5 个 NPC。";
            if (catalog.props == null || catalog.props.Count != 5)
                return "NpcCatalog 应有 5 件道具。";

            NpcCharacter bard = catalog.FindCharacter("npc_bard");
            NpcCharacter cat = catalog.FindCharacter("npc_ocatopus");
            NpcCharacter novelist = catalog.FindCharacter("npc_novelist");
            NpcCharacter designer = catalog.FindCharacter("npc_designer");
            NpcCharacter tutu = catalog.FindCharacter("npc_tutu");
            if (bard == null || cat == null || novelist == null || designer == null || tutu == null)
                return "缺少 npc_bard、npc_ocatopus、npc_novelist、npc_designer 或 npc_tutu。";

            if (bard.unlockDay != 2 || bard.phase != DayPhase.Night || bard.portrait == null)
                return "吟游诗人应为第 2 夜，并有头像。";
            if (cat.unlockDay != 3 || cat.phase != DayPhase.Night || cat.portrait == null)
                return "猫章鱼应为第 3 夜，并有头像。";
            if (cat.displayNameEn != "Ocatopus")
                return "猫章鱼英文名应为 Ocatopus。";
            if (tutu.unlockDay != 6 || tutu.phase != DayPhase.Day || tutu.portrait == null)
                return "鼻嘎图应为第 6 天白天，并有头像。";
            if (designer.displayNameEn != "Dyspegniter")
                return "社积食英文名应为 Dyspegniter。";

            if (!HasLine(
                bard,
                "opt_bard_you",
                "也许是神明的启示，也许只是我不甘心就此沉默。",
                "Perhaps it was a revelation from the gods—or perhaps I simply refused to fall silent."))
                return "吟游诗人「那你呢」缺少英文那一句。";
            if (LineStartsWithSpeaker(novelist))
                return "小说家句子仍带着说话人前缀。";

            if (!HasReward(bard, "opt_bard_silent", NpcRewardKind.Ingredient, "ing_beef", 3))
                return "我不会唱歌应给虹彩牛肉 3。";
            if (!HasReward(cat, "opt_ocatopus_leave", NpcRewardKind.Ingredient, "ing_berry", 2))
                return "放弃它吧应给莓果 2。";
            if (!HasReward(designer, "opt_designer_bigger", NpcRewardKind.Ingredient, "ing_rice", 5)
                || !HasReward(designer, "opt_designer_bigger", NpcRewardKind.Ingredient, "ing_tomato", 5))
                return "把图画大一点应给大米 5 和番茄 5。";
            if (!HasReward(tutu, "opt_tutu_keep", NpcRewardKind.Ingredient, "ing_cucumber", 5)
                || !HasReward(tutu, "opt_tutu_keep", NpcRewardKind.Ingredient, "ing_egg", 3))
                return "你留着吧应给黄瓜 5 和鸡蛋 3。";
            if (!HasReward(tutu, "opt_tutu_take", NpcRewardKind.Prop, "prop_list", 1)
                || !HasReward(tutu, "opt_tutu_both", NpcRewardKind.Prop, "prop_list", 1))
                return "鼻嘎图的两个选项应给异常记录清单。";

            string[] propIds = { "prop_earphone", "prop_list", "prop_bucket", "prop_sampler", "prop_manuscript" };
            for (int i = 0; i < propIds.Length; i++)
            {
                NpcProp prop = catalog.FindProp(propIds[i]);
                if (prop == null || prop.sprite == null || string.IsNullOrEmpty(prop.descriptionZh))
                    return "道具缺少图或中文描述：" + propIds[i];
            }

            return null;
        }

        static Dictionary<string, NpcCharacter> ImportCharacters(DataTable table, ImportResult result)
        {
            var characters = new Dictionary<string, NpcCharacter>();
            if (table == null)
                return characters;

            int rowIndex = 1;
            foreach (DataRow row in table.Rows)
            {
                rowIndex++;
                string id = Cell(row, "id");
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                id = id.Trim();
                if (characters.ContainsKey(id))
                {
                    Fail(result, "NpcCharacters 重复 id " + id + "，行 " + rowIndex);
                    continue;
                }

                string nameZh = Cell(row, "displayNameZh");
                if (string.IsNullOrEmpty(nameZh))
                {
                    Fail(result, "NpcCharacters 缺少中文名，行 " + rowIndex);
                    continue;
                }

                string portraitPath = Cell(row, "portrait");
                Sprite portrait = LoadSprite(portraitPath, "NpcCharacters/" + id + " 行 " + rowIndex, result);
                if (portrait == null)
                    continue;

                if (!TryParseInt(Cell(row, "unlockDay"), out int unlockDay, "NpcCharacters/" + id + ".unlockDay 行 " + rowIndex, result))
                    continue;
                if (!TryParseEnum(Cell(row, "phase"), out DayPhase phase, "NpcCharacters/" + id + ".phase 行 " + rowIndex, result))
                    continue;

                characters[id] = new NpcCharacter
                {
                    id = id,
                    displayNameZh = nameZh,
                    displayNameEn = Cell(row, "displayNameEn"),
                    eventNameZh = Cell(row, "eventNameZh"),
                    eventNameEn = Cell(row, "eventNameEn"),
                    portrait = portrait,
                    unlockDay = unlockDay,
                    phase = phase,
                    options = new List<NpcOption>(),
                    lines = new List<NpcLine>()
                };
            }

            return characters;
        }

        static Dictionary<string, NpcProp> ImportProps(DataTable table, ImportResult result)
        {
            var props = new Dictionary<string, NpcProp>();
            if (table == null)
                return props;

            int rowIndex = 1;
            foreach (DataRow row in table.Rows)
            {
                rowIndex++;
                string id = Cell(row, "id");
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                id = id.Trim();
                if (props.ContainsKey(id))
                {
                    Fail(result, "NpcProps 重复 id " + id + "，行 " + rowIndex);
                    continue;
                }

                string nameZh = Cell(row, "displayNameZh");
                string description = NormalizeBreaks(Cell(row, "descriptionZh"));
                if (string.IsNullOrEmpty(nameZh) || string.IsNullOrEmpty(description))
                {
                    Fail(result, "NpcProps 缺少中文名或描述，行 " + rowIndex);
                    continue;
                }

                Sprite sprite = LoadSprite(Cell(row, "sprite"), "NpcProps/" + id + " 行 " + rowIndex, result);
                if (sprite == null)
                    continue;

                props[id] = new NpcProp
                {
                    id = id,
                    displayNameZh = nameZh,
                    displayNameEn = Cell(row, "displayNameEn"),
                    descriptionZh = description,
                    descriptionEn = NormalizeBreaks(Cell(row, "descriptionEn")),
                    sprite = sprite
                };
            }

            return props;
        }

        static void ImportOptions(DataTable table, Dictionary<string, NpcCharacter> characters, ImportResult result)
        {
            if (table == null)
                return;

            var seen = new HashSet<string>();
            int rowIndex = 1;
            foreach (DataRow row in table.Rows)
            {
                rowIndex++;
                string npcId = Cell(row, "npcId");
                string optionId = Cell(row, "optionId");
                if (string.IsNullOrWhiteSpace(npcId) && string.IsNullOrWhiteSpace(optionId))
                    continue;

                if (!characters.TryGetValue(npcId, out NpcCharacter character))
                {
                    Fail(result, "NpcOptions 找不到 NPC " + npcId + "，行 " + rowIndex);
                    continue;
                }

                if (string.IsNullOrEmpty(optionId) || !seen.Add(optionId))
                {
                    Fail(result, "NpcOptions 选项 id 空或重复，行 " + rowIndex);
                    continue;
                }

                string label = Cell(row, "displayNameZh");
                if (string.IsNullOrEmpty(label))
                {
                    Fail(result, "NpcOptions 缺少中文，行 " + rowIndex);
                    continue;
                }

                if (!TryParseInt(Cell(row, "order"), out int order, "NpcOptions/" + optionId + ".order 行 " + rowIndex, result))
                    continue;

                character.options.Add(new NpcOption
                {
                    optionId = optionId,
                    order = order,
                    displayNameZh = label,
                    displayNameEn = Cell(row, "displayNameEn"),
                    rewards = new List<NpcReward>()
                });
            }
        }

        static void ImportRewards(
            DataTable table,
            Dictionary<string, NpcCharacter> characters,
            Dictionary<string, NpcProp> props,
            ImportResult result)
        {
            if (table == null)
                return;

            int rowIndex = 1;
            foreach (DataRow row in table.Rows)
            {
                rowIndex++;
                string optionId = Cell(row, "optionId");
                string rewardId = Cell(row, "rewardId");
                if (string.IsNullOrWhiteSpace(optionId) && string.IsNullOrWhiteSpace(rewardId))
                    continue;

                NpcOption option = FindOption(characters, optionId);
                if (option == null)
                {
                    Fail(result, "NpcRewards 找不到选项 " + optionId + "，行 " + rowIndex);
                    continue;
                }

                if (!TryParseEnum(Cell(row, "kind"), out NpcRewardKind kind, "NpcRewards/" + optionId + " 行 " + rowIndex, result))
                    continue;
                if (!TryParseInt(Cell(row, "count"), out int count, "NpcRewards/" + optionId + ".count 行 " + rowIndex, result))
                    continue;
                if (count < 1)
                {
                    Fail(result, "NpcRewards 数量至少为 1，行 " + rowIndex);
                    continue;
                }

                if (kind == NpcRewardKind.Ingredient)
                {
                    string path = IngredientsFolder + "/" + rewardId + ".asset";
                    if (AssetDatabase.LoadAssetAtPath<IngredientDefinition>(path) == null)
                    {
                        Fail(result, "NpcRewards 找不到食材 " + rewardId + "，行 " + rowIndex);
                        continue;
                    }
                }
                else if (!props.ContainsKey(rewardId))
                {
                    Fail(result, "NpcRewards 找不到道具 " + rewardId + "，行 " + rowIndex);
                    continue;
                }

                option.rewards.Add(new NpcReward
                {
                    kind = kind,
                    rewardId = rewardId,
                    count = count
                });
            }
        }

        static void ImportLines(DataTable table, Dictionary<string, NpcCharacter> characters, ImportResult result)
        {
            if (table == null)
                return;

            int rowIndex = 1;
            foreach (DataRow row in table.Rows)
            {
                rowIndex++;
                string npcId = Cell(row, "npcId");
                string text = StripSpeaker(Cell(row, "textZh"));
                if (string.IsNullOrWhiteSpace(npcId) && string.IsNullOrEmpty(text))
                    continue;

                if (!characters.TryGetValue(npcId, out NpcCharacter character))
                {
                    Fail(result, "NpcLines 找不到 NPC " + npcId + "，行 " + rowIndex);
                    continue;
                }

                if (string.IsNullOrEmpty(text))
                {
                    Fail(result, "NpcLines 缺少中文，行 " + rowIndex);
                    continue;
                }

                if (!TryParseInt(Cell(row, "order"), out int order, "NpcLines/" + npcId + ".order 行 " + rowIndex, result))
                    continue;

                string optionId = Cell(row, "optionId");
                if (!string.IsNullOrEmpty(optionId) && FindOption(character, optionId) == null)
                {
                    Fail(result, "NpcLines 找不到选项 " + optionId + "，行 " + rowIndex);
                    continue;
                }

                character.lines.Add(new NpcLine
                {
                    optionId = optionId,
                    order = order,
                    textZh = text,
                    textEn = StripSpeaker(Cell(row, "textEn"))
                });
            }
        }

        static void ImportUiStrings(DataTable table, ImportResult result)
        {
            if (table == null)
                return;

            UiTextCatalog asset = AssetDatabase.LoadAssetAtPath<UiTextCatalog>(UiTextCatalogPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<UiTextCatalog>();
                AssetDatabase.CreateAsset(asset, UiTextCatalogPath);
            }

            asset.rows = new List<UiTextString>();
            foreach (DataRow row in table.Rows)
            {
                string id = Cell(row, "id");
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                string textZh = Cell(row, "textZh");
                if (string.IsNullOrEmpty(textZh))
                {
                    Fail(result, "UiStrings." + id + " 缺少 textZh。");
                    continue;
                }

                asset.rows.Add(new UiTextString
                {
                    id = id.Trim(),
                    textZh = textZh,
                    textEn = Cell(row, "textEn")
                });
            }

            EditorUtility.SetDirty(asset);
        }

        static NpcCatalog FindOrCreateCatalog()
        {
            NpcCatalog catalog = AssetDatabase.LoadAssetAtPath<NpcCatalog>(CatalogAssetPath);
            if (catalog != null)
                return catalog;

            catalog = ScriptableObject.CreateInstance<NpcCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
            return catalog;
        }

        static Sprite LoadSprite(string path, string context, ImportResult result)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                Fail(result, "缺少图片路径 @ " + context);
                return null;
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path.Trim());
            if (sprite == null)
                Fail(result, "找不到 Sprite " + path + " @ " + context);

            return sprite;
        }

        static NpcOption FindOption(Dictionary<string, NpcCharacter> characters, string optionId)
        {
            foreach (KeyValuePair<string, NpcCharacter> pair in characters)
            {
                NpcOption option = FindOption(pair.Value, optionId);
                if (option != null)
                    return option;
            }

            return null;
        }

        static NpcOption FindOption(NpcCharacter character, string optionId)
        {
            if (character == null || character.options == null)
                return null;

            for (int i = 0; i < character.options.Count; i++)
            {
                NpcOption option = character.options[i];
                if (option != null && option.optionId == optionId)
                    return option;
            }

            return null;
        }

        static bool HasReward(NpcCharacter character, string optionId, NpcRewardKind kind, string rewardId, int count)
        {
            NpcOption option = FindOption(character, optionId);
            if (option == null || option.rewards == null)
                return false;

            for (int i = 0; i < option.rewards.Count; i++)
            {
                NpcReward reward = option.rewards[i];
                if (reward != null && reward.kind == kind && reward.rewardId == rewardId && reward.count == count)
                    return true;
            }

            return false;
        }

        static bool HasLine(NpcCharacter character, string optionId, string zh, string en)
        {
            if (character == null || character.lines == null)
                return false;

            for (int i = 0; i < character.lines.Count; i++)
            {
                NpcLine line = character.lines[i];
                if (line != null && line.optionId == optionId && line.textZh == zh && (line.textEn ?? "") == en)
                    return true;
            }

            return false;
        }

        static bool LineStartsWithSpeaker(NpcCharacter character)
        {
            if (character == null || character.lines == null)
                return false;

            for (int i = 0; i < character.lines.Count; i++)
            {
                NpcLine line = character.lines[i];
                if (line == null)
                    continue;

                if (StartsWithSpeaker(line.textZh) || StartsWithSpeaker(line.textEn))
                    return true;
            }

            return false;
        }

        static bool StartsWithSpeaker(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            for (int i = 0; i < SpeakerPrefixes.Length; i++)
            {
                if (text.StartsWith(SpeakerPrefixes[i], StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        static string StripSpeaker(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            string trimmed = text.Trim();
            for (int i = 0; i < SpeakerPrefixes.Length; i++)
            {
                string prefix = SpeakerPrefixes[i];
                if (trimmed.StartsWith(prefix, StringComparison.Ordinal))
                    return trimmed.Substring(prefix.Length).Trim();
            }

            return trimmed;
        }

        static string NormalizeBreaks(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            return text.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        }

        static void Fail(ImportResult result, string message)
        {
            Debug.LogError(message);
            result.ErrorCount++;
        }

        static DataTable RequireSheet(DataSet dataSet, string name, ImportResult result)
        {
            DataTable table = dataSet.Tables[name];
            if (table == null)
                Fail(result, "Excel 缺少 Sheet：" + name);

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

        static bool TryParseEnum<T>(string text, out T value, string context, ImportResult result) where T : struct
        {
            value = default;
            if (string.IsNullOrWhiteSpace(text))
            {
                Fail(result, "缺少枚举值 @ " + context);
                return false;
            }

            if (Enum.TryParse(text.Trim(), true, out value))
                return true;

            Fail(result, "无法解析枚举 " + typeof(T).Name + "='" + text + "' @ " + context);
            return false;
        }

        static bool TryParseInt(string text, out int value, string context, ImportResult result)
        {
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                return true;

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
            {
                value = (int)number;
                return true;
            }

            value = 0;
            Fail(result, "无法解析整数='" + text + "' @ " + context);
            return false;
        }

        public sealed class ImportResult
        {
            public int ErrorCount;
        }
    }
}
