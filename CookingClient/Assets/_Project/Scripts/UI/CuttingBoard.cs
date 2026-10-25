using Cooking.Data;
using Cooking.Runtime;
using UnityEngine;

namespace Cooking.UI
{
    public sealed class CuttingBoard : MonoBehaviour
    {
        [SerializeField] IngredientDefinition[] definitions;

        public bool TryPlace(IngredientInstance item)
        {
            if (item == null)
                return false;

            IngredientDefinition definition = FindDefinition(item.DefinitionId);
            if (definition == null || !definition.canCut)
                return false;

            if (item.Shape == CutShape.Whole)
                item.Shape = CutShape.Slice;

            return true;
        }

        IngredientDefinition FindDefinition(string definitionId)
        {
            if (definitions == null)
                return null;

            for (int i = 0; i < definitions.Length; i++)
            {
                IngredientDefinition definition = definitions[i];
                if (definition != null && definition.id == definitionId)
                    return definition;
            }

            return null;
        }
    }
}
