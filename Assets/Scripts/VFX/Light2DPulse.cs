using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Light2DPulse : MonoBehaviour
{
    [Tooltip("If left empty, will use the Light2D on this GameObject.")]
    public Light2D target;

    [Header("Pulse")]
    public float baseIntensity = 1.6f; // baseline
    public float amplitude     = 0.4f; // +/- around baseline
    public float speed         = 2.0f; // Hz-like speed

    float phase;

    void Reset() { target = GetComponent<Light2D>(); }
    void Awake() { if (!target) target = GetComponent<Light2D>(); phase = Random.value * 6.28318f; }

    void LateUpdate()
    {
        if (!target) return;
        target.intensity = Mathf.Max(0f, baseIntensity + Mathf.Sin(Time.time * speed + phase) * amplitude);
    }
}
