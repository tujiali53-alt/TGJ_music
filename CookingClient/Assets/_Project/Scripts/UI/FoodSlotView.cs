using System.Globalization;
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
        [SerializeField] TMP_Text statusText;
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

            if (statusText == null)
            {
                Transform found = transform.Find("StatusText");
                if (found != null)
                    statusText = found.GetComponent<TMP_Text>();
            }

            if (statusText == null)
                Debug.LogError("FoodSlot 下需要有 StatusText。", this);

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
            IngredientInstance item = ShownItem();
            if (item == null || item.Quantity <= 0)
                return;

            if (statusText != null)
                statusText.text = "熟度：" + item.Doneness.ToString("0.00", CultureInfo.InvariantCulture);

            if (drag != null && drag.IsDragging)
                return;

            IngredientDefinition definition = FindDefinition(item.DefinitionId);
            if (icon != null && definition != null && definition.spriteWhole != null)
                icon.sprite = definition.spriteWhole;

            if (countText != null)
                countText.text = "x" + item.Quantity + ShapeLabel(item.Shape) + MethodLabel(item.CookMethod);
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

        IngredientInstance ShownItem()
        {
            if (GetComponentInParent<Plate>() != null)
                return Plate.FirstHeld();

            if (drag != null && drag.IsDragging && FirstItem() == null)
                return Plate.FirstHeld();

            return FirstItem();
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
