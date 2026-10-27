using System.Globalization;
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
        [SerializeField] FoodSlotDrag drag;
        [SerializeField] int stackIndex;
        [SerializeField] bool inventorySlot = true;

        IngredientInstance shown;
        IngredientDefinition shownDefinition;
        int shownQuantity;

        public int StackIndex => stackIndex;
        public bool InventorySlot => inventorySlot;
        public IngredientInstance Shown => shown;
        public IngredientDefinition ShownDefinition => shownDefinition;

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

            if (!inventorySlot)
            {
                Image background = GetComponent<Image>();
                if (background != null && background != icon)
                    background.enabled = false;
            }
        }

        void LateUpdate()
        {
            if (drag != null && drag.IsDragging)
                return;

            if (inventorySlot || shown == null)
                return;

            Apply();
        }

        public void ShowStack(IngredientInstance item, int quantity, IngredientDefinition definition)
        {
            shown = item;
            shownQuantity = quantity;
            shownDefinition = definition;
            if (drag != null && drag.IsDragging)
                return;

            Apply();
        }

        void Apply()
        {
            bool has = shown != null && shownQuantity > 0;
            if (statusText != null)
                statusText.text = has
                    ? "熟度：" + shown.Doneness.ToString("0.00", CultureInfo.InvariantCulture)
                    : "";

            if (icon != null)
            {
                Sprite sprite = null;
                if (has && shownDefinition != null)
                {
                    sprite = FoodVisualResolver.FindSprite(shownDefinition, shown.Shape, shown.CookMethod);
                    if (sprite == null)
                        sprite = shownDefinition.spriteWhole;
                }

                icon.enabled = sprite != null;
                icon.preserveAspect = true;
                icon.color = Color.white;
                if (sprite != null)
                    icon.sprite = sprite;
            }

            if (countText != null)
                countText.text = has
                    ? "x" + shownQuantity + ShapeLabel(shown.Shape) + MethodLabel(shown.CookMethod)
                    : "";
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
                    return " 煎煮";
                default:
                    return "";
            }
        }

    }
}
