using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Cooking.Data
{
    [Serializable]
    public struct ScoreCommentString
    {
        public string id;
        public string textZh;
        public string textEn;
    }

    [CreateAssetMenu(fileName = "ScoreCommentCatalog", menuName = "Cooking/Score Comment Catalog")]
    public sealed class ScoreCommentCatalog : ScriptableObject
    {
        public List<ScoreCommentString> rows = new List<ScoreCommentString>();

        public string Format(string id, DiaryLanguage language, params string[] args)
        {
            string template = Template(id, language);
            if (string.IsNullOrEmpty(template))
                return "";

            if (args == null || args.Length == 0)
                return template;

            try
            {
                return string.Format(CultureInfo.InvariantCulture, template, args);
            }
            catch (FormatException)
            {
                return template;
            }
        }

        public string Template(string id, DiaryLanguage language)
        {
            if (rows == null || string.IsNullOrEmpty(id))
                return "";

            for (int i = 0; i < rows.Count; i++)
            {
                ScoreCommentString row = rows[i];
                if (row.id != id)
                    continue;

                return NightEventDefinition.Pick(language, row.textZh, row.textEn);
            }

            return "";
        }

        public string NameJoin(DiaryLanguage language)
        {
            string text = Template("comment.name_join", language);
            if (!string.IsNullOrEmpty(text))
                return text;

            return language == DiaryLanguage.En ? ", " : "、";
        }
    }
}
