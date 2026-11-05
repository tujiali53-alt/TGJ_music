using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Cooking.Data
{
    [Serializable]
    public struct NightReward
    {
        public NightRewardPick pickMode;
        public string ingredientId;
        public int count;
    }

    [Serializable]
    public struct NightLine
    {
        public string optionId;
        public NightLinePhase phase;
        public int order;
        [FormerlySerializedAs("text")]
        public string textZh;
        public string textEn;
    }

    [Serializable]
    public struct NightOption
    {
        public string optionId;
        public int order;
        [FormerlySerializedAs("displayName")]
        public string displayNameZh;
        public string displayNameEn;
        public NightConditionType conditionType;
        public float conditionValue;
        public int successFaces;
        public List<NightReward> rewards;
        public Sprite successPortrait;
        public Sprite failPortrait;
    }

    [CreateAssetMenu(fileName = "NightEvent", menuName = "Cooking/Night Event")]
    public sealed class NightEventDefinition : ScriptableObject
    {
        public string id;
        [FormerlySerializedAs("displayName")]
        public string displayNameZh;
        public string displayNameEn;
        public int unlockNight;
        public Sprite introPortrait;
        public List<NightOption> options;
        public List<NightLine> lines;

        public static string Pick(DiaryLanguage language, string zh, string en)
        {
            if (language == DiaryLanguage.En && !string.IsNullOrEmpty(en))
                return en;

            return zh ?? "";
        }

        public string DisplayName(DiaryLanguage language)
        {
            return Pick(language, displayNameZh, displayNameEn);
        }
    }
}
