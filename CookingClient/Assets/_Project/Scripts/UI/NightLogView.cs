using Cooking.Core;
using Cooking.Data;
using Cooking.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cooking.UI
{
    public sealed class NightLogView : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text logText;
        [SerializeField] ScrollRect scroll;

        static NightLogView instance;
        string shown;

        void Awake()
        {
            instance = this;

            if (panel == null)
            {
                Transform found = transform.Find("RealTimeInformation");
                if (found != null)
                    panel = found.gameObject;
            }

            if (panel != null && logText == null)
            {
                Transform found = panel.transform.Find("ScrollRect/Viewport/Content/LogText");
                if (found != null)
                    logText = found.GetComponent<TMP_Text>();
            }

            if (panel != null && scroll == null)
            {
                Transform found = panel.transform.Find("ScrollRect");
                if (found != null)
                    scroll = found.GetComponent<ScrollRect>();
            }

            if (panel == null)
                Debug.LogError("InteractionPanel 下需要有 RealTimeInformation。", this);

            if (logText == null)
                Debug.LogError("RealTimeInformation 下需要有 ScrollRect/Viewport/Content/LogText。", this);

            if (scroll == null)
                Debug.LogError("RealTimeInformation 下需要有 ScrollRect。", this);

            if (panel != null)
                panel.SetActive(false);

            shown = null;
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        void LateUpdate()
        {
            Refresh();
        }

        public static void Append(NightLogKind kind, string text)
        {
            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null || root.Session.NightLog == null)
            {
                Debug.LogError("GameSession 缺少今夜记录。");
                return;
            }

            root.Session.NightLog.Add(new NightLogEntry
            {
                Kind = kind,
                Text = text
            });

            if (instance != null)
                instance.Refresh();
        }

        public void Refresh()
        {
            if (logText == null || panel == null)
                return;

            GameRoot root = GameRoot.Instance;
            string next = "";
            if (root != null && root.Session != null && root.Session.NightLog != null)
            {
                for (int i = 0; i < root.Session.NightLog.Count; i++)
                {
                    NightLogEntry entry = root.Session.NightLog[i];
                    if (entry == null || string.IsNullOrEmpty(entry.Text))
                        continue;

                    if (next.Length > 0)
                        next += "\n";
                    next += entry.Text;
                }
            }

            bool hasText = next.Length > 0;
            if (panel.activeSelf != hasText)
                panel.SetActive(hasText);

            if (!hasText || next == shown)
                return;

            shown = next;
            logText.text = next;
            if (scroll != null && scroll.content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            if (scroll != null)
                scroll.verticalNormalizedPosition = 0f;
        }
    }
}
