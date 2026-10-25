using Cooking.Core;
using Cooking.Data;
using Cooking.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cooking.UI
{
    public sealed class CookingPot : MonoBehaviour, IFoodStation, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] IngredientDefinition[] definitions;
        [SerializeField] float donenessPerSecond = 40f;

        bool holding;

        public bool TryPlace(IngredientInstance item)
        {
            return item != null;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            holding = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            holding = false;
        }

        void OnDisable()
        {
            holding = false;
        }

        void Update()
        {
            if (!holding)
                return;

            if (GetComponentInChildren<FoodSlotDrag>() == null)
                return;

            IngredientInstance item = FirstItem();
            if (item == null)
                return;

            IngredientDefinition definition = FindDefinition(item.DefinitionId);
            if (definition == null)
            {
                Debug.LogError("煮锅缺少这份食材的定义：" + item.DefinitionId, this);
                return;
            }

            if (item.Doneness >= definition.maxDoneness)
                return;

            float next = item.Doneness + donenessPerSecond * Time.deltaTime;
            item.Doneness = Mathf.Min(definition.maxDoneness, next);
            ApplyBoil(item);
        }

        static void ApplyBoil(IngredientInstance item)
        {
            if (item.CookMethod == CookMethod.Raw)
                item.CookMethod = CookMethod.Boiled;
            else if (item.CookMethod == CookMethod.Fried)
                item.CookMethod = CookMethod.BoiledAndFried;
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
