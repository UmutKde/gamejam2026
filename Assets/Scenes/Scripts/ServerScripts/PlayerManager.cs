using UnityEngine;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    [Header("Kimlik")]
    public int myPlayerId;
    public LocalNetwork network;

    [Header("Referanslar")]
    public GameObject commonCardPrefab;
    public Transform myHandTransform;
    public Transform enemyHandTransform;

    public List<Transform> myBoardSlots;
    public List<Transform> enemyBoardSlots;

    public void AttemptPlayCard(int cardId, int slotIndex)
    {
        PlayerAction action = new PlayerAction();
        action.actionType = "PlayCard";
        action.cardId = cardId;
        action.slotIndex = slotIndex;
        action.playerId = myPlayerId;
        network.SendPacket(JsonUtility.ToJson(action));
    }

    public void AttemptEndTurn()
    {
        PlayerAction action = new PlayerAction();
        action.actionType = "EndTurn";
        action.playerId = myPlayerId;
        network.SendPacket(JsonUtility.ToJson(action));
    }

    public void UpdateGameState(string json)
    {
        if (json.Contains("SpawnCard"))
        {
            ServerCardSpawn data = JsonUtility.FromJson<ServerCardSpawn>(json);
            SpawnVisualCard(data);
        }
        else
        {
            GameState state = JsonUtility.FromJson<GameState>(json);

            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.UpdateTurnFromServer(state.turnOwnerId);
            }

            SyncBoard(state);
        }
    }

    void SpawnVisualCard(ServerCardSpawn data)
    {
        // Zaten varsa tekrar oluþturma
        if (FindCardById(data.cardId) != null) return;

        Transform targetParent = (data.ownerId == myPlayerId) ? myHandTransform : enemyHandTransform;

        // 1. BOÞ PREFABI OLUÞTUR
        GameObject newCard = Instantiate(commonCardPrefab, targetParent);

        // 2. DATAYI GAME MANAGER'DAN ÇEK
        CardData cardData = GameManager.Instance.GetCardDataByID(data.cardId);

        // 3. GÖRSELÝ DOLDUR (Arkadaþýnýn Scripti)
        MinionCardDisplay display = newCard.GetComponent<MinionCardDisplay>();
        if (display != null && cardData != null)
        {
            // Ýþte o "sürükle býrak" iþlemini kodla yapýyoruz:
            display.Setup(cardData);
        }

        // 4. DRAGGABLE AYARLARI
        Draggable draggable = newCard.GetComponent<Draggable>();
        if (draggable != null)
        {
            draggable.InitializeCard(data.cardId, data.ownerId);
            draggable.isOwnedByClient = (data.ownerId == myPlayerId);
        }

        newCard.name = $"Card_{data.cardId}_P{data.ownerId}";
    }

    void SyncBoard(GameState state)
    {
        List<Transform> p1Targets = (myPlayerId == 1) ? myBoardSlots : enemyBoardSlots;
        UpdateSlots(state.p1Slots, p1Targets);

        List<Transform> p2Targets = (myPlayerId == 2) ? myBoardSlots : enemyBoardSlots;
        UpdateSlots(state.p2Slots, p2Targets);
    }

    void UpdateSlots(int[] slotData, List<Transform> physicalSlots)
    {
        for (int i = 0; i < 5; i++)
        {
            int cardId = slotData[i];
            if (cardId != -1)
            {
                Draggable card = FindCardById(cardId);
                if (card != null && card.transform.parent != physicalSlots[i])
                {
                    card.MoveToSlot(physicalSlots[i]);
                }
            }
        }
    }

    Draggable FindCardById(int id)
    {
        Draggable[] allCards = FindObjectsByType<Draggable>(FindObjectsSortMode.None);
        foreach (Draggable c in allCards) if (c.cardId == id) return c;
        return null;
    }
}