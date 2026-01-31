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

    void SpawnCard(int ownerId)
    {
        int newId = globalCardIdCounter++;

        ServerCardSpawn packet = new ServerCardSpawn();
        packet.cardId = newId;
        packet.ownerId = ownerId;

        string json = JsonUtility.ToJson(packet);

        if (p1Network) p1Network.OnPacketReceived(json);
        if (p2Network) p2Network.OnPacketReceived(json);
    }

    public void ReceivePacketFromClient(string json)
    {
        PlayerAction action = JsonUtility.FromJson<PlayerAction>(json);

        if (action.playerId != currentState.turnOwnerId)
        {
            Debug.LogWarning($"Sýra Player {action.playerId}'de deðil!");
            return;
        }

        if (action.actionType == "PlayCard")
        {
            if (action.playerId == 1) currentState.p1Slots[action.slotIndex] = action.cardId;
            else currentState.p2Slots[action.slotIndex] = action.cardId;
        }
        else if (action.actionType == "EndTurn")
        {
            currentState.turnOwnerId = (currentState.turnOwnerId == 1) ? 2 : 1;

            Debug.Log($"Sunucu: Tur deðiþti. Yeni Sýra: Player {currentState.turnOwnerId}");
        }

        BroadcastState();
    }

    void BroadcastState()
    {
        string json = JsonUtility.ToJson(currentState);
        if (p1Network) p1Network.OnPacketReceived(json);
        if (p2Network) p2Network.OnPacketReceived(json);
    }
}