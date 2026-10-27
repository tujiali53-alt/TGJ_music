using System.Collections.Generic;
using UnityEngine;

namespace Cooking.Data
{
    [CreateAssetMenu(fileName = "NightBuffTable", menuName = "Cooking/Night Buff Table")]
    public sealed class NightBuffTable : ScriptableObject
    {
        public List<NightBuffRow> rows = new List<NightBuffRow>();
    }
}