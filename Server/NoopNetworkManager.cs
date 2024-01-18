namespace Server;

/**
 * Mocks network for single-player or local only simulations
 */
public class NoopNetworkManager : INetworkManager
{
    private Client myClient = new Client
    {
        CurrentTurnDone = false,
        PeerId = 0,
        PlayerId = 1,
        PlayerName = "Player 1",
        State = ClientState.Waiting,
    };

    public void AddCallback(PacketType type, INetworkManager.PacketHandler handler)
    {
        // Do nothing - no network events ever happen
    }

    public void Dispose()
    {
        // Do nothing
    }

    public IEnumerable<Client> GetClients()
    {
        return new List<Client> { myClient };
    }

    public IEnumerable<int> GetPlayerIds()
    {
        return new List<int>(1);
    }

    public int GetLocalPlayer()
    {
        // Local player is always 1 when not in multiplayer
        return 1;
    }

    public bool IsServer()
    {
        return false;
    }

    public void PollEvents()
    {
        // do nothing
    }

    public void RemoveCallback(PacketType type)
    {
        // Do nothing - no network events ever happen
    }

    public void UpdateLocalState(ClientState state)
    {
        myClient.State = state;
    }

    public void QueuePacket<T>(T packet) where T : NetworkPacket
    {
        // Noop, we don't have networking
    }

    public Client GetLocalClient()
    {
        return myClient;
    }
}