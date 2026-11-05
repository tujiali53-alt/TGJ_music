using System;
using System.Collections.Generic;
using System.Globalization;
using Cooking.Core;
using Cooking.Data;
using Cooking.Runtime;
using UnityEngine;

namespace Cooking.UI
{
    public sealed class FoodTray : MonoBehaviour
    {
        [SerializeField] RectTransform content;
        [SerializeField] IngredientDefinition[] definitions;
        [SerializeField] NpcCatalog npcCatalog;

        readonly List<FoodSlotView> slots = new List<FoodSlotView>();
        readonly List<Stack> stacks = new List<Stack>();
        readonly List<PropStack> props = new List<PropStack>();

        void Awake()
        {
            if (content == null)
            {
                Transform found = transform.Find("InventoryScroll/Viewport/InventoryContent");
                if (found != null)
                    content = found as RectTransform;
            }

            if (content == null)
                Debug.LogError("TrayPanel 下需要有 InventoryScroll/Viewport/InventoryContent。", this);

            if (npcCatalog == null)
                Debug.LogError("TrayPanel 需要绑上 NpcCatalog。", this);
        }

        void LateUpdate()
        {
            if (content == null)
                return;

            CollectSlots();
            BuildStacks();
            BuildProps();
            for (int i = 0; i < slots.Count; i++)
            {
                FoodSlotView slot = slots[i];
                if (slot == null)
                    continue;

                int index = slot.StackIndex;
                if (index >= 0 && index < stacks.Count)
                {
                    Stack stack = stacks[index];
                    slot.ShowStack(stack.Head, stack.Quantity, FindDefinition(stack.Head.DefinitionId));
                }
                else if (index >= stacks.Count && index - stacks.Count < props.Count)
                {
                    PropStack prop = props[index - stacks.Count];
                    NpcProp definition = npcCatalog != null ? npcCatalog.FindProp(prop.PropId) : null;
                    Sprite sprite = definition != null ? definition.sprite : null;
                    string description = definition != null
                        ? NpcCatalog.Pick(GameLanguage.Current, definition.descriptionZh, definition.descriptionEn)
                        : "";
                    if (definition == null && npcCatalog != null)
                        Debug.LogError("NpcCatalog 里没有道具 " + prop.PropId + "。", npcCatalog);
                    slot.ShowProp(prop.PropId, sprite, prop.Quantity, description);
                }
                else
                    slot.ShowStack(null, 0, null);
            }
        }

        void CollectSlots()
        {
            slots.Clear();
            FoodSlotView[] found = content.GetComponentsInChildren<FoodSlotView>(true);
            for (int i = 0; i < found.Length; i++)
            {
                FoodSlotView slot = found[i];
                if (slot != null && slot.InventorySlot)
                    slots.Add(slot);
            }
        }

        void BuildStacks()
        {
            stacks.Clear();
            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null || root.Session.Inventory == null)
                return;

            for (int i = 0; i < root.Session.Inventory.Count; i++)
            {
                IngredientInstance item = root.Session.Inventory[i];
                if (item == null || item.Quantity <= 0)
                    continue;

                string key = StackKey(item);
                Stack match = null;
                for (int s = 0; s < stacks.Count; s++)
                {
                    if (stacks[s].Key == key)
                    {
                        match = stacks[s];
                        break;
                    }
                }

                if (match == null)
                {
                    stacks.Add(new Stack
                    {
                        Key = key,
                        Head = item,
                        Quantity = item.Quantity
                    });
                }
                else
                    match.Quantity += item.Quantity;
            }
        }

        void BuildProps()
        {
            props.Clear();
            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null || root.Session.Props == null)
                return;

            for (int i = 0; i < root.Session.Props.Count; i++)
            {
                PropStack item = root.Session.Props[i];
                if (item == null || item.Quantity <= 0 || string.IsNullOrEmpty(item.PropId))
                    continue;

                PropStack match = null;
                for (int s = 0; s < props.Count; s++)
                {
                    if (props[s].PropId == item.PropId)
                    {
                        match = props[s];
                        break;
                    }
                }

                if (match == null)
                {
                    props.Add(new PropStack
                    {
                        PropId = item.PropId,
                        Quantity = item.Quantity
                    });
                }
                else
                    match.Quantity += item.Quantity;
            }
        }

        static string StackKey(IngredientInstance item)
        {
            string seasonings = "";
            if (item.SeasoningIds != null && item.SeasoningIds.Count > 0)
            {
                var copy = new List<string>(item.SeasoningIds);
                copy.Sort(CompareSeasoning);
                seasonings = string.Join(",", copy);
            }
            return item.DefinitionId
                + "|" + item.Doneness.ToString("0.00", CultureInfo.InvariantCulture)
                + "|" + (int)item.Shape
                + "|" + (int)item.CookMethod
                + "|" + seasonings;
        }

        static int CompareSeasoning(string a, string b)
        {
            return string.CompareOrdinal(a, b);
        }

        public static IngredientDefinition Lookup(string definitionId)
        {
            FoodTray tray = FindAnyObjectByType<FoodTray>(FindObjectsInactive.Include);
            if (tray == null)
                return null;

            return tray.FindDefinition(definitionId);
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

        sealed class Stack
        {
            public string Key;
            public IngredientInstance Head;
            public int Quantity;
        }
    }
}
