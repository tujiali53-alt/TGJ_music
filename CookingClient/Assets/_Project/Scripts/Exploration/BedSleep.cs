using Cooking.Core;
using Cooking.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Cooking.Exploration
{
    public sealed class BedSleep : MonoBehaviour
    {
        [SerializeField] Collider2D hitCollider;
        [SerializeField] Transform player;
        [SerializeField] GameObject bedPanel;
        [SerializeField] Button confirmButton;
        [SerializeField] Button cancelButton;

        void Awake()
        {
            if (hitCollider == null)
                hitCollider = GetComponent<Collider2D>();

            if (hitCollider == null)
                Debug.LogError("bed 上需要已经放好碰撞体。", this);

            if (player == null)
            {
                GameObject found = GameObject.Find("Player");
                if (found != null)
                    player = found.transform;
            }

            if (player == null)
                Debug.LogError("场景里需要有 Player。", this);

            if (bedPanel == null)
                bedPanel = FindBedPanel();

            if (bedPanel == null)
                Debug.LogError("Canvas 下需要有 bedPanel。", this);
            else
            {
                if (confirmButton == null)
                    confirmButton = FindChildButton(bedPanel.transform, "ComfirmButton");

                if (cancelButton == null)
                    cancelButton = FindChildButton(bedPanel.transform, "CancelButton");
            }

            if (confirmButton == null)
                Debug.LogError("bedPanel 下需要有 ComfirmButton，并且带 Button。", this);
            else
                confirmButton.onClick.AddListener(ConfirmSleep);

            if (cancelButton == null)
                Debug.LogError("bedPanel 下需要有 CancelButton，并且带 Button。", this);
            else
                cancelButton.onClick.AddListener(ClosePanel);

            if (bedPanel != null)
                bedPanel.SetActive(false);
        }

        void Update()
        {
            if (hitCollider == null || player == null || bedPanel == null)
                return;

            if (bedPanel.activeSelf)
                return;

            if (!ScenePointer.TryGetClick(out Vector2 world))
                return;

            if (!hitCollider.OverlapPoint(world))
                return;

            if (!PlayerAtBed())
                return;

            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null || root.Flow == null)
                return;

            if (root.Flow.IsLoading || root.Session.Phase != DayPhase.Night)
                return;

            bedPanel.SetActive(true);
        }

        bool PlayerAtBed()
        {
            if (hitCollider.OverlapPoint(player.position))
                return true;

            Collider2D body = player.GetComponent<Collider2D>();
            if (body != null && body.bounds.Intersects(hitCollider.bounds))
                return true;

            SpriteRenderer sprite = player.GetComponent<SpriteRenderer>();
            if (sprite == null)
                return false;

            Bounds area = hitCollider.bounds;
            Bounds drawn = sprite.bounds;
            return drawn.min.x <= area.max.x && drawn.max.x >= area.min.x
                && drawn.min.y <= area.max.y && drawn.max.y >= area.min.y;
        }

        void ConfirmSleep()
        {
            GameRoot root = GameRoot.Instance;
            if (root == null)
                return;

            root.SleepAndEnterDay();
        }

        void ClosePanel()
        {
            if (bedPanel != null)
                bedPanel.SetActive(false);
        }

        static GameObject FindBedPanel()
        {
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas == null)
                return null;

            Transform found = canvas.transform.Find("bedPanel");
            return found != null ? found.gameObject : null;
        }

        static Button FindChildButton(Transform parent, string childName)
        {
            Transform found = parent.Find(childName);
            if (found == null)
                return null;

            return found.GetComponent<Button>();
        }
    }
}
