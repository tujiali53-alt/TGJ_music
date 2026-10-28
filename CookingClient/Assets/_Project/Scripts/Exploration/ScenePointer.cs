using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Cooking.Exploration
{
    public static class ScenePointer
    {
        static readonly List<RaycastResult> hits = new List<RaycastResult>();

        public static bool TryGetClick(out Vector2 world)
        {
            world = default;
            if (!TryGetPress(out Vector2 screen) || IsOverUi(screen))
                return false;

            Camera cam = Camera.main;
            if (cam == null)
                return false;

            var point = new Vector3(screen.x, screen.y, Mathf.Abs(cam.transform.position.z));
            world = cam.ScreenToWorldPoint(point);
            return true;
        }

        public static bool TryGetPress(out Vector2 screen)
        {
            screen = default;
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                screen = mouse.position.ReadValue();
                return true;
            }

            Pointer pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame)
            {
                screen = pointer.position.ReadValue();
                return true;
            }

            return false;
        }

        public static bool IsOverUi(Vector2 screen)
        {
            if (EventSystem.current == null)
                return false;

            var data = new PointerEventData(EventSystem.current);
            data.position = screen;
            hits.Clear();
            EventSystem.current.RaycastAll(data, hits);
            return hits.Count > 0;
        }
    }
}
