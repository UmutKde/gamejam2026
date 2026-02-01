using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("--- ELEMENT ÖDÜLLERİ (Minyonları Buraya Sürükle) ---")]
    public CardData fireRewardMinion;     // Ateş maskesi için ödül
    public CardData waterRewardMinion;    // Su maskesi için ödül
    public CardData natureRewardMinion;   // Doğa maskesi için ödül
    public CardData electricRewardMinion; // Elektrik maskesi için ödül
    public CardData airRewardMinion;      // Hava maskesi için ödül

    [Header("Ağ Bağlantıları")]
    public LocalNetwork p1Network;
    public LocalNetwork p2Network;

    [Header("Oyun Durumu")]
    public GameState currentState;
    public int currentTurn = 1; // 1 = Player 1, 2 = Player 2
    
    private int globalCardIdCounter = 0;

    [Header("KART KÜTÜPHANESİ")]
    public List<CardData> allCardsLibrary;

    // UniqueID'den (Örn: 5) CardDataID'ye (Örn: 101) hızlı erişim için harita
    private Dictionary<int, int> uniqueIdToLibraryIdMap = new Dictionary<int, int>();

    void Awake()
    {
        Instance = this;
        currentState = new GameState();
        currentState.turnOwnerId = 1; // Başlangıç sırası

        // Slotları temizle
        for (int i = 0; i < 5; i++) { currentState.p1Slots[i] = -1; currentState.p2Slots[i] = -1; }
    }

    void Start()
    {
        BroadcastState();
        StartCoroutine(DealInitialHand());
    }

    // --- BAŞLANGIÇ DAĞITIMI ---
    IEnumerator DealInitialHand()
    {
        yield return new WaitForSeconds(1.0f);

        int startingCardCount = 5;

        for (int i = 0; i < startingCardCount; i++)
        {
            SpawnCard(1); // P1'e Rastgele (Ödüller hariç)
            yield return new WaitForSeconds(0.4f);
            SpawnCard(2); // P2'ye Rastgele (Ödüller hariç)
            yield return new WaitForSeconds(0.4f);
        }

        Debug.Log("Başlangıç kartları dağıtıldı!");
    }

    // --- KART OLUŞTURMA (SPAWN) ---
    // specificCardData: Eğer null gelirse rastgele seçer, dolu gelirse o kartı üretir.
    public void SpawnCard(int ownerId, CardData specificCardData = null)
    {
        if (allCardsLibrary.Count == 0) return;

        CardData cardToSpawn = specificCardData;

        // --- RASTGELE SEÇİM MANTIĞI (FİLTRELİ) ---
        if (cardToSpawn == null)
        {
            // 1. Sadece "Ödül Olmayan" (Normal) kartları bir listeye topla
            List<CardData> validCards = new List<CardData>();

            foreach (var card in allCardsLibrary)
            {
                // Eğer kart "Sadece Ödül" DEĞİLSE ve tipi Minyon ise listeye ekle
                // NOT: CardData scriptinde 'isRewardOnly' değişkeni olmalı!
                if (card.isRewardOnly == false && card.cardType == CardType.Minion)
                {
                    validCards.Add(card);
                }
            }

            // 2. Eğer geçerli kart varsa içinden seç
            if (validCards.Count > 0)
            {
                int randomIndex = Random.Range(0, validCards.Count);
                cardToSpawn = validCards[randomIndex];
            }
            else
            {
                Debug.LogError("HATA: Kütüphanede hiç normal (ödül olmayan) kart kalmamış!");
                return;
            }
        }
        // ------------------------------------------------

        // 1. Unique ID Üret
        int uniqueInstanceId = globalCardIdCounter++;

        // 2. Sözlüğe Kaydet (Hızlı Erişim İçin)
        if (!uniqueIdToLibraryIdMap.ContainsKey(uniqueInstanceId))
        {
            uniqueIdToLibraryIdMap.Add(uniqueInstanceId, cardToSpawn.CardId);
        }

        // 3. Hakeme (GameLogic) Can değerini kaydet
        if (GameLogic.Instance != null)
        {
            GameLogic.Instance.RegisterCardHealth(uniqueInstanceId, cardToSpawn.healthPoint);
        }

        // 4. Paketi Hazırla ve Yolla
        ServerCardSpawn packet = new ServerCardSpawn();
        packet.uniqueId = uniqueInstanceId;
        packet.cardDataId = cardToSpawn.CardId;
        packet.ownerId = ownerId;

        string json = JsonUtility.ToJson(packet);

        if (p1Network) p1Network.OnPacketReceived(json);
        // if (p2Network) p2Network.OnPacketReceived(json); 
    }

    // --- MASKE VE ÖDÜL SİSTEMİ ---
    public void SacrificeMask(int playerId, ElementTypes element)
    {
        Debug.Log($"Player {playerId}, {element} maskesini feda etti. Özel ödül hazırlanıyor...");

        CardData reward = null;

        // Gelen elemente göre yukarıda tanımladığın değişkenleri eşleştiriyoruz
        switch (element)
        {
            case ElementTypes.Fire:
                reward = fireRewardMinion;
                break;
            case ElementTypes.Water:
                reward = waterRewardMinion;
                break;
            case ElementTypes.Nature:
                reward = natureRewardMinion;
                break;
            case ElementTypes.Electric:
                reward = electricRewardMinion;
                break;
            case ElementTypes.Air:
                reward = airRewardMinion;
                break;
        }

        // Ödülü ver
        if (reward != null)
        {
            Debug.Log($"Özel Ödül Veriliyor: {reward.cardName}");
            // Buraya özel kartı yolluyoruz (specificCardData dolu olduğu için filtreye takılmaz)
            SpawnCard(playerId, reward);
        }
        else
        {
            Debug.LogError($"HATA: {element} elementi için GameManager'da ödül kartı seçilmemiş! Lütfen Inspector'dan atama yap.");
        }
    }

    // --- VERİ ERİŞİM ---
    public CardData GetCardDataByID(int libraryId)
    {
        foreach (var data in allCardsLibrary)
        {
            if (data.CardId == libraryId) return data;
        }
        return null;
    }

    public CardData GetCardDataByUniqueId(int uniqueId)
    {
        if (uniqueIdToLibraryIdMap.ContainsKey(uniqueId))
        {
            int libraryId = uniqueIdToLibraryIdMap[uniqueId];
            return GetCardDataByID(libraryId);
        }
        return null;
    }

    // --- İLETİŞİM VE STATE ---
    public void ReceivePacketFromClient(string json)
    {
        PlayerAction action = JsonUtility.FromJson<PlayerAction>(json);

        if (action.playerId != currentState.turnOwnerId)
        {
            Debug.LogWarning($"Sıra hatası! İstek: P{action.playerId}, Sıra: P{currentState.turnOwnerId}");
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

        string json = JsonUtility.ToJson(currentState);
        if (p1Network) p1Network.OnPacketReceived(json);
        if (p2Network) p2Network.OnPacketReceived(json);
    }

    public void ServerKillCard(int ownerId, int slotIndex, int uniqueId)
    {
        if (ownerId == 1) currentState.p1Slots[slotIndex] = -1;
        else currentState.p2Slots[slotIndex] = -1;

        BroadcastState();
    }
}