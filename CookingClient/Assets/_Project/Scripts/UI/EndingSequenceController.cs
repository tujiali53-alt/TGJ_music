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
        [SerializeField] float cgClickDelay = 3f;
        [SerializeField] float clickFadeDuration = 0.5f;
        [SerializeField] float endingImageHoldDuration = 3f;
        [SerializeField] GameObject gameOverPanel;
        [SerializeField] GameObject dialogPanel;
        [SerializeField] GameObject metaPanel;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] TMP_Text nameText;
        [SerializeField] Image profileImage;
        [SerializeField] GameObject endingCg;
        [SerializeField] Image endingCgImage;
        [SerializeField] Button cgClickButton;
        [SerializeField] TMP_Text cgClickText;
        [SerializeField] CanvasGroup cgClickCanvasGroup;
        [SerializeField] GameObject endingPanel;
        [SerializeField] Image endingPanelImage;
        [SerializeField] GameObject scrollPanel;
        [SerializeField] Image scrollEndingImage;
        [SerializeField] Button quitButton;
        [SerializeField] Button backMenuButton;

        readonly List<EndingLine> queue = new List<EndingLine>();
        EndingDefinition resolved;
        Sprite displayCg;
        Sprite displayEndingImage;
        bool hasAllNpcProps;
        Sprite lastExpression;
        int lineIndex;
        bool running;
        bool dialogActive;
        bool awaitingCgClick;
        Coroutine routine;
        GameObject activeDialogRoot;

        public bool IsDialogActive => dialogActive;

        void Awake()
        {
            ResolveRefs();
            HideChildren();
            BindEndingButtons();
            if (cgClickButton != null)
                cgClickButton.onClick.AddListener(OnCgClicked);

            GameLanguage.Changed += OnLanguageChanged;
        }

        void OnDestroy()
        {
            GameLanguage.Changed -= OnLanguageChanged;
            if (cgClickButton != null)
                cgClickButton.onClick.RemoveListener(OnCgClicked);
            if (quitButton != null)
                quitButton.onClick.RemoveListener(QuitGame);
            if (backMenuButton != null)
                backMenuButton.onClick.RemoveListener(BackToMenu);
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
            {
                RefreshDisplayEndingImage();
                ApplyEndingImages();
            }
        }

        IEnumerator RunSequence()
        {
            running = true;
            dialogActive = false;
            awaitingCgClick = false;

            GameSession session = GameRoot.Instance != null ? GameRoot.Instance.Session : null;
            resolved = EndingRules.Resolve(session, endingCatalog, diaryCatalog, npcCatalog);
            hasAllNpcProps = EndingRules.HasAllNpcProps(session, npcCatalog);
            displayCg = ResolveDisplayCg();
            RefreshDisplayEndingImage();

            if (resolved == null)
                Debug.LogError("没有命中任何结局，请检查 EndingCatalog 条件。", endingCatalog);

            // 1) 黑屏后只播 endingCG。主 BGM 随黑屏淡出，结局音乐随 CG 渐显进入。
            AudioManager.StopBGMForEnding();
            yield return FadeToBlack();
            AudioManager.PlayBGMEnd();
            ShowEndingCg();
            yield return FadeFromBlack();

            // 2) 间隔后渐显 ClickButton。
            float delay = Mathf.Max(0f, cgClickDelay);
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            yield return FadeInClickButton();

            if (cgClickButton != null)
            {
                awaitingCgClick = true;
                while (awaitingCgClick)
                    yield return null;
            }

            HideClickButton();

            // 3) 齐道具：黑屏后再进 metaPanel；未齐道具直接进 DialogPanel。
            if (hasAllNpcProps)
            {
                yield return FadeToBlack();
                PrepareDialogPhase();
                if (queue.Count > 0)
                {
                    dialogActive = true;
                    ShowCurrentLine();
                }

                yield return FadeFromBlack();
            }
            else
            {
                PrepareDialogPhase();
                if (queue.Count > 0)
                {
                    dialogActive = true;
                    ShowCurrentLine();
                }
            }

            while (dialogActive)
                yield return null;

            // 4) 黑屏后进入 EndingPanel，先只显示 endingImage。
            yield return FadeToBlack();
            HideDialogPhase();
            if (endingCg != null)
                endingCg.SetActive(false);

            ShowEndingPanelIntro();
            yield return FadeFromBlack();

            float hold = Mathf.Max(0f, endingImageHoldDuration);
            if (hold > 0f)
                yield return new WaitForSecondsRealtime(hold);

            ShowEndingScrollPanel();

            running = false;
            routine = null;
        }

        Sprite ResolveDisplayCg()
        {
            if (hasAllNpcProps && endingCatalog != null)
            {
                EndingDefinition normal = endingCatalog.Find("ending_normal");
                if (normal != null && normal.cg != null)
                    return normal.cg;
            }

            return resolved != null ? resolved.cg : null;
        }

        void RefreshDisplayEndingImage()
        {
            if (resolved == null)
            {
                displayEndingImage = null;
                return;
            }

            Sprite zh = hasAllNpcProps ? resolved.metaEndingImageZh : resolved.endingImageZh;
            Sprite en = hasAllNpcProps ? resolved.metaEndingImageEn : resolved.endingImageEn;
            displayEndingImage = EndingCatalog.PickSprite(GameLanguage.Current, zh, en);
        }

        void PrepareDialogPhase()
        {
            lastExpression = null;
            lineIndex = 0;
            queue.Clear();

            if (hasAllNpcProps)
            {
                if (metaPanel != null)
                    metaPanel.SetActive(true);
                if (dialogPanel != null)
                    dialogPanel.SetActive(false);

                BindDialogUi(FindMetaDialogPanel());
                AppendLines(endingCatalog != null ? endingCatalog.commonLines : null);
                AppendLines(resolved != null ? resolved.lines : null);
            }
            else
            {
                if (metaPanel != null)
                    metaPanel.SetActive(false);
                if (dialogPanel != null)
                    dialogPanel.SetActive(true);

                BindDialogUi(dialogPanel);
                AppendLines(resolved != null ? resolved.lines : null);
            }

            HideOptions(activeDialogRoot);
        }

        void HideDialogPhase()
        {
            dialogActive = false;
            if (dialogPanel != null)
                dialogPanel.SetActive(false);
            if (metaPanel != null)
                metaPanel.SetActive(false);
        }

        GameObject FindMetaDialogPanel()
        {
            if (metaPanel == null)
                return null;

            Transform found = metaPanel.transform.Find("DialogPanel");
            return found != null ? found.gameObject : metaPanel;
        }

        void BindDialogUi(GameObject root)
        {
            activeDialogRoot = root;
            bodyText = null;
            nameText = null;
            profileImage = null;

            if (root == null)
                return;

            Transform player = root.transform.Find("PlayerDialogue");
            if (player != null)
            {
                Transform body = player.Find("BodyText");
                if (body != null)
                    bodyText = body.GetComponent<TMP_Text>();

                Transform name = player.Find("UserNameText");
                if (name != null)
                    nameText = name.GetComponent<TMP_Text>();

                Transform profile = player.Find("PlayerProfileImage");
                if (profile != null)
                    profileImage = profile.GetComponent<Image>();
            }

            NightDialogClick click = root.GetComponent<NightDialogClick>();
            if (click != null)
                click.BindEnding(this);
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
                GameRoot root = GameRoot.Instance;
                nameText.text = root != null
                    ? root.PlayerDisplayName
                    : (GameLanguage.Current == DiaryLanguage.En
                        ? GameRoot.DefaultPlayerNameEn
                        : GameRoot.DefaultPlayerNameZh);
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
            if (dialogPanel != null)
                dialogPanel.SetActive(false);
            if (metaPanel != null)
                metaPanel.SetActive(false);

            if (endingCg != null)
                endingCg.SetActive(true);

            if (endingCgImage != null)
            {
                endingCgImage.sprite = displayCg;
                endingCgImage.enabled = displayCg != null;
                endingCgImage.color = displayCg != null ? Color.white : Color.black;
            }

            HideClickButton();
        }

        IEnumerator FadeInClickButton()
        {
            if (cgClickButton == null)
                yield break;

            ApplyClickText();
            cgClickButton.gameObject.SetActive(true);
            cgClickButton.interactable = false;
            SetClickAlpha(0f);

            float duration = Mathf.Max(0.01f, clickFadeDuration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetClickAlpha(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            SetClickAlpha(1f);
            cgClickButton.interactable = true;
        }

        void HideClickButton()
        {
            awaitingCgClick = false;
            if (cgClickButton == null)
                return;

            cgClickButton.interactable = false;
            SetClickAlpha(0f);
            cgClickButton.gameObject.SetActive(false);
        }

        void SetClickAlpha(float alpha)
        {
            if (cgClickCanvasGroup != null)
            {
                cgClickCanvasGroup.alpha = alpha;
                cgClickCanvasGroup.blocksRaycasts = alpha > 0.01f;
                cgClickCanvasGroup.interactable = alpha > 0.99f;
            }

            if (cgClickText != null)
            {
                Color color = cgClickText.color;
                color.a = alpha;
                cgClickText.color = color;
            }
        }

        void OnCgClicked()
        {
            if (!awaitingCgClick)
                return;

            AudioManager.PlayClick();
            awaitingCgClick = false;
        }

        void BindEndingButtons()
        {
            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(QuitGame);
                quitButton.onClick.AddListener(QuitGame);
            }
            else
                Debug.LogError("需要 ScrollPanel/QuitButton，并且带 Button。", this);

            if (backMenuButton != null)
            {
                backMenuButton.onClick.RemoveListener(BackToMenu);
                backMenuButton.onClick.AddListener(BackToMenu);
            }
            else
                Debug.LogError("需要 ScrollPanel/BackMeunButton，并且带 Button。", this);
        }

        void QuitGame()
        {
            AudioManager.PlayClick();
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        void BackToMenu()
        {
            GameRoot root = GameRoot.Instance;
            if (root == null)
                return;

            AudioManager.PlayClick();
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);

            root.ReturnToMenu();
        }

        void ShowEndingPanelIntro()
        {
            if (endingPanel != null)
                endingPanel.SetActive(true);

            if (scrollPanel != null)
                scrollPanel.SetActive(false);

            if (endingPanelImage != null)
                endingPanelImage.gameObject.SetActive(true);

            ApplyEndingImages();
        }

        void ShowEndingScrollPanel()
        {
            if (endingPanelImage != null)
                endingPanelImage.gameObject.SetActive(false);

            if (scrollPanel != null)
                scrollPanel.SetActive(true);

            ApplyEndingImages();
        }

        void ApplyEndingImages()
        {
            ApplyImage(endingPanelImage, displayEndingImage);
            ApplyImage(scrollEndingImage, displayEndingImage);
        }

        static void ApplyImage(Image image, Sprite sprite)
        {
            if (image == null)
                return;

            image.sprite = sprite;
            image.enabled = sprite != null;
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
            if (metaPanel != null)
                metaPanel.SetActive(false);
            if (endingCg != null)
                endingCg.SetActive(false);
            if (scrollPanel != null)
                scrollPanel.SetActive(false);
            if (endingPanelImage != null)
                endingPanelImage.gameObject.SetActive(false);
            if (endingPanel != null)
                endingPanel.SetActive(false);
            HideClickButton();
            if (fadeOverlay != null)
            {
                Color color = fadeOverlay.color;
                color.a = 0f;
                fadeOverlay.color = color;
                fadeOverlay.gameObject.SetActive(false);
            }

            HideOptions(dialogPanel);
            HideOptions(FindMetaDialogPanel());
        }

        void HideOptions(GameObject root)
        {
            if (root == null)
                return;

            for (int i = 1; i <= 3; i++)
            {
                Transform found = root.transform.Find("Option" + i);
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

            if (metaPanel == null)
            {
                Transform found = transform.Find("metaPanel");
                if (found != null)
                    metaPanel = found.gameObject;
            }

            if (dialogPanel != null)
            {
                NightDialogClick click = dialogPanel.GetComponent<NightDialogClick>();
                if (click != null)
                    click.BindEnding(this);
            }

            GameObject metaDialog = FindMetaDialogPanel();
            if (metaDialog != null)
            {
                NightDialogClick click = metaDialog.GetComponent<NightDialogClick>();
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

                if (cgClickButton != null)
                {
                    if (cgClickText == null)
                    {
                        TMP_Text tmp = cgClickButton.GetComponentInChildren<TMP_Text>(true);
                        if (tmp != null)
                            cgClickText = tmp;
                    }

                    if (cgClickCanvasGroup == null)
                        cgClickCanvasGroup = cgClickButton.GetComponent<CanvasGroup>();
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

                if (scrollPanel == null)
                {
                    Transform found = endingPanel.transform.Find("ScrollPanel");
                    if (found != null)
                        scrollPanel = found.gameObject;
                }
            }

            if (scrollPanel != null)
            {
                if (scrollEndingImage == null)
                {
                    Transform found = scrollPanel.transform.Find("endingImage");
                    if (found != null)
                        scrollEndingImage = found.GetComponent<Image>();
                }

                if (quitButton == null)
                {
                    Transform found = scrollPanel.transform.Find("QuitButton");
                    if (found != null)
                        quitButton = found.GetComponent<Button>();
                }

                if (backMenuButton == null)
                {
                    Transform found = scrollPanel.transform.Find("BackMeunButton");
                    if (found != null)
                        backMenuButton = found.GetComponent<Button>();
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
