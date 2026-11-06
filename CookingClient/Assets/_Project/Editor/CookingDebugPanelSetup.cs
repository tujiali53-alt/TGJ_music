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
    public static class CookingDebugPanelSetup
    {
        const string MainCookingPath = "Assets/_Project/Scenes/MainCooking.unity";

        [MenuItem("Cooking/Setup Cooking Debug Panel")]
        public static void SetupMenu()
        {
            Debug.Log(SetupMainCooking());
        }

        public static string SetupMainCooking()
        {
            Scene scene = EditorSceneManager.OpenScene(MainCookingPath, OpenSceneMode.Single);
            Transform canvas = FindDeep(scene, "Canvas");
            if (canvas == null)
                return "Canvas missing";

            Transform debugButtonT = canvas.Find("DebugButton");
            if (debugButtonT == null)
                return "Canvas/DebugButton missing";

            Button debugButton = debugButtonT.GetComponent<Button>();
            GameObject panelGo = EnsurePanel(canvas);
            CookingDebugPanel controller = canvas.GetComponent<CookingDebugPanel>();
            if (controller == null)
                controller = Undo.AddComponent<CookingDebugPanel>(canvas.gameObject);

            DiaryCatalog diaryCatalog = AssetDatabase.LoadAssetAtPath<DiaryCatalog>(
                "Assets/_Project/Data/Diary/DiaryCatalog.asset");
            NpcCatalog npcCatalog = AssetDatabase.LoadAssetAtPath<NpcCatalog>(
                "Assets/_Project/Data/NpcCatalog.asset");
            MealEatController mealEat = Object.FindFirstObjectByType<MealEatController>(
                FindObjectsInactive.Include);

            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("debugButton").objectReferenceValue = debugButton;
            so.FindProperty("panel").objectReferenceValue = panelGo;
            so.FindProperty("todayDiaryButton").objectReferenceValue =
                FindButton(panelGo.transform, "TodayDiaryButton");
            so.FindProperty("unlockAllDiariesButton").objectReferenceValue =
                FindButton(panelGo.transform, "UnlockAllDiariesButton");
            so.FindProperty("grantAllPropsButton").objectReferenceValue =
                FindButton(panelGo.transform, "GrantAllPropsButton");
            so.FindProperty("perfectEatButton").objectReferenceValue =
                FindButton(panelGo.transform, "PerfectEatButton");
            so.FindProperty("jumpDayButton").objectReferenceValue =
                FindButton(panelGo.transform, "JumpDayButton");
            so.FindProperty("closeButton").objectReferenceValue =
                FindButton(panelGo.transform, "CloseButton");

            Transform today = panelGo.transform.Find("TodayDiaryButton");
            if (today != null)
            {
                TMP_Text label = today.GetComponentInChildren<TMP_Text>(true);
                so.FindProperty("todayDiaryLabel").objectReferenceValue = label;
            }

            Transform dayJump = panelGo.transform.Find("DayJumpRow");
            if (dayJump != null)
            {
                so.FindProperty("dayPrevButton").objectReferenceValue =
                    FindButton(dayJump, "DayPrevButton");
                so.FindProperty("dayNextButton").objectReferenceValue =
                    FindButton(dayJump, "DayNextButton");
                Transform dayLabel = dayJump.Find("DaySelectLabel");
                if (dayLabel != null)
                    so.FindProperty("daySelectLabel").objectReferenceValue =
                        dayLabel.GetComponent<TMP_Text>();
            }

            so.FindProperty("diaryCatalog").objectReferenceValue = diaryCatalog;
            so.FindProperty("npcCatalog").objectReferenceValue = npcCatalog;
            so.FindProperty("mealEat").objectReferenceValue = mealEat;

            if (mealEat != null && mealEat.Recipes != null)
            {
                SerializedProperty recipesProp = so.FindProperty("recipes");
                recipesProp.arraySize = mealEat.Recipes.Length;
                for (int i = 0; i < mealEat.Recipes.Length; i++)
                    recipesProp.GetArrayElementAtIndex(i).objectReferenceValue = mealEat.Recipes[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            panelGo.SetActive(false);

            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            return "Cooking debug panel setup OK. saved=" + saved
                + " mealEat=" + (mealEat != null)
                + " diary=" + (diaryCatalog != null)
                + " npc=" + (npcCatalog != null);
        }

        static GameObject EnsurePanel(Transform canvas)
        {
            Transform existing = canvas.Find("DebugPanel");
            GameObject panel;
            if (existing != null)
            {
                panel = existing.gameObject;
            }
            else
            {
                panel = CreateUiObject("DebugPanel", canvas);
                Image bg = panel.AddComponent<Image>();
                bg.color = new Color(0f, 0f, 0f, 0.75f);
                Undo.RegisterCreatedObjectUndo(panel, "Create DebugPanel");
            }

            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(24f, 24f);
            rt.sizeDelta = new Vector2(420f, 460f);

            EnsureButtons(panel.transform);
            return panel;
        }

        static void EnsureButtons(Transform panel)
        {
            CreateDebugButton(panel, "TodayDiaryButton", "当天日记：未解锁", 0);
            CreateDebugButton(panel, "UnlockAllDiariesButton", "一键解锁全部日记", 1);
            CreateDebugButton(panel, "GrantAllPropsButton", "一键获得全部NPC道具", 2);
            CreateDebugButton(panel, "PerfectEatButton", "一键完美进食", 3);
            EnsureDayJumpRow(panel, 4);
            CreateDebugButton(panel, "JumpDayButton", "跳转到所选天", 5);
            CreateDebugButton(panel, "CloseButton", "关闭", 6);
        }

        static void EnsureDayJumpRow(Transform panel, int index)
        {
            Transform rowT = panel.Find("DayJumpRow");
            GameObject row;
            if (rowT == null)
            {
                row = CreateUiObject("DayJumpRow", panel);
            }
            else
            {
                row = rowT.gameObject;
            }

            RectTransform rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -16f - index * 56f);
            rt.sizeDelta = new Vector2(380f, 48f);

            CreateSideButton(row.transform, "DayPrevButton", "<", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(56f, 48f));
            CreateSideButton(row.transform, "DayNextButton", ">", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0f), new Vector2(56f, 48f));
            EnsureDaySelectLabel(row.transform);
        }

        static void EnsureDaySelectLabel(Transform row)
        {
            Transform existing = row.Find("DaySelectLabel");
            GameObject labelGo;
            if (existing == null)
            {
                labelGo = CreateUiObject("DaySelectLabel", row);
                TextMeshProUGUI tmp = labelGo.AddComponent<TextMeshProUGUI>();
                tmp.text = "跳转：第1天";
                tmp.fontSize = 22f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.raycastTarget = false;
            }
            else
            {
                labelGo = existing.gameObject;
            }

            RectTransform rt = labelGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(64f, 0f);
            rt.offsetMax = new Vector2(-64f, 0f);
        }

        static void CreateSideButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 size)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                return;

            GameObject buttonGo = CreateUiObject(name, parent);
            RectTransform rt = buttonGo.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;

            Image image = buttonGo.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);
            Button button = buttonGo.AddComponent<Button>();
            button.targetGraphic = image;

            GameObject textGo = CreateUiObject("Label", buttonGo.transform);
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 24f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
        }

        static void CreateDebugButton(Transform parent, string name, string label, int index)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                RectTransform existingRt = existing as RectTransform;
                if (existingRt != null)
                    existingRt.anchoredPosition = new Vector2(0f, -16f - index * 56f);

                TMP_Text oldText = existing.GetComponentInChildren<TMP_Text>(true);
                if (oldText != null && name != "TodayDiaryButton")
                    oldText.text = label;
                return;
            }

            GameObject buttonGo = CreateUiObject(name, parent);
            RectTransform rt = buttonGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -16f - index * 56f);
            rt.sizeDelta = new Vector2(380f, 48f);

            Image image = buttonGo.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);
            Button button = buttonGo.AddComponent<Button>();
            button.targetGraphic = image;

            GameObject textGo = CreateUiObject("Label", buttonGo.transform);
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(8f, 4f);
            textRt.offsetMax = new Vector2(-8f, -4f);

            TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 22f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
        }

        static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one;
            return go;
        }

        static Button FindButton(Transform root, string childName)
        {
            Transform found = root.Find(childName);
            return found != null ? found.GetComponent<Button>() : null;
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
