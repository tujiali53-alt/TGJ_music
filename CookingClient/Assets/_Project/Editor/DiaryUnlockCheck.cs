using System.Collections.Generic;
using Cooking.Data;
using Cooking.Runtime;
using UnityEditor;
using UnityEngine;

namespace Cooking.EditorTools
{
    public static class DiaryUnlockCheck
    {
        const string CatalogAssetPath = "Assets/_Project/Data/Diary/DiaryCatalog.asset";

        [MenuItem("Cooking/Check Diary Unlock")]
        public static void RunMenu()
        {
            string error = Run();
            if (error == null)
                Debug.Log("日记解锁通过：第 1 夜只有过去的日记，第 2 天白天两页未读，已读后红点消失。");
            else
                Debug.LogError(error);
        }

        public static string Run()
        {
            string rules = CheckRules();
            if (rules != null)
                return rules;

            return CheckImported();
        }

        public static string CheckRules()
        {
            if (DiaryBook.Moment(1, DayPhase.Day) != 2)
                return "第 1 天白天的时刻应为 2。";
            if (DiaryBook.Moment(1, DayPhase.Night) != 3)
                return "第 1 天夜晚的时刻应为 3。";
            if (DiaryBook.Moment(2, DayPhase.Day) != 4)
                return "第 2 天白天的时刻应为 4。";

            DiaryCatalog catalog = SampleCatalog();
            try
            {
            GameSession session = GameSession.NewGame();
            session.DayIndex = 2;
            session.Phase = DayPhase.Day;
            if (DiaryBook.Visible(catalog, session).Count != 0)
                return "没做对食谱时，第 2 天白天不应有日记。";
            if (DiaryBook.HasUnread(DiaryBook.Visible(catalog, session), session.DiarySeenIds))
                return "没有日记时不应算未读。";

            session.RememberCorrectRecipe("recipe_day1");
            session.DayIndex = 1;
            session.Phase = DayPhase.Day;
            if (DiaryBook.Visible(catalog, session).Count != 0)
                return "第 1 天白天还不应打开过去的日记。";

            session.Phase = DayPhase.Night;
            List<DiaryEntry> night = DiaryBook.Visible(catalog, session);
            if (night.Count != 1 || night[0].id != "diary_day1_past")
                return "第 1 夜应只有 diary_day1_past。";

            session.DayIndex = 2;
            session.Phase = DayPhase.Day;
            List<DiaryEntry> morning = DiaryBook.Visible(catalog, session);
            if (morning.Count != 2 || morning[0].id != "diary_day1_past" || morning[1].id != "diary_day1_present")
                return "第 2 天白天应是过去的日记和现在的日记。";
            if (!DiaryBook.HasUnread(morning, session.DiarySeenIds))
                return "刚解锁的两页应为未读。";

            session.RememberDiarySeen(morning);
            if (DiaryBook.HasUnread(morning, session.DiarySeenIds))
                return "打开日记后这两页不应再算未读。";

            session.Phase = DayPhase.Night;
            List<DiaryEntry> still = DiaryBook.Visible(catalog, session);
            if (still.Count != 2)
                return "第 2 夜第 1 天的两页应还在。";

            session.DayIndex = 3;
            session.Phase = DayPhase.Day;
            List<DiaryEntry> third = DiaryBook.Visible(catalog, session);
            if (third.Count != 2 || third[0].id != "diary_day1_past" || third[1].id != "diary_day1_present")
                return "没做对第 2 天时，第 3 天白天仍应只有第 1 天的两页。";

            return null;
            }
            finally
            {
                if (catalog != null)
                    Object.DestroyImmediate(catalog);
            }
        }

        public static string CheckImported()
        {
            DiaryCatalog catalog = AssetDatabase.LoadAssetAtPath<DiaryCatalog>(CatalogAssetPath);
            if (catalog == null || catalog.entries == null)
                return "缺少 DiaryCatalog，请先执行 Cooking/Import Diary Tables。";

            if (catalog.entries.Count != 15)
                return "日记应为 15 条，实际 " + catalog.entries.Count + "。";

            int inBook = 0;
            for (int i = 0; i < catalog.entries.Count; i++)
            {
                if (catalog.entries[i] != null && catalog.entries[i].inBook)
                    inBook++;
            }

            if (inBook != 12)
                return "进书的日记应为 12 条，实际 " + inBook + "。";

            string split = CheckSplit(catalog, "diary_day1_past", 5, 5);
            if (split != null)
                return split;
            split = CheckSplit(catalog, "diary_day1_present", 5, 4);
            if (split != null)
                return split;
            split = CheckSplit(catalog, "diary_day2_past", 7, 7);
            if (split != null)
                return split;
            split = CheckSplit(catalog, "diary_day6_present", 12, 12);
            if (split != null)
                return split;
            split = CheckSplit(catalog, "diary_day6_fallback", 5, 4);
            if (split != null)
                return split;

            DiaryEntry past = Find(catalog, "diary_day1_past");
            if (past == null || past.paragraphs == null || past.paragraphs.Count == 0)
                return "缺少 diary_day1_past 的段落。";
            if (past.paragraphs[0].textZh != "世界将在新年迎来终结。")
                return "diary_day1_past 首段中文不符。";
            if (past.paragraphs[0].textEn != "The world will meet its end on New Year's Day.")
                return "diary_day1_past 首段英文不符。";

            DiaryEntry hidden = Find(catalog, "diary_day7_ending_good");
            if (hidden == null || hidden.inBook)
                return "结局页应在表里，并且 inBook 为 0。";

            DiaryEntry fallback = Find(catalog, "diary_day6_fallback");
            if (fallback == null || fallback.inBook)
                return "第 6 天替页应在表里，并且 inBook 为 0。";

            return null;
        }

        static string CheckSplit(DiaryCatalog catalog, string id, int left, int right)
        {
            DiaryEntry entry = Find(catalog, id);
            if (entry == null)
                return "缺少 " + id + "。";

            int actualLeft = DiaryBook.CountSide(entry, DiarySide.Left);
            int actualRight = DiaryBook.CountSide(entry, DiarySide.Right);
            if (actualLeft != left || actualRight != right)
                return id + " 左右段数应为 " + left + "/" + right + "，实际 " + actualLeft + "/" + actualRight + "。";

            return null;
        }

        static DiaryEntry Find(DiaryCatalog catalog, string id)
        {
            for (int i = 0; i < catalog.entries.Count; i++)
            {
                DiaryEntry entry = catalog.entries[i];
                if (entry != null && entry.id == id)
                    return entry;
            }

            return null;
        }

        static DiaryCatalog SampleCatalog()
        {
            DiaryCatalog catalog = ScriptableObject.CreateInstance<DiaryCatalog>();
            catalog.entries = new List<DiaryEntry>
            {
                Page("diary_day1_past", "recipe_day1", 1, DiaryKind.Past, 1, DayPhase.Night, 1, true),
                Page("diary_day1_present", "recipe_day1", 1, DiaryKind.Present, 2, DayPhase.Day, 2, true),
                Page("diary_day2_past", "recipe_day2", 2, DiaryKind.Past, 2, DayPhase.Night, 3, true),
                Page("diary_day6_fallback", "recipe_day1", 6, DiaryKind.PresentFallback, 2, DayPhase.Day, 13, false)
            };
            return catalog;
        }

        static DiaryEntry Page(
            string id,
            string recipeId,
            int storyDay,
            DiaryKind kind,
            int unlockDay,
            DayPhase phase,
            int sortOrder,
            bool inBook)
        {
            return new DiaryEntry
            {
                id = id,
                recipeId = recipeId,
                storyDay = storyDay,
                kind = kind,
                unlockDay = unlockDay,
                unlockPhase = phase,
                sortOrder = sortOrder,
                inBook = inBook,
                paragraphs = new List<DiaryParagraph>()
            };
        }
    }
}
