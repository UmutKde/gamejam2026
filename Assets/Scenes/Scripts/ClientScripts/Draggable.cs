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

    // --- YENÝ BAÐLANTI: Arkadaþýnýn Scripti ---
    private MinionCardDisplay visualDisplay;
    // ------------------------------------------

    [Header("Animasyon Ayarlarý")]
    public float hoverScaleAmount = 1.3f;
    public float dragScaleAmount = 1.1f;
    public float playedScaleAmount = 1.2f;
    public float normalScaleAmount = 1.0f;
    public float animSpeed = 15f;

    private CanvasGroup canvasGroup;
    private Canvas cardCanvas;
    private RectTransform rectTransform;
    public Transform parentToReturnTo = null;
    public bool isLocked = false;
    private Vector3 targetScale;
    private int originalSortingOrder;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();

        // Arkadaþýnýn görsel scriptini buluyoruz
        visualDisplay = GetComponent<MinionCardDisplay>();

        cardCanvas = GetComponent<Canvas>();
        if (cardCanvas == null)
        {
            cardCanvas = gameObject.AddComponent<Canvas>();
            gameObject.AddComponent<GraphicRaycaster>();
            cardCanvas.overrideSorting = true;
        }
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
        if (!isLocked || isPlayedOnBoard)
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animSpeed);
    }

    public void UpdateCardVisualsAndState()
    {
        // GÖRSELÝ YÖNETEN KISIM
        bool shouldFaceDown = true; // Varsayýlan kapalý

        // 1. KART YERDEYSE: Her zaman AÇIK (FaceDown = false)
        if (isPlayedOnBoard)
        {
            shouldFaceDown = false;
            canvasGroup.blocksRaycasts = false;
        }
        // 2. KART ELDEYSE:
        else if (TurnManager.Instance != null)
        {
            int currentTurnPlayer = TurnManager.Instance.isPlayerOneTurn ? 1 : 2;

            // Kartýn sahibi oynuyorsa -> AÇIK (False)
            if (ownerId == currentTurnPlayer)
            {
                shouldFaceDown = false;
                canvasGroup.blocksRaycasts = true;
            }
            // Rakip oynuyorsa -> KAPALI (True)
            else
            {
                shouldFaceDown = true;
                canvasGroup.blocksRaycasts = false;
            }
        }

        // Arkadaþýnýn fonksiyonunu tetikle
        if (visualDisplay != null)
        {
            visualDisplay.SetFaceDown(shouldFaceDown);
        }
    }

    // --- SÜRÜKLEME KODLARI (AYNI) ---
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
    public void OnDrag(PointerEventData eventData) { if (!isLocked) transform.position = eventData.position; }
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
        targetScale = Vector3.one * playedScaleAmount;
        if (cardCanvas) cardCanvas.overrideSorting = false;
        UpdateCardVisualsAndState();
    }
}