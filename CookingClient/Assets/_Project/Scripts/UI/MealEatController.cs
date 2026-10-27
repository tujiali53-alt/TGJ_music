using System.Collections;
using System.Collections.Generic;
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
        [SerializeField] Button oneMoreButton;
        [SerializeField] TMP_Text mealsText;
        [SerializeField] TMP_Text deliciousnessText;
        [SerializeField] TMP_Text ratingFirstText;
        [SerializeField] TMP_Text ratingSecondText;
        [SerializeField] TMP_Text ratingThirdText;
        [SerializeField] TMP_Text ratingFinalText;
        [SerializeField] TMP_Text commentsText;
        [SerializeField] Image finalFoodImage;
        [SerializeField] GameObject resultPanel;
        [SerializeField] float resultDelay = 1f;
        [SerializeField] RecipeDefinition[] recipes;
        [SerializeField] DishPresentation presentation;

        DishResult lastResult;
        bool resolving;

        void Awake()
        {
            if (eatButton == null)
                eatButton = FindButton("EattingButton");

            if (enterNightButton == null)
                enterNightButton = FindButton("enterNightButton");

            if (oneMoreButton == null)
                oneMoreButton = FindButton("OneMoreTimeButton");

            if (mealsText == null)
                mealsText = FindText("NumberOfMealsText");

            if (deliciousnessText == null)
                deliciousnessText = FindText("DeliciousnessText");

            if (ratingFirstText == null)
                ratingFirstText = FindText("RatingFirstText");

            if (ratingSecondText == null)
                ratingSecondText = FindText("RatingSecondText");

            if (ratingThirdText == null)
                ratingThirdText = FindText("RatingThirdText");

            if (ratingFinalText == null)
                ratingFinalText = FindText("RatingFinalText");

            if (commentsText == null)
                commentsText = FindText("CommentsText");

            if (finalFoodImage == null)
            {
                Transform found = FindNamed("FinalFoodImage");
                if (found != null)
                    finalFoodImage = found.GetComponent<Image>();
            }

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
                enterNightButton.onClick.AddListener(EnterNight);

            if (oneMoreButton == null)
                Debug.LogError("需要 CheckPanel/OneMoreTimeButton，并且带 Button。", this);
            else
                oneMoreButton.onClick.AddListener(ClosePanel);

            if (mealsText == null)
                Debug.LogError("需要 NumberOfMealsText。", this);

            if (deliciousnessText == null)
                Debug.LogError("需要 DeliciousnessText。", this);

            if (ratingFirstText == null || ratingSecondText == null || ratingThirdText == null || ratingFinalText == null)
                Debug.LogError("需要 RatingFirstText、RatingSecondText、RatingThirdText 和 RatingFinalText。", this);

            if (commentsText == null)
                Debug.LogError("需要 CommentsText。", this);

            if (finalFoodImage == null)
                Debug.LogError("需要 CheckPanel/FinalFoodImage。", this);

            if (resultPanel == null)
                Debug.LogError("需要 EatingCheckPanel。", this);
            else
                resultPanel.SetActive(false);
        }

        void OnDestroy()
        {
            if (eatButton != null)
                eatButton.onClick.RemoveListener(Eat);

            if (enterNightButton != null)
                enterNightButton.onClick.RemoveListener(EnterNight);

            if (oneMoreButton != null)
                oneMoreButton.onClick.RemoveListener(ClosePanel);
        }

        void LateUpdate()
        {
            GameSession session = Session();
            if (mealsText != null)
            {
                int meals = session != null ? session.MealsEaten : 0;
                mealsText.text = meals + "/" + MealsPerDay;
            }

            if (deliciousnessText != null)
            {
                float value = session != null ? session.TodayDeliciousness : 0f;
                deliciousnessText.text = "美味度：" + DeliciousnessScore.FormatScore(value);
            }
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

            GameSession session = root.Session;
            bool hasFood = Plate.HasFood();
            if (session.MealsEaten >= MealsPerDay || !hasFood)
            {
                if (session.MealsEaten > 0)
                {
                    resolving = true;
                    ShowResult();
                    return;
                }

                Debug.Log("盘子上没有食物，不能进食。", this);
                return;
            }

            if (session.HeldDish == null || session.HeldDish.Items == null)
            {
                Debug.LogError("需要 GameSession 上的 HeldDish。", this);
                return;
            }

            var plate = new List<IngredientInstance>(session.HeldDish.Items);
            RecipeDefinition today = TodayRecipe(session);
            DishResult result = DeliciousnessScore.Evaluate(plate, DefinitionsFor(plate), today, presentation);
            if (result.Picture == DishPictureKind.Recipe && today != null)
                session.RememberCorrectRecipe(today.id);
            if (session.AttemptScores == null || session.AttemptScores.Length < MealsPerDay)
                session.AttemptScores = new float[MealsPerDay];

            session.AttemptScores[session.MealsEaten] = result.Score;
            session.MealsEaten += 1;
            session.TodayDeliciousness = DeliciousnessScore.Average(session.AttemptScores, session.MealsEaten);
            session.HeldDish.Items.Clear();
            Plate foundPlate = FindAnyObjectByType<Plate>(FindObjectsInactive.Include);
            if (foundPlate != null)
                foundPlate.ClearAll();

            lastResult = result;
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
            ApplySummary();
            if (resultPanel != null)
                resultPanel.SetActive(true);
        }

        public void ClosePanel()
        {
            if (resultPanel != null)
                resultPanel.SetActive(false);

            resolving = false;
        }

        public void EnterNight()
        {
            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null || root.Flow == null || root.Flow.IsLoading)
                return;

            if (root.Session.MealsEaten < 1)
                return;

            root.Session.Phase = DayPhase.Night;
            root.Flow.Load(SceneNames.Outing);
        }

        void ApplySummary()
        {
            GameSession session = Session();
            int meals = session != null ? session.MealsEaten : 0;
            if (ratingFirstText != null)
                ratingFirstText.text = ScoreAt(session, 0);

            if (ratingSecondText != null)
                ratingSecondText.text = ScoreAt(session, 1);

            if (ratingThirdText != null)
                ratingThirdText.text = ScoreAt(session, 2);

            if (ratingFinalText != null)
                ratingFinalText.text = meals > 0 ? DeliciousnessScore.FormatScore(session.TodayDeliciousness) : "";

            if (commentsText != null)
                commentsText.text = lastResult != null ? lastResult.Comment : "";

            if (finalFoodImage != null)
            {
                finalFoodImage.sprite = lastResult != null ? lastResult.SummarySprite : null;
                finalFoodImage.color = Color.white;
                finalFoodImage.preserveAspect = true;
            }

            if (oneMoreButton != null)
                oneMoreButton.gameObject.SetActive(meals < MealsPerDay);
        }

        static string ScoreAt(GameSession session, int index)
        {
            if (session == null || session.AttemptScores == null || index >= session.MealsEaten || index >= session.AttemptScores.Length)
                return "";

            return DeliciousnessScore.FormatScore(session.AttemptScores[index]);
        }

        RecipeDefinition TodayRecipe(GameSession session)
        {
            if (session == null || recipes == null)
                return null;

            for (int i = 0; i < recipes.Length; i++)
            {
                RecipeDefinition recipe = recipes[i];
                if (recipe != null && recipe.unlockDay == session.DayIndex)
                    return recipe;
            }

            return null;
        }

        static List<IngredientDefinition> DefinitionsFor(List<IngredientInstance> plate)
        {
            var definitions = new List<IngredientDefinition>();
            for (int i = 0; i < plate.Count; i++)
            {
                IngredientInstance item = plate[i];
                if (item == null)
                    continue;

                IngredientDefinition definition = FoodTray.Lookup(item.DefinitionId);
                if (definition != null && !definitions.Contains(definition))
                    definitions.Add(definition);
            }

            return definitions;
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
