using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cooking.Data
{
    public enum DiaryKind
    {
        Past = 0,
        Present = 1,
        PresentFallback = 2,
        EndingNormal = 3,
        EndingGood = 4
    }

    public enum DiarySide
    {
        Left = 0,
        Right = 1
    }

    public enum DiaryLanguage
    {
        Zh = 0,
        En = 1
    }

    [Serializable]
    public struct DiaryParagraph
    {
        public int order;
        public DiarySide side;
        public string textZh;
        public string textEn;
    }

    [Serializable]
    public struct DiaryStringRow
    {
        public string id;
        public string textZh;
        public string textEn;
    }

    [Serializable]
    public sealed class DiaryEntry
    {
        public string id;
        public string recipeId;
        public int storyDay;
        public DiaryKind kind;
        public string titleId;
        public int unlockDay;
        public DayPhase unlockPhase;
        public int sortOrder;
        public bool inBook;
        public List<DiaryParagraph> paragraphs = new List<DiaryParagraph>();
    }

    [CreateAssetMenu(fileName = "DiaryCatalog", menuName = "Cooking/Diary Catalog")]
    public sealed class DiaryCatalog : ScriptableObject
    {
        public List<DiaryEntry> entries = new List<DiaryEntry>();
        public List<DiaryStringRow> strings = new List<DiaryStringRow>();
    }
}
