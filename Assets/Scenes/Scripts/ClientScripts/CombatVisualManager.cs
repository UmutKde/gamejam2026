using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CombatVisualManager : MonoBehaviour
{
    public static CombatVisualManager Instance;

    [Header("Ayarlar")]
    public float animDuration = 0.3f; // Vuruş hızı
    public float clashImpactScale = 1.6f; // Vuruş anındaki büyüklük

    [Header("Efektler")]
    public GameObject clashEffectPrefab;

    void Awake()
    {
        Instance = this;
    }

    public void StartClashAnimation(
        Draggable card1, Draggable card2,
        bool p1Dies, bool p2Dies,
        int p1Damage, int p1Type,
        int p2Damage, int p2Type
    )
    {
        StartCoroutine(ClashRoutine(card1, card2, p1Dies, p2Dies, p1Damage, p1Type, p2Damage, p2Type));
    }

    private IEnumerator ClashRoutine(
        Draggable card1, Draggable card2,
        bool p1Dies, bool p2Dies,
        int p1Damage, int p1Type,
        int p2Damage, int p2Type
    )
    {
        // 1. ORİJİNAL KONUMLARI KAYDET
        // "true" parametresi world position'ı koru demektir.
        Transform card1OriginalParent = card1.transform.parent;
        Transform card2OriginalParent = card2.transform.parent;

        Vector3 card1StartPos = card1.transform.position; // Şu anki Dünya Konumu
        Vector3 card2StartPos = card2.transform.position;
        Quaternion card1StartRot = card1.transform.rotation;
        Quaternion card2StartRot = card2.transform.rotation;

        // --- KRİTİK ADIM: EBEVEYNDEN KURTARMA ---
        // Kartları slotlardan çıkarıp "Canvas"ın (Root) direkt çocuğu yapıyoruz.
        // Bu sayede koordinat sistemi evrenselleşir ve en üstte görünürler.
        card1.transform.SetParent(card1.transform.root, true);
        card2.transform.SetParent(card2.transform.root, true);

        card1.isAnimatingCombat = true;
        card2.isAnimatingCombat = true;

        // Raycast'i kapat (Tıklanmasınlar)
        CanvasGroup cg1 = card1.GetComponent<CanvasGroup>();
        CanvasGroup cg2 = card2.GetComponent<CanvasGroup>();
        if (cg1) cg1.blocksRaycasts = false;
        if (cg2) cg2.blocksRaycasts = false;

        // --- MERKEZİ BULMA (EN SAĞLAM YÖNTEM) ---
        // Artık "transform.root" Canvas olduğu için, onun pozisyonu tam olarak ekranın ortasıdır.
        // Z eksenini kartların görünürlüğü için sabitliyoruz.
        Vector3 combatCenter = card1.transform.root.position;
        combatCenter.z = 0; // UI olduğu için Z'yi sıfırla

        // --- DOTWEEN SEQUENCE ---
        Sequence clashSeq = DOTween.Sequence();

        // AŞAMA 1: GERİLME (Mevcut konumlarından geriye doğru yaylanma)
        // Kart kendi konumundan, merkeze zıt yöne hafifçe çekilir.
        Vector3 dir1 = (card1StartPos - combatCenter).normalized; // Merkezden dışarı yön
        Vector3 dir2 = (card2StartPos - combatCenter).normalized;

        clashSeq.Append(card1.transform.DOMove(card1StartPos + (dir1 * 100f), animDuration).SetEase(Ease.OutQuad));
        clashSeq.Join(card2.transform.DOMove(card2StartPos + (dir2 * 100f), animDuration).SetEase(Ease.OutQuad));

        // Hafif Dönme
        clashSeq.Join(card1.transform.DORotate(new Vector3(0, 0, 15f), animDuration));
        clashSeq.Join(card2.transform.DORotate(new Vector3(0, 0, -15f), animDuration));

        // Büyüme
        clashSeq.Join(card1.transform.DOScale(Vector3.one * clashImpactScale * 0.8f, animDuration));
        clashSeq.Join(card2.transform.DOScale(Vector3.one * clashImpactScale * 0.8f, animDuration));

        // AŞAMA 2: ÇARPIŞMA (Merkezde Buluşma)
        clashSeq.Append(card1.transform.DOMove(combatCenter, 0.15f).SetEase(Ease.InBack)); // InBack = Gerilip vurma
        clashSeq.Join(card2.transform.DOMove(combatCenter, 0.15f).SetEase(Ease.InBack));

        // Dönmeyi düzelt
        clashSeq.Join(card1.transform.DORotate(Vector3.zero, 0.15f));
        clashSeq.Join(card2.transform.DORotate(Vector3.zero, 0.15f));

        // Tam Boyut
        clashSeq.Join(card1.transform.DOScale(Vector3.one * clashImpactScale, 0.15f));
        clashSeq.Join(card2.transform.DOScale(Vector3.one * clashImpactScale, 0.15f));

        // AŞAMA 3: EFEKTLER VE HASAR (Callback)
        clashSeq.AppendCallback(() => {
            // Sarsıntı
            Camera.main.transform.DOShakePosition(0.4f, 10f, 20, 90, false, true);

            // Efekt
            if (clashEffectPrefab) Instantiate(clashEffectPrefab, combatCenter, Quaternion.identity, card1.transform.root);

            // Hasar Yazıları
            if (FloatingTextManager.Instance != null)
            {
                // P1'in yediği hasar (Biraz sola)
                FloatingTextManager.Instance.ShowDamage(combatCenter + new Vector3(-150, 150, 0), p1Damage, (ElementLogic.DamageInteraction)p1Type);
                // P2'nin yediği hasar (Biraz sağa)
                FloatingTextManager.Instance.ShowDamage(combatCenter + new Vector3(150, 150, 0), p2Damage, (ElementLogic.DamageInteraction)p2Type);
            }
        });

        // AŞAMA 4: GERİ TEPME (Recoil)
        clashSeq.Append(card1.transform.DOMove(card1StartPos, 0.25f).SetEase(Ease.OutBack));
        clashSeq.Join(card2.transform.DOMove(card2StartPos, 0.25f).SetEase(Ease.OutBack));

        // AŞAMA 5: SONUÇLARI GÖRMEK İÇİN BEKLE
        clashSeq.AppendInterval(0.5f);

        yield return clashSeq.WaitForCompletion();

        // --- SONUÇ (EVE DÖNÜŞ veya ÖLÜM) ---

        // P1 İşlemleri
        if (p1Dies)
        {
            card1.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack);
        }
        else
        {
            // Ölmediyse eski boyutuna ve ebeveynine dönmek için hazırlan
            card1.transform.DOScale(Vector3.one * 1.0f, 0.3f);
            // Parent'a dönüşü aşağıda yapacağız
        }

        // P2 İşlemleri
        if (p2Dies)
        {
            card2.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack);
        }
        else
        {
            card2.transform.DOScale(Vector3.one * 1.0f, 0.3f);
        }

        yield return new WaitForSeconds(0.3f);

        // --- TEMİZLİK VE YERİNE KOYMA ---

        if (!p1Dies)
        {
            // Eski ebeveynine geri ver (Slotuna oturt)
            card1.transform.SetParent(card1OriginalParent, true);
            // Pozisyonu sıfırla (Slotun tam ortasına otursun)
            card1.transform.localPosition = Vector3.zero;
            card1.transform.localRotation = Quaternion.identity;

            card1.isAnimatingCombat = false;
        }

        if (!p2Dies)
        {
            card2.transform.SetParent(card2OriginalParent, true);
            card2.transform.localPosition = Vector3.zero;
            card2.transform.localRotation = Quaternion.identity;

            card2.isAnimatingCombat = false;
        }

        // Raycastleri geri aç (Draggable scripti zaten Update'te kontrol ediyor ama garanti olsun)
        if (cg1) cg1.blocksRaycasts = true;
        if (cg2) cg2.blocksRaycasts = true;
    }
}