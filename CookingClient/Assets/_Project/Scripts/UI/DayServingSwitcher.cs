using Cooking.Core;
using UnityEngine;

namespace Cooking.UI
{
    public sealed class DayServingSwitcher : MonoBehaviour
    {
        public const int HotpotDay = 7;

        [SerializeField] GameObject plate;
        [SerializeField] GameObject hotPot;

        void Awake()
        {
            if (plate == null)
            {
                Transform found = transform.Find("Plate");
                if (found != null)
                    plate = found.gameObject;
            }

            if (hotPot == null)
            {
                Transform found = transform.Find("Hot_pot");
                if (found != null)
                    hotPot = found.gameObject;
            }

            if (plate == null)
                Debug.LogError("CookingPanel 下需要已经放好的 Plate。", this);

            if (hotPot == null)
                Debug.LogError("CookingPanel 下需要已经放好的 Hot_pot。", this);
        }

        void OnEnable()
        {
            Apply();
        }

        void Start()
        {
            Apply();
        }

        public void Apply()
        {
            bool useHotpot = IsHotpotDay();
            if (plate != null)
                plate.SetActive(!useHotpot);

            if (hotPot != null)
                hotPot.SetActive(useHotpot);
        }

        static bool IsHotpotDay()
        {
            GameRoot root = GameRoot.Instance;
            return root != null && root.Session != null && root.Session.DayIndex == HotpotDay;
        }
    }
}
