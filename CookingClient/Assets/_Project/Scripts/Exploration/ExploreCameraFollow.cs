using UnityEngine;

namespace Cooking.Exploration
{
    public sealed class ExploreCameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector2 min;
        [SerializeField] Vector2 max;

        void LateUpdate()
        {
            if (target == null)
                return;

            Vector3 position = target.position;
            position.x = Mathf.Clamp(position.x, min.x, max.x);
            position.y = Mathf.Clamp(position.y, min.y, max.y);
            position.z = -10f;
            transform.position = position;
        }
    }
}
