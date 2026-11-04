using UnityEngine;
using UnityEngine.EventSystems;

namespace Cooking.UI
{
    public sealed class NightDialogClick : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] NightDialogPanel panel;
        [SerializeField] NpcDialogPanel npc;

        public void Bind(NightDialogPanel owner)
        {
            panel = owner;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (npc == null)
                npc = GetComponentInParent<NpcDialogPanel>(true);

            if (npc != null && npc.IsOpen)
            {
                npc.OnPanelClick(eventData);
                return;
            }

            if (panel != null)
                panel.OnPanelClick(eventData);
        }
    }
}
