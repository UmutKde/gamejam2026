using UnityEngine;
using UnityEngine.UI;

public class LaneStatusDisplay : MonoBehaviour
{
    [Header("UI Referanslarý")]
    public GameObject activeBorderObj;   // Aktifken çýkacak çerçeve
    public GameObject lockOverlayObj;    // Kilitliyken çýkacak siyah perde + kilit ikonu
    public GameObject finishedOverlayObj;// Bittiðinde çýkacak gri perde + ikon

    // Durumu dýþarýdan deðiþtirmek için fonksiyon
    public void SetState(LaneState state)
    {
        // Önce hepsini kapat
        if (activeBorderObj) activeBorderObj.SetActive(false);
        if (lockOverlayObj) lockOverlayObj.SetActive(false);
        if (finishedOverlayObj) finishedOverlayObj.SetActive(false);

        // Duruma göre ilgili olaný aç
        switch (state)
        {
            case LaneState.Locked:
                if (lockOverlayObj) lockOverlayObj.SetActive(true);
                break;

            case LaneState.Active:
                if (activeBorderObj) activeBorderObj.SetActive(true);
                break;

            case LaneState.Finished:
                if (finishedOverlayObj) finishedOverlayObj.SetActive(true);
                break;
        }
    }
}

// Durumlarý belirten basit bir Enum
public enum LaneState
{
    Locked,
    Active,
    Finished
}