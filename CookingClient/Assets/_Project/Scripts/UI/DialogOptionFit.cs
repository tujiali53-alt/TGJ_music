using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cooking.UI
{
    public static class DialogOptionFit
    {
        const float MinWidth = 236f;
        const float MaxWidth = 520f;
        const float MinHeight = 97f;
        const float MaxHeight = 180f;
        const float HorizontalPad = 32f;
        const float VerticalChrome = 60f;
        const float StackGap = 8f;

        struct Baseline
        {
            public Vector2 size;
            public float rightEdge;
            public float topEdge;
        }

        static readonly Dictionary<int, Baseline> baselines = new Dictionary<int, Baseline>();

        public static void Apply(Button button, TMP_Text label, TMP_Text reward, bool rewardVisible)
        {
            if (button == null)
                return;

            RectTransform root = button.transform as RectTransform;
            if (root == null)
                return;

            EnsureBaseline(root);

            if (label != null)
            {
                label.enableWordWrapping = true;
                label.overflowMode = TextOverflowModes.Overflow;
            }

            float contentWidth = 0f;
            if (label != null && !string.IsNullOrEmpty(label.text))
            {
                Vector2 preferred = label.GetPreferredValues(label.text);
                contentWidth = preferred.x;
            }

            if (rewardVisible && reward != null && reward.gameObject.activeSelf && !string.IsNullOrEmpty(reward.text))
            {
                Vector2 preferred = reward.GetPreferredValues(reward.text);
                if (preferred.x > contentWidth)
                    contentWidth = preferred.x;
            }

            float width = Mathf.Clamp(contentWidth + HorizontalPad, MinWidth, MaxWidth);
            float labelHeight = 0f;
            if (label != null && !string.IsNullOrEmpty(label.text))
            {
                float measureWidth = Mathf.Max(1f, width - HorizontalPad);
                label.ForceMeshUpdate();
                labelHeight = label.GetPreferredValues(label.text, measureWidth, 0f).y;
            }

            float height = Mathf.Clamp(labelHeight + VerticalChrome, MinHeight, MaxHeight);
            SetSizeKeepRight(root, width, height);
        }

        public static void StackVisible(Button[] buttons)
        {
            if (buttons == null)
                return;

            float? top = null;
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null || !button.gameObject.activeSelf)
                    continue;

                RectTransform root = button.transform as RectTransform;
                if (root == null)
                    continue;

                EnsureBaseline(root);
                Baseline baseline = baselines[root.GetInstanceID()];
                float height = root.sizeDelta.y > 0f ? root.sizeDelta.y : baseline.size.y;
                if (top == null)
                    top = baseline.topEdge;

                float pivotY = root.pivot.y;
                Vector2 pos = root.anchoredPosition;
                pos.y = top.Value - (1f - pivotY) * height;
                root.anchoredPosition = pos;
                top = pos.y - pivotY * height - StackGap;
            }
        }

        static void EnsureBaseline(RectTransform root)
        {
            int id = root.GetInstanceID();
            if (baselines.ContainsKey(id))
                return;

            float width = root.rect.width > 1f ? root.rect.width : root.sizeDelta.x;
            float height = root.rect.height > 1f ? root.rect.height : root.sizeDelta.y;
            if (width < 1f)
                width = MinWidth;
            if (height < 1f)
                height = MinHeight;

            baselines[id] = new Baseline
            {
                size = new Vector2(width, height),
                rightEdge = root.anchoredPosition.x + (1f - root.pivot.x) * width,
                topEdge = root.anchoredPosition.y + (1f - root.pivot.y) * height
            };
        }

        static void SetSizeKeepRight(RectTransform root, float width, float height)
        {
            Baseline baseline = baselines[root.GetInstanceID()];
            root.sizeDelta = new Vector2(width, height);
            Vector2 pos = root.anchoredPosition;
            pos.x = baseline.rightEdge - (1f - root.pivot.x) * width;
            root.anchoredPosition = pos;
        }
    }
}
