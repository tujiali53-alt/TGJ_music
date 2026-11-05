using Cooking.Core;
using Cooking.Data;
using Cooking.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace Cooking.UI
{
    public sealed class UiSpriteView : MonoBehaviour
    {
        [SerializeField] string id;
        [SerializeField] UiSpriteCatalog catalog;
        [SerializeField] DiaryLanguage language = DiaryLanguage.En;

        Image image;

        public string Id => id;
        public UiSpriteCatalog Catalog => catalog;

        void Awake()
        {
            image = GetComponent<Image>();
        }

        void OnEnable()
        {
            language = GameLanguage.Current;
            GameLanguage.Changed += OnLanguageChanged;
            Apply();
        }

        void OnDisable()
        {
            GameLanguage.Changed -= OnLanguageChanged;
        }

        public void SetLanguage(DiaryLanguage next)
        {
            language = next;
            Apply();
        }

        void OnLanguageChanged()
        {
            language = GameLanguage.Current;
            Apply();
        }

        void Apply()
        {
            if (image == null)
                image = GetComponent<Image>();

            if (image == null)
            {
                Debug.LogError("UiSpriteView 需要同一物体上的 Image。", this);
                return;
            }

            if (catalog == null)
            {
                Debug.LogError("UiSpriteView 缺少 UiSpriteCatalog。", this);
                return;
            }

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("UiSpriteView 的 id 不能为空。", this);
                return;
            }

            Sprite sprite = UiSprites.SpriteFor(catalog, id, language);
            if (sprite == null)
            {
                Debug.LogError("UiSpriteCatalog 里没有可用图：" + id, this);
                return;
            }

            image.sprite = sprite;
            image.color = Color.white;
        }
    }
}
