using System.Collections.Generic;
using Cooking.Data;

namespace Cooking.Runtime
{
    public sealed class GameSession
    {
        public int SaveVersion = 1;
        public int DayIndex;
        public DayPhase Phase;
        public List<IngredientInstance> Inventory = new List<IngredientInstance>();
        public Dish HeldDish = new Dish();
        public int MealsEaten;
        public float[] AttemptScores = new float[3];
        public string[][] AttemptComments = new string[3][];
        public float TodayDeliciousness;
        public int LightsAbsorbedTonight;
        public List<string> ResolvedOptionIdsTonight = new List<string>();
        public List<string> RevealedRewardOptionIds = new List<string>();
        public List<string> SeenNightIntroIds = new List<string>();
        public List<string> SeenNightSuccessOptionIds = new List<string>();
        public List<string> SeenNightFailOptionIds = new List<string>();
        public List<NightLogEntry> NightLog = new List<NightLogEntry>();
        public List<string> CorrectRecipeIds = new List<string>();
        public List<string> DiarySeenIds = new List<string>();
        public List<string> ResolvedNpcIds = new List<string>();
        public List<PropStack> Props = new List<PropStack>();
        public List<string> SeenTutorialIds = new List<string>();

        public static GameSession NewGame()
        {
            return new GameSession
            {
                SaveVersion = 1,
                DayIndex = 0,
                Phase = DayPhase.Night,
                Inventory = new List<IngredientInstance>(),
                HeldDish = new Dish { DishId = 1, ExecutionScore = 1f },
                MealsEaten = 0,
                AttemptScores = new float[3],
                AttemptComments = new string[3][],
                TodayDeliciousness = 0f,
                LightsAbsorbedTonight = 0,
                ResolvedOptionIdsTonight = new List<string>(),
                RevealedRewardOptionIds = new List<string>(),
                SeenNightIntroIds = new List<string>(),
                SeenNightSuccessOptionIds = new List<string>(),
                SeenNightFailOptionIds = new List<string>(),
                NightLog = new List<NightLogEntry>(),
                CorrectRecipeIds = new List<string>(),
                DiarySeenIds = new List<string>(),
                ResolvedNpcIds = new List<string>(),
                Props = new List<PropStack>(),
                SeenTutorialIds = new List<string>()
            };
        }

        public bool HasSeenTutorial(string tutorialId)
        {
            return Contains(SeenTutorialIds, tutorialId);
        }

        public void RememberTutorial(string tutorialId)
        {
            RememberId(ref SeenTutorialIds, tutorialId);
        }

        public bool HasSeenNightIntro(string eventId)
        {
            return !string.IsNullOrEmpty(eventId) && Contains(SeenNightIntroIds, eventId);
        }

        public bool HasSeenNightOutcome(string optionId, bool success)
        {
            if (string.IsNullOrEmpty(optionId))
                return false;

            return Contains(success ? SeenNightSuccessOptionIds : SeenNightFailOptionIds, optionId);
        }

        public bool HasRolledNightOption(string optionId)
        {
            return HasSeenNightOutcome(optionId, true) || HasSeenNightOutcome(optionId, false);
        }

        public void RememberNightIntro(string eventId)
        {
            RememberId(ref SeenNightIntroIds, eventId);
        }

        public void RememberNightOutcome(string optionId, bool success)
        {
            if (success)
                RememberId(ref SeenNightSuccessOptionIds, optionId);
            else
                RememberId(ref SeenNightFailOptionIds, optionId);
        }

        static void RememberId(ref List<string> ids, string id)
        {
            if (string.IsNullOrEmpty(id))
                return;

            if (ids == null)
                ids = new List<string>();

            if (!Contains(ids, id))
                ids.Add(id);
        }

        public void RememberCorrectRecipe(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId))
                return;

            if (CorrectRecipeIds == null)
                CorrectRecipeIds = new List<string>();

            if (!Contains(CorrectRecipeIds, recipeId))
                CorrectRecipeIds.Add(recipeId);
        }

        public void RememberDiarySeen(List<DiaryEntry> pages)
        {
            if (pages == null || pages.Count == 0)
                return;

            if (DiarySeenIds == null)
                DiarySeenIds = new List<string>();

            for (int i = 0; i < pages.Count; i++)
            {
                DiaryEntry entry = pages[i];
                if (entry == null || string.IsNullOrEmpty(entry.id))
                    continue;

                if (!Contains(DiarySeenIds, entry.id))
                    DiarySeenIds.Add(entry.id);
            }
        }

        static bool Contains(List<string> ids, string id)
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

        public void ClearCookingScores()
        {
            MealsEaten = 0;
            TodayDeliciousness = 0f;
            AttemptScores = new float[3];
            AttemptComments = new string[3][];
        }
    }

    public sealed class PropStack
    {
        public string PropId;
        public int Quantity = 1;
    }
}

