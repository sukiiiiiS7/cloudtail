using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class PlanetSwitcher : MonoBehaviour
{
    [Header("Planet set")]
    [Tooltip("Ordered list of planets. Only current index stays active when toggleActiveOnSwitch = true.")]
    public List<GameObject> planets = new List<GameObject>();

    [Tooltip("Initial active index on Start.")]
    public int startIndex = 0;

    [Header("Transition")]
    [Tooltip("Animate on switch. If disabled, switch is instant.")]
    public bool animate = false;

    [Tooltip("Duration for simple tween.")]
    [Range(0.05f, 1.0f)] public float moveDuration = 0.25f;

    [Tooltip("Disable input during switching.")]
    public bool disableInputWhileSwitching = true;

    [Tooltip("Toggle GameObject active so only current index is visible.")]
    public bool toggleActiveOnSwitch = true;

    [Header("Events")]
    [Tooltip("Invoked after index changed. Argument is new index.")]
    public UnityEvent<int> onIndexChanged;

    // Standard C# event for += style subscribers.
    public event System.Action<int> OnPlanetChanged;

    int _index = 0;
    bool _isSwitching = false;

    public int Index => _index;
    public bool IsSwitching => _isSwitching;

    void Awake()
    {
        if (planets.Count > 0) _index = Mod(startIndex, planets.Count);
        else _index = 0;
    }

    void Start()
    {
        ApplyStateInstant();
        onIndexChanged?.Invoke(_index);
        OnPlanetChanged?.Invoke(_index);
    }

    public void Prev()
    {
        if (!CanSwitch()) return;
        int next = (_index - 1 + (planets.Count > 0 ? planets.Count : 1)) % (planets.Count > 0 ? planets.Count : 1);
        JumpTo(next);
    }

    public void Next()
    {
        if (!CanSwitch()) return;
        int next = (_index + 1) % (planets.Count > 0 ? planets.Count : 1);
        JumpTo(next);
    }

    public void JumpTo(int targetIndex)
    {
        if (!CanSwitch()) return;
        if (planets.Count == 0) return;

        int wrapped = Mod(targetIndex, planets.Count);
        if (wrapped == _index) return;

        if (animate) StartCoroutine(SwitchRoutine(wrapped));
        else ApplyIndex(wrapped);
    }

    public void JumpBy(int delta)
    {
        if (!CanSwitch()) return;
        if (planets.Count == 0) return;
        JumpTo(_index + delta);
    }
    /// <summary>
    /// Compatibility shim for external callers (e.g., RecommendClient).
    /// Accepts any int, wraps to valid range, then delegates to JumpTo().
    /// No changes to existing switching logic.
    /// </summary>
    public void OpenWithSuggestionIndex(int idx)
    {
        if (planets == null || planets.Count == 0)
        {
            Debug.LogWarning("PlanetSwitcher: no planets configured; suggestion ignored.");
            return;
            
        }
        // Wrap to list range; preserves negative/overflow indices via Mod().
        int target = Mod(idx, planets.Count);
        JumpTo(target);
    }


    bool CanSwitch()
    {
        if (planets == null || planets.Count == 0) return false;
        if (disableInputWhileSwitching && _isSwitching) return false;
        return true;
    }

    IEnumerator SwitchRoutine(int newIndex)
    {
        _isSwitching = true;

        GameObject oldGo = SafeGet(_index);
        GameObject newGo = SafeGet(newIndex);

        if (toggleActiveOnSwitch) { if (newGo) newGo.SetActive(true); }

        float t = 0f;
        Vector3 oldStart = oldGo ? oldGo.transform.localScale : Vector3.one;
        Vector3 newStart = newGo ? newGo.transform.localScale : Vector3.one * 0.9f;
        if (newGo) newGo.transform.localScale = newStart;

        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / moveDuration);
            float ease = EaseOutCubic(k);

            if (oldGo) oldGo.transform.localScale = Vector3.Lerp(oldStart, Vector3.one * 0.95f, ease);
            if (newGo) newGo.transform.localScale = Vector3.Lerp(newStart, Vector3.one, ease);

            yield return null;
        }

        ApplyIndex(newIndex);

        if (oldGo) oldGo.transform.localScale = Vector3.one;
        if (newGo) newGo.transform.localScale = Vector3.one;

        _isSwitching = false;
    }

    void ApplyIndex(int newIndex)
    {
        _index = newIndex;

        if (toggleActiveOnSwitch) ToggleActiveOnly(_index);

        onIndexChanged?.Invoke(_index);   // UnityEvent
        OnPlanetChanged?.Invoke(_index);  // C# event
    }

    void ApplyStateInstant()
    {
        if (toggleActiveOnSwitch) ToggleActiveOnly(_index);
    }

    void ToggleActiveOnly(int activeIndex)
    {
        for (int i = 0; i < planets.Count; i++)
        {
            var go = planets[i];
            if (!go) continue;
            go.SetActive(i == activeIndex);
        }
    }

    static int Mod(int x, int m)
    {
        if (m <= 0) return 0;
        int r = x % m;
        return r < 0 ? r + m : r;
    }

    static float EaseOutCubic(float x)
    {
        return 1f - Mathf.Pow(1f - x, 3f);
    }

    GameObject SafeGet(int idx)
    {
        if (idx < 0 || idx >= planets.Count) return null;
        return planets[idx];
    }
}
