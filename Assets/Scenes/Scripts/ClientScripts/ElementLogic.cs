using UnityEngine;

public static class ElementLogic
{
    // Çarpanlar
    private const float ADVANTAGE_MULTIPLIER = 1.25f; // Sarý Hasar (Ýç Yýldýz)
    private const float DOMINANCE_MULTIPLIER = 1.50f; // Kýrmýzý Hasar (Dýþ Çember)

    // Sonuç Paketi
    public struct CombatResult
    {
        public int finalDamage;
        public DamageInteraction interactionType;
    }

    public enum DamageInteraction
    {
        Neutral,        // Beyaz
        Advantage,      // Sarý
        Dominance       // Kýrmýzý
    }

    public static CombatResult CalculateDamage(int baseDamage, ElementTypes attacker, ElementTypes defender)
    {
        CombatResult result = new CombatResult();
        float multiplier = 1.0f;
        result.interactionType = DamageInteraction.Neutral;

        // --- 1. DIÞ ÇEMBER (DOMINANCE - KIRMIZI OKLAR) ---
        // Ateþ -> Hava -> Doða -> Elektrik -> Su -> Ateþ
        if (
            (attacker == ElementTypes.Fire && defender == ElementTypes.Air) ||
            (attacker == ElementTypes.Air && defender == ElementTypes.Nature) ||
            (attacker == ElementTypes.Nature && defender == ElementTypes.Electric) ||
            (attacker == ElementTypes.Electric && defender == ElementTypes.Water) ||
            (attacker == ElementTypes.Water && defender == ElementTypes.Fire)
           )
        {
            multiplier = DOMINANCE_MULTIPLIER;
            result.interactionType = DamageInteraction.Dominance;
        }

        // --- 2. ÝÇ YILDIZ (ADVANTAGE - SARI OKLAR) ---
        // Ateþ -> Doða -> Su -> Hava -> Elektrik -> Ateþ
        else if (
            (attacker == ElementTypes.Fire && defender == ElementTypes.Nature) ||
            (attacker == ElementTypes.Nature && defender == ElementTypes.Water) ||
            (attacker == ElementTypes.Water && defender == ElementTypes.Air) ||
            (attacker == ElementTypes.Air && defender == ElementTypes.Electric) ||
            (attacker == ElementTypes.Electric && defender == ElementTypes.Fire)
           )
        {
            multiplier = ADVANTAGE_MULTIPLIER;
            result.interactionType = DamageInteraction.Advantage;
        }

        // Hasarý hesapla (Yuvarlama iþlemi)
        result.finalDamage = Mathf.RoundToInt(baseDamage * multiplier);
        return result;
    }
}