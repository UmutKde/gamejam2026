using UnityEngine;
using System.Collections.Generic;

public class LaneVisualManager : MonoBehaviour
{
    [Header("Bölge Listesi (Soldan Saða)")]
    public List<LaneStatusDisplay> laneDisplays; // 5 adet Overlay buraya sürüklenecek

    private int lastKnownActiveIndex = -1;

    void Update()
    {
        // GameLogic hazýr mý?
        if (GameLogic.Instance == null) return;

        // Gereksiz yere her karede iþlem yapmayalým, sadece indeks deðiþince yapalým
        if (GameLogic.Instance.activeLaneIndex != lastKnownActiveIndex)
        {
            UpdateLaneVisuals(GameLogic.Instance.activeLaneIndex);
            lastKnownActiveIndex = GameLogic.Instance.activeLaneIndex;
        }
    }

    void UpdateLaneVisuals(int activeIndex)
    {
        for (int i = 0; i < laneDisplays.Count; i++)
        {
            if (laneDisplays[i] == null) continue;

            if (i < activeIndex)
            {
                // Ýndeks bizden büyükse biz geçmiþte kaldýk -> BÝTTÝ
                laneDisplays[i].SetState(LaneState.Finished);
            }
            else if (i == activeIndex)
            {
                // Ýndeks biziz -> AKTÝF SAVAÞ
                laneDisplays[i].SetState(LaneState.Active);
            }
            else
            {
                // Ýndeks bizden küçükse henüz sýra gelmedi -> KÝLÝTLÝ
                laneDisplays[i].SetState(LaneState.Locked);
            }
        }
    }
}