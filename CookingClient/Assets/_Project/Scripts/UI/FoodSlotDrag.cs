using System.Collections.Generic;
using Cooking.Core;
using Cooking.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cooking.UI
{
    public sealed class FoodSlotDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public bool IsDragging { get; private set; }

        RectTransform rect;
        Canvas canvas;
        Transform homeParent;
        Transform draggedFrom;
        Vector2 homePosition;
        readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

        void Awake()
        {
            rect = (RectTransform)transform;
            homeParent = transform.parent;
            homePosition = rect.anchoredPosition;
            canvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            IsDragging = true;
            draggedFrom = transform.parent;
            if (canvas == null)
                return;

            transform.SetParent(canvas.transform, true);
            transform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (rect == null)
                return;

            float scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
            rect.anchoredPosition += eventData.delta / scale;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            IsDragging = false;
            IngredientInstance item = ItemFrom(draggedFrom);
            IFoodStation station = FindStation(eventData);
            if (!(station is Plate))
                Plate.ReleaseToInventory(item);

            if (station != null && station.TryPlace(item))
                SitOn(((Component)station).transform);
            else
                ReturnHome();
        }

        IFoodStation FindStation(PointerEventData eventData)
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
                if (hit == null || hit.transform.IsChildOf(transform) || hit == gameObject)
                    continue;

                IFoodStation station = hit.GetComponentInParent<IFoodStation>();
                if (station != null)
                    return station;
            }

            return null;
        }

        IngredientInstance ItemFrom(Transform source)
        {
            if (source != null && source.GetComponent<Plate>() != null)
                return Plate.FirstHeld();

            return FirstItem();
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

        void SitOn(Transform board)
        {
            transform.SetParent(board, false);
            if (rect != null)
                rect.anchoredPosition = Vector2.zero;
        }

        public void ReturnToTray()
        {
            IsDragging = false;
            if (homeParent != null)
                transform.SetParent(homeParent, false);

            if (rect != null)
                rect.anchoredPosition = homePosition;
        }

        void ReturnHome()
        {
            ReturnToTray();
        }
    }
}
