using UnityEngine;
using UnityEngine.InputSystem;

namespace Cooking.Exploration
{
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 4f;
        [SerializeField] Collider2D body;

        public float SpeedMultiplier { get; set; } = 1f;

        const float Skin = 0.02f;
        readonly RaycastHit2D[] hits = new RaycastHit2D[8];
        ContactFilter2D boundsFilter;

        void Awake()
        {
            if (body == null)
                body = GetComponent<Collider2D>();

            if (body == null)
                Debug.LogError("Player 上需要已经放好碰撞体。", this);

            if (FindFirstObjectByType<MapBoundary>() == null)
                Debug.LogError("场景里需要有 MapBounds，并带 MapBoundary。");

            boundsFilter = new ContactFilter2D();
            boundsFilter.useTriggers = true;
            boundsFilter.useLayerMask = false;
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            var direction = Vector2.zero;
            if (keyboard.aKey.isPressed)
                direction.x -= 1f;
            if (keyboard.dKey.isPressed)
                direction.x += 1f;
            if (keyboard.sKey.isPressed)
                direction.y -= 1f;
            if (keyboard.wKey.isPressed)
                direction.y += 1f;

            if (direction.sqrMagnitude < 0.0001f)
                return;

            direction.Normalize();
            Vector2 delta = direction * (moveSpeed * SpeedMultiplier * Time.deltaTime);
            Vector2 position = transform.position;
            position += Move(new Vector2(delta.x, 0f));
            position += Move(new Vector2(0f, delta.y));
            transform.position = new Vector3(position.x, position.y, 0f);
        }

        Vector2 Move(Vector2 delta)
        {
            if (delta.sqrMagnitude < 0.0000001f || body == null)
                return delta;

            float distance = delta.magnitude;
            int count = body.Cast(delta / distance, boundsFilter, hits, distance);
            float allowed = distance;
            for (int i = 0; i < count; i++)
            {
                Collider2D other = hits[i].collider;
                if (other == null || other == body || other.GetComponent<MapBoundary>() == null)
                    continue;

                float gap = hits[i].distance - Skin;
                if (gap < allowed)
                    allowed = Mathf.Max(0f, gap);
            }

            return delta / distance * allowed;
        }
    }
}
