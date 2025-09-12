using UnityEngine;

/// <summary>
/// Smooth elliptical bobbing around the spawn point (pixel-art friendly).
/// Make the firefly drift slowly in X/Y with optional random phase.
/// </summary>
public class FloatBob2D : MonoBehaviour
{
    [Tooltip("World-units amplitude (X,Y). Example: (0.12, 0.28)")]
    public Vector2 amplitude = new Vector2(0.12f, 0.28f);

    [Tooltip("Oscillation speed (cycles per second). Lower = slower.")]
    public float frequency = 0.35f;

    [Tooltip("Adds a bit of Perlin noise wobble so it feels alive.")]
    public float noiseAmount = 0.02f;

    [Tooltip("Randomize initial phase so instances don't sync.")]
    public bool randomizePhase = true;

    Vector3 _origin;
    float _phaseX;
    float _phaseY;

    void Awake()
    {
        _origin = transform.position;
        if (randomizePhase)
        {
            _phaseX = Random.Range(0f, Mathf.PI * 2f);
            _phaseY = Random.Range(0f, Mathf.PI * 2f);
        }
    }

    void Update()
    {
        float t = Time.time * frequency;
        float x = Mathf.Sin(t + _phaseX) * amplitude.x;
        float y = Mathf.Sin(t * 0.85f + _phaseY) * amplitude.y;

        // tiny noise so it isn't perfectly periodic
        x += (Mathf.PerlinNoise(t, 0.123f) - 0.5f) * 2f * noiseAmount;
        y += (Mathf.PerlinNoise(0.456f, t) - 0.5f) * 2f * noiseAmount;

        transform.position = _origin + new Vector3(x, y, 0f);
    }
}
