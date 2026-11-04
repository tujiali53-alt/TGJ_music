using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cooking.Data
{
    public enum NpcRewardKind
    {
        Ingredient = 0,
        Prop = 1
    }

    [Serializable]
    public sealed class NpcReward
    {
        public NpcRewardKind kind;
        public string rewardId;
        public int count;
    }

    [Serializable]
    public sealed class NpcOption
    {
        public string optionId;
        public int order;
        public string displayNameZh;
        public string displayNameEn;
        public List<NpcReward> rewards = new List<NpcReward>();
    }

    [Serializable]
    public sealed class NpcLine
    {
        public string optionId;
        public int order;
        public string textZh;
        public string textEn;
    }

    [Serializable]
    public sealed class NpcCharacter
    {
        public string id;
        public string displayNameZh;
        public string displayNameEn;
        public string eventNameZh;
        public string eventNameEn;
        public Sprite portrait;
        public int unlockDay;
        public DayPhase phase;
        public List<NpcOption> options = new List<NpcOption>();
        public List<NpcLine> lines = new List<NpcLine>();
    }

    [Serializable]
    public sealed class NpcProp
    {
        public string id;
        public string displayNameZh;
        public string displayNameEn;
        public string descriptionZh;
        public string descriptionEn;
        public Sprite sprite;
    }

    [CreateAssetMenu(fileName = "NpcCatalog", menuName = "Cooking/Npc Catalog")]
    public sealed class NpcCatalog : ScriptableObject
    {
        public List<NpcCharacter> characters = new List<NpcCharacter>();
        public List<NpcProp> props = new List<NpcProp>();

        public static string Pick(DiaryLanguage language, string zh, string en)
        {
            if (language == DiaryLanguage.En && !string.IsNullOrEmpty(en))
                return en;

            return zh ?? "";
        }

        public NpcCharacter FindCharacter(string npcId)
        {
            if (characters == null || string.IsNullOrEmpty(npcId))
                return null;

            for (int i = 0; i < characters.Count; i++)
            {
                NpcCharacter character = characters[i];
                if (character != null && character.id == npcId)
                    return character;
            }

            return null;
        }

        public NpcProp FindProp(string propId)
        {
            if (props == null || string.IsNullOrEmpty(propId))
                return null;

            for (int i = 0; i < props.Count; i++)
            {
                NpcProp prop = props[i];
                if (prop != null && prop.id == propId)
                    return prop;
            }

            return null;
        }
    }
}
