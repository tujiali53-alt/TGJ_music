using System;
using System.Collections.Generic;
using UnityEngine;

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
        public string text;
    }

    [Serializable]
    public struct NightOption
    {
        public string optionId;
        public int order;
        public string displayName;
        public NightConditionType conditionType;
        public float conditionValue;
        public int successFaces;
        public List<NightReward> rewards;
    }

    [CreateAssetMenu(fileName = "NightEvent", menuName = "Cooking/Night Event")]
    public sealed class NightEventDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public int unlockNight;
        public List<NightOption> options;
        public List<NightLine> lines;
    }
}
