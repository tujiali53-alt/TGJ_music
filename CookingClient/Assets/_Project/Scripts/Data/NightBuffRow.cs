using System;
using UnityEngine.Serialization;

namespace Cooking.Data
{
    [Serializable]
    public struct NightBuffRow
    {
        public float minDeliciousness;
        public float maxDeliciousness;
        public float speedMultiplier;
        public int successFaceDelta;
        public float radiusBonus;
        [FormerlySerializedAs("logText")]
        public string logTextId;
    }
}
