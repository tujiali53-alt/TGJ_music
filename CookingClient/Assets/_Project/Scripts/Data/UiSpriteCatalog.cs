using System;
using UnityEngine;

namespace Cooking.Data
{
    [Serializable]
    public struct UiSpriteRow
    {
        public string id;
        public Sprite spriteZh;
        public Sprite spriteEn;
    }

    [CreateAssetMenu(menuName = "Cooking/Ui Sprite Catalog", fileName = "UiSpriteCatalog")]
    public sealed class UiSpriteCatalog : ScriptableObject
    {
        public UiSpriteRow[] rows = Array.Empty<UiSpriteRow>();

        public bool TryGet(string id, out UiSpriteRow row)
        {
            row = default;
            if (rows == null || string.IsNullOrEmpty(id))
                return false;

            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i].id == id)
                {
                    row = rows[i];
                    return true;
                }
            }

            return false;
        }
    }
}
