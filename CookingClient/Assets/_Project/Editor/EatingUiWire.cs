using Cooking.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cooking.EditorTools
{
    public static class EatingUiWire
    {
        static bool loggedMissing;

        [InitializeOnLoadMethod]
        static void Schedule()
        {
            EditorApplication.delayCall += WireWhenIdle;
        }

        [MenuItem("Cooking/Wire Eating UI")]
        public static void WireWhenIdle()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += WireWhenIdle;
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != SceneNamesCooking())
                return;

            Button eatButton = FindButton(scene, "EattingButton");
            Button nightButton = FindButton(scene, "enterNightButton");
            TMP_Text mealsText = FindText(scene, "NumberOfMealsText");
            Transform panel = FindNamed(scene, "EatingCheckPanel");
            Transform canvas = FindNamed(scene, "Canvas");

            if (eatButton == null || nightButton == null || mealsText == null || panel == null || canvas == null)
            {
                if (!loggedMissing)
                {
                    loggedMissing = true;
                    Debug.LogError("MainCooking 需要 EattingButton、NumberOfMealsText、EatingCheckPanel、enterNightButton，以及 Canvas。");
                }

                return;
            }

            MealEatController controller = canvas.GetComponent<MealEatController>();
            bool added = controller == null;
            if (added)
                controller = Undo.AddComponent<MealEatController>(canvas.gameObject);

            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("eatButton").objectReferenceValue = eatButton;
            serialized.FindProperty("enterNightButton").objectReferenceValue = nightButton;
            serialized.FindProperty("mealsText").objectReferenceValue = mealsText;
            serialized.FindProperty("resultPanel").objectReferenceValue = panel.gameObject;
            if (added)
                serialized.FindProperty("resultDelay").floatValue = 1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
        }

        static string SceneNamesCooking()
        {
            return "MainCooking";
        }

        static Button FindButton(Scene scene, string objectName)
        {
            Transform found = FindNamed(scene, objectName);
            if (found == null)
                return null;

            return found.GetComponent<Button>();
        }

        static TMP_Text FindText(Scene scene, string objectName)
        {
            Transform found = FindNamed(scene, objectName);
            if (found == null)
                return null;

            return found.GetComponent<TMP_Text>();
        }

        static Transform FindNamed(Scene scene, string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int t = 0; t < transforms.Length; t++)
                {
                    if (transforms[t].name == objectName)
                        return transforms[t];
                }
            }

            return null;
        }
    }
}
