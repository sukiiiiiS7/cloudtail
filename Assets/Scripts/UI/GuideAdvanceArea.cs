using UnityEngine;
using UnityEngine.EventSystems;

namespace Cloudtail.UI
{
    /// <summary>
    /// Advances guide when this UI area is clicked. Requires an Image with Raycast Target enabled.
    /// </summary>
    public class GuideAdvanceArea : MonoBehaviour, IPointerClickHandler
    {
        public NokaGuideController guide;

        public void OnPointerClick(PointerEventData e)
        {
            if (guide != null)
            {
                guide.OnAdvance();
                Debug.Log("[Guide] Advance via ClickCatcher");
            }
        }
    }
}
