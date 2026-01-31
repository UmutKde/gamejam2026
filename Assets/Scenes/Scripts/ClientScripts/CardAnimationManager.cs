using UnityEngine;
using System.Collections;

public class CardAnimationManager : MonoBehaviour
{
    public static CardAnimationManager Instance;

    [Header("Ayarlar")]
    public Transform demonSpawnPoint; // Þeytanýn aðzýndaki nokta
    public float drawDuration = 0.7f; // Ele gelme süresi (Biraz uzattýk, süzülsün)

    [Header("Yayýn Þekli")]
    // Yayýn tepe noktasý baþlangýçla bitiþin neresinde olsun? (0.3 = %30'da, þeytana yakýn)
    [Range(0.1f, 0.9f)] public float arcPeakBias = 0.3f;
    public float arcHeight = 150f; // Yayýn yukarý bombesi
    public float arcDepth = -100f;  // Yayýn ekrana doðru bombesi (Negatif = bize doðru)

    [Header("Rotasyon Efekti")]
    // Kart çýkarken nasýl dönük olsun?
    public Vector3 startRotationEuler = new Vector3(0, 0, 90);

    void Awake()
    {
        Instance = this;
    }

    public void AnimateCardToHand(GameObject cardObj, Transform handTransform)
    {
        StartCoroutine(DrawCardRoutine(cardObj, handTransform));
    }

    private IEnumerator DrawCardRoutine(GameObject card, Transform targetHand)
    {
        // 1. BAÞLANGIÇ HAZIRLIÐI
        Transform originalParent = card.transform.parent;
        // Kartý Canvas'ýn en üstüne al ki her þeyin üstünden uçsun
        card.transform.SetParent(targetHand.root);

        Vector3 startPos = demonSpawnPoint.position;
        Vector3 endPos = targetHand.position; // Elin o anki merkezi

        // Baþlangýç durumu: Þeytanýn orada, sýfýr boyut, açýlý rotasyon
        card.transform.position = startPos;
        card.transform.localScale = Vector3.zero;
        card.transform.rotation = Quaternion.Euler(startRotationEuler);
        Quaternion endRotation = Quaternion.identity; // Bitiþ rotasyonu (Düz)

        // --- GELÝÞMÝÞ KONTROL NOKTASI HESABI ---
        // Tepe noktasýný tam ortaya deðil, 'arcPeakBias' kadar baþlangýca yakýn bir yere koyuyoruz.
        // Bu, kartýn aniden fýrlayýp sonra süzülmesi hissini verir.
        Vector3 baseControlPoint = Vector3.Lerp(startPos, endPos, arcPeakBias);

        // Yükseklik ve Derinlik ekle
        // Not: UI'da Z ekseni negatif oldukça kameraya yaklaþýr (Overlay ise deðiþir, deneyerek bulacaðýz)
        Vector3 controlPoint = baseControlPoint;
        controlPoint.y += arcHeight;
        controlPoint.z += arcDepth;


        // 2. ANÝMASYON DÖNGÜSÜ
        float elapsed = 0f;
        while (elapsed < drawDuration)
        {
            float t = elapsed / drawDuration;
            // SmoothStep: Hem baþta hem sonda yumuþak hýzlanma/yavaþlama
            t = t * t * (3f - 2f * t);

            // A) HAREKET (Quadratic Bezier)
            Vector3 m1 = Vector3.Lerp(startPos, controlPoint, t);
            Vector3 m2 = Vector3.Lerp(controlPoint, endPos, t);
            card.transform.position = Vector3.Lerp(m1, m2, t);

            // B) BÜYÜME (Scale Up) - Baþta hýzlý büyüsün
            float scaleT = Mathf.Sin(t * Mathf.PI * 0.5f); // EaseOut
            card.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, scaleT);

            // C) DÖNME (Baþlangýç açýsýndan düze doðru)
            card.transform.rotation = Quaternion.Lerp(Quaternion.Euler(startRotationEuler), endRotation, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 3. BÝTÝÞ
        // Kartý hedefe tam oturt ve düzelt
        card.transform.position = endPos;
        card.transform.localScale = Vector3.one;
        card.transform.rotation = endRotation;

        // Kartý gerçek sahibine (Elin Layout'una) teslim et
        card.transform.SetParent(targetHand);

        // Layout'un anýnda güncellemesi için (Gerekirse)
        // LayoutRebuilder.ForceRebuildLayoutImmediate(targetHand.GetComponent<RectTransform>());
    }
}