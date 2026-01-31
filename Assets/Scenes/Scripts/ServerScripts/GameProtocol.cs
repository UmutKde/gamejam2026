[System.Serializable]
public class PlayerAction
{
    public string actionType;
    public int cardId;
    public int slotIndex;
    public int playerId;
}

[System.Serializable]
public class ServerCardSpawn
{
    public string type = "SpawnCard";
    public int uniqueId;
    public int cardDataId;
    public int ownerId;
}

[System.Serializable]
public class GameState
{
    public int turnOwnerId;
    public int[] p1Slots = new int[5];
    public int[] p2Slots = new int[5];
}