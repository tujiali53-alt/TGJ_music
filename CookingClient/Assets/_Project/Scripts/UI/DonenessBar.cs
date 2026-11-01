using Cooking.Data;
using Cooking.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace Cooking.UI
{
    public sealed class DonenessBar : MonoBehaviour
    {
        // UI_DonenessBar.png 宽 863。绿色从 x=608 到 x=665，下一段从 x=666 开始。
        const float GreenStart = 608f / 863f;
        const float GreenEnd = 666f / 863f;

        Image back;
        Image fill;
        RectTransform cursor;
        float cursorFromX;
        float cursorToX;
        float cursorY;
        FoodSlotView slot;
        PotArea area;

        void Awake()
        {
            back = FindImage("Back");
            fill = FindImage("Fill");
            slot = GetComponentInParent<FoodSlotView>();
            area = GetComponentInParent<PotArea>();
            HideWindow();
            BindCursor();
            if (back == null || back.sprite == null || fill == null || fill.sprite == null || cursor == null || slot == null || area == null)
            {
                Debug.LogError("DonenessBar 需要 Back（UI_DonenessBar01）、Fill（UI_DonenessBar）和 Cursor。", this);
                return;
            }

            back.type = Image.Type.Simple;
            back.preserveAspect = false;
            back.color = Color.white;
            back.raycastTarget = false;
            back.enabled = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.preserveAspect = false;
            fill.color = Color.white;
            fill.enabled = false;
            cursor.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (back == null || fill == null || cursor == null || slot == null || area == null)
                return;

            IngredientInstance item = slot.Shown;
            bool show = item != null && CookingPot.IsHeating(area.Method);
            if (!show)
            {
                back.enabled = false;
                fill.enabled = false;
                cursor.gameObject.SetActive(false);
                return;
            }

            float amount = FillAmount(slot.ShownDefinition, item.Doneness);
            fill.fillAmount = amount;
            back.enabled = true;
            fill.enabled = true;
            cursor.gameObject.SetActive(true);
            cursor.anchoredPosition = new Vector2(Mathf.Lerp(cursorFromX, cursorToX, amount), cursorY);
        }

        public static float FillAmount(IngredientDefinition definition, float doneness)
        {
            if (definition == null || definition.maxDoneness <= 0f)
                return 0f;

            float max = definition.maxDoneness;
            doneness = Mathf.Clamp(doneness, 0f, max);
            float underEnd = Mathf.Clamp(definition.optimalDoneness - DeliciousnessScore.OptimalWindow, 0f, max);
            float overStart = Mathf.Clamp(definition.optimalDoneness + DeliciousnessScore.OptimalWindow, 0f, max);
            if (overStart < underEnd)
                overStart = underEnd;

            if (underEnd > 0f && doneness <= underEnd)
                return Mathf.Lerp(0f, GreenStart, doneness / underEnd);

            if (doneness <= overStart)
            {
                float span = overStart - underEnd;
                float t = span <= 0f ? 1f : (doneness - underEnd) / span;
                return Mathf.Lerp(GreenStart, GreenEnd, t);
            }

            float overSpan = max - overStart;
            float u = overSpan <= 0f ? 1f : (doneness - overStart) / overSpan;
            return Mathf.Lerp(GreenEnd, 1f, u);
        }

        void BindCursor()
        {
            Transform found = transform.Find("Cursor");
            cursor = found as RectTransform;
            if (cursor == null)
                return;

            Image image = found.GetComponent<Image>();
            if (image != null)
                image.raycastTarget = false;

            cursorFromX = cursor.anchoredPosition.x;
            cursorToX = -cursorFromX;
            cursorY = cursor.anchoredPosition.y;
        }

        void HideWindow()
        {
            Transform window = transform.Find("Window");
            if (window != null)
                window.gameObject.SetActive(false);
        }

        Image FindImage(string childName)
        {
            Transform child = transform.Find(childName);
            if (child == null)
                return null;

            return child.GetComponent<Image>();
        }
    }
}
