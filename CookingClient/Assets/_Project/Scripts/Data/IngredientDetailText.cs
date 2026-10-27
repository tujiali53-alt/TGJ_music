using System.Globalization;

namespace Cooking.Data
{
    public static class IngredientDetailText
    {
        public static readonly string[] RequiredIds =
        {
            "label.description",
            "label.kind",
            "label.optimalDoneness",
            "label.maxDoneness",
            "label.state",
            "label.canEatRaw",
            "label.canCut",
            "canEatRaw.true",
            "canEatRaw.false",
            "canCut.true",
            "canCut.false",
            "kind.Meat",
            "kind.Vegetable",
            "kind.Other",
            "shape.Whole",
            "shape.Dice",
            "shape.Slice",
            "shape.Shred",
            "cook.Raw",
            "cook.Boiled",
            "cook.Fried",
            "cook.BoiledAndFried",
            "state.joiner"
        };

        public static string DisplayName(IngredientDefinition definition, DiaryLanguage language)
        {
            if (definition == null)
                return "";

            if (language == DiaryLanguage.En && !string.IsNullOrEmpty(definition.displayNameEn))
                return definition.displayNameEn;

            return definition.displayName ?? "";
        }

        public static string Description(IngredientDefinition definition, DiaryLanguage language)
        {
            if (definition == null)
                return "";

            if (language == DiaryLanguage.En && !string.IsNullOrEmpty(definition.descriptionEn))
                return definition.descriptionEn;

            return definition.description ?? "";
        }

        public static string Number(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        public static bool TryText(
            IngredientAttributeCatalog catalog,
            string id,
            DiaryLanguage language,
            out string text)
        {
            text = "";
            if (catalog == null || catalog.rows == null || string.IsNullOrEmpty(id))
                return false;

            for (int i = 0; i < catalog.rows.Count; i++)
            {
                IngredientAttributeString row = catalog.rows[i];
                if (row.id != id)
                    continue;

                text = language == DiaryLanguage.En ? row.textEn ?? "" : row.textZh ?? "";
                return true;
            }

            return false;
        }

        public static string Label(IngredientAttributeCatalog catalog, string id, DiaryLanguage language)
        {
            if (TryText(catalog, id, language, out string text))
                return text;

            return "";
        }

        public static string Kind(IngredientAttributeCatalog catalog, IngredientKind kind, DiaryLanguage language)
        {
            return Label(catalog, "kind." + kind, language);
        }

        public static string Flag(
            IngredientAttributeCatalog catalog,
            string key,
            bool value,
            DiaryLanguage language)
        {
            string phrase = Label(catalog, key + "." + (value ? "true" : "false"), language);
            string label = Label(catalog, "label." + key, language);
            if (string.IsNullOrEmpty(label))
                return phrase;
            if (string.IsNullOrEmpty(phrase))
                return label;

            return label + " " + phrase;
        }

        public static string State(
            IngredientAttributeCatalog catalog,
            CutShape shape,
            CookMethod method,
            DiaryLanguage language)
        {
            string shapeText = Label(catalog, "shape." + shape, language);
            string cookText = Label(catalog, "cook." + method, language);
            string joiner = Label(catalog, "state.joiner", language);
            if (string.IsNullOrEmpty(joiner))
                joiner = language == DiaryLanguage.En ? "; " : "；";

            return shapeText + joiner + cookText;
        }
    }
}
