using Cooking.Runtime;

namespace Cooking.UI
{
    public interface IFoodStation
    {
        bool TryPlace(IngredientInstance item);
    }
}
