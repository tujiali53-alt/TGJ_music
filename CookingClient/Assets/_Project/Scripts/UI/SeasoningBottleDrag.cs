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
        [Tooltip("倒出动画时，瓶子中心比食物上沿高出的距离。单位与界面坐标相同。")]
        [SerializeField] float pourAboveFood = 80f;

        const float PourSeconds = 2f;
        const float PourDip = 50f;

        RectTransform rect;
        Canvas canvas;
        Transform homeParent;
        int homeSibling;
        Vector2 homePosition;
        bool tracking;
        bool pouring;
        float pourTime;
        Vector2 pourOrigin;
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

        void Update()
        {
            if (!pouring || rect == null)
                return;

            pourTime += Time.deltaTime;
            float t = pourTime / PourSeconds;
            if (t >= 1f)
            {
                pouring = false;
                rect.localRotation = Quaternion.identity;
                ReturnHome();
                return;
            }

            SamplePour(t, out float angle, out float dip);
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
            rect.anchoredPosition = pourOrigin + new Vector2(0f, -dip);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (pouring || seasoning == null || string.IsNullOrEmpty(seasoning.id))
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
            IngredientInstance item = FindFood(eventData, out RectTransform food);
            if (item != null)
            {
                if (item.SeasoningIds == null)
                    item.SeasoningIds = new List<string>();

                item.SeasoningIds.Add(seasoning.id);
                AudioManager.PlaySeasoning(seasoning.id);
                BeginPour(food);
                return;
            }

            ReturnHome();
        }

        void BeginPour(RectTransform food)
        {
            if (rect == null)
            {
                ReturnHome();
                return;
            }

            LiftAbove(food);
            pouring = true;
            pourTime = 0f;
            pourOrigin = rect.anchoredPosition;
            rect.localRotation = Quaternion.identity;
        }

        void LiftAbove(RectTransform food)
        {
            if (food == null)
                return;

            Vector3 top = food.TransformPoint(new Vector3(food.rect.center.x, food.rect.yMax, 0f));
            float scale = 1f;
            if (canvas != null)
            {
                float canvasScale = canvas.transform.lossyScale.y;
                if (Mathf.Abs(canvasScale) > 0.0001f)
                    scale = canvasScale;
            }

            Vector3 world = rect.position;
            world.y = top.y + pourAboveFood * scale;
            rect.position = world;
        }

        static void SamplePour(float t, out float angle, out float dip)
        {
            t = Mathf.Clamp01(t);
            if (t < 0.20f)
            {
                angle = Mathf.Lerp(0f, 180f, Smooth(t / 0.20f));
                dip = 0f;
                return;
            }

            if (t < 0.80f)
            {
                angle = 180f;
                dip = PourDip * DownTwice(t);
                return;
            }

            angle = Mathf.Lerp(180f, 0f, Smooth((t - 0.80f) / 0.20f));
            dip = 0f;
        }

        static float DownTwice(float t)
        {
            if (t < 0.32f)
                return Smooth((t - 0.20f) / 0.12f);
            if (t < 0.40f)
                return 1f;
            if (t < 0.50f)
                return 1f - Smooth((t - 0.40f) / 0.10f);
            if (t < 0.62f)
                return Smooth((t - 0.50f) / 0.12f);
            if (t < 0.70f)
                return 1f;

            return 1f - Smooth((t - 0.70f) / 0.10f);
        }

        static float Smooth(float u)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u));
        }

        IngredientInstance FindFood(PointerEventData eventData, out RectTransform food)
        {
            food = null;
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

                bool onBoard = view.GetComponentInParent<CuttingBoard>() != null;
                bool inPot = view.GetComponentInParent<CookingPot>() != null;
                bool onPlate = view.GetComponentInParent<Plate>() != null;
                if (!onBoard && !inPot && !onPlate)
                    continue;

                food = view.transform as RectTransform;
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
            {
                rect.localRotation = Quaternion.identity;
                rect.anchoredPosition = homePosition;
            }
        }
    }
}
