using UnityEngine;
using UnityEngine.EventSystems;

namespace Cooking.UI
{
    public sealed class FoodSlotDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public bool IsDragging { get; private set; }

        RectTransform rect;
        Canvas canvas;
        Vector2 homePosition;

        void Awake()
        {
            rect = (RectTransform)transform;
            homePosition = rect.anchoredPosition;
            canvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            IsDragging = true;
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
            if (rect != null)
                rect.anchoredPosition = homePosition;
        }
    }
}
