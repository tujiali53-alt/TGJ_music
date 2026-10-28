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
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] Button option1;
        [SerializeField] Button option2;
        [SerializeField] Button option3;
        [SerializeField] NightDicePanel dice;

        readonly List<string> lines = new List<string>();
        readonly List<NightOption> choices = new List<NightOption>();
        Button[] buttons;
        int lineIndex;
        bool resultMode;
        bool optionsVisible;
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

            buttons = new[] { option1, option2, option3 };

            if (panel == null)
                Debug.LogError("InteractionPanel 下需要有 DialogPanel。", this);
            if (bodyText == null)
                Debug.LogError("DialogPanel 下需要有 BodyText。", this);
            if (option1 == null || option2 == null || option3 == null)
                Debug.LogError("DialogPanel 下需要有 Option1、Option2、Option3，并且带 Button。", this);
            if (dice == null)
                Debug.LogError("InteractionPanel 上需要有 NightDicePanel。", this);

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

            HideOptions();
        }

        public void OpenIntro(NightEventDefinition definition, int buffFaceDelta)
        {
            current = definition;
            buffDelta = buffFaceDelta;
            resultMode = false;
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

        public void OpenResult(NightEventDefinition definition, NightOption option, bool success)
        {
            current = definition;
            resultMode = true;
            choices.Clear();
            NightLinePhase phase = success ? NightLinePhase.Success : NightLinePhase.Fail;
            string optionId = option.optionId;
            OpenLines(CollectLines(definition, optionId, phase));
        }

        public bool IsOpen => panel != null && panel.activeSelf;

        public void OnPanelClick(PointerEventData eventData)
        {
            if (panel == null || !panel.activeSelf)
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

            if (lines.Count == 0 && resultMode)
            {
                if (panel != null)
                    panel.SetActive(false);
                return;
            }

            if (panel != null)
            {
                panel.SetActive(true);
                Canvas.ForceUpdateCanvases();
            }

            lineIndex = 0;
            optionsVisible = false;
            HideOptions();
            if (bodyText != null)
                bodyText.text = lines.Count > 0 ? lines[0] : "";

            if (lines.Count == 0)
                ShowOptions();
        }

        void Advance()
        {
            if (lineIndex < lines.Count - 1)
            {
                lineIndex++;
                if (bodyText != null)
                    bodyText.text = lines[lineIndex];
                return;
            }

            if (!resultMode && !optionsVisible)
            {
                ShowOptions();
                return;
            }

            if (resultMode && panel != null)
                panel.SetActive(false);
        }

        void ShowOptions()
        {
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
                    label.text = option.displayName;
                if (reward != null)
                    reward.text = IsRevealed(option.optionId) && dice != null ? dice.Describe(option) : "";
            }
        }

        void HideOptions()
        {
            optionsVisible = false;
            if (buttons == null)
                return;

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                    buttons[i].gameObject.SetActive(false);
            }
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
            if (resultMode || index < 0 || index >= choices.Count)
                return;

            NightOption option = choices[index];
            string blocked = BlockReason(option);
            if (!string.IsNullOrEmpty(blocked))
            {
                NightLogView.Append(NightLogKind.OptionBlocked, blocked);
                return;
            }

            if (panel != null)
                panel.SetActive(false);

            if (dice != null)
                dice.Open(current, option, buffDelta);
        }

        static bool IsRevealed(string optionId)
        {
            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null || root.Session.RevealedRewardOptionIds == null)
                return false;

            return root.Session.RevealedRewardOptionIds.Contains(optionId);
        }

        string BlockReason(NightOption option)
        {
            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null)
                return "";

            if (root.Session.ResolvedOptionIdsTonight != null
                && root.Session.ResolvedOptionIdsTonight.Contains(option.optionId))
            {
                string name = string.IsNullOrEmpty(option.displayName) ? option.optionId : option.displayName;
                return name + "今夜已经判定过";
            }

            if (option.conditionType == NightConditionType.None)
                return "";

            if (ConditionMet(option, root.Session))
                return "";

            if (option.conditionType == NightConditionType.HasAnyFood)
                return "没有食物";
            if (option.conditionType == NightConditionType.HasVegetable)
                return "没有素食";
            if (option.conditionType == NightConditionType.DeliciousnessAbove)
                return "白天美味度不足";
            if (option.conditionType == NightConditionType.DeliciousnessBelow)
                return "白天美味度过高";
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
                if (line.phase != phase || string.IsNullOrEmpty(line.text))
                    continue;

                string lineOption = line.optionId ?? "";
                string wanted = optionId ?? "";
                if (lineOption != wanted)
                    continue;

                matched.Add(line);
            }

            matched.Sort(CompareLine);
            for (int i = 0; i < matched.Count; i++)
                result.Add(matched[i].text);
            return result;
        }

        static int CompareLine(NightLine a, NightLine b)
        {
            return a.order.CompareTo(b.order);
        }
    }
}
