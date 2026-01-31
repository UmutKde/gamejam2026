using UnityEngine;

public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance;

    public GameObject damagePopupPrefab;

    // YENÝ: Popup'lar nerenin içine doðacak? (Canvas veya bir Panel)
    public Transform popupParent;

    void Awake()
    {
        Instance = this;
    }

    public void ShowDamage(Vector3 position, int damage, ElementLogic.DamageInteraction type)
    {
        if (damage <= 0) return;

        // Prefab'ý yaratýrken Parent'ý belirliyoruz
        GameObject popup = Instantiate(damagePopupPrefab, popupParent);

        // Pozisyonu kartýn olduðu yere eþitle
        popup.transform.position = position;

        // UI yaratýlýnca bazen Scale bozulur, düzeltelim
        popup.transform.localScale = Vector3.one;

        DamagePopup popupScript = popup.GetComponent<DamagePopup>();
        if (popupScript != null)
        {
            popupScript.Setup(damage, type);
        }
    }
}