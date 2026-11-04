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
    public sealed class NpcDialogPanel : MonoBehaviour
    {
        [SerializeField] NpcCatalog catalog;
        [SerializeField] DiaryLanguage language = DiaryLanguage.Zh;
        [SerializeField] bool openOnDayStart;
        [SerializeField] string dayNpcId = "npc_tutu";
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] TMP_Text nameText;
        [SerializeField] Image profileImage;
        [SerializeField] Button option1;
        [SerializeField] Button option2;
        [SerializeField] Button option3;

        readonly List<string> lines = new List<string>();
        readonly List<NpcOption> choices = new List<NpcOption>();
        Button[] buttons;
        int lineIndex;
        bool npcOpen;
        bool optionsVisible;
        bool replying;
        NpcCharacter current;

        void Awake()
        {
            if (catalog == null)
                Debug.LogError("NPC 对话需要绑上 NpcCatalog。", this);

            if (panel == null)
            {
                Transform found = transform.Find("DialogPanel");
                if (found != null)
                    panel = found.gameObject;
            }

            ResolveWidgets();
            buttons = new[] { option1, option2, option3 };
            if (panel == null)
                Debug.LogError("InteractionPanel 下需要有 DialogPanel。", this);
            if (bodyText == null)
                Debug.LogError("PlayerDialogue 下需要有 BodyText。", this);
            if (nameText == null)
                Debug.LogError("PlayerDialogue 下需要有 UserNameText。", this);
            if (profileImage == null)
                Debug.LogError("PlayerDialogue 下需要有 PlayerProfileImage。", this);
            if (option1 == null || option2 == null || option3 == null)
                Debug.LogError("DialogPanel 下需要有 Option1、Option2、Option3，并且带 Button。", this);

            if (option1 != null)
                option1.onClick.AddListener(() => Choose(0));
            if (option2 != null)
                option2.onClick.AddListener(() => Choose(1));
            if (option3 != null)
                option3.onClick.AddListener(() => Choose(2));

            HideOptions();
            if (panel != null)
                panel.SetActive(false);
        }

        void Start()
        {
            if (!openOnDayStart || string.IsNullOrEmpty(dayNpcId))
                return;

            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null)
                return;

            if (root.Session.Phase != DayPhase.Day || root.Session.DayIndex != 6)
                return;

            if (IsResolved(dayNpcId))
                return;

            Open(dayNpcId);
        }

        public bool IsOpen => npcOpen && panel != null && panel.activeSelf;

        public NpcCharacter FindCharacter(string npcId)
        {
            if (catalog == null)
            {
                Debug.LogError("NPC 对话需要绑上 NpcCatalog。", this);
                return null;
            }

            NpcCharacter character = catalog.FindCharacter(npcId);
            if (character == null)
                Debug.LogError("NpcCatalog 里没有 " + npcId + "。", catalog);

            return character;
        }

        public bool IsResolved(string npcId)
        {
            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null || string.IsNullOrEmpty(npcId))
                return false;

            return Contains(root.Session.ResolvedNpcIds, npcId);
        }

        public void YieldToNight()
        {
            npcOpen = false;
            optionsVisible = false;
            replying = false;
            current = null;
            HideOptions();
        }

        public void Open(string npcId)
        {
            NightDialogPanel night = FindAnyObjectByType<NightDialogPanel>(FindObjectsInactive.Include);
            if (night != null && night.IsOpen)
                return;

            NpcCharacter character = FindCharacter(npcId);
            if (character == null || IsResolved(npcId))
                return;

            current = character;
            npcOpen = true;
            replying = false;
            choices.Clear();
            if (character.options != null)
            {
                for (int i = 0; i < character.options.Count; i++)
                {
                    if (character.options[i] != null)
                        choices.Add(character.options[i]);
                }

                choices.Sort((a, b) => a.order.CompareTo(b.order));
                if (choices.Count > 3)
                    choices.RemoveRange(3, choices.Count - 3);
            }

            ShowChrome();
            if (profileImage != null)
            {
                profileImage.sprite = character.portrait;
                profileImage.enabled = character.portrait != null;
                profileImage.preserveAspect = true;
                profileImage.color = Color.white;
            }

            if (nameText != null)
                nameText.text = NpcCatalog.Pick(language, character.displayNameZh, character.displayNameEn);

            HideJudgment();
            BeginLines(CollectLines(""));
        }

        public void OnPanelClick(PointerEventData eventData)
        {
            if (!IsOpen)
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

        void BeginLines(List<string> source)
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
            HideOptions();
            if (!replying && lines.Count == 0)
            {
                ShowOptions();
                return;
            }

            WriteLine();
        }

        void Advance()
        {
            if (lineIndex < lines.Count - 1)
            {
                lineIndex++;
                WriteLine();
                return;
            }

            if (!replying && !optionsVisible)
            {
                ShowOptions();
                return;
            }

            if (replying)
                Close();
        }

        void Choose(int index)
        {
            if (!npcOpen || replying || !optionsVisible || index < 0 || index >= choices.Count)
                return;

            NpcOption option = choices[index];
            Grant(option);
            Remember(current != null ? current.id : null);
            replying = true;
            optionsVisible = false;
            HideOptions();
            BeginLines(CollectLines(option.optionId));
            if (lines.Count == 0)
                Close();
        }

        void Close()
        {
            npcOpen = false;
            replying = false;
            optionsVisible = false;
            current = null;
            HideOptions();
            if (panel != null)
                panel.SetActive(false);
        }

        void ShowChrome()
        {
            if (panel == null)
                return;

            Transform player = panel.transform.Find("PlayerDialogue");
            if (player != null)
                player.gameObject.SetActive(true);

            Transform frame = player != null ? player.Find("DialogueImage") : null;
            if (frame != null)
                frame.gameObject.SetActive(true);

            if (nameText != null)
                nameText.gameObject.SetActive(true);
            if (profileImage != null)
                profileImage.gameObject.SetActive(true);
            if (bodyText != null)
                bodyText.gameObject.SetActive(true);
        }

        void HideJudgment()
        {
            if (panel == null)
                return;

            Transform judgment = panel.transform.Find("DialogueJudgment");
            if (judgment != null)
                judgment.gameObject.SetActive(false);

            Transform cursor = panel.transform.Find("Cursor");
            if (cursor != null)
                cursor.gameObject.SetActive(false);
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

                NpcOption option = choices[i];
                TMP_Text label = FindChildText(button.transform, "Label");
                if (label == null)
                    Debug.LogError(button.name + " 下需要有 Label。", button);
                else
                    label.text = NpcCatalog.Pick(language, option.displayNameZh, option.displayNameEn);

                TMP_Text reward = FindChildText(button.transform, "RewardText");
                if (reward != null)
                    reward.gameObject.SetActive(false);
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

        void WriteLine()
        {
            if (bodyText == null)
                return;

            bodyText.text = lineIndex >= 0 && lineIndex < lines.Count ? lines[lineIndex] : "";
        }

        List<string> CollectLines(string optionId)
        {
            var result = new List<NpcLine>();
            if (current != null && current.lines != null)
            {
                for (int i = 0; i < current.lines.Count; i++)
                {
                    NpcLine line = current.lines[i];
                    if (line == null)
                        continue;

                    string owner = line.optionId ?? "";
                    if (owner == (optionId ?? ""))
                        result.Add(line);
                }
            }

            result.Sort((a, b) => a.order.CompareTo(b.order));
            var texts = new List<string>();
            for (int i = 0; i < result.Count; i++)
                texts.Add(NpcCatalog.Pick(language, result[i].textZh, result[i].textEn));

            return texts;
        }

        void Grant(NpcOption option)
        {
            if (option == null || option.rewards == null)
                return;

            for (int i = 0; i < option.rewards.Count; i++)
            {
                NpcReward reward = option.rewards[i];
                if (reward == null || reward.count <= 0 || string.IsNullOrEmpty(reward.rewardId))
                    continue;

                if (reward.kind == NpcRewardKind.Prop)
                    AddProp(reward.rewardId, reward.count);
                else
                    AddIngredient(reward.rewardId, reward.count);
            }
        }

        void AddIngredient(string ingredientId, int count)
        {
            if (FoodTray.Lookup(ingredientId) == null)
                Debug.LogError("缺少食材 " + ingredientId + "。", this);

            IngredientInventory.Add(new IngredientInstance
            {
                InstanceId = NextInstanceId(),
                DefinitionId = ingredientId,
                Quality = QualityTier.Normal,
                Quantity = count,
                Doneness = 0f,
                Shape = CutShape.Whole,
                CookMethod = CookMethod.Raw,
                SeasoningIds = new List<string>(),
                IsCharcoal = false
            });
        }

        void AddProp(string propId, int count)
        {
            if (catalog == null || catalog.FindProp(propId) == null)
                Debug.LogError("缺少道具 " + propId + "。", catalog != null ? catalog : this);

            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null)
            {
                Debug.LogError("GameSession 缺少道具列表。", this);
                return;
            }

            if (root.Session.Props == null)
                root.Session.Props = new List<PropStack>();

            for (int i = 0; i < root.Session.Props.Count; i++)
            {
                PropStack stack = root.Session.Props[i];
                if (stack != null && stack.PropId == propId)
                {
                    stack.Quantity += count;
                    return;
                }
            }

            root.Session.Props.Add(new PropStack
            {
                PropId = propId,
                Quantity = count
            });
        }

        static int NextInstanceId()
        {
            GameRoot root = GameRoot.Instance;
            int next = 1;
            if (root == null || root.Session == null || root.Session.Inventory == null)
                return next;

            for (int i = 0; i < root.Session.Inventory.Count; i++)
            {
                IngredientInstance item = root.Session.Inventory[i];
                if (item != null && item.InstanceId >= next)
                    next = item.InstanceId + 1;
            }

            return next;
        }

        void Remember(string npcId)
        {
            if (string.IsNullOrEmpty(npcId))
                return;

            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null)
                return;

            if (root.Session.ResolvedNpcIds == null)
                root.Session.ResolvedNpcIds = new List<string>();

            if (!Contains(root.Session.ResolvedNpcIds, npcId))
                root.Session.ResolvedNpcIds.Add(npcId);
        }

        static bool Contains(List<string> ids, string id)
        {
            if (ids == null)
                return false;

            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] == id)
                    return true;
            }

            return false;
        }

        void ResolveWidgets()
        {
            if (panel == null)
                return;

            Transform player = panel.transform.Find("PlayerDialogue");
            if (bodyText == null && player != null)
            {
                Transform found = player.Find("BodyText");
                if (found != null)
                    bodyText = found.GetComponent<TMP_Text>();
            }

            if (player != null)
            {
                if (profileImage == null)
                {
                    Transform found = player.Find("PlayerProfileImage");
                    if (found != null)
                        profileImage = found.GetComponent<Image>();
                }

                if (nameText == null)
                {
                    Transform found = player.Find("UserNameText");
                    if (found != null)
                        nameText = found.GetComponent<TMP_Text>();
                }
            }

            if (option1 == null)
                option1 = FindOption("Option1");
            if (option2 == null)
                option2 = FindOption("Option2");
            if (option3 == null)
                option3 = FindOption("Option3");
        }

        Button FindOption(string widgetName)
        {
            if (panel == null)
                return null;

            Transform found = panel.transform.Find(widgetName);
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
    }
}
