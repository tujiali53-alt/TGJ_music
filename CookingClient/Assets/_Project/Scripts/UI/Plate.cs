using System.Collections.Generic;
using Cooking.Core;
using Cooking.Data;
using Cooking.Runtime;
using UnityEngine;

namespace Cooking.UI
{
    public sealed class Plate : MonoBehaviour, IFoodStation
    {
        public const int Capacity = 8;

        static Plate current;

        readonly List<FoodSlotView> tokens = new List<FoodSlotView>();

        void Awake()
        {
            current = this;
            tokens.Clear();
            FoodSlotView[] views = GetComponentsInChildren<FoodSlotView>(true);
            for (int i = 0; i < views.Length; i++)
            {
                FoodSlotView view = views[i];
                if (view != null && !view.InventorySlot)
                    tokens.Add(view);
            }

            if (tokens.Count == 0)
                Debug.LogError("Plate 下需要已经摆好的食物令牌。", this);
        }

        public bool TryPlace(IngredientInstance item)
        {
            return TryPlace(item, Vector2.zero, null);
        }

        public bool TryPlace(IngredientInstance item, Vector2 localPoint, IngredientDefinition definition)
        {
            GameSession session = Session();
            if (session == null || session.HeldDish == null)
            {
                Debug.LogError("需要 GameSession 上的 HeldDish。", this);
                return false;
            }

            if (item == null)
                return false;

            if (session.HeldDish.Items.Contains(item))
            {
                Present(item, localPoint, definition);
                return true;
            }

            if (session.HeldDish.Items.Count >= Capacity || FreeToken() == null)
                return false;

            if (session.Inventory != null)
                session.Inventory.Remove(item);

            session.HeldDish.Items.Add(item);
            Present(item, localPoint, definition);
            return true;
        }

        public static void ReleaseToInventory(IngredientInstance item)
        {
            GameSession session = Session();
            if (session == null || session.HeldDish == null || item == null)
                return;

            if (!session.HeldDish.Items.Remove(item))
                return;

            if (session.Inventory == null)
            {
                Debug.LogError("GameSession 缺少库存列表。");
                return;
            }

            if (!session.Inventory.Contains(item))
                session.Inventory.Add(item);

            if (current != null)
                current.ClearToken(item);
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

        public static bool HasFood()
        {
            GameSession session = Session();
            if (session == null || session.HeldDish == null)
                return false;

            for (int i = 0; i < session.HeldDish.Items.Count; i++)
            {
                IngredientInstance item = session.HeldDish.Items[i];
                if (item != null && item.Quantity > 0)
                    return true;
            }

            return false;
        }

        void Present(IngredientInstance item, Vector2 localPoint, IngredientDefinition definition)
        {
            FoodSlotView token = TokenFor(item);
            if (token == null)
                token = FreeToken();
            if (token == null)
                return;

            if (definition == null)
                definition = FoodTray.Lookup(item.DefinitionId);

            token.transform.SetParent(transform, false);
            token.ShowStack(item, item.Quantity, definition);
            RectTransform rect = token.transform as RectTransform;
            if (rect != null)
                rect.anchoredPosition = localPoint;

            token.transform.SetAsLastSibling();
        }

        void ClearToken(IngredientInstance item)
        {
            FoodSlotView token = TokenFor(item);
            if (token == null)
                return;

            token.ShowStack(null, 0, null);
            FoodSlotDrag drag = token.GetComponent<FoodSlotDrag>();
            if (drag != null)
                drag.ReturnToTray();
        }

        FoodSlotView TokenFor(IngredientInstance item)
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                FoodSlotView token = tokens[i];
                if (token != null && token.Shown == item)
                    return token;
            }

            return null;
        }

        FoodSlotView FreeToken()
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                FoodSlotView token = tokens[i];
                if (token != null && token.Shown == null)
                    return token;
            }

            return null;
        }

        static GameSession Session()
        {
            GameRoot root = GameRoot.Instance;
            if (root == null)
                return null;

            return root.Session;
        }
    }
}
