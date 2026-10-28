using UnityEngine;
using UnityEngine.EventSystems;

namespace Cooking.UI
{
    public sealed class NightDialogClick : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] NightDialogPanel panel;

        public void Bind(NightDialogPanel owner)
        {
            panel = owner;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (panel != null)
                panel.OnPanelClick(eventData);
        }
    }
}
