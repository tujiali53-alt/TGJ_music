using Cooking.Data;
using Cooking.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace Cooking.UI
{
    public sealed class DonenessBar : MonoBehaviour
    {
        Image fill;
        RectTransform cursor;
        float cursorFromX;
        float cursorToX;
        float cursorY;
        FoodSlotView slot;
        PotArea area;

        void Awake()
        {
            fill = FindImage("Fill");
            slot = GetComponentInParent<FoodSlotView>();
            area = GetComponentInParent<PotArea>();
            HideGeneratedPieces();
            BindCursor();
            if (fill == null || fill.sprite == null || cursor == null || slot == null || area == null)
            {
                Debug.LogError("DonenessBar 需要 Fill（UI_DonenessBar）和 Cursor。", this);
                return;
            }

            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.color = Color.white;
            fill.enabled = false;
            cursor.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (fill == null || cursor == null || slot == null || area == null)
                return;

            IngredientInstance item = slot.Shown;
            bool show = item != null && CookingPot.IsHeating(area.Method);
            if (!show)
            {
                fill.enabled = false;
                cursor.gameObject.SetActive(false);
                return;
            }

            IngredientDefinition definition = slot.ShownDefinition;
            float max = definition != null ? definition.maxDoneness : 0f;
            float amount = max <= 0f ? 0f : Mathf.Clamp01(item.Doneness / max);
            fill.fillAmount = amount;
            fill.enabled = true;
            cursor.gameObject.SetActive(true);
            cursor.anchoredPosition = new Vector2(Mathf.Lerp(cursorFromX, cursorToX, amount), cursorY);
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

        void HideGeneratedPieces()
        {
            Transform track = transform.Find("Track");
            Transform window = transform.Find("Window");
            if (track != null)
                track.gameObject.SetActive(false);
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
