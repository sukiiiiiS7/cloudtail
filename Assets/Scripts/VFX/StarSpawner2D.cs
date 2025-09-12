using System.Linq;
using UnityEngine;

/// <summary>
/// Runtime/PlayMode starfield spawner for background.
/// - No drifting by default; each star gets a random initial alpha.
/// - (Optional) add a very subtle twinkle per-star by auto-attaching StarTwinkle2D.
/// - Heavily weight S/M stars; lightly weight L/XL via the inspector.
/// Attach this to the Background sprite (or any GameObject),
/// assign your star prefabs + the background SpriteRenderer,
/// then press Play or use the context menu "Respawn".
/// </summary>
[ExecuteAlways]
public class StarSpawner2D : MonoBehaviour
{
    [System.Serializable]
    public class PrefabEntry
    {
        [Tooltip("Star prefab to spawn")]
        public GameObject prefab;

        [Tooltip("Relative weight for weighted random pick (S/M higher, L/XL lower).")]
        [Range(0f, 100f)] public float weight = 1f;

        [Header("Optional per-star scale randomization")]
        [Min(0f)] public float minScale = 1f;
        [Min(0f)] public float maxScale = 1f;
    }

    [Header("Prefabs with Weights (set S/M big; L/XL small)")]
    public PrefabEntry[] prefabs;

    [Header("Parent for spawned stars (defaults to this transform)")]
    public Transform starsParent;

    [Header("Spawn Area (pick one)")]
    [Tooltip("Background SpriteRenderer – easiest; we use its world-space bounds")]
    public SpriteRenderer areaSprite;
    [Tooltip("Or a BoxCollider2D volume to spawn inside")]
    public BoxCollider2D areaBox;
    [Tooltip("If both assigned, prefer Sprite bounds")]
    public bool useSpriteBounds = true;

    [Header("Count & Spacing")]
    [Min(0)] public int count = 60;
    [Tooltip("Min world distance between stars")]
    [Min(0f)] public float minDistance = 0.4f;

    [Header("Alpha randomization (no drifting)")]
    [Tooltip("Per-star initial alpha (transparency)")]
    public Vector2 alphaRange = new Vector2(0.35f, 0.80f);

    [Header("Optional subtle twinkle")]
    public bool addTwinkle = true;
    [Range(0f, 1f)] public float twinkleProbability = 0.6f;
    public Vector2 twinkleAmplitudeRange = new Vector2(0.10f, 0.20f);
    public Vector2 twinkleSpeedRange = new Vector2(0.40f, 0.80f);

    void OnEnable()
    {
        // Avoid respawning in Edit time unless asked; at Play, spawn automatically.
        if (Application.isPlaying) Respawn();
    }

    [ContextMenu("Respawn")]
    public void Respawn()
    {
        if (!starsParent) starsParent = transform;

        // Clear previous children
        for (int i = starsParent.childCount - 1; i >= 0; i--)
        {
            var child = starsParent.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }

        if (prefabs == null || prefabs.Length == 0) return;

        // Prepare weights
        float totalW = prefabs.Sum(p => Mathf.Max(0f, p.weight));
        if (totalW <= 0f) return;

        Bounds spawnBounds = GetBounds();

        // Guarded placement with minimal spacing
        int spawned = 0;
        int safety = 0;
        Vector3[] placed = new Vector3[count];

        while (spawned < count && safety < count * 50)
        {
            safety++;

            Vector3 pos = new Vector3(
                Random.Range(spawnBounds.min.x, spawnBounds.max.x),
                Random.Range(spawnBounds.min.y, spawnBounds.max.y),
                0f
            );

            // spacing check
            bool ok = true;
            for (int i = 0; i < spawned; i++)
            {
                if (Vector2.Distance(pos, placed[i]) < minDistance)
                {
                    ok = false; break;
                }
            }
            if (!ok) continue;

            // weighted pick
            float r = Random.Range(0f, totalW);
            PrefabEntry entry = null;
            float acc = 0f;
            foreach (var e in prefabs)
            {
                float w = Mathf.Max(0f, e.weight);
                acc += w;
                if (r <= acc) { entry = e; break; }
            }
            if (entry == null) entry = prefabs[prefabs.Length - 1];

            // spawn
            var go = Instantiate(entry.prefab, pos, Quaternion.identity, starsParent);

            // scale
            float s = Random.Range(Mathf.Min(entry.minScale, entry.maxScale),
                                   Mathf.Max(entry.minScale, entry.maxScale));
            go.transform.localScale = new Vector3(s, s, 1f);

            // random initial alpha (no movement)
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr)
            {
                var c = sr.color;
                c.a = Random.Range(alphaRange.x, alphaRange.y);
                sr.color = c;
            }

            // optional subtle twinkle
            if (addTwinkle && Random.value < twinkleProbability)
            {
                var tw = go.GetComponent<StarTwinkle2D>();
                if (!tw) tw = go.AddComponent<StarTwinkle2D>();

                // These property names assume your StarTwinkle2D has these fields;
                // if they differ, rename to match your script.
                tw.baseAlpha = sr ? sr.color.a : Random.Range(alphaRange.x, alphaRange.y);
                tw.amplitude = Random.Range(twinkleAmplitudeRange.x, twinkleAmplitudeRange.y);
                tw.speed = Random.Range(twinkleSpeedRange.x, twinkleSpeedRange.y);
                tw.randomizeOnEnable = true;
            }

            placed[spawned++] = pos;
        }
    }

    Bounds GetBounds()
    {
        if (useSpriteBounds && areaSprite) return areaSprite.bounds;
        if (areaBox) return areaBox.bounds;

        // Fallback small box around this object
        return new Bounds(transform.position, new Vector3(12f, 7f, 0f));
    }
}
