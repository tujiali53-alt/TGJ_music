using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cooking.Data
{
    [Serializable]
    public struct IngredientVisualState
    {
        public CutShape shape;
        public CookMethod cookMethod;
        public Sprite sprite;
    }

    [CreateAssetMenu(fileName = "Ingredient", menuName = "Cooking/Ingredient")]
    public sealed class IngredientDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public string displayNameEn;
        public string description;
        public string descriptionEn;
        public IngredientKind kind;
        public bool canEatRaw;
        public bool canCut;
        public float optimalDoneness = 100f;
        public float maxDoneness = 140f;
        public Sprite spriteWhole;
        public Color baseColor = Color.white;
        public List<IngredientVisualState> stateSprites = new List<IngredientVisualState>();
    }
}
