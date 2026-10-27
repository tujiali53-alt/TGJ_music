using System.Collections.Generic;
using Cooking.Data;
using Cooking.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cooking.EditorTools
{
    public static class RecipePageWire
    {
        const string ScenePath = "Assets/_Project/Scenes/MainCooking.unity";
        const string RecipesFolder = "Assets/_Project/Data/Recipes";

        [MenuItem("Cooking/Wire Recipe Page")]
        public static void WireMenu()
        {
            string error = Wire();
            if (error == null)
                Debug.Log("食谱页已接到 MainCooking/RecipeImage。");
            else
                Debug.LogError(error);
        }

        public static string Wire()
        {
            RecipeDefinition[] recipes = LoadDailyRecipes();
            if (recipes.Length == 0)
                return "Recipes 文件夹里没有 recipe_day 食谱。";

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Transform imageObject = FindNamed(scene, "RecipeImage");
            if (imageObject == null || imageObject.parent == null || imageObject.parent.name != "RecipePanel")
                return "MainCooking 需要 Canvas/RecipePanel/RecipeImage。";

            Image image = imageObject.GetComponent<Image>();
            if (image == null)
                return "RecipeImage 上需要 Image。";

            RecipePageView view = imageObject.GetComponent<RecipePageView>();
            if (view == null)
                view = Undo.AddComponent<RecipePageView>(imageObject.gameObject);

            SerializedObject serialized = new SerializedObject(view);
            serialized.Update();
            serialized.FindProperty("recipeImage").objectReferenceValue = image;
            serialized.FindProperty("language").enumValueIndex = (int)DiaryLanguage.Zh;
            SerializedProperty list = serialized.FindProperty("recipes");
            list.arraySize = recipes.Length;
            for (int i = 0; i < recipes.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = recipes[i];
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(view);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return null;
        }

        public static RecipeDefinition[] LoadDailyRecipes()
        {
            string[] guids = AssetDatabase.FindAssets("t:RecipeDefinition", new[] { RecipesFolder });
            var found = new List<RecipeDefinition>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                RecipeDefinition recipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>(path);
                if (recipe == null || string.IsNullOrEmpty(recipe.id))
                    continue;
                if (!recipe.id.StartsWith("recipe_day"))
                    continue;
                found.Add(recipe);
            }

            found.Sort((a, b) =>
            {
                int day = a.unlockDay.CompareTo(b.unlockDay);
                if (day != 0)
                    return day;
                return string.CompareOrdinal(a.id, b.id);
            });
            return found.ToArray();
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
