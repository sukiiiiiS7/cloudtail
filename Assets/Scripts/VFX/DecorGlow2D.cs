using UnityEngine;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class DecorGlow2D : MonoBehaviour
{
    [Header("Light")]
    public bool autoCreate = true;
    public Light2D light2D;

    public Color color = Color.white;
    [Range(0f, 5f)] public float intensity = 1.2f;

    [Tooltip("Outer radius in world units (URP 2D Point Light).")]
    [Min(0f)] public float outerRadius = 1.0f;
    [Min(0f)] public float innerRadius = 0.1f;

    [Tooltip("Local offset of the glow center.")]
    public Vector2 offset = Vector2.zero;

    [Header("Flicker")]
    public bool flicker = true;
    [Range(0f, 1f)] public float flickerAmount = 0.15f;
    [Range(0.1f, 10f)] public float flickerSpeed = 2f;

    float _baseIntensity;

    void Awake()
    {
        if (!light2D && autoCreate)
        {
            var go = new GameObject("Glow2D");
            go.transform.SetParent(transform, false);
            light2D = go.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Point;
        }
        if (light2D)
        {
            light2D.transform.localPosition = new Vector3(offset.x, offset.y, 0);
            light2D.color = color;
            light2D.intensity = intensity;
            light2D.pointLightInnerRadius = innerRadius;
            light2D.pointLightOuterRadius = outerRadius;
            _baseIntensity = intensity;
        }
    }

    void Update()
    {
        if (flicker && light2D)
        {
            float t = Time.time * flickerSpeed;
            float delta = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * flickerAmount;
            light2D.intensity = Mathf.Max(0f, _baseIntensity * (1f + delta));
        }
    }
}
