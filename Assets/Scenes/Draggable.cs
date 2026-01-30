using UnityEngine;
using UnityEngine.EventSystems;

public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Transform parentToReturnTo = null;
    public bool isLocked = false;

    CanvasGroup canvasGroup;
    RectTransform rectTransform;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isLocked) return;
        parentToReturnTo = transform.parent;
        transform.SetParent(transform.root);
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isLocked) return;
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isLocked) return;
        canvasGroup.blocksRaycasts = true;
        transform.SetParent(parentToReturnTo);
        rectTransform.anchoredPosition = Vector2.zero;
    }

    public void LockAndShrink()
    {
        transform.SetParent(parentToReturnTo);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        canvasGroup.blocksRaycasts = true;
        isLocked = true;
        transform.localScale = new Vector3(0.7f, 0.7f, 1f);
    }
}