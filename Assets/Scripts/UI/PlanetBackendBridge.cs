using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlanetBackendBridge : MonoBehaviour
{
    [Header("Wiring")]
    [Tooltip("PlanetSwitcher placed on PlanetsRoot (required).")]
    public PlanetSwitcher switcher;          // required: controls active planet
    [Tooltip("DecorPlacer to update its decorRoot per planet (optional).")]
    public DecorPlacer placer;               // optional: switch current DecorRoot
    [Tooltip("InventoryPager to swap items per planet (optional).")]
    public InventoryPager pager;             // optional: switch visible item list

    [Header("Stable mapping (index ↔ key)")]
    [Tooltip("Stable string IDs aligned with PlanetSwitcher.planets order (e.g. \"island\",\"amber\",\"crystal\",\"void\").")]
    public List<string> planetKeysByIndex = new();  // must match switcher.planets order

    [Tooltip("Per-planet DecorRoot (Transform). New placements will be parented here (optional).")]
    public List<Transform> decorRootsByIndex = new();

    [Tooltip("Per-planet ItemSet (ScriptableObject listing items for the pager; optional).")]
    public List<ItemSet> itemSetsByIndex = new();

    [Serializable]
    public struct EmotionRoute
    {
        public string emotion;    // e.g., "calm", "sad", "excited"
        public string planetKey;  // e.g., "island"
    }

    [Header("Local routing (emotion → planetKey)")]
    [Tooltip("Optional local table that maps backend emotion strings to a planetKey.")]
    public List<EmotionRoute> emotionRoutes = new();

    [Header("Options")]
    [Tooltip("When planet changes, also update DecorPlacer.decorRoot.")]
    public bool updatePlacerRoot = true;
    [Tooltip("When planet changes, also update InventoryPager.items and reset to page 0.")]
    public bool updatePagerItems = true;
    [Tooltip("Remember last selected planet index via PlayerPrefs.")]
    public bool rememberLastIndex = true;

    private Action<int> _switcherHandler;

    void Start()
    {
        if (!switcher || switcher.planets == null || switcher.planets.Count == 0)
        {
            Debug.LogWarning("[PlanetBridge] PlanetSwitcher is not assigned or has no planets.");
            return;
        }

        // Subscribe to planet change (C# event signature: int)
        _switcherHandler = OnSwitcherPlanetChanged;
        switcher.OnPlanetChanged += _switcherHandler;

        // Optionally restore last index, then ensure dependents are synced once
        if (rememberLastIndex && PlayerPrefs.HasKey("PlanetIndex"))
        {
            int saved = Mathf.Clamp(PlayerPrefs.GetInt("PlanetIndex", 0), 0, switcher.planets.Count - 1);
            SwitchToIndex(saved);
        }

        // Push current state to dependents on startup
        OnPlanetChanged(switcher.Index);
    }

    void OnDestroy()
    {
        if (switcher != null && _switcherHandler != null)
            switcher.OnPlanetChanged -= _switcherHandler;
    }

    // -------- Backend entry points --------

    /// Apply a backend-provided planet index.
    public void ApplyBackendIndex(int index)
    {
        if (!switcher) return;
        SwitchToIndex(index);
    }

    /// Apply a backend-provided planet key (string ID).
    public void ApplyBackendKey(string key)
    {
        if (!switcher || string.IsNullOrEmpty(key) || planetKeysByIndex == null) return;
        int idx = planetKeysByIndex.FindIndex(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0) SwitchToIndex(idx);
        else Debug.LogWarning($"[PlanetBridge] Unknown planetKey: {key}");
    }

    /// Apply a backend-provided emotion. Looks up a planetKey via local routing.
    public void ApplyBackendEmotion(string emotion)
    {
        if (string.IsNullOrEmpty(emotion) || emotionRoutes == null) return;

        string key = null;
        for (int i = 0; i < emotionRoutes.Count; i++)
        {
            if (string.Equals(emotionRoutes[i].emotion, emotion, StringComparison.OrdinalIgnoreCase))
            {
                key = emotionRoutes[i].planetKey;
                break;
            }
        }

        if (!string.IsNullOrEmpty(key)) ApplyBackendKey(key);
        else Debug.LogWarning($"[PlanetBridge] No route for emotion: {emotion}");
    }

    // -------- Internal switching --------

    /// Centralized planet switcher. Prefer JumpTo(int); fallback walks Next/Prev.
    private void SwitchToIndex(int targetIndex)
    {
        if (!switcher || switcher.planets == null || switcher.planets.Count == 0) return;

        int n = switcher.planets.Count;
        int clamped = Mathf.Clamp(targetIndex, 0, n - 1);

        // Prefer direct API (present in current PlanetSwitcher)
        switcher.JumpTo(clamped);
        // Note: PlanetSwitcher will invoke its change events; manual OnPlanetChanged call is not required.
    }

    private void OnSwitcherPlanetChanged(int index)
    {
        OnPlanetChanged(index);
    }

    /// Sync dependent systems (placer/pager) and optionally persist the index.
    private void OnPlanetChanged(int index)
    {
        // DecorPlacer: switch current DecorRoot
        if (updatePlacerRoot && placer && index >= 0 && index < decorRootsByIndex.Count && decorRootsByIndex[index])
            placer.decorRoot = decorRootsByIndex[index];

        // InventoryPager: swap items and reset to page 0
        if (updatePagerItems && pager)
        {
            if (index >= 0 && index < itemSetsByIndex.Count && itemSetsByIndex[index])
                pager.items = new List<GameObject>(itemSetsByIndex[index].items);

            pager.JumpTo(0);
            pager.RefreshPage();
        }

        if (rememberLastIndex)
        {
            PlayerPrefs.SetInt("PlanetIndex", Mathf.Max(0, index));
            PlayerPrefs.Save();
        }
    }
}
