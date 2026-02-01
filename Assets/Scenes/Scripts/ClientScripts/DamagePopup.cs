using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    public TextMeshProUGUI textMesh;

    public float moveSpeed = 150f; // Hýzý biraz artýrdým
    public float disappearSpeed = 3f;
    public float lifeTime = 1f;

    private Color textColor;
    private float disappearTimer;

    public void Setup(int damageAmount, ElementLogic.DamageInteraction interactionType)
    {
        textMesh.text = damageAmount.ToString();
        disappearTimer = lifeTime;

        switch (interactionType)
        {
            case ElementLogic.DamageInteraction.Neutral:
                textColor = Color.white;
                textMesh.fontSize = 65; // ESKÝSÝ: 36 -> BÜYÜDÜ
                break;

            case ElementLogic.DamageInteraction.Advantage:
                textColor = Color.yellow;
                textMesh.fontSize = 85; // ESKÝSÝ: 45 -> BÜYÜDÜ
                break;

            case ElementLogic.DamageInteraction.Dominance:
                textColor = Color.red;
                textMesh.fontSize = 110; // ESKÝSÝ: 55 -> KOCAMAN OLDU
                textMesh.fontStyle = FontStyles.Bold;
                break;
        }

        textMesh.color = textColor;
    }

    void Update()
    {
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

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