using System.Collections.Generic;
using UnityEngine;
using ENet;
using System.Linq;

namespace Server
{
    public class MultiplayerNetworkManager : INetworkManager
    {
        private Host host;
        private bool isServer;
        private Peer? peer;

        private Dictionary<int, Client> remotePeers;
        private Dictionary<PacketType, INetworkManager.PacketHandler> callbacks;
        private int myPlayerId;
        public int lastTurnSignaled = -1;

        public NetworkingStats stats;

        private MultiplayerNetworkManager(Host host, bool isServer)
        {
            ENet.Library.Initialize();
            this.host = host;
            this.isServer = isServer;
            this.remotePeers = new();
            this.callbacks = new();
            this.stats = new();
        }


        public static MultiplayerNetworkManager NewServer(ushort port, int maxClients = 8)
        {
            var nm = new MultiplayerNetworkManager(new Host(), true);
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
                // We don't have our own peer ID
                PeerId = 0,
                PlayerId = 1,
                PlayerName = "Player 1",
                State = ClientState.Waiting,
            });

            return nm;
        }

        public static MultiplayerNetworkManager NewClient(string ip, ushort port)
        {
            var nm = new MultiplayerNetworkManager(new Host(), false);
            var address = new Address
            {
                Port = port,
            };
            address.SetHost(ip);
            nm.host.Create();
            nm.peer = nm.host.Connect(address);

            return nm;
        }

        public void PollEvents()
        {
            ENet.Event netEvent;
            bool polled = false;

            while (!polled)
            {
                // Check all events 
                if (host.CheckEvents(out netEvent) <= 0)
                {
                    // No timeout (nonblocking) because we run in Unity's game loop
                    if (host.Service(0, out netEvent) <= 0)
                    {
                        break;
                    }
                    polled = true;
                }

                UpdateStats(netEvent.Peer);

                switch (netEvent.Type)
                {
                    case ENet.EventType.Connect:
                        if (!isServer)
                        {
                            Debug.Log("NM: Connected to server!");
                        }
                        else
                        {
                            // TODO: This logic will break if we automatically handle disconnects - peer IDs might change!
                            Debug.Log("NM: Peer " + netEvent.Peer.ID + " connected, will be assigned ID " + (remotePeers.Count + 1));
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
                    case ENet.EventType.Disconnect:
                        Debug.LogWarning("NM: Peer " + netEvent.Peer.ID + " disconnected!");

                        {
                            var playerId = remotePeers.Where(client => client.Value.PeerId == netEvent.Peer.ID).First().Key;
                            remotePeers.Remove(playerId);
                        }

                        break;
                    case ENet.EventType.Timeout:
                        if (isServer)
                        {
                            Debug.LogError("NM: Connection to peer " + netEvent.Peer.ID + " has timed out!");
                            var playerId = remotePeers.Where(client => client.Value.PeerId == netEvent.Peer.ID).First().Key;
                            remotePeers.Remove(playerId);
                        }
                        else
                        {
                            Debug.LogError("NM: Connection has timed out :(");
                        }
                        break;
                    case ENet.EventType.Receive:
                        using (netEvent.Packet)
                        {
                            HandleReceivedPacket(netEvent);
                        }
                        break;
                    default:
                        break;
                }
            }
        }



        private void HandleReceivedPacket(ENet.Event netEvent)
        {
            var type = NetworkPacket.DetectType(netEvent.Packet);
            if (type == PacketType.ServerGreeting)
            {
                if (isServer)
                {
                    Debug.LogWarning("NM: Received ServerGreeting as Server, ignoring!");
                    return;
                }

                var greeting = NetworkPacket.Deserialize<ServerGreetingPacket>(netEvent.Packet);

                Debug.Log($"NM: Received ServerGreeting. Our ID is {greeting.AssignedPlayerId}");

                myPlayerId = greeting.AssignedPlayerId;

                var myClientState = new Client
                {
                    CurrentTurnDone = false,
                    PeerId = netEvent.Peer.ID,
                    PlayerId = greeting.AssignedPlayerId,
                    State = ClientState.Waiting,
                    PlayerName = "Player " + myPlayerId,
                };
                remotePeers.Add(greeting.AssignedPlayerId, myClientState);

                // Send Hello to everyone
                var hello = NetworkPacket.Serialize(new HelloPacket
                {
                    PkgType = PacketType.Hello,
                    ClientState = myClientState.State,
                    PlayerId = myPlayerId,
                    PlayerName = myClientState.PlayerName,
                    CurrentTurnDone = myClientState.CurrentTurnDone,
                });
                host.Broadcast(0, ref hello);

                CallHandler(type, greeting);
            }
            else if (type == PacketType.Hello)
            {
                var hello = NetworkPacket.Deserialize<HelloPacket>(netEvent.Packet);

                Debug.Log($"NM: Received Hello from {hello.PlayerName}");

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

                    var myClientState = remotePeers[myPlayerId];
                    // Send Hello back
                    var ourHello = NetworkPacket.Serialize(new HelloPacket
                    {
                        PkgType = PacketType.Hello,
                        ClientState = myClientState.State,
                        PlayerId = myPlayerId,
                        PlayerName = myClientState.PlayerName,
                        CurrentTurnDone = myClientState.CurrentTurnDone,
                    });
                    netEvent.Peer.Send(0, ref ourHello);

                    Debug.Log("NM: Greeted them back!");
                }

                CallHandler(type, hello);
            }
            else if (type == PacketType.StateChange)
            {
                var stateChange = NetworkPacket.Deserialize<StateChangePacket>(netEvent.Packet);
                Debug.Log($"NM: Received StateChange from {stateChange.PlayerId}: {stateChange.NewClientState}");
                if (!remotePeers.ContainsKey(stateChange.PlayerId))
                {
                    // Drop packet because we don't know this player yet. Once they send us a HELLO packet,
                    // we will know their current state.
                    return;
                }
                remotePeers[stateChange.PlayerId].State = stateChange.NewClientState;

                CallHandler(type, stateChange);
            }
            else if (type == PacketType.StartGame)
            {
                CallHandler(type, NetworkPacket.Deserialize<StartGamePacket>(netEvent.Packet));
            }
            else if (type == PacketType.Command)
            {
                CallHandler(type, NetworkPacket.Deserialize<CommandPacket>(netEvent.Packet));
            }
            else if (type == PacketType.EndOfTurn)
            {
                var endOfTurn = NetworkPacket.Deserialize<EndOfTurnPacket>(netEvent.Packet);
                //Debug.Log($"Player {endOfTurn.PlayerId} is done with their turn {endOfTurn.CurrentTurn}!");
                remotePeers[endOfTurn.PlayerId].CurrentTurnDone = true;
            }
            else
            {
                Debug.LogWarning("NM: Unknown Packet received from " + netEvent.Peer.ID + " - Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
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
            if (!remotePeers.ContainsKey(myPlayerId))
            {
                Debug.LogError($"NM: Could not update local state to {state}! No state for myself yet!");
                return;
            }

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
            // TODO: It might not be optimal for all situations to always flush packets immediately. Maybe flush on poll?
            host.Flush();
        }

        public Client GetLocalClient()
        {
            if (IsConnected())
            {
                return remotePeers[myPlayerId];
            }
            else
            {
                return new Client()
                {
                    CurrentTurnDone = false,
                    PeerId = 0,
                    PlayerId = 0,
                    PlayerName = "none",
                    State = ClientState.Disconnected,
                };
            }

        }

        public bool CanAdvanceTurn()
        {
            var allDone = true;
            foreach (var peer in remotePeers)
            {
                allDone = allDone && peer.Value.CurrentTurnDone;
            }

            if (!allDone)
            {
                return false;
            }

            foreach (var peer in remotePeers)
            {
                peer.Value.CurrentTurnDone = false;
            }
            return true;
        }

        public void SignalNextTurn(int currentTurn)
        {
            if (lastTurnSignaled < currentTurn)
            {
                var pack = NetworkPacket.Serialize(new EndOfTurnPacket()
                {
                    PkgType = PacketType.EndOfTurn,
                    PlayerId = myPlayerId,
                    CurrentTurn = currentTurn,
                });

                // Set local instance of this client ready, too
                remotePeers[myPlayerId].CurrentTurnDone = true;

                host.Broadcast(0, ref pack);
                lastTurnSignaled = currentTurn;
            }
        }

        public bool IsConnected()
        {
            if (isServer)
            {
                // Server is always connected to itself, duh
                return true;
            }
            else
            {
                return myPlayerId != 0 && remotePeers.ContainsKey(myPlayerId);
            }
        }

        /**
         * Sync statistics from ENet to our stats object
         */
        private void UpdateStats(Peer peer)
        {
            stats.roundTripTime[peer.ID] = peer.RoundTripTime;
            stats.packetsSent = host.PacketsReceived;
            stats.packetsReceived = host.PacketsReceived;
            stats.bytesSent = host.BytesSent;
            stats.bytesReceived = host.BytesReceived;
        }

        public NetworkingStats GetStats()
        {
            return stats;
        }
    }
}
