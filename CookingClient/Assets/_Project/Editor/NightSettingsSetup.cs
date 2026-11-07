using Cooking.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cooking.EditorTools
{
    public static class NightSettingsSetup
    {
        const string OutingPath = "Assets/_Project/Scenes/outingScene.unity";
        const string CookingPath = "Assets/_Project/Scenes/MainCooking.unity";

        [MenuItem("Cooking/Setup Night Settings")]
        public static void SetupMenu()
        {
            Debug.Log(SetupOutingSettings());
        }

        public static string SetupOutingSettings()
        {
            Scene cooking = EditorSceneManager.OpenScene(CookingPath, OpenSceneMode.Additive);
            Scene outing = EditorSceneManager.OpenScene(OutingPath, OpenSceneMode.Additive);
            EditorSceneManager.SetActiveScene(outing);

            Transform cookingCanvas = FindDeep(cooking, "Canvas");
            Transform outingCanvas = FindDeep(outing, "Canvas");
            Transform outingInteraction = FindDeep(outing, "InteractionPanel");
            if (cookingCanvas == null || outingCanvas == null || outingInteraction == null)
            {
                EditorSceneManager.CloseScene(cooking, true);
                return "missing Canvas or InteractionPanel";
            }

            CopyUnder(FindChild(cookingCanvas, "SettingPanel"), outingCanvas);
            CopyUnder(FindChild(cookingCanvas, "InsructionPanel"), outingCanvas);
            CopyUnder(FindChild(FindDeep(cooking, "InteractionPanel"), "SettingButton"), outingInteraction);

            SettingsPanelController controller = outingCanvas.GetComponent<SettingsPanelController>();
            if (controller == null)
                controller = Undo.AddComponent<SettingsPanelController>(outingCanvas.gameObject);

            EditorSceneManager.MarkSceneDirty(outing);
            bool saved = EditorSceneManager.SaveScene(outing);
            EditorSceneManager.CloseScene(cooking, true);
            return "night settings saved=" + saved + " controller=" + (controller != null);
        }

        static void CopyUnder(Transform source, Transform parent)
        {
            if (source == null || parent == null)
                return;

            Transform existing = parent.Find(source.name);
            if (existing != null)
                Undo.DestroyObjectImmediate(existing.gameObject);

            GameObject copy = Object.Instantiate(source.gameObject, parent);
            copy.name = source.name;
            Undo.RegisterCreatedObjectUndo(copy, "Copy " + source.name);
            if (source.name == "SettingPanel" || source.name == "InsructionPanel")
                copy.SetActive(false);
        }

        static Transform FindChild(Transform parent, string name)
        {
            if (parent == null)
                return null;
            return parent.Find(name);
        }

        static Transform FindDeep(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform found = FindDeep(roots[i].transform, name);
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
