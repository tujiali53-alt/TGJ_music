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
    public sealed class NightDialogPanel : MonoBehaviour
    {
        const string SuccessTitleId = "ui.judgment_success_title";
        const string FailTitleId = "ui.judgment_fail_title";
        const string EmptyHandedId = "ui.judgment_empty";

        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] Button option1;
        [SerializeField] Button option2;
        [SerializeField] Button option3;
        [SerializeField] NightDicePanel dice;
        [SerializeField] GameObject playerDialogue;
        [SerializeField] Image profileImage;
        [SerializeField] GameObject dialogueFrame;
        [SerializeField] GameObject dialogueName;
        [SerializeField] TMP_Text nameText;
        [SerializeField] GameObject judgment;
        [SerializeField] GameObject successRoot;
        [SerializeField] GameObject failRoot;
        [SerializeField] TMP_Text successBody;
        [SerializeField] TMP_Text successTitleText;
        [SerializeField] TMP_Text obtainFoodNameText;
        [SerializeField] TMP_Text obtainFoodAmountText;
        [SerializeField] Image obtainFoodImage;
        [SerializeField] TMP_Text failBody;
        [SerializeField] TMP_Text failTitleText;
        [SerializeField] TMP_Text failNameText;
        [SerializeField] RectTransform choiceCursor;
        [SerializeField] Sprite choiceIdle;
        [SerializeField] Sprite choiceHighlight;
        [SerializeField] UiTextCatalog uiText;

        readonly List<string> lines = new List<string>();
        readonly List<NightOption> choices = new List<NightOption>();
        readonly List<NightReward> lastRewards = new List<NightReward>();
        Button[] buttons;
        int aimedIndex;
        int lineIndex;
        bool resultMode;
        bool optionsVisible;
        bool awaitingPlayerLine;
        bool loggedMissingShocked;
        bool openForNight;
        bool resultSuccess;
        string resultOptionId;
        Sprite pendingPortrait;
        int buffDelta;
        NightEventDefinition current;

        void Awake()
        {
            if (panel == null)
            {
                Transform found = transform.Find("DialogPanel");
                if (found != null)
                    panel = found.gameObject;
            }

            if (panel != null && bodyText == null)
            {
                Transform found = panel.transform.Find("BodyText");
                if (found != null)
                    bodyText = found.GetComponent<TMP_Text>();
            }

            if (option1 == null)
                option1 = FindOption("Option1");
            if (option2 == null)
                option2 = FindOption("Option2");
            if (option3 == null)
                option3 = FindOption("Option3");

            if (dice == null)
                dice = GetComponent<NightDicePanel>();

            ResolvePresentation();
            ResolveChoiceArt();
            buttons = new[] { option1, option2, option3 };
            if (choiceCursor == null)
                choiceCursor = FindCursor(panel != null ? panel.transform : null);
            if (choiceCursor == null)
                Debug.LogError("DialogPanel 下需要有 Cursor。", this);
            else
            {
                Image cursorImage = choiceCursor.GetComponent<Image>();
                if (cursorImage != null)
                    cursorImage.raycastTarget = false;
                choiceCursor.gameObject.SetActive(false);
            }

            if (panel == null)
                Debug.LogError("InteractionPanel 下需要有 DialogPanel。", this);
            if (bodyText == null)
                Debug.LogError("PlayerDialogue 下需要有 BodyText。", this);
            if (option1 == null || option2 == null || option3 == null)
                Debug.LogError("DialogPanel 下需要有 Option1、Option2、Option3，并且带 Button。", this);
            if (dice == null)
                Debug.LogError("InteractionPanel 上需要有 NightDicePanel。", this);
            if (playerDialogue == null)
                Debug.LogError("DialogPanel 下需要有 PlayerDialogue。", this);
            if (profileImage == null)
                Debug.LogError("PlayerDialogue 下需要有 PlayerProfileImage。", this);
            if (judgment == null)
                Debug.LogError("DialogPanel 下需要有 DialogueJudgment。", this);
            if (successRoot == null || failRoot == null)
                Debug.LogError("DialogueJudgment 下需要有 DeterminationSuccessful 和 CheckFailed。", this);
            if (successBody == null || successTitleText == null || obtainFoodNameText == null || obtainFoodAmountText == null || obtainFoodImage == null)
                Debug.LogError("DeterminationSuccessful 下需要有 Title、bodyText、ObtainFoodNameText、obtainFoodText、ObtainFoodImage。", this);
            if (failBody == null || failTitleText == null || failNameText == null)
                Debug.LogError("CheckFailed 下需要有 Title、bodyText (1) 和 ObtainFoodNameText。", this);
            if (uiText == null)
                Debug.LogError("NightDialogPanel 需要绑定 UiTextCatalog。", this);

            if (option1 != null)
                option1.onClick.AddListener(ChooseFirst);
            if (option2 != null)
                option2.onClick.AddListener(ChooseSecond);
            if (option3 != null)
                option3.onClick.AddListener(ChooseThird);

            if (panel != null)
            {
                NightDialogClick click = panel.GetComponent<NightDialogClick>();
                if (click != null)
                    click.Bind(this);
                panel.SetActive(false);
            }

            CaptureCursorGap();
            HideOptions();
            SilenceJudgmentBody();
            ApplyJudgmentTitles();
            GameLanguage.Changed += OnLanguageChanged;
        }

        void OnDestroy()
        {
            GameLanguage.Changed -= OnLanguageChanged;
        }

        void OnLanguageChanged()
        {
            if (!openForNight)
                return;

            if (resultMode)
            {
                ApplyJudgmentTitles();
                if (resultSuccess)
                    FillSuccess(lastRewards);
                else
                    FillFail();
            }

            if (optionsVisible)
                ShowOptions();

            if (!resultMode && nameText != null && playerDialogue != null && playerDialogue.activeSelf)
                nameText.text = ResolvePlayerName();
        }

        public void OpenIntro(NightEventDefinition definition, int buffFaceDelta)
        {
            NpcDialogPanel npcDialog = FindAnyObjectByType<NpcDialogPanel>(FindObjectsInactive.Include);
            if (npcDialog != null)
                npcDialog.YieldToNight();

            openForNight = true;
            current = definition;
            buffDelta = buffFaceDelta;
            resultOptionId = "";
            UsePlayerDialogue();
            if (definition != null && definition.introPortrait == null)
            {
                if (definition.id == "event_merchant")
                    NoteMissingShocked();
                else
                    Debug.LogError("事件缺少开场表情：" + definition.id, this);
            }
            ApplyPortrait(definition != null ? definition.introPortrait : null);
            choices.Clear();
            if (definition != null && definition.options != null)
            {
                for (int i = 0; i < definition.options.Count; i++)
                    choices.Add(definition.options[i]);
                choices.Sort(CompareOption);
                if (choices.Count > 3)
                    choices.RemoveRange(3, choices.Count - 3);
            }

            OpenLines(CollectLines(definition, "", NightLinePhase.Intro));
        }

        public void OpenResult(
            NightEventDefinition definition,
            NightOption option,
            bool success,
            IList<NightReward> rewards)
        {
            openForNight = true;
            current = definition;
            choices.Clear();
            resultSuccess = success;
            resultOptionId = option.optionId;
            pendingPortrait = success ? option.successPortrait : option.failPortrait;
            if (pendingPortrait == null && success && option.optionId == "opt_grave_mourn")
                NoteMissingShocked();
            lastRewards.Clear();
            if (rewards != null)
            {
                for (int i = 0; i < rewards.Count; i++)
                    lastRewards.Add(rewards[i]);
            }

            UseJudgment(success);
            ApplyJudgmentTitles();
            if (success)
                FillSuccess(lastRewards);
            else
                FillFail();

            NightLinePhase phase = success ? NightLinePhase.Success : NightLinePhase.Fail;
            lines.Clear();
            List<string> source = CollectLines(definition, option.optionId, phase);
            for (int i = 0; i < source.Count; i++)
                lines.Add(source[i]);
            lineIndex = 0;
            optionsVisible = false;
            awaitingPlayerLine = true;
            HideOptions();
            if (panel != null)
            {
                panel.SetActive(true);
                Canvas.ForceUpdateCanvases();
            }
        }

        public bool IsOpen => openForNight && panel != null && panel.activeSelf;

        public void OnPanelClick(PointerEventData eventData)
        {
            if (!openForNight || panel == null || !panel.activeSelf)
                return;

            if (eventData != null)
            {
                GameObject pressed = eventData.pointerPress != null
                    ? eventData.pointerPress
                    : eventData.rawPointerPress;
                if (pressed != null && pressed.GetComponentInParent<Button>() != null)
                    return;
            }

            Advance();
        }

        void OpenLines(List<string> source)
        {
            lines.Clear();
            if (source != null)
            {
                for (int i = 0; i < source.Count; i++)
                    lines.Add(source[i]);
            }

            if (panel != null)
            {
                panel.SetActive(true);
                Canvas.ForceUpdateCanvases();
            }

            lineIndex = 0;
            optionsVisible = false;
            awaitingPlayerLine = false;
            HideOptions();
            WriteLine();

            if (!resultMode && lines.Count == 0)
                ShowOptions();
        }

        void Advance()
        {
            if (awaitingPlayerLine)
            {
                awaitingPlayerLine = false;
                if (lines.Count == 0)
                {
                    ClosePanel();
                    return;
                }

                ShowPlayerChrome();
                ApplyPortrait(pendingPortrait);
                lineIndex = 0;
                WriteLine();
                return;
            }

            if (lineIndex < lines.Count - 1)
            {
                lineIndex++;
                WriteLine();
                return;
            }

            if (!resultMode && !optionsVisible)
            {
                ShowOptions();
                return;
            }

            if (resultMode)
            {
                ClosePanel();
                return;
            }

            if (!resultMode && optionsVisible && !HasActionable(choices))
                ClosePanel();
        }

        public bool HasActionableOption(NightEventDefinition definition)
        {
            if (definition == null)
                return false;

            return HasActionable(definition.options);
        }

        bool HasActionable(IList<NightOption> options)
        {
            if (options == null)
                return false;

            for (int i = 0; i < options.Count; i++)
            {
                if (string.IsNullOrEmpty(BlockReason(options[i])))
                    return true;
            }

            return false;
        }

        void ShowOptions()
        {
            RememberIntroSeen();
            optionsVisible = true;
            if (buttons == null)
                return;

            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null)
                    continue;

                bool show = i < choices.Count;
                button.gameObject.SetActive(show);
                if (!show)
                    continue;

                NightOption option = choices[i];
                TMP_Text label = FindChildText(button.transform, "Label");
                TMP_Text reward = FindChildText(button.transform, "RewardText");
                if (label != null)
                    label.text = NightEventDefinition.Pick(
                        GameLanguage.Current,
                        option.displayNameZh,
                        option.displayNameEn);
                if (reward != null)
                {
                    reward.gameObject.SetActive(true);
                    reward.text = SuccessPercent(option);
                    reward.alignment = GameLanguage.Current == DiaryLanguage.En
                        ? TextAlignmentOptions.Right
                        : TextAlignmentOptions.Left;
                }

                DialogOptionFit.Apply(button, label, reward, reward != null && reward.gameObject.activeSelf);
            }

            DialogOptionFit.StackVisible(buttons);
            aimedIndex = 0;
            ApplyAim();
        }

        void HideOptions()
        {
            optionsVisible = false;
            if (buttons != null)
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i] != null)
                        buttons[i].gameObject.SetActive(false);
                }
            }

            if (choiceCursor != null)
                choiceCursor.gameObject.SetActive(false);
        }

        void ChooseFirst()
        {
            Choose(0);
        }

        void ChooseSecond()
        {
            Choose(1);
        }

        void ChooseThird()
        {
            Choose(2);
        }

        void Choose(int index)
        {
            if (!openForNight || resultMode || !optionsVisible || index < 0 || index >= choices.Count)
                return;

            if (index != aimedIndex)
            {
                aimedIndex = index;
                ApplyAim();
                return;
            }

            NightOption option = choices[index];
            if (TryAppendBlocked(option))
                return;

            AudioManager.PlayClick();
            ClosePanel();

            if (dice != null)
                dice.Open(current, option, buffDelta);
        }

        void ResolveChoiceArt()
        {
            if (choiceIdle != null && choiceHighlight != null)
                return;

            NightChoiceSprites art = Resources.Load<NightChoiceSprites>("NightChoiceSprites");
            if (art == null)
            {
                Debug.LogError("缺少 NightChoiceSprites。", this);
                return;
            }

            if (choiceIdle == null)
                choiceIdle = art.idle;
            if (choiceHighlight == null)
                choiceHighlight = art.highlight;

            if (choiceIdle == null || choiceHighlight == null)
                Debug.LogError("NightChoiceSprites 需要绑上 UI_ChoiceBG_a 和 UI_ChoiceBG_b。", this);
        }

        void ApplyAim()
        {
            if (buttons != null)
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    Button button = buttons[i];
                    if (button == null || !button.gameObject.activeSelf)
                        continue;

                    Image image = button.targetGraphic as Image;
                    if (image == null)
                        image = button.GetComponent<Image>();
                    if (image == null)
                        continue;

                    Sprite sprite = i == aimedIndex ? choiceHighlight : choiceIdle;
                    if (sprite != null)
                        image.sprite = sprite;
                }
            }

            PlaceCursor();
        }

        void PlaceCursor()
        {
            if (choiceCursor == null || buttons == null)
                return;
            if (aimedIndex < 0 || aimedIndex >= buttons.Length)
                return;

            Button button = buttons[aimedIndex];
            if (button == null || !button.gameObject.activeSelf)
                return;

            RectTransform option = button.transform as RectTransform;
            RectTransform cursorParent = choiceCursor.parent as RectTransform;
            if (option == null || cursorParent == null)
                return;

            choiceCursor.gameObject.SetActive(true);
            float optionWidth = option.rect.width > 0f ? option.rect.width : option.sizeDelta.x;
            float optionHeight = option.rect.height > 0f ? option.rect.height : option.sizeDelta.y;
            float cursorWidth = choiceCursor.rect.width > 0f ? choiceCursor.rect.width : choiceCursor.sizeDelta.x;
            float cursorHeight = choiceCursor.rect.height > 0f ? choiceCursor.rect.height : choiceCursor.sizeDelta.y;
            Vector2 pos = choiceCursor.anchoredPosition;

            if (cursorParent == option.parent)
            {
                // 枢轴在中心时，中线就是选项的 Pos Y。枢轴在顶边时，中线是 Pos Y 减去一半高度。
                float optionMiddle = option.anchoredPosition.y + (0.5f - option.pivot.y) * optionHeight;
                pos.y = optionMiddle - (0.5f - choiceCursor.pivot.y) * cursorHeight;

                // 选项加宽时右缘不动、向左伸；光标贴在当前左缘外侧，间距与场景初始布局一致。
                float optionLeft = option.anchoredPosition.x - option.pivot.x * optionWidth;
                pos.x = optionLeft - cursorLeftGap - (1f - choiceCursor.pivot.x) * cursorWidth;
            }
            else
            {
                Vector3 local = cursorParent.InverseTransformPoint(option.TransformPoint(option.rect.center));
                Vector2 anchor = (choiceCursor.anchorMin + choiceCursor.anchorMax) * 0.5f;
                float anchorY = Mathf.Lerp(cursorParent.rect.yMin, cursorParent.rect.yMax, anchor.y);
                float pivotY = local.y - (0.5f - choiceCursor.pivot.y) * cursorHeight;
                pos.y = pivotY - anchorY;

                Vector3 leftLocal = cursorParent.InverseTransformPoint(
                    option.TransformPoint(new Vector3(option.rect.xMin, option.rect.center.y, 0f)));
                float anchorX = Mathf.Lerp(cursorParent.rect.xMin, cursorParent.rect.xMax, anchor.x);
                float pivotX = leftLocal.x - cursorLeftGap - (1f - choiceCursor.pivot.x) * cursorWidth;
                pos.x = pivotX - anchorX;
            }

            choiceCursor.anchoredPosition = pos;
        }

        float cursorLeftGap = 22f;

        void CaptureCursorGap()
        {
            if (choiceCursor == null || option1 == null)
                return;

            RectTransform option = option1.transform as RectTransform;
            if (option == null)
                return;

            float optionWidth = option.rect.width > 0f ? option.rect.width : option.sizeDelta.x;
            float cursorWidth = choiceCursor.rect.width > 0f ? choiceCursor.rect.width : choiceCursor.sizeDelta.x;
            if (optionWidth < 1f || cursorWidth < 1f)
                return;

            float optionLeft = option.anchoredPosition.x - option.pivot.x * optionWidth;
            float cursorRight = choiceCursor.anchoredPosition.x + (1f - choiceCursor.pivot.x) * cursorWidth;
            cursorLeftGap = Mathf.Max(0f, optionLeft - cursorRight);
        }

        static RectTransform FindCursor(Transform root)
        {
            if (root == null)
                return null;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == "Cursor")
                    return child as RectTransform;

                RectTransform nested = FindCursor(child);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        bool TryAppendBlocked(NightOption option)
        {
            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null)
                return false;

            if (root.Session.ResolvedOptionIdsTonight != null
                && root.Session.ResolvedOptionIdsTonight.Contains(option.optionId))
            {
                string nameZh = option.displayNameZh;
                string nameEn = option.displayNameEn;
                if (string.IsNullOrEmpty(nameZh))
                    nameZh = option.optionId;
                if (string.IsNullOrEmpty(nameEn))
                    nameEn = nameZh;
                NightLogView.Append(
                    NightLogKind.OptionBlocked,
                    "log.option_resolved",
                    new[] { nameZh },
                    new[] { nameEn });
                return true;
            }

            if (option.conditionType == NightConditionType.None)
                return false;

            if (ConditionMet(option, root.Session))
                return false;

            string id = "";
            if (option.conditionType == NightConditionType.HasAnyFood)
                id = "log.no_food";
            else if (option.conditionType == NightConditionType.HasVegetable)
                id = "log.no_veg";
            else if (option.conditionType == NightConditionType.DeliciousnessAbove)
                id = "log.deliciousness_low";
            else if (option.conditionType == NightConditionType.DeliciousnessBelow)
                id = "log.deliciousness_high";

            if (string.IsNullOrEmpty(id))
                return false;

            NightLogView.Append(NightLogKind.OptionBlocked, id);
            return true;
        }

        string BlockReason(NightOption option)
        {
            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null)
                return "";

            if (root.Session.ResolvedOptionIdsTonight != null
                && root.Session.ResolvedOptionIdsTonight.Contains(option.optionId))
                return "blocked";

            if (option.conditionType == NightConditionType.None)
                return "";

            if (ConditionMet(option, root.Session))
                return "";

            if (option.conditionType == NightConditionType.HasAnyFood
                || option.conditionType == NightConditionType.HasVegetable
                || option.conditionType == NightConditionType.DeliciousnessAbove
                || option.conditionType == NightConditionType.DeliciousnessBelow)
                return "blocked";

            return "";
        }

        bool ConditionMet(NightOption option, GameSession session)
        {
            if (option.conditionType == NightConditionType.HasAnyFood)
                return HasFood(session, false);
            if (option.conditionType == NightConditionType.HasVegetable)
                return HasFood(session, true);
            if (option.conditionType == NightConditionType.DeliciousnessBelow)
                return session.TodayDeliciousness <= option.conditionValue;
            if (option.conditionType == NightConditionType.DeliciousnessAbove)
                return session.TodayDeliciousness >= option.conditionValue;
            return true;
        }

        bool HasFood(GameSession session, bool vegetableOnly)
        {
            if (session.Inventory == null)
                return false;

            IngredientDefinition[] known = dice != null ? dice.Definitions : null;
            for (int i = 0; i < session.Inventory.Count; i++)
            {
                IngredientInstance item = session.Inventory[i];
                if (item == null || item.Quantity <= 0)
                    continue;
                if (!vegetableOnly)
                    return true;

                IngredientDefinition definition = FindKnown(known, item.DefinitionId);
                if (definition != null && definition.kind == IngredientKind.Vegetable)
                    return true;
            }

            return false;
        }

        static IngredientDefinition FindKnown(IngredientDefinition[] known, string id)
        {
            if (known == null)
                return null;

            for (int i = 0; i < known.Length; i++)
            {
                if (known[i] != null && known[i].id == id)
                    return known[i];
            }

            return null;
        }

        void ResolvePresentation()
        {
            if (panel == null)
                return;

            if (playerDialogue == null)
            {
                Transform found = panel.transform.Find("PlayerDialogue");
                if (found != null)
                    playerDialogue = found.gameObject;
            }

            if (bodyText == null && playerDialogue != null)
            {
                Transform found = playerDialogue.transform.Find("BodyText");
                if (found != null)
                    bodyText = found.GetComponent<TMP_Text>();
            }

            if (playerDialogue != null)
            {
                if (profileImage == null)
                {
                    Transform found = playerDialogue.transform.Find("PlayerProfileImage");
                    if (found != null)
                        profileImage = found.GetComponent<Image>();
                }

                if (dialogueFrame == null)
                {
                    Transform found = playerDialogue.transform.Find("DialogueImage");
                    if (found != null)
                        dialogueFrame = found.gameObject;
                }

                if (dialogueName == null)
                {
                    Transform found = playerDialogue.transform.Find("UserNameText");
                    if (found != null)
                        dialogueName = found.gameObject;
                }

                if (nameText == null && dialogueName != null)
                    nameText = dialogueName.GetComponent<TMP_Text>();
            }

            if (judgment == null)
            {
                Transform found = panel.transform.Find("DialogueJudgment");
                if (found != null)
                    judgment = found.gameObject;
            }

            Transform success = judgment != null ? judgment.transform.Find("DeterminationSuccessful") : null;
            Transform failed = judgment != null ? judgment.transform.Find("CheckFailed") : null;
            if (successRoot == null && success != null)
                successRoot = success.gameObject;
            if (failRoot == null && failed != null)
                failRoot = failed.gameObject;

            if (successBody == null && success != null)
                successBody = FindChildText(success, "bodyText");
            if (successTitleText == null && success != null)
                successTitleText = FindChildText(success, "Title");
            if (obtainFoodNameText == null && success != null)
                obtainFoodNameText = FindChildText(success, "ObtainFoodNameText");
            if (obtainFoodAmountText == null && success != null)
                obtainFoodAmountText = FindChildText(success, "obtainFoodText");
            if (obtainFoodImage == null && success != null)
            {
                Transform found = success.Find("ObtainFoodImage");
                if (found != null)
                    obtainFoodImage = found.GetComponent<Image>();
            }

            if (failBody == null && failed != null)
                failBody = FindChildText(failed, "bodyText (1)");
            if (failTitleText == null && failed != null)
                failTitleText = FindChildText(failed, "Title");
            if (failNameText == null && failed != null)
            {
                failNameText = FindChildText(failed, "ObtainFoodNameText");
                if (failNameText == null)
                    failNameText = FindChildText(failed, "ObtainFoodNameText (1)");
            }
        }

        void UsePlayerDialogue()
        {
            resultMode = false;
            awaitingPlayerLine = false;
            ShowPlayerChrome();
        }

        void ShowPlayerChrome()
        {
            if (playerDialogue != null)
                playerDialogue.SetActive(true);
            if (dialogueFrame != null)
                dialogueFrame.SetActive(true);
            if (dialogueName != null)
                dialogueName.SetActive(true);
            if (openForNight && nameText != null)
                nameText.text = ResolvePlayerName();
            if (profileImage != null)
                profileImage.gameObject.SetActive(true);
            if (bodyText != null)
                bodyText.gameObject.SetActive(true);
            if (judgment != null)
                judgment.SetActive(false);
        }

        void ClosePanel()
        {
            bool wasResult = resultMode;
            if (resultMode)
                RememberOutcomeSeen();

            openForNight = false;
            resultOptionId = "";
            if (panel != null)
                panel.SetActive(false);

            if (wasResult && TutorialDirector.Instance != null)
                TutorialDirector.Instance.NotifyNightResultClosed();
        }

        void RememberIntroSeen()
        {
            GameSession session = Session();
            if (session == null || current == null)
                return;

            session.RememberNightIntro(current.id);
        }

        void RememberOutcomeSeen()
        {
            GameSession session = Session();
            if (session == null || string.IsNullOrEmpty(resultOptionId))
                return;

            session.RememberNightOutcome(resultOptionId, resultSuccess);
        }

        static GameSession Session()
        {
            GameRoot root = GameRoot.Instance;
            return root != null ? root.Session : null;
        }

        void UseJudgment(bool success)
        {
            resultMode = true;
            if (playerDialogue != null)
                playerDialogue.SetActive(false);
            if (bodyText != null)
                bodyText.gameObject.SetActive(false);
            if (judgment != null)
                judgment.SetActive(true);
            if (successRoot != null)
                successRoot.SetActive(success);
            if (failRoot != null)
                failRoot.SetActive(!success);
            SilenceJudgmentBody();
        }

        void SilenceJudgmentBody()
        {
            if (successBody != null)
            {
                successBody.text = "";
                successBody.gameObject.SetActive(false);
            }

            if (failBody != null)
            {
                failBody.text = "";
                failBody.gameObject.SetActive(false);
            }
        }

        void NoteMissingShocked()
        {
            if (loggedMissingShocked)
                return;

            loggedMissingShocked = true;
            Debug.LogError("缺少表情 Main_Shocked。", this);
        }

        void ApplyPortrait(Sprite sprite)
        {
            if (profileImage == null || sprite == null)
                return;

            profileImage.sprite = sprite;
            profileImage.color = Color.white;
        }

        void WriteLine()
        {
            if (bodyText == null)
                return;

            bodyText.text = lineIndex >= 0 && lineIndex < lines.Count ? lines[lineIndex] : "";
        }

        void FillSuccess(IList<NightReward> rewards)
        {
            bool english = GameLanguage.Current == DiaryLanguage.En;
            string nameJoin = english ? ", " : "、";
            string amountJoin = english ? ", " : "，";
            string names = "";
            string amounts = "";
            Sprite sprite = null;
            IngredientDefinition[] known = dice != null ? dice.Definitions : null;

            if (rewards != null)
            {
                for (int i = 0; i < rewards.Count; i++)
                {
                    NightReward reward = rewards[i];
                    if (string.IsNullOrEmpty(reward.ingredientId) || reward.count <= 0)
                        continue;

                    IngredientDefinition definition = FindKnown(known, reward.ingredientId);
                    string name = IngredientDetailText.DisplayName(definition, GameLanguage.Current);
                    if (string.IsNullOrEmpty(name))
                        name = reward.ingredientId;
                    if (sprite == null && definition != null && definition.spriteWhole != null)
                        sprite = definition.spriteWhole;

                    names = Join(names, name, nameJoin);
                    string piece = english ? name + " × " + reward.count : name + "×" + reward.count;
                    amounts = Join(amounts, piece, amountJoin);
                }
            }

            if (obtainFoodImage != null)
            {
                obtainFoodImage.sprite = sprite;
                obtainFoodImage.color = Color.white;
            }

            if (obtainFoodNameText != null)
                obtainFoodNameText.text = names;

            if (obtainFoodAmountText != null)
                obtainFoodAmountText.text = amounts;
        }

        void FillFail()
        {
            if (failNameText == null)
                return;

            failNameText.text = UiPick(EmptyHandedId, "一无所获", "Came away empty-handed");
        }

        void ApplyJudgmentTitles()
        {
            if (successTitleText != null)
                successTitleText.text = UiPick(SuccessTitleId, "成功!!", "Success!!");

            if (failTitleText != null)
                failTitleText.text = UiPick(
                    FailTitleId,
                    "失败!!......一无所获......",
                    "Fail!!......Came away empty-handed......");
        }

        string UiPick(string id, string fallbackZh, string fallbackEn)
        {
            string text = uiText != null ? uiText.Pick(id, GameLanguage.Current) : "";
            if (!string.IsNullOrEmpty(text))
                return text;

            return GameLanguage.Current == DiaryLanguage.En ? fallbackEn : fallbackZh;
        }

        string SuccessPercent(NightOption option)
        {
            int faces = NightCheck.EffectiveFaces(option.successFaces, buffDelta);
            int percent = Mathf.RoundToInt(faces * 100f / 6f);
            return GameLanguage.Current == DiaryLanguage.En
                ? percent + "%"
                : "成功概率" + percent + "%";
        }

        static string ResolvePlayerName()
        {
            GameRoot root = GameRoot.Instance;
            if (root != null)
                return root.PlayerDisplayName;

            return GameLanguage.Current == DiaryLanguage.En
                ? GameRoot.DefaultPlayerNameEn
                : GameRoot.DefaultPlayerNameZh;
        }

        static string Join(string left, string right, string separator)
        {
            if (string.IsNullOrEmpty(left))
                return right;
            if (string.IsNullOrEmpty(right))
                return left;
            return left + separator + right;
        }

        Button FindOption(string name)
        {
            if (panel == null)
                return null;

            Transform found = panel.transform.Find(name);
            if (found == null)
                return null;

            return found.GetComponent<Button>();
        }

        static TMP_Text FindChildText(Transform parent, string childName)
        {
            Transform found = parent.Find(childName);
            if (found == null)
                return null;

            return found.GetComponent<TMP_Text>();
        }

        static int CompareOption(NightOption a, NightOption b)
        {
            return a.order.CompareTo(b.order);
        }

        static List<string> CollectLines(NightEventDefinition definition, string optionId, NightLinePhase phase)
        {
            var result = new List<string>();
            if (definition == null || definition.lines == null)
                return result;

            var matched = new List<NightLine>();
            for (int i = 0; i < definition.lines.Count; i++)
            {
                NightLine line = definition.lines[i];
                string text = NightEventDefinition.Pick(GameLanguage.Current, line.textZh, line.textEn);
                if (line.phase != phase || string.IsNullOrEmpty(text))
                    continue;

                string lineOption = line.optionId ?? "";
                string wanted = optionId ?? "";
                if (lineOption != wanted)
                    continue;

                matched.Add(line);
            }

            matched.Sort(CompareLine);
            for (int i = 0; i < matched.Count; i++)
            {
                result.Add(NightEventDefinition.Pick(
                    GameLanguage.Current,
                    matched[i].textZh,
                    matched[i].textEn));
            }
            return result;
        }

        static int CompareLine(NightLine a, NightLine b)
        {
            return a.order.CompareTo(b.order);
        }
    }
}
