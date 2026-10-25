using Cooking.Core;
using Cooking.Runtime;
using UnityEngine;

namespace Cooking.UI
{
    public sealed class Plate : MonoBehaviour, IFoodStation
    {
        public bool TryPlace(IngredientInstance item)
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
                return true;

            if (session.HeldDish.Items.Count > 0)
                return false;

            if (session.Inventory != null)
                session.Inventory.Remove(item);

            session.HeldDish.Items.Add(item);
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
        }

        public static IngredientInstance FirstHeld()
        {
            GameSession session = Session();
            if (session == null || session.HeldDish == null)
                return null;

            for (int i = 0; i < session.HeldDish.Items.Count; i++)
            {
                IngredientInstance item = session.HeldDish.Items[i];
                if (item != null && item.Quantity > 0)
                    return item;
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
