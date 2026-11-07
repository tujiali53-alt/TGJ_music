using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cooking.Data
{
    [Serializable]
    public sealed class TutorialLine
    {
        public string textZh;
        public string textEn;
        public string expressionId;
        public Sprite expression;
    }

    [Serializable]
    public sealed class TutorialSegment
    {
        public string id;
        public List<TutorialLine> lines = new List<TutorialLine>();
    }

    [CreateAssetMenu(fileName = "TutorialCatalog", menuName = "Cooking/Tutorial Catalog")]
    public sealed class TutorialCatalog : ScriptableObject
    {
        public List<TutorialSegment> segments = new List<TutorialSegment>();

        public static string Pick(DiaryLanguage language, string zh, string en)
        {
            if (language == DiaryLanguage.En && !string.IsNullOrWhiteSpace(en))
                return en;

            return zh ?? "";
        }

        public TutorialSegment Find(string segmentId)
        {
            if (segments == null || string.IsNullOrEmpty(segmentId))
                return null;

            for (int i = 0; i < segments.Count; i++)
            {
                TutorialSegment segment = segments[i];
                if (segment != null && segment.id == segmentId)
                    return segment;
            }

            return null;
        }
    }

    public static class TutorialIds
    {
        public const string Night0Intro = "night0_intro";
        public const string Night0AfterHint = "night0_after_hint";
        public const string Night0PostForage = "night0_post_forage";
        public const string Day1Wakeup = "day1_wakeup";
        public const string Day1AfterRecipe = "day1_after_recipe";
        public const string Day1Fry = "day1_fry";
        public const string Day1Oil = "day1_oil";
        public const string Day1Boil = "day1_boil";
        public const string Day1Salt = "day1_salt";
        public const string Day1Scoop = "day1_scoop";
        public const string Day1FindPlate = "day1_find_plate";
        public const string Day1Plate = "day1_plate";
        public const string Day1Taste = "day1_taste";
        public const string Completed = "tutorial_completed";
    }
}
