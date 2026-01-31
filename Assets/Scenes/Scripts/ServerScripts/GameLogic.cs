using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLogic : MonoBehaviour
{
    public static GameLogic Instance;

    [Header("Oyun Durumu")]
    public int activeLaneIndex = 0; // Þu an savaþýn döndüðü bölge (0-4)
    public int p1VictoryScore = 0;
    public int p2VictoryScore = 0;

    private int passCounter = 0; // Arka arkaya kaç kiþi pas dedi?
    private int lastPasserId = 0; // Yeni tura kimin baþlayacaðýný belirlemek için

    // Kartlarýn Canlarýný Takip Eden Sözlük (UniqueId -> Kalan Can)
    // ScriptableObject verisini bozmamak için canlý veriyi burada tutuyoruz.
    private Dictionary<int, int> liveCardHealths = new Dictionary<int, int>();

    void Awake()
    {
        Instance = this;
    }

    // --- 1. KART OYNAMA KONTROLÜ ---
    // PlayerManager kart koymadan önce buraya soracak: "Koyabilir miyim?"
    public bool CanPlayCard(int slotIndex)
    {
        // KURAL: Sadece aktif olan (savaþýn döndüðü) lane'e kart oynanabilir.
        if (slotIndex != activeLaneIndex)
        {
            Debug.LogWarning($"HATA: Sadece {activeLaneIndex}. bölgeye kart oynayabilirsin!");
            return false;
        }
        return true;
    }

    // --- 2. CAN TAKÝBÝ (SÝSTEM) ---
    // Kart ilk oluþtuðunda GameManager buraya kaydettirecek
    public void RegisterCardHealth(int uniqueId, int maxHealth)
    {
        if (!liveCardHealths.ContainsKey(uniqueId))
        {
            liveCardHealths.Add(uniqueId, maxHealth);
        }
    }

    // --- 3. AKSÝYON YÖNETÝMÝ ---
    // GameManager, gelen paketi iþledikten sonra buraya bildirecek
    // --- 3. AKSÝYON YÖNETÝMÝ ---
    public void OnPlayerAction(int playerId, string actionType)
    {
        // Önceki tur sahibini al
        int currentTurnOwner = GameManager.Instance.currentState.turnOwnerId;

        // Gelen istek sýrasý gelen kiþiden mi? (Extra Güvenlik)
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

            // 1. STATE'Ý GÜNCELLE (Sunucunun hafýzasý)
            GameManager.Instance.currentState.turnOwnerId = nextPlayer;

            // 2. HERKESE HABER VER (Clientlarýn hafýzasý)
            GameManager.Instance.BroadcastState();
        }
        else if (actionType == "Pass")
        {
            passCounter++;
            lastPasserId = playerId;

            if (passCounter >= 2)
            {
                // Savaþ Baþlýyor -> Savaþ bitince sýra mantýðý ResolveCombat içinde kurulacak
                StartCoroutine(ResolveCombatCoroutine());
                //ResolveCombat();
            }
            else
            {
                // Henüz 1 pas -> Sýra diðerine
                int nextPlayer = (playerId == 1) ? 2 : 1;

                // STATE GÜNCELLE VE YAYINLA
                GameManager.Instance.currentState.turnOwnerId = nextPlayer;
                GameManager.Instance.BroadcastState();
            }
        }
    }

    // --- 4. SAVAÞ MEKANÝÐÝ (CORE) ---
    // --- 4. SAVAÞ MEKANÝÐÝ (CORE - COROUTINE) ---
    IEnumerator ResolveCombatCoroutine()
    {
        Debug.Log($"--- {activeLaneIndex}. BÖLGE SAVAÞI BAÞLADI ---");

        int p1CardUniqueId = GameManager.Instance.currentState.p1Slots[activeLaneIndex];
        int p2CardUniqueId = GameManager.Instance.currentState.p2Slots[activeLaneIndex];

        CardData p1Data = null;
        CardData p2Data = null;
        Draggable p1Draggable = null;
        Draggable p2Draggable = null;

        // ÖN HESAPLAMA DEÐÝÞKENLERÝ
        bool p1WillDie = false;
        bool p2WillDie = false;

        // 1. Verileri Topla
        if (p1CardUniqueId != -1)
        {
            p1Data = GameManager.Instance.GetCardDataByUniqueId(p1CardUniqueId);
            p1Draggable = FindDraggableByUniqueId(p1CardUniqueId);
        }
        if (p2CardUniqueId != -1)
        {
            p2Data = GameManager.Instance.GetCardDataByUniqueId(p2CardUniqueId);
            p2Draggable = FindDraggableByUniqueId(p2CardUniqueId);
        }

        // 2. Ölecekleri Önceden Hesapla (Animasyona haber vermek için)
        if (p1Data != null && p2Data != null)
        {
            int p1TempHp = liveCardHealths[p1CardUniqueId] - p2Data.attackPoint;
            int p2TempHp = liveCardHealths[p2CardUniqueId] - p1Data.attackPoint;

            if (p1TempHp <= 0) p1WillDie = true;
            if (p2TempHp <= 0) p2WillDie = true;
        }

        // 3. ANÝMASYONU BAÞLAT
        if (p1Draggable != null && p2Draggable != null)
        {
            p1Draggable.RevealCard();
            p2Draggable.RevealCard();

            // Buradaki parametreler ile "Vurulduðu an yok ol" emrini veriyoruz
            yield return CombatVisualManager.Instance.StartClashAnimation(p1Draggable, p2Draggable, p1WillDie, p2WillDie);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        // 4. GERÇEK VERÝLERÝ GÜNCELLE (Database Ýþlemi)
        if (p1Data != null && p2Data != null)
        {
            int p1Hp = liveCardHealths[p1CardUniqueId];
            int p2Hp = liveCardHealths[p2CardUniqueId];

            p1Hp -= p2Data.attackPoint;
            p2Hp -= p1Data.attackPoint;

            liveCardHealths[p1CardUniqueId] = p1Hp;
            liveCardHealths[p2CardUniqueId] = p2Hp;

            // Zaten görsel olarak gizledik, þimdi mantýksal olarak siliyoruz.
            if (p1Hp <= 0) KillCard(1, activeLaneIndex, p1CardUniqueId);
            if (p2Hp <= 0) KillCard(2, activeLaneIndex, p2CardUniqueId);

            ResetRound(lastPasserId);
        }
        else if (p1Data != null && p2Data == null)
        {
            ConquerLane(1);
        }
        else if (p2Data != null && p1Data == null)
        {
            ConquerLane(2);
        }
        else
        {
            ResetRound(lastPasserId);
        }

        // Herkesi güncelle (Ölenler tamamen Destroy olacak)
        GameManager.Instance.BroadcastState();
    }

    // Yardýmcý fonksiyon (GameLogic içine ekle)
    private Draggable FindDraggableByUniqueId(int uniqueId)
    {
        Draggable[] cards = FindObjectsByType<Draggable>(FindObjectsSortMode.None);
        foreach (var card in cards)
        {
            if (card.cardId == uniqueId) return card;
        }
        return null;
    }

    // --- 5. YARDIMCI FONKSÝYONLAR ---

    void KillCard(int ownerId, int slotIndex, int uniqueId)
    {
        // GameManager'a söyle silsin (State ve Görsel)
        GameManager.Instance.ServerKillCard(ownerId, slotIndex, uniqueId);
    }

    void ConquerLane(int winnerId)
    {
        // 1. Puan ver
        if (winnerId == 1) p1VictoryScore++;
        else p2VictoryScore++;

        // 2. Kart Çektir (Ödül)
        GameManager.Instance.SpawnCard(winnerId);

        // 3. Bölgeyi Ýlerle
        activeLaneIndex++;

        // Oyun Bitti mi?
        if (activeLaneIndex >= 5)
        {
            Debug.Log($"OYUN BÝTTÝ! Skor - P1: {p1VictoryScore} | P2: {p2VictoryScore}");
            // TODO: Game Over ekraný
        }
        else
        {
            // Yeni bölge için paslarý sýfýrla, sýra kazananýn olsun (opsiyonel) veya sýrayla
            ResetRound(lastPasserId);
        }
    }

    void ResetRound(int starterId)
    {
        passCounter = 0;

        // Savaþ bitti, yeni tur baþlýyor. State'i güncelle.
        GameManager.Instance.currentState.turnOwnerId = starterId;
        GameManager.Instance.BroadcastState();

        Debug.Log($"Yeni tur baþladý. Sýra Player {starterId}'de.");
    }

    // UI Güncellemesi için kartýn anlýk canýný döndürür
    public int GetLiveHealth(int uniqueId)
    {
        if (liveCardHealths.ContainsKey(uniqueId)) return liveCardHealths[uniqueId];
        return 0;
    }
}