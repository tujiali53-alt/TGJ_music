using System.Collections.Generic;
using Cooking.Data;
using Cooking.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cooking.UI
{
    public sealed class FoodSlotDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public bool IsDragging { get; private set; }

        RectTransform rect;
        Canvas canvas;
        Image background;
        Transform homeParent;
        int homeSibling;
        Vector2 homePosition;
        Vector2 heldPosition;
        bool tracking;
        bool restoreBackground;
        readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

        void Awake()
        {
            rect = (RectTransform)transform;
            homeParent = transform.parent;
            homeSibling = transform.GetSiblingIndex();
            homePosition = rect.anchoredPosition;
            canvas = GetComponentInParent<Canvas>();
            background = GetComponent<Image>();
            FoodSlotView view = GetComponent<FoodSlotView>();
            restoreBackground = view == null || view.InventorySlot;
            if (!restoreBackground && background != null)
                background.enabled = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            FoodSlotView view = GetComponent<FoodSlotView>();
            if (view == null || view.Shown == null)
                return;

            tracking = true;
            IsDragging = true;
            heldPosition = rect != null ? rect.anchoredPosition : Vector2.zero;
            if (background != null)
                background.enabled = false;
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

            IngredientDefinition definition = view != null ? view.ShownDefinition : null;
            if (fromInventory)
            {
                Place(item, definition, station, eventData);
                ReturnToTray();
                return;
            }

            if (station == null)
            {
                Plate.ReleaseToInventory(item);
                CuttingBoard.ReleaseToInventory(item);
                CookingPot.ReleaseToInventory(item);
                ReturnToTray();
                if (view != null)
                    view.ShowStack(null, 0, null);
                return;
            }

            if (!Place(item, definition, station, eventData))
                Settle(heldPosition, false);
        }

        bool Place(IngredientInstance item, IngredientDefinition definition, IFoodStation station, PointerEventData eventData)
        {
            if (station is Plate plate && LocalOn(plate.transform, eventData, out Vector2 platePoint))
                return plate.TryPlace(item, platePoint, definition);

            if (station is CuttingBoard board && LocalOn(board.transform, eventData, out Vector2 boardPoint))
                return board.TryPlace(item, boardPoint, definition);

            PotArea area = FindPotArea(eventData);
            if (area != null && LocalOn(area.transform, eventData, out Vector2 areaPoint))
                return CookingPot.PlaceOn(area, item, areaPoint, definition);

            return false;
        }

        static bool LocalOn(Transform target, PointerEventData eventData, out Vector2 local)
        {
            local = Vector2.zero;
            RectTransform targetRect = target as RectTransform;
            if (targetRect == null)
                return false;

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetRect,
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

        PotArea FindPotArea(PointerEventData eventData)
        {
            if (EventSystem.current == null)
                return null;

            raycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, raycastResults);
            for (int i = 0; i < raycastResults.Count; i++)
            {
                GameObject hit = raycastResults[i].gameObject;
                if (hit == null || hit.transform.IsChildOf(transform) || hit == gameObject)
                    continue;

                PotArea area = hit.GetComponentInParent<PotArea>();
                if (area != null)
                    return area;
            }

            return null;
        }

        public void ReturnToTray()
        {
            Settle(homePosition, true);
        }

        void Settle(Vector2 position, bool restSibling)
        {
            IsDragging = false;
            tracking = false;
            if (homeParent != null)
            {
                transform.SetParent(homeParent, false);
                if (restSibling)
                {
                    int sibling = homeSibling;
                    if (sibling >= homeParent.childCount)
                        sibling = homeParent.childCount - 1;
                    if (sibling < 0)
                        sibling = 0;
                    transform.SetSiblingIndex(sibling);
                }
                else
                    transform.SetAsLastSibling();
            }

            if (rect != null)
                rect.anchoredPosition = position;

            if (background != null)
                background.enabled = restoreBackground;
        }
    }
}
