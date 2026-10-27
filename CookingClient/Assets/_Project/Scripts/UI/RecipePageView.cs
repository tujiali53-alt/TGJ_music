using Cooking.Core;
using Cooking.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cooking.UI
{
    public sealed class RecipePageView : MonoBehaviour
    {
        [SerializeField] Image recipeImage;
        [SerializeField] RecipeDefinition[] recipes;
        [SerializeField] DiaryLanguage language = DiaryLanguage.Zh;

        void Awake()
        {
            if (recipeImage == null)
                recipeImage = GetComponent<Image>();

            if (recipeImage == null)
                Debug.LogError("RecipeImage 上需要 Image。", this);

            if (recipes == null || recipes.Length == 0)
                Debug.LogError("RecipeImage 需要绑上每日食谱。", this);
        }

        void OnEnable()
        {
            Apply();
        }

        public void Apply()
        {
            HidePlaceholderText();
            if (recipeImage == null)
                return;

            int dayIndex = 0;
            GameRoot root = GameRoot.Instance;
            if (root != null && root.Session != null)
                dayIndex = root.Session.DayIndex;

            Sprite sprite = SpriteFor(recipes, dayIndex, language);
            recipeImage.sprite = sprite;
            recipeImage.enabled = sprite != null;
            if (sprite == null)
                return;

            recipeImage.color = Color.white;
            recipeImage.preserveAspect = true;
        }

        public static Sprite SpriteFor(RecipeDefinition[] recipes, int dayIndex, DiaryLanguage language)
        {
            RecipeDefinition recipe = Find(recipes, dayIndex);
            if (recipe == null)
                return null;

            return language == DiaryLanguage.En ? recipe.pageSpriteEn : recipe.pageSpriteZh;
        }

        public static RecipeDefinition Find(RecipeDefinition[] recipes, int dayIndex)
        {
            if (recipes == null)
                return null;

            for (int i = 0; i < recipes.Length; i++)
            {
                RecipeDefinition recipe = recipes[i];
                if (recipe != null && recipe.unlockDay == dayIndex)
                    return recipe;
            }

            return null;
        }

        void HidePlaceholderText()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.GetComponent<TMP_Text>() != null)
                    child.gameObject.SetActive(false);
            }
        }
    }
}
