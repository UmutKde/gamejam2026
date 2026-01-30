using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Transform parentToReturnTo = null;
    public bool isLocked = false;

    [Header("Kart Ayarlarý")]
    public bool belongsToPlayerOne = true;
    public Sprite cardFrontSprite;
    public Sprite cardBackSprite;

    private Image cardImage;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        cardImage = GetComponent<Image>();

        if (cardFrontSprite == null) cardFrontSprite = cardImage.sprite;
    }

    void Start()
    {
        UpdateCardVisualsAndState();
    }

    public void UpdateCardVisualsAndState()
    {
        if (isLocked) return;

        bool isMyTurn = false;

        if (TurnManager.Instance.isPlayerOneTurn && belongsToPlayerOne)
        {
            isMyTurn = true;
        }
        else if (!TurnManager.Instance.isPlayerOneTurn && !belongsToPlayerOne)
        {
            isMyTurn = true;
        }

        if (isMyTurn)
        {
            cardImage.sprite = cardFrontSprite;
            canvasGroup.blocksRaycasts = true;
        }
        else
        {
            cardImage.sprite = cardBackSprite;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        bool isMyTurn = (TurnManager.Instance.isPlayerOneTurn == belongsToPlayerOne);
        if (!isMyTurn || isLocked) return;

        parentToReturnTo = transform.parent;
        transform.SetParent(transform.root);
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        bool isMyTurn = (TurnManager.Instance.isPlayerOneTurn == belongsToPlayerOne);
        if (!isMyTurn || isLocked) return;
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
        cardImage.sprite = cardFrontSprite;
    }
}