using System.Collections.Generic;
using Cooking.Data;
using Cooking.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cooking.UI
{
    public sealed class SeasoningBottleDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] SeasoningDefinition seasoning;

        RectTransform rect;
        Canvas canvas;
        Transform homeParent;
        int homeSibling;
        Vector2 homePosition;
        bool tracking;
        readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

        void Awake()
        {
            rect = (RectTransform)transform;
            homeParent = transform.parent;
            homeSibling = transform.GetSiblingIndex();
            homePosition = rect.anchoredPosition;
            canvas = GetComponentInParent<Canvas>();
            if (seasoning == null || string.IsNullOrEmpty(seasoning.id))
                Debug.LogError("调料瓶需要绑定调味品。", this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (seasoning == null || string.IsNullOrEmpty(seasoning.id))
                return;

            tracking = true;
            if (canvas == null)
                return;

            transform.SetParent(canvas.transform, true);
            transform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!tracking || rect == null)
                return;

            float scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
            rect.anchoredPosition += eventData.delta / scale;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!tracking)
                return;

            tracking = false;
            IngredientInstance item = FindFood(eventData);
            if (item != null)
            {
                if (item.SeasoningIds == null)
                    item.SeasoningIds = new List<string>();

                item.SeasoningIds.Add(seasoning.id);
            }

            ReturnHome();
        }

        IngredientInstance FindFood(PointerEventData eventData)
        {
            if (EventSystem.current == null)
            {
                Debug.LogError("场景里需要有 EventSystem。", this);
                return null;
            }

            raycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, raycastResults);
            for (int i = 0; i < raycastResults.Count; i++)
            {
                GameObject hit = raycastResults[i].gameObject;
                if (hit == null || hit == gameObject || hit.transform.IsChildOf(transform))
                    continue;

                FoodSlotView view = hit.GetComponentInParent<FoodSlotView>();
                if (view == null || view.InventorySlot || view.Shown == null)
                    continue;

                if (view.GetComponentInParent<CuttingBoard>() == null && view.GetComponentInParent<CookingPot>() == null)
                    continue;

                return view.Shown;
            }

            return null;
        }

        void ReturnHome()
        {
            if (homeParent != null)
            {
                transform.SetParent(homeParent, false);
                int sibling = homeSibling;
                if (sibling >= homeParent.childCount)
                    sibling = homeParent.childCount - 1;
                if (sibling < 0)
                    sibling = 0;
                transform.SetSiblingIndex(sibling);
            }

            if (rect != null)
                rect.anchoredPosition = homePosition;
        }
    }
}
