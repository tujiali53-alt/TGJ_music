using Cooking.Core;
using Cooking.Data;
using Cooking.Runtime;
using UnityEngine;

namespace Cooking.UI
{
    public sealed class DiaryNotice : MonoBehaviour
    {
        [SerializeField] GameObject redPoint;

        DiaryCatalog catalog;

        void Awake()
        {
            Resolve();
            if (redPoint == null)
                Debug.LogError("需要 InteractionPanel/RedPoint。", this);
            if (catalog == null)
                Debug.LogError("DiaryPanel 上需要 DiaryPanelView，并且绑上 DiaryCatalog。", this);
        }

        void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            Resolve();
            bool unread = false;
            GameRoot root = GameRoot.Instance;
            if (catalog != null && root != null && root.Session != null)
                unread = DiaryBook.HasUnread(DiaryBook.Visible(catalog, root.Session), root.Session.DiarySeenIds);

            if (redPoint != null)
                redPoint.SetActive(unread);
        }

        void Resolve()
        {
            if (redPoint == null)
            {
                Transform found = transform.Find("RedPoint");
                if (found != null)
                    redPoint = found.gameObject;
            }

            if (catalog != null)
                return;

            Transform canvas = transform.parent;
            if (canvas == null)
                return;

            Transform diary = canvas.Find("DiaryPanel");
            if (diary == null)
                return;

            DiaryPanelView view = diary.GetComponent<DiaryPanelView>();
            if (view == null)
                return;

            catalog = view.Catalog;
        }
    }
}
