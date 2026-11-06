using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cooking.Data
{
    [Serializable]
    public sealed class EndingLine
    {
        public string speaker;
        public string textZh;
        public string textEn;
        public string expressionId;
        public Sprite expression;
    }

    [Serializable]
    public sealed class EndingDefinition
    {
        public string id;
        public int priority;
        public bool requireAllInBookDiaries;
        public bool forbidAllInBookDiaries;
        public bool requireAllNpcProps;
        public bool forbidAllNpcProps;
        public Sprite cg;
        public string panelTextZh;
        public string panelTextEn;
        public string clickTextZh;
        public string clickTextEn;
        public List<EndingLine> lines = new List<EndingLine>();
    }

    [CreateAssetMenu(fileName = "EndingCatalog", menuName = "Cooking/Ending Catalog")]
    public sealed class EndingCatalog : ScriptableObject
    {
        public List<EndingLine> commonLines = new List<EndingLine>();
        public List<EndingDefinition> endings = new List<EndingDefinition>();

        public static string Pick(DiaryLanguage language, string zh, string en)
        {
            if (language == DiaryLanguage.En && !string.IsNullOrWhiteSpace(en))
                return en;

            return zh ?? "";
        }

        public EndingDefinition Find(string endingId)
        {
            if (endings == null || string.IsNullOrEmpty(endingId))
                return null;

            for (int i = 0; i < endings.Count; i++)
            {
                EndingDefinition ending = endings[i];
                if (ending != null && ending.id == endingId)
                    return ending;
            }

            return null;
        }
    }
}
