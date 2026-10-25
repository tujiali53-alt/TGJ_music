using System.Collections;
using Cooking.Core;
using Cooking.Data;
using Cooking.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cooking.UI
{
    public sealed class MealEatController : MonoBehaviour
    {
        public const int MealsPerDay = 3;

        [SerializeField] Button eatButton;
        [SerializeField] Button enterNightButton;
        [SerializeField] TMP_Text mealsText;
        [SerializeField] GameObject resultPanel;
        [SerializeField] float resultDelay = 1f;

        bool resolving;

        void Awake()
        {
            if (eatButton == null)
                eatButton = FindButton("EattingButton");

            if (enterNightButton == null)
                enterNightButton = FindButton("enterNightButton");

            if (mealsText == null)
                mealsText = FindText("NumberOfMealsText");

            if (resultPanel == null)
            {
                Transform found = FindNamed("EatingCheckPanel");
                if (found != null)
                    resultPanel = found.gameObject;
            }

            if (eatButton == null)
                Debug.LogError("需要 InteractionPanel/EattingButton，并且带 Button。", this);
            else
                eatButton.onClick.AddListener(Eat);

            if (enterNightButton == null)
                Debug.LogError("需要 EatingCheckPanel/enterNightButton，并且带 Button。", this);
            else
                enterNightButton.onClick.AddListener(CloseResult);

            if (mealsText == null)
                Debug.LogError("需要 NumberOfMealsText。", this);

            if (resultPanel == null)
                Debug.LogError("需要 EatingCheckPanel。", this);
            else
                resultPanel.SetActive(false);
        }

        void LateUpdate()
        {
            if (mealsText == null)
                return;

            GameSession session = Session();
            int meals = session != null ? session.MealsEaten : 0;
            mealsText.text = meals + "/" + MealsPerDay;
        }

        public void Eat()
        {
            if (resolving)
                return;

            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null || root.Flow == null)
            {
                Debug.LogError("进食需要已经放好的 GameRoot、GameSession 和 SceneFlow。", this);
                return;
            }

            if (root.Flow.IsLoading)
                return;

            if (root.Session.MealsEaten >= MealsPerDay)
            {
                Debug.Log("今天的进食次数已经用完。", this);
                return;
            }

            if (Plate.FirstHeld() == null)
            {
                Debug.Log("盘子上没有食物，不能进食。", this);
                return;
            }

            root.Session.HeldDish.Items.Clear();
            root.Session.MealsEaten += 1;
            ReturnTokenToTray();
            resolving = true;

            if (resultDelay <= 0f)
                ShowResult();
            else
                StartCoroutine(ShowResultAfterDelay());
        }

        IEnumerator ShowResultAfterDelay()
        {
            yield return new WaitForSeconds(resultDelay);
            ShowResult();
        }

        void ShowResult()
        {
            if (resultPanel != null)
                resultPanel.SetActive(true);
        }

        public void CloseResult()
        {
            if (resultPanel != null)
                resultPanel.SetActive(false);

            resolving = false;

            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null || root.Flow == null)
                return;

            if (root.Session.MealsEaten < MealsPerDay)
                return;

            root.Session.Phase = DayPhase.Night;
            root.Session.MealsEaten = 0;
            root.Session.HeldDish = new Dish { DishId = root.Session.HeldDish != null ? root.Session.HeldDish.DishId + 1 : 1, ExecutionScore = 1f };
            root.Flow.Load(SceneNames.Outing);
        }

        static void ReturnTokenToTray()
        {
            FoodSlotDrag drag = FindToken();
            if (drag != null)
                drag.ReturnToTray();
        }

        static FoodSlotDrag FindToken()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                FoodSlotDrag drag = roots[i].GetComponentInChildren<FoodSlotDrag>(true);
                if (drag != null)
                    return drag;
            }

            return null;
        }

        static GameSession Session()
        {
            GameRoot root = GameRoot.Instance;
            if (root == null)
                return null;

            return root.Session;
        }

        Button FindButton(string objectName)
        {
            Transform found = FindNamed(objectName);
            if (found == null)
                return null;

            return found.GetComponent<Button>();
        }

        TMP_Text FindText(string objectName)
        {
            Transform found = FindNamed(objectName);
            if (found == null)
                return null;

            return found.GetComponent<TMP_Text>();
        }

        static Transform FindNamed(string objectName)
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int t = 0; t < transforms.Length; t++)
                {
                    if (transforms[t].name == objectName)
                        return transforms[t];
                }
            }

            return null;
        }
    }
}
