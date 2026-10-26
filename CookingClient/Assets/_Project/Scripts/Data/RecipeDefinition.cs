using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cooking.Data
{
    [Serializable]
    public struct RecipeIngredient
    {
        public string ingredientId;
        public CutShape shape;
        public CookMethod cookMethod;
        public int count;
    }

    [Serializable]
    public struct RecipeSeasoning
    {
        public string seasoningId;
        public string targetIngredientId;
        public int count;
    }

    [Serializable]
    public struct RecipeStep
    {
        public int order;
        public string hintText;
        public CookAction action;
        public string ingredientId;
        public CookMethod cookMethod;
        public string seasoningId;
    }

    [CreateAssetMenu(fileName = "Recipe", menuName = "Cooking/Recipe")]
    public sealed class RecipeDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public int unlockDay = 1;
        public Sprite finishedSprite;
        public List<RecipeIngredient> ingredients = new List<RecipeIngredient>();
        public List<RecipeSeasoning> seasonings = new List<RecipeSeasoning>();
        public List<RecipeStep> steps = new List<RecipeStep>();
    }
}
