using UnityEngine;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    // UI Durumu: Sýra P1'de mi? (Draggable scriptleri bunu okuyup aç/kapa yapýyor)
    public bool isPlayerOneTurn = true;

    // Hot-Seat modunda olduðumuz için sahnedeki iki manager'ý da bilmeliyiz
    public PlayerManager p1Manager;
    public PlayerManager p2Manager;

    [Header("UI Referanslarý")]
    public Button endTurnButton;

    void Awake()
    {
        Instance = this;

        if (endTurnButton != null)
        {
            endTurnButton.onClick.RemoveAllListeners();
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
        }
    }

    public void OnEndTurnClicked()
    {
        int playerId = isPlayerOneTurn ? 1 : 2;
        GameLogic.Instance.OnPlayerAction(playerId, "Pass");
    }

    public void UpdateTurnFromServer(int turnOwnerId)
    {
        bool wasP1 = isPlayerOneTurn;
        isPlayerOneTurn = (turnOwnerId == 1);

        if (wasP1 != isPlayerOneTurn)
        {
            Debug.Log($"TurnManager: Sunucu sýrayý Player {turnOwnerId} olarak güncelledi.");
            Draggable[] allCards = FindObjectsByType<Draggable>(FindObjectsSortMode.None);
            foreach (Draggable card in allCards)
            {
                card.UpdateCardVisualsAndState();
            }
        }
    }
}