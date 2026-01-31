using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MinionCardDisplay : MonoBehaviour
{
    [Header("Kart Verisi")]
    public CardData cardData; // ScriptableObject buraya gelecek

    [Header("UI Bağlantıları (Sürükle-Bırak)")]
    public TextMeshProUGUI nameText;    // İsim Yazısı
    public TextMeshProUGUI attackText;   // Sol Alt (Saldırı)
    public TextMeshProUGUI healthText;  // Sağ Alt (Can)
    
    public Image minionImage;       // Ortadaki Canavar Resmi
    public Image elementIconImage;  // Alt Ortadaki Element İkonu

    [Header("Kart Arkası (Maske)")]
    public GameObject cardBackObject; // Hiyerarşideki "CardBack" objesi

    // Inspector'da Element ve İkon eşleştirmesi yapmak için yapı
    [System.Serializable]
    public struct ElementToSprite
    {
        public ElementTypes type;
        public Sprite icon;
    }

    [Header("Element İkon Ayarları")]
    public List<ElementToSprite> elementIcons; // Listeyi Inspector'dan doldur

    // Kart ilk yaratıldığında çalışacak fonksiyon
    public void Setup(CardData data)
    {
        cardData = data;
        UpdateUI();
        SetFaceDown(false); // Başlangıçta kartın önü açık olsun
    }

    // Inspector'da bir şeyi değiştirince anlık görmek için
    void OnValidate()
    {
        UpdateUI();
    }

    // Görüntüleri yenileme fonksiyonu
    void UpdateUI()
    {
        if (cardData == null) return;

        // 1. Yazıları Güncelle
        if (nameText != null) nameText.text = cardData.cardName;
        if (attackText != null) attackText.text = cardData.attackPoint.ToString();
        if (healthText != null) healthText.text = cardData.healthPoint.ToString();

        // 2. Minyon Resmini Güncelle
        if (minionImage != null && cardData.cardImage != null)
        {
            minionImage.sprite = cardData.cardImage;
            // Resmin oranını koruması için (sündürmemesi için)
            minionImage.preserveAspect = true; 
        }

        // 3. Element İkonunu Bul ve Yerleştir
        if (elementIconImage != null && elementIcons != null)
        {
            foreach (var item in elementIcons)
            {
                if (item.type == cardData.element)
                {
                    elementIconImage.sprite = item.icon;
                    elementIconImage.enabled = true; // Resim varsa görünür yap
                    break;
                }
            }
        }
    }

    // Kartın arkasını çevirme / açma fonksiyonu
    public void SetFaceDown(bool isFaceDown)
    {
        if (cardBackObject != null)
        {
            // true gelirse (kapalı), Maske objesini aç.
            // false gelirse (açık), Maske objesini kapat.
            cardBackObject.SetActive(isFaceDown);
        }
    }
    // Anlık can değişimi için harici fonksiyon
    public void UpdateHealthUI(int currentHealth)
    {
        if (healthText != null)
        {
            healthText.text = currentHealth.ToString();
        }
    }
}