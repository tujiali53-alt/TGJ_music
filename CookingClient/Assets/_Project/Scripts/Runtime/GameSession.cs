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
        public float TodayDeliciousness;
        public int LightsAbsorbedTonight;
        public List<string> ResolvedOptionIdsTonight = new List<string>();
        public List<string> RevealedRewardOptionIds = new List<string>();
        public List<NightLogEntry> NightLog = new List<NightLogEntry>();

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
                TodayDeliciousness = 0f,
                LightsAbsorbedTonight = 0,
                ResolvedOptionIdsTonight = new List<string>(),
                RevealedRewardOptionIds = new List<string>(),
                NightLog = new List<NightLogEntry>()
            };
        }

        public void ClearCookingScores()
        {
            MealsEaten = 0;
            TodayDeliciousness = 0f;
            AttemptScores = new float[3];
        }
    }
}

