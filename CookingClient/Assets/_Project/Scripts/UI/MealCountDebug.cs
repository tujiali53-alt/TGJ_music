using Cooking.Core;
using Cooking.Runtime;
using UnityEngine;

namespace Cooking.UI
{
    public sealed class MealCountDebug : MonoBehaviour
    {
        [SerializeField, Range(0, MealEatController.MealsPerDay)] int mealsEaten;

        int applied = -1;
        bool loggedMissingSession;

        void Update()
        {
            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null)
            {
                if (!loggedMissingSession)
                {
                    loggedMissingSession = true;
                    Debug.LogError("MealCountDebug 需要已经放好的 GameRoot。", this);
                }

                return;
            }

            GameSession session = root.Session;
            mealsEaten = Mathf.Clamp(mealsEaten, 0, MealEatController.MealsPerDay);

            if (applied < 0 || mealsEaten != applied)
            {
                session.MealsEaten = mealsEaten;
                applied = mealsEaten;
                return;
            }

            if (session.MealsEaten != applied)
            {
                mealsEaten = Mathf.Clamp(session.MealsEaten, 0, MealEatController.MealsPerDay);
                applied = mealsEaten;
            }
        }
    }
}
