using Cooking.Core;
using Cooking.Data;
using Cooking.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cooking.UI
{
    public sealed class FoodSlotView : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] TMP_Text countText;
        [SerializeField] IngredientDefinition[] definitions;
        [SerializeField] FoodSlotDrag drag;

        void Awake()
        {
            if (icon == null)
            {
                Transform found = transform.Find("Image");
                if (found != null)
                    icon = found.GetComponent<Image>();
            }

            if (countText == null)
            {
                Transform found = transform.Find("CountText");
                if (found != null)
                    countText = found.GetComponent<TMP_Text>();
            }

            if (drag == null)
                drag = GetComponent<FoodSlotDrag>();
        }

        void OnEnable()
        {
            Refresh();
        }

        void LateUpdate()
        {
            Refresh();
        }

        void Refresh()
        {
            if (drag != null && drag.IsDragging)
                return;

            IngredientInstance item = FirstItem();
            if (item == null || item.Quantity <= 0)
                return;

            IngredientDefinition definition = FindDefinition(item.DefinitionId);
            if (icon != null && definition != null && definition.spriteWhole != null)
                icon.sprite = definition.spriteWhole;

            if (countText != null)
                countText.text = Describe(item);
        }

        static string Describe(IngredientInstance item)
        {
            string text = "x" + item.Quantity + ShapeLabel(item.Shape);
            if (item.Doneness <= 0f && item.CookMethod == CookMethod.Raw)
                return text;

            text += "\n";
            if (item.Doneness > 0f)
                text += "熟" + Mathf.RoundToInt(item.Doneness);

            text += MethodLabel(item.CookMethod);
            return text;
        }

        static string ShapeLabel(CutShape shape)
        {
            switch (shape)
            {
                case CutShape.Dice:
                    return " 切丁";
                case CutShape.Slice:
                    return " 切片";
                case CutShape.Shred:
                    return " 切丝";
                default:
                    return "";
            }
        }

        static string MethodLabel(CookMethod method)
        {
            switch (method)
            {
                case CookMethod.Boiled:
                    return " 煮";
                case CookMethod.Fried:
                    return " 煎";
                case CookMethod.BoiledAndFried:
                    return " 煮煎";
                default:
                    return "";
            }
        }

        IngredientInstance FirstItem()
        {
            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null || root.Session.Inventory == null)
                return null;

            for (int i = 0; i < root.Session.Inventory.Count; i++)
            {
                IngredientInstance item = root.Session.Inventory[i];
                if (item != null && item.Quantity > 0)
                    return item;
            }

            return null;
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
