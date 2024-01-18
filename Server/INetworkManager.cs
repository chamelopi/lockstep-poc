namespace Server;


public interface INetworkManager : IDisposable
{
    /**
     * Checks for new network packets and other events like disconnects, connects and timeouts.
     * Should be called periodically in the main loop.
     */
    public void PollEvents();
    public IEnumerable<int> GetPlayerIds();
    public IEnumerable<Client> GetClients();
    /**
     * Returns the local player's ID
     */
    public int GetLocalPlayer();
    public bool IsServer();
    /**
     * Defines an event handler for a packet type. There can only be one handler per type.
     */
    public void AddCallback(PacketType type, PacketHandler handler);
    /**
     * Removes a previously defined packet handler
     */
    public void RemoveCallback(PacketType type);
    /**
     * Updates this client's state. Broadcasts the update to all other clients.
     */
    public void UpdateLocalState(ClientState state);
    public Client GetLocalClient();
    /**
     * Broadcasts a network packet to all other clients. This client does NOT receive its own packet.
     */
    public void QueuePacket<T>(T packet) where T : NetworkPacket;
    /**
     * Returns true if all players are done with their turn and everyone can step their simulation.
     * Automatically resets all player's states on this client to indicate that the others are NOT done with the next turn.
     */
    public bool CanAdvanceTurn();
    /**
     * Signal to other players that this client elapsed its turn time, and that it is ready to advance to the next turn
     */
    public void SignalNextTurn(int currentTurn);


    delegate void PacketHandler(NetworkPacket packet);
}


// We will probably have another NetworkManager for DOTSNet/Mirror