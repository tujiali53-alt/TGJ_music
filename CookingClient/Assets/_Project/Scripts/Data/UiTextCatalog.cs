using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cooking.Data
{
    [Serializable]
    public struct UiTextString
    {
        public string id;
        public string textZh;
        public string textEn;
    }

    [CreateAssetMenu(fileName = "UiTextCatalog", menuName = "Cooking/Ui Text Catalog")]
    public sealed class UiTextCatalog : ScriptableObject
    {
        public List<UiTextString> rows = new List<UiTextString>();

        public string Pick(string id, DiaryLanguage language)
        {
            if (rows == null || string.IsNullOrEmpty(id))
                return "";

            for (int i = 0; i < rows.Count; i++)
            {
                UiTextString row = rows[i];
                if (row.id != id)
                    continue;

                return NightEventDefinition.Pick(language, row.textZh, row.textEn);
            }

            return "";
        }
    }
}
