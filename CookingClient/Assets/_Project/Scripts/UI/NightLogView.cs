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
        [SerializeField] NightLogCatalog catalog;

        static NightLogView instance;
        string shown;
        DiaryLanguage shownLanguage;

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

            if (catalog == null)
                Debug.LogError("NightLogView 需要绑定 NightLogCatalog。", this);

            if (panel != null)
                panel.SetActive(false);

            shown = null;
        }

        void OnEnable()
        {
            GameLanguage.Changed += OnLanguageChanged;
            shown = null;
            Refresh();
        }

        void OnDisable()
        {
            GameLanguage.Changed -= OnLanguageChanged;
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

        void OnLanguageChanged()
        {
            shown = null;
            Refresh();
        }

        public static void Append(NightLogKind kind, string id, params string[] args)
        {
            Append(kind, id, args, args);
        }

        public static void Append(NightLogKind kind, string id, string[] argsZh, string[] argsEn)
        {
            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null || root.Session.NightLog == null)
            {
                Debug.LogError("GameSession 缺少今夜记录。");
                return;
            }

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("NightLog 缺少 id。");
                return;
            }

            root.Session.NightLog.Add(new NightLogEntry
            {
                Kind = kind,
                Id = id,
                ArgsZh = CopyArgs(argsZh),
                ArgsEn = CopyArgs(argsEn)
            });

            if (instance != null)
                instance.Refresh();
        }

        public void Refresh()
        {
            if (logText == null || panel == null)
                return;

            DiaryLanguage language = GameLanguage.Current;
            GameRoot root = GameRoot.Instance;
            string next = "";
            if (root != null && root.Session != null && root.Session.NightLog != null)
            {
                for (int i = 0; i < root.Session.NightLog.Count; i++)
                {
                    NightLogEntry entry = root.Session.NightLog[i];
                    string line = FormatEntry(entry, language);
                    if (string.IsNullOrEmpty(line))
                        continue;

                    if (next.Length > 0)
                        next += "\n";
                    next += line;
                }
            }

            bool hasText = next.Length > 0;
            if (panel.activeSelf != hasText)
                panel.SetActive(hasText);

            if (!hasText || (next == shown && language == shownLanguage))
                return;

            shown = next;
            shownLanguage = language;
            logText.text = next;
            if (scroll != null && scroll.content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            if (scroll != null)
                scroll.verticalNormalizedPosition = 0f;
        }

        string FormatEntry(NightLogEntry entry, DiaryLanguage language)
        {
            if (entry == null || string.IsNullOrEmpty(entry.Id))
                return "";

            if (catalog == null)
                return entry.Id;

            string[] args = language == DiaryLanguage.En ? entry.ArgsEn : entry.ArgsZh;
            if (args == null || args.Length == 0)
                args = entry.ArgsZh ?? entry.ArgsEn;

            string text = catalog.Format(entry.Id, language, args);
            return NightLogCatalog.WrapColor(text, catalog.ColorHex(entry.Id));
        }

        static string[] CopyArgs(string[] source)
        {
            if (source == null || source.Length == 0)
                return null;

            var copy = new string[source.Length];
            for (int i = 0; i < source.Length; i++)
                copy[i] = source[i] ?? "";
            return copy;
        }
    }
}
