using UnityEngine;
#if UNITY_RENDER_PIPELINES_UNIVERSAL
using UnityEngine.Rendering.Universal;
#endif

/// Ultra-safe twinkle by changing SpriteRenderer.color.a only.
/// Works on default sprite shaders (Unlit/Lit). No material params needed.
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class StarTwinkleSimple : MonoBehaviour
{
    [Header("Twinkle")]
    [Range(0f, 1f)] public float baseAlpha = 0.28f;   // base opacity
    [Range(0f, 1f)] public float amplitude = 0.14f;   // amount of flicker
    [Min(0f)] public float speed = 0.8f;              // flicker speed
    public bool randomizeOnEnable = true;             // slight per-star variance

#if UNITY_RENDER_PIPELINES_UNIVERSAL
    [Header("Optional 2D Light for L/XL")]
    public Light2D optionalLight2D = null;            // for big stars (optional)
    public float lightBase = 1.2f;
    public float lightAmplitude = 0.6f;
#endif

    SpriteRenderer _sr;
    float _phase;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (randomizeOnEnable)
        {
            _phase = Random.value * 6.2831853f;               // 0..2π
            speed     *= Random.Range(0.85f, 1.20f);
            amplitude *= Random.Range(0.80f, 1.20f);
            baseAlpha *= Random.Range(0.90f, 1.10f);
        }
    }

    void LateUpdate()
    {
        if (!_sr) return;

        // 0..1 sine
        float t = (Mathf.Sin(Time.time * speed + _phase) + 1f) * 0.5f;
        float a = Mathf.Clamp01(baseAlpha + (t - 0.5f) * 2f * amplitude);

        var c = _sr.color; c.a = a;
        _sr.color = c;

#if UNITY_RENDER_PIPELINES_UNIVERSAL
        if (optionalLight2D)
        {
            float li = Mathf.Max(0f, lightBase + (t - 0.5f) * 2f * lightAmplitude);
            optionalLight2D.intensity = li;
        }
#endif
    }
}
