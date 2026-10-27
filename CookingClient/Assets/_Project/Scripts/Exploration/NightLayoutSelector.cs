using System.Globalization;
using Cooking.Core;
using Cooking.Data;
using Cooking.Runtime;
using Cooking.UI;
using UnityEngine;

namespace Cooking.Exploration
{
    public sealed class NightLayoutSelector : MonoBehaviour
    {
        [SerializeField] NightBuffTable buffs;

        public static int SuccessFaceDelta { get; private set; }

        void Awake()
        {
            if (buffs == null)
            {
                Debug.LogError("NightLayouts 上需要已经放好夜晚 buff 表。", this);
                return;
            }
        }

        void Start()
        {
            BeginNight();
        }

        public void BeginNight()
        {
            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null)
            {
                Debug.LogError("进入夜晚需要已经放好的 GameRoot。", this);
                return;
            }

            GameSession session = root.Session;
            session.LightsAbsorbedTonight = 0;
            if (session.ResolvedOptionIdsTonight != null)
                session.ResolvedOptionIdsTonight.Clear();
            if (session.NightLog != null)
                session.NightLog.Clear();

            PlayerMotor motor = FindAnyObjectByType<PlayerMotor>();
            PlayerLight light = FindAnyObjectByType<PlayerLight>();
            bool firstNight = session.DayIndex == 0;
            if (firstNight)
            {
                SuccessFaceDelta = 0;
                if (motor != null)
                    motor.SpeedMultiplier = 1f;
                if (light != null)
                    light.BuffRadiusBonus = 0f;
            }
            else
            {
                NightBuffRow row = NightBuffs.Match(
                    session.TodayDeliciousness,
                    buffs != null ? buffs.rows : null);
                SuccessFaceDelta = row.successFaceDelta;
                if (motor != null)
                    motor.SpeedMultiplier = row.speedMultiplier;
                if (light != null)
                    light.BuffRadiusBonus = row.radiusBonus;

                if (!string.IsNullOrEmpty(row.logText))
                    NightLogView.Append(NightLogKind.Buff, row.logText);

                if (light != null && Mathf.Abs(row.radiusBonus) > 0.001f)
                {
                    string radius = light.RadiusFor(session.LightsAbsorbedTonight).ToString("0.00", CultureInfo.InvariantCulture);
                    NightLogView.Append(NightLogKind.RadiusChanged, "照亮范围变为 " + radius);
                }
            }

            ApplyLayout(session.DayIndex);
            RestoreLights();
        }

        void ApplyLayout(int dayIndex)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                NightLayout layout = child.GetComponent<NightLayout>();
                if (layout == null)
                    continue;

                child.gameObject.SetActive(layout.NightIndex == dayIndex);
            }
        }

        static void RestoreLights()
        {
            LighterPoint[] lights = FindObjectsByType<LighterPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                LighterPoint light = lights[i];
                if (light == null || HiddenByLayout(light))
                    continue;

                light.Restore();
            }
        }

        static bool HiddenByLayout(LighterPoint light)
        {
            NightLayout layout = light.GetComponentInParent<NightLayout>(true);
            return layout != null && !layout.gameObject.activeInHierarchy;
        }
    }
}
