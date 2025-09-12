using UnityEngine;
#if UNITY_RENDER_PIPELINES_UNIVERSAL
using UnityEngine.Rendering.Universal;
#endif

/// <summary>
/// Safe twinkle for pixel stars.
/// - Uses MaterialPropertyBlock to change SpriteRenderer color alpha (no material instances).
/// - Optional Light2D pulsing if present.
/// - Random phase/speed to avoid uniform blinking.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class StarTwinkle2D : MonoBehaviour
{
    [Header("Target (leave empty to auto-detect)")]
    [SerializeField] private SpriteRenderer target;

#if UNITY_RENDER_PIPELINES_UNIVERSAL
    [Tooltip("Optional 2D light to pulse with alpha")]
    [SerializeField] private Light2D light2D = null;
    [Min(0f)] public float lightBase = 1.2f;
    [Min(0f)] public float lightAmplitude = 0.6f;
#endif

    [Header("Twinkle")]
    [Range(0f, 1f)] public float baseAlpha = 0.55f;
    [Range(0f, 1f)] public float amplitude = 0.25f;
    [Min(0f)] public float speed = 1.0f;
    public bool randomizeOnEnable = true;

    MaterialPropertyBlock _mpb;
    float _phase;

    void Awake()
    {
        if (!target) target = GetComponent<SpriteRenderer>();
#if UNITY_RENDER_PIPELINES_UNIVERSAL
        if (!light2D) light2D = GetComponentInChildren<Light2D>();
#endif
        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        if (randomizeOnEnable)
        {
            _phase = Random.value * Mathf.PI * 2f;
            speed *= Random.Range(0.85f, 1.25f);
            amplitude *= Random.Range(0.8f, 1.2f);
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        // 0..1 smooth oscillation
        float t = Mathf.Sin(Time.time * speed + _phase) * 0.5f + 0.5f;
        float a = Mathf.Clamp01(baseAlpha + (t - 0.5f) * 2f * amplitude);

        // Apply alpha via MPB (no material instance)
        target.GetPropertyBlock(_mpb);
        Color c = target.color;
        c.a = a;
        _mpb.SetColor("_Color", c);
        target.SetPropertyBlock(_mpb);

#if UNITY_RENDER_PIPELINES_UNIVERSAL
        if (light2D)
        {
            float li = Mathf.Max(0f, lightBase + (t - 0.5f) * 2f * lightAmplitude);
            light2D.intensity = li;
        }
#endif
    }
}
