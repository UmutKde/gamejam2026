using UnityEngine;
using UnityEngine.EventSystems;

public class DropZone : MonoBehaviour, IDropHandler
{
    [Header("Slot Ayarlarý")]
    public bool isArenaSlot = true;
    public bool isPlayerOneSlot = true;

    public void OnDrop(PointerEventData eventData)
    {
        Draggable d = eventData.pointerDrag.GetComponent<Draggable>();

        if (d != null)
        {
            if (transform.childCount > 0) return;

            if (isArenaSlot)
            {
                if (TurnManager.Instance.isPlayerOneTurn && !isPlayerOneSlot)
                {
                    Debug.Log("Hata: Player 1, rakip alana kart koyamaz!");
                    return;
                }

                if (!TurnManager.Instance.isPlayerOneTurn && isPlayerOneSlot)
                {
                    Debug.Log("Hata: Player 2, senin alanýna kart koyamaz!");
                    return;
                }
            }

            d.parentToReturnTo = this.transform;

            if (isArenaSlot)
            {
                d.LockAndShrink();
            }
        }
    }
}