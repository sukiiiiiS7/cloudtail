using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple page switcher: activates exactly one child page at a time.
/// Place on PageRoot, then either press 'Collect Pages From Children'
/// or assign pages manually in the Inspector.
/// </summary>
[DisallowMultipleComponent]
public class PageController : MonoBehaviour
{
    [SerializeField] private List<GameObject> pages = new List<GameObject>();

    [Tooltip("Zero-based page index.")]
    [Min(0)] public int currentIndex = 0;

    void Awake()
    {
        if (pages.Count == 0) CollectPagesFromChildren();
        ClampIndex();
        Apply();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        ClampIndex();
        if (!Application.isPlaying) Apply();
    }
#endif

    [ContextMenu("Collect Pages From Children")]
    public void CollectPagesFromChildren()
    {
        pages.Clear();
        for (int i = 0; i < transform.childCount; i++)
            pages.Add(transform.GetChild(i).gameObject);
    }

    public void NextPage()
    {
        if (pages.Count == 0) return;
        if (currentIndex < pages.Count - 1) currentIndex++;
        Apply();
    }

    public void PrevPage()
    {
        if (pages.Count == 0) return;
        if (currentIndex > 0) currentIndex--;
        Apply();
    }

    public void SetPage(int index)
    {
        currentIndex = index;
        ClampIndex();
        Apply();
    }

    void ClampIndex()
    {
        if (pages.Count == 0) { currentIndex = 0; return; }
        currentIndex = Mathf.Clamp(currentIndex, 0, pages.Count - 1);
    }

    void Apply()
    {
        for (int i = 0; i < pages.Count; i++)
        {
            if (!pages[i]) continue;
            pages[i].SetActive(i == currentIndex);
        }
        // Optional: Debug.Log($"Page {currentIndex+1}/{pages.Count}");
    }
}

