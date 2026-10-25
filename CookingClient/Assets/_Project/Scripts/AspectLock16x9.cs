using UnityEngine;

/// <summary>
/// Keeps the camera viewport at 16:9. Wider or taller windows get black bars
/// from Letterbox Camera instead of stretching the layout.
/// </summary>
[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public class AspectLock16x9 : MonoBehaviour
{
    public float targetAspect = 16f / 9f;

    Camera cam;
    int lastWidth = -1;
    int lastHeight = -1;

    void Awake()
    {
        cam = GetComponent<Camera>();
        Apply();
    }

    void Update()
    {
        if (Screen.width == lastWidth && Screen.height == lastHeight)
            return;

        Apply();
    }

    void Apply()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;

        float windowAspect = (float)lastWidth / lastHeight;
        float scale = windowAspect / targetAspect;
        Rect rect = cam.rect;

        if (scale < 1f)
        {
            rect.width = 1f;
            rect.height = scale;
            rect.x = 0f;
            rect.y = (1f - scale) / 2f;
        }
        else
        {
            float width = 1f / scale;
            rect.width = width;
            rect.height = 1f;
            rect.x = (1f - width) / 2f;
            rect.y = 0f;
        }

        cam.rect = rect;
    }
}
