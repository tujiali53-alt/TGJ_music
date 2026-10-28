using Cooking.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cooking.UI
{
    public sealed class PotArea : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] CookMethod method = CookMethod.Fried;

        public CookMethod Method => method;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                TrayToggle.OpenCurrent();
        }
    }
}
