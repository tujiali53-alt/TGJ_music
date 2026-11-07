using System.Globalization;
using Cooking.Core;
using Cooking.Data;
using Cooking.UI;
using UnityEngine;

namespace Cooking.Exploration
{
    public sealed class LighterPoint : MonoBehaviour
    {
        [SerializeField] Collider2D hitCollider;
        [SerializeField] Transform player;
        [SerializeField] float flySpeed = 6f;
        [SerializeField] float absorbDistance = 0.2f;

        Vector3 home;
        bool flying;

        void Awake()
        {
            home = transform.position;

            if (hitCollider == null)
                hitCollider = GetComponent<Collider2D>();

            if (hitCollider == null)
                Debug.LogError("光点上需要已经放好碰撞体。", this);

            if (player == null)
            {
                GameObject found = GameObject.Find("Player");
                if (found != null)
                    player = found.transform;
            }

            if (player == null)
                Debug.LogError("场景里需要有 Player。", this);
        }

        void Update()
        {
            if (player == null || hitCollider == null)
                return;

            if (!flying)
            {
                if (TouchesPlayer())
                    flying = true;
                return;
            }

            Vector3 target = PlayerCenter();
            target.z = transform.position.z;
            transform.position = Vector3.MoveTowards(transform.position, target, flySpeed * Time.deltaTime);
            if (Vector2.Distance(transform.position, target) > absorbDistance)
                return;

            Absorb();
        }

        public void Restore()
        {
            flying = false;
            transform.position = home;
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
        }

        bool TouchesPlayer()
        {
            if (!hitCollider.enabled || !hitCollider.gameObject.activeInHierarchy)
                return false;

            if (hitCollider.OverlapPoint(player.position))
                return true;

            Collider2D playerBody = player.GetComponent<Collider2D>();
            if (playerBody != null && playerBody.enabled)
            {
                ColliderDistance2D gap = hitCollider.Distance(playerBody);
                if (gap.isValid && (gap.isOverlapped || gap.distance <= 0.05f))
                    return true;
            }

            SpriteRenderer body = player.GetComponent<SpriteRenderer>();
            if (body == null || body.sprite == null)
                return false;

            Bounds area = hitCollider.bounds;
            Bounds sprite = body.bounds;
            return sprite.min.x <= area.max.x && sprite.max.x >= area.min.x
                && sprite.min.y <= area.max.y && sprite.max.y >= area.min.y;
        }

        Vector3 PlayerCenter()
        {
            Collider2D playerBody = player.GetComponent<Collider2D>();
            if (playerBody != null && playerBody.enabled)
                return playerBody.bounds.center;

            SpriteRenderer body = player.GetComponent<SpriteRenderer>();
            if (body != null && body.sprite != null)
                return body.bounds.center;

            return player.position;
        }

        void Absorb()
        {
            GameRoot root = GameRoot.Instance;
            if (root == null || root.Session == null)
            {
                Debug.LogError("吸收光点需要已经放好的 GameRoot。", this);
                gameObject.SetActive(false);
                return;
            }

            root.Session.LightsAbsorbedTonight += 1;
            int count = root.Session.LightsAbsorbedTonight;
            NightLogView.Append(NightLogKind.LightAbsorb, "log.light_absorb", count.ToString(CultureInfo.InvariantCulture));

            PlayerLight light = FindAnyObjectByType<PlayerLight>();
            if (light != null)
            {
                string radius = light.RadiusFor(count).ToString("0.00", CultureInfo.InvariantCulture);
                NightLogView.Append(NightLogKind.RadiusChanged, "log.radius", radius);
            }

            flying = false;
            gameObject.SetActive(false);
        }
    }
}
