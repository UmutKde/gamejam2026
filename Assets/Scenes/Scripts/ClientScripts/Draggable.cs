using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    // --- GLOBAL DURUMLAR ---
    public static bool globalIsDragging = false;
    public bool isRevealed = false;
    public bool isAnimatingCombat = false;

    // --- KİMLİK VE SAHİPLİK ---
    [Header("Kimlik")]
    public int cardId; // Minyonlar için Unique ID
    public int ownerId; // 1 veya 2

    [Header("Durum")]
    public bool isOwnedByClient; // Bu istemciye mi ait? (LAN için)
    public bool isPlayedOnBoard = false;
    public bool isLocked = false; // Hareket kilitli mi?

    // --- GÖRSEL REFERANSLAR ---
    private MinionCardDisplay minionDisplay; // Minyon Görseli (Varsa)
    private MaskCardDisplay maskDisplay;     // Maske Görseli (Varsa)
    private CanvasGroup canvasGroup;
    private Canvas cardCanvas;
    private RectTransform rectTransform;

    // --- ANİMASYON AYARLARI ---
    [Header("Animasyon Ayarları")]
    public float hoverScaleAmount = 1.3f;
    public float hoverHeightAmount = 50f;
    public float dragScaleAmount = 1.1f;
    public float playedScaleAmount = 1.2f;
    public float normalScaleAmount = 1.0f;
    public float animSpeed = 15f;

    public Transform parentToReturnTo = null;
    private Vector3 targetScale;
    private float targetY;
    private Vector2 dragOffset;
    private bool isHovering = false;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();

        // Kartın tipine göre componentleri almaya çalış
        minionDisplay = GetComponent<MinionCardDisplay>();
        maskDisplay = GetComponent<MaskCardDisplay>();

        cardCanvas = GetComponent<Canvas>();
        if (cardCanvas == null)
        {
            cardCanvas = gameObject.AddComponent<Canvas>();
            // Raycaster ekle ki tutulabilsin
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
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

    public void RevealCard()
    {
        isRevealed = true;
        UpdateCardVisualsAndState();
    }

    void Update()
    {
        // YENİ: Her frame'de durumunu kontrol et (Hata riskini sıfıra indirir)
        if (!globalIsDragging && !isAnimatingCombat)
        {
            UpdateCardVisualsAndState();
        }

        if (isAnimatingCombat) return;

        // ... (Kalan animasyon kodları aynı) ...
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animSpeed);

        if (!isLocked && canvasGroup.blocksRaycasts && !isPlayedOnBoard)
        {
            float currentX = transform.localPosition.x;
            float newY = Mathf.Lerp(transform.localPosition.y, targetY, Time.deltaTime * animSpeed);
            transform.localPosition = new Vector3(currentX, newY, 0);
        }
    }

    public void UpdateCardVisualsAndState()
    {
        if (maskDisplay != null) return;

        bool shouldFaceDown = true;
        bool canInteract = false;

        // MASADAKİ KARTLAR
        if (isPlayedOnBoard)
        {
            canInteract = false; // Masadaki karta dokunulmaz

            if (isRevealed) shouldFaceDown = false; // Savaşta açıldıysa açık kal
            else shouldFaceDown = !isOwnedByClient; // Değilse sadece sahibi görsün
        }
        // ELDEKİ KARTLAR
        else
        {
            // 1. GÖRÜNÜRLÜK: Benim kartımsa her zaman açık, rakibinkiyse kapalı
            shouldFaceDown = !isOwnedByClient;

            // 2. ETKİLEŞİM: Benim kartımsa VE Sıra bendeyse dokunabilirim
            // GameManager.currentTurn 1 veya 2 döner. ownerId de 1 veya 2'dir.
            if (isOwnedByClient && GameManager.Instance.currentTurn == ownerId)
            {
                canInteract = true;
            }
        }

        // Raycast (Dokunma) ayarı
        canvasGroup.blocksRaycasts = canInteract;

        // Yüzünü dön
        if (minionDisplay != null) minionDisplay.SetFaceDown(shouldFaceDown);

        // Hover animasyonu sıfırlama (Eğer kilitlendiyse havada kalmasın)
        if (!canInteract && !isPlayedOnBoard && !isHovering) targetY = 0f;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isLocked || !canvasGroup.blocksRaycasts || globalIsDragging) return;

        // Maske veya İstemcinin Minyonu ise büyüt
        if (maskDisplay != null || (isOwnedByClient && !isPlayedOnBoard))
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

        if (maskDisplay != null || (isOwnedByClient && !isPlayedOnBoard))
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

        // 1. GÜVENLİK: MASKE İSE
        if (maskDisplay != null)
        {
            if (maskDisplay.ownerId != GameManager.Instance.currentTurn)
            {
                Debug.LogWarning("Sıra sende değil, maskeyi oynayamazsın!");
                return;
            }
        }
        // 2. GÜVENLİK: MİNYON İSE
        else
        {
            if (this.ownerId != GameManager.Instance.currentTurn)
            {
                Debug.LogWarning("Sıra sende değil, kart oynayamazsın!");
                return;
            }
        }

        // Sürükleme Başlıyor
        globalIsDragging = true;
        isHovering = false;

        parentToReturnTo = transform.parent;
        transform.SetParent(transform.root); // En üste taşı
        canvasGroup.blocksRaycasts = false; // Raycast'i kapat ki altı görebilelim
        targetScale = Vector3.one * dragScaleAmount;

        cardCanvas.overrideSorting = true;
        cardCanvas.sortingOrder = 200; // En önde çizilsin

        Vector3 mousePos;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out mousePos);
        dragOffset = transform.position - mousePos;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isLocked || !globalIsDragging) return;

        Vector3 mousePos;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out mousePos);
        transform.position = mousePos + (Vector3)dragOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!globalIsDragging) return;
        globalIsDragging = false;

        bool isMask = (maskDisplay != null);

        if (isMask)
        {
            HandleMaskDrop(eventData);
        }
        else
        {
            HandleMinionDrop(eventData);
        }
    }

    void HandleMaskDrop(PointerEventData eventData)
    {
        GameObject droppedObj = eventData.pointerEnter;

        if (droppedObj != null && (droppedObj.name == "DemonZone" || droppedObj.CompareTag("DemonZone")))
        {
            Debug.Log($"Maske Şeytana verildi!");

            // --- DEĞİŞİKLİK BURADA ---
            // Direkt GameManager yerine PlayerManager üzerinden istek yolluyoruz

            // Sahnedeki kendi PlayerManager'ını bul (veya statik instance varsa onu kullan)
            PlayerManager[] managers = FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);
            foreach (var pm in managers)
            {
                if (pm.myPlayerId == maskDisplay.ownerId) // Kartın sahibiyle eşleşen yönetici
                {
                    pm.AttemptSacrificeMask(maskDisplay.myData.element);
                    break;
                }
            }
            // -------------------------

            Destroy(gameObject);
        }
        else
        {
            ResetToHand();
        }
    }

    void HandleMinionDrop(PointerEventData eventData)
    {
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
                    myManager.AttemptPlayCard(this, foundIndex);
                    return;
                }
            }
        }

        // Eğer buraya geldiyse kart oynanamamıştır
        if (!isLocked) ResetToHand(); // İsim değişti
    }

    public void ResetToHand()
    {
        canvasGroup.blocksRaycasts = true;
        transform.SetParent(parentToReturnTo);
        targetScale = Vector3.one * normalScaleAmount;
        targetY = 0f;

        if (cardCanvas != null)
        {
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