using UnityEngine;

namespace Cooking.Data
{
    [CreateAssetMenu(fileName = "Seasoning", menuName = "Cooking/Seasoning")]
    public sealed class SeasoningDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public string displayNameEn;
        public Sprite icon;

        public string DisplayName(DiaryLanguage language)
        {
            return NightEventDefinition.Pick(language, displayName, displayNameEn);
        }
    }
}
