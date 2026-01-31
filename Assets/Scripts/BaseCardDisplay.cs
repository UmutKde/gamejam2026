using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Bu sınıf tek başına kullanılmaz, diğerleri bundan türetilir (abstract)
public abstract class BaseCardDisplay : MonoBehaviour
{
    public CardData cardData;

    [Header("Ortak UI Bağlantıları")]
    public TextMeshProUGUI nameText;
    public Image cardImage;
    public Image elementIcon; 
    public Image frameImage; // Çerçeve
    public GameObject cardBack; // Kart arkası (Maske)

    public virtual void Setup(CardData data)
    {
        cardData = data;
        
        // 1. İsim ve Resim
        if(nameText) nameText.text = cardData.cardName;
        if(cardImage) cardImage.sprite = cardData.cardImage;

        // 2. Element Rengi veya İkonu (Basitçe rengini değiştiriyoruz örnek olarak)
        UpdateElementVisuals();

        // 3. Kartı Başlangıçta Açık Yap
        if(cardBack) cardBack.SetActive(false);
    }

    // Her kart tipi elementi farklı gösterebilir, o yüzden virtual yaptık
    protected virtual void UpdateElementVisuals()
    {
        // Burada senin element ikon mantığın devreye girecek
        // Şimdilik basit renk kodu:
        if (frameImage)
        {
            switch (cardData.element)
            {
                case ElementTypes.Fire: frameImage.color = Color.red; break;
                case ElementTypes.Water: frameImage.color = Color.blue; break;
                // Diğerleri...
                default: frameImage.color = Color.white; break;
            }
        }
    }
}