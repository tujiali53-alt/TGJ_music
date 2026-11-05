using System.Collections.Generic;
using System.Text;
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
    public static class UiSpriteValidate
    {
        static readonly string[] ScenePaths =
        {
            "Assets/_Project/Scenes/MeunScene.unity",
            "Assets/_Project/Scenes/outingScene.unity",
            "Assets/_Project/Scenes/MainCooking.unity"
        };

        [MenuItem("Cooking/Validate Ui Sprites")]
        public static void ValidateMenu()
        {
            string error = Validate();
            if (error == null)
                Debug.Log("Ui Sprites 校验通过。");
            else
                Debug.LogError(error);
        }

        public static string Validate()
        {
            UiSpriteCatalog catalog = AssetDatabase.LoadAssetAtPath<UiSpriteCatalog>(
                UiSpriteTableImporter.CatalogAssetPath);
            if (catalog == null)
                return "缺少 UiSpriteCatalog，请先执行 Cooking/Import Ui Sprite Tables。";

            if (catalog.rows == null || catalog.rows.Length == 0)
                return "UiSpriteCatalog 为空。";

            var seen = new HashSet<string>();
            for (int i = 0; i < catalog.rows.Length; i++)
            {
                UiSpriteRow row = catalog.rows[i];
                if (string.IsNullOrEmpty(row.id))
                    return "目录有空 id。";
                if (!seen.Add(row.id))
                    return "目录 id 重复：" + row.id;
                if (row.spriteZh == null)
                    return row.id + " 缺少 spriteZh。";

                Sprite en = UiSprites.SpriteFor(catalog, row.id, DiaryLanguage.En);
                Sprite zh = UiSprites.SpriteFor(catalog, row.id, DiaryLanguage.Zh);
                if (zh == null)
                    return row.id + " Zh 取图失败。";
                if (row.spriteEn != null && en == null)
                    return row.id + " En 取图失败。";
                if (row.spriteEn == null && en != zh)
                    return row.id + " 无英文图时 En 应回退中文图。";
            }

            var import = new UiSpriteTableImporter.ImportResult();
            List<UiSpriteTableImporter.UiSpriteTargetRow> targets = UiSpriteTableImporter.ReadTargets(import);
            if (import.ErrorCount > 0)
                return "UiSpriteTargets 读取失败。";

            var wired = new HashSet<string>();
            string previous = SceneManager.GetActiveScene().path;
            var notes = new StringBuilder();

            for (int s = 0; s < ScenePaths.Length; s++)
            {
                string scenePath = ScenePaths[s];
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

                for (int t = 0; t < targets.Count; t++)
                {
                    UiSpriteTableImporter.UiSpriteTargetRow target = targets[t];
                    if (target.scene != sceneName)
                        continue;

                    Transform obj = FindPath(scene, target.objectPath);
                    if (obj == null)
                    {
                        notes.AppendLine("未挂接：" + target.id + " @ " + sceneName + "/" + target.objectPath);
                        continue;
                    }

                    UiSpriteView view = obj.GetComponent<UiSpriteView>();
                    if (view == null)
                        return target.objectPath + " 缺少 UiSpriteView。";
                    if (view.Id != target.id)
                        return target.objectPath + " id 应为 " + target.id + "，实际 " + view.Id;
                    if (view.Catalog != catalog)
                        return target.objectPath + " 未挂当前 UiSpriteCatalog。";

                    Image image = obj.GetComponent<Image>();
                    Sprite expectedZh = UiSprites.SpriteFor(catalog, target.id, DiaryLanguage.Zh);
                    if (image == null || expectedZh == null)
                        return target.objectPath + " 无法对照 Image.sprite。";
                    if (image.sprite == null)
                        return target.objectPath + " Image.sprite 为空。";

                    wired.Add(target.id);
                }
            }

            for (int i = 0; i < catalog.rows.Length; i++)
            {
                string id = catalog.rows[i].id;
                if (!wired.Contains(id))
                    notes.AppendLine("未挂接：" + id);
            }

            if (!string.IsNullOrEmpty(previous) && System.IO.File.Exists(previous))
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);

            if (notes.Length > 0)
                Debug.Log(notes.ToString().TrimEnd());

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
    }
}
