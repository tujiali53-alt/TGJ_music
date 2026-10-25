using UnityEngine;
using UnityEngine.UI;

namespace Cooking.UI
{
    public sealed class RecipeDiaryPanels : MonoBehaviour
    {
        [SerializeField] GameObject recipePanel;
        [SerializeField] GameObject diaryPanel;
        [SerializeField] Button openRecipeButton;
        [SerializeField] Button closeRecipeButton;
        [SerializeField] Button openDiaryButton;
        [SerializeField] Button closeDiaryButton;

        void Awake()
        {
            if (recipePanel == null)
            {
                Transform found = transform.Find("RecipePanel");
                if (found != null)
                    recipePanel = found.gameObject;
            }

            if (diaryPanel == null)
            {
                Transform found = transform.Find("DiaryPanel");
                if (found != null)
                    diaryPanel = found.gameObject;
            }

            if (openRecipeButton == null)
                openRecipeButton = FindButton("InteractionPanel/openRecipeButton");

            if (closeRecipeButton == null)
                closeRecipeButton = FindButton("RecipePanel/closeButton");

            if (openDiaryButton == null)
                openDiaryButton = FindButton("InteractionPanel/openDiaryButton");

            if (closeDiaryButton == null)
                closeDiaryButton = FindButton("DiaryPanel/closeButton");

            if (recipePanel == null)
                Debug.LogError("Canvas 下需要有 RecipePanel。", this);

            if (diaryPanel == null)
                Debug.LogError("Canvas 下需要有 DiaryPanel。", this);

            if (openRecipeButton == null)
                Debug.LogError("需要 InteractionPanel/openRecipeButton，并且带 Button。", this);
            else
                openRecipeButton.onClick.AddListener(OpenRecipe);

            if (closeRecipeButton == null)
                Debug.LogError("需要 RecipePanel/closeButton，并且带 Button。", this);
            else
                closeRecipeButton.onClick.AddListener(CloseRecipe);

            if (openDiaryButton == null)
                Debug.LogError("需要 InteractionPanel/openDiaryButton，并且带 Button。", this);
            else
                openDiaryButton.onClick.AddListener(OpenDiary);

            if (closeDiaryButton == null)
                Debug.LogError("需要 DiaryPanel/closeButton，并且带 Button。", this);
            else
                closeDiaryButton.onClick.AddListener(CloseDiary);

            if (recipePanel != null)
                recipePanel.SetActive(false);

            if (diaryPanel != null)
                diaryPanel.SetActive(false);
        }

        Button FindButton(string path)
        {
            Transform found = transform.Find(path);
            if (found == null)
                return null;

            return found.GetComponent<Button>();
        }

        public void OpenRecipe()
        {
            if (recipePanel != null)
                recipePanel.SetActive(true);
        }

        public void CloseRecipe()
        {
            if (recipePanel != null)
                recipePanel.SetActive(false);
        }

        public void OpenDiary()
        {
            if (diaryPanel != null)
                diaryPanel.SetActive(true);
        }

        public void CloseDiary()
        {
            if (diaryPanel != null)
                diaryPanel.SetActive(false);
        }
    }
}
