using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    // DÝKKAT: Artýk 'TextMeshProUGUI' kullanýyoruz (UI için)
    public TextMeshProUGUI textMesh;

    public float moveSpeed = 100f; // UI olduðu için Pixel cinsinden hýz (Deðeri artýrdýk)
    public float disappearSpeed = 3f;
    public float lifeTime = 1f;

    private Color textColor;
    private float disappearTimer;

    // ... (Start fonksiyonunu silebilirsin, UI'da rotasyon sorun olmaz) ...

    public void Setup(int damageAmount, ElementLogic.DamageInteraction interactionType)
    {
        textMesh.text = damageAmount.ToString();
        disappearTimer = lifeTime;

        switch (interactionType)
        {
            case ElementLogic.DamageInteraction.Neutral:
                textColor = Color.white;
                textMesh.fontSize = 36; // UI olduðu için fontu büyüt
                break;

            case ElementLogic.DamageInteraction.Advantage:
                textColor = Color.yellow;
                textMesh.fontSize = 45;
                break;

            case ElementLogic.DamageInteraction.Dominance:
                textColor = Color.red;
                textMesh.fontSize = 55;
                textMesh.fontStyle = FontStyles.Bold;
                break;
        }

        textMesh.color = textColor;
    }

    void Update()
    {
        // Yukarý Hareket
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // Kaybolma
        disappearTimer -= Time.deltaTime;
        if (disappearTimer < 0)
        {
            textColor.a -= disappearSpeed * Time.deltaTime;
            textMesh.color = textColor;

            if (textColor.a <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}