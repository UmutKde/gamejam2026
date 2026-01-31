using UnityEngine;
using System.Collections;

public class CombatVisualManager : MonoBehaviour
{
    public static CombatVisualManager Instance;

    [Header("Mesafe Ayarları")]
    public float contactPadding = 40f; // İç içe girmemeleri için arada kalacak boşluk
    public float windUpDistance = 30f; // Vurmadan önce ne kadar geriye çekilecek?

    [Header("Zamanlama Ayarları")]
    public float windUpDuration = 0.2f; // Geri çekilme süresi
    public float strikeDuration = 0.1f; // İleri atılma (Vuruş) süresi (Hızlı olmalı)
    public float returnDuration = 0.3f; // Geri dönme süresi
    public float clashScaleIncrease = 1.2f; // Vuruş anında büyüme oranı

    void Awake()
    {
        Instance = this;
    }

    // Yeni parametreler: p1Dies ve p2Dies (Kim ölecek?)
    public Coroutine StartClashAnimation(Draggable p1Card, Draggable p2Card, bool p1Dies, bool p2Dies)
    {
        return StartCoroutine(ClashSequence(p1Card, p2Card, p1Dies, p2Dies));
    }

    private IEnumerator ClashSequence(Draggable p1Card, Draggable p2Card, bool p1Dies, bool p2Dies)
    {
        // 1. BAŞLANGIÇ POZİSYONLARI
        Vector3 p1StartPos = p1Card.transform.position;
        Vector3 p2StartPos = p2Card.transform.position;
        Vector3 p1StartScale = p1Card.transform.localScale;
        Vector3 p2StartScale = p2Card.transform.localScale;

        // Kartları animasyon moduna al (Update karışmasın)
        if (p1Card) p1Card.isAnimatingCombat = true;
        if (p2Card) p2Card.isAnimatingCombat = true;

        // Vuruş Yönü ve Hedef Noktalar
        Vector3 direction = (p2StartPos - p1StartPos).normalized;
        float distance = Vector3.Distance(p1StartPos, p2StartPos);

        // Çarpışma noktası tam orta değil, birbirlerine "contactPadding" kadar yaklaştıkları yerdir.
        // P1 ne kadar ileri gidecek? (Toplam mesafe / 2) - (Aradaki boşluk / 2)
        float moveDistance = (distance / 2f) - (contactPadding / 2f);

        Vector3 p1StrikePos = p1StartPos + (direction * moveDistance);
        Vector3 p2StrikePos = p2StartPos - (direction * moveDistance);

        // Geri Çekilme (Wind Up) Pozisyonları
        Vector3 p1WindUpPos = p1StartPos - (direction * windUpDistance);
        Vector3 p2WindUpPos = p2StartPos + (direction * windUpDistance);

        // --- FAZ 1: GERİ ÇEKİLME (WIND UP) ---
        float elapsed = 0f;
        while (elapsed < windUpDuration)
        {
            float t = elapsed / windUpDuration;
            // EaseOut (Yavaşça geriye git)
            t = t * t * (3f - 2f * t);

            if (p1Card) p1Card.transform.position = Vector3.Lerp(p1StartPos, p1WindUpPos, t);
            if (p2Card) p2Card.transform.position = Vector3.Lerp(p2StartPos, p2WindUpPos, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // --- FAZ 2: VURUŞ (STRIKE) ---
        elapsed = 0f;
        while (elapsed < strikeDuration)
        {
            float t = elapsed / strikeDuration;
            // EaseIn (Hızlanarak vur)
            t = t * t;

            if (p1Card) p1Card.transform.position = Vector3.Lerp(p1WindUpPos, p1StrikePos, t);
            if (p2Card) p2Card.transform.position = Vector3.Lerp(p2WindUpPos, p2StrikePos, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // --- FAZ 3: ÇARPIŞMA ANI (IMPACT) ---
        // Tam vuruş noktasındayız
        if (p1Card) p1Card.transform.position = p1StrikePos;
        if (p2Card) p2Card.transform.position = p2StrikePos;

        // Vuruş Efekti (Büyüme)
        if (p1Card) p1Card.transform.localScale = p1StartScale * clashScaleIncrease;
        if (p2Card) p2Card.transform.localScale = p2StartScale * clashScaleIncrease;

        Debug.Log("💥 GÜM! 💥");

        // ÖLÜM EFEKTİ: Eğer kart ölecekse, tam çarptığı an GÖRÜNMEZ yap.
        // (Yok etmiyoruz, sadece gizliyoruz. GameLogic birazdan silecek.)
        if (p1Dies && p1Card != null) p1Card.gameObject.SetActive(false);
        if (p2Dies && p2Card != null) p2Card.gameObject.SetActive(false);

        // Vuruşun görülmesi için çok kısa bir bekleme
        yield return new WaitForSeconds(0.1f);

        // Boyutları düzelt
        if (p1Card && !p1Dies) p1Card.transform.localScale = p1StartScale;
        if (p2Card && !p2Dies) p2Card.transform.localScale = p2StartScale;

        // --- FAZ 4: GERİ DÖNÜŞ (RETURN) ---
        // Sadece hayatta kalanlar geri döner
        elapsed = 0f;
        while (elapsed < returnDuration)
        {
            float t = elapsed / returnDuration;
            // SmoothStep
            t = t * t * (3f - 2f * t);

            if (p1Card && !p1Dies) p1Card.transform.position = Vector3.Lerp(p1StrikePos, p1StartPos, t);
            if (p2Card && !p2Dies) p2Card.transform.position = Vector3.Lerp(p2StrikePos, p2StartPos, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Yerlerine sabitle
        if (p1Card && !p1Dies) p1Card.transform.position = p1StartPos;
        if (p2Card && !p2Dies) p2Card.transform.position = p2StartPos;

        // Animasyon bitti
        if (p1Card) p1Card.isAnimatingCombat = false;
        if (p2Card) p2Card.isAnimatingCombat = false;
    }
}