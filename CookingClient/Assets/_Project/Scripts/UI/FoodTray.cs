using Cooking.Core;
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
            RefreshVisibility();
        }

        void LateUpdate()
        {
            RefreshVisibility();
        }

        void RefreshVisibility()
        {
            if (slot == null)
                return;

            if (drag != null && drag.IsDragging)
                return;

            if (slot.transform.parent != transform)
                return;

            slot.SetActive(FirstItem() != null);
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
    }
}
