using Cooking.Core;
using Cooking.Data;
using Cooking.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cooking.UI
{
    public sealed class FoodTray : MonoBehaviour
    {
        [SerializeField] GameObject slot;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text countText;
        [SerializeField] IngredientDefinition[] definitions;
        [SerializeField] FoodSlotDrag drag;

        void Awake()
        {
            if (slot == null)
            {
                Transform found = transform.Find("FoodSlot");
                if (found != null)
                    slot = found.gameObject;
            }

            if (slot == null)
            {
                Debug.LogError("TrayPanel 下需要有 FoodSlot。", this);
                return;
            }

            if (icon == null)
            {
                Transform found = slot.transform.Find("Image");
                if (found != null)
                    icon = found.GetComponent<Image>();
            }

            if (countText == null)
            {
                Transform found = slot.transform.Find("CountText");
                if (found != null)
                    countText = found.GetComponent<TMP_Text>();
            }

            if (drag == null)
                drag = slot.GetComponent<FoodSlotDrag>();
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
            if (slot == null)
                return;

            if (drag != null && drag.IsDragging)
                return;

            IngredientInstance item = FirstItem();
            if (item == null || item.Quantity <= 0)
            {
                slot.SetActive(false);
                return;
            }

            slot.SetActive(true);
            IngredientDefinition definition = FindDefinition(item.DefinitionId);
            if (icon != null && definition != null && definition.spriteWhole != null)
                icon.sprite = definition.spriteWhole;

            if (countText != null)
                countText.text = "x" + item.Quantity + ShapeLabel(item.Shape);
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
