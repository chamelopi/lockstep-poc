using ENet;

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

// We will probably have another NetworkManager for DOTSNet/Mirror


public class ENetNetworkManager : INetworkManager
{
    private Host host;
    private bool isServer;
    private Peer? peer;

    private Dictionary<int, Client> remotePeers;
    private Dictionary<PacketType, INetworkManager.PacketHandler> callbacks;
    private int myPlayerId;

    private ENetNetworkManager(Host host, bool isServer)
    {
        ENet.Library.Initialize();
        this.host = host;
        this.isServer = isServer;
        this.remotePeers = new();
        this.callbacks = new();
    }


    public static ENetNetworkManager NewServer(ushort port, int maxClients = 8)
    {
        var nm = new ENetNetworkManager(new Host(), true);
        var address = new Address
        {
            Port = port
        };
        nm.host.Create(address, maxClients);

        // Set up our own state
        nm.myPlayerId = 1;
        nm.remotePeers.Add(1, new Client
        {
            CurrentTurnDone = false,
            // TODO: Do we have our own peer id?
            PeerId = 0,
            PlayerId = 1,
            PlayerName = "Player 1",
            State = ClientState.Waiting,
        });

        return nm;
    }

    public static ENetNetworkManager NewClient(string ip, ushort port)
    {
        var nm = new ENetNetworkManager(new Host(), false);
        var address = new Address
        {
            Port = port,
        };
        address.SetHost(ip);
        nm.host.Create();
        nm.peer = nm.host.Connect(address);
        return nm;
    }

    // TODO: What to return? Is network manager responsible for command serialization?
    // should this return a IEnumerable<Command>?
    public void PollEvents()
    {
        Event netEvent;
        bool polled = false;

        while (!polled)
        {
            // Check all events 
            if (host.CheckEvents(out netEvent) <= 0)
            {
                if (host.Service(15, out netEvent) <= 0)
                {
                    break;
                }
                polled = true;
            }

            switch (netEvent.Type)
            {
                case EventType.Connect:
                    if (!isServer)
                    {
                        Console.WriteLine("Connected to server!");
                    }
                    else
                    {
                        // TODO: This logic will break if we automatically handle disconnects - peer IDs might change!
                        Console.WriteLine("Peer " + netEvent.Peer.ID + " connected, will be assigned ID " + (remotePeers.Count + 1));
                        var greeting = new ServerGreetingPacket
                        {
                            PkgType = PacketType.ServerGreeting,
                            AssignedPlayerId = remotePeers.Count + 1,
                        };
                        var packet = NetworkPacket.Serialize(greeting);
                        // Send only to the other client - it will send a HelloPacket to everyone later
                        netEvent.Peer.Send(0, ref packet);
                    }
                    break;
                case EventType.Disconnect:
                    Console.WriteLine("Peer " + netEvent.Peer.ID + " disconnected!");

                    {
                        var playerId = remotePeers.Where(client => client.Value.PeerId == netEvent.Peer.ID).First().Key;
                        remotePeers.Remove(playerId);
                    }

                    break;
                case EventType.Timeout:
                    if (isServer)
                    {
                        Console.WriteLine("Connection to peer " + netEvent.Peer.ID + " has timed out!");
                        var playerId = remotePeers.Where(client => client.Value.PeerId == netEvent.Peer.ID).First().Key;
                        remotePeers.Remove(playerId);
                    }
                    else
                    {
                        Console.WriteLine("Connection has timed out :(");
                    }
                    break;
                case EventType.Receive:
                    Console.WriteLine("Packet received from " + netEvent.Peer.ID + " - Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);

                    var type = NetworkPacket.DetectType(netEvent.Packet);
                    if (type == PacketType.ServerGreeting)
                    {
                        if (isServer)
                        {
                            Console.WriteLine("ERROR: Received ServerGreeting as Server, ignoring!");
                            netEvent.Packet.Dispose();
                            break;
                        }

                        var greeting = NetworkPacket.Deserialize<ServerGreetingPacket>(netEvent.Packet);

                        Console.WriteLine($"Received ServerGreeting. Our ID is {greeting.AssignedPlayerId}");

                        myPlayerId = greeting.AssignedPlayerId;

                        var myState = new Client
                        {
                            CurrentTurnDone = false,
                            PeerId = netEvent.Peer.ID,
                            PlayerId = greeting.AssignedPlayerId,
                            State = ClientState.Waiting,
                            PlayerName = "Player " + myPlayerId,
                        };
                        remotePeers.Add(greeting.AssignedPlayerId, myState);

                        // Send Hello to everyone
                        var hello = NetworkPacket.Serialize(new HelloPacket
                        {
                            PkgType = PacketType.Hello,
                            ClientState = myState.State,
                            PlayerId = myPlayerId,
                            PlayerName = myState.PlayerName,
                            CurrentTurnDone = myState.CurrentTurnDone,
                        });
                        host.Broadcast(0, ref hello);

                        CallHandler(type, greeting);
                    }
                    else if (type == PacketType.Hello)
                    {
                        var hello = NetworkPacket.Deserialize<HelloPacket>(netEvent.Packet);

                        Console.WriteLine($"Received Hello from {hello.PlayerName}");

                        // If we don't know them yet, register them and send our own hello back
                        if (!remotePeers.ContainsKey(hello.PlayerId))
                        {
                            var theirState = new Client
                            {
                                CurrentTurnDone = hello.CurrentTurnDone,
                                PeerId = netEvent.Peer.ID,
                                PlayerId = hello.PlayerId,
                                State = hello.ClientState,
                                PlayerName = hello.PlayerName,
                            };
                            remotePeers.Add(hello.PlayerId, theirState);

                            var myState = remotePeers[myPlayerId];
                            // Send Hello back
                            var ourHello = NetworkPacket.Serialize(new HelloPacket
                            {
                                PkgType = PacketType.Hello,
                                ClientState = myState.State,
                                PlayerId = myPlayerId,
                                PlayerName = myState.PlayerName,
                                CurrentTurnDone = myState.CurrentTurnDone,
                            });
                            netEvent.Peer.Send(0, ref ourHello);

                            Console.WriteLine("Greeted them back!");
                        }

                        CallHandler(type, hello);
                    }
                    else if (type == PacketType.StateChange)
                    {
                        var stateChange = NetworkPacket.Deserialize<StateChangePacket>(netEvent.Packet);
                        Console.WriteLine($"Received StateChange from {stateChange.PlayerId}: {stateChange.NewClientState}");
                        remotePeers[stateChange.PlayerId].State = stateChange.NewClientState;

                        CallHandler(type, stateChange);
                    }
                    else if (type == PacketType.StartGame)
                    {
                        CallHandler(type, NetworkPacket.Deserialize<StartGamePacket>(netEvent.Packet));
                    }
                    else if (type == PacketType.Command) {
                        CallHandler(type, NetworkPacket.Deserialize<CommandPacket>(netEvent.Packet));
                    }
                    netEvent.Packet.Dispose();
                    break;
                default:
                    break;
            }
        }
    }

    private void CallHandler(PacketType? type, NetworkPacket hello)
    {
        if (type != null && callbacks.ContainsKey(type.Value))
        {
            callbacks[type.Value](hello);
        }
    }

    public void Dispose()
    {
        // Properly disconnect if we can, to tell server that we are gone
        peer?.Disconnect(0);
        host.Dispose();
        ENet.Library.Deinitialize();
    }

    public IEnumerable<int> GetPlayerIds()
    {
        return remotePeers.Keys;
    }

    public IEnumerable<Client> GetClients()
    {
        return remotePeers.Values;
    }

    public bool IsServer()
    {
        return isServer;
    }

    public void AddCallback(PacketType type, INetworkManager.PacketHandler handler)
    {
        callbacks.Add(type, handler);
    }

    public void RemoveCallback(PacketType type)
    {
        callbacks.Remove(type);
    }

    public int GetLocalPlayer()
    {
        return myPlayerId;
    }

    public void UpdateLocalState(ClientState state)
    {
        remotePeers[myPlayerId].State = state;
        // Notify other clients of the state change
        var changePacket = NetworkPacket.Serialize(new StateChangePacket()
        {
            PlayerId = myPlayerId,
            NewClientState = state,
            PkgType = PacketType.StateChange,
        });
        host.Broadcast(0, ref changePacket);
        host.Flush();
    }

    
    public void QueuePacket<T>(T packet) where T : NetworkPacket
    {
        var serialized = NetworkPacket.Serialize(packet);
        host.Broadcast(0, ref serialized);
        // TODO: It might not be optimal for all situations to always flush packets immediately
        host.Flush();
    }

    public Client GetLocalClient()
    {
        return remotePeers[myPlayerId];
    }
}
