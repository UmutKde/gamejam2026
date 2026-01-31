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

    // --- YENÝ FONKSÝYON: Slot Ýndeksi Bulucu ---
    public int GetSlotIndex(GameObject hitObject)
    {
        // 1. Direkt slot objesine mi býraktýk?
        for (int i = 0; i < myBoardSlots.Count; i++)
        {
            if (myBoardSlots[i].gameObject == hitObject) return i;
        }

        // 2. Belki slotun içindeki bir resme veya çocuðuna býraktýk?
        if (hitObject.transform.parent != null)
        {
            for (int i = 0; i < myBoardSlots.Count; i++)
            {
                if (myBoardSlots[i] == hitObject.transform.parent) return i;
            }
        }

        return -1; // Slot bulunamadý
    }

    // Parametre olarak artýk Kartýn kendisini de (Draggable card) alýyoruz
    public void AttemptPlayCard(Draggable card, int rawSlotIndex)
    {
        // 1. MATEMATÝKSEL DÖNÜÞÜM
        int targetLaneIndex = rawSlotIndex % 5;

        // 2. SUNUCU MANTIK KONTROLÜ (Bölge Sýrasý)
        if (!GameLogic.Instance.CanPlayCard(targetLaneIndex))
        {
            Debug.LogWarning("Sýra bu bölgede deðil!");
            return;
        }

        // 3. DOLULUK KONTROLÜ (Lock Mantýðý)
        // Hedef slotu buluyoruz
        Transform targetSlot = myBoardSlots[targetLaneIndex];

        // Eðer slotta zaten bir kart (child) varsa OYNAYAMAZSIN.
        if (targetSlot.childCount > 0)
        {
            Debug.LogWarning("Bu slot zaten dolu!");
            // Kartý eline geri gönder (Draggable OnEndDrag bunu halleder)
            return;
        }

        // --- 4. ANLIK GÖRSEL GÜNCELLEME (Client-Side Prediction) ---
        // Sunucudan cevap beklemeden kartý hemen oraya oturt!
        card.MoveToSlot(targetSlot);

        // Ses efekti varsa burada çaldýrabilirsin.
        // -----------------------------------------------------------

        // 5. SUNUCUYA BÝLDÝR
        PlayerAction action = new PlayerAction();
        action.actionType = "PlayCard";
        action.cardId = card.cardId;
        action.slotIndex = targetLaneIndex;
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
        if (FindCardById(data.uniqueId) != null) return;

        Transform targetParent = (data.ownerId == myPlayerId) ? myHandTransform : enemyHandTransform;
        GameObject newCard = Instantiate(commonCardPrefab, targetParent);

        // Görünmezlik sorununu çözen ayarlar
        newCard.transform.localScale = Vector3.one;
        newCard.transform.localPosition = Vector3.zero;
        newCard.transform.localRotation = Quaternion.identity;

        // Görsel Ayarla
        CardData cardData = GameManager.Instance.GetCardDataByID(data.cardDataId);
        MinionCardDisplay display = newCard.GetComponent<MinionCardDisplay>();
        if (display != null && cardData != null)
        {
            display.Setup(cardData);
        }

        // Draggable Ayarla
        Draggable draggable = newCard.GetComponent<Draggable>();
        if (draggable != null)
        {
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
            Transform slot = physicalSlots[i];
            int serverCardId = slotData[i]; // Sunucudaki durum (-1 veya UniqueID)

            // --- 1. ÖLÜM KONTROLÜ (GÖRSEL SÝLME) ---
            // Eðer sunucuda slot boþsa (-1) ama sahnede o slotta bir kart varsa -> YOK ET
            if (serverCardId == -1)
            {
                if (slot.childCount > 0)
                {
                    foreach (Transform child in slot)
                    {
                        Destroy(child.gameObject); // Görseli yok et
                    }
                }
                continue; // Bu slot bitti, diðerine geç
            }

            // --- 2. HAREKET VE GÜNCELLEME ---
            // Sunucuda kart var. Sahnedeki kartý bul veya yerine oturt.
            Draggable card = FindCardById(serverCardId);

            if (card != null)
            {
                // A. Kart yanlýþ yerdeyse (eldeyse veya baþka slottaysa) yerine taþý
                if (card.transform.parent != slot)
                {
                    card.MoveToSlot(slot);
                }

                // B. CAN DEÐERÝNÝ GÜNCELLE
                // GameLogic'ten canlý veriyi çek
                int currentHealth = GameLogic.Instance.GetLiveHealth(serverCardId);

                // Görsel scriptine ulaþ ve yazýyý deðiþtir
                var display = card.GetComponent<MinionCardDisplay>();
                if (display != null)
                {
                    display.UpdateHealthUI(currentHealth);
                }

                // C. KARTI AÇ (REVEAL) - KRÝTÝK NOKTA!
                // Savaþ güncellemesi geldiyse veya kart artýk bir slota oturduysa,
                // kartýn "Açýklanmýþ" (Revealed) olduðunu iþaretliyoruz.
                // Bu sayede Draggable içindeki UpdateCardVisualsAndState fonksiyonu,
                // rakip kart olsa bile onu FaceUp (Açýk) hale getirecek.
                card.RevealCard();
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