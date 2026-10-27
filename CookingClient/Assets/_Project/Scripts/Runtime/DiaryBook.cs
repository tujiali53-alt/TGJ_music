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
            if (entry == null || session == null || string.IsNullOrEmpty(entry.recipeId))
                return false;

            if (!Contains(session.CorrectRecipeIds, entry.recipeId))
                return false;

            return Moment(session.DayIndex, session.Phase) >= Moment(entry.unlockDay, entry.unlockPhase);
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

                pages.Add(entry);
            }

            pages.Sort(CompareOrder);
            return pages;
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
