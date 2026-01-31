using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    // --- YENÝ: TÜM KARTLARIN ORTAKLAÞA KULLANDIÐI "BÝRÝ SÜRÜKLENÝYOR MU?" DEÐÝÞKENÝ ---
    public static bool globalIsDragging = false;

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

    // Hedef Deðerler
    private Vector3 targetScale;
    private float targetY;

    // Sürükleme Hesabý
    private Vector2 dragOffset;
    private bool isHovering = false;
    private int originalSortingOrder;

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
            // Baþlangýçta override kapalý olsun ki Layout Group sýralasýn
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
        // HATA BURADAYDI: "if (isLocked) return;" satýrý animasyonu öldürüyordu.
        // Onu kaldýrdýk. Artýk Scale animasyonu her durumda çalýþacak.

        // 1. SCALE ANÝMASYONU
        // Kart kilitli olsa bile (MoveToSlot çalýþtýðýnda) hedef boyuta küçülmesi lazým.
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animSpeed);

        // 2. POZÝSYON ANÝMASYONU (Sadece Kart Eldeyse)
        // Pozisyonu sadece kart kilitli deðilse (eldeyse) ve oynanmamýþsa yönetiyoruz.
        if (!isLocked && canvasGroup.blocksRaycasts && !isPlayedOnBoard)
        {
            float currentX = transform.localPosition.x;
            float newY = Mathf.Lerp(transform.localPosition.y, targetY, Time.deltaTime * animSpeed);
            transform.localPosition = new Vector3(currentX, newY, 0);
        }
    }

    public void UpdateCardVisualsAndState()
    {
        bool shouldFaceDown = true;

        if (isPlayedOnBoard)
        {
            shouldFaceDown = false;
            canvasGroup.blocksRaycasts = false;
        }
        else if (TurnManager.Instance != null)
        {
            int currentTurnPlayer = TurnManager.Instance.isPlayerOneTurn ? 1 : 2;

            if (ownerId == currentTurnPlayer)
            {
                shouldFaceDown = false;
                canvasGroup.blocksRaycasts = true;
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

    // --- MOUSE ÜZERÝNE GELÝNCE ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 1. Kilitliyse veya baþkasý sürükleniyorsa tepki verme!
        if (isLocked || !canvasGroup.blocksRaycasts || globalIsDragging) return;

        if (isOwnedByClient && !isPlayedOnBoard)
        {
            isHovering = true;
            targetY = hoverHeightAmount;
            targetScale = Vector3.one * hoverScaleAmount;

            // 2. EN ÜSTTE GÖRÜNME (Sorting Order)
            cardCanvas.overrideSorting = true; // Sýralamayý biz devralýyoruz
            cardCanvas.sortingOrder = 100;     // Diðer kartlarýn (genelde 0-10 arasýdýr) üzerine çýk
        }
    }

    // --- MOUSE GÝDÝNCE ---
    public void OnPointerExit(PointerEventData eventData)
    {
        if (isLocked || !canvasGroup.blocksRaycasts) return;

        // Eðer sürükleme baþladýysa Exit animasyonunu iptal et (Drag yönetecek)
        if (globalIsDragging && isHovering) return;

        if (isOwnedByClient && !isPlayedOnBoard)
        {
            isHovering = false;
            targetY = 0f;
            targetScale = Vector3.one * normalScaleAmount;

            // Sýralamayý Layout Group'a geri ver
            cardCanvas.overrideSorting = false;
            cardCanvas.sortingOrder = 0;
        }
    }

    // --- SÜRÜKLEME BAÞLADI ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isLocked || !canvasGroup.blocksRaycasts) return;

        // GLOBAL KÝLÝT: Artýk bir kart sürükleniyor, diðerleri Hover olamaz
        globalIsDragging = true;

        isHovering = false;

        parentToReturnTo = transform.parent;
        transform.SetParent(transform.root);
        canvasGroup.blocksRaycasts = false;

        targetScale = Vector3.one * dragScaleAmount;

        // Sürüklenen kart en üsttedir
        cardCanvas.overrideSorting = true;
        cardCanvas.sortingOrder = 200; // Hover'dan (100) bile yüksek olsun

        Vector3 mousePos;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out mousePos);
        dragOffset = transform.position - mousePos;
    }

    // --- SÜRÜKLENÝYOR ---
    public void OnDrag(PointerEventData eventData)
    {
        if (isLocked) return;
        Vector3 mousePos;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out mousePos);
        transform.position = mousePos + (Vector3)dragOffset;
    }

    // --- SÜRÜKLEME BÝTTÝ ---
    public void OnEndDrag(PointerEventData eventData)
    {
        // GLOBAL KÝLÝT AÇILDI: Artýk diðer kartlar Hover olabilir
        globalIsDragging = false;

        if (!isLocked)
        {
            canvasGroup.blocksRaycasts = true;
            transform.SetParent(parentToReturnTo);

            targetScale = Vector3.one * normalScaleAmount;
            targetY = 0f;

            // Sýralamayý normale döndür
            cardCanvas.overrideSorting = false;
            cardCanvas.sortingOrder = 0;
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