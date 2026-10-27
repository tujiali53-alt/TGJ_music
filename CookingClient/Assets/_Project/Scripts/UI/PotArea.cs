using Cooking.Data;
using UnityEngine;

namespace Cooking.UI
{
    public sealed class PotArea : MonoBehaviour
    {
        [SerializeField] CookMethod method = CookMethod.Fried;

        public CookMethod Method => method;
    }
}
