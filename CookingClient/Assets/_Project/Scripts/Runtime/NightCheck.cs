using System.Collections.Generic;
using Cooking.Data;
using UnityEngine;

namespace Cooking.Runtime
{
    public sealed class NightRollResult
    {
        public int Face;
        public bool Success;
        public List<NightReward> Rewards = new List<NightReward>();
    }

    public static class NightCheck
    {
        public static int EffectiveFaces(int successFaces, int buffDelta)
        {
            int faces = successFaces + buffDelta;
            if (faces < 1)
                faces = 1;
            if (faces > 6)
                faces = 6;
            return faces;
        }

        public static NightRollResult Roll(NightOption option, int buffDelta)
        {
            int face = Random.Range(1, 7);
            int oneCount = CountPickOne(option.rewards);
            int oneIndex = oneCount > 0 ? Random.Range(0, oneCount) : 0;
            return Resolve(option, face, buffDelta, oneIndex);
        }

        public static NightRollResult Resolve(NightOption option, int face, int buffDelta, int onePickIndex)
        {
            if (face < 1)
                face = 1;
            if (face > 6)
                face = 6;

            bool success = face <= EffectiveFaces(option.successFaces, buffDelta);
            var result = new NightRollResult
            {
                Face = face,
                Success = success
            };
            if (success)
                result.Rewards = SelectRewards(option.rewards, onePickIndex);
            return result;
        }

        public static List<NightReward> SelectRewards(IList<NightReward> rewards, int onePickIndex)
        {
            var chosen = new List<NightReward>();
            var one = new List<NightReward>();
            if (rewards == null)
                return chosen;

            for (int i = 0; i < rewards.Count; i++)
            {
                NightReward reward = rewards[i];
                if (reward.pickMode == NightRewardPick.One)
                    one.Add(reward);
                else
                    chosen.Add(reward);
            }

            if (one.Count == 0)
                return chosen;

            int index = onePickIndex % one.Count;
            if (index < 0)
                index += one.Count;
            chosen.Add(one[index]);
            return chosen;
        }

        static int CountPickOne(IList<NightReward> rewards)
        {
            if (rewards == null)
                return 0;

            int count = 0;
            for (int i = 0; i < rewards.Count; i++)
            {
                if (rewards[i].pickMode == NightRewardPick.One)
                    count++;
            }

            return count;
        }
    }
}
