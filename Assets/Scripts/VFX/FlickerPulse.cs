using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Smooth "breathing" flicker for both the SpriteRenderer alpha and a 2D Light.
/// Designed for pixel art: no hard strobe, gentle eased pulse.
/// </summary>
public class FlickerPulse : MonoBehaviour
{
    [Header("Targets")]
    public SpriteRenderer sprite;         // drag the firefly SpriteRenderer
    public Light2D light2D;               // drag the child Light 2D (Point/Spot)

    [Header("Sprite Alpha")]
    [Range(0f, 1f)] public float baseAlpha = 0.20f;
    [Range(0f, 1f)] public float alphaAmplitude = 0.35f;

    [Header("Light Intensity")]
    public float baseIntensity = 1.4f;
    public float intensityAmplitude = 0.8f;

    [Header("Pulse Shape")]
    [Tooltip("Pulses per second. Lower = slower breathing.")]
    public float speed = 0.7f;
    [Tooltip("Extra subtle noise to avoid being perfectly periodic.")]
    public float noise = 0.15f;
    [Tooltip("Response smoothing. 0 = instant, 0.2–0.3 = very smooth.")]
    [Range(0f, 0.5f)] public float smoothTime = 0.15f;

    float _phase;
    float _current;   // smoothed 0..1 value
    float _vel;       // SmoothDamp velocity

    void Awake()
    {
        if (!sprite) sprite = GetComponent<SpriteRenderer>();
        if (!light2D) light2D = GetComponentInChildren<Light2D>();
        _phase = Random.value * Mathf.PI * 2f; // desync between instances
    }

    void Update()
    {
        float t = Time.time * speed + _phase;

        // 0..1 sine (ease-in-out-ish)
        float s = 0.5f + 0.5f * Mathf.Sin(t);

        // soft noise
        s += (Mathf.PerlinNoise(t, 0.37f) - 0.5f) * noise;
        s = Mathf.Clamp01(s);

        // smooth to avoid strobing
        _current = Mathf.SmoothDamp(_current, s, ref _vel, smoothTime);

        // drive sprite alpha
        if (sprite)
        {
            var c = sprite.color;
            float a = Mathf.Clamp01(baseAlpha + alphaAmplitude * _current);
            c.a = a;
            sprite.color = c;
        }

        // drive 2D light intensity (this is what creates the big glow with Bloom)
        if (light2D)
        {
            light2D.intensity = Mathf.Max(0f, baseIntensity + intensityAmplitude * _current);
        }
    }
}
