using System.Collections.Generic;
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
        [SerializeField] FoodSlotDrag drag;
        [SerializeField] int stackIndex;
        [SerializeField] bool inventorySlot = true;

        static readonly HashSet<string> missingCutSprites = new HashSet<string>();
        static Sprite slotHighlight;
        static bool missingHighlight;
        static FoodSlotView hoveredSlot;
        static FoodSlotView selectedSlot;

        IngredientInstance shown;
        IngredientDefinition shownDefinition;
        int shownQuantity;
        bool showingProp;
        string shownPropId;
        Sprite shownPropSprite;
        string shownPropDescription;
        Image background;
        Sprite normalSprite;
        bool hovered;
        bool selected;
        bool potToken;
        static Sprite potOccupiedBase;
        static bool missingPotOccupiedBase;

        public int StackIndex => stackIndex;
        public bool InventorySlot => inventorySlot;
        public IngredientInstance Shown => shown;
        public IngredientDefinition ShownDefinition => shownDefinition;
        public bool ShowingProp => showingProp;
        public string ShownPropId => shownPropId;
        public Sprite ShownPropSprite => shownPropSprite;
        public string ShownPropDescription => shownPropDescription;

        void Awake()
        {
            if (icon == null)
            {
                Transform found = transform.Find("Image");
                if (found == null)
                    found = transform.Find("area");
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

            background = GetComponent<Image>();
            if (background == icon)
                background = null;
            if (background != null)
                normalSprite = background.sprite;

            potToken = GetComponentInParent<PotArea>() != null;

            if (!inventorySlot)
            {
                if (background != null)
                    background.enabled = false;
                if (countText != null)
                    countText.gameObject.SetActive(false);
                if (statusText != null)
                    statusText.gameObject.SetActive(false);
            }
            else
                ResolveHighlight();
        }

        void OnDisable()
        {
            if (hoveredSlot == this)
                hoveredSlot = null;
            if (selectedSlot == this)
                selectedSlot = null;
            hovered = false;
            selected = false;
            ApplyFrame();
        }

        public static void UpdatePointer(FoodSlotView under, bool click)
        {
            if (hoveredSlot != under)
            {
                if (hoveredSlot != null)
                    hoveredSlot.SetHovered(false);
                hoveredSlot = under;
                if (hoveredSlot != null && hoveredSlot.inventorySlot)
                    hoveredSlot.SetHovered(true);
            }

            if (!click || under == null || !under.inventorySlot || under == selectedSlot)
                return;

            if (selectedSlot != null)
                selectedSlot.SetSelected(false);
            selectedSlot = under;
            selectedSlot.SetSelected(true);
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
            showingProp = false;
            shownPropId = null;
            shownPropSprite = null;
            shownPropDescription = "";
            shown = item;
            shownQuantity = quantity;
            shownDefinition = definition;
            if (drag != null && drag.IsDragging)
                return;

            Apply();
        }

        public void RefreshOccupiedBase()
        {
            if (drag != null && drag.IsDragging)
                return;

            ApplyOccupiedBase(shown != null && shownQuantity > 0);
        }

        public void ShowProp(string propId, Sprite sprite, int quantity, string description)
        {
            showingProp = true;
            shownPropId = propId;
            shownPropSprite = sprite;
            shownPropDescription = description ?? "";
            shown = null;
            shownDefinition = null;
            shownQuantity = quantity;
            if (drag != null && drag.IsDragging)
                return;

            ApplyProp();
        }

        static string DonenessPrefix()
        {
            return GameLanguage.Current == DiaryLanguage.En ? "Doneness: " : "熟度：";
        }

        void Apply()
        {
            bool has = shown != null && shownQuantity > 0;
            if (statusText != null)
                statusText.text = has
                    ? DonenessPrefix() + shown.Doneness.ToString("0.00", CultureInfo.InvariantCulture)
                    : "";

            if (icon != null)
            {
                Sprite sprite = null;
                if (has && shownDefinition != null)
                {
                    if (shown.Shape == CutShape.Cut && shownDefinition.spriteCut == null && missingCutSprites.Add(shownDefinition.id))
                        Debug.LogError(shownDefinition.id + " 切开后需要 spriteCut。", shownDefinition);

                    sprite = FoodVisualResolver.SpriteFor(shownDefinition, shown.Shape, shown.CookMethod);
                }
                icon.enabled = sprite != null;
                icon.preserveAspect = true;
                icon.color = has && shownDefinition != null && potToken
                    ? CookingPot.DonenessTint(shown.Doneness, shownDefinition.maxDoneness)
                    : Color.white;
                if (sprite != null)
                    icon.sprite = sprite;
            }

            if (countText != null)
                countText.text = has ? "x" + shownQuantity : "";

            ApplyOccupiedBase(has);
        }

        void ApplyOccupiedBase(bool has)
        {
            if (!potToken || background == null || (drag != null && drag.IsDragging))
                return;

            Sprite baseSprite = ResolvePotOccupiedBase();
            if (has && baseSprite != null)
            {
                background.sprite = baseSprite;
                background.color = Color.white;
                background.preserveAspect = true;
                background.enabled = true;
            }
            else
            {
                background.enabled = false;
            }
        }

        static Sprite ResolvePotOccupiedBase()
        {
            if (potOccupiedBase != null || missingPotOccupiedBase)
                return potOccupiedBase;

            CookingPot pot = Object.FindAnyObjectByType<CookingPot>(FindObjectsInactive.Include);
            if (pot != null)
                potOccupiedBase = pot.OccupiedTokenBase;

            if (potOccupiedBase == null)
            {
                missingPotOccupiedBase = true;
                Debug.LogError("缺少锅令牌底图 pot.png。请在 CookingPot 上绑 OccupiedTokenBase。");
            }

            return potOccupiedBase;
        }

        void ApplyProp()
        {
            if (statusText != null)
                statusText.text = "";

            if (icon != null)
            {
                icon.enabled = shownPropSprite != null && shownQuantity > 0;
                icon.preserveAspect = true;
                icon.color = Color.white;
                if (shownPropSprite != null)
                    icon.sprite = shownPropSprite;
            }

            if (countText != null)
                countText.text = shownQuantity > 0 ? "x" + shownQuantity : "";
        }

        void SetHovered(bool value)
        {
            hovered = value;
            ApplyFrame();
        }

        void SetSelected(bool value)
        {
            selected = value;
            ApplyFrame();
        }

        void ApplyFrame()
        {
            if (!inventorySlot || background == null)
                return;

            bool show = (hovered || selected) && slotHighlight != null;
            background.sprite = show ? slotHighlight : normalSprite;
        }

        static void ResolveHighlight()
        {
            if (slotHighlight != null || missingHighlight)
                return;

            SlotHighlight art = Resources.Load<SlotHighlight>("SlotHighlight");
            if (art == null || art.sprite == null)
            {
                missingHighlight = true;
                Debug.LogError("缺少 SlotHighlight，或未绑上 bag_01。");
                return;
            }

            slotHighlight = art.sprite;
        }

    }
}
