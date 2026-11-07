using System.Collections.Generic;
using Cooking.Core;
using Cooking.Data;
using Cooking.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Cooking.UI
{
    [DefaultExecutionOrder(100)]
    public sealed class IngredientDetails : MonoBehaviour
    {
        [SerializeField] IngredientAttributeCatalog catalog;
        [SerializeField] NpcCatalog npcCatalog;
        [SerializeField] GameObject panel;
        [SerializeField] GameObject propPanel;
        [SerializeField] RectTransform slotContent;

        TMP_Text displayNameText;
        TMP_Text displayNameCh;
        TMP_Text displayNameEn;
        Image foodImage;
        Image propImage;
        TMP_Text propDescText;
        TMP_Text propDisplayNameText;
        GameObject attributeNames;
        GameObject attributeValues;
        TMP_Text descriptionLabel;
        TMP_Text descriptionValue;
        TMP_Text kindLabel;
        TMP_Text kindValue;
        TMP_Text canEatRawText;
        TMP_Text canCutText;
        TMP_Text optimalLabel;
        TMP_Text optimalValue;
        TMP_Text maxLabel;
        TMP_Text maxValue;
        TMP_Text stateLabel;
        TMP_Text stateValue;

        bool bound;
        bool propBound;
        bool loggedMissingPropChrome;
        readonly List<RaycastResult> hits = new List<RaycastResult>();
        readonly HashSet<string> missingIds = new HashSet<string>();
        PointerEventData pointerData;

        void Awake()
        {
            Bind();
            BindPropPanel();
            if (panel != null)
            {
                IgnoreRaycasts(panel);
                panel.SetActive(false);
            }

            if (propPanel != null)
            {
                IgnoreRaycasts(propPanel);
                propPanel.SetActive(false);
            }
        }

        void OnDisable()
        {
            Hide();
            HideProp();
        }

        void LateUpdate()
        {
            if (panel == null && propPanel == null)
                return;

            FoodSlotView slot = SlotUnderPointer();
            bool click = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            FoodSlotView.UpdatePointer(slot, click);
            if (slot == null)
            {
                Hide();
                HideProp();
                return;
            }

            FoodSlotDrag drag = slot.GetComponent<FoodSlotDrag>();
            if (drag != null && drag.IsDragging)
            {
                Hide();
                HideProp();
                return;
            }

            if (slot.ShowingProp)
            {
                Hide();
                ShowProp(slot);
                return;
            }

            if (slot.Shown == null || slot.ShownDefinition == null)
            {
                Hide();
                HideProp();
                return;
            }

            HideProp();
            Show(slot.Shown, slot.ShownDefinition);
        }

        public void Show(IngredientInstance item, IngredientDefinition definition)
        {
            if (item == null || definition == null)
            {
                Hide();
                return;
            }

            if (!bound)
                Bind();

            if (panel == null)
                return;

            if (!panel.activeSelf)
                panel.SetActive(true);

            SetFoodChrome(true);
            ApplyDisplayNames(definition);
            if (foodImage != null)
            {
                Sprite sprite = FoodVisualResolver.SpriteFor(definition, item.Shape, item.CookMethod);
                foodImage.sprite = sprite;
                foodImage.enabled = sprite != null;
                foodImage.preserveAspect = true;
                foodImage.color = Color.white;
            }
            SetFromTable(descriptionLabel, "label.description");
            SetText(descriptionValue, IngredientDetailText.Description(definition, GameLanguage.Current));
            SetFromTable(kindLabel, "label.kind");
            SetFromTable(kindValue, "kind." + definition.kind);
            SetFlag(canEatRawText, "canEatRaw", definition.canEatRaw);
            SetFlag(canCutText, "canCut", definition.canCut);
            SetFromTable(optimalLabel, "label.optimalDoneness");
            SetText(optimalValue, IngredientDetailText.Number(definition.optimalDoneness));
            SetFromTable(maxLabel, "label.maxDoneness");
            SetText(maxValue, IngredientDetailText.Number(definition.maxDoneness));
            SetFromTable(stateLabel, "label.state");
            WarnIfMissing("shape." + item.Shape);
            WarnIfMissing("cook." + item.CookMethod);
            WarnIfMissing("state.joiner");
            SetText(stateValue, IngredientDetailText.State(catalog, item.Shape, item.CookMethod, GameLanguage.Current));
        }

        public void ShowProp(FoodSlotView slot)
        {
            if (slot == null || !slot.ShowingProp)
            {
                HideProp();
                return;
            }

            if (!propBound)
                BindPropPanel();

            if (propPanel == null)
                return;

            if (propImage == null || propDescText == null || propDisplayNameText == null)
            {
                if (!loggedMissingPropChrome)
                {
                    loggedMissingPropChrome = true;
                    Debug.LogError(
                        "PropDetailsPanel 下需要有 PropImage、PropDescText 和 PropDiasPlayName。",
                        propPanel);
                }
                return;
            }

            NpcProp definition = npcCatalog != null ? npcCatalog.FindProp(slot.ShownPropId) : null;
            if (definition == null && npcCatalog != null && !string.IsNullOrEmpty(slot.ShownPropId))
                Debug.LogError("NpcCatalog 里没有道具 " + slot.ShownPropId + "。", npcCatalog);

            if (!propPanel.activeSelf)
                propPanel.SetActive(true);

            Sprite sprite = definition != null && definition.sprite != null
                ? definition.sprite
                : slot.ShownPropSprite;
            propImage.sprite = sprite;
            propImage.enabled = sprite != null;
            propImage.preserveAspect = true;
            propImage.color = Color.white;
            propDescText.text = definition != null
                ? NpcCatalog.Pick(GameLanguage.Current, definition.descriptionZh, definition.descriptionEn)
                : slot.ShownPropDescription ?? "";
            propDisplayNameText.text = definition != null
                ? NpcCatalog.Pick(GameLanguage.Current, definition.displayNameZh, definition.displayNameEn)
                : "";
        }

        public void Hide()
        {
            if (panel != null && panel.activeSelf)
                panel.SetActive(false);
        }

        public void HideProp()
        {
            if (propPanel != null && propPanel.activeSelf)
                propPanel.SetActive(false);
        }

        void Bind()
        {
            if (panel == null)
            {
                Transform found = transform.Find("DetailsPanel");
                if (found != null)
                    panel = found.gameObject;
            }

            if (slotContent == null)
            {
                Transform found = transform.Find("InventoryScroll/Viewport/InventoryContent");
                if (found != null)
                    slotContent = found as RectTransform;
            }

            if (panel == null)
            {
                Debug.LogError("TrayPanel 下需要有 DetailsPanel。", this);
                return;
            }

            if (slotContent == null)
                Debug.LogError("TrayPanel 下需要有 InventoryScroll/Viewport/InventoryContent。", this);

            if (catalog == null)
                Debug.LogError("IngredientDetails 需要绑定 IngredientAttributeCatalog。", this);

            Transform names = panel.transform.Find("AttributeName");
            Transform values = panel.transform.Find("AttributeValue");
            if (names == null || values == null)
            {
                Debug.LogError("DetailsPanel 下需要有 AttributeName 和 AttributeValue。", panel);
                return;
            }

            displayNameText = FindText(names, "DisplayName");
            displayNameCh = Require(names, "DisplayName_ch");
            displayNameEn = Require(names, "DisplayName_eng");
            attributeNames = names.gameObject;
            attributeValues = values.gameObject;

            Transform food = panel.transform.Find("FoodImage");
            if (food == null)
                Debug.LogError("DetailsPanel 下需要有 FoodImage。", panel);
            else
            {
                foodImage = food.GetComponent<Image>();
                if (foodImage == null)
                    Debug.LogError("DetailsPanel/FoodImage 需要有 Image。", food);
            }
            descriptionLabel = Require(names, "Description");
            kindLabel = Require(names, "kind");
            optimalLabel = Require(names, "OpyimalDoneness");
            maxLabel = Require(names, "MaxDoness");
            stateLabel = Require(names, "State");

            kindValue = Require(values, "kind");
            canEatRawText = Require(values, "CanEatRaw");
            canCutText = Require(values, "CanCut");
            descriptionValue = Require(values, "Description");
            optimalValue = Require(values, "OpyimalDoneness");
            maxValue = Require(values, "MaxDoness");
            stateValue = Require(values, "State");
            bound = true;
        }

        void BindPropPanel()
        {
            if (propPanel == null)
            {
                Transform found = transform.Find("PropDetailsPanel");
                if (found != null)
                    propPanel = found.gameObject;
            }

            if (npcCatalog == null)
            {
                System.Reflection.FieldInfo field = typeof(FoodTray).GetField(
                    "npcCatalog",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                FoodTray tray = GetComponent<FoodTray>();
                if (field != null && tray != null)
                    npcCatalog = field.GetValue(tray) as NpcCatalog;
            }

            if (propPanel == null)
            {
                Debug.LogError("TrayPanel 下需要有 PropDetailsPanel。", this);
                return;
            }

            if (npcCatalog == null)
                Debug.LogError("IngredientDetails 需要绑定 NpcCatalog。", this);

            Transform propPicture = propPanel.transform.Find("PropImage");
            if (propPicture == null)
                Debug.LogError("PropDetailsPanel 下需要有 PropImage。", propPanel);
            else
            {
                propImage = propPicture.GetComponent<Image>();
                if (propImage == null)
                    Debug.LogError("PropDetailsPanel/PropImage 需要有 Image。", propPicture);
            }

            Transform propText = propPanel.transform.Find("PropDescText");
            if (propText == null)
                Debug.LogError("PropDetailsPanel 下需要有 PropDescText。", propPanel);
            else
            {
                propDescText = propText.GetComponent<TMP_Text>();
                if (propDescText == null)
                    Debug.LogError("PropDetailsPanel/PropDescText 需要有 TextMeshPro 文字。", propText);
            }

            Transform propName = propPanel.transform.Find("PropDiasPlayName");
            if (propName == null)
                Debug.LogError("PropDetailsPanel 下需要有 PropDiasPlayName。", propPanel);
            else
            {
                propDisplayNameText = propName.GetComponent<TMP_Text>();
                if (propDisplayNameText == null)
                    Debug.LogError("PropDetailsPanel/PropDiasPlayName 需要有 TextMeshPro 文字。", propName);
            }

            propBound = true;
        }

        TMP_Text Require(Transform parent, string childName)
        {
            Transform found = parent.Find(childName);
            if (found == null)
            {
                Debug.LogError(parent.name + " 下需要有 " + childName + "。", parent);
                return null;
            }

            TMP_Text text = found.GetComponent<TMP_Text>();
            if (text == null)
                Debug.LogError(parent.name + "/" + childName + " 需要有 TextMeshPro 文字。", found);

            return text;
        }

        static TMP_Text FindText(Transform parent, string childName)
        {
            Transform found = parent.Find(childName);
            if (found == null)
                return null;

            return found.GetComponent<TMP_Text>();
        }

        void ApplyDisplayNames(IngredientDefinition definition)
        {
            bool english = GameLanguage.Current == DiaryLanguage.En;
            string zh = definition != null ? definition.displayName ?? "" : "";
            string en = definition != null ? definition.displayNameEn ?? "" : "";
            if (english && string.IsNullOrEmpty(en))
                en = zh;

            SetText(displayNameText, IngredientDetailText.DisplayName(definition, GameLanguage.Current));
            SetText(displayNameCh, english ? "" : zh);
            SetText(displayNameEn, english ? en : "");
            if (displayNameCh != null)
                displayNameCh.gameObject.SetActive(!english);
            if (displayNameEn != null)
                displayNameEn.gameObject.SetActive(english);
        }

        void SetFlag(TMP_Text target, string key, bool value)
        {
            WarnIfMissing("label." + key);
            WarnIfMissing(key + "." + (value ? "true" : "false"));
            SetText(target, IngredientDetailText.Flag(catalog, key, value, GameLanguage.Current));
        }

        void SetFromTable(TMP_Text target, string id)
        {
            if (target == null)
                return;

            if (!IngredientDetailText.TryText(catalog, id, GameLanguage.Current, out string text))
            {
                WarnIfMissing(id);
                target.text = "";
                return;
            }

            target.text = text;
        }

        void WarnIfMissing(string id)
        {
            if (IngredientDetailText.TryText(catalog, id, GameLanguage.Current, out _))
                return;

            if (missingIds.Add(id))
                Debug.LogError("AttributeNames 缺少 id：" + id, catalog != null ? catalog : this);
        }

        void SetFoodChrome(bool food)
        {
            if (attributeNames != null)
                attributeNames.SetActive(food);
            if (attributeValues != null)
                attributeValues.SetActive(food);
            if (foodImage != null)
                foodImage.gameObject.SetActive(food);
        }

        static void SetText(TMP_Text target, string value)
        {
            if (target != null)
                target.text = value ?? "";
        }

        FoodSlotView SlotUnderPointer()
        {
            if (slotContent == null || EventSystem.current == null || Mouse.current == null)
                return null;

            if (pointerData == null)
                pointerData = new PointerEventData(EventSystem.current);

            pointerData.position = Mouse.current.position.ReadValue();
            hits.Clear();
            EventSystem.current.RaycastAll(pointerData, hits);
            for (int i = 0; i < hits.Count; i++)
            {
                GameObject hit = hits[i].gameObject;
                if (hit == null)
                    continue;

                FoodSlotView slot = hit.GetComponentInParent<FoodSlotView>();
                if (slot == null || !slot.InventorySlot)
                    continue;

                if (!slot.transform.IsChildOf(slotContent))
                    continue;

                return slot;
            }

            return null;
        }

        static void IgnoreRaycasts(GameObject root)
        {
            Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null)
                    graphics[i].raycastTarget = false;
            }
        }
    }
}
