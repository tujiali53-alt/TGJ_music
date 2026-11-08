using System.Collections.Generic;
using System.Reflection;
using Cooking.Data;
using Cooking.Runtime;
using Cooking.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cooking.EditorTools
{
    public static class UiSpriteWire
    {
        static readonly string[] ScenePaths =
        {
            "Assets/_Project/Scenes/MeunScene.unity",
            "Assets/_Project/Scenes/outingScene.unity",
            "Assets/_Project/Scenes/MainCooking.unity"
        };

        [MenuItem("Cooking/Wire Ui Sprites")]
        public static void WireMenu()
        {
            string error = Wire();
            if (error == null)
                Debug.Log("UiSpriteView 已按表接到三张场景。");
            else
                Debug.LogError(error);
        }

        public static string Wire()
        {
            UiSpriteCatalog catalog = AssetDatabase.LoadAssetAtPath<UiSpriteCatalog>(
                UiSpriteTableImporter.CatalogAssetPath);
            if (catalog == null)
                return "缺少 UiSpriteCatalog，请先执行 Cooking/Import Ui Sprite Tables。";

            var import = new UiSpriteTableImporter.ImportResult();
            List<UiSpriteTableImporter.UiSpriteTargetRow> targets = UiSpriteTableImporter.ReadTargets(import);
            if (import.ErrorCount > 0)
                return "UiSpriteTargets 读取失败。";

            string previous = SceneManager.GetActiveScene().path;
            var missing = new List<string>();
            for (int s = 0; s < ScenePaths.Length; s++)
            {
                string scenePath = ScenePaths[s];
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                bool dirty = false;

                for (int t = 0; t < targets.Count; t++)
                {
                    UiSpriteTableImporter.UiSpriteTargetRow target = targets[t];
                    if (target.scene != sceneName)
                        continue;

                    if (!catalog.TryGet(target.id, out _))
                        return "目录里没有 id：" + target.id;

                    Transform obj = FindPath(scene, target.objectPath);
                    if (obj == null)
                    {
                        missing.Add(sceneName + " 找不到 " + target.objectPath);
                        continue;
                    }

                    Image image = obj.GetComponent<Image>();
                    if (image == null)
                    {
                        missing.Add(target.objectPath + " 上需要 Image。");
                        continue;
                    }

                    UiSpriteView view = obj.GetComponent<UiSpriteView>();
                    if (view == null)
                        view = Undo.AddComponent<UiSpriteView>(obj.gameObject);

                    Undo.RecordObject(view, "Wire UiSpriteView");
                    SetPrivate(view, "id", target.id);
                    SetPrivate(view, "catalog", catalog);
                    SetPrivate(view, "language", DiaryLanguage.En);
                    if (!ReferenceEquals(GetPrivate(view, "catalog"), catalog))
                        return target.objectPath + " 未能写入 UiSpriteCatalog。";

                    Sprite sprite = UiSprites.SpriteFor(catalog, target.id, DiaryLanguage.En);
                    if (sprite == null)
                        sprite = UiSprites.SpriteFor(catalog, target.id, DiaryLanguage.Zh);
                    if (sprite != null)
                    {
                        image.sprite = sprite;
                        image.color = Color.white;
                        EditorUtility.SetDirty(image);
                    }

                    EditorUtility.SetDirty(view);
                    dirty = true;
                }

                if (dirty)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }

            if (!string.IsNullOrEmpty(previous) && System.IO.File.Exists(previous))
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);

            for (int i = 0; i < missing.Count; i++)
                Debug.LogWarning(missing[i] + "（未挂接）");

            return null;
        }

        static Transform FindPath(Scene scene, string objectPath)
        {
            string[] parts = objectPath.Split('/');
            if (parts.Length == 0)
                return null;

            GameObject[] roots = scene.GetRootGameObjects();
            Transform current = null;
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == parts[0])
                {
                    current = roots[i].transform;
                    break;
                }
            }

            if (current == null)
                return null;

            for (int p = 1; p < parts.Length; p++)
            {
                Transform next = FindChildIncludingInactive(current, parts[p]);
                if (next == null)
                    return null;
                current = next;
            }

            return current;
        }

        static Transform FindChildIncludingInactive(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == childName)
                    return child;
            }

            return null;
        }

        static void SetPrivate(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                throw new System.MissingFieldException(target.GetType().FullName, fieldName);
            field.SetValue(target, value);
        }

        static object GetPrivate(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            return field != null ? field.GetValue(target) : null;
        }
    }
}
