using UnityEngine;
using UnityEngine.InputSystem;

namespace Cooking.Exploration
{
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 4f;

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
            Vector3 position = transform.position;
            position.x += direction.x * moveSpeed * Time.deltaTime;
            position.y += direction.y * moveSpeed * Time.deltaTime;
            position.z = 0f;
            transform.position = position;
        }
    }
}
