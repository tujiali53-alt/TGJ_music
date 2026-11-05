using UnityEngine;
using UnityEngine.InputSystem;

namespace Cooking.Exploration
{
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 4f;
        [SerializeField] Collider2D body;
        [SerializeField] Animator animator;

        public float SpeedMultiplier { get; set; } = 1f;

        const float Skin = 0.02f;
        const int FaceFront = 0;
        const int FaceBack = 1;
        const int FaceLeft = 2;
        const int FaceRight = 3;
        const string WorldLayerName = "World";

        static readonly int FacingId = Animator.StringToHash("Facing");
        static readonly int WalkingId = Animator.StringToHash("Walking");

        Collider2D boundary;
        ContactFilter2D worldFilter;
        readonly RaycastHit2D[] hits = new RaycastHit2D[8];
        int facing = FaceFront;
        bool worldFilterReady;

        void Awake()
        {
            if (body == null)
                body = GetComponent<Collider2D>();

            if (body == null)
                Debug.LogError("Player 上需要已经放好碰撞体。", this);

            if (animator == null)
                animator = GetComponent<Animator>();

            if (animator == null)
                Debug.LogError("Player 上需要已经放好 Animator。", this);

            MapBoundary marker = FindFirstObjectByType<MapBoundary>();
            if (marker == null)
                Debug.LogError("场景里需要有 MapBounds，并带 MapBoundary。");
            else
            {
                boundary = marker.GetComponent<Collider2D>();
                if (boundary == null)
                    Debug.LogError("MapBounds 上需要已经放好碰撞体。", marker);
            }

            int worldLayer = LayerMask.NameToLayer(WorldLayerName);
            if (worldLayer < 0)
                Debug.LogError("缺少 Layer：World。", this);
            else
            {
                worldFilter = new ContactFilter2D();
                worldFilter.SetLayerMask(1 << worldLayer);
                worldFilter.useTriggers = false;
                worldFilter.useLayerMask = true;
                worldFilterReady = true;
            }
        }

        void Update()
        {
            Vector2 position = transform.position;
            Keyboard keyboard = Keyboard.current;
            bool walking = false;
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
                    walking = true;
                    if (!Mathf.Approximately(Mathf.Abs(direction.x), Mathf.Abs(direction.y)))
                    {
                        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                            facing = direction.x > 0f ? FaceRight : FaceLeft;
                        else
                            facing = direction.y > 0f ? FaceBack : FaceFront;
                    }

                    direction.Normalize();
                    Vector2 delta = direction * (moveSpeed * SpeedMultiplier * Time.deltaTime);
                    // 先横后竖，贴着 World 层障碍物滑动，不能穿过。
                    position = Slide(position, new Vector2(delta.x, 0f));
                    transform.position = new Vector3(position.x, position.y, 0f);
                    position = Slide(position, new Vector2(0f, delta.y));
                }
            }

            if (animator != null)
            {
                animator.SetInteger(FacingId, facing);
                animator.SetBool(WalkingId, walking);
            }

            position = ClampToMap(position);
            transform.position = new Vector3(position.x, position.y, 0f);
        }

        Vector2 Slide(Vector2 origin, Vector2 delta)
        {
            if (body == null || !worldFilterReady || delta.sqrMagnitude < 0.0000001f)
                return origin + delta;

            float distance = delta.magnitude;
            Vector2 direction = delta / distance;
            int count = body.Cast(direction, worldFilter, hits, distance + Skin);
            float travel = distance;
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = hits[i];
                if (hit.collider == null || hit.collider == body)
                    continue;

                travel = Mathf.Min(travel, Mathf.Max(0f, hit.distance - Skin));
            }

            return origin + direction * travel;
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
