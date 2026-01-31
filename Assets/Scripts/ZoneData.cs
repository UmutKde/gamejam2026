using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName ="GameJam/Zone Card")]
public class ZoneData : ScriptableObject
{
    public string zoneName;
    public Sprite zoneImage;
    public ElementTypes elementTypes;

    [Header("Rules")]
    [Tooltip("Cards with this element deal extra damage here.")]
    public ElementTypes powerfullType;
 
    [Tooltip("Cards with this element deal low damage here.")]
    public List<ElementTypes> weaknessTypes;
}