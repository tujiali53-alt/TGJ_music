using System.Collections.Generic;
using Cooking.Data;
using Cooking.Runtime;
using UnityEditor;
using UnityEngine;

namespace Cooking.EditorTools
{
    public static class NightRollCheck
    {
        [MenuItem("Cooking/Check Night Roll")]
        public static void RunMenu()
        {
            string error = Run();
            if (error == null)
                Debug.Log("夜晚掷点通过：连根拔起 4 成功 5 失败，减 1 面后 4 失败，拉竿只给鱼或虾。");
            else
                Debug.LogError(error);
        }

        public static string Run()
        {
            NightOption pull = PullOption();
            NightRollResult success = NightCheck.Resolve(pull, 4, 0, 0);
            if (!success.Success || success.Face != 4)
                return "连根拔起掷出 4 应成功。";
            if (!HasReward(success.Rewards, "ing_mushroom", 10) || !HasReward(success.Rewards, "ing_potato", 1))
                return "连根拔起成功应同时给白蘑菇 10 和星土豆 1。";
            if (success.Rewards.Count != 2)
                return "连根拔起成功应正好两行奖励。";

            NightRollResult fail = NightCheck.Resolve(pull, 5, 0, 0);
            if (fail.Success || fail.Rewards.Count != 0)
                return "连根拔起掷出 5 应失败且没有奖励。";

            NightBuffRow low = NightBuffs.Match(0f, SampleBuffs());
            if (low.successFaceDelta != -1)
                return "美味度 0 的成功率加减应为 -1。";

            NightRollResult weakened = NightCheck.Resolve(pull, 4, low.successFaceDelta, 0);
            if (weakened.Success)
                return "成功率减 1 面时，连根拔起掷出 4 应失败。";

            NightOption cast = CastOption();
            NightRollResult fish = NightCheck.Resolve(cast, 5, 0, 0);
            NightRollResult shrimp = NightCheck.Resolve(cast, 5, 0, 1);
            if (!fish.Success || fish.Rewards.Count != 1 || !HasReward(fish.Rewards, "ing_cornfish", 5))
                return "拉竿选取第 1 行时应只给玉米鱼 5。";
            if (!shrimp.Success || shrimp.Rewards.Count != 1 || !HasReward(shrimp.Rewards, "ing_shrimp", 3))
                return "拉竿选取第 2 行时应只给红斑虾 3。";

            return null;
        }

        static NightOption PullOption()
        {
            return new NightOption
            {
                optionId = "opt_mushroom_pull",
                order = 1,
                displayName = "连根拔起",
                successFaces = 4,
                rewards = new List<NightReward>
                {
                    new NightReward { pickMode = NightRewardPick.All, ingredientId = "ing_mushroom", count = 10 },
                    new NightReward { pickMode = NightRewardPick.All, ingredientId = "ing_potato", count = 1 }
                }
            };
        }

        static NightOption CastOption()
        {
            return new NightOption
            {
                optionId = "opt_fishing_cast",
                order = 1,
                displayName = "拉竿！",
                conditionType = NightConditionType.DeliciousnessAbove,
                conditionValue = 60f,
                successFaces = 5,
                rewards = new List<NightReward>
                {
                    new NightReward { pickMode = NightRewardPick.One, ingredientId = "ing_cornfish", count = 5 },
                    new NightReward { pickMode = NightRewardPick.One, ingredientId = "ing_shrimp", count = 3 }
                }
            };
        }

        static List<NightBuffRow> SampleBuffs()
        {
            return new List<NightBuffRow>
            {
                new NightBuffRow { minDeliciousness = 0f, maxDeliciousness = 10f, speedMultiplier = 0.8f, successFaceDelta = -1, logText = "今夜：移速降低，事件成功率降低" },
                new NightBuffRow { minDeliciousness = 11f, maxDeliciousness = 40f, speedMultiplier = 0.8f, successFaceDelta = 0, logText = "今夜：移速降低" },
                new NightBuffRow { minDeliciousness = 41f, maxDeliciousness = 100f, speedMultiplier = 1f, successFaceDelta = 0, logText = "今夜：状态正常" },
                new NightBuffRow { minDeliciousness = 101f, maxDeliciousness = 9999f, speedMultiplier = 1.2f, successFaceDelta = 0, radiusBonus = 0.5f, logText = "今夜：移速提升，照亮范围增加" }
            };
        }

        static bool HasReward(List<NightReward> rewards, string ingredientId, int count)
        {
            for (int i = 0; i < rewards.Count; i++)
            {
                NightReward reward = rewards[i];
                if (reward.ingredientId == ingredientId && reward.count == count)
                    return true;
            }

            return false;
        }
    }
}
