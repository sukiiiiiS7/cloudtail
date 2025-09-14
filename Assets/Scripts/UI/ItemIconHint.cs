using UnityEngine;

/// Per-item UI tweak for SlotIcon. Put this on the item prefab.
public class ItemIconHint : MonoBehaviour
{
    [Tooltip("Extra pixel offset in the slot (x,y). +Y is up.")]
    public Vector2 nudgePixels = Vector2.zero;

    [Tooltip("Extra uniform scale multiplier after fit. 1 = no change.")]
    public float scaleMul = 1f;
}
