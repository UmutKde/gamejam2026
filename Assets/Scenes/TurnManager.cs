using UnityEngine;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;
    public bool isPlayerOneTurn = true; // True: Oyuncu 1, False: Oyuncu 2 (Rakip)

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ChangeTurn()
    {
        isPlayerOneTurn = !isPlayerOneTurn;

        Debug.Log("Tur Deðiþti! Sýra: " + (isPlayerOneTurn ? "Oyuncu 1" : "Oyuncu 2"));

        Draggable[] allCards = FindObjectsOfType<Draggable>();
        foreach (Draggable card in allCards)
        {
            card.UpdateCardVisualsAndState();
        }
    }
}