using Cooking.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cooking.EditorTools
{
    public static class IngredientDetailCheck
    {
        const string MushroomPath = "Assets/_Project/Data/Ingredients/ing_mushroom.asset";
        const string CatalogPath = "Assets/_Project/Data/IngredientAttributeCatalog.asset";

        [MenuItem("Cooking/Validate Ingredient Details")]
        public static void RunMenu()
        {
            string error = Run();
            if (error == null)
                Debug.Log("食材详情对照通过：白蘑菇中文为整块、生的，英文名为 White Mushroom。");
            else
                Debug.LogError(error);
        }

        public static string Run()
        {
            IngredientDefinition mushroom = AssetDatabase.LoadAssetAtPath<IngredientDefinition>(MushroomPath);
            if (mushroom == null)
                return "缺少 ing_mushroom，请先执行 Cooking/Import Cooking Tables。";

            if (mushroom.displayName != "白蘑菇")
                return "ing_mushroom.displayName 应为 白蘑菇，实际 " + mushroom.displayName;
            if (mushroom.displayNameEn != "White Mushroom")
                return "ing_mushroom.displayNameEn 应为 White Mushroom，实际 " + mushroom.displayNameEn;

            const string descriptionZh = "生长在森林中的菌类，形似夜空中皎白的明月。";
            if (mushroom.description != descriptionZh)
                return "ing_mushroom.description 与食材描述不一致。";
            if (mushroom.descriptionEn != "A fungus that grows in the forest, shaped like a bright white moon in the night sky.")
                return "ing_mushroom.descriptionEn 与英翻对照不一致。";

            if (mushroom.kind != IngredientKind.Vegetable || mushroom.canEatRaw || !mushroom.canCut)
                return "ing_mushroom 应为蔬菜、不可生食、可切。";
            if (mushroom.optimalDoneness != 120f || mushroom.maxDoneness != 180f)
                return "ing_mushroom 熟度应为 120 / 180。";

            IngredientAttributeCatalog catalog = AssetDatabase.LoadAssetAtPath<IngredientAttributeCatalog>(CatalogPath);
            if (catalog == null)
                return "缺少 IngredientAttributeCatalog，请先执行 Cooking/Import Cooking Tables。";

            string mismatch = Expect(catalog, "label.description", "食物描述：", "");
            if (mismatch != null)
                return mismatch;
            mismatch = Expect(catalog, "label.kind", "类别：", "Kind:");
            if (mismatch != null)
                return mismatch;
            mismatch = Expect(catalog, "label.optimalDoneness", "最佳熟度：", "OptimalDoneness:");
            if (mismatch != null)
                return mismatch;
            mismatch = Expect(catalog, "label.maxDoneness", "最大熟度：", "MaxDoneness:");
            if (mismatch != null)
                return mismatch;
            mismatch = Expect(catalog, "label.state", "状态：", "State:");
            if (mismatch != null)
                return mismatch;
            mismatch = Expect(catalog, "canCut.true", "可切", "True");
            if (mismatch != null)
                return mismatch;
            mismatch = Expect(catalog, "canCut.false", "不可切", "False");
            if (mismatch != null)
                return mismatch;
            mismatch = Expect(catalog, "canEatRaw.true", "可煎煮", "True");
            if (mismatch != null)
                return mismatch;
            mismatch = Expect(catalog, "canEatRaw.false", "不可煎煮", "False");
            if (mismatch != null)
                return mismatch;
            mismatch = Expect(catalog, "kind.Vegetable", "蔬菜", "Vegetable");
            if (mismatch != null)
                return mismatch;
            mismatch = Expect(catalog, "kind.Meat", "肉类", "Meat");
            if (mismatch != null)
                return mismatch;
            mismatch = Expect(catalog, "shape.Whole", "整块", "Whole");
            if (mismatch != null)
                return mismatch;
            mismatch = Expect(catalog, "shape.Dice", "切丁状", "Dice");
            if (mismatch != null)
                return mismatch;
            mismatch = Expect(catalog, "shape.Slice", "片状", "Slice");
            if (mismatch != null)
                return mismatch;
            mismatch = Expect(catalog, "shape.Shred", "丝状", "Shred");
            if (mismatch != null)
                return mismatch;
            mismatch = Expect(catalog, "cook.Raw", "生的", "Raw");
            if (mismatch != null)
                return mismatch;
            mismatch = Expect(catalog, "cook.Boiled", "煮过", "Boiled");
            if (mismatch != null)
                return mismatch;
            mismatch = Expect(catalog, "cook.Fried", "煎过", "Fried");
            if (mismatch != null)
                return mismatch;
            mismatch = Expect(catalog, "cook.BoiledAndFried", "煎煮过", "BoiledAndFried");
            if (mismatch != null)
                return mismatch;

            if (IngredientDetailText.Flag(catalog, "canCut", true, DiaryLanguage.Zh) != "可切")
                return "中文可切短语拼接错误。";
            if (IngredientDetailText.Flag(catalog, "canCut", false, DiaryLanguage.En) != "CanCut: False")
                return "英文 CanCut 拼接错误。";
            if (IngredientDetailText.Flag(catalog, "canEatRaw", false, DiaryLanguage.Zh) != "不可煎煮")
                return "中文不可煎煮短语拼接错误。";
            if (IngredientDetailText.Flag(catalog, "canEatRaw", true, DiaryLanguage.En) != "CanEatRaw: True")
                return "英文 CanEatRaw 拼接错误。";
            if (IngredientDetailText.State(catalog, CutShape.Whole, CookMethod.Raw, DiaryLanguage.Zh) != "整块；生的")
                return "中文状态应为 整块；生的。";
            if (IngredientDetailText.State(catalog, CutShape.Dice, CookMethod.Fried, DiaryLanguage.En) != "Dice; Fried")
                return "英文状态应为 Dice; Fried。";
            if (IngredientDetailText.Number(mushroom.optimalDoneness) != "120")
                return "最佳熟度显示应为 120。";
            if (IngredientDetailText.Number(mushroom.maxDoneness) != "180")
                return "最大熟度显示应为 180。";
            if (IngredientDetailText.DisplayName(mushroom, DiaryLanguage.En) != "White Mushroom")
                return "英文显示名未从食材表读出。";

            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "MainCooking")
                return null;

            IngredientDetailsViewMissing missing = FindDetails(scene);
            return missing.Error;
        }

        static string Expect(IngredientAttributeCatalog catalog, string id, string zh, string en)
        {
            if (!IngredientDetailText.TryText(catalog, id, DiaryLanguage.Zh, out string actualZh))
                return "AttributeNames 缺少 " + id;
            if (actualZh != zh)
                return id + " 中文应为 " + zh + "，实际 " + actualZh;

            if (!IngredientDetailText.TryText(catalog, id, DiaryLanguage.En, out string actualEn))
                return "AttributeNames 缺少 " + id;
            if (actualEn != en)
                return id + " 英文应为 " + en + "，实际 " + actualEn;

            return null;
        }

        struct IngredientDetailsViewMissing
        {
            public string Error;
        }

        static IngredientDetailsViewMissing FindDetails(Scene scene)
        {
            Cooking.UI.IngredientDetails[] views = Resources.FindObjectsOfTypeAll<Cooking.UI.IngredientDetails>();
            Cooking.UI.IngredientDetails view = null;
            for (int i = 0; i < views.Length; i++)
            {
                Cooking.UI.IngredientDetails candidate = views[i];
                if (candidate == null || EditorUtility.IsPersistent(candidate))
                    continue;
                if (candidate.gameObject.scene == scene)
                {
                    view = candidate;
                    break;
                }
            }

            if (view == null)
            {
                return new IngredientDetailsViewMissing
                {
                    Error = "MainCooking 的 TrayPanel 上需要有 IngredientDetails。"
                };
            }

            SerializedObject so = new SerializedObject(view);
            SerializedProperty catalogProp = so.FindProperty("catalog");
            if (catalogProp == null || catalogProp.objectReferenceValue == null)
            {
                return new IngredientDetailsViewMissing
                {
                    Error = "IngredientDetails 未绑定 IngredientAttributeCatalog。"
                };
            }

            Transform panel = view.transform.Find("DetailsPanel");
            if (panel == null)
            {
                return new IngredientDetailsViewMissing
                {
                    Error = "TrayPanel 下需要有 DetailsPanel。"
                };
            }

            if (panel.gameObject.activeSelf)
            {
                return new IngredientDetailsViewMissing
                {
                    Error = "DetailsPanel 默认应关闭。"
                };
            }

            return new IngredientDetailsViewMissing();
        }
    }
}
