using System.Collections.Generic;
using Cooking.Core;
using Cooking.Data;
using Cooking.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cooking.UI
{
    public sealed class FoodTokens
    {
        readonly Transform root;
        readonly List<FoodSlotView> tokens = new List<FoodSlotView>();

        public FoodTokens(Transform root)
        {
            this.root = root;
        }

        public int TokenCount => tokens.Count;

        public void Collect()
        {
            tokens.Clear();
            if (root == null)
                return;

            FoodSlotView[] views = root.GetComponentsInChildren<FoodSlotView>(true);
            for (int i = 0; i < views.Length; i++)
            {
                FoodSlotView view = views[i];
                if (view != null && !view.InventorySlot)
                    tokens.Add(view);
            }
        }

        public bool HasFree()
        {
            return Free() != null;
        }

        public void Present(IngredientInstance item, Vector2 localPoint, IngredientDefinition definition)
        {
            if (item == null)
                return;

            FoodSlotView token = Find(item);
            if (token == null)
                token = Free();
            if (token == null)
                return;

            if (definition == null)
                definition = FoodTray.Lookup(item.DefinitionId);

            token.transform.SetParent(root, false);
            token.ShowStack(item, item.Quantity, definition);
            RectTransform rect = token.transform as RectTransform;
            if (rect != null)
                rect.anchoredPosition = localPoint;

            token.transform.SetAsLastSibling();
        }

        public void Clear(IngredientInstance item)
        {
            FoodSlotView token = Find(item);
            if (token == null)
                return;

            token.ShowStack(null, 0, null);
            FoodSlotDrag drag = token.GetComponent<FoodSlotDrag>();
            if (drag != null)
                drag.ReturnToTray();
        }

        public void ClearAll()
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                FoodSlotView token = tokens[i];
                if (token == null)
                    continue;

                token.ShowStack(null, 0, null);
                FoodSlotDrag drag = token.GetComponent<FoodSlotDrag>();
                if (drag != null)
                    drag.ReturnToTray();
            }
        }

        FoodSlotView Find(IngredientInstance item)
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                FoodSlotView token = tokens[i];
                if (token != null && token.Shown == item)
                    return token;
            }

            return null;
        }

        FoodSlotView Free()
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                FoodSlotView token = tokens[i];
                if (token != null && token.Shown == null)
                    return token;
            }

            return null;
        }
    }

    public static class IngredientInventory
    {
        public static void Remove(IngredientInstance item)
        {
            GameSession session = Session();
            if (session == null || session.Inventory == null || item == null)
                return;

            session.Inventory.Remove(item);
        }

        public static void Add(IngredientInstance item)
        {
            if (item == null)
                return;

            GameSession session = Session();
            if (session == null || session.Inventory == null)
            {
                Debug.LogError("GameSession 缺少库存列表。");
                return;
            }

            if (!session.Inventory.Contains(item))
                session.Inventory.Add(item);
        }

        public static GameSession Session()
        {
            GameRoot root = GameRoot.Instance;
            if (root == null)
                return null;

            return root.Session;
        }
    }

    public static class SceneButtons
    {
        public static Button Find(string objectName)
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int t = 0; t < transforms.Length; t++)
                {
                    if (transforms[t].name != objectName)
                        continue;

                    Button button = transforms[t].GetComponent<Button>();
                    if (button != null)
                        return button;
                }
            }

            Debug.LogError("场景里需要有 " + objectName + "。");
            return null;
        }
    }
}
