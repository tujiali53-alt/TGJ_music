using UnityEngine;

namespace Cooking.Data
{
    public enum IngredientKind
    {
        Meat = 0,
        Vegetable = 1,
        Other = 2
    }

    public enum CutShape
    {
        Whole = 0,
        Cut = 1
    }

    public enum CookMethod
    {
        Raw = 0,
        Boiled = 1,
        Fried = 2,
        BoiledAndFried = 3
    }

    public enum QualityTier
    {
        Poor = 0,
        Normal = 1,
        Fine = 2
    }

    public enum CookAction
    {
        Cut = 0,
        Boil = 1,
        Fry = 2,
        Season = 3,
        Plate = 4
    }
}
