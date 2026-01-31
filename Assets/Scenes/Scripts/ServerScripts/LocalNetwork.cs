using UnityEngine;

public class LocalNetwork : MonoBehaviour, INetworkHandler
{
    public GameManager gameManager; // Server (Yönetici)
    public PlayerManager myPlayerManager; // Client (Oyuncu)

    // Oyuncu veriyi gönderdiðinde (Client -> Server)
    public void SendPacket(string json)
    {
        // Gerçek hayatta burada "socket.Send(json)" olurdu.
        // Þimdi direkt müdüre (GameManager) veriyoruz.
        gameManager.ReceivePacketFromClient(json);
    }

    // Sunucudan cevap geldiðinde (Server -> Client)
    public void OnPacketReceived(string json)
    {
        // Gelen veriyi oyuncu yöneticisine ilet
        myPlayerManager.UpdateGameState(json);
    }
}