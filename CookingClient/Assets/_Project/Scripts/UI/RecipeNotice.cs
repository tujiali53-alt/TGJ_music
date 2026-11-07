using System.Collections;
using Cooking.Core;
using Cooking.Data;
using Cooking.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace Cooking.UI
{
    public sealed class RecipeNotice : MonoBehaviour
    {
        [SerializeField] GameObject redPoint;
        [Tooltip("dayIndex 小于 2 的白天，进入后等待这么多秒再自动打开食谱。")]
        [SerializeField] float autoOpenDelay = 1.5f;

        RecipeDiaryPanels panels;
        Button openRecipeButton;
        bool opened;

        void Awake()
        {
            Resolve();
            HideRedPoint();

            if (redPoint == null)
                Debug.LogError("需要 InteractionPanel/RedPoint_recipe。", this);
            if (panels == null)
                Debug.LogError("Canvas 上需要 RecipeDiaryPanels。", this);
            if (openRecipeButton == null)
                Debug.LogError("需要 InteractionPanel/openRecipeButton，并且带 Button。", this);
        }

        void Start()
        {
            if (openRecipeButton != null)
                openRecipeButton.onClick.AddListener(OnRecipeClicked);

            if (WaitForDay1TutorialWakeup())
                return;

            if (AutoOpens(CurrentDayIndex()))
            {
                StartCoroutine(OpenAfterDelay());
                return;
            }

            ShowRedPoint();
        }

        static bool WaitForDay1TutorialWakeup()
        {
            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null)
                return false;

            GameSession session = root.Session;
            if (session.DayIndex != 1 || session.Phase != DayPhase.Day)
                return false;

            if (session.HasSeenTutorial(TutorialIds.Completed))
                return false;

            return !session.HasSeenTutorial(TutorialIds.Day1Wakeup);
        }

        public void OpenNowForTutorial()
        {
            if (opened || panels == null)
                return;

            opened = true;
            HideRedPoint();
            panels.OpenRecipe();
        }

        public void ShowRedPointForTutorial()
        {
            if (!opened)
                ShowRedPoint();
        }

        void OnDestroy()
        {
            if (openRecipeButton != null)
                openRecipeButton.onClick.RemoveListener(OnRecipeClicked);
        }

        public static bool AutoOpens(int dayIndex)
        {
            return dayIndex < 2;
        }

        void OnRecipeClicked()
        {
            opened = true;
            HideRedPoint();
        }

        IEnumerator OpenAfterDelay()
        {
            if (autoOpenDelay > 0f)
                yield return new WaitForSeconds(autoOpenDelay);

            if (opened || panels == null)
                yield break;

            opened = true;
            panels.OpenRecipe();
        }

        void HideRedPoint()
        {
            if (redPoint != null)
                redPoint.SetActive(false);
        }

        void ShowRedPoint()
        {
            if (redPoint != null)
                redPoint.SetActive(true);
        }

        int CurrentDayIndex()
        {
            GameRoot root = GameRoot.Instance;
            if (root != null && root.Session != null)
                return root.Session.DayIndex;

            return 0;
        }

        void Resolve()
        {
            if (redPoint == null)
            {
                Transform found = transform.Find("RedPoint_recipe");
                if (found != null)
                    redPoint = found.gameObject;
            }

            if (redPoint != null)
            {
                Image image = redPoint.GetComponent<Image>();
                if (image != null)
                    image.raycastTarget = false;
            }

            if (openRecipeButton == null)
            {
                Transform found = transform.Find("openRecipeButton");
                if (found != null)
                    openRecipeButton = found.GetComponent<Button>();
            }

            if (panels == null && transform.parent != null)
                panels = transform.parent.GetComponent<RecipeDiaryPanels>();
        }
    }
}
