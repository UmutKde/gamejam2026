using UnityEngine;
using Mirror;

public class GameNetworkPlayer : NetworkBehaviour
{
    public static GameNetworkPlayer LocalPlayer;
    [SyncVar] public int playerId;

    public override void OnStartLocalPlayer()
    {
        LocalPlayer = this;
        PlayerManager pm = FindFirstObjectByType<PlayerManager>();
        if (pm != null) pm.InitNetwork(this);
        CmdRegisterPlayer();
    }

    [Command]
    public void CmdRegisterPlayer()
    {
        if (GameManager.Instance != null) GameManager.Instance.RegisterNetworkPlayer(this);
    }

    [Command]
    public void CmdSendPacketToServer(string json)
    {
        if (GameManager.Instance != null) GameManager.Instance.ReceivePacketFromClient(json);
    }

    [ClientRpc]
    public void RpcReceivePacketToClient(string json)
    {
        PlayerManager pm = FindFirstObjectByType<PlayerManager>();
        if (pm != null) pm.UpdateGameState(json);
    }

    [ClientRpc]
    public void RpcPlayClashAnimation(
        int p1CardId, int p2CardId,
        bool p1Dies, bool p2Dies,
        int p1Damage, int p1Type, // P1'in yediði hasar
        int p2Damage, int p2Type  // P2'nin yediði hasar
    )
    {
        PlayerManager pm = FindFirstObjectByType<PlayerManager>();
        if (pm != null)
        {
            pm.TriggerLocalClash(p1CardId, p2CardId, p1Dies, p2Dies, p1Damage, p1Type, p2Damage, p2Type);
        }
    }

    // --- YENÝ EKLENEN KISIM: HASAR YAZISI EMRÝ ---
    [ClientRpc]
    public void RpcShowDamage(int targetCardId, int damageAmount, int interactionTypeIndex)
    {
        PlayerManager pm = FindFirstObjectByType<PlayerManager>();
        if (pm != null)
        {
            pm.TriggerLocalDamagePopup(targetCardId, damageAmount, interactionTypeIndex);
        }
    }

    // --- MASKE FEDA ETME EMRÝ ---
    [Command]
    public void CmdSacrificeMask(int playerId, int elementIndex)
    {
        // Enum int olarak gelir, Server'da tekrar Enum'a çevrilir
        ElementTypes element = (ElementTypes)elementIndex;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SacrificeMask(playerId, element);
        }
    }
}