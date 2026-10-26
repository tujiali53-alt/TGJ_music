using System.Collections.Generic;
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

        static CookingPot current;

        readonly List<IngredientInstance> cooking = new List<IngredientInstance>();
        bool holding;

        void Awake()
        {
            current = this;
        }

        public bool TryPlace(IngredientInstance item)
        {
            if (item == null)
                return false;

            if (!cooking.Contains(item))
                cooking.Add(item);

            return true;
        }

        public static void Forget(IngredientInstance item)
        {
            if (current == null || item == null)
                return;

            current.cooking.Remove(item);
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

            for (int i = 0; i < cooking.Count; i++)
            {
                IngredientInstance item = cooking[i];
                if (item == null)
                    continue;

                IngredientDefinition definition = FindDefinition(item.DefinitionId);
                if (definition == null)
                {
                    Debug.LogError("煮锅缺少这份食材的定义：" + item.DefinitionId, this);
                    continue;
                }

                if (item.Doneness >= definition.maxDoneness)
                    continue;

                float next = item.Doneness + donenessPerSecond * Time.deltaTime;
                item.Doneness = Mathf.Min(definition.maxDoneness, next);
                ApplyBoil(item);
            }
        }

        static void ApplyBoil(IngredientInstance item)
        {
            if (item.CookMethod == CookMethod.Raw)
                item.CookMethod = CookMethod.Boiled;
            else if (item.CookMethod == CookMethod.Fried)
                item.CookMethod = CookMethod.BoiledAndFried;
        }

        IngredientDefinition FindDefinition(string definitionId)
        {
            if (definitions != null)
            {
                for (int i = 0; i < definitions.Length; i++)
                {
                    IngredientDefinition definition = definitions[i];
                    if (definition != null && definition.id == definitionId)
                        return definition;
                }
            }

            return FoodTray.Lookup(definitionId);
        }
    }
}
