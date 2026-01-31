using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ZoneCardDisplay : MonoBehaviour
{
    public ZoneData zoneData;

    [Header("UI Connection")]
    public TextMeshProUGUI zoneName;
    public Image zoneImage;
    public List<ElementTypes> weaknessTypes;
    public ElementTypes powerfullType;

    void Start()
    {
        if(zoneData != null)
            UpdateCardData();
    }

    void UpdateCardData()
    {
        zoneName.text = zoneData.zoneName;
        powerfullType = zoneData.powerfullType;
        weaknessTypes = zoneData.weaknessTypes;

        if(zoneData.zoneImage != null)
            zoneImage.sprite = zoneData.zoneImage;
    }
}