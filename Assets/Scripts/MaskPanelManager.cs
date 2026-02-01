using UnityEngine;
using System.Collections.Generic;

public class MaskPanelManager : MonoBehaviour
{
    [Header("Panel Ayarları")]
    public int panelOwnerId; // 1 = Player 1 (Sol/Alt), 2 = Player 2 (Sağ/Üst)
    public GameObject maskPrefab;
    public Transform contentParent;

    [Header("Maske Verileri")]
    public List<CardData> maskTemplates; // 5 Elementin CardData'sı buraya

    void Start()
    {
        GenerateMasks();
    }

    public void GenerateMasks()
    {
        // Önce temizle
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        int indexCounter = 0;

        foreach (CardData data in maskTemplates)
        {
            GameObject newMask = Instantiate(maskPrefab, contentParent);
            MaskCardDisplay display = newMask.GetComponent<MaskCardDisplay>();
            
            if (display != null)
            {
                // ID Üretme Mantığı: (OwnerID * 1000) + Sıra No
                // Örn: P1 için 1000, 1001... P2 için 2000, 2001...
                int uniqueId = (panelOwnerId * 1000) + indexCounter;
                
                display.SetupMask(uniqueId, panelOwnerId, data);
            }
            indexCounter++;
        }
    }
}