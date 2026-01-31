using UnityEngine;
using UnityEngine.UI;

public class LaneStatusDisplay : MonoBehaviour
{
    [Header("Sahne Referanslarý")]
    public Image backgroundRenderer; // Arkaplan/Çerçeve olacak Image
    public Image iconRenderer;       // Ortadaki ikon (Kilit/Kuru Kafa) olacak Image

    [Header("Sprite Ayarlarý (Resimleri Buraya Sürükle)")]
    // 1. AKTÝF DURUM (Savaþ Var)
    public Sprite activeBorderSprite; // Yanan çerçeve resmi
    public Color activeColor = Color.white; // Ýstersen rengi de açabilirsin

    // 2. KÝLÝTLÝ DURUM (Sýra Gelmedi)
    public Sprite lockedPanelSprite;  // Siyah/Karanlýk panel resmi
    public Sprite lockIconSprite;     // Kilit ikonu
    public Color lockedColor = new Color(0, 0, 0, 0.7f); // Yarý saydam siyah

    // 3. BÝTMÝÞ DURUM (Geçmiþ Savaþ)
    public Sprite finishedPanelSprite;// Gri/Sönmüþ panel resmi
    public Sprite finishedIconSprite; // Kuru kafa veya X ikonu
    public Color finishedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Gri

    public void SetState(LaneState state)
    {
        switch (state)
        {
            case LaneState.Locked:
                // Arkaplaný ayarla
                backgroundRenderer.sprite = lockedPanelSprite;
                backgroundRenderer.color = lockedColor;

                // Ýkonu ayarla
                if (lockIconSprite != null)
                {
                    iconRenderer.gameObject.SetActive(true);
                    iconRenderer.sprite = lockIconSprite;
                }
                else iconRenderer.gameObject.SetActive(false);
                break;

            case LaneState.Active:
                // Arkaplaný ayarla (Çerçeve)
                backgroundRenderer.sprite = activeBorderSprite;
                backgroundRenderer.color = activeColor;

                // Aktifken ortada ikon istemiyoruz, kapat
                iconRenderer.gameObject.SetActive(false);
                break;

            case LaneState.Finished:
                // Arkaplaný ayarla
                backgroundRenderer.sprite = finishedPanelSprite;
                backgroundRenderer.color = finishedColor;

                // Ýkonu ayarla
                if (finishedIconSprite != null)
                {
                    iconRenderer.gameObject.SetActive(true);
                    iconRenderer.sprite = finishedIconSprite;
                }
                else iconRenderer.gameObject.SetActive(false);
                break;
        }
    }
}
public enum LaneState
{
    Locked,
    Active,
    Finished
}