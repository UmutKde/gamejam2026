using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLogic : MonoBehaviour
{
    public static GameLogic Instance;

    // --- DEÐÝÞÝKLÝK: ElementType enum'ýný sildik. ---
    // Artýk CardData'nýn kullandýðý global 'ElementTypes' enum'ýný kullanacaðýz.

    // Vuruþ Tipi (Renkler buna göre belirlenecek)
    public enum DamageInteraction
    {
        Neutral,        // Beyaz (x1.0 Hasar)
        Advantage,      // Sarý (x1.25 Hasar)
        Dominance       // Kýrmýzý (x1.5 Hasar)
    }

    // Sonuç Paketi
    public struct CombatResult
    {
        public int finalDamage;
        public DamageInteraction interactionType;
    }

    [Header("Oyun Durumu")]
    public int activeLaneIndex = 0; // Þu an savaþýn döndüðü bölge
    public int p1VictoryScore = 0;
    public int p2VictoryScore = 0;

    private int passCounter = 0;
    private int lastPasserId = 0;

    // Kartlarýn Canlarýný Takip Eden Sözlük
    private Dictionary<int, int> liveCardHealths = new Dictionary<int, int>();

    void Awake()
    {
        Instance = this;
    }

    // --- 1. KART OYNAMA KONTROLÜ ---
    public bool CanPlayCard(int slotIndex)
    {
        if (slotIndex != activeLaneIndex)
        {
            // Debug.LogWarning kapalý kalabilir, spam yapmasýn
            return false;
        }
        return true;
    }

    // --- 2. CAN TAKÝBÝ ---
    public void RegisterCardHealth(int uniqueId, int maxHealth)
    {
        if (!liveCardHealths.ContainsKey(uniqueId))
        {
            liveCardHealths.Add(uniqueId, maxHealth);
        }
    }

    // --- 3. AKSÝYON YÖNETÝMÝ ---
    public void OnPlayerAction(int playerId, string actionType)
    {
        int currentTurnOwner = GameManager.Instance.currentState.turnOwnerId;

        if (playerId != currentTurnOwner)
        {
            Debug.LogWarning($"HATA: Sýra {currentTurnOwner}'de ama {playerId} iþlem yapmaya çalýþtý.");
            return;
        }

        if (actionType == "PlayCard")
        {
            passCounter = 0;
            // Sýrayý deðiþtir
            int nextPlayer = (playerId == 1) ? 2 : 1;

            GameManager.Instance.currentState.turnOwnerId = nextPlayer;
            GameManager.Instance.BroadcastState();
        }
        else if (actionType == "Pass")
        {
            passCounter++;
            lastPasserId = playerId;

            if (passCounter >= 2)
            {
                // Herkes pas geçti, savaþý baþlat
                StartCoroutine(ResolveCombatCoroutine());
            }
            else
            {
                // Henüz 1 pas, sýra diðerine
                int nextPlayer = (playerId == 1) ? 2 : 1;
                GameManager.Instance.currentState.turnOwnerId = nextPlayer;
                GameManager.Instance.BroadcastState();
            }
        }
    }

    // --- 4. SAVAÞ MEKANÝÐÝ (GÜNCEL - NETWORK UYUMLU) ---
    IEnumerator ResolveCombatCoroutine()
    {
        Debug.Log($"--- {activeLaneIndex}. BÖLGE SAVAÞI BAÞLADI ---");

        int p1CardUniqueId = GameManager.Instance.currentState.p1Slots[activeLaneIndex];
        int p2CardUniqueId = GameManager.Instance.currentState.p2Slots[activeLaneIndex];

        CardData p1Data = null;
        CardData p2Data = null;

        bool p1WillDie = false;
        bool p2WillDie = false;

        // Verileri Topla
        if (p1CardUniqueId != -1) p1Data = GameManager.Instance.GetCardDataByUniqueId(p1CardUniqueId);
        if (p2CardUniqueId != -1) p2Data = GameManager.Instance.GetCardDataByUniqueId(p2CardUniqueId);

        // --- A. ÝKÝ TARAF DA VARSA SAVAÞ ---
        if (p1Data != null && p2Data != null)
        {
            // ELEMENT HESAPLAMALARI (Global ElementTypes Kullanýyor)
            // P1 saldýrýyor, P2 savunuyor
            var p1AttackResult = ElementLogic.CalculateDamage(p1Data.attackPoint, p1Data.element, p2Data.element);

            // P2 saldýrýyor, P1 savunuyor
            var p2AttackResult = ElementLogic.CalculateDamage(p2Data.attackPoint, p2Data.element, p1Data.element);

            // ÖLÜM TAHMÝNÝ
            int p1TempHp = liveCardHealths[p1CardUniqueId] - p2AttackResult.finalDamage;
            int p2TempHp = liveCardHealths[p2CardUniqueId] - p1AttackResult.finalDamage;

            if (p1TempHp <= 0) p1WillDie = true;
            if (p2TempHp <= 0) p2WillDie = true;

            // --- YENÝ: VERÝ PAKETLEME VE YAYINLAMA ---
            // Enumlarý int'e çeviriyoruz çünkü Mirror bazen Enum sevmez
            int p1TakingDamageType = (int)p2AttackResult.interactionType; // P1'in yediði hasarýn rengi
            int p2TakingDamageType = (int)p1AttackResult.interactionType; // P2'nin yediði hasarýn rengi

            // TEK FONKSÝYONLA HER ÞEYÝ YOLLA:
            // "Animasyonu baþlat, þu kiþiler ölecek, þu kadar hasar sayýlarý çýkacak"
            GameManager.Instance.BroadcastClash(
                p1CardUniqueId, p2CardUniqueId,
                p1WillDie, p2WillDie,
                p2AttackResult.finalDamage, p1TakingDamageType, // P1'in Hasarý
                p1AttackResult.finalDamage, p2TakingDamageType  // P2'nin Hasarý
            );

            // --- YENÝ: SUNUCU BEKLEMESÝ ---
            // Animasyonun oynatýlmasý için süre taný (3.5 sn ideal, animasyon sürene göre ayarla)
            yield return new WaitForSeconds(3.5f);

            // --- DEÐERLERÝ GERÇEKTEN GÜNCELLE ---
            int p1CurrentHp = liveCardHealths[p1CardUniqueId];
            int p2CurrentHp = liveCardHealths[p2CardUniqueId];

            p1CurrentHp -= p2AttackResult.finalDamage;
            p2CurrentHp -= p1AttackResult.finalDamage;

            liveCardHealths[p1CardUniqueId] = p1CurrentHp;
            liveCardHealths[p2CardUniqueId] = p2CurrentHp;

            // Ölenleri Sil
            if (p1CurrentHp <= 0) KillCard(1, activeLaneIndex, p1CardUniqueId);
            if (p2CurrentHp <= 0) KillCard(2, activeLaneIndex, p2CardUniqueId);

            // Yeni tura hazýrlan
            ResetRound(lastPasserId);
        }
        // --- B. SADECE P1 VARSA ---
        else if (p1Data != null && p2Data == null)
        {
            ConquerLane(1);
        }
        // --- C. SADECE P2 VARSA ---
        else if (p2Data != null && p1Data == null)
        {
            ConquerLane(2);
        }
        // --- D. KÝMSE YOKSA ---
        else
        {
            ResetRound(lastPasserId);
        }

        // Son durumu herkese bildir (Canlar güncellendi, slotlar boþaldý vs.)
        GameManager.Instance.BroadcastState();
    }

    // --- YARDIMCI FONKSÝYONLAR ---

    private Draggable FindDraggableByUniqueId(int uniqueId)
    {
        Draggable[] cards = FindObjectsByType<Draggable>(FindObjectsSortMode.None);
        foreach (var card in cards)
        {
            if (card.cardId == uniqueId) return card;
        }
        return null;
    }

    void KillCard(int ownerId, int slotIndex, int uniqueId)
    {
        GameManager.Instance.ServerKillCard(ownerId, slotIndex, uniqueId);
    }

    void ConquerLane(int winnerId)
    {
        Debug.Log($"Bölge {activeLaneIndex} fethedildi! Kazanan: {winnerId}");

        if (winnerId == 1) p1VictoryScore++;
        else p2VictoryScore++;

        // Kazanan ödül kartý alýr
        GameManager.Instance.SpawnCard(winnerId);

        activeLaneIndex++;

        if (activeLaneIndex >= 5)
        {
            Debug.Log($"OYUN BÝTTÝ! Skor - P1: {p1VictoryScore} | P2: {p2VictoryScore}");
            // Game Over iþlemleri burada yapýlabilir
        }
        else
        {
            // Yeni kartlarý daðýt
            StartCoroutine(DistributeNewRoundCards());
            ResetRound(lastPasserId);
        }
    }

    void ResetRound(int starterId)
    {
        passCounter = 0;
        GameManager.Instance.currentState.turnOwnerId = starterId;
        GameManager.Instance.BroadcastState();

        Debug.Log($"Yeni tur baþladý. Sýra Player {starterId}'de.");
    }

    public int GetLiveHealth(int uniqueId)
    {
        if (liveCardHealths.ContainsKey(uniqueId)) return liveCardHealths[uniqueId];
        return 0;
    }

    IEnumerator DistributeNewRoundCards()
    {
        for (int i = 0; i < 2; i++)
        {
            yield return new WaitForSeconds(0.2f);
            GameManager.Instance.SpawnCard(1);
            yield return new WaitForSeconds(0.4f);
            GameManager.Instance.SpawnCard(2);
            yield return new WaitForSeconds(0.4f);
        }
    }
}