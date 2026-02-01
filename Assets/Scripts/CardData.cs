using UnityEngine;

// Element türlerini burada tanımlıyoruz
public enum ElementTypes
{
    Fire,       // Ateş
    Water,      // Su
    Nature,     // Doğa
    Electric,   // Elektrik
    Air         // Hava
}
public enum CardType
{
    Minion,
    Ground,
    Mask
}


[CreateAssetMenu(fileName = "New Card", menuName = "GameJam/Universal Card Data")]
public class CardData : ScriptableObject
{
    [Header("Temel Bilgiler")]
    public int CardId;
    public string cardName;         // Kartın İsmi (Örn: Magma Golem)
    public Sprite cardImage;        // Minyonun Resmi
    public ElementTypes element;    // Element Türü
    public CardType cardType;

    [Header("Değerler")]
    public int attackPoint;         // Saldırı Gücü
    public int healthPoint;         // Can Değeri

    [Header("Oyun İçi Durumlar")]
    public bool isReversed;         // Ters mi? (Mekanik için)
    public bool whoPlayed;          // Kim oynadı? (Oyuncu/Rakip)
}