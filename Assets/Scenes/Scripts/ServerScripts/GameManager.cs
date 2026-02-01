using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Ağ Bağlantıları")]
    public LocalNetwork p1Network;
    public LocalNetwork p2Network;

    [Header("Oyun Durumu")]
    public GameState currentState;
    public int currentTurn = 1; // 1 = Player 1, 2 = Player 2 (Draggable bunu okuyacak)
    
    private int globalCardIdCounter = 0;

    [Header("KART KÜTÜPHANESİ")]
    public List<CardData> allCardsLibrary;

    // ÖNEMLİ: UniqueID'den (Örn: 5) CardDataID'ye (Örn: 101) hızlı erişim için harita
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
            SpawnCard(1); // P1'e Rastgele
            yield return new WaitForSeconds(0.4f);
            SpawnCard(2); // P2'ye Rastgele
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

        // Eğer özel bir kart istenmediyse rastgele seç
        if (cardToSpawn == null)
        {
            int randomIndex = Random.Range(0, allCardsLibrary.Count);
            cardToSpawn = allCardsLibrary[randomIndex];
            
            // Rastgele seçimde sadece MİNYON gelmesini sağla (Maske gelmesin)
            // (Basit bir while döngüsü ile minyon bulana kadar dene veya listeyi filtrele)
            // Şimdilik kütüphanende sadece minyonlar olduğunu varsayıyoruz.
        }

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
        // if (p2Network) p2Network.OnPacketReceived(json); // İkinci oyuncu bağlanınca aç
    }

    // --- MASKE VE ÖDÜL SİSTEMİ (YENİ) ---
    public void SacrificeMask(int playerId, ElementTypes element)
    {
        Debug.Log($"Player {playerId}, {element} maskesini feda etti. Ödül aranıyor...");

        // 1. Kütüphaneden Elementi eşleşen MİNYONLARI bul
        List<CardData> matchingCards = new List<CardData>();
        
        foreach (var card in allCardsLibrary)
        {
            if (card.element == element && card.cardType == CardType.Minion)
            {
                matchingCards.Add(card);
            }
        }

        // 2. Ödül ver
        if (matchingCards.Count > 0)
        {
            // Rastgele birini seç
            CardData reward = matchingCards[Random.Range(0, matchingCards.Count)];
            Debug.Log($"Ödül bulundu: {reward.cardName}");

            // O oyuncuya bu özel kartı spawnla
            SpawnCard(playerId, reward);
        }
        else
        {
            Debug.LogWarning($"Bu elemente ({element}) uygun minyon bulunamadı! Rastgele veriliyor.");
            SpawnCard(playerId); // Bulamazsa teselli ödülü rastgele kart
        }
    }

    // --- VERİ ERİŞİM (OPTIMIZED) ---
    public CardData GetCardDataByID(int libraryId)
    {
        foreach (var data in allCardsLibrary)
        {
            if (data.CardId == libraryId) return data;
        }
        return null;
    }

    // Sahnedeki objeleri taramak yerine Dictionary kullanıyoruz (Çok daha hızlı)
    public CardData GetCardDataByUniqueId(int uniqueId)
    {
        if (uniqueIdToLibraryIdMap.ContainsKey(uniqueId))
        {
            int libraryId = uniqueIdToLibraryIdMap[uniqueId];
            return GetCardDataByID(libraryId);
        }
        
        // Hata durumunda null dön
        return null;
    }

    // --- İLETİŞİM VE STATE ---
    public void ReceivePacketFromClient(string json)
    {
        PlayerAction action = JsonUtility.FromJson<PlayerAction>(json);

        // SIRA KONTROLÜ
        if (action.playerId != currentState.turnOwnerId)
        {
            Debug.LogWarning($"Sıra hatası! İstek: P{action.playerId}, Sıra: P{currentState.turnOwnerId}");
            BroadcastState();
            return;
        }

        // MANTIK İŞLEME
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
        // currentTurn değerini state ile senkronize tutalım
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