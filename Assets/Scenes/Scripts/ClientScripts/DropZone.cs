using UnityEngine;
using UnityEngine.EventSystems;

public class DropZone : MonoBehaviour, IDropHandler
{
    [Header("Slot Ayarlarý")]
    public int slotIndex;
    public int slotOwnerId;
    public bool isArenaSlot = true;

    public PlayerManager connectedManager;

    public void OnDrop(PointerEventData eventData)
    {
        /*
        Draggable d = eventData.pointerDrag.GetComponent<Draggable>();

        if (d != null)
        {
            if (transform.childCount > 0) return;

            if (isArenaSlot)
            {
                if (d.ownerId != slotOwnerId)
                {
                    Debug.Log($"HATA: Player {d.ownerId}, Player {slotOwnerId} bölgesine oynayamaz!");
                    return;
                }

                connectedManager.AttemptPlayCard(d.cardId, slotIndex);
            }
            else
            {
                d.parentToReturnTo = this.transform;
            }
        }
        */
    }
}