using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror; // Mirror Eklendi

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("--- ELEMENT ÖDÜLLERİ ---")]
    public CardData fireRewardMinion;
    public CardData waterRewardMinion;
    public CardData natureRewardMinion;
    public CardData electricRewardMinion;
    public CardData airRewardMinion;

    // YENİ: Bağlı Oyuncular Listesi
    public List<GameNetworkPlayer> connectedPlayers = new List<GameNetworkPlayer>();

    [Header("Oyun Durumu")]
    public GameState currentState;
    public int currentTurn = 1;

    private int globalCardIdCounter = 0;

    [Header("KART KÜTÜPHANESİ")]
    public List<CardData> allCardsLibrary;
    private Dictionary<int, int> uniqueIdToLibraryIdMap = new Dictionary<int, int>();

    void Awake()
    {
        Instance = this;
        currentState = new GameState();
        currentState.turnOwnerId = 1;
        for (int i = 0; i < 5; i++) { currentState.p1Slots[i] = -1; currentState.p2Slots[i] = -1; }
    }

    void Start() { }

    // --- YENİ: OYUNCU BAĞLANINCA ---
    public void RegisterNetworkPlayer(GameNetworkPlayer player)
    {
        connectedPlayers.Add(player);

        // ID Ataması: İlk gelen 1, ikinci gelen 2
        int newId = connectedPlayers.Count;
        player.playerId = newId;

        Debug.Log($"Oyuncu Bağlandı! ID: {newId}");

        if (connectedPlayers.Count == 2)
        {
            Debug.Log("İki oyuncu da hazır. Oyun Başlıyor!");
            BroadcastState();
            StartCoroutine(DealInitialHand());
        }
    }

    // --- BAŞLANGIÇ DAĞITIMI ---
    IEnumerator DealInitialHand()
    {
        yield return new WaitForSeconds(1.0f);
        int startingCardCount = 5;
        for (int i = 0; i < startingCardCount; i++)
        {
            SpawnCard(1);
            yield return new WaitForSeconds(0.4f);
            SpawnCard(2);
            yield return new WaitForSeconds(0.4f);
        }
    }

    // --- KART OLUŞTURMA ---
    public void SpawnCard(int ownerId, CardData specificCardData = null)
    {
        if (allCardsLibrary.Count == 0) return;

        CardData cardToSpawn = specificCardData;

        // Rastgele seçim mantığı
        if (cardToSpawn == null)
        {
            List<CardData> validCards = new List<CardData>();
            foreach (var card in allCardsLibrary)
            {
                // CardData scriptinde 'isRewardOnly' ve 'cardType' olduğundan emin ol
                if (!card.isRewardOnly && card.cardType == CardType.Minion) validCards.Add(card);
            }

            if (validCards.Count > 0)
            {
                cardToSpawn = validCards[Random.Range(0, validCards.Count)];
            }
            else
            {
                Debug.LogError("HATA: Kütüphanede uygun kart bulunamadı!");
                return;
            }
        }

        int uniqueInstanceId = globalCardIdCounter++;
        if (!uniqueIdToLibraryIdMap.ContainsKey(uniqueInstanceId))
            uniqueIdToLibraryIdMap.Add(uniqueInstanceId, cardToSpawn.CardId);

        if (GameLogic.Instance != null)
            GameLogic.Instance.RegisterCardHealth(uniqueInstanceId, cardToSpawn.healthPoint);

        ServerCardSpawn packet = new ServerCardSpawn();
        packet.uniqueId = uniqueInstanceId;
        packet.cardDataId = cardToSpawn.CardId;
        packet.ownerId = ownerId;

        string json = JsonUtility.ToJson(packet);

        foreach (var player in connectedPlayers)
        {
            player.RpcReceivePacketToClient(json);
        }
    }

    // --- İLETİŞİM ---
    public void ReceivePacketFromClient(string json)
    {
        PlayerAction action = JsonUtility.FromJson<PlayerAction>(json);

        if (action.playerId != currentState.turnOwnerId)
        {
            BroadcastState();
            return;
        }

        if (action.actionType == "PlayCard")
        {
            if (action.playerId == 1) currentState.p1Slots[action.slotIndex] = action.cardId;
            else currentState.p2Slots[action.slotIndex] = action.cardId;

            GameLogic.Instance.OnPlayerAction(action.playerId, "PlayCard");
        }
        else if (action.actionType == "EndTurn")
        {
            GameLogic.Instance.OnPlayerAction(action.playerId, "Pass");
        }
    }

    public void BroadcastState()
    {
        currentTurn = currentState.turnOwnerId;

        if (GameLogic.Instance != null)
        {
            currentState.activeLaneIndex = GameLogic.Instance.activeLaneIndex;

            // --- YENİ: Canları Pakete İşle ---
            for (int i = 0; i < 5; i++)
            {
                // P1 Slotundaki kartın canı
                int p1CardId = currentState.p1Slots[i];
                if (p1CardId != -1) currentState.p1Healths[i] = GameLogic.Instance.GetLiveHealth(p1CardId);
                else currentState.p1Healths[i] = 0;

                // P2 Slotundaki kartın canı
                int p2CardId = currentState.p2Slots[i];
                if (p2CardId != -1) currentState.p2Healths[i] = GameLogic.Instance.GetLiveHealth(p2CardId);
                else currentState.p2Healths[i] = 0;
            }
        }

        string json = JsonUtility.ToJson(currentState);
        foreach (var player in connectedPlayers) player.RpcReceivePacketToClient(json);
    }

    public void ServerKillCard(int ownerId, int slotIndex, int uniqueId)
    {
        if (ownerId == 1) currentState.p1Slots[slotIndex] = -1;
        else currentState.p2Slots[slotIndex] = -1;
        BroadcastState();
    }

    // --- DÜZELTİLEN KISIM BURASI ---
    // GameLogic.ElementType YERİNE ElementTypes KULLANMALIYIZ
    public void SacrificeMask(int playerId, ElementTypes element)
    {
        Debug.Log($"Player {playerId}, {element} maskesini feda etti. Özel ödül hazırlanıyor...");

        CardData reward = null;

        // Switch case artık Global Enum (ElementTypes) ile çalışıyor
        switch (element)
        {
            case ElementTypes.Fire:
                reward = fireRewardMinion;
                break;
            case ElementTypes.Water:
                reward = waterRewardMinion;
                break;
            case ElementTypes.Nature: // Earth yerine Nature kullanmıştık
                reward = natureRewardMinion;
                break;
            case ElementTypes.Electric:
                reward = electricRewardMinion;
                break;
            case ElementTypes.Air:
                reward = airRewardMinion;
                break;
        }

        if (reward != null)
        {
            Debug.Log($"Özel Ödül Veriliyor: {reward.cardName}");
            SpawnCard(playerId, reward);
        }
        else
        {
            Debug.LogError($"HATA: {element} elementi için GameManager'da ödül kartı seçilmemiş!");
        }
    }

    public CardData GetCardDataByID(int libraryId)
    {
        foreach (var data in allCardsLibrary) if (data.CardId == libraryId) return data;
        return null;
    }

    public CardData GetCardDataByUniqueId(int uniqueId)
    {
        if (uniqueIdToLibraryIdMap.ContainsKey(uniqueId)) return GetCardDataByID(uniqueIdToLibraryIdMap[uniqueId]);
        return null;
    }
    // --- SAVAŞ YAYINI ---
    public void BroadcastClash(
        int p1CardId, int p2CardId,
        bool p1Dies, bool p2Dies,
        int p1Damage, int p1Type,
        int p2Damage, int p2Type
    )
    {
        foreach (var player in connectedPlayers)
        {
            player.RpcPlayClashAnimation(p1CardId, p2CardId, p1Dies, p2Dies, p1Damage, p1Type, p2Damage, p2Type);
        }
    }

}