using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    public static bool globalIsDragging = false;
    public bool isRevealed = false;
    public bool isAnimatingCombat = false;

    public void RevealCard()
    {
        isRevealed = true;
        UpdateCardVisualsAndState();
    }

    [Header("Kimlik")]
    public int cardId;
    public int ownerId;

    [Header("Durum")]
    public bool isOwnedByClient;
    public bool isPlayedOnBoard = false;

    private MinionCardDisplay visualDisplay;

    [Header("Animasyon Ayarlarý")]
    public float hoverScaleAmount = 1.3f;
    public float hoverHeightAmount = 50f;
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
    private float targetY;
    private Vector2 dragOffset;
    private bool isHovering = false;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        visualDisplay = GetComponent<MinionCardDisplay>();

        cardCanvas = GetComponent<Canvas>();
        if (cardCanvas == null)
        {
            cardCanvas = gameObject.AddComponent<Canvas>();
            gameObject.AddComponent<GraphicRaycaster>();
            cardCanvas.overrideSorting = false;
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
        // EÐER SAVAÞ ANÝMASYONU VARSA BU KOD ÇALIÞMASIN, KARIÞMASIN.
        if (isAnimatingCombat) return;

        // 1. SCALE ANÝMASYONU
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animSpeed);

        // 2. POZÝSYON ANÝMASYONU
        if (!isLocked && canvasGroup.blocksRaycasts && !isPlayedOnBoard)
        {
            // ... (Eski kodlar aynen kalacak) ...
            float currentX = transform.localPosition.x;
            float newY = Mathf.Lerp(transform.localPosition.y, targetY, Time.deltaTime * animSpeed);
            transform.localPosition = new Vector3(currentX, newY, 0);
        }
    }

    public void UpdateCardVisualsAndState()
    {
        bool shouldFaceDown = true;

        // 1. KART SAHADAYSA (Lock Mantýðý ve Gizlilik)
        if (isPlayedOnBoard)
        {
            canvasGroup.blocksRaycasts = false;

            // EÐER KART AÇIKLANDIYSA (SAVAÞTIYSA) HERKESE GÖZÜKÜR
            if (isRevealed)
            {
                shouldFaceDown = false;
            }
            // DEÐÝLSE SADECE SAHÝBÝ GÖRÜR
            else
            {
                shouldFaceDown = !isOwnedByClient;
            }
        }
        // 2. KART ELDEYSE (Sýra Mantýðý)
        else if (TurnManager.Instance != null)
        {
            int currentTurnPlayer = TurnManager.Instance.isPlayerOneTurn ? 1 : 2;

            if (ownerId == currentTurnPlayer)
            {
                shouldFaceDown = false;
                canvasGroup.blocksRaycasts = true; // Sadece sýrasý gelen oynayabilir
            }
            else
            {
                shouldFaceDown = true;
                canvasGroup.blocksRaycasts = false;
            }
        }

        if (visualDisplay != null) visualDisplay.SetFaceDown(shouldFaceDown);

        if (!isHovering && !isPlayedOnBoard)
        {
            targetY = 0f;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isLocked || !canvasGroup.blocksRaycasts || globalIsDragging) return;

        if (isOwnedByClient && !isPlayedOnBoard)
        {
            isHovering = true;
            targetY = hoverHeightAmount;
            targetScale = Vector3.one * hoverScaleAmount;
            cardCanvas.overrideSorting = true;
            cardCanvas.sortingOrder = 100;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isLocked || !canvasGroup.blocksRaycasts) return;
        if (globalIsDragging && isHovering) return;

        if (isOwnedByClient && !isPlayedOnBoard)
        {
            isHovering = false;
            targetY = 0f;
            targetScale = Vector3.one * normalScaleAmount;
            cardCanvas.overrideSorting = false;
            cardCanvas.sortingOrder = 0;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isLocked || !canvasGroup.blocksRaycasts) return;

        globalIsDragging = true;
        isHovering = false;

        parentToReturnTo = transform.parent;
        transform.SetParent(transform.root);
        canvasGroup.blocksRaycasts = false;
        targetScale = Vector3.one * dragScaleAmount;

        cardCanvas.overrideSorting = true;
        cardCanvas.sortingOrder = 200;

        Vector3 mousePos;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out mousePos);
        dragOffset = transform.position - mousePos;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isLocked) return;
        Vector3 mousePos;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out mousePos);
        transform.position = mousePos + (Vector3)dragOffset;
    }

    // --- BURASI GÜNCELLENDÝ: SLOTINFO KALDIRILDI ---
    public void OnEndDrag(PointerEventData eventData)
    {
        globalIsDragging = false;

        if (!isLocked)
        {
            // Varsayýlan olarak kartý yerine dönmeye ayarla.
            // Eðer AttemptPlayCard baþarýlý olursa, kart zaten slota ýþýnlanacak ve bu override edilecek.
            bool playSuccessful = false;

            if (eventData.pointerEnter != null)
            {
                PlayerManager myManager = null;
                PlayerManager[] managers = FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);

                foreach (var pm in managers)
                {
                    if (pm.myPlayerId == ownerId)
                    {
                        myManager = pm;
                        break;
                    }
                }

                if (myManager != null)
                {
                    int foundIndex = myManager.GetSlotIndex(eventData.pointerEnter);

                    if (foundIndex != -1)
                    {
                        // DEÐÝÞÝKLÝK BURADA: Kartýn kendisini (this) gönderiyoruz
                        // myManager artýk kartý alýr almaz slota yapýþtýracak.
                        myManager.AttemptPlayCard(this, foundIndex);

                        // Not: PlayCard baþarýlý olursa isLocked = true olur.
                        // Aþaðýdaki kontrolle çakýþmamasý için kontrol edebilirsin ama
                        // MoveToSlot içinde parent deðiþtiði için sorun olmaz.
                    }
                }
            }

            // Eðer kart kilitlenmediyse (yani oynanamadýysa veya boþluða býrakýldýysa) eline geri dönsün
            if (!isLocked)
            {
                canvasGroup.blocksRaycasts = true;
                transform.SetParent(parentToReturnTo);
                targetScale = Vector3.one * normalScaleAmount;
                targetY = 0f;
                cardCanvas.overrideSorting = false;
                cardCanvas.sortingOrder = 0;
            }
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