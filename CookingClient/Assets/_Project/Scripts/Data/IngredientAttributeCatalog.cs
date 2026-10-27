using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cooking.Data
{
    [Serializable]
    public struct IngredientAttributeString
    {
        public string id;
        public string textZh;
        public string textEn;
    }

    [CreateAssetMenu(fileName = "IngredientAttributeCatalog", menuName = "Cooking/Ingredient Attribute Catalog")]
    public sealed class IngredientAttributeCatalog : ScriptableObject
    {
        public List<IngredientAttributeString> rows = new List<IngredientAttributeString>();
    }
}
