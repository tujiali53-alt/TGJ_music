using System.Collections.Generic;
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
        [SerializeField] Button previousButton;
        [SerializeField] Button nextButton;

        readonly List<RecipeDefinition> pages = new List<RecipeDefinition>();
        int pageIndex;

        void Awake()
        {
            if (recipeImage == null)
                recipeImage = GetComponent<Image>();

            if (recipeImage == null)
                Debug.LogError("RecipeImage 上需要 Image。", this);

            if (recipes == null || recipes.Length == 0)
                Debug.LogError("RecipeImage 需要绑上每日食谱。", this);

            ResolveButtons();

            if (previousButton == null)
                Debug.LogError("需要 RecipePanel/PrePageRecipeButton，并且带 Button。", this);
            else
                previousButton.onClick.AddListener(ShowPrevious);

            if (nextButton == null)
                Debug.LogError("需要 RecipePanel/nextPageRecipeButton，并且带 Button。", this);
            else
                nextButton.onClick.AddListener(ShowNext);
        }

        void OnEnable()
        {
            Apply();
        }

        void OnDestroy()
        {
            if (previousButton != null)
                previousButton.onClick.RemoveListener(ShowPrevious);

            if (nextButton != null)
                nextButton.onClick.RemoveListener(ShowNext);
        }

        public void ShowPrevious()
        {
            if (pageIndex <= 0)
                return;

            AudioManager.PlayPage();
            pageIndex--;
            ApplyPage();
        }

        public void ShowNext()
        {
            if (pageIndex >= pages.Count - 1)
                return;

            AudioManager.PlayPage();
            pageIndex++;
            ApplyPage();
        }

        public void Apply()
        {
            HidePlaceholderText();
            FillUnlocked(pages, recipes, CurrentDayIndex());
            pageIndex = pages.Count > 0 ? pages.Count - 1 : 0;
            ApplyPage();
        }

        public static void FillUnlocked(List<RecipeDefinition> destination, RecipeDefinition[] recipes, int dayIndex)
        {
            destination.Clear();
            if (recipes == null)
                return;

            for (int i = 0; i < recipes.Length; i++)
            {
                RecipeDefinition recipe = recipes[i];
                if (recipe != null && recipe.unlockDay >= 1 && recipe.unlockDay <= dayIndex)
                    destination.Add(recipe);
            }

            destination.Sort(CompareUnlock);
        }

        public static Sprite PageSprite(RecipeDefinition recipe, DiaryLanguage language)
        {
            if (recipe == null)
                return null;

            return language == DiaryLanguage.En ? recipe.pageSpriteEn : recipe.pageSpriteZh;
        }

        static int CompareUnlock(RecipeDefinition a, RecipeDefinition b)
        {
            int day = a.unlockDay.CompareTo(b.unlockDay);
            if (day != 0)
                return day;

            return string.CompareOrdinal(a.id, b.id);
        }

        void ApplyPage()
        {
            if (recipeImage != null)
            {
                RecipeDefinition recipe = pageIndex >= 0 && pageIndex < pages.Count ? pages[pageIndex] : null;
                Sprite sprite = PageSprite(recipe, language);
                recipeImage.sprite = sprite;
                recipeImage.enabled = sprite != null;
                if (sprite != null)
                {
                    recipeImage.color = Color.white;
                    recipeImage.preserveAspect = true;
                }
            }

            if (previousButton != null)
                previousButton.interactable = pageIndex > 0;
            if (nextButton != null)
                nextButton.interactable = pages.Count > 0 && pageIndex < pages.Count - 1;
        }

        void ResolveButtons()
        {
            Transform panel = transform.parent;
            if (panel == null)
                return;

            if (previousButton == null)
                previousButton = FindButton(panel, "PrePageRecipeButton");
            if (nextButton == null)
                nextButton = FindButton(panel, "nextPageRecipeButton");
        }

        static Button FindButton(Transform panel, string childName)
        {
            Transform found = panel.Find(childName);
            if (found == null)
                return null;

            return found.GetComponent<Button>();
        }

        static int CurrentDayIndex()
        {
            GameRoot root = GameRoot.Instance;
            if (root != null && root.Session != null)
                return root.Session.DayIndex;

            return 0;
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
