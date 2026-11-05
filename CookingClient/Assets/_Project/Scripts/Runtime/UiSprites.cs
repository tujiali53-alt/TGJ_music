using Cooking.Data;
using UnityEngine;

namespace Cooking.Runtime
{
    public static class UiSprites
    {
        public static Sprite SpriteFor(UiSpriteCatalog catalog, string id, DiaryLanguage language)
        {
            if (catalog == null || string.IsNullOrEmpty(id))
                return null;

            if (!catalog.TryGet(id, out UiSpriteRow row))
                return null;

            if (language == DiaryLanguage.En && row.spriteEn != null)
                return row.spriteEn;

            return row.spriteZh;
        }
    }
}
