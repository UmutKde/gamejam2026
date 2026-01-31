using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Að Baðlantýlarý")]
    public LocalNetwork p1Network;
    public LocalNetwork p2Network;

    [Header("Oyun Durumu")]
    public GameState currentState;
    private int globalCardIdCounter = 0;

    [Header("KART KÜTÜPHANESÝ")]
    public List<CardData> allCardsLibrary;

    public CardData GetCardDataByID(int requestedId)
    {
        foreach (var data in allCardsLibrary)
        {
            if (data.CardId == requestedId) return data;
        }
        Debug.LogError($"HATA: ID {requestedId} kütüphanede bulunamadý!");
        return null;
    }

    void Awake()
    {
        Instance = this;
        currentState = new GameState();
        currentState.turnOwnerId = 1;

        for (int i = 0; i < 5; i++) { currentState.p1Slots[i] = -1; currentState.p2Slots[i] = -1; }
    }

    void Start()
    {
        BroadcastState();

        DistributeHands();
    }

    void DistributeHands()
    {
        for (int i = 0; i < 5; i++)
        {
            SpawnCard(1);
            SpawnCard(2);
        }
    }

    public void SpawnCard(int ownerId)
    {
        if (allCardsLibrary.Count == 0) return;

        int randomIndex = Random.Range(0, allCardsLibrary.Count);
        CardData selectedRandomCard = allCardsLibrary[randomIndex];

        // DÜZELTME 2: Satýrlarýn yerini deðiþtirdik.
        // Önce ID'yi oluþturuyoruz:
        int uniqueInstanceId = globalCardIdCounter++;

        // Sonra bu ID'yi kullanýyoruz:
        if (GameLogic.Instance != null)
        {
            GameLogic.Instance.RegisterCardHealth(uniqueInstanceId, selectedRandomCard.healthPoint);
        }

        ServerCardSpawn packet = new ServerCardSpawn();
        packet.uniqueId = uniqueInstanceId;
        packet.cardDataId = selectedRandomCard.CardId;
        packet.ownerId = ownerId;

        string json = JsonUtility.ToJson(packet);

        if (p1Network) p1Network.OnPacketReceived(json);
        // if (p2Network) p2Network.OnPacketReceived(json); 
    }

    public void ReceivePacketFromClient(string json)
    {
        PlayerAction action = JsonUtility.FromJson<PlayerAction>(json);

        // 1. SIRA KONTROLÜ (Devlet Kapýsý)
        if (action.playerId != currentState.turnOwnerId)
        {
            Debug.LogWarning($"Sýra Player {action.playerId}'de deðil! Þu anki sýra: {currentState.turnOwnerId}");
            // Ýsteði reddet, client'a mevcut durumu tekrar gönder ki senkron olsun
            BroadcastState();
            return;
        }

        // 2. MANTIK ÝÞLEME (Hakeme Gönder)
        if (action.actionType == "PlayCard")
        {
            // Önce kartý slota yerleþtir (State Güncellemesi)
            if (action.playerId == 1) currentState.p1Slots[action.slotIndex] = action.cardId;
            else currentState.p2Slots[action.slotIndex] = action.cardId;

            // Sonra Hakeme Söyle: "Kart oynandý, sýrayý deðiþtir"
            GameLogic.Instance.OnPlayerAction(action.playerId, "PlayCard");
        }
        else if (action.actionType == "EndTurn")
        {
            // Hakeme Söyle: "Pas geçildi, ne yapacaksan yap"
            GameLogic.Instance.OnPlayerAction(action.playerId, "Pass");
        }

        // Not: BroadcastState()'i artýk GameLogic çaðýrýyor, burada çaðýrmamýza gerek yok.
    }

    public void BroadcastState()
    {
        string json = JsonUtility.ToJson(currentState);
        if (p1Network) p1Network.OnPacketReceived(json);
        if (p2Network) p2Network.OnPacketReceived(json);
    }
    // GameManager.cs içine ekle:
    public CardData GetCardDataByUniqueId(int uniqueId)
    {
        // State'deki veya sahnedeki kart listesinden bulmamýz lazým.
        // Basit yöntem: Eþleþen kartý kütüphaneden bulamayýz çünkü uniqueId sahnede üretildi.
        // ÇÖZÜM: Kart oluþtururken "Hangi Unique ID = Hangi CardData ID" diye bir sözlük tutman lazým.
        // Þimdilik sahneden bulalým (En kolayý):

        Draggable[] cards = FindObjectsByType<Draggable>(FindObjectsSortMode.None);
        foreach (var card in cards)
        {
            if (card.cardId == uniqueId) // Draggable.cardId artýk uniqueId tutuyor
            {
                // Kartýn üzerindeki görsel scriptten dataya ulaþabiliriz
                return card.GetComponent<MinionCardDisplay>().cardData;
            }
        }
        return null;
    }

    public void ServerKillCard(int ownerId, int slotIndex, int uniqueId)
    {
        // 1. State'den sil
        if (ownerId == 1) currentState.p1Slots[slotIndex] = -1;
        else currentState.p2Slots[slotIndex] = -1;

        // 2. Clientlara "Bu kartý yok et" emri yolla (Yeni bir paket tipi gerekebilir veya State update yeterli olur)
        // State update ile slot -1 olunca PlayerManager o kartý yok etmeli.
        BroadcastState();
    }
}