using System.Collections;
using System.Collections.Generic;
using Cooking.Core;
using Cooking.Data;
using Cooking.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cooking.UI
{
    public sealed class NightDicePanel : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Button beginButton;
        [SerializeField] Button confirmButton;
        [SerializeField] float confirmDelay = 2f;
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text resultText;
        [SerializeField] IngredientDefinition[] definitions;
        [SerializeField] NightDialogPanel dialog;
        [SerializeField] Image diceImage;
        [SerializeField] Animator diceAnimator;
        [SerializeField] Sprite[] faceSprites;

        const string RollState = "Dice_anim";

        NightEventDefinition current;
        NightOption option;
        int buffDelta;
        bool rolled;
        NightRollResult lastRoll;
        Coroutine confirmWait;
        Coroutine rollPlay;

        void Awake()
        {
            if (panel == null)
            {
                Transform found = transform.Find("DicePanel");
                if (found != null)
                    panel = found.gameObject;
            }

            if (panel != null && beginButton == null)
            {
                Transform found = panel.transform.Find("beginDiceButton");
                if (found != null)
                    beginButton = found.GetComponent<Button>();
            }

            if (panel != null && confirmButton == null)
            {
                Transform found = panel.transform.Find("AfterComfirmDiceButton");
                if (found != null)
                    confirmButton = found.GetComponent<Button>();
            }

            if (panel != null && scoreText == null)
                scoreText = FindText(panel.transform, "scoreText");

            if (panel != null && resultText == null)
                resultText = FindText(panel.transform, "DiceResultText");

            if (panel != null && diceImage == null)
            {
                Transform found = panel.transform.Find("Dice");
                if (found != null)
                    diceImage = found.GetComponent<Image>();
            }

            if (diceImage != null && diceAnimator == null)
                diceAnimator = diceImage.GetComponent<Animator>();

            if (dialog == null)
                dialog = GetComponent<NightDialogPanel>();

            if (panel == null)
                Debug.LogError("InteractionPanel 下需要有 DicePanel。", this);
            if (beginButton == null)
                Debug.LogError("DicePanel 下需要有 beginDiceButton，并且带 Button。", this);
            else
                beginButton.onClick.AddListener(OnBegin);

            if (confirmButton == null)
                Debug.LogError("DicePanel 下需要有 AfterComfirmDiceButton，并且带 Button。", this);
            else
                confirmButton.onClick.AddListener(OnConfirm);

            if (scoreText == null)
                Debug.LogError("DicePanel 下需要有 scoreText。", this);
            if (resultText == null)
                Debug.LogError("DicePanel 下需要有 DiceResultText。", this);
            if (diceImage == null)
                Debug.LogError("DicePanel 下需要有 Dice，并且带 Image。", this);
            if (diceAnimator == null)
                Debug.LogError("Dice 上需要有 Animator，并使用 Dice 控制器。", this);
            if (faceSprites == null || faceSprites.Length < 6)
                Debug.LogError("NightDicePanel 需要按 1 到 6 绑上骰子图。", this);
            if (definitions == null || definitions.Length == 0)
                Debug.LogError("NightDicePanel 需要绑上食材定义。", this);

            HoldDice();
            if (panel != null)
                panel.SetActive(false);
        }

        public void Open(NightEventDefinition definition, NightOption chosen, int buffFaceDelta)
        {
            current = definition;
            option = chosen;
            buffDelta = buffFaceDelta;
            rolled = false;
            lastRoll = null;
            StopConfirmWait();
            StopRoll();
            HoldDice();
            if (scoreText != null)
                scoreText.text = "";
            if (resultText != null)
                resultText.text = "";
            if (beginButton != null)
                beginButton.gameObject.SetActive(true);
            if (confirmButton != null)
                confirmButton.gameObject.SetActive(false);
            if (panel != null)
            {
                panel.SetActive(true);
                Canvas.ForceUpdateCanvases();
            }
        }

        public bool IsOpen => panel != null && panel.activeSelf;

        public IngredientDefinition[] Definitions => definitions;

        public string Describe(NightOption source)
        {
            if (source.rewards == null || source.rewards.Count == 0)
                return "";

            bool english = GameLanguage.Current == DiaryLanguage.En;
            string allJoin = english ? ", " : "，";
            string oneJoin = english ? " or " : "或";
            string times = english ? " x " : "×";
            string all = "";
            string one = "";
            for (int i = 0; i < source.rewards.Count; i++)
            {
                NightReward reward = source.rewards[i];
                string piece = DisplayName(reward.ingredientId) + times + reward.count;
                if (reward.pickMode == NightRewardPick.One)
                    one = Join(one, piece, oneJoin);
                else
                    all = Join(all, piece, allJoin);
            }

            if (all.Length == 0)
                return one;
            if (one.Length == 0)
                return all;
            return all + allJoin + one;
        }

        void OnBegin()
        {
            if (rolled || rollPlay != null || panel == null || !panel.activeSelf)
                return;

            if (beginButton != null)
                beginButton.gameObject.SetActive(false);

            AudioManager.PlayClick();
            rollPlay = StartCoroutine(RollAfterAnimation());
        }

        IEnumerator RollAfterAnimation()
        {
            AudioManager.Post("Play_Dice_Roll");
            NightRollResult result = NightCheck.Roll(option, buffDelta);
            if (diceAnimator != null)
            {
                diceAnimator.enabled = true;
                diceAnimator.Play(RollState, 0, 0f);
                diceAnimator.Update(0f);
                float length = diceAnimator.GetCurrentAnimatorStateInfo(0).length;
                if (length <= 0f)
                    length = 1.0166667f;
                yield return new WaitForSeconds(length);
                diceAnimator.enabled = false;
            }

            rollPlay = null;
            if (panel == null || !panel.activeSelf)
                yield break;

            Present(result);
        }

        void Present(NightRollResult result)
        {
            lastRoll = result;
            rolled = true;
            ShowFace(result.Face);
            if (scoreText != null)
                scoreText.text = result.Face.ToString();
            bool english = GameLanguage.Current == DiaryLanguage.En;
            if (resultText != null)
                resultText.text = result.Success
                    ? (english ? "Success" : "成功")
                    : (english ? "Fail" : "失败");

            string name = NightEventDefinition.Pick(
                GameLanguage.Current,
                option.displayNameZh,
                option.displayNameEn);
            if (string.IsNullOrEmpty(name))
                name = option.optionId;
            NightLogView.Append(
                NightLogKind.DiceRoll,
                english ? "Rolled " + result.Face : "掷出 " + result.Face + " 点");
            NightLogView.Append(
                NightLogKind.CheckResult,
                name + (english ? ": " : "：")
                + (result.Success
                    ? (english ? "Success" : "成功")
                    : (english ? "Fail" : "失败")));
            Remember(option.optionId, result.Success);
            ScheduleConfirm();

            if (!result.Success || result.Rewards == null)
                return;

            for (int i = 0; i < result.Rewards.Count; i++)
                Grant(result.Rewards[i]);
        }

        void HoldDice()
        {
            if (diceAnimator != null)
                diceAnimator.enabled = false;
            ShowFace(1);
        }

        void ShowFace(int face)
        {
            if (diceImage == null || faceSprites == null)
                return;

            int index = face - 1;
            if (index < 0 || index >= faceSprites.Length || faceSprites[index] == null)
                return;

            diceImage.sprite = faceSprites[index];
        }

        void StopRoll()
        {
            if (rollPlay == null)
                return;

            StopCoroutine(rollPlay);
            rollPlay = null;
            if (diceAnimator != null)
                diceAnimator.enabled = false;
        }

        void ScheduleConfirm()
        {
            StopConfirmWait();
            confirmWait = StartCoroutine(ShowConfirmAfterDelay());
        }

        IEnumerator ShowConfirmAfterDelay()
        {
            float wait = Mathf.Max(0f, confirmDelay);
            if (wait > 0f)
                yield return new WaitForSeconds(wait);

            confirmWait = null;
            if (!rolled || panel == null || !panel.activeSelf || confirmButton == null)
                yield break;

            confirmButton.gameObject.SetActive(true);
        }

        void OnConfirm()
        {
            AudioManager.PlayClick();
            StopConfirmWait();
            StopRoll();
            bool success = lastRoll != null && lastRoll.Success;
            List<NightReward> rewards = lastRoll != null ? lastRoll.Rewards : null;
            if (panel != null)
                panel.SetActive(false);
            if (dialog != null)
                dialog.OpenResult(current, option, success, rewards);
        }

        void StopConfirmWait()
        {
            if (confirmWait == null)
                return;

            StopCoroutine(confirmWait);
            confirmWait = null;
        }

        void Grant(NightReward reward)
        {
            if (string.IsNullOrEmpty(reward.ingredientId) || reward.count <= 0)
                return;

            string display = DisplayName(reward.ingredientId);
            if (FindDefinition(reward.ingredientId) == null)
                Debug.LogError("缺少食材 " + reward.ingredientId + "。", this);

            IngredientInventory.Add(new IngredientInstance
            {
                InstanceId = NextInstanceId(),
                DefinitionId = reward.ingredientId,
                Quality = QualityTier.Normal,
                Quantity = reward.count,
                Doneness = 0f,
                Shape = CutShape.Whole,
                CookMethod = CookMethod.Raw,
                SeasoningIds = new List<string>(),
                IsCharcoal = false
            });
            NightLogView.Append(NightLogKind.IngredientGain, "获得" + display + "×" + reward.count);
        }

        static void Remember(string optionId, bool success)
        {
            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null || string.IsNullOrEmpty(optionId))
                return;

            if (root.Session.ResolvedOptionIdsTonight != null
                && !root.Session.ResolvedOptionIdsTonight.Contains(optionId))
                root.Session.ResolvedOptionIdsTonight.Add(optionId);

            if (!success || root.Session.RevealedRewardOptionIds == null)
                return;

            if (!root.Session.RevealedRewardOptionIds.Contains(optionId))
                root.Session.RevealedRewardOptionIds.Add(optionId);
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

        IngredientDefinition FindDefinition(string id)
        {
            if (definitions == null)
                return null;

            for (int i = 0; i < definitions.Length; i++)
            {
                IngredientDefinition definition = definitions[i];
                if (definition != null && definition.id == id)
                    return definition;
            }

            return null;
        }

        string DisplayName(string ingredientId)
        {
            IngredientDefinition definition = FindDefinition(ingredientId);
            string name = IngredientDetailText.DisplayName(definition, GameLanguage.Current);
            return string.IsNullOrEmpty(name) ? ingredientId : name;
        }

        static string Join(string left, string right, string separator)
        {
            if (string.IsNullOrEmpty(left))
                return right;
            if (string.IsNullOrEmpty(right))
                return left;
            return left + separator + right;
        }

        static TMP_Text FindText(Transform parent, string childName)
        {
            Transform found = parent.Find(childName);
            if (found == null)
                return null;

            return found.GetComponent<TMP_Text>();
        }
    }
}
