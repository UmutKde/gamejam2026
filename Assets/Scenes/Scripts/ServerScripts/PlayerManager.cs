using UnityEngine;
using System.Collections.Generic;
using Mirror; // GameNetworkPlayer referansý için gerekli

public class PlayerManager : MonoBehaviour
{
    [Header("Kimlik")]
    public int myPlayerId; // 1 (Host/Server) veya 2 (Client)

    // YENÝ: Mirror Köprüsü (Ýnternet Kablomuz)
    private GameNetworkPlayer myNetworkPlayer;

    [Header("Referanslar")]
    public GameObject commonCardPrefab;
    public Transform myHandTransform;
    public Transform enemyHandTransform;

    public List<Transform> myBoardSlots;
    public List<Transform> enemyBoardSlots;

    [Header("Maske Referanslarý")]
    public Transform myMasksParent;
    public Transform enemyMasksParent;

    public void InitNetwork(GameNetworkPlayer netPlayer)
    {
        myNetworkPlayer = netPlayer;

        if (netPlayer.isServer) myPlayerId = 1;
        else myPlayerId = 2;

        Debug.Log($"Að Baþlatýldý. Kimlik: Player {myPlayerId}");

        SetupMaskOwnership();
    }
    void SetupMaskOwnership()
    {
        // 1. Benim Maskelerim (Aþaðýdakiler) -> Benim ID'm
        if (myMasksParent != null)
        {
            foreach (Transform child in myMasksParent)
            {
                MaskCardDisplay mask = child.GetComponent<MaskCardDisplay>();
                Draggable drag = child.GetComponent<Draggable>();

                if (mask) mask.ownerId = myPlayerId;
                if (drag)
                {
                    drag.ownerId = myPlayerId;
                    drag.isOwnedByClient = true; // Sürüklemeye izin ver
                }
            }
        }

        // 2. Rakibin Maskeleri (Yukarýdakiler) -> Rakibin ID'si
        int enemyId = (myPlayerId == 1) ? 2 : 1;
        if (enemyMasksParent != null)
        {
            foreach (Transform child in enemyMasksParent)
            {
                MaskCardDisplay mask = child.GetComponent<MaskCardDisplay>();
                Draggable drag = child.GetComponent<Draggable>();

                if (mask) mask.ownerId = enemyId;
                if (drag)
                {
                    drag.ownerId = enemyId;
                    drag.isOwnedByClient = false; // Dokunmayý yasakla
                }
            }
        }
    }
    public int GetSlotIndex(GameObject hitObject)
    {
        for (int i = 0; i < myBoardSlots.Count; i++)
        {
            if (myBoardSlots[i].gameObject == hitObject) return i;
        }

        if (hitObject.transform.parent != null)
        {
            for (int i = 0; i < myBoardSlots.Count; i++)
            {
                if (myBoardSlots[i] == hitObject.transform.parent) return i;
            }
        }
        return -1;
    }
    public void AttemptPlayCard(Draggable card, int rawSlotIndex)
    {
        // A. MATEMATÝKSEL DÖNÜÞÜM
        int targetLaneIndex = rawSlotIndex % 5;

        // B. SIRA VE BÖLGE KONTROLÜ (Yerel Kontrol)
        if (GameLogic.Instance != null && !GameLogic.Instance.CanPlayCard(targetLaneIndex))
        {
            Debug.LogWarning("Sýra bu bölgede deðil veya senin sýran deðil!");
            // Kartý eline geri yolla (Görsel düzeltme)
            card.transform.SetParent(myHandTransform);
            card.ResetToHand();
            return;
        }

        // C. DOLULUK KONTROLÜ
        Transform targetSlot = myBoardSlots[targetLaneIndex];
        if (targetSlot.childCount > 0)
        {
            Debug.LogWarning("Bu slot dolu!");
            return;
        }

        // D. ANLIK GÖRSEL TAHMÝN (Client-Side Prediction)
        // Sunucudan cevap gelmesini beklemeden kartý "þak" diye oraya koyuyoruz.
        // Böylece oyun yað gibi akar. Eðer sunucu "Hayýr" derse sonra düzeltiriz.
        card.MoveToSlot(targetSlot);

        // E. SUNUCUYA HABER VER (Mirror üzerinden)
        PlayerAction action = new PlayerAction();
        action.actionType = "PlayCard";
        action.cardId = card.cardId;
        action.slotIndex = targetLaneIndex;
        action.playerId = myPlayerId;

        string json = JsonUtility.ToJson(action);

        if (myNetworkPlayer != null)
        {
            myNetworkPlayer.CmdSendPacketToServer(json);
        }
        else
        {
            Debug.LogError("HATA: Network baðlantýsý yok! (GameNetworkPlayer eksik)");
        }
    }
    public void AttemptEndTurn()
    {
        PlayerAction action = new PlayerAction();
        action.actionType = "EndTurn";
        action.playerId = myPlayerId;

        string json = JsonUtility.ToJson(action);

        if (myNetworkPlayer != null)
        {
            myNetworkPlayer.CmdSendPacketToServer(json);
        }
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

            // GameManager ve GameLogic'i Senkronize Et
            if (GameManager.Instance != null)
            {
                GameManager.Instance.currentState = state;
                GameManager.Instance.currentTurn = state.turnOwnerId;
            }

            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.UpdateTurnFromServer(state.turnOwnerId);
            }

            // --- YENÝ EKLENEN KISIM: BÖLGE SENKRONÝZASYONU ---
            // Client'ýn GameLogic beynini sunucuyla eþitle
            if (GameLogic.Instance != null)
            {
                GameLogic.Instance.activeLaneIndex = state.activeLaneIndex;
            }
            // ------------------------------------------------

            SyncBoard(state);
        }
    }
    void SpawnVisualCard(ServerCardSpawn data)
    {
        if (FindCardById(data.uniqueId) != null) return;

        // Kart kime ait?
        Transform targetHand = (data.ownerId == myPlayerId) ? myHandTransform : enemyHandTransform;

        GameObject newCard = Instantiate(commonCardPrefab, null);
        newCard.transform.localScale = Vector3.zero;

        CardData cardData = GameManager.Instance.GetCardDataByID(data.cardDataId);
        MinionCardDisplay display = newCard.GetComponent<MinionCardDisplay>();
        if (display != null && cardData != null)
        {
            display.Setup(cardData);
        }

        Draggable draggable = newCard.GetComponent<Draggable>();
        if (draggable != null)
        {
            // --- DÜZELTME BURADA ---
            // 1. Önce "Bu kart bana mý ait?" bilgisini ver
            draggable.isOwnedByClient = (data.ownerId == myPlayerId);

            // 2. Sonra kartý baþlat (Böylece yüzü doðru açýlýr)
            draggable.InitializeCard(data.uniqueId, data.ownerId);
        }

        newCard.name = $"Card_{data.uniqueId}_(Type_{data.cardDataId})";

        if (CardAnimationManager.Instance != null)
        {
            CardAnimationManager.Instance.AnimateCardToHand(newCard, targetHand);
        }
        else
        {
            newCard.transform.SetParent(targetHand);
            newCard.transform.localScale = Vector3.one;
            newCard.transform.localPosition = Vector3.zero;
        }
    }
    void SyncBoard(GameState state)
    {
        // P1 tarafýný güncelle (Slotlar + Canlar)
        List<Transform> p1Targets = (myPlayerId == 1) ? myBoardSlots : enemyBoardSlots;
        UpdateSlots(state.p1Slots, state.p1Healths, p1Targets); // Can dizisini de gönder

        // P2 tarafýný güncelle (Slotlar + Canlar)
        List<Transform> p2Targets = (myPlayerId == 2) ? myBoardSlots : enemyBoardSlots;
        UpdateSlots(state.p2Slots, state.p2Healths, p2Targets); // Can dizisini de gönder
    }
    void UpdateSlots(int[] slotData, int[] healthData, List<Transform> physicalSlots)
    {
        for (int i = 0; i < 5; i++)
        {
            Transform slot = physicalSlots[i];
            int serverCardId = slotData[i];
            int serverHealth = healthData[i]; // Paketten gelen can

            if (serverCardId == -1)
            {
                if (slot.childCount > 0) foreach (Transform child in slot) Destroy(child.gameObject);
                continue;
            }

            Draggable card = FindCardById(serverCardId);
            if (card != null)
            {
                if (card.transform.parent != slot) card.MoveToSlot(slot);

                // --- DÜZELTME: Caný paketten alýyoruz ---
                var display = card.GetComponent<MinionCardDisplay>();
                if (display != null)
                {
                    display.UpdateHealthUI(serverHealth);
                }
                card.RevealCard();
            }
        }
    }
    Draggable FindCardById(int id)
    {
        // Sahnedeki tüm Draggable objeleri tara
        Draggable[] allCards = FindObjectsByType<Draggable>(FindObjectsSortMode.None);
        foreach (Draggable c in allCards)
        {
            if (c.cardId == id) return c;
        }
        return null;
    }
    public void TriggerLocalClash(
        int p1CardId, int p2CardId,
        bool p1Dies, bool p2Dies,
        int p1Damage, int p1Type,
        int p2Damage, int p2Type
    )
    {
        Draggable p1Card = FindCardById(p1CardId);
        Draggable p2Card = FindCardById(p2CardId);

        if (p1Card != null && p2Card != null && CombatVisualManager.Instance != null)
        {
            // CombatManager'a hasar bilgisini de veriyoruz
            CombatVisualManager.Instance.StartClashAnimation(
                p1Card, p2Card, p1Dies, p2Dies,
                p1Damage, p1Type, p2Damage, p2Type
            );
        }
    }
    public void TriggerLocalDamagePopup(int targetCardId, int amount, int typeIndex)
    {
        Draggable card = FindCardById(targetCardId);
        if (card != null && FloatingTextManager.Instance != null)
        {
            // int index'i tekrar Enum'a çeviriyoruz
            ElementLogic.DamageInteraction interaction = (ElementLogic.DamageInteraction)typeIndex;
            FloatingTextManager.Instance.ShowDamage(card.transform.position, amount, interaction);
        }
    }
    public void AttemptSacrificeMask(ElementTypes element)
    {
        if (myNetworkPlayer != null)
        {
            int elementIndex = (int)element;
            myNetworkPlayer.CmdSacrificeMask(myPlayerId, elementIndex);
        }
    }
}