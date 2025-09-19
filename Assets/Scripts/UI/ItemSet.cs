using System.Collections.Generic;
using UnityEngine;

/// ScriptableObject: list of prefabs to display in InventoryPager.
/// Create via: Assets > Create > Cloudtail > Item Set.
[CreateAssetMenu(menuName = "Cloudtail/Item Set", fileName = "ItemSet")]
public class ItemSet : ScriptableObject
{
    [Tooltip("Prefabs in display order (used by InventoryPager).")]
    public List<GameObject> items = new List<GameObject>();
}
