using System;
using Cooking.Data;
using UnityEngine;

namespace Cooking.Core
{
    public static class GameLanguage
    {
        const string PrefsKey = "cooking.language";

        static bool loaded;
        static DiaryLanguage current = DiaryLanguage.En;

        public static event Action Changed;

        public static DiaryLanguage Current
        {
            get
            {
                EnsureLoaded();
                return current;
            }
        }

        public static void Set(DiaryLanguage language)
        {
            EnsureLoaded();
            if (current == language)
                return;

            current = language;
            PlayerPrefs.SetInt(PrefsKey, (int)language);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        static void EnsureLoaded()
        {
            if (loaded)
                return;

            loaded = true;
            if (PlayerPrefs.HasKey(PrefsKey))
                current = (DiaryLanguage)PlayerPrefs.GetInt(PrefsKey, (int)DiaryLanguage.En);
            else
                current = DiaryLanguage.En;
        }
    }
}
