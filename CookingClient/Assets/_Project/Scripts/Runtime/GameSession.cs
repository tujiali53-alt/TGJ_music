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

        public static GameSession NewGame()
        {
            return new GameSession
            {
                SaveVersion = 1,
                DayIndex = 0,
                Phase = DayPhase.Night,
                Inventory = new List<IngredientInstance>()
            };
        }
    }
}

