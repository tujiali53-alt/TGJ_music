using Cooking.Data;
using Cooking.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cooking.EditorTools
{
    public static class TutorialSceneSetup
    {
        const string OutingPath = "Assets/_Project/Scenes/outingScene.unity";
        const string CookingPath = "Assets/_Project/Scenes/MainCooking.unity";
        const string CatalogPath = "Assets/_Project/Data/TutorialCatalog.asset";

        [MenuItem("Cooking/Setup Tutorial Scenes")]
        public static void SetupMenu()
        {
            Debug.Log(SetupAll());
        }

        public static string SetupAll()
        {
            string outing = SetupOuting();
            string cooking = SetupCooking();
            return outing + " | " + cooking;
        }

        public static string SetupOuting()
        {
            Scene scene = EditorSceneManager.OpenScene(OutingPath, OpenSceneMode.Single);
            Transform canvas = FindDeep(scene, "Canvas");
            if (canvas == null)
                return "outing: Canvas missing";

            Transform interaction = FindDeep(scene, "InteractionPanel");
            if (interaction != null)
                interaction.gameObject.SetActive(true);

            Transform tutorialPanel = FindDeep(scene, "tutorialPanel");
            if (tutorialPanel != null)
            {
                tutorialPanel.gameObject.SetActive(false);
                Transform close = tutorialPanel.Find("closedButton");
                if (close != null)
                    close.gameObject.SetActive(false);
            }

            WireDirector(canvas.gameObject, tutorialPanel != null ? tutorialPanel.gameObject : null, null);
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            return "outing saved=" + saved;
        }

        public static string SetupCooking()
        {
            Scene scene = EditorSceneManager.OpenScene(CookingPath, OpenSceneMode.Single);
            Transform canvas = FindDeep(scene, "Canvas");
            if (canvas == null)
                return "cooking: Canvas missing";

            GameObject mask = EnsureGuideMask(canvas);
            WireDirector(canvas.gameObject, null, mask);
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            return "cooking saved=" + saved + " mask=" + (mask != null);
        }

        static void WireDirector(GameObject canvas, GameObject tutorialPanel, GameObject guideMask)
        {
            TutorialDialogPlayer player = canvas.GetComponent<TutorialDialogPlayer>();
            if (player == null)
                player = Undo.AddComponent<TutorialDialogPlayer>(canvas);

            TutorialDirector director = canvas.GetComponent<TutorialDirector>();
            if (director == null)
                director = Undo.AddComponent<TutorialDirector>(canvas);

            TutorialCatalog catalog = AssetDatabase.LoadAssetAtPath<TutorialCatalog>(CatalogPath);
            MealEatController mealEat = Object.FindFirstObjectByType<MealEatController>(FindObjectsInactive.Include);
            RecipeDiaryPanels recipePanels = Object.FindFirstObjectByType<RecipeDiaryPanels>(FindObjectsInactive.Include);
            RecipeNotice recipeNotice = Object.FindFirstObjectByType<RecipeNotice>(FindObjectsInactive.Include);

            SerializedObject so = new SerializedObject(director);
            so.FindProperty("catalog").objectReferenceValue = catalog;
            so.FindProperty("dialogPlayer").objectReferenceValue = player;
            so.FindProperty("tutorialPanel").objectReferenceValue = tutorialPanel;
            so.FindProperty("tutorialCloseDelay").floatValue = 2f;
            so.FindProperty("guideMask").objectReferenceValue = guideMask;
            so.FindProperty("mealEat").objectReferenceValue = mealEat;
            so.FindProperty("recipePanels").objectReferenceValue = recipePanels;
            so.FindProperty("recipeNotice").objectReferenceValue = recipeNotice;

            if (tutorialPanel != null)
            {
                Transform close = tutorialPanel.transform.Find("closedButton");
                if (close != null)
                    so.FindProperty("tutorialCloseButton").objectReferenceValue = close.GetComponent<Button>();
            }

            Transform eat = FindDeep(canvas.gameObject.scene, "EattingButton");
            if (eat != null)
                so.FindProperty("eatButton").objectReferenceValue = eat.GetComponent<Button>();

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(canvas);
        }

        static GameObject EnsureGuideMask(Transform canvas)
        {
            Transform existing = canvas.Find("TutorialGuideMask");
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
                return existing.gameObject;
            }

            GameObject mask = new GameObject("TutorialGuideMask", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            mask.layer = canvas.gameObject.layer;
            mask.transform.SetParent(canvas, false);
            RectTransform rt = mask.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Image image = mask.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.65f);
            image.raycastTarget = true;
            mask.SetActive(false);

            // Keep InteractionPanel above the dim mask so EatingButton/Dialog stay usable.
            Transform interaction = canvas.Find("InteractionPanel");
            if (interaction != null)
                interaction.SetAsLastSibling();

            Undo.RegisterCreatedObjectUndo(mask, "Create TutorialGuideMask");
            return mask;
        }

        static Transform FindDeep(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                Transform found = FindDeep(roots[r].transform, name);
                if (found != null)
                    return found;
            }

            return null;
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
