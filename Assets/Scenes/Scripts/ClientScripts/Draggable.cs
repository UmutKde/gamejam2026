using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Kimlik")]
    public int cardId;
    public int ownerId;

    [Header("Durum")]
    public bool isOwnedByClient;
    public bool isPlayedOnBoard = false;

    [Header("Görseller")]
    public Sprite frontSprite;
    public Sprite backSprite;

    private Image cardImage;
    private CanvasGroup canvasGroup;
    public Transform parentToReturnTo = null;
    public bool isLocked = false;
    private RectTransform rectTransform;

    void Awake()
    {
        cardImage = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        if (frontSprite == null && cardImage != null) frontSprite = cardImage.sprite;
    }

    public void InitializeCard(int id, int owner)
    {
        cardId = id;
        ownerId = owner;
        UpdateCardVisualsAndState();
    }

    public void UpdateCardVisualsAndState()
    {
        if (isPlayedOnBoard)
        {
            cardImage.sprite = frontSprite;
            canvasGroup.blocksRaycasts = false;
            return;
        }

        if (TurnManager.Instance != null)
        {
            int currentTurnPlayer = TurnManager.Instance.isPlayerOneTurn ? 1 : 2;

            if (ownerId == currentTurnPlayer)
            {
                cardImage.sprite = frontSprite;
                canvasGroup.blocksRaycasts = true;
            }
            else
            {
                cardImage.sprite = backSprite;
                canvasGroup.blocksRaycasts = false;
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isLocked || !canvasGroup.blocksRaycasts) return;
        parentToReturnTo = transform.parent;
        transform.SetParent(transform.root);
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isLocked) transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isLocked)
        {
            canvasGroup.blocksRaycasts = true;
            transform.SetParent(parentToReturnTo);
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }

    public void MoveToSlot(Transform slotTransform)
    {
        transform.SetParent(slotTransform);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;

        isPlayedOnBoard = true;
        isLocked = true;

        UpdateCardVisualsAndState();
    }
}