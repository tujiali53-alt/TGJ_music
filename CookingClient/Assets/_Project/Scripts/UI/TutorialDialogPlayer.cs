using System;
using System.Collections.Generic;
using Cooking.Core;
using Cooking.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cooking.UI
{
    public sealed class TutorialDialogPlayer : MonoBehaviour
    {
        [SerializeField] GameObject interactionPanel;
        [SerializeField] GameObject dialogPanel;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] TMP_Text nameText;
        [SerializeField] Image profileImage;

        readonly List<TutorialLine> queue = new List<TutorialLine>();
        Sprite lastExpression;
        int lineIndex;
        bool active;
        Action onComplete;

        public bool IsActive => active;

        public void Bind(GameObject interaction, GameObject dialog, TMP_Text body, TMP_Text name, Image profile)
        {
            interactionPanel = interaction;
            dialogPanel = dialog;
            bodyText = body;
            nameText = name;
            profileImage = profile;
        }

        public void Play(IList<TutorialLine> lines, Action completed)
        {
            queue.Clear();
            if (lines != null)
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i] != null)
                        queue.Add(lines[i]);
                }
            }

            onComplete = completed;
            lineIndex = 0;
            lastExpression = null;
            active = true;

            if (interactionPanel != null)
                interactionPanel.SetActive(true);

            if (dialogPanel != null)
            {
                dialogPanel.SetActive(true);
                HideOptions();
            }

            NightDialogClick click = dialogPanel != null
                ? dialogPanel.GetComponent<NightDialogClick>()
                : null;
            if (click != null)
                click.BindTutorial(this);

            GameLanguage.Changed -= OnLanguageChanged;
            GameLanguage.Changed += OnLanguageChanged;

            if (queue.Count == 0)
            {
                Finish();
                return;
            }

            ShowCurrent();
        }

        public void OnPanelClick(PointerEventData eventData)
        {
            if (!active)
                return;

            lineIndex++;
            if (lineIndex >= queue.Count)
            {
                Finish();
                return;
            }

            ShowCurrent();
        }

        void OnDestroy()
        {
            GameLanguage.Changed -= OnLanguageChanged;
        }

        void OnLanguageChanged()
        {
            if (active)
                ShowCurrent();
        }

        void ShowCurrent()
        {
            if (lineIndex < 0 || lineIndex >= queue.Count)
                return;

            TutorialLine line = queue[lineIndex];
            if (bodyText != null)
                bodyText.text = TutorialCatalog.Pick(GameLanguage.Current, line.textZh, line.textEn);

            if (nameText != null)
            {
                nameText.text = GameLanguage.Current == DiaryLanguage.En
                    ? "Lil Tu"
                    : "小鼻嘎";
            }

            // expressionId 对应 PlayerProfileImage；空单元格沿用上一句表情。
            if (line.expression != null)
                lastExpression = line.expression;

            if (profileImage != null)
            {
                profileImage.gameObject.SetActive(true);
                if (lastExpression != null)
                {
                    profileImage.sprite = lastExpression;
                    profileImage.enabled = true;
                    profileImage.color = Color.white;
                }
            }
        }

        void Finish()
        {
            active = false;
            GameLanguage.Changed -= OnLanguageChanged;
            if (dialogPanel != null)
                dialogPanel.SetActive(false);

            Action done = onComplete;
            onComplete = null;
            if (done != null)
                done();
        }

        void HideOptions()
        {
            if (dialogPanel == null)
                return;

            for (int i = 1; i <= 3; i++)
            {
                Transform found = dialogPanel.transform.Find("Option" + i);
                if (found != null)
                    found.gameObject.SetActive(false);
            }

            Transform judgment = dialogPanel.transform.Find("DialogueJudgment");
            if (judgment != null)
                judgment.gameObject.SetActive(false);
        }
    }
}
