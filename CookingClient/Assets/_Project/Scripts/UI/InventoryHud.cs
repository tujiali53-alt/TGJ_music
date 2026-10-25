using System.Text;
using Cooking.Core;
using Cooking.Data;
using Cooking.Runtime;
using TMPro;
using UnityEngine;

namespace Cooking.UI
{
    public sealed class InventoryHud : MonoBehaviour
    {
        [SerializeField] TMP_Text label;
        [SerializeField] IngredientDefinition[] definitions;

        void Awake()
        {
            if (label == null)
                label = GetComponent<TMP_Text>();
        }

        void LateUpdate()
        {
            if (label == null)
                return;

            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null || root.Session.Inventory == null || root.Session.Inventory.Count == 0)
            {
                label.text = "";
                return;
            }

            var builder = new StringBuilder();
            for (int i = 0; i < root.Session.Inventory.Count; i++)
            {
                IngredientInstance item = root.Session.Inventory[i];
                if (builder.Length > 0)
                    builder.Append('\n');

                builder.Append(NameOf(item.DefinitionId));
                builder.Append(" x");
                builder.Append(item.Quantity);
            }

            label.text = builder.ToString();
        }

        string NameOf(string definitionId)
        {
            if (definitions != null)
            {
                for (int i = 0; i < definitions.Length; i++)
                {
                    IngredientDefinition definition = definitions[i];
                    if (definition != null && definition.id == definitionId)
                        return definition.displayName;
                }
            }

            return definitionId;
        }
    }
}
