using UnityEngine;

namespace Cooking.Exploration
{
    public sealed class NightLayout : MonoBehaviour
    {
        [SerializeField] int nightIndex;

        public int NightIndex => nightIndex;
    }
}
