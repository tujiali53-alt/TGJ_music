using Cooking.Core;
using Cooking.Data;
using Cooking.UI;
using UnityEngine;

namespace Cooking.Exploration
{
    public sealed class InteractionPoint : MonoBehaviour
    {
        static readonly Color SpentTint = new Color(0.45f, 0.45f, 0.45f, 1f);
        const string SpentMessage = "该交互点已经没什么好看的了。";

        [SerializeField] string eventId;
        [SerializeField] NightEventDefinition definition;
        [SerializeField] Collider2D hitCollider;
        [SerializeField] Transform player;

        NightDialogPanel dialog;
        SpriteRenderer[] pictures;
        Color[] pictureColors;

        void Awake()
        {
            if (hitCollider == null)
                hitCollider = GetComponent<Collider2D>();

            if (hitCollider == null)
                Debug.LogError("交互点上需要已经放好碰撞体。", this);

            if (string.IsNullOrEmpty(eventId) && definition != null)
                eventId = definition.id;

            if (definition == null)
                Debug.LogError("交互点需要指定事件。", this);
            else if (!string.IsNullOrEmpty(eventId) && definition.id != eventId)
                Debug.LogError("交互点的 eventId 和事件资产不一致。", this);

            if (player == null)
            {
                GameObject found = GameObject.Find("Player");
                if (found != null)
                    player = found.transform;
            }

            if (player == null)
                Debug.LogError("场景里需要有 Player。", this);

            dialog = FindAnyObjectByType<NightDialogPanel>(FindObjectsInactive.Include);
            if (dialog == null)
                Debug.LogError("场景里需要有对话面板。", this);

            pictures = GetComponentsInChildren<SpriteRenderer>(true);
            pictureColors = new Color[pictures.Length];
            for (int i = 0; i < pictures.Length; i++)
                pictureColors[i] = pictures[i] != null ? pictures[i].color : Color.white;

            ApplyUnlock();
        }

        void OnEnable()
        {
            ApplyUnlock();
        }

        void Start()
        {
            ApplyUnlock();
        }

        void Update()
        {
            if (hitCollider == null || player == null || dialog == null || definition == null)
                return;

            if (!ScenePointer.TryGetClick(out Vector2 world))
                return;

            TryOpen(world);
        }

        void LateUpdate()
        {
            TintPictures(AllOptionsResolved());
        }

        public void TryOpen(Vector2 world)
        {
            if (hitCollider == null || player == null || dialog == null || definition == null)
                return;

            if (dialog.IsOpen)
                return;

            NightDicePanel dice = dialog.GetComponent<NightDicePanel>();
            if (dice != null && dice.IsOpen)
                return;

            if (!hitCollider.OverlapPoint(world))
                return;

            if (!PlayerIsInside())
                return;

            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null || root.Session.Phase != DayPhase.Night)
                return;

            if (root.Session.DayIndex < definition.unlockNight)
                return;

            if (AllOptionsResolved())
            {
                NightLogView.Append(NightLogKind.OptionBlocked, SpentMessage);
                return;
            }

            dialog.OpenIntro(definition, NightLayoutSelector.SuccessFaceDelta);
        }

        bool AllOptionsResolved()
        {
            if (definition == null || definition.options == null || definition.options.Count == 0)
                return false;

            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null || root.Session.ResolvedOptionIdsTonight == null)
                return false;

            var done = root.Session.ResolvedOptionIdsTonight;
            for (int i = 0; i < definition.options.Count; i++)
            {
                string id = definition.options[i].optionId;
                if (string.IsNullOrEmpty(id) || !done.Contains(id))
                    return false;
            }

            return true;
        }

        void TintPictures(bool spent)
        {
            if (pictures == null)
                return;

            for (int i = 0; i < pictures.Length; i++)
            {
                SpriteRenderer picture = pictures[i];
                if (picture == null)
                    continue;

                Color source = pictureColors != null && i < pictureColors.Length
                    ? pictureColors[i]
                    : Color.white;
                picture.color = spent
                    ? new Color(SpentTint.r, SpentTint.g, SpentTint.b, source.a)
                    : source;
            }
        }

        bool PlayerIsInside()
        {
            if (hitCollider.OverlapPoint(player.position))
                return true;

            SpriteRenderer body = player.GetComponent<SpriteRenderer>();
            if (body == null || body.sprite == null)
                return false;

            Bounds area = hitCollider.bounds;
            Bounds sprite = body.bounds;
            return sprite.min.x <= area.max.x && sprite.max.x >= area.min.x
                && sprite.min.y <= area.max.y && sprite.max.y >= area.min.y;
        }

        void ApplyUnlock()
        {
            GameRoot root = GameRoot.Instance;
            if (definition == null || root == null || root.Session == null)
                return;

            if (root.Session.DayIndex < definition.unlockNight)
                gameObject.SetActive(false);
        }
    }
}
