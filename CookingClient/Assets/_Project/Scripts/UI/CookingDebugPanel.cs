using System.Collections.Generic;
using Cooking.Core;
using Cooking.Data;
using Cooking.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cooking.UI
{
    public sealed class CookingDebugPanel : MonoBehaviour
    {
        [SerializeField] Button debugButton;
        [SerializeField] GameObject panel;
        [SerializeField] Button todayDiaryButton;
        [SerializeField] TMP_Text todayDiaryLabel;
        [SerializeField] Button unlockAllDiariesButton;
        [SerializeField] Button grantAllPropsButton;
        [SerializeField] Button perfectEatButton;
        [SerializeField] Button dayPrevButton;
        [SerializeField] Button dayNextButton;
        [SerializeField] Button jumpDayButton;
        [SerializeField] TMP_Text daySelectLabel;
        [SerializeField] Button closeButton;
        [SerializeField] DiaryCatalog diaryCatalog;
        [SerializeField] NpcCatalog npcCatalog;
        [SerializeField] MealEatController mealEat;
        [SerializeField] RecipeDefinition[] recipes;

        const int MinJumpDay = 1;
        const int MaxJumpDay = 7;

        int selectedDay = 1;

        void Awake()
        {
            ResolveRefs();
            SyncSelectedDayFromSession();

            if (debugButton != null)
                debugButton.onClick.AddListener(TogglePanel);
            if (todayDiaryButton != null)
                todayDiaryButton.onClick.AddListener(UnlockTodayDiary);
            if (unlockAllDiariesButton != null)
                unlockAllDiariesButton.onClick.AddListener(UnlockAllDiaries);
            if (grantAllPropsButton != null)
                grantAllPropsButton.onClick.AddListener(GrantAllNpcProps);
            if (perfectEatButton != null)
                perfectEatButton.onClick.AddListener(PerfectEat);
            if (dayPrevButton != null)
                dayPrevButton.onClick.AddListener(SelectPrevDay);
            if (dayNextButton != null)
                dayNextButton.onClick.AddListener(SelectNextDay);
            if (jumpDayButton != null)
                jumpDayButton.onClick.AddListener(JumpToSelectedDay);
            if (closeButton != null)
                closeButton.onClick.AddListener(HidePanel);

            if (panel != null)
                panel.SetActive(false);

            RefreshLabels();
        }

        void OnDestroy()
        {
            if (debugButton != null)
                debugButton.onClick.RemoveListener(TogglePanel);
            if (todayDiaryButton != null)
                todayDiaryButton.onClick.RemoveListener(UnlockTodayDiary);
            if (unlockAllDiariesButton != null)
                unlockAllDiariesButton.onClick.RemoveListener(UnlockAllDiaries);
            if (grantAllPropsButton != null)
                grantAllPropsButton.onClick.RemoveListener(GrantAllNpcProps);
            if (perfectEatButton != null)
                perfectEatButton.onClick.RemoveListener(PerfectEat);
            if (dayPrevButton != null)
                dayPrevButton.onClick.RemoveListener(SelectPrevDay);
            if (dayNextButton != null)
                dayNextButton.onClick.RemoveListener(SelectNextDay);
            if (jumpDayButton != null)
                jumpDayButton.onClick.RemoveListener(JumpToSelectedDay);
            if (closeButton != null)
                closeButton.onClick.RemoveListener(HidePanel);
        }

        void OnEnable()
        {
            RefreshLabels();
        }

        void Update()
        {
            if (panel != null && panel.activeSelf)
                RefreshTodayDiaryLabel();
        }

        void TogglePanel()
        {
            AudioManager.PlayClick();
            if (panel == null)
            {
                Debug.LogError("DebugButton 需要配对 DebugPanel。", this);
                return;
            }

            bool open = !panel.activeSelf;
            panel.SetActive(open);
            if (open)
            {
                SyncSelectedDayFromSession();
                RefreshLabels();
            }
        }

        void HidePanel()
        {
            AudioManager.PlayCancel();
            if (panel != null)
                panel.SetActive(false);
        }

        void UnlockTodayDiary()
        {
            GameSession session = Session();
            if (session == null)
                return;

            AudioManager.PlayClick();
            RecipeDefinition today = TodayRecipe(session);
            if (today == null || string.IsNullOrEmpty(today.id))
            {
                Debug.LogWarning("今天没有可解锁日记的食谱（DayIndex=" + session.DayIndex + "）。", this);
                RefreshTodayDiaryLabel();
                return;
            }

            session.RememberCorrectRecipe(today.id);
            RefreshLabels();
            Debug.Log("Debug：已解锁当天食谱/日记标记 " + today.id);
        }

        void UnlockAllDiaries()
        {
            GameSession session = Session();
            if (session == null || diaryCatalog == null || diaryCatalog.entries == null)
            {
                Debug.LogError("解锁全部日记需要 GameSession 与 DiaryCatalog。", this);
                return;
            }

            AudioManager.PlayClick();
            int maxDay = session.DayIndex;
            DayPhase maxPhase = session.Phase;
            for (int i = 0; i < diaryCatalog.entries.Count; i++)
            {
                DiaryEntry entry = diaryCatalog.entries[i];
                if (entry == null || !entry.inBook)
                    continue;

                if (!string.IsNullOrEmpty(entry.recipeId))
                    session.RememberCorrectRecipe(entry.recipeId);

                if (entry.unlockDay > maxDay
                    || (entry.unlockDay == maxDay
                        && DiaryBook.Moment(entry.unlockDay, entry.unlockPhase)
                            > DiaryBook.Moment(maxDay, maxPhase)))
                {
                    maxDay = entry.unlockDay;
                    maxPhase = entry.unlockPhase;
                }
            }

            if (DiaryBook.Moment(session.DayIndex, session.Phase) < DiaryBook.Moment(maxDay, maxPhase))
            {
                session.DayIndex = maxDay;
                session.Phase = maxPhase;
                Debug.Log("Debug：为满足日记时刻，已将进度调到 DAY" + maxDay + " " + maxPhase);
            }

            SyncSelectedDayFromSession();
            RefreshLabels();
            Debug.Log(
                "Debug：全部 inBook 日记应已解锁。当前解锁数="
                + DiaryBook.Visible(diaryCatalog, session).Count);
        }

        void SelectPrevDay()
        {
            AudioManager.PlayClick();
            selectedDay = Mathf.Max(MinJumpDay, selectedDay - 1);
            RefreshDaySelectLabel();
        }

        void SelectNextDay()
        {
            AudioManager.PlayClick();
            selectedDay = Mathf.Min(MaxJumpDay, selectedDay + 1);
            RefreshDaySelectLabel();
        }

        void JumpToSelectedDay()
        {
            GameSession session = Session();
            if (session == null)
            {
                Debug.LogError("跳转天数需要 GameSession。", this);
                return;
            }

            AudioManager.PlayClick();
            selectedDay = Mathf.Clamp(selectedDay, MinJumpDay, MaxJumpDay);
            session.DayIndex = selectedDay;
            session.Phase = DayPhase.Day;
            session.ClearCookingScores();
            if (session.HeldDish != null && session.HeldDish.Items != null)
                session.HeldDish.Items.Clear();
            Plate.ClearActive();

            AudioManager.SetDayStage(session.DayIndex);
            DayServingSwitcher switcher = FindAnyObjectByType<DayServingSwitcher>(FindObjectsInactive.Include);
            if (switcher != null)
                switcher.Apply();

            RefreshLabels();
            Debug.Log("Debug：已跳转到第 " + selectedDay + " 天（DAY" + selectedDay + " Daylight）。");
        }

        void GrantAllNpcProps()
        {
            GameSession session = Session();
            if (session == null || npcCatalog == null || npcCatalog.props == null)
            {
                Debug.LogError("发放 NPC 道具需要 GameSession 与 NpcCatalog。", this);
                return;
            }

            AudioManager.PlayClick();
            if (session.Props == null)
                session.Props = new List<PropStack>();

            int granted = 0;
            for (int i = 0; i < npcCatalog.props.Count; i++)
            {
                NpcProp prop = npcCatalog.props[i];
                if (prop == null || string.IsNullOrEmpty(prop.id))
                    continue;

                AddOrRaiseProp(session, prop.id, 1);
                granted++;
            }

            Debug.Log("Debug：已发放 NPC 道具 " + granted + " 种。");
        }

        void PerfectEat()
        {
            GameSession session = Session();
            if (session == null || mealEat == null)
            {
                Debug.LogError("完美进食需要 GameSession 与 MealEatController。", this);
                return;
            }

            AudioManager.PlayClick();
            if (session.MealsEaten >= MealsAllowed(session))
            {
                Debug.LogWarning("今天进食次数已满，直接打开结算。", this);
                mealEat.Eat();
                return;
            }

            RecipeDefinition today = TodayRecipe(session);
            if (today == null)
            {
                Debug.LogError("找不到当天食谱，无法完美进食。DayIndex=" + session.DayIndex, this);
                return;
            }

            if (session.HeldDish == null)
                session.HeldDish = new Dish { DishId = 1, ExecutionScore = 1f };
            if (session.HeldDish.Items == null)
                session.HeldDish.Items = new List<IngredientInstance>();

            session.HeldDish.Items.Clear();
            Plate.ClearActive();

            if (!BuildPerfectPlate(session, today))
                return;

            mealEat.Eat();
        }

        bool BuildPerfectPlate(GameSession session, RecipeDefinition today)
        {
            if (RecipeMatcher.IsFreeform(today))
                return BuildFreeformPlate(session);

            if (today.ingredients == null || today.ingredients.Count == 0)
            {
                Debug.LogError("食谱 " + today.id + " 没有食材条目。", today);
                return false;
            }

            int nextId = NextInstanceId(session);
            for (int i = 0; i < today.ingredients.Count; i++)
            {
                RecipeIngredient requirement = today.ingredients[i];
                IngredientDefinition definition = FoodTray.Lookup(requirement.ingredientId);
                if (definition == null)
                {
                    Debug.LogError("找不到食材定义 " + requirement.ingredientId, this);
                    return false;
                }

                int count = requirement.count <= 0 ? 1 : requirement.count;
                var seasonings = new List<string>();
                List<string> expected = RecipeMatcher.ExpectedSeasonings(today, requirement.ingredientId);
                for (int s = 0; s < expected.Count; s++)
                    seasonings.Add(expected[s]);

                session.HeldDish.Items.Add(new IngredientInstance
                {
                    InstanceId = nextId++,
                    DefinitionId = requirement.ingredientId,
                    Quality = QualityTier.Normal,
                    Quantity = count,
                    Doneness = definition.optimalDoneness,
                    Shape = requirement.shape,
                    CookMethod = requirement.cookMethod,
                    SeasoningIds = seasonings,
                    IsCharcoal = false,
                    OptimalHitTime = Time.time
                });
            }

            return session.HeldDish.Items.Count > 0;
        }

        bool BuildFreeformPlate(GameSession session)
        {
            IngredientDefinition definition = FoodTray.Lookup("ing_mushroom");
            if (definition == null)
                definition = FoodTray.Lookup("ing_rice");
            if (definition == null)
            {
                Debug.LogError("自由食谱完美进食需要至少一种可用食材定义。", this);
                return false;
            }

            session.HeldDish.Items.Add(new IngredientInstance
            {
                InstanceId = NextInstanceId(session),
                DefinitionId = definition.id,
                Quality = QualityTier.Normal,
                Quantity = 1,
                Doneness = definition.optimalDoneness,
                Shape = CutShape.Whole,
                CookMethod = CookMethod.Boiled,
                SeasoningIds = new List<string>(),
                IsCharcoal = false,
                OptimalHitTime = Time.time
            });
            return true;
        }

        void RefreshLabels()
        {
            RefreshTodayDiaryLabel();
            RefreshDaySelectLabel();
        }

        void RefreshTodayDiaryLabel()
        {
            if (todayDiaryLabel == null)
                return;

            GameSession session = Session();
            RecipeDefinition today = TodayRecipe(session);
            bool unlocked = IsTodayDiaryUnlocked(session, today);
            string recipe = today != null ? today.id : "无";
            todayDiaryLabel.text = unlocked
                ? "当天日记：已解锁 (" + recipe + ")"
                : "当天日记：未解锁 (" + recipe + ")";
        }

        void RefreshDaySelectLabel()
        {
            if (daySelectLabel == null)
                return;

            daySelectLabel.text = "跳转：第" + selectedDay + "天";
        }

        void SyncSelectedDayFromSession()
        {
            GameSession session = Session();
            if (session == null)
            {
                selectedDay = MinJumpDay;
                return;
            }

            selectedDay = Mathf.Clamp(session.DayIndex, MinJumpDay, MaxJumpDay);
            if (session.DayIndex < MinJumpDay)
                selectedDay = MinJumpDay;
        }

        bool IsTodayDiaryUnlocked(GameSession session, RecipeDefinition today)
        {
            if (session == null || today == null || string.IsNullOrEmpty(today.id))
                return false;

            if (diaryCatalog == null || diaryCatalog.entries == null)
                return Contains(session.CorrectRecipeIds, today.id);

            for (int i = 0; i < diaryCatalog.entries.Count; i++)
            {
                DiaryEntry entry = diaryCatalog.entries[i];
                if (entry == null || !entry.inBook || entry.recipeId != today.id)
                    continue;

                return DiaryBook.IsUnlocked(entry, session);
            }

            return Contains(session.CorrectRecipeIds, today.id);
        }

        RecipeDefinition TodayRecipe(GameSession session)
        {
            if (session == null || recipes == null)
                return null;

            RecipeDefinition best = null;
            for (int i = 0; i < recipes.Length; i++)
            {
                RecipeDefinition recipe = recipes[i];
                if (recipe == null || recipe.unlockDay < 1 || recipe.unlockDay > session.DayIndex)
                    continue;

                if (recipe.unlockDay == session.DayIndex)
                    return recipe;

                if (best == null || recipe.unlockDay > best.unlockDay)
                    best = recipe;
            }

            return best;
        }

        static void AddOrRaiseProp(GameSession session, string propId, int count)
        {
            for (int i = 0; i < session.Props.Count; i++)
            {
                PropStack stack = session.Props[i];
                if (stack != null && stack.PropId == propId)
                {
                    if (stack.Quantity < count)
                        stack.Quantity = count;
                    return;
                }
            }

            session.Props.Add(new PropStack
            {
                PropId = propId,
                Quantity = count
            });
        }

        static int NextInstanceId(GameSession session)
        {
            int next = 1;
            if (session == null)
                return next;

            RaiseId(ref next, session.Inventory);
            if (session.HeldDish != null)
                RaiseId(ref next, session.HeldDish.Items);
            return next;
        }

        static void RaiseId(ref int next, IList<IngredientInstance> items)
        {
            if (items == null)
                return;

            for (int i = 0; i < items.Count; i++)
            {
                IngredientInstance item = items[i];
                if (item != null && item.InstanceId >= next)
                    next = item.InstanceId + 1;
            }
        }

        static bool Contains(IList<string> ids, string id)
        {
            if (ids == null || string.IsNullOrEmpty(id))
                return false;

            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] == id)
                    return true;
            }

            return false;
        }

        static int MealsAllowed(GameSession session)
        {
            if (session != null && session.DayIndex == MealEatController.FinalDayIndex)
                return MealEatController.FinalDayMeals;

            return MealEatController.MealsPerDay;
        }

        static GameSession Session()
        {
            GameRoot root = GameRoot.Instance;
            return root != null ? root.Session : null;
        }

        void ResolveRefs()
        {
            if (debugButton == null)
            {
                Transform found = transform.Find("DebugButton");
                if (found == null && transform.parent != null)
                    found = transform.parent.Find("DebugButton");
                if (found != null)
                    debugButton = found.GetComponent<Button>();
            }

            if (panel == null)
            {
                Transform found = transform.Find("DebugPanel");
                if (found == null && transform.parent != null)
                    found = transform.parent.Find("DebugPanel");
                if (found != null)
                    panel = found.gameObject;
            }

            if (panel != null)
            {
                if (todayDiaryButton == null)
                    todayDiaryButton = FindButton(panel.transform, "TodayDiaryButton");
                if (unlockAllDiariesButton == null)
                    unlockAllDiariesButton = FindButton(panel.transform, "UnlockAllDiariesButton");
                if (grantAllPropsButton == null)
                    grantAllPropsButton = FindButton(panel.transform, "GrantAllPropsButton");
                if (perfectEatButton == null)
                    perfectEatButton = FindButton(panel.transform, "PerfectEatButton");
                if (jumpDayButton == null)
                    jumpDayButton = FindButton(panel.transform, "JumpDayButton");
                if (closeButton == null)
                    closeButton = FindButton(panel.transform, "CloseButton");

                Transform dayJump = panel.transform.Find("DayJumpRow");
                if (dayJump != null)
                {
                    if (dayPrevButton == null)
                        dayPrevButton = FindButton(dayJump, "DayPrevButton");
                    if (dayNextButton == null)
                        dayNextButton = FindButton(dayJump, "DayNextButton");
                    if (daySelectLabel == null)
                    {
                        Transform label = dayJump.Find("DaySelectLabel");
                        if (label != null)
                            daySelectLabel = label.GetComponent<TMP_Text>();
                    }
                }

                if (todayDiaryLabel == null && todayDiaryButton != null)
                    todayDiaryLabel = todayDiaryButton.GetComponentInChildren<TMP_Text>(true);
            }

            if (mealEat == null)
                mealEat = FindAnyObjectByType<MealEatController>(FindObjectsInactive.Include);

            if ((recipes == null || recipes.Length == 0) && mealEat != null)
                recipes = mealEat.Recipes;
        }

        static Button FindButton(Transform root, string childName)
        {
            if (root == null)
                return null;

            Transform found = root.Find(childName);
            return found != null ? found.GetComponent<Button>() : null;
        }
    }
}
