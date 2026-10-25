using Cooking.Core;
using Cooking.Data;
using TMPro;
using UnityEngine;

namespace Cooking.Exploration
{
    public sealed class SessionHud : MonoBehaviour
    {
        [SerializeField] TMP_Text dayText;
        [SerializeField] TMP_Text statusText;

        void Awake()
        {
            if (dayText == null)
            {
                Transform day = transform.Find("DayText");
                if (day != null)
                    dayText = day.GetComponent<TMP_Text>();
            }

            if (statusText == null)
            {
                Transform status = transform.Find("StatusText");
                if (status != null)
                    statusText = status.GetComponent<TMP_Text>();
            }
        }

        void LateUpdate()
        {
            Refresh();
        }

        public void Refresh()
        {
            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null)
                return;

            if (dayText != null)
                dayText.text = FormatDay(root.Session.DayIndex);

            if (statusText != null)
                statusText.text = FormatStatus(root.Session.Phase);
        }

        public static string FormatDay(int dayIndex)
        {
            return "Day " + dayIndex;
        }

        public static string FormatStatus(DayPhase phase)
        {
            if (phase == DayPhase.Day)
                return "daylight";

            return "Night";
        }
    }
}
