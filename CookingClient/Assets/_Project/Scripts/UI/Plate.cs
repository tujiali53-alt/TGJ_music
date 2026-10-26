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

        FoodTokens tokens;

        void Awake()
        {
            current = this;
            tokens = new FoodTokens(transform);
            tokens.Collect();
            if (tokens.TokenCount == 0)
                Debug.LogError("Plate 下需要已经摆好的食物令牌。", this);
        }

        public bool TryPlace(IngredientInstance item)
        {
            return TryPlace(item, Vector2.zero, null);
        }

        public bool TryPlace(IngredientInstance item, Vector2 localPoint, IngredientDefinition definition)
        {
            GameSession session = IngredientInventory.Session();
            if (session == null || session.HeldDish == null)
            {
                Debug.LogError("需要 GameSession 上的 HeldDish。", this);
                return false;
            }

            if (item == null || tokens == null)
                return false;

            if (session.HeldDish.Items.Contains(item))
            {
                tokens.Present(item, localPoint, definition);
                return true;
            }

            if (session.HeldDish.Items.Count >= Capacity || !tokens.HasFree())
                return false;

            CuttingBoard.Detach(item);
            CookingPot.Detach(item);
            IngredientInventory.Remove(item);
            session.HeldDish.Items.Add(item);
            tokens.Present(item, localPoint, definition);
            return true;
        }

        public static bool Detach(IngredientInstance item)
        {
            GameSession session = IngredientInventory.Session();
            if (current == null || current.tokens == null || session == null || session.HeldDish == null || item == null)
                return false;

            if (!session.HeldDish.Items.Remove(item))
                return false;

            current.tokens.Clear(item);
            return true;
        }

        public static void ReleaseToInventory(IngredientInstance item)
        {
            if (!Detach(item))
                return;

            IngredientInventory.Add(item);
        }

        public void ClearAll()
        {
            if (tokens != null)
                tokens.ClearAll();
        }

        public static bool HasFood()
        {
            GameSession session = IngredientInventory.Session();
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
    }
}
