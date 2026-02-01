using UnityEngine;
using UnityEngine.UI;

public class MaskCardDisplay : MonoBehaviour
{
    [Header("Kimlik Bilgileri")]
    public int maskUniqueId;   // Bu maskenin o anki benzersiz ID'si (Örn: 1001)
    public int ownerId;        // Kime ait? (1 veya 2)
    public CardData myData;    // Hangi element?

    [Header("Görsel")]
    public Image iconImage;

    // Manager bu fonksiyonu çağırıp maskeyi yaratacak
    public void SetupMask(int uniqueId, int owner, CardData data)
    {
        this.maskUniqueId = uniqueId;
        this.ownerId = owner;
        this.myData = data;

        if (iconImage != null && data != null)
        {
            iconImage.sprite = data.cardImage;
            // Görseli görünür yap (Alpha ayarı)
            var color = iconImage.color;
            color.a = 1f;
            iconImage.color = color;
        }
    }
}