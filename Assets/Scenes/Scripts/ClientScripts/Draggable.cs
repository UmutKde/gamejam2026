using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
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

    [Header("Animasyon Ayarlarý")]
    public float hoverScaleAmount = 1.3f; // Mouse üzerine gelince
    public float dragScaleAmount = 1.1f;  // Sürüklerken
    public float playedScaleAmount = 1.2f; // YERE ÝNÝNCE (Yeni Eklendi!)
    public float normalScaleAmount = 1.0f; // Elde dururken
    public float animSpeed = 15f;

    private Image cardImage;
    private CanvasGroup canvasGroup;
    private Canvas cardCanvas;
    private RectTransform rectTransform;

    public Transform parentToReturnTo = null;
    public bool isLocked = false;

    private Vector3 targetScale;
    private int originalSortingOrder;

    void Awake()
    {
        cardImage = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();

        // Canvas ve Raycaster Kontrolü
        cardCanvas = GetComponent<Canvas>();
        if (cardCanvas == null)
        {
            cardCanvas = gameObject.AddComponent<Canvas>();
            gameObject.AddComponent<GraphicRaycaster>();
            cardCanvas.overrideSorting = true;
        }

        if (frontSprite == null && cardImage != null) frontSprite = cardImage.sprite;

        targetScale = Vector3.one * normalScaleAmount;
    }

    public void InitializeCard(int id, int owner)
    {
        cardId = id;
        ownerId = owner;
        UpdateCardVisualsAndState();
    }

    void Update()
    {
        // Hedef boyuta doðru yumuþak geçiþ
        if (!isLocked || isPlayedOnBoard)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isLocked || !canvasGroup.blocksRaycasts) return;

        if (isOwnedByClient && !isPlayedOnBoard)
        {
            targetScale = Vector3.one * hoverScaleAmount;
            originalSortingOrder = cardCanvas.sortingOrder;
            cardCanvas.sortingOrder = 100;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isLocked || !canvasGroup.blocksRaycasts) return;

        if (isOwnedByClient && !isPlayedOnBoard)
        {
            targetScale = Vector3.one * normalScaleAmount;
            cardCanvas.sortingOrder = originalSortingOrder;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isLocked || !canvasGroup.blocksRaycasts) return;

        parentToReturnTo = transform.parent;
        transform.SetParent(transform.root);
        canvasGroup.blocksRaycasts = false;

        targetScale = Vector3.one * dragScaleAmount;
        cardCanvas.sortingOrder = 101;
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

            targetScale = Vector3.one * normalScaleAmount;
            cardCanvas.sortingOrder = originalSortingOrder;
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

        // --- DEÐÝÞÝKLÝK BURADA: ARTIK ÖZEL BOYUTA BÜYÜYOR ---
        targetScale = Vector3.one * playedScaleAmount;

        if (cardCanvas) cardCanvas.overrideSorting = false;

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
}