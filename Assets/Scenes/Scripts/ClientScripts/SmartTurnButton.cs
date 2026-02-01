using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro kullanýyorsan
using DG.Tweening;

public class SmartTurnButton : MonoBehaviour
{
    [Header("Bileþenler")]
    public Button myButton;
    public TextMeshProUGUI buttonText; // Eðer standart Text ise 'Text' yap
    public Image buttonImage; // Butonun arkaplan görseli

    [Header("Ayarlar")]
    public string myTurnText = "TURU BÝTÝR";
    public string enemyTurnText = "RAKÝP BEKLENÝYOR...";

    public Color myTurnColor = new Color(0f, 0.8f, 0f); // Canlý Yeþil
    public Color enemyTurnColor = Color.gray; // Sönük Gri

    private bool isMyTurnAnimating = false;
    private Tween pulseTween;

    void Start()
    {
        // Otomatik bulmaya çalýþ
        if (myButton == null) myButton = GetComponent<Button>();
        if (buttonImage == null) buttonImage = GetComponent<Image>();
        if (buttonText == null) buttonText = GetComponentInChildren<TextMeshProUGUI>();
    }

    void Update()
    {
        // Gerekli yöneticiler sahnede var mý?
        if (GameManager.Instance == null) return;
        var pm = FindFirstObjectByType<PlayerManager>();
        if (pm == null) return;

        // Kontrol: Sýra bende mi?
        bool isMyTurn = (GameManager.Instance.currentTurn == pm.myPlayerId);

        UpdateVisuals(isMyTurn);
    }

    void UpdateVisuals(bool isMyTurn)
    {
        // 1. Durum deðiþmediyse iþlem yapma (Performans)
        if (isMyTurn == isMyTurnAnimating) return;
        isMyTurnAnimating = isMyTurn;

        if (isMyTurn)
        {
            // --- BENÝM SIRAM ---
            myButton.interactable = true; // Týklanabilir
            if (buttonText) buttonText.text = myTurnText;
            if (buttonImage) buttonImage.color = myTurnColor;

            // NEFES ALMA ANÝMASYONU (PULSE)
            // Önce varsa eskiyi öldür
            if (pulseTween != null) pulseTween.Kill();

            // Büyü ve Küçül (Sonsuz Döngü)
            transform.localScale = Vector3.one;
            pulseTween = transform.DOScale(1.1f, 0.8f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
        else
        {
            // --- RAKÝP SIRASI ---
            myButton.interactable = false; // Týklanamaz
            if (buttonText) buttonText.text = enemyTurnText;
            if (buttonImage) buttonImage.color = enemyTurnColor;

            // Animasyonu durdur ve normal boyuta dön
            if (pulseTween != null) pulseTween.Kill();
            transform.DOScale(1f, 0.3f);
        }
    }
}