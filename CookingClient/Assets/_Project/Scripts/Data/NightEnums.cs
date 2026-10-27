namespace Cooking.Data
{
    public enum NightConditionType
    {
        None = 0,
        HasAnyFood = 1,
        HasVegetable = 2,
        DeliciousnessBelow = 3,
        DeliciousnessAbove = 4
    }

    public enum NightRewardPick
    {
        All = 0,
        One = 1
    }

    public enum NightLinePhase
    {
        Intro = 0,
        Success = 1,
        Fail = 2
    }

    public enum NightLogKind
    {
        Buff = 0,
        DiceRoll = 1,
        CheckResult = 2,
        IngredientGain = 3,
        LightAbsorb = 4,
        RadiusChanged = 5,
        OptionBlocked = 6
    }
}
