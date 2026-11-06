using System.Collections;
using System.Collections.Generic;
using Cooking.Core;
using Cooking.Data;
using Cooking.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cooking.UI
{
    public sealed class EndingSequenceController : MonoBehaviour
    {
        [SerializeField] EndingCatalog endingCatalog;
        [SerializeField] DiaryCatalog diaryCatalog;
        [SerializeField] NpcCatalog npcCatalog;
        [SerializeField] Image fadeOverlay;
        [SerializeField] float fadeDuration = 2f;
        [SerializeField] float cgClickDelay = 2f;
        [SerializeField] GameObject gameOverPanel;
        [SerializeField] GameObject dialogPanel;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] TMP_Text nameText;
        [SerializeField] Image profileImage;
        [SerializeField] GameObject endingCg;
        [SerializeField] Image endingCgImage;
        [SerializeField] Button cgClickButton;
        [SerializeField] TMP_Text cgClickText;
        [SerializeField] GameObject endingPanel;
        [SerializeField] Image endingPanelImage;
        [SerializeField] TMP_Text endingText;

        readonly List<EndingLine> queue = new List<EndingLine>();
        EndingDefinition resolved;
        Sprite lastExpression;
        int lineIndex;
        bool running;
        bool dialogActive;
        bool awaitingCgClick;
        Coroutine routine;

        public bool IsDialogActive => dialogActive;

        void Awake()
        {
            ResolveRefs();
            HideChildren();
            if (cgClickButton != null)
                cgClickButton.onClick.AddListener(OnCgClicked);

            GameLanguage.Changed += OnLanguageChanged;
        }

        void OnDestroy()
        {
            GameLanguage.Changed -= OnLanguageChanged;
            if (cgClickButton != null)
                cgClickButton.onClick.RemoveListener(OnCgClicked);
        }

        public void Begin()
        {
            if (running)
                return;

            if (endingCatalog == null)
            {
                Debug.LogError("EndingSequenceController 需要绑定 EndingCatalog。", this);
                return;
            }

            if (fadeOverlay == null)
            {
                Debug.LogError("MainCooking 的 Canvas 下需要已经放好带 Image 的 FadeOverlay。", this);
                return;
            }

            if (gameOverPanel == null)
            {
                Debug.LogError("需要 Canvas/GameOverPanel。", this);
                return;
            }

            ResolveRefs();

            // GameOverPanel 需先激活，协程才能在本组件上跑；子面板仍保持关闭。
            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);
            HideChildren();

            if (routine != null)
                StopCoroutine(routine);

            routine = StartCoroutine(RunSequence());
        }

        public void OnPanelClick(PointerEventData eventData)
        {
            if (!dialogActive || running == false)
                return;

            AdvanceDialog();
        }

        void OnLanguageChanged()
        {
            if (dialogActive)
                ShowCurrentLine();

            if (awaitingCgClick)
                ApplyClickText();

            if (endingPanel != null && endingPanel.activeSelf)
                ApplyEndingPanelTexts();
        }

        IEnumerator RunSequence()
        {
            running = true;
            dialogActive = false;
            awaitingCgClick = false;
            resolved = EndingRules.Resolve(
                GameRoot.Instance != null ? GameRoot.Instance.Session : null,
                endingCatalog,
                diaryCatalog,
                npcCatalog);

            if (resolved == null)
                Debug.LogError("没有命中任何结局，请检查 EndingCatalog 条件。", endingCatalog);

            yield return FadeToBlack();

            BuildQueue();
            lastExpression = null;
            lineIndex = 0;

            if (queue.Count > 0)
            {
                if (dialogPanel != null)
                    dialogPanel.SetActive(true);
                HideOptions();
                dialogActive = true;
                ShowCurrentLine();
                yield return FadeFromBlack();

                while (dialogActive)
                    yield return null;
            }
            else
            {
                if (dialogPanel != null)
                    dialogPanel.SetActive(false);
                yield return FadeFromBlack();
            }

            yield return FadeToBlack();

            if (dialogPanel != null)
                dialogPanel.SetActive(false);

            ShowEndingCg();
            yield return FadeFromBlack();

            float delay = Mathf.Max(0f, cgClickDelay);
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            awaitingCgClick = true;
            if (cgClickButton != null)
            {
                cgClickButton.gameObject.SetActive(true);
                ApplyClickText();
            }

            while (awaitingCgClick)
                yield return null;

            if (endingCg != null)
                endingCg.SetActive(false);

            ShowEndingPanel();
            running = false;
            routine = null;
        }

        void BuildQueue()
        {
            queue.Clear();
            AppendLines(endingCatalog != null ? endingCatalog.commonLines : null);
            AppendLines(resolved != null ? resolved.lines : null);
        }

        void AppendLines(IList<EndingLine> lines)
        {
            if (lines == null)
                return;

            for (int i = 0; i < lines.Count; i++)
            {
                EndingLine line = lines[i];
                if (line == null)
                    continue;

                queue.Add(line);
            }
        }

        void AdvanceDialog()
        {
            if (!dialogActive)
                return;

            lineIndex++;
            if (lineIndex >= queue.Count)
            {
                dialogActive = false;
                return;
            }

            ShowCurrentLine();
        }

        void ShowCurrentLine()
        {
            if (lineIndex < 0 || lineIndex >= queue.Count)
                return;

            EndingLine line = queue[lineIndex];
            if (bodyText != null)
                bodyText.text = EndingCatalog.Pick(GameLanguage.Current, line.textZh, line.textEn);

            if (nameText != null)
            {
                nameText.text = GameLanguage.Current == DiaryLanguage.En
                    ? "Lil Tu"
                    : "小鼻嘎";
            }

            if (line.expression != null)
                lastExpression = line.expression;

            if (profileImage != null)
            {
                profileImage.sprite = lastExpression;
                profileImage.enabled = lastExpression != null;
            }
        }

        void ShowEndingCg()
        {
            if (endingCg != null)
                endingCg.SetActive(true);

            Sprite cg = resolved != null ? resolved.cg : null;
            if (endingCgImage != null)
            {
                endingCgImage.sprite = cg;
                endingCgImage.enabled = cg != null;
                endingCgImage.color = cg != null ? Color.white : Color.black;
            }

            if (cgClickButton != null)
                cgClickButton.gameObject.SetActive(false);
        }

        void OnCgClicked()
        {
            if (!awaitingCgClick)
                return;

            AudioManager.PlayClick();
            awaitingCgClick = false;
        }

        void ShowEndingPanel()
        {
            if (endingPanel != null)
                endingPanel.SetActive(true);

            Sprite cg = resolved != null ? resolved.cg : null;
            if (endingPanelImage != null)
            {
                endingPanelImage.sprite = cg;
                endingPanelImage.enabled = cg != null;
            }

            ApplyEndingPanelTexts();
        }

        void ApplyEndingPanelTexts()
        {
            if (endingText == null)
                return;

            if (resolved == null)
            {
                endingText.text = "";
                return;
            }

            endingText.text = EndingCatalog.Pick(
                GameLanguage.Current,
                resolved.panelTextZh,
                resolved.panelTextEn);
        }

        void ApplyClickText()
        {
            if (cgClickText == null)
                return;

            if (resolved == null)
            {
                cgClickText.text = "";
                return;
            }

            cgClickText.text = EndingCatalog.Pick(
                GameLanguage.Current,
                resolved.clickTextZh,
                resolved.clickTextEn);
        }

        IEnumerator FadeToBlack()
        {
            if (fadeOverlay == null)
                yield break;

            fadeOverlay.gameObject.SetActive(true);
            Color color = fadeOverlay.color;
            color.r = 0f;
            color.g = 0f;
            color.b = 0f;
            color.a = 0f;
            fadeOverlay.color = color;

            float duration = Mathf.Max(0.01f, fadeDuration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                color.a = Mathf.Clamp01(elapsed / duration);
                fadeOverlay.color = color;
                yield return null;
            }

            color.a = 1f;
            fadeOverlay.color = color;
        }

        IEnumerator FadeFromBlack()
        {
            if (fadeOverlay == null)
                yield break;

            fadeOverlay.gameObject.SetActive(true);
            Color color = fadeOverlay.color;
            color.r = 0f;
            color.g = 0f;
            color.b = 0f;
            color.a = 1f;
            fadeOverlay.color = color;

            float duration = Mathf.Max(0.01f, fadeDuration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                color.a = 1f - Mathf.Clamp01(elapsed / duration);
                fadeOverlay.color = color;
                yield return null;
            }

            color.a = 0f;
            fadeOverlay.color = color;
            fadeOverlay.gameObject.SetActive(false);
        }

        void HideChildren()
        {
            if (dialogPanel != null)
                dialogPanel.SetActive(false);
            if (endingCg != null)
                endingCg.SetActive(false);
            if (endingPanel != null)
                endingPanel.SetActive(false);
            if (cgClickButton != null)
                cgClickButton.gameObject.SetActive(false);
            if (fadeOverlay != null)
            {
                Color color = fadeOverlay.color;
                color.a = 0f;
                fadeOverlay.color = color;
                fadeOverlay.gameObject.SetActive(false);
            }

            HideOptions();
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
        }

        void ResolveRefs()
        {
            if (gameOverPanel == null)
                gameOverPanel = gameObject;

            if (dialogPanel == null)
            {
                Transform found = transform.Find("DialogPanel");
                if (found != null)
                    dialogPanel = found.gameObject;
            }

            if (dialogPanel != null)
            {
                Transform player = dialogPanel.transform.Find("PlayerDialogue");
                if (player != null)
                {
                    if (bodyText == null)
                    {
                        Transform found = player.Find("BodyText");
                        if (found != null)
                            bodyText = found.GetComponent<TMP_Text>();
                    }

                    if (nameText == null)
                    {
                        Transform found = player.Find("UserNameText");
                        if (found != null)
                            nameText = found.GetComponent<TMP_Text>();
                    }

                    if (profileImage == null)
                    {
                        Transform found = player.Find("PlayerProfileImage");
                        if (found != null)
                            profileImage = found.GetComponent<Image>();
                    }
                }

                NightDialogClick click = dialogPanel.GetComponent<NightDialogClick>();
                if (click != null)
                    click.BindEnding(this);
            }

            if (endingCg == null)
            {
                Transform found = transform.Find("endingCG");
                if (found != null)
                    endingCg = found.gameObject;
            }

            if (endingCg != null)
            {
                if (endingCgImage == null)
                {
                    Transform found = endingCg.transform.Find("EndingImage");
                    if (found != null)
                        endingCgImage = found.GetComponent<Image>();
                }

                if (cgClickButton == null)
                {
                    Transform found = endingCg.transform.Find("ClickButton");
                    if (found != null)
                        cgClickButton = found.GetComponent<Button>();
                }

                if (cgClickText == null && cgClickButton != null)
                {
                    TMP_Text tmp = cgClickButton.GetComponentInChildren<TMP_Text>(true);
                    if (tmp != null)
                        cgClickText = tmp;
                }
            }

            if (endingPanel == null)
            {
                Transform found = transform.Find("EndingPanel");
                if (found != null)
                    endingPanel = found.gameObject;
            }

            if (endingPanel != null)
            {
                if (endingPanelImage == null)
                {
                    Transform found = endingPanel.transform.Find("endingImage");
                    if (found != null)
                        endingPanelImage = found.GetComponent<Image>();
                }

                if (endingText == null)
                {
                    Transform found = endingPanel.transform.Find("endingText");
                    if (found != null)
                        endingText = found.GetComponent<TMP_Text>();
                }
            }

            if (fadeOverlay == null)
            {
                Transform canvas = transform.parent;
                if (canvas != null)
                {
                    Transform found = canvas.Find("FadeOverlay");
                    if (found != null)
                        fadeOverlay = found.GetComponent<Image>();
                }
            }
        }
    }
}
