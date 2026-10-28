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
        Collider2D boundary;

        void Awake()
        {
            if (body == null)
                body = GetComponent<Collider2D>();

            if (body == null)
                Debug.LogError("Player 上需要已经放好碰撞体。", this);

            MapBoundary marker = FindFirstObjectByType<MapBoundary>();
            if (marker == null)
                Debug.LogError("场景里需要有 MapBounds，并带 MapBoundary。");
            else
            {
                boundary = marker.GetComponent<Collider2D>();
                if (boundary == null)
                    Debug.LogError("MapBounds 上需要已经放好碰撞体。", marker);
            }
        }

        void Update()
        {
            Vector2 position = transform.position;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                var direction = Vector2.zero;
                if (keyboard.aKey.isPressed)
                    direction.x -= 1f;
                if (keyboard.dKey.isPressed)
                    direction.x += 1f;
                if (keyboard.sKey.isPressed)
                    direction.y -= 1f;
                if (keyboard.wKey.isPressed)
                    direction.y += 1f;

                if (direction.sqrMagnitude > 0.0001f)
                {
                    direction.Normalize();
                    position += direction * (moveSpeed * SpeedMultiplier * Time.deltaTime);
                }
            }

            position = ClampToMap(position);
            transform.position = new Vector3(position.x, position.y, 0f);
        }

        Vector2 ClampToMap(Vector2 position)
        {
            if (boundary == null)
                return position;

            Bounds area = boundary.bounds;
            float padX = Skin;
            float padY = Skin;
            if (body != null)
            {
                padX += body.bounds.extents.x;
                padY += body.bounds.extents.y;
            }

            float minX = area.min.x + padX;
            float maxX = area.max.x - padX;
            float minY = area.min.y + padY;
            float maxY = area.max.y - padY;

            position.x = minX > maxX ? area.center.x : Mathf.Clamp(position.x, minX, maxX);
            position.y = minY > maxY ? area.center.y : Mathf.Clamp(position.y, minY, maxY);
            return position;
        }
    }
}
