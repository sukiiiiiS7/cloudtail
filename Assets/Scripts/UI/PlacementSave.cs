using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cloudtail.IO
{
    /// <summary>
    /// Minimal local save/load for placed decorations.
    /// - Saves children under DecorPlacer.decorRoot: prefab name + position/rotation/scale + planet index
    /// - Rebuilds by matching prefab name in InventoryPager.items (fallback: exact name in scene if present)
    /// - JSON stored in PlayerPrefs
    /// Triggers SaveComplete guidance after a successful save.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlacementSave : MonoBehaviour
    {
        [Header("Scene refs")]
        public DecorPlacer placer;                 // provides decorRoot + render defaults
        public InventoryPager pager;               // lookup prefabs by name
        public PlanetSwitcher switcher;            // save current planet index (future-proof)

        [Header("Keys")]
        public string prefsKey = "PLACEMENTS_V1";

        [Header("Options")]
        public bool includeScale = false;
        public bool log = true;

        [Header("Guide (optional)")]
        [Tooltip("Bridge used to show the dialog and switch guidance to SaveComplete.")]
        public Cloudtail.UI.GuideFlowBridge guideFlow; // optional reference; will auto-find if null

        Transform DecorRoot => placer ? placer.decorRoot : null;

        public void SaveNow()
        {
            var root = DecorRoot;
            if (!root)
            {
                if (log) Debug.LogWarning("[PlacementSave] decorRoot not set.");
                return;
            }

            var data = new SaveData
            {
                planet = switcher ? switcher.Index : 0,
                entries = new List<Entry>(root.childCount)
            };

            for (int i = 0; i < root.childCount; i++)
            {
                var t = root.GetChild(i);
                if (!t || t.gameObject.hideFlags == HideFlags.HideInHierarchy) continue;

                string prefabName = t.gameObject.name;
                // When instantiated, Unity appends "(Clone)"; trim it
                int idx = prefabName.IndexOf("(Clone)", StringComparison.Ordinal);
                if (idx >= 0) prefabName = prefabName.Substring(0, idx);

                var e = new Entry
                {
                    name = prefabName,
                    x = t.position.x,
                    y = t.position.y,
                    rot = t.eulerAngles.z,
                    sx = includeScale ? t.localScale.x : 1f,
                    sy = includeScale ? t.localScale.y : 1f,
                };
                data.entries.Add(e);
            }

            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(prefsKey, json);
            PlayerPrefs.Save();
            if (log) Debug.Log($"[PlacementSave] Saved {data.entries.Count} items (planet={data.planet}).");

            // ---------- NEW: switch Noka guidance to SaveComplete ----------
            // Prefer assigned reference, otherwise fallback to auto-find.
            var flow = guideFlow ? guideFlow : UnityEngine.Object.FindFirstObjectByType<Cloudtail.UI.GuideFlowBridge>();
            if (flow != null) flow.ShowSaveComplete();
            // ---------------------------------------------------------------
        }

        public void LoadNow(bool clearBefore = true)
        {
            string json = PlayerPrefs.GetString(prefsKey, "");
            if (string.IsNullOrEmpty(json))
            {
                if (log) Debug.Log("[PlacementSave] No data.");
                return;
            }

            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null) { if (log) Debug.LogWarning("[PlacementSave] Parse failed."); return; }

            // Optional: jump to saved planet
            if (switcher && data.planet >= 0)
            {
                var jump = typeof(PlanetSwitcher).GetMethod("JumpTo", new Type[] { typeof(int) });
                if (jump != null) jump.Invoke(switcher, new object[] { Mathf.Clamp(data.planet, 0, switcher.planets.Count - 1) });
            }

            var root = DecorRoot;
            if (!root) { if (log) Debug.LogWarning("[PlacementSave] decorRoot not set."); return; }

            if (clearBefore) ClearNow();

            // Build a quick name->prefab map from pager
            Dictionary<string, GameObject> byName = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            if (pager != null && pager.items != null)
            {
                foreach (var p in pager.items)
                    if (p) byName[p.name] = p;
            }

            foreach (var e in data.entries)
            {
                GameObject prefab = null;
                byName.TryGetValue(e.name, out prefab);

                if (!prefab)
                {
                    if (log) Debug.LogWarning($"[PlacementSave] Prefab not found: {e.name}. Skipped.");
                    continue;
                }

                var pos = new Vector3(e.x, e.y, 0f);
                var go = Instantiate(prefab, pos, Quaternion.Euler(0, 0, e.rot), root);
                if (includeScale) go.transform.localScale = new Vector3(e.sx, e.sy, 1f);

                // Reapply render defaults like DecorPlacer does
                if (placer)
                {
                    var rds = go.GetComponentsInChildren<SpriteRenderer>(true);
                    foreach (var r in rds)
                    {
                        if (!string.IsNullOrEmpty(placer.sortingLayerName)) r.sortingLayerName = placer.sortingLayerName;
                        r.sortingOrder = placer.sortingOrder;
                    }
                }
            }

            if (log) Debug.Log($"[PlacementSave] Loaded {data.entries.Count} items.");
        }

        public void ClearNow()
        {
            var root = DecorRoot;
            if (!root) return;
            List<GameObject> toDel = new List<GameObject>();
            for (int i = 0; i < root.childCount; i++)
            {
                var t = root.GetChild(i);
                if (t) toDel.Add(t.gameObject);
            }
            foreach (var go in toDel) Destroy(go);
            if (log) Debug.Log("[PlacementSave] Cleared scene items.");
        }

        // ---------- Data ----------
        [Serializable]
        public class SaveData
        {
            public int planet;
            public List<Entry> entries;
        }

        [Serializable]
        public class Entry
        {
            public string name;
            public float x, y;
            public float rot;
            public float sx, sy;
        }
    }
}
