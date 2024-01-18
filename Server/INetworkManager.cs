namespace Server;


public interface INetworkManager : IDisposable
{
    public void PollEvents();
    public IEnumerable<int> GetPlayerIds();
    public IEnumerable<Client> GetClients();
    public int GetLocalPlayer();
    public bool IsServer();
    public void AddCallback(PacketType type, PacketHandler handler);
    public void RemoveCallback(PacketType type);
    public void UpdateLocalState(ClientState state);
    public Client GetLocalClient();
    public void QueuePacket<T>(T packet) where T : NetworkPacket;

    delegate void PacketHandler(NetworkPacket packet);
}



// We will probably have another NetworkManager for DOTSNet/Mirror


