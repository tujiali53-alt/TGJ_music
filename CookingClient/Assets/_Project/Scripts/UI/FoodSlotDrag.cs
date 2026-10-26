using System.Collections.Generic;
using Cooking.Data;
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
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            FoodSlotView view = GetComponent<FoodSlotView>();
            if (view == null || view.Shown == null)
                return;

            tracking = true;
            IsDragging = true;
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
            IsDragging = false;
            FoodSlotView view = GetComponent<FoodSlotView>();
            IngredientInstance item = view != null ? view.Shown : null;
            IFoodStation station = FindStation(eventData);
            bool fromInventory = view != null && view.InventorySlot;

            if (fromInventory)
            {
                PlaceFromInventory(item, view, station, eventData);
                ReturnToTray();
                return;
            }

            if (station is Plate plate && LocalOn(plate, eventData, out Vector2 local))
            {
                plate.TryPlace(item, local, view != null ? view.ShownDefinition : null);
                CookingPot.Forget(item);
                return;
            }

            Plate.ReleaseToInventory(item);
            if (station != null && !(station is Plate))
                station.TryPlace(item);

            if (!(station is CookingPot))
                CookingPot.Forget(item);

            ReturnToTray();
            if (view != null)
                view.ShowStack(null, 0, null);
        }

        void PlaceFromInventory(IngredientInstance item, FoodSlotView view, IFoodStation station, PointerEventData eventData)
        {
            bool placedOnPlate = false;
            if (station is Plate plate && LocalOn(plate, eventData, out Vector2 local))
                placedOnPlate = plate.TryPlace(item, local, view != null ? view.ShownDefinition : null);
            else if (station != null)
                station.TryPlace(item);

            if (placedOnPlate || !(station is CookingPot))
                CookingPot.Forget(item);
        }

        bool LocalOn(Plate plate, PointerEventData eventData, out Vector2 local)
        {
            local = Vector2.zero;
            RectTransform plateRect = plate.transform as RectTransform;
            if (plateRect == null)
                return false;

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                plateRect,
                eventData.position,
                eventData.pressEventCamera,
                out local);
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

        public void ReturnToTray()
        {
            IsDragging = false;
            tracking = false;
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
