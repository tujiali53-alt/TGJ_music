using UnityEngine;

namespace Cooking.Exploration
{
    /// <summary>
    /// 事先挂在 Light_* 上，对 SpriteRenderer 做呼吸闪烁。不在播放时 AddComponent。
    /// </summary>
    public sealed class LighterBlink : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] float pulseSpeed = 3f;
        [SerializeField] float minAlpha = 0.35f;
        [SerializeField] float maxAlpha = 1f;
        [SerializeField] float scalePulse = 0.12f;

        Color baseColor = Color.white;
        Vector3 baseScale = Vector3.one;
        float phase;

        Transform pulseTarget;

        void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

            if (spriteRenderer == null)
            {
                Debug.LogError("光点闪烁需要已经放好 SpriteRenderer。", this);
                enabled = false;
                return;
            }

            // 图在子物体时只闪子物体，避免连带放大碰撞体。
            pulseTarget = spriteRenderer.transform;
            baseColor = spriteRenderer.color;
            baseScale = pulseTarget.localScale;
            phase = Random.Range(0f, Mathf.PI * 2f);
        }

        void OnEnable()
        {
            if (spriteRenderer != null)
                baseColor = spriteRenderer.color;
            if (pulseTarget != null)
                baseScale = pulseTarget.localScale;
        }

        void Update()
        {
            if (spriteRenderer == null || pulseTarget == null)
                return;

            phase += pulseSpeed * Time.deltaTime;
            float t = (Mathf.Sin(phase) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
            float scale = 1f + scalePulse * (t * 2f - 1f);

            Color c = baseColor;
            c.a = baseColor.a * alpha;
            spriteRenderer.color = c;
            pulseTarget.localScale = baseScale * scale;
        }
    }
}
