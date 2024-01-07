using System.Runtime.InteropServices;
using ENet;

namespace Server {

    public interface INetworkManager : IDisposable {
        public void PollEvents();
        public IEnumerable<uint> GetConnectedClients();
    }

    /**
     * Mocks network for single-player or local only simulations
     */
    public class NoopNetworkManager : INetworkManager
    {
        public void Dispose()
        {
            // Do nothing
        }

        public IEnumerable<uint> GetConnectedClients()
        {
            return new List<uint>();
        }

        public void PollEvents() {
            // do nothing
        }
    }


    public class NetworkManager : INetworkManager {
        private Host host;
        private bool isServer;
        private Peer? peer;

        private HashSet<uint> remotePeers;

        private NetworkManager(Host host, bool isServer) {
            ENet.Library.Initialize();
            this.host = host;
            this.isServer = isServer;
            this.remotePeers = new();
        }


        public static NetworkManager NewServer(ushort port, int maxClients = 8) {
            var nm = new NetworkManager(new Host(), true);
            var address = new Address
            {
                Port = port
            };
            nm.host.Create(address, maxClients);
            return nm;
        }

        public static NetworkManager NewClient(string ip, ushort port) {
            var nm = new NetworkManager(new Host(), true);
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
        public void PollEvents() {
            Event netEvent;
            bool polled = false;

            while(!polled) {
                // Check all events 
                if (host.CheckEvents(out netEvent) <= 0) {
                    if (host.Service(15, out netEvent) <= 0) {
                        break;
                    }
                    polled = true;
                }

                switch(netEvent.Type) {
                    case EventType.Connect:
                        if (!isServer) {
                            Console.WriteLine("Connected to server!");
                        } else {
                            Console.WriteLine("Peer " + netEvent.Peer.ID + " connected!");
                            remotePeers.Add(netEvent.Peer.ID);
                            // Tell other clients about this client
                            var bytes = new ClientConnectPacket[]{ new ClientConnectPacket { connected = true, magic = 0xcafe, peerID = netEvent.Peer.ID }};
                            var packet = default(Packet);
                            packet.Create(MemoryMarshal.Cast<ClientConnectPacket, byte>(bytes).ToArray());
                            host.Broadcast(0, ref packet);
                        }
                        break;
                    case EventType.Disconnect:
                        Console.WriteLine("Peer " + netEvent.Peer.ID + " disconnected!");
                        remotePeers.Remove(netEvent.Peer.ID);
                        break;
                    case EventType.Timeout:
                        if (isServer) {
                            Console.WriteLine("Connection to peer " + netEvent.Peer.ID + " has timed out!");
                            remotePeers.Remove(netEvent.Peer.ID);
                        } else {
                            Console.WriteLine("Connection has timed out :(");
                        }
                        break;
                    case EventType.Receive:
                        Console.WriteLine("Packet received from " + netEvent.Peer.ID + " - Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);

                        var data = new byte[netEvent.Packet.Length];
                        netEvent.Packet.CopyTo(data);
                        if (IsClientConnectStatus(data)) {
                            var pack = MemoryMarshal.Cast<byte, ClientConnectPacket>(data);
                            if (pack.Length != 1) {
                                Console.WriteLine("Malformed client connection status package");
                                break;
                            }
                            remotePeers.Add(pack[0].peerID);
                        }

                        netEvent.Packet.Dispose();
                        break;
                    default:
                        break;
                }
            }
        }

        public void Dispose()
        {
            // Properly disconnect if we can, to tell server that we are gone
            peer?.Disconnect(0);
            host.Dispose();
            ENet.Library.Deinitialize();
        }

        public IEnumerable<uint> GetConnectedClients()
        {
            return remotePeers;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct ClientConnectPacket {
            public ushort magic;
            public uint peerID;
            public bool connected;
        }

        // Length: 7
        // CA FE [4 byte ID] [1 for connected, 0 for disconnected]
        private bool IsClientConnectStatus(byte[] data) {
            return data[0] == 0xca && data[1] == 0xfe && data.Length == 7;
        }
    }
}