using Cooking.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cooking.Exploration
{
    public sealed class BedSleep : MonoBehaviour
    {
        Collider2D hitCollider;

        void Awake()
        {
            hitCollider = GetComponent<Collider2D>();
            if (hitCollider == null)
                Debug.LogError("bed 上需要已经放好碰撞体。", this);
        }

        void Update()
        {
            if (hitCollider == null || !WasClicked(out Vector2 world))
                return;

            if (!hitCollider.OverlapPoint(world))
                return;

            GameRoot root = GameRoot.Instance;
            if (root == null)
                return;

            root.SleepAndEnterDay();
        }

        static bool WasClicked(out Vector2 world)
        {
            world = default;
            if (!TryGetPointerPress(out Vector2 screenPos))
                return false;

            Camera cam = Camera.main;
            if (cam == null)
                return false;

            var screen = new Vector3(screenPos.x, screenPos.y, Mathf.Abs(cam.transform.position.z));
            world = cam.ScreenToWorldPoint(screen);
            return true;
        }

        static bool TryGetPointerPress(out Vector2 screenPos)
        {
            screenPos = default;
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                screenPos = mouse.position.ReadValue();
                return true;
            }

            Pointer pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame)
            {
                screenPos = pointer.position.ReadValue();
                return true;
            }

            return false;
        }
    }
}
