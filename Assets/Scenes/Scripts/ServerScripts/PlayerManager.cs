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
        // Kontrolü artýk Unique ID üzerinden yapýyoruz.
        // Böylece 2 tane Ejderha (ID: 5) gelse bile, Unique ID'leri farklý (100 ve 101) olacaðý için sorun çýkmaz.
        if (FindCardById(data.uniqueId) != null) return;

        Transform targetParent = (data.ownerId == myPlayerId) ? myHandTransform : enemyHandTransform;

        GameObject newCard = Instantiate(commonCardPrefab, targetParent);

        newCard.transform.localScale = Vector3.one;

        // 2. Pozisyonu yerel olarak sýfýrla (Z ekseni kaymasýn)
        newCard.transform.localPosition = Vector3.zero;

        // 3. Dönüþü sýfýrla
        newCard.transform.localRotation = Quaternion.identity;

        // --- 1. GÖRSELÝ AYARLA (Data ID Kullanarak) ---
        // GameManager'dan "Ejderha" verisini çek
        CardData cardData = GameManager.Instance.GetCardDataByID(data.cardDataId);

        MinionCardDisplay display = newCard.GetComponent<MinionCardDisplay>();
        if (display != null && cardData != null)
        {
            display.Setup(cardData);
        }

        // --- 2. MEKANÝÐÝ AYARLA (Unique ID Kullanarak) ---
        Draggable draggable = newCard.GetComponent<Draggable>();
        if (draggable != null)
        {
            // Kartýn ID'si artýk Unique ID oldu.
            draggable.InitializeCard(data.uniqueId, data.ownerId);
            draggable.isOwnedByClient = (data.ownerId == myPlayerId);
        }

        newCard.name = $"Card_{data.uniqueId}_(Type_{data.cardDataId})";
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