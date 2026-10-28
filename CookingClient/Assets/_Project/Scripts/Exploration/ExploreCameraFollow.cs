using UnityEngine;

namespace Cooking.Exploration
{
    public sealed class ExploreCameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector2 min;
        [SerializeField] Vector2 max;

        Collider2D boundary;

        void Awake()
        {
            MapBoundary marker = FindFirstObjectByType<MapBoundary>();
            if (marker != null)
                boundary = marker.GetComponent<Collider2D>();
        }

        void LateUpdate()
        {
            if (target == null)
                return;

            Vector2 low = min;
            Vector2 high = max;
            if (boundary != null)
            {
                low = boundary.bounds.min;
                high = boundary.bounds.max;
            }

            Vector3 position = target.position;
            position.x = Mathf.Clamp(position.x, low.x, high.x);
            position.y = Mathf.Clamp(position.y, low.y, high.y);
            position.z = -10f;
            transform.position = position;
        }
    }
}
