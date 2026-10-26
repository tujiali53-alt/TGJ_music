using System.Collections.Generic;
using Cooking.Data;
using Cooking.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace Cooking.UI
{
    public sealed class CuttingBoard : MonoBehaviour, IFoodStation
    {
        public const int Capacity = 8;

        [SerializeField] IngredientDefinition[] definitions;

        static CuttingBoard current;

        readonly List<IngredientInstance> held = new List<IngredientInstance>();
        FoodTokens tokens;
        Button cutButton;

        void Awake()
        {
            current = this;
            tokens = new FoodTokens(transform);
            tokens.Collect();
            if (tokens.TokenCount == 0)
                Debug.LogError("CuttingBoard 下需要已经摆好的食物令牌。", this);

            cutButton = SceneButtons.Find("CuttingButton");
            if (cutButton != null)
                cutButton.onClick.AddListener(CutHeld);
        }

        void OnDestroy()
        {
            if (cutButton != null)
                cutButton.onClick.RemoveListener(CutHeld);
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

            IngredientDefinition resolved = definition != null ? definition : FindDefinition(item.DefinitionId);
            if (resolved == null || !resolved.canCut)
                return false;

            if (held.Count >= Capacity || !tokens.HasFree())
                return false;

            Plate.Detach(item);
            CookingPot.Detach(item);
            IngredientInventory.Remove(item);
            held.Add(item);
            tokens.Present(item, localPoint, resolved);
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

        public void CutHeld()
        {
            for (int i = 0; i < held.Count; i++)
            {
                IngredientInstance item = held[i];
                if (item == null || item.Shape != CutShape.Whole)
                    continue;

                IngredientDefinition definition = FindDefinition(item.DefinitionId);
                if (definition == null || !definition.canCut)
                    continue;

                item.Shape = CutShape.Slice;
            }
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
