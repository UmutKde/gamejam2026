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
                ResolveCombat();
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
    void ResolveCombat()
    {
        Debug.Log($"--- {activeLaneIndex}. BÖLGE SAVAÞI BAÞLADI ---");

        // 1. O anki lane'deki kartlarý bul
        int p1CardUniqueId = GameManager.Instance.currentState.p1Slots[activeLaneIndex];
        int p2CardUniqueId = GameManager.Instance.currentState.p2Slots[activeLaneIndex];

        CardData p1Data = null;
        CardData p2Data = null;

        // ID'leri kullanarak orjinal datalara (Atak gücü için) ulaþ
        if (p1CardUniqueId != -1) p1Data = GameManager.Instance.GetCardDataByUniqueId(p1CardUniqueId);
        if (p2CardUniqueId != -1) p2Data = GameManager.Instance.GetCardDataByUniqueId(p2CardUniqueId);

        // --- SENARYO 1: KARÞILIKLI ÇARPIÞMA (FETÝH YOK) ---
        if (p1Data != null && p2Data != null)
        {
            // Canlarý çek
            int p1Hp = liveCardHealths[p1CardUniqueId];
            int p2Hp = liveCardHealths[p2CardUniqueId];

            // Birbirlerine vururlar (Kalýcý Hasar)
            p1Hp -= p2Data.attackPoint;
            p2Hp -= p1Data.attackPoint;

            // Canlarý güncelle
            liveCardHealths[p1CardUniqueId] = p1Hp;
            liveCardHealths[p2CardUniqueId] = p2Hp;

            Debug.Log($"Çarpýþma! P1 Kart Caný: {p1Hp}, P2 Kart Caný: {p2Hp}");

            // Ölüm Kontrolü
            if (p1Hp <= 0) KillCard(1, activeLaneIndex, p1CardUniqueId);
            if (p2Hp <= 0) KillCard(2, activeLaneIndex, p2CardUniqueId);

            // Feth SAYILMAZ. Oyun ayný bölgede devam eder.
            // Sýra: En son pas diyen (savaþý baþlatan) kiþide baþlar.
            ResetRound(lastPasserId);
        }
        // --- SENARYO 2: P1 VURDU, P2 BOÞ (FETÝH!) ---
        else if (p1Data != null && p2Data == null)
        {
            Debug.Log("P1 BÖLGEYÝ FETHETTÝ!");
            ConquerLane(1);
        }
        // --- SENARYO 3: P2 VURDU, P1 BOÞ (FETÝH!) ---
        else if (p2Data != null && p1Data == null)
        {
            Debug.Log("P2 BÖLGEYÝ FETHETTÝ!");
            ConquerLane(2);
        }
        // --- SENARYO 4: ÝKÝSÝ DE BOÞ ---
        else
        {
            Debug.Log("Savaþ alaný sessiz... Devam.");
            ResetRound(lastPasserId);
        }
        GameManager.Instance.BroadcastState();
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