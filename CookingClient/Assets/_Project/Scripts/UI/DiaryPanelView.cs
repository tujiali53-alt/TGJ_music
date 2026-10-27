using System.Collections.Generic;
using Cooking.Core;
using Cooking.Data;
using Cooking.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cooking.UI
{
    public sealed class DiaryPanelView : MonoBehaviour
    {
        [SerializeField] DiaryCatalog catalog;
        [SerializeField] DiaryLanguage language = DiaryLanguage.Zh;
        [SerializeField] TMP_Text dayText;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text leftText;
        [SerializeField] TMP_Text rightText;
        [SerializeField] Button previousButton;
        [SerializeField] Button nextButton;

        int pageIndex;

        public DiaryCatalog Catalog => catalog;

        void Awake()
        {
            Resolve();

            if (previousButton == null)
                Debug.LogError("需要 DiaryPanel/PreviousPageButton，并且带 Button。", this);
            else
                previousButton.onClick.AddListener(ShowPrevious);

            if (nextButton == null)
                Debug.LogError("需要 DiaryPanel/NextPageButton，并且带 Button。", this);
            else
                nextButton.onClick.AddListener(ShowNext);
        }

        void OnEnable()
        {
            Resolve();
            ShowOpeningPage();
        }

        void OnDestroy()
        {
            if (previousButton != null)
                previousButton.onClick.RemoveListener(ShowPrevious);

            if (nextButton != null)
                nextButton.onClick.RemoveListener(ShowNext);
        }

        public void ShowPrevious()
        {
            if (pageIndex <= 0)
                return;

            pageIndex--;
            Apply(CurrentPages(), false);
        }

        public void ShowNext()
        {
            List<DiaryEntry> pages = CurrentPages();
            if (pageIndex >= pages.Count - 1)
                return;

            pageIndex++;
            Apply(pages, false);
        }

        void ShowOpeningPage()
        {
            List<DiaryEntry> pages = CurrentPages();
            GameSession session = Session();
            pageIndex = DiaryBook.FirstUnreadIndex(pages, session != null ? session.DiarySeenIds : null);
            Apply(pages, true);
        }

        void Apply(List<DiaryEntry> pages, bool markSeen)
        {
            if (catalog == null)
            {
                Debug.LogError("DiaryPanel 需要绑上 DiaryCatalog。", this);
                ClearPage();
                return;
            }

            GameSession session = Session();
            if (session == null)
            {
                Debug.LogError("需要 GameRoot 上的 GameSession。", this);
                ClearPage();
                return;
            }

            if (pages == null || pages.Count == 0)
            {
                pageIndex = 0;
                ClearPage();
                return;
            }

            if (pageIndex < 0)
                pageIndex = 0;
            if (pageIndex >= pages.Count)
                pageIndex = pages.Count - 1;

            DiaryEntry entry = pages[pageIndex];
            if (dayText != null)
                dayText.text = DiaryBook.DayLine(catalog, entry.storyDay, language);
            if (titleText != null)
                titleText.text = DiaryBook.Title(catalog, entry, language);
            if (leftText != null)
                leftText.text = DiaryBook.Join(entry, DiarySide.Left, language);
            if (rightText != null)
                rightText.text = DiaryBook.Join(entry, DiarySide.Right, language);

            if (previousButton != null)
                previousButton.interactable = pageIndex > 0;
            if (nextButton != null)
                nextButton.interactable = pageIndex < pages.Count - 1;

            if (!markSeen)
                return;

            session.RememberDiarySeen(pages);
            DiaryNotice notice = FindNotice();
            if (notice != null)
                notice.Refresh();
        }

        void ClearPage()
        {
            if (dayText != null)
                dayText.text = "";
            if (titleText != null)
                titleText.text = "";
            if (leftText != null)
                leftText.text = "";
            if (rightText != null)
                rightText.text = "";
            if (previousButton != null)
                previousButton.interactable = false;
            if (nextButton != null)
                nextButton.interactable = false;
        }

        List<DiaryEntry> CurrentPages()
        {
            return DiaryBook.Visible(catalog, Session());
        }

        void Resolve()
        {
            Transform area = transform.Find("TextArea");
            if (area == null)
                Debug.LogError("DiaryPanel 下需要有 TextArea。", this);

            if (dayText == null)
                dayText = FindText(area, "DayText");
            if (titleText == null)
                titleText = FindText(area, "TitleText");
            if (leftText == null)
                leftText = FindText(area, "MainText_left");
            if (rightText == null)
                rightText = FindText(area, "MainText_right");

            if (previousButton == null)
                previousButton = FindButton("PreviousPageButton");
            if (nextButton == null)
                nextButton = FindButton("NextPageButton");

            if (dayText == null || titleText == null || leftText == null || rightText == null)
                Debug.LogError("DiaryPanel/TextArea 下需要 DayText、TitleText、MainText_left、MainText_right。", this);
        }

        static TMP_Text FindText(Transform area, string childName)
        {
            if (area == null)
                return null;

            Transform found = area.Find(childName);
            if (found == null)
                return null;

            return found.GetComponent<TMP_Text>();
        }

        Button FindButton(string childName)
        {
            Transform found = transform.Find(childName);
            if (found == null)
                return null;

            return found.GetComponent<Button>();
        }

        DiaryNotice FindNotice()
        {
            Transform canvas = transform.parent;
            if (canvas == null)
                return null;

            Transform panel = canvas.Find("InteractionPanel");
            if (panel == null)
                return null;

            return panel.GetComponent<DiaryNotice>();
        }

        static GameSession Session()
        {
            GameRoot root = GameRoot.Instance;
            if (root == null)
                return null;

            return root.Session;
        }
    }
}
