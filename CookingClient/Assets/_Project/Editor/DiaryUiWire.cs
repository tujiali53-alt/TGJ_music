using Cooking.Data;
using Cooking.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cooking.EditorTools
{
    public static class DiaryUiWire
    {
        const string ScenePath = "Assets/_Project/Scenes/MainCooking.unity";
        const string CatalogAssetPath = "Assets/_Project/Data/Diary/DiaryCatalog.asset";

        [MenuItem("Cooking/Wire Diary UI")]
        public static void WireMenu()
        {
            string error = Wire();
            if (error == null)
                Debug.Log("日记界面已接到 MainCooking。");
            else
                Debug.LogError(error);
        }

        public static string Wire()
        {
            DiaryCatalog catalog = AssetDatabase.LoadAssetAtPath<DiaryCatalog>(CatalogAssetPath);
            if (catalog == null)
                return "缺少 DiaryCatalog，请先执行 Cooking/Import Diary Tables。";

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Transform diary = FindNamed(scene, "DiaryPanel");
            Transform interaction = FindNamed(scene, "InteractionPanel");
            if (diary == null || interaction == null)
                return "MainCooking 需要 Canvas/DiaryPanel 和 Canvas/InteractionPanel。";

            DiaryPanelView view = diary.GetComponent<DiaryPanelView>();
            if (view == null)
                view = Undo.AddComponent<DiaryPanelView>(diary.gameObject);

            SerializedObject serialized = new SerializedObject(view);
            serialized.FindProperty("catalog").objectReferenceValue = catalog;
            serialized.FindProperty("language").enumValueIndex = (int)DiaryLanguage.Zh;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (interaction.GetComponent<DiaryNotice>() == null)
                Undo.AddComponent<DiaryNotice>(interaction.gameObject);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return null;
        }

        static Transform FindNamed(Scene scene, string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] children = roots[i].GetComponentsInChildren<Transform>(true);
                for (int c = 0; c < children.Length; c++)
                {
                    if (children[c].name == objectName)
                        return children[c];
                }
            }

            return null;
        }
    }
}
