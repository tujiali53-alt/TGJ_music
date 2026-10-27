using System;

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
        public string logText;
    }
}
