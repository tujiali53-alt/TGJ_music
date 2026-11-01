using UnityEngine;

namespace Cooking.Exploration
{
    public sealed class ExploreCameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector2 min;
        [SerializeField] Vector2 max;

        const float TargetAspect = 16f / 9f;

        Camera view;
        Collider2D boundary;

        void Awake()
        {
            view = GetComponent<Camera>();
            if (view == null)
                Debug.LogError("Main Camera 上需要已经放好摄像机。", this);

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

        void LateUpdate()
        {
            if (target == null)
                return;

            Vector3 position = target.position;
            if (boundary != null && view != null)
            {
                float halfHeight = view.orthographicSize;
                float halfWidth = halfHeight * TargetAspect;
                Bounds area = boundary.bounds;
                float minX = area.min.x + halfWidth;
                float maxX = area.max.x - halfWidth;
                float minY = area.min.y + halfHeight;
                float maxY = area.max.y - halfHeight;
                position.x = minX > maxX ? area.center.x : Mathf.Clamp(position.x, minX, maxX);
                position.y = minY > maxY ? area.center.y : Mathf.Clamp(position.y, minY, maxY);
            }
            else
            {
                position.x = Mathf.Clamp(position.x, min.x, max.x);
                position.y = Mathf.Clamp(position.y, min.y, max.y);
            }

            position.z = -10f;
            transform.position = position;
        }
    }
}
