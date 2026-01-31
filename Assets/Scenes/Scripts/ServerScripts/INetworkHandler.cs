public interface INetworkHandler
{
    void SendPacket(string json);
    void OnPacketReceived(string json);
}