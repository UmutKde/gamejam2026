using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLogic : MonoBehaviour
{
    public static GameLogic Instance;
    // Element Türleri
    public enum ElementType
    {
        Fire,       // Ateþ
        Water,      // Su
        Earth,      // Toprak
        Air,        // Hava
        Electric    // Elektrik
    }

    // Vuruþ Tipi (Renkler buna göre belirlenecek)
    public enum DamageInteraction
    {
        Neutral,        // Beyaz (x1.0 Hasar) - Nötr
        Advantage,      // Sarý (x1.25 Hasar) - Ýç Döngü (Hafif Üstünlük)
        Dominance       // Kýrmýzý (x1.5 Hasar) - Dýþ Döngü (Tam Üstünlük)
    }

    // Sonuç Paketi (GameLogic'e hem hasarý hem rengi göndermek için)
    public struct CombatResult
    {
        public int finalDamage;
        public DamageInteraction interactionType;
    }

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
    // --- 4. SAVAÞ MEKANÝÐÝ (GÜNCEL - ELEMENT DESTEKLÝ) ---
    IEnumerator ResolveCombatCoroutine()
    {
        Debug.Log($"--- {activeLaneIndex}. BÖLGE SAVAÞI BAÞLADI ---");

        // 1. Mevcut Slotlardaki Kart ID'lerini al
        int p1CardUniqueId = GameManager.Instance.currentState.p1Slots[activeLaneIndex];
        int p2CardUniqueId = GameManager.Instance.currentState.p2Slots[activeLaneIndex];

        CardData p1Data = null;
        CardData p2Data = null;
        Draggable p1Draggable = null;
        Draggable p2Draggable = null;

        // "Ölecek mi?" bayraklarý
        bool p1WillDie = false;
        bool p2WillDie = false;

        // --- VERÝLERÝ TOPLA ---
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

        // --- SAVAÞ HESAPLAMALARI ---
        // Eðer iki tarafta da kart varsa savaþtýr
        if (p1Data != null && p2Data != null)
        {
            // A. ELEMENT HASARLARINI HESAPLA
            // P1 saldýrýyor -> P2 savunuyor
            var p1AttackResult = ElementLogic.CalculateDamage(p1Data.attackPoint, p1Data.element, p2Data.element);

            // P2 saldýrýyor -> P1 savunuyor
            var p2AttackResult = ElementLogic.CalculateDamage(p2Data.attackPoint, p2Data.element, p1Data.element);

            // Loglarda görelim (Debug için)
            Debug.Log($"P1 ({p1Data.element}) vurdu: {p1AttackResult.finalDamage} Hasar ({p1AttackResult.interactionType})");
            Debug.Log($"P2 ({p2Data.element}) vurdu: {p2AttackResult.finalDamage} Hasar ({p2AttackResult.interactionType})");

            // B. ÖLÜM TAHMÝNÝ (Animasyon için)
            // Mevcut canlardan hesaplanan hasarý çýkarýp kontrol ediyoruz
            int p1TempHp = liveCardHealths[p1CardUniqueId] - p2AttackResult.finalDamage;
            int p2TempHp = liveCardHealths[p2CardUniqueId] - p1AttackResult.finalDamage;

            if (p1TempHp <= 0) p1WillDie = true;
            if (p2TempHp <= 0) p2WillDie = true;

            // C. ANÝMASYONU OYNAT
            if (p1Draggable != null && p2Draggable != null)
            {
                p1Draggable.RevealCard();
                p2Draggable.RevealCard();

                // Animasyon yöneticisine kimin öleceðini söyleyerek baþlat
                yield return CombatVisualManager.Instance.StartClashAnimation(p1Draggable, p2Draggable, p1WillDie, p2WillDie);
            }
            if (FloatingTextManager.Instance != null)
            {
                // P2'nin tepesinde, P1'den yediði hasar çýksýn
                FloatingTextManager.Instance.ShowDamage(
                    p2Draggable.transform.position,
                    p1AttackResult.finalDamage,
                    p1AttackResult.interactionType
                );

                // P1'in tepesinde, P2'den yediði hasar çýksýn
                FloatingTextManager.Instance.ShowDamage(
                    p1Draggable.transform.position,
                    p2AttackResult.finalDamage,
                    p2AttackResult.interactionType
                );
            }

            // D. GERÇEK HASARI UYGULA (Database Güncellemesi)
            int p1CurrentHp = liveCardHealths[p1CardUniqueId];
            int p2CurrentHp = liveCardHealths[p2CardUniqueId];

            p1CurrentHp -= p2AttackResult.finalDamage; // P1, P2'nin vuruþunu yer
            p2CurrentHp -= p1AttackResult.finalDamage; // P2, P1'in vuruþunu yer

            liveCardHealths[p1CardUniqueId] = p1CurrentHp;
            liveCardHealths[p2CardUniqueId] = p2CurrentHp;

            // TODO: Ýleride buraya "Floating Text" (Uçan Sayý) ekleyeceðiz.
            // Örn: ShowDamageNumber(p1Draggable.transform, p2AttackResult.finalDamage, p2AttackResult.interactionType);

            // Ölenleri oyundan sil
            if (p1CurrentHp <= 0) KillCard(1, activeLaneIndex, p1CardUniqueId);
            if (p2CurrentHp <= 0) KillCard(2, activeLaneIndex, p2CardUniqueId);

            // Yeni tura hazýrlan
            ResetRound(lastPasserId);
        }
        // Sadece P1 varsa -> Bölgeyi P1 alýr
        else if (p1Data != null && p2Data == null)
        {
            ConquerLane(1);
        }
        // Sadece P2 varsa -> Bölgeyi P2 alýr
        else if (p2Data != null && p1Data == null)
        {
            ConquerLane(2);
        }
        // Kimse yoksa
        else
        {
            ResetRound(lastPasserId);
        }

        // Son durumu herkese bildir
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
        Debug.Log($"Bölge {activeLaneIndex} fethedildi! Kazanan: {winnerId}");

        // 1. Skoru Ýþle
        if (winnerId == 1) p1VictoryScore++;
        else p2VictoryScore++;

        // 2. MEVCUT ÖDÜL: Kazananýn ödül kartýný ver (Bu hemen çýksýn, önden gitsin)
        GameManager.Instance.SpawnCard(winnerId);

        // 3. Bölgeyi Ýlerle
        activeLaneIndex++;

        // Oyun Bitti mi?
        if (activeLaneIndex >= 5)
        {
            Debug.Log($"OYUN BÝTTÝ! Skor - P1: {p1VictoryScore} | P2: {p2VictoryScore}");
            // TODO: Game Over iþlemleri
        }
        else
        {
            // --- KRÝTÝK NOKTA ---
            // Burada eski 'for' döngüsü KESÝNLÝKLE OLMAMALI.
            // Sadece bu Coroutine baþlatýcý satýr olmalý:
            StartCoroutine(DistributeNewRoundCards());

            // Yeni bölgeye geç ve paslarý sýfýrla
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
    // Kartlarý sýrayla, bir sana bir ona (seri þekilde) daðýtan fonksiyon
    // Bu fonksiyon sýnýfýn içinde, diðer metodlarýn yanýnda durmalý
    IEnumerator DistributeNewRoundCards()
    {
        // Toplam 2 tur dönecek (+2 kart için)
        for (int i = 0; i < 2; i++)
        {
            // Bekleme süresi: Ýlk kartlarýn animasyonu karýþmasýn diye baþta da azýcýk bekle
            yield return new WaitForSeconds(0.2f);

            // 1. Kart: Player 1'e fýrlat
            GameManager.Instance.SpawnCard(1);

            // Bekle: P1'in kartý havada süzülürken zaman geçsin
            yield return new WaitForSeconds(0.4f);

            // 2. Kart: Player 2'ye fýrlat
            GameManager.Instance.SpawnCard(2);

            // Bekle: Diðer tura geçmeden önce
            yield return new WaitForSeconds(0.4f);
        }
    }
}