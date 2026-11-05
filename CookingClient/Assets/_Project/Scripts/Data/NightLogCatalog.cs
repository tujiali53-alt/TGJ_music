using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Cooking.Data
{
    [Serializable]
    public struct NightLogString
    {
        public string id;
        public string textZh;
        public string textEn;
        public string color;
    }

    [CreateAssetMenu(fileName = "NightLogCatalog", menuName = "Cooking/Night Log Catalog")]
    public sealed class NightLogCatalog : ScriptableObject
    {
        public List<NightLogString> rows = new List<NightLogString>();

        public bool TryGet(string id, out NightLogString row)
        {
            row = default;
            if (rows == null || string.IsNullOrEmpty(id))
                return false;

            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].id != id)
                    continue;

                row = rows[i];
                return true;
            }

            return false;
        }

        public string Format(string id, DiaryLanguage language, string[] args)
        {
            if (!TryGet(id, out NightLogString row))
                return id ?? "";

            string template = NightEventDefinition.Pick(language, row.textZh, row.textEn);
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

        public string ColorHex(string id)
        {
            if (!TryGet(id, out NightLogString row))
                return "";

            return row.color ?? "";
        }

        public static string WrapColor(string text, string colorHex)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(colorHex))
                return text ?? "";

            string hex = colorHex.Trim();
            if (hex.StartsWith("#", StringComparison.Ordinal))
                hex = hex.Substring(1);
            if (hex.Length != 6 && hex.Length != 8)
                return text;

            return "<color=#" + hex + ">" + text + "</color>";
        }
    }
}
