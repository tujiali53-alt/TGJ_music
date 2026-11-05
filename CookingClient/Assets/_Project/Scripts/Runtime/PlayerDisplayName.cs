using Cooking.Core;
using Cooking.Data;

namespace Cooking.Runtime
{
    public static class PlayerDisplayName
    {
        public const string DefaultZh = "小鼻嘎";
        public const string DefaultEn = "Lil Tu";

        public static string Resolve(GameSession session)
        {
            if (session != null && !string.IsNullOrWhiteSpace(session.PlayerName))
                return session.PlayerName.Trim();

            return GameLanguage.Current == DiaryLanguage.En ? DefaultEn : DefaultZh;
        }
    }
}
