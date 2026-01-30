using UnityEngine;
using UnityEngine.EventSystems;

public class DropZone : MonoBehaviour, IDropHandler
{
    public bool isArenaSlot = true;
    public void OnDrop(PointerEventData eventData)
    {
        Draggable d = eventData.pointerDrag.GetComponent<Draggable>();

        if (d != null)
        {
            if (transform.childCount > 0) return;

            d.parentToReturnTo = this.transform;
            if (isArenaSlot)
            {
                d.LockAndShrink();
            }
        }
    }
}