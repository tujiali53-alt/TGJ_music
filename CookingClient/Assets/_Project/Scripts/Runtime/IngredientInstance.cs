using System.Collections.Generic;
using Cooking.Data;

namespace Cooking.Runtime
{
    public sealed class IngredientInstance
    {
        public int InstanceId;
        public string DefinitionId;
        public QualityTier Quality;
        public int Quantity = 1;
        public float Doneness;
        public CutShape Shape;
        public CookMethod CookMethod;
        public List<string> SeasoningIds = new List<string>();
        public bool IsCharcoal;
    }
}
