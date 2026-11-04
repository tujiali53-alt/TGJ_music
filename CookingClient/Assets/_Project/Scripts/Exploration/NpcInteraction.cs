using Cooking.Core;
using Cooking.Data;
using Cooking.UI;
using UnityEngine;

namespace Cooking.Exploration
{
    public sealed class NpcInteraction : MonoBehaviour
    {
        static readonly Color SpentTint = new Color(0.45f, 0.45f, 0.45f, 1f);
        const string SpentMessage = "该交互点已经没什么好看的了。";

        [SerializeField] string npcId;
        [SerializeField] Collider2D hitCollider;
        [SerializeField] Transform player;

        NpcDialogPanel dialog;
        NightDialogPanel nightDialog;
        SpriteRenderer[] pictures;
        Color[] pictureColors;

        void Awake()
        {
            if (hitCollider == null)
                hitCollider = GetComponent<Collider2D>();

            if (hitCollider == null)
                Debug.LogError("NPCInteraction 上需要已经放好碰撞体。", this);

            if (string.IsNullOrEmpty(npcId))
                Debug.LogError("NPCInteraction 需要 npcId。", this);

            if (player == null)
            {
                GameObject found = GameObject.Find("Player");
                if (found != null)
                    player = found.transform;
            }

            if (player == null)
                Debug.LogError("场景里需要有 Player。", this);

            dialog = FindAnyObjectByType<NpcDialogPanel>(FindObjectsInactive.Include);
            if (dialog == null)
                Debug.LogError("场景里需要有 NPC 对话面板。", this);

            nightDialog = FindAnyObjectByType<NightDialogPanel>(FindObjectsInactive.Include);

            pictures = GetComponentsInChildren<SpriteRenderer>(true);
            pictureColors = new Color[pictures.Length];
            for (int i = 0; i < pictures.Length; i++)
                pictureColors[i] = pictures[i] != null ? pictures[i].color : Color.white;
        }

        void Update()
        {
            if (hitCollider == null || player == null || dialog == null)
                return;

            if (!ScenePointer.TryGetClick(out Vector2 world))
                return;

            TryOpen(world);
        }

        void LateUpdate()
        {
            TintPictures(dialog != null && dialog.IsResolved(npcId));
        }

        public void TryOpen(Vector2 world)
        {
            if (hitCollider == null || player == null || dialog == null)
                return;

            if (dialog.IsOpen)
                return;

            if (nightDialog != null && nightDialog.IsOpen)
                return;

            NightDicePanel dice = nightDialog != null ? nightDialog.GetComponent<NightDicePanel>() : null;
            if (dice != null && dice.IsOpen)
                return;

            if (!hitCollider.OverlapPoint(world))
                return;

            if (!PlayerIsInside())
                return;

            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null || root.Session.Phase != DayPhase.Night)
                return;

            NpcCharacter character = dialog.FindCharacter(npcId);
            if (character == null)
                return;

            if (character.phase != DayPhase.Night || root.Session.DayIndex != character.unlockDay)
                return;

            if (dialog.IsResolved(npcId))
            {
                NightLogView.Append(NightLogKind.OptionBlocked, SpentMessage);
                return;
            }

            dialog.Open(npcId);
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
    }
}
