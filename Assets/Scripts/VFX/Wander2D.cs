using UnityEngine;

/// <summary>
/// Gentle 2D wandering motion using Perlin noise.
/// Attach to the root of the firefly prefab (not the Light child).
/// Keeps the object drifting around its initial anchor position.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("VFX/Wander 2D (gentle)")]
public class Wander2D : MonoBehaviour
{
    [Header("Motion Area (world units)")]
    [Tooltip("Max distance from the anchor position.")]
    [Min(0f)] public float radius = 0.15f;

    [Header("Animation")]
    [Tooltip("How fast the noise field is sampled.")]
    [Min(0f)] public float speed = 0.3f;

    [Tooltip("How quickly we steer towards the target (per second).")]
    [Min(0f)] public float lerpPerSecond = 6f;

    [Tooltip("Randomize starting phase so multiple instances don't sync.")]
    public bool randomizePhase = true;

    Vector3 _anchor;
    float _tx, _ty;

    void OnEnable()
    {
        _anchor = transform.position;
        if (randomizePhase)
        {
            _tx = Random.value * 1000f;
            _ty = Random.value * 1000f + 13.37f;
        }
    }

    void LateUpdate()
    {
        // Advance noise time
        float dt = Time.deltaTime * Mathf.Max(0.0001f, speed);
        _tx += dt;
        _ty += dt;

        // Smooth pseudo-random offset in [-1, 1]
        float nx = Mathf.PerlinNoise(_tx, 0f) * 2f - 1f;
        float ny = Mathf.PerlinNoise(0f, _ty) * 2f - 1f;

        // Target position within radius around the anchor
        Vector3 target = _anchor + new Vector3(nx, ny, 0f) * radius;

        // Ease towards target (frame-rate independent)
        float t = Mathf.Clamp01(Time.deltaTime * lerpPerSecond);
        transform.position = Vector3.Lerp(transform.position, target, t);
    }

    void OnDrawGizmosSelected()
    {
        // Visualize the drift radius
        Gizmos.color = new Color(1f, 0.92f, 0.2f, 0.35f);
        var center = Application.isPlaying ? _anchor : transform.position;
        Gizmos.DrawWireSphere(center, radius);
    }
}
