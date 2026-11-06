using Cooking.Data;
using Cooking.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cooking.EditorTools
{
    public static class EndingSceneSetup
    {
        const string MainCookingPath = "Assets/_Project/Scenes/MainCooking.unity";

        [MenuItem("Cooking/Setup Ending Scene")]
        public static void SetupMenu()
        {
            string result = SetupMainCooking();
            Debug.Log(result);
        }

        public static string SetupMainCooking()
        {
            Scene scene = EditorSceneManager.OpenScene(MainCookingPath, OpenSceneMode.Single);
            Transform canvas = FindDeep(scene, "Canvas");
            if (canvas == null)
                return "Canvas missing";

            Image fadeImg = EnsureFadeOverlay(canvas);
            Transform gameOver = FindDeep(scene, "GameOverPanel");
            if (gameOver == null)
                return "GameOverPanel missing";

            EndingSequenceController seq = gameOver.GetComponent<EndingSequenceController>();
            if (seq == null)
                seq = Undo.AddComponent<EndingSequenceController>(gameOver.gameObject);

            EndingCatalog endingCatalog = AssetDatabase.LoadAssetAtPath<EndingCatalog>(
                "Assets/_Project/Data/EndingCatalog.asset");
            DiaryCatalog diaryCatalog = AssetDatabase.LoadAssetAtPath<DiaryCatalog>(
                "Assets/_Project/Data/Diary/DiaryCatalog.asset");
            NpcCatalog npcCatalog = AssetDatabase.LoadAssetAtPath<NpcCatalog>(
                "Assets/_Project/Data/NpcCatalog.asset");

            Transform dialog = gameOver.Find("DialogPanel");
            Transform endingCg = gameOver.Find("endingCG");
            Transform metaPanel = gameOver.Find("metaPanel");
            Transform endingPanel = gameOver.Find("EndingPanel");

            if (endingCg != null)
                endingCg.SetAsFirstSibling();
            if (dialog != null)
                dialog.SetSiblingIndex(1);
            if (metaPanel != null)
                metaPanel.SetSiblingIndex(2);
            if (endingPanel != null)
                endingPanel.SetAsLastSibling();

            SerializedObject so = new SerializedObject(seq);
            so.FindProperty("endingCatalog").objectReferenceValue = endingCatalog;
            so.FindProperty("diaryCatalog").objectReferenceValue = diaryCatalog;
            so.FindProperty("npcCatalog").objectReferenceValue = npcCatalog;
            so.FindProperty("fadeOverlay").objectReferenceValue = fadeImg;
            so.FindProperty("fadeDuration").floatValue = 2f;
            so.FindProperty("cgClickDelay").floatValue = 3f;
            so.FindProperty("clickFadeDuration").floatValue = 0.5f;
            so.FindProperty("endingImageHoldDuration").floatValue = 3f;
            so.FindProperty("creditScrollSpeed").floatValue = 80f;
            so.FindProperty("gameOverPanel").objectReferenceValue = gameOver.gameObject;

            so.FindProperty("dialogPanel").objectReferenceValue =
                dialog != null ? dialog.gameObject : null;
            so.FindProperty("metaPanel").objectReferenceValue =
                metaPanel != null ? metaPanel.gameObject : null;

            if (dialog != null)
            {
                NightDialogClick click = dialog.GetComponent<NightDialogClick>();
                if (click == null)
                    click = Undo.AddComponent<NightDialogClick>(dialog.gameObject);
                click.BindEnding(seq);
            }

            if (metaPanel != null)
            {
                Transform metaDialog = metaPanel.Find("DialogPanel");
                if (metaDialog != null)
                {
                    NightDialogClick click = metaDialog.GetComponent<NightDialogClick>();
                    if (click == null)
                        click = Undo.AddComponent<NightDialogClick>(metaDialog.gameObject);
                    click.BindEnding(seq);
                }
            }

            so.FindProperty("endingCg").objectReferenceValue =
                endingCg != null ? endingCg.gameObject : null;
            if (endingCg != null)
            {
                AssignImage(so, "endingCgImage", endingCg.Find("EndingImage"));
                Transform click = endingCg.Find("ClickButton");
                if (click != null)
                {
                    so.FindProperty("cgClickButton").objectReferenceValue =
                        click.GetComponent<Button>();
                    TMP_Text tmp = click.GetComponentInChildren<TMP_Text>(true);
                    if (tmp != null)
                        so.FindProperty("cgClickText").objectReferenceValue = tmp;

                    CanvasGroup group = click.GetComponent<CanvasGroup>();
                    if (group == null)
                        group = Undo.AddComponent<CanvasGroup>(click.gameObject);
                    group.alpha = 0f;
                    group.interactable = false;
                    group.blocksRaycasts = false;
                    so.FindProperty("cgClickCanvasGroup").objectReferenceValue = group;
                    click.gameObject.SetActive(false);
                }
            }

            so.FindProperty("endingPanel").objectReferenceValue =
                endingPanel != null ? endingPanel.gameObject : null;
            Transform scrollPanel = null;
            if (endingPanel != null)
            {
                AssignImage(so, "endingPanelImage", endingPanel.Find("endingImage"));
                AssignText(so, "endingText", endingPanel.Find("endingText"));

                scrollPanel = endingPanel.Find("ScrollPanel");
                so.FindProperty("scrollPanel").objectReferenceValue =
                    scrollPanel != null ? scrollPanel.gameObject : null;

                if (scrollPanel != null)
                {
                    AssignImage(so, "scrollEndingImage", scrollPanel.Find("endingImage"));
                    Transform credit = scrollPanel.Find("CreditText");
                    if (credit != null)
                    {
                        so.FindProperty("creditText").objectReferenceValue =
                            credit.GetComponent<TMP_Text>();
                        so.FindProperty("creditRect").objectReferenceValue =
                            credit.GetComponent<RectTransform>();
                    }

                    scrollPanel.gameObject.SetActive(false);
                }

                Transform panelImage = endingPanel.Find("endingImage");
                if (panelImage != null)
                    panelImage.gameObject.SetActive(false);
                Transform endingText = endingPanel.Find("endingText");
                if (endingText != null)
                    endingText.gameObject.SetActive(false);
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            if (dialog != null)
                dialog.gameObject.SetActive(false);
            if (metaPanel != null)
                metaPanel.gameObject.SetActive(false);
            if (endingCg != null)
                endingCg.gameObject.SetActive(false);
            if (endingPanel != null)
                endingPanel.gameObject.SetActive(false);
            gameOver.gameObject.SetActive(false);

            MealEatController[] meals = Object.FindObjectsByType<MealEatController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            int mealCount = 0;
            for (int i = 0; i < meals.Length; i++)
            {
                SerializedObject mso = new SerializedObject(meals[i]);
                mso.FindProperty("endingSequence").objectReferenceValue = seq;
                mso.FindProperty("gameOverPanel").objectReferenceValue = gameOver.gameObject;
                mso.ApplyModifiedPropertiesWithoutUndo();
                mealCount++;
            }

            EditorUtility.SetDirty(gameOver.gameObject);
            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            return "Ending scene setup OK. fade=" + (fadeImg != null)
                + " meta=" + (metaPanel != null)
                + " scroll=" + (scrollPanel != null)
                + " meals=" + mealCount
                + " catalog=" + (endingCatalog != null)
                + " saved=" + saved;
        }

        static Image EnsureFadeOverlay(Transform canvas)
        {
            Transform fadeT = canvas.Find("FadeOverlay");
            if (fadeT == null)
            {
                GameObject go = new GameObject(
                    "FadeOverlay",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                go.layer = canvas.gameObject.layer;
                go.transform.SetParent(canvas, false);
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one;
                Image img = go.GetComponent<Image>();
                img.color = new Color(0f, 0f, 0f, 0f);
                img.raycastTarget = true;
                go.SetActive(false);
                go.transform.SetAsLastSibling();
                Undo.RegisterCreatedObjectUndo(go, "Create FadeOverlay");
                return img;
            }

            fadeT.SetAsLastSibling();
            return fadeT.GetComponent<Image>();
        }

        static void AssignText(SerializedObject so, string property, Transform target)
        {
            if (target == null)
                return;

            so.FindProperty(property).objectReferenceValue = target.GetComponent<TMP_Text>();
        }

        static void AssignImage(SerializedObject so, string property, Transform target)
        {
            if (target == null)
                return;

            so.FindProperty(property).objectReferenceValue = target.GetComponent<Image>();
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
