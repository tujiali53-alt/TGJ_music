using Cooking.Data;
using Cooking.Exploration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cooking.EditorTools
{
    public static class PlacedPickupWire
    {
        const string DefinitionPath = "Assets/_Project/Data/Ingredients/ing_mushroom.asset";

        static bool retried;

        [InitializeOnLoadMethod]
        static void Schedule()
        {
            EditorApplication.delayCall += WireWhenIdle;
        }

        [MenuItem("Cooking/Wire Placed Mushroom")]
        public static void WireWhenIdle()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += WireWhenIdle;
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            IngredientDefinition definition = AssetDatabase.LoadAssetAtPath<IngredientDefinition>(DefinitionPath);
            if (definition == null)
            {
                if (!retried)
                {
                    retried = true;
                    EditorApplication.delayCall += WireWhenIdle;
                    return;
                }

                Debug.LogError("找不到食材资产：" + DefinitionPath);
                return;
            }

            int wired = 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                foreach (GameObject root in scene.GetRootGameObjects())
                    wired += WireTree(root.transform, definition, scene);
            }

            if (wired == 0)
                Debug.Log("没有在已打开的场景里找到 masheroom / Mushroom。请先保存你摆好采集物的 outingScene，不要从磁盘重新加载。");
        }

        static int WireTree(Transform node, IngredientDefinition definition, Scene scene)
        {
            int wired = WireOne(node.gameObject, definition, scene);
            for (int i = 0; i < node.childCount; i++)
                wired += WireTree(node.GetChild(i), definition, scene);
            return wired;
        }

        static int WireOne(GameObject go, IngredientDefinition definition, Scene scene)
        {
            if (!IsPlacedMushroom(go))
                return 0;

            bool changed = false;
            IngredientPickup pickup = go.GetComponent<IngredientPickup>();
            if (pickup == null)
            {
                pickup = Undo.AddComponent<IngredientPickup>(go);
                changed = true;
            }

            SerializedObject so = new SerializedObject(pickup);
            so.FindProperty("definition").objectReferenceValue = definition;
            Transform player = FindNamed(scene, "Player");
            if (player != null)
                so.FindProperty("player").objectReferenceValue = player;

            Collider2D collider = go.GetComponent<Collider2D>();
            if (collider == null)
            {
                var box = Undo.AddComponent<BoxCollider2D>(go);
                SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.sprite != null)
                {
                    box.size = renderer.sprite.bounds.size;
                    box.offset = renderer.sprite.bounds.center;
                }

                collider = box;
                changed = true;
            }

            so.FindProperty("hitCollider").objectReferenceValue = collider;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("已把采集脚本挂到 " + go.name + "。请保存 outingScene。", go);
            }

            return 1;
        }

        static bool IsPlacedMushroom(GameObject go)
        {
            string name = go.name.ToLowerInvariant();
            if (name.Contains("mash") || name.Contains("mush"))
                return true;

            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null)
                return false;

            string spriteName = renderer.sprite.name.ToLowerInvariant();
            if (spriteName.Contains("mush"))
                return true;

            string path = AssetDatabase.GetAssetPath(renderer.sprite);
            return path.Contains("Mushroom");
        }

        static Transform FindNamed(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform found = FindChild(root.transform, objectName);
                if (found != null)
                    return found;
            }

            return null;
        }

        static Transform FindChild(Transform node, string objectName)
        {
            if (node.name == objectName)
                return node;

            for (int i = 0; i < node.childCount; i++)
            {
                Transform found = FindChild(node.GetChild(i), objectName);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
