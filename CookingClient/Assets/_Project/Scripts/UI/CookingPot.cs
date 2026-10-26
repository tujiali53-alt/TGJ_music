using System.Collections.Generic;
using Cooking.Data;
using Cooking.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace Cooking.UI
{
    public sealed class CookingPot : MonoBehaviour, IFoodStation
    {
        public const int Capacity = 8;

        [SerializeField] IngredientDefinition[] definitions;
        [SerializeField] float donenessPerSecond = 40f;

        static CookingPot current;

        readonly List<IngredientInstance> held = new List<IngredientInstance>();
        FoodTokens tokens;
        Button fireButton;
        bool burning;

        void Awake()
        {
            current = this;
            tokens = new FoodTokens(transform);
            tokens.Collect();
            if (tokens.TokenCount == 0)
                Debug.LogError("CookingPot 下需要已经摆好的食物令牌。", this);

            fireButton = SceneButtons.Find("openFireButton");
            if (fireButton != null)
                fireButton.onClick.AddListener(ToggleFire);
        }

        void OnDestroy()
        {
            if (fireButton != null)
                fireButton.onClick.RemoveListener(ToggleFire);
        }

        public bool TryPlace(IngredientInstance item)
        {
            return TryPlace(item, Vector2.zero, null);
        }

        public bool TryPlace(IngredientInstance item, Vector2 localPoint, IngredientDefinition definition)
        {
            if (item == null || tokens == null)
                return false;

            if (held.Contains(item))
            {
                tokens.Present(item, localPoint, definition);
                return true;
            }

            if (held.Count >= Capacity || !tokens.HasFree())
                return false;

            Plate.Detach(item);
            CuttingBoard.Detach(item);
            IngredientInventory.Remove(item);
            held.Add(item);
            tokens.Present(item, localPoint, definition);
            return true;
        }

        public static bool Detach(IngredientInstance item)
        {
            if (current == null || current.tokens == null || item == null)
                return false;

            if (!current.held.Remove(item))
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

        public void ToggleFire()
        {
            burning = !burning;
        }

        void Update()
        {
            if (!burning)
                return;

            for (int i = 0; i < held.Count; i++)
            {
                IngredientInstance item = held[i];
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
            if (item.CookMethod != CookMethod.Boiled)
                item.CookMethod = CookMethod.Boiled;
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
