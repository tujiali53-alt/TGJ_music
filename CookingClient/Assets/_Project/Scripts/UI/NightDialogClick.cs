using UnityEngine;
using UnityEngine.EventSystems;

namespace Cooking.UI
{
    public sealed class NightDialogClick : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] NightDialogPanel panel;
        [SerializeField] NpcDialogPanel npc;
        [SerializeField] EndingSequenceController ending;

        public void Bind(NightDialogPanel owner)
        {
            panel = owner;
        }

        public void BindEnding(EndingSequenceController owner)
        {
            ending = owner;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (ending == null)
                ending = GetComponentInParent<EndingSequenceController>(true);

            if (ending != null && ending.IsDialogActive)
            {
                ending.OnPanelClick(eventData);
                return;
            }

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
