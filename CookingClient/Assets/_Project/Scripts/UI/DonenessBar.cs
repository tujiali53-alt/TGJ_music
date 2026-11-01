using Cooking.Data;
using Cooking.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace Cooking.UI
{
    public sealed class DonenessBar : MonoBehaviour
    {
        Image fill;
        FoodSlotView slot;
        PotArea area;

        void Awake()
        {
            fill = FindImage("Fill");
            slot = GetComponentInParent<FoodSlotView>();
            area = GetComponentInParent<PotArea>();
            HideGeneratedPieces();
            if (fill == null || fill.sprite == null || slot == null || area == null)
            {
                Debug.LogError("DonenessBar 的 Fill 需要指定 UI_DonenessBar。", this);
                return;
            }

            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.color = Color.white;
            fill.enabled = false;
        }

        void LateUpdate()
        {
            if (fill == null || slot == null || area == null)
                return;

            IngredientInstance item = slot.Shown;
            bool show = item != null && CookingPot.IsHeating(area.Method);
            if (!show)
            {
                fill.enabled = false;
                return;
            }

            IngredientDefinition definition = slot.ShownDefinition;
            float max = definition != null ? definition.maxDoneness : 0f;
            fill.fillAmount = max <= 0f ? 0f : Mathf.Clamp01(item.Doneness / max);
            fill.enabled = true;
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
