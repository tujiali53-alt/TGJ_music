using UnityEngine;

namespace Cooking.Data
{
    [CreateAssetMenu(fileName = "Seasoning", menuName = "Cooking/Seasoning")]
    public sealed class SeasoningDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite icon;
    }
}
