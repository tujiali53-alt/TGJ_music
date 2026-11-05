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
    public static class NightTableImporter
    {
        const string TableAssetPath = "Assets/_Project/Data/Tables/NightTables.xlsx";
        const string EventsFolder = "Assets/_Project/Data/NightEvents";
        const string BuffAssetPath = "Assets/_Project/Data/NightBuffTable.asset";
        const string LogCatalogPath = "Assets/_Project/Data/NightLogCatalog.asset";
        const string IngredientsFolder = "Assets/_Project/Data/Ingredients";

        [MenuItem("Cooking/Import Night Tables")]
        public static void ImportMenu()
        {
            ImportResult result = Import();
            if (result.ErrorCount > 0)
                Debug.LogError(
                    "Night tables import finished with errors. Created "
                    + result.Created + ", updated " + result.Updated + ", errors "
                    + result.ErrorCount + ".");
            else
                Debug.Log(
                    "Night tables import OK. Created " + result.Created
                    + ", updated " + result.Updated + ".");
        }

        public static string ValidateImported()
        {
            NightEventDefinition mushroom = FindEvent("event_mushroom");
            if (mushroom == null || mushroom.unlockNight != 0 || mushroom.displayNameZh != "蘑菇深埋于地")
                return "event_mushroom 未按表导入。";

            NightOption pull = FindOption(mushroom, "opt_mushroom_pull");
            if (pull.optionId != "opt_mushroom_pull" || pull.successFaces != 4 || pull.rewards == null || pull.rewards.Count != 2)
                return "连根拔起的成功面或奖励行数不对。";
            if (pull.rewards[0].ingredientId != "ing_mushroom" || pull.rewards[0].count != 10
                || pull.rewards[1].ingredientId != "ing_yam" || pull.rewards[1].count != 1)
                return "连根拔起奖励应为蘑菇 10 和山药 1。";

            NightEventDefinition fishing = FindEvent("event_fishing");
            NightOption cast = FindOption(fishing, "opt_fishing_cast");
            if (cast.successFaces != 5 || cast.conditionType != NightConditionType.None || cast.conditionValue != 0f)
                return "拉竿的成功面或条件不对。";
            if (cast.rewards == null || cast.rewards.Count != 2
                || cast.rewards[0].pickMode != NightRewardPick.One
                || cast.rewards[1].pickMode != NightRewardPick.One)
                return "拉竿奖励应为两行 One。";

            NightBuffTable buffs = AssetDatabase.LoadAssetAtPath<NightBuffTable>(BuffAssetPath);
            if (buffs == null || buffs.rows == null || buffs.rows.Count != 4)
                return "NightBuffTable 应有 4 行。";
            if (buffs.rows[0].successFaceDelta != -1 || buffs.rows[3].radiusBonus != 0.5f)
                return "buff 首行应减 1 面，末行半径加 0.5。";
            if (buffs.rows[0].logTextId != "log.buff_very_low" || buffs.rows[3].logTextId != "log.buff_high")
                return "buff 日志 id 应对准 NightLogs。";

            NightLogCatalog logs = AssetDatabase.LoadAssetAtPath<NightLogCatalog>(LogCatalogPath);
            if (logs == null || logs.rows == null || logs.rows.Count < 16)
                return "NightLogCatalog 行数不足。";
            if (!logs.TryGet("log.roll", out NightLogString roll) || string.IsNullOrEmpty(roll.textEn))
                return "NightLogs 缺少 log.roll 英文。";
            if (string.IsNullOrEmpty(roll.color))
                return "NightLogs 的 color 不能为空。";

            if (mushroom.introPortrait == null || mushroom.introPortrait.name != "Main_Normal")
                return "蘑菇深埋于地的开场表情应为 Main_Normal。";
            if (pull.successPortrait == null || pull.successPortrait.name != "Main_Happy"
                || pull.failPortrait == null || pull.failPortrait.name != "Main_Worry")
                return "连根拔起成功应为 Main_Happy，失败应为 Main_Worry。";
            NightOption pinch = FindOption(mushroom, "opt_mushroom_pinch");
            if (pinch.successPortrait == null || pinch.successPortrait.name != "Main_Smile" || pinch.failPortrait != null)
                return "小心掰断成功应为 Main_Smile，没有失败表情。";

            string shocked = PortraitOn(FindEvent("event_merchant"), true, "");
            string mourn = PortraitOn(FindEvent("event_grave"), false, "opt_grave_mourn");
            Sprite shockedSprite = FindPortraitSprite("Main_Shocked");
            if (shockedSprite == null)
            {
                if (shocked != "" || mourn != "")
                    return "Main_Shocked 不存在时，投机者开场和默哀成功应留空。";
            }
            else if (shocked != "Main_Shocked" || mourn != "Main_Shocked")
                return "投机者开场和默哀成功应为 Main_Shocked。";

            string[] ids =
            {
                "event_mushroom",
                "event_merchant",
                "event_beasts",
                "event_fishing",
                "event_grave",
                "event_moonstone",
                "event_a",
                "event_b",
                "event_c"
            };
            for (int i = 0; i < ids.Length; i++)
            {
                if (FindEvent(ids[i]) == null)
                    return "缺少事件资产 " + ids[i];
            }

            return null;
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
            EnsureFolder(EventsFolder);
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

            Dictionary<string, string> priorEventEn = SnapshotEventEn();
            Dictionary<string, string> priorOptionEn = SnapshotOptionEn();
            Dictionary<string, string> priorLineEn = SnapshotLineEn();

            Dictionary<string, NightEventDefinition> events = ImportEvents(
                RequireSheet(dataSet, "NightEvents", result),
                result);
            ImportOptions(RequireSheet(dataSet, "NightOptions", result), events, result);
            ImportRewards(RequireSheet(dataSet, "NightRewards", result), events, result);
            ImportLines(RequireSheet(dataSet, "NightLines", result), events, result);
            ImportBuffs(RequireSheet(dataSet, "NightBuffs", result), result);
            ImportLogs(RequireSheet(dataSet, "NightLogs", result), result);
            ImportPortraits(RequireSheet(dataSet, "NightPortraits", result), events, result);
            RestoreMissingEn(events, priorEventEn, priorOptionEn, priorLineEn);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return result;
        }

        static Dictionary<string, string> SnapshotEventEn()
        {
            var map = new Dictionary<string, string>();
            string[] guids = AssetDatabase.FindAssets("t:NightEventDefinition", new[] { EventsFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                NightEventDefinition asset = AssetDatabase.LoadAssetAtPath<NightEventDefinition>(
                    AssetDatabase.GUIDToAssetPath(guids[i]));
                if (asset == null || string.IsNullOrEmpty(asset.id) || string.IsNullOrEmpty(asset.displayNameEn))
                    continue;
                map[asset.id] = asset.displayNameEn;
            }

            return map;
        }

        static Dictionary<string, string> SnapshotOptionEn()
        {
            var map = new Dictionary<string, string>();
            string[] guids = AssetDatabase.FindAssets("t:NightEventDefinition", new[] { EventsFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                NightEventDefinition asset = AssetDatabase.LoadAssetAtPath<NightEventDefinition>(
                    AssetDatabase.GUIDToAssetPath(guids[i]));
                if (asset == null || asset.options == null)
                    continue;

                for (int o = 0; o < asset.options.Count; o++)
                {
                    NightOption option = asset.options[o];
                    if (string.IsNullOrEmpty(option.optionId) || string.IsNullOrEmpty(option.displayNameEn))
                        continue;
                    map[option.optionId] = option.displayNameEn;
                }
            }

            return map;
        }

        static Dictionary<string, string> SnapshotLineEn()
        {
            var map = new Dictionary<string, string>();
            string[] guids = AssetDatabase.FindAssets("t:NightEventDefinition", new[] { EventsFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                NightEventDefinition asset = AssetDatabase.LoadAssetAtPath<NightEventDefinition>(
                    AssetDatabase.GUIDToAssetPath(guids[i]));
                if (asset == null || asset.lines == null)
                    continue;

                for (int l = 0; l < asset.lines.Count; l++)
                {
                    NightLine line = asset.lines[l];
                    if (string.IsNullOrEmpty(line.textEn))
                        continue;
                    map[LineKey(asset.id, line)] = line.textEn;
                }
            }

            return map;
        }

        static string LineKey(string eventId, NightLine line)
        {
            return eventId + "|" + (line.optionId ?? "") + "|" + (int)line.phase + "|" + line.order;
        }

        static void RestoreMissingEn(
            Dictionary<string, NightEventDefinition> events,
            Dictionary<string, string> priorEventEn,
            Dictionary<string, string> priorOptionEn,
            Dictionary<string, string> priorLineEn)
        {
            if (events == null)
                return;

            foreach (KeyValuePair<string, NightEventDefinition> pair in events)
            {
                NightEventDefinition asset = pair.Value;
                if (asset == null)
                    continue;

                if (string.IsNullOrEmpty(asset.displayNameEn)
                    && priorEventEn.TryGetValue(asset.id, out string eventEn))
                {
                    asset.displayNameEn = eventEn;
                    EditorUtility.SetDirty(asset);
                }

                if (asset.options != null)
                {
                    for (int i = 0; i < asset.options.Count; i++)
                    {
                        NightOption option = asset.options[i];
                        if (string.IsNullOrEmpty(option.displayNameEn)
                            && priorOptionEn.TryGetValue(option.optionId, out string optionEn))
                        {
                            option.displayNameEn = optionEn;
                            asset.options[i] = option;
                            EditorUtility.SetDirty(asset);
                        }
                    }
                }

                if (asset.lines != null)
                {
                    for (int i = 0; i < asset.lines.Count; i++)
                    {
                        NightLine line = asset.lines[i];
                        if (!string.IsNullOrEmpty(line.textEn))
                            continue;
                        if (!priorLineEn.TryGetValue(LineKey(asset.id, line), out string lineEn))
                            continue;
                        line.textEn = lineEn;
                        asset.lines[i] = line;
                        EditorUtility.SetDirty(asset);
                    }
                }
            }
        }

        static Dictionary<string, NightEventDefinition> ImportEvents(DataTable table, ImportResult result)
        {
            var events = new Dictionary<string, NightEventDefinition>();
            if (table == null)
                return events;

            foreach (DataRow row in table.Rows)
            {
                string id = Cell(row, "id");
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                id = id.Trim();
                NightEventDefinition asset = FindOrCreateEvent(id, result);
                asset.id = id;
                asset.displayNameZh = FirstCell(row, "displayNameZh", "displayName");
                asset.displayNameEn = Cell(row, "displayNameEn");
                if (!TryParseInt(Cell(row, "unlockNight"), out int unlockNight, "NightEvents/" + id + ".unlockNight", result))
                    continue;

                asset.unlockNight = unlockNight;
                asset.options = new List<NightOption>();
                asset.lines = new List<NightLine>();
                events[id] = asset;
                EditorUtility.SetDirty(asset);
            }

            return events;
        }

        static void ImportOptions(DataTable table, Dictionary<string, NightEventDefinition> events, ImportResult result)
        {
            if (table == null)
                return;

            foreach (DataRow row in table.Rows)
            {
                string eventId = Cell(row, "eventId");
                string optionId = Cell(row, "optionId");
                if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(optionId))
                    continue;

                eventId = eventId.Trim();
                optionId = optionId.Trim();
                if (!events.TryGetValue(eventId, out NightEventDefinition asset))
                {
                    Debug.LogError("NightOptions 找不到事件：" + eventId);
                    result.ErrorCount++;
                    continue;
                }

                if (!TryParseInt(Cell(row, "order"), out int order, "NightOptions/" + optionId + ".order", result))
                    continue;
                if (!TryParseEnum(Cell(row, "conditionType"), out NightConditionType condition, "NightOptions/" + optionId + ".conditionType", result))
                    continue;
                if (!TryParseInt(Cell(row, "successFaces"), out int faces, "NightOptions/" + optionId + ".successFaces", result))
                    continue;
                if (faces < 1 || faces > 6)
                {
                    Debug.LogError("successFaces 必须是 1 到 6 @ " + optionId);
                    result.ErrorCount++;
                    continue;
                }

                float conditionValue = 0f;
                string valueText = Cell(row, "conditionValue");
                if (!string.IsNullOrWhiteSpace(valueText)
                    && !TryParseFloat(valueText, out conditionValue, "NightOptions/" + optionId + ".conditionValue", result))
                    continue;

                asset.options.Add(new NightOption
                {
                    optionId = optionId,
                    order = order,
                    displayNameZh = FirstCell(row, "displayNameZh", "displayName"),
                    displayNameEn = Cell(row, "displayNameEn"),
                    conditionType = condition,
                    conditionValue = conditionValue,
                    successFaces = faces,
                    rewards = new List<NightReward>()
                });
                EditorUtility.SetDirty(asset);
            }

            foreach (KeyValuePair<string, NightEventDefinition> pair in events)
            {
                pair.Value.options.Sort((a, b) => a.order.CompareTo(b.order));
                EditorUtility.SetDirty(pair.Value);
            }
        }

        static void ImportRewards(DataTable table, Dictionary<string, NightEventDefinition> events, ImportResult result)
        {
            if (table == null)
                return;

            foreach (DataRow row in table.Rows)
            {
                string optionId = Cell(row, "optionId");
                string ingredientId = Cell(row, "ingredientId");
                if (string.IsNullOrWhiteSpace(optionId) || string.IsNullOrWhiteSpace(ingredientId))
                    continue;

                optionId = optionId.Trim();
                ingredientId = ingredientId.Trim();
                if (!TryParseEnum(Cell(row, "pickMode"), out NightRewardPick pickMode, "NightRewards/" + optionId + ".pickMode", result))
                    continue;
                if (!TryParseInt(Cell(row, "count"), out int count, "NightRewards/" + optionId + ".count", result))
                    continue;
                if (FindIngredient(ingredientId) == null)
                {
                    Debug.LogError("NightRewards 找不到食材：" + ingredientId);
                    result.ErrorCount++;
                    continue;
                }

                if (!TryAddReward(events, optionId, new NightReward
                {
                    pickMode = pickMode,
                    ingredientId = ingredientId,
                    count = count
                }))
                {
                    Debug.LogError("NightRewards 找不到选项：" + optionId);
                    result.ErrorCount++;
                }
            }
        }

        static bool TryAddReward(Dictionary<string, NightEventDefinition> events, string optionId, NightReward reward)
        {
            foreach (KeyValuePair<string, NightEventDefinition> pair in events)
            {
                List<NightOption> options = pair.Value.options;
                for (int i = 0; i < options.Count; i++)
                {
                    if (options[i].optionId != optionId)
                        continue;

                    NightOption option = options[i];
                    if (option.rewards == null)
                        option.rewards = new List<NightReward>();
                    option.rewards.Add(reward);
                    options[i] = option;
                    EditorUtility.SetDirty(pair.Value);
                    return true;
                }
            }

            return false;
        }

        static void ImportLines(DataTable table, Dictionary<string, NightEventDefinition> events, ImportResult result)
        {
            if (table == null)
                return;

            foreach (DataRow row in table.Rows)
            {
                string eventId = Cell(row, "eventId");
                if (string.IsNullOrWhiteSpace(eventId))
                    continue;

                eventId = eventId.Trim();
                if (!events.TryGetValue(eventId, out NightEventDefinition asset))
                {
                    Debug.LogError("NightLines 找不到事件：" + eventId);
                    result.ErrorCount++;
                    continue;
                }

                if (!TryParseEnum(Cell(row, "phase"), out NightLinePhase phase, "NightLines/" + eventId + ".phase", result))
                    continue;
                if (!TryParseInt(Cell(row, "order"), out int order, "NightLines/" + eventId + ".order", result))
                    continue;

                string textZh = FirstCell(row, "textZh", "text");
                if (textZh == "-")
                    continue;

                if (asset.lines == null)
                    asset.lines = new List<NightLine>();
                asset.lines.Add(new NightLine
                {
                    optionId = Cell(row, "optionId"),
                    phase = phase,
                    order = order,
                    textZh = textZh,
                    textEn = Cell(row, "textEn")
                });
                EditorUtility.SetDirty(asset);
            }

            foreach (KeyValuePair<string, NightEventDefinition> pair in events)
            {
                if (pair.Value.lines != null)
                    pair.Value.lines.Sort((a, b) => a.order.CompareTo(b.order));
            }
        }

        static void ImportBuffs(DataTable table, ImportResult result)
        {
            if (table == null)
                return;

            NightBuffTable asset = AssetDatabase.LoadAssetAtPath<NightBuffTable>(BuffAssetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<NightBuffTable>();
                AssetDatabase.CreateAsset(asset, BuffAssetPath);
                result.Created++;
            }
            else
                result.Updated++;

            asset.rows = new List<NightBuffRow>();
            foreach (DataRow row in table.Rows)
            {
                string minText = FirstCell(row, "min", "minDeliciousness");
                if (string.IsNullOrWhiteSpace(minText))
                    continue;

                if (!TryParseFloat(minText, out float min, "NightBuffs.min", result))
                    continue;
                if (!TryParseFloat(FirstCell(row, "max", "maxDeliciousness"), out float max, "NightBuffs.max", result))
                    continue;
                if (!TryParseFloat(Cell(row, "speedMultiplier"), out float speed, "NightBuffs.speedMultiplier", result))
                    continue;
                if (!TryParseInt(Cell(row, "successFaceDelta"), out int delta, "NightBuffs.successFaceDelta", result))
                    continue;
                if (!TryParseFloat(Cell(row, "radiusBonus"), out float radius, "NightBuffs.radiusBonus", result))
                    continue;

                string logTextId = FirstCell(row, "logTextId", "logText");
                asset.rows.Add(new NightBuffRow
                {
                    minDeliciousness = min,
                    maxDeliciousness = max,
                    speedMultiplier = speed,
                    successFaceDelta = delta,
                    radiusBonus = radius,
                    logTextId = logTextId
                });
            }

            EditorUtility.SetDirty(asset);
        }

        static void ImportLogs(DataTable table, ImportResult result)
        {
            if (table == null)
                return;

            NightLogCatalog asset = AssetDatabase.LoadAssetAtPath<NightLogCatalog>(LogCatalogPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<NightLogCatalog>();
                AssetDatabase.CreateAsset(asset, LogCatalogPath);
                result.Created++;
            }
            else
                result.Updated++;

            Dictionary<string, string> priorEn = new Dictionary<string, string>();
            Dictionary<string, string> priorColor = new Dictionary<string, string>();
            if (asset.rows != null)
            {
                for (int i = 0; i < asset.rows.Count; i++)
                {
                    NightLogString row = asset.rows[i];
                    if (string.IsNullOrEmpty(row.id))
                        continue;
                    if (!string.IsNullOrEmpty(row.textEn))
                        priorEn[row.id] = row.textEn;
                    if (!string.IsNullOrEmpty(row.color))
                        priorColor[row.id] = row.color;
                }
            }

            asset.rows = new List<NightLogString>();
            foreach (DataRow row in table.Rows)
            {
                string id = Cell(row, "id");
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                string textZh = Cell(row, "textZh");
                string textEn = Cell(row, "textEn");
                string color = Cell(row, "color");
                if (string.IsNullOrEmpty(textEn) && priorEn.TryGetValue(id, out string keptEn))
                    textEn = keptEn;
                if (string.IsNullOrEmpty(color) && priorColor.TryGetValue(id, out string keptColor))
                    color = keptColor;

                if (string.IsNullOrEmpty(textZh))
                {
                    Debug.LogError("NightLogs." + id + " 缺少 textZh。");
                    result.ErrorCount++;
                    continue;
                }

                asset.rows.Add(new NightLogString
                {
                    id = id,
                    textZh = textZh,
                    textEn = textEn,
                    color = color
                });
            }

            EditorUtility.SetDirty(asset);
        }

        static readonly string[] PortraitFolders =
        {
            "Assets/_Project/Arts/player/emotion",
            "Assets/_Project/Arts/player/Walking"
        };

        static void ImportPortraits(DataTable table, Dictionary<string, NightEventDefinition> events, ImportResult result)
        {
            if (table == null)
                return;

            foreach (DataRow row in table.Rows)
            {
                string eventId = Cell(row, "eventId");
                if (string.IsNullOrWhiteSpace(eventId))
                    continue;

                eventId = eventId.Trim();
                if (!events.TryGetValue(eventId, out NightEventDefinition asset))
                {
                    Debug.LogError("NightPortraits 找不到事件：" + eventId);
                    result.ErrorCount++;
                    continue;
                }

                string shown = FirstCell(row, "displayNameZh", "displayName");
                if (!string.IsNullOrEmpty(shown) && shown != asset.displayNameZh)
                {
                    Debug.LogError("NightPortraits 事件名不一致：" + eventId + " 表上是 " + shown);
                    result.ErrorCount++;
                }

                if (TryLoadPortrait(Cell(row, "intro"), out Sprite intro, "NightPortraits/" + eventId + ".intro", result))
                    asset.introPortrait = intro;
                else
                    asset.introPortrait = null;

                AssignOptionPortrait(asset, 1, row, "option1Success", "option1Fail", result);
                AssignOptionPortrait(asset, 2, row, "option2Success", "option2Fail", result);
                AssignOptionPortrait(asset, 3, row, "option3Success", "option3Fail", result);
                EditorUtility.SetDirty(asset);
            }
        }

        static void AssignOptionPortrait(
            NightEventDefinition asset,
            int order,
            DataRow row,
            string successColumn,
            string failColumn,
            ImportResult result)
        {
            string successText = Cell(row, successColumn);
            string failText = Cell(row, failColumn);
            int index = FindOptionIndex(asset, order);
            if (index < 0)
            {
                if (!IsBlankPortrait(successText) || !IsBlankPortrait(failText))
                {
                    Debug.LogError(asset.id + " 没有第 " + order + " 个选项，不能绑表情。");
                    result.ErrorCount++;
                }

                return;
            }

            string context = "NightPortraits/" + asset.id + ".option" + order;
            bool successOk = TryLoadPortrait(successText, out Sprite success, context + ".success", result);
            bool failOk = TryLoadPortrait(failText, out Sprite fail, context + ".fail", result);
            NightOption option = asset.options[index];
            option.successPortrait = successOk ? success : null;
            option.failPortrait = failOk ? fail : null;
            asset.options[index] = option;
        }

        static int FindOptionIndex(NightEventDefinition asset, int order)
        {
            if (asset == null || asset.options == null)
                return -1;

            for (int i = 0; i < asset.options.Count; i++)
            {
                if (asset.options[i].order == order)
                    return i;
            }

            return -1;
        }

        static bool IsBlankPortrait(string text)
        {
            return string.IsNullOrWhiteSpace(text) || text.Trim() == "-";
        }

        static bool TryLoadPortrait(string text, out Sprite sprite, string context, ImportResult result)
        {
            sprite = null;
            if (IsBlankPortrait(text))
                return true;

            string name = text.Trim();
            Sprite found = null;
            int hits = 0;
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
                Debug.LogError("找不到表情 " + name + " @ " + context);
                result.ErrorCount++;
                return false;
            }

            if (hits > 1)
            {
                Debug.LogError("表情重名 " + name + " @ " + context);
                result.ErrorCount++;
                return false;
            }

            sprite = found;
            return true;
        }

        static Sprite FindPortraitSprite(string name)
        {
            for (int i = 0; i < PortraitFolders.Length; i++)
            {
                Sprite loaded = AssetDatabase.LoadAssetAtPath<Sprite>(PortraitFolders[i] + "/" + name + ".png");
                if (loaded != null)
                    return loaded;
            }

            return null;
        }

        static string PortraitOn(NightEventDefinition asset, bool intro, string optionId)
        {
            if (asset == null)
                return "";

            if (intro)
                return asset.introPortrait != null ? asset.introPortrait.name : "";

            NightOption option = FindOption(asset, optionId);
            if (option.successPortrait == null)
                return "";

            return option.successPortrait.name;
        }

        static NightEventDefinition FindOrCreateEvent(string id, ImportResult result)
        {
            NightEventDefinition existing = FindEvent(id);
            if (existing != null)
            {
                result.Updated++;
                return existing;
            }

            NightEventDefinition created = ScriptableObject.CreateInstance<NightEventDefinition>();
            AssetDatabase.CreateAsset(created, EventsFolder + "/" + id + ".asset");
            result.Created++;
            return created;
        }

        static NightEventDefinition FindEvent(string id)
        {
            string[] guids = AssetDatabase.FindAssets("t:NightEventDefinition", new[] { EventsFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                NightEventDefinition asset = AssetDatabase.LoadAssetAtPath<NightEventDefinition>(path);
                if (asset != null && (asset.id == id || Path.GetFileNameWithoutExtension(path) == id))
                    return asset;
            }

            return null;
        }

        static NightOption FindOption(NightEventDefinition asset, string optionId)
        {
            if (asset == null || asset.options == null)
                return default;

            for (int i = 0; i < asset.options.Count; i++)
            {
                if (asset.options[i].optionId == optionId)
                    return asset.options[i];
            }

            return default;
        }

        static IngredientDefinition FindIngredient(string id)
        {
            string[] guids = AssetDatabase.FindAssets("t:IngredientDefinition", new[] { IngredientsFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                IngredientDefinition asset = AssetDatabase.LoadAssetAtPath<IngredientDefinition>(
                    AssetDatabase.GUIDToAssetPath(guids[i]));
                if (asset != null && asset.id == id)
                    return asset;
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

        public sealed class ImportResult
        {
            public int Created;
            public int Updated;
            public int ErrorCount;
        }
    }
}
