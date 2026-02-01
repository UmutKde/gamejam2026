using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CombatVisualManager : MonoBehaviour
{
    public static CombatVisualManager Instance;

    [Header("Ayarlar")]
    public float animDuration = 0.25f;
    public float clashImpactScale = 1.6f;

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
        // 1. EBEVEYN VE BAŞLANGIÇ KONUMLARI
        Transform card1OriginalParent = card1.transform.parent;
        Transform card2OriginalParent = card2.transform.parent;

        Vector3 card1StartPos = card1.transform.position;
        Vector3 card2StartPos = card2.transform.position;

        // EBEVEYNDEN KURTAR (Canvas'ın tepesine al)
        card1.transform.SetParent(card1.transform.root, true);
        card2.transform.SetParent(card2.transform.root, true);

        // Raycast kapat
        CanvasGroup cg1 = card1.GetComponent<CanvasGroup>();
        CanvasGroup cg2 = card2.GetComponent<CanvasGroup>();
        if (cg1) cg1.blocksRaycasts = false;
        if (cg2) cg2.blocksRaycasts = false;

        card1.isAnimatingCombat = true;
        card2.isAnimatingCombat = true;

        // MERKEZ NOKTASI
        Vector3 combatCenter = card1.transform.root.position;
        combatCenter.z = 0;

        // --- SEQUENCE BAŞLIYOR ---
        Sequence clashSeq = DOTween.Sequence();

        // AŞAMA 1: GERİLME (Anticipation)
        Vector3 dir1 = (card1StartPos - combatCenter).normalized;
        Vector3 dir2 = (card2StartPos - combatCenter).normalized;

        clashSeq.Append(card1.transform.DOMove(card1StartPos + (dir1 * 50f), animDuration).SetEase(Ease.OutQuad));
        clashSeq.Join(card2.transform.DOMove(card2StartPos + (dir2 * 50f), animDuration).SetEase(Ease.OutQuad));

        // Hafif Dönme
        clashSeq.Join(card1.transform.DORotate(new Vector3(0, 0, 15f), animDuration));
        clashSeq.Join(card2.transform.DORotate(new Vector3(0, 0, -15f), animDuration));

        // Büyüme
        clashSeq.Join(card1.transform.DOScale(Vector3.one * clashImpactScale * 0.8f, animDuration));
        clashSeq.Join(card2.transform.DOScale(Vector3.one * clashImpactScale * 0.8f, animDuration));

        // AŞAMA 2: VURUŞ (Impact)
        clashSeq.Append(card1.transform.DOMove(combatCenter, 0.1f).SetEase(Ease.InBack));
        clashSeq.Join(card2.transform.DOMove(combatCenter, 0.1f).SetEase(Ease.InBack));

        clashSeq.Join(card1.transform.DORotate(Vector3.zero, 0.1f));
        clashSeq.Join(card2.transform.DORotate(Vector3.zero, 0.1f));

        clashSeq.Join(card1.transform.DOScale(Vector3.one * clashImpactScale, 0.1f));
        clashSeq.Join(card2.transform.DOScale(Vector3.one * clashImpactScale, 0.1f));

        // AŞAMA 3: EFEKT & HASAR (Callback)
        clashSeq.AppendCallback(() => {
            Camera.main.transform.DOShakePosition(0.4f, 10f, 20, 90, false, true);
            if (clashEffectPrefab) Instantiate(clashEffectPrefab, combatCenter, Quaternion.identity, card1.transform.root);

            if (FloatingTextManager.Instance != null)
            {
                // DÜZELTME: Ofsetleri azalttık ve kartların merkezine yaklaştırdık.
                // Yükseklik için sadece +50 (veya Canvas ölçeğine göre +100) yeterli.
                // X ekseninde hafif ayırma yapıyoruz ki sayılar üst üste binmesin.

                Vector3 p1TextPos = combatCenter + new Vector3(-60f, 50f, 0); // P1 Kartının az solu ve üstü
                Vector3 p2TextPos = combatCenter + new Vector3(60f, 50f, 0);  // P2 Kartının az sağı ve üstü

                FloatingTextManager.Instance.ShowDamage(p1TextPos, p1Damage, (ElementLogic.DamageInteraction)p1Type);
                FloatingTextManager.Instance.ShowDamage(p2TextPos, p2Damage, (ElementLogic.DamageInteraction)p2Type);
            }
        });

        // AŞAMA 4: GERİ TEPME (Recoil) - Fiziksel Savrulma
        // Çarpışmanın etkisiyle başladıkları yerin biraz önüne savrulurlar
        Vector3 recoilPos1 = card1StartPos + (combatCenter - card1StartPos).normalized * 100f;
        Vector3 recoilPos2 = card2StartPos + (combatCenter - card2StartPos).normalized * 100f;

        clashSeq.Append(card1.transform.DOMove(recoilPos1, 0.2f).SetEase(Ease.OutBack)); // OutBack ile yaylanarak durur
        clashSeq.Join(card2.transform.DOMove(recoilPos2, 0.2f).SetEase(Ease.OutBack));

        // SONUÇLARI GÖRMEK İÇİN BEKLEME (Havada Asılı Kalma)
        clashSeq.AppendInterval(0.6f);

        yield return clashSeq.WaitForCompletion();

        // --- AŞAMA 5: FİNAL (ÖLÜM VEYA YUVAYA SÜZÜLME) ---

        float returnDuration = 0.5f;

        // P1 İşlemleri
        if (p1Dies)
        {
            // Olduğu yerde (Recoil pozisyonunda) küçülerek yok ol
            card1.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack);
        }
        else
        {
            // --- CİLALI DÖNÜŞ ---
            // 1. Önce ebeveyne bağla ama DÜNYA POZİSYONUNU KORU (true parametresi)
            card1.transform.SetParent(card1OriginalParent, true);

            // 2. Şimdi bulunduğu o uzak noktadan (0,0,0)'a YAVAŞÇA süzül
            // Işınlanma yok, kayma var.
            card1.transform.DOLocalMove(Vector3.zero, returnDuration).SetEase(Ease.OutQuad);
            card1.transform.DOLocalRotate(Vector3.zero, returnDuration);
            card1.transform.DOScale(Vector3.one * 1.0f, returnDuration);
        }

        // P2 İşlemleri
        if (p2Dies)
        {
            card2.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack);
        }
        else
        {
            // --- CİLALI DÖNÜŞ ---
            card2.transform.SetParent(card2OriginalParent, true);

            card2.transform.DOLocalMove(Vector3.zero, returnDuration).SetEase(Ease.OutQuad);
            card2.transform.DOLocalRotate(Vector3.zero, returnDuration);
            card2.transform.DOScale(Vector3.one * 1.0f, returnDuration);
        }

        yield return new WaitForSeconds(returnDuration);

        // --- TEMİZLİK ---

        if (!p1Dies)
        {
            card1.isAnimatingCombat = false;
        }

        if (!p2Dies)
        {
            card2.isAnimatingCombat = false;
        }

        if (cg1) cg1.blocksRaycasts = true;
        if (cg2) cg2.blocksRaycasts = true;
    }
}