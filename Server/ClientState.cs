namespace Server;

/**
 * Represents the state of the other clients
 */
public class Client
{
    public ClientState State { get; set; }
    public uint PeerId { get; set; }
    public int PlayerId { get; set; }
    public string PlayerName { set; get; }
    public bool CurrentTurnDone { get; set; }
}