using System.Collections.Generic;
using System.Text;
using Cooking.Data;

namespace Cooking.Runtime
{
    public static class DiaryBook
    {
        public static int Moment(int dayIndex, DayPhase phase)
        {
            int phaseRank = phase == DayPhase.Day ? 0 : 1;
            return dayIndex * 2 + phaseRank;
        }

        public static bool IsUnlocked(DiaryEntry entry, GameSession session)
        {
            if (entry == null || session == null)
                return false;

            // 第 7 天白天起，第 6 天「现在的日记」不依赖做对食谱。
            if (IsDay6PresentFreeOnFinalDay(entry, session))
                return true;

            if (string.IsNullOrEmpty(entry.recipeId))
                return false;

            if (!Contains(session.CorrectRecipeIds, entry.recipeId))
                return false;

            return Moment(session.DayIndex, session.Phase) >= Moment(entry.unlockDay, entry.unlockPhase);
        }

        static bool IsDay6PresentFreeOnFinalDay(DiaryEntry entry, GameSession session)
        {
            if (entry.storyDay != 6)
                return false;
            if (entry.kind != DiaryKind.Present && entry.kind != DiaryKind.PresentFallback)
                return false;

            return Moment(session.DayIndex, session.Phase) >= Moment(7, DayPhase.Day);
        }

        public static List<DiaryEntry> Visible(DiaryCatalog catalog, GameSession session)
        {
            var pages = new List<DiaryEntry>();
            if (catalog == null || catalog.entries == null || session == null)
                return pages;

            for (int i = 0; i < catalog.entries.Count; i++)
            {
                DiaryEntry entry = catalog.entries[i];
                if (entry == null || !entry.inBook || !IsUnlocked(entry, session))
                    continue;

                // 第 6 天「现在的日记」由 present / fallback 二选一，不在这里直接加入。
                if (entry.storyDay == 6 && entry.kind == DiaryKind.Present)
                    continue;

                pages.Add(entry);
            }

            DiaryEntry day6 = ResolveDay6Present(catalog, session);
            if (day6 != null)
                pages.Add(day6);

            pages.Sort(CompareOrder);
            return pages;
        }

        // 第 1–5 天进书日记全解锁 → diary_day6_present；否则 → diary_day6_fallback。
        static DiaryEntry ResolveDay6Present(DiaryCatalog catalog, GameSession session)
        {
            DiaryEntry present = FindKind(catalog, 6, DiaryKind.Present);
            DiaryEntry fallback = FindKind(catalog, 6, DiaryKind.PresentFallback);
            bool presentReady = present != null && IsUnlocked(present, session);
            bool fallbackReady = fallback != null && IsUnlocked(fallback, session);
            if (!presentReady && !fallbackReady)
                return null;

            if (AreStoryDaysUnlocked(catalog, session, 1, 5))
                return presentReady ? present : null;

            return fallbackReady ? fallback : null;
        }

        public static bool AreStoryDaysUnlocked(
            DiaryCatalog catalog,
            GameSession session,
            int fromDay,
            int toDay)
        {
            if (catalog == null || catalog.entries == null || session == null)
                return false;

            int needed = 0;
            for (int i = 0; i < catalog.entries.Count; i++)
            {
                DiaryEntry entry = catalog.entries[i];
                if (entry == null || !entry.inBook)
                    continue;
                if (entry.storyDay < fromDay || entry.storyDay > toDay)
                    continue;

                needed++;
                if (!IsUnlocked(entry, session))
                    return false;
            }

            return needed > 0;
        }

        static DiaryEntry FindKind(DiaryCatalog catalog, int storyDay, DiaryKind kind)
        {
            if (catalog == null || catalog.entries == null)
                return null;

            for (int i = 0; i < catalog.entries.Count; i++)
            {
                DiaryEntry entry = catalog.entries[i];
                if (entry != null && entry.storyDay == storyDay && entry.kind == kind)
                    return entry;
            }

            return null;
        }

        public static bool HasUnread(IList<DiaryEntry> visible, IList<string> seenIds)
        {
            if (visible == null)
                return false;

            for (int i = 0; i < visible.Count; i++)
            {
                DiaryEntry entry = visible[i];
                if (entry == null || string.IsNullOrEmpty(entry.id))
                    continue;

                if (!Contains(seenIds, entry.id))
                    return true;
            }

            return false;
        }

        public static int FirstUnreadIndex(IList<DiaryEntry> visible, IList<string> seenIds)
        {
            if (visible == null || visible.Count == 0)
                return 0;

            for (int i = 0; i < visible.Count; i++)
            {
                DiaryEntry entry = visible[i];
                if (entry == null || string.IsNullOrEmpty(entry.id))
                    continue;

                if (!Contains(seenIds, entry.id))
                    return i;
            }

            return visible.Count - 1;
        }

        public static string Join(DiaryEntry entry, DiarySide side, DiaryLanguage language)
        {
            if (entry == null || entry.paragraphs == null)
                return "";

            var lines = new List<DiaryParagraph>();
            for (int i = 0; i < entry.paragraphs.Count; i++)
            {
                DiaryParagraph paragraph = entry.paragraphs[i];
                if (paragraph.side == side)
                    lines.Add(paragraph);
            }

            lines.Sort(CompareParagraph);
            var builder = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                if (i > 0)
                    builder.Append('\n');

                builder.Append(TextOf(lines[i], language));
            }

            return builder.ToString();
        }

        public static string DayLine(DiaryCatalog catalog, int storyDay, DiaryLanguage language)
        {
            string format = Lookup(catalog, "day_format", language);
            if (string.IsNullOrEmpty(format))
                format = "Day {0}";

            return string.Format(format, storyDay);
        }

        public static string Title(DiaryCatalog catalog, DiaryEntry entry, DiaryLanguage language)
        {
            if (entry == null || string.IsNullOrEmpty(entry.titleId))
                return "";

            return Lookup(catalog, entry.titleId, language);
        }

        public static int CountSide(DiaryEntry entry, DiarySide side)
        {
            if (entry == null || entry.paragraphs == null)
                return 0;

            int count = 0;
            for (int i = 0; i < entry.paragraphs.Count; i++)
            {
                if (entry.paragraphs[i].side == side)
                    count++;
            }

            return count;
        }

        static string Lookup(DiaryCatalog catalog, string id, DiaryLanguage language)
        {
            if (catalog == null || catalog.strings == null || string.IsNullOrEmpty(id))
                return "";

            for (int i = 0; i < catalog.strings.Count; i++)
            {
                DiaryStringRow row = catalog.strings[i];
                if (row.id != id)
                    continue;

                string text = language == DiaryLanguage.En ? row.textEn : row.textZh;
                return text ?? "";
            }

            return "";
        }

        static string TextOf(DiaryParagraph paragraph, DiaryLanguage language)
        {
            string text = language == DiaryLanguage.En ? paragraph.textEn : paragraph.textZh;
            return text ?? "";
        }

        static bool Contains(IList<string> ids, string id)
        {
            if (ids == null || string.IsNullOrEmpty(id))
                return false;

            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] == id)
                    return true;
            }

            return false;
        }

        static int CompareOrder(DiaryEntry a, DiaryEntry b)
        {
            return a.sortOrder.CompareTo(b.sortOrder);
        }

        static int CompareParagraph(DiaryParagraph a, DiaryParagraph b)
        {
            return a.order.CompareTo(b.order);
        }
    }
}
