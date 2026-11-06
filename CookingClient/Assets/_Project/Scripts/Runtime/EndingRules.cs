using System.Collections.Generic;
using Cooking.Data;

namespace Cooking.Runtime
{
    public static class EndingRules
    {
        public static bool AreAllInBookDiariesUnlocked(GameSession session, DiaryCatalog diaryCatalog)
        {
            if (session == null || diaryCatalog == null || diaryCatalog.entries == null)
                return false;

            int inBook = 0;
            for (int i = 0; i < diaryCatalog.entries.Count; i++)
            {
                DiaryEntry entry = diaryCatalog.entries[i];
                if (entry == null || !entry.inBook)
                    continue;

                inBook++;
                if (!DiaryBook.IsUnlocked(entry, session))
                    return false;
            }

            return inBook > 0;
        }

        public static bool HasAllNpcProps(GameSession session, NpcCatalog npcCatalog)
        {
            if (session == null || npcCatalog == null || npcCatalog.props == null || npcCatalog.props.Count == 0)
                return false;

            for (int i = 0; i < npcCatalog.props.Count; i++)
            {
                NpcProp prop = npcCatalog.props[i];
                if (prop == null || string.IsNullOrEmpty(prop.id))
                    continue;

                if (CountProp(session.Props, prop.id) < 1)
                    return false;
            }

            return true;
        }

        public static EndingDefinition Resolve(
            GameSession session,
            EndingCatalog endingCatalog,
            DiaryCatalog diaryCatalog,
            NpcCatalog npcCatalog)
        {
            if (endingCatalog == null || endingCatalog.endings == null || endingCatalog.endings.Count == 0)
                return null;

            bool allDiaries = AreAllInBookDiariesUnlocked(session, diaryCatalog);
            bool allProps = HasAllNpcProps(session, npcCatalog);

            var ranked = new List<EndingDefinition>();
            for (int i = 0; i < endingCatalog.endings.Count; i++)
            {
                EndingDefinition ending = endingCatalog.endings[i];
                if (ending == null || string.IsNullOrEmpty(ending.id))
                    continue;

                ranked.Add(ending);
            }

            ranked.Sort(ComparePriority);

            for (int i = 0; i < ranked.Count; i++)
            {
                if (Matches(ranked[i], allDiaries, allProps))
                    return ranked[i];
            }

            return null;
        }

        static bool Matches(EndingDefinition ending, bool allDiaries, bool allProps)
        {
            if (ending.requireAllInBookDiaries && !allDiaries)
                return false;

            if (ending.forbidAllInBookDiaries && allDiaries)
                return false;

            if (ending.requireAllNpcProps && !allProps)
                return false;

            if (ending.forbidAllNpcProps && allProps)
                return false;

            return true;
        }

        static int CountProp(IList<PropStack> props, string propId)
        {
            if (props == null || string.IsNullOrEmpty(propId))
                return 0;

            for (int i = 0; i < props.Count; i++)
            {
                PropStack stack = props[i];
                if (stack != null && stack.PropId == propId)
                    return stack.Quantity;
            }

            return 0;
        }

        static int ComparePriority(EndingDefinition a, EndingDefinition b)
        {
            int byPriority = b.priority.CompareTo(a.priority);
            if (byPriority != 0)
                return byPriority;

            return string.CompareOrdinal(a.id, b.id);
        }
    }
}
