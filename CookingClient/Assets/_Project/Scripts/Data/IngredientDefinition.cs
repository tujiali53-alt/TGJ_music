using UnityEngine;

namespace Cooking.Data
{
    [CreateAssetMenu(fileName = "Ingredient", menuName = "Cooking/Ingredient")]
    public sealed class IngredientDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public IngredientKind kind;
        public bool canEatRaw;
        public bool canCut;
        public float optimalDoneness = 100f;
        public float maxDoneness = 140f;
        public Sprite spriteWhole;
        public Color baseColor = Color.white;
    }
}
