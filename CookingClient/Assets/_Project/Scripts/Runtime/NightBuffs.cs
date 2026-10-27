using System.Collections.Generic;
using Cooking.Data;

namespace Cooking.Runtime
{
    public static class NightBuffs
    {
        public static NightBuffRow Match(float deliciousness, IList<NightBuffRow> rows)
        {
            if (rows != null)
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    NightBuffRow row = rows[i];
                    if (deliciousness >= row.minDeliciousness && deliciousness <= row.maxDeliciousness)
                        return row;
                }
            }

            return new NightBuffRow
            {
                minDeliciousness = 41f,
                maxDeliciousness = 100f,
                speedMultiplier = 1f,
                successFaceDelta = 0,
                radiusBonus = 0f,
                logText = ""
            };
        }
    }
}
