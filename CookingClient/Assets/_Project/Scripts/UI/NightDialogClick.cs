using UnityEngine;
using UnityEngine.EventSystems;

namespace Cooking.UI
{
    public sealed class NightDialogClick : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] NightDialogPanel panel;
        [SerializeField] NpcDialogPanel npc;
        [SerializeField] EndingSequenceController ending;
        [SerializeField] TutorialDialogPlayer tutorial;

        public void Bind(NightDialogPanel owner)
        {
            panel = owner;
        }

        public void BindEnding(EndingSequenceController owner)
        {
            ending = owner;
        }

        public void BindTutorial(TutorialDialogPlayer owner)
        {
            tutorial = owner;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (tutorial == null)
                tutorial = GetComponentInParent<TutorialDialogPlayer>(true);
            if (tutorial == null && TutorialDirector.Instance != null)
                tutorial = TutorialDirector.Instance.GetComponent<TutorialDialogPlayer>();

            if (tutorial != null && tutorial.IsActive)
            {
                tutorial.OnPanelClick(eventData);
                return;
            }

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
