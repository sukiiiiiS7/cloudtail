using System.Collections.Generic;
using UnityEngine;

/// Minimal stack of placed decor instances for quick undo.
public static class PlacementHistory
{
    private static readonly Stack<GameObject> stack = new Stack<GameObject>();

    public static void Push(GameObject go)
    {
        if (go != null) stack.Push(go);
    }

    public static void RemoveLast()
    {
        while (stack.Count > 0)
        {
            var go = stack.Pop();
            if (go != null)
            {
                Object.Destroy(go);
                return;
            }
        }
    }
}
