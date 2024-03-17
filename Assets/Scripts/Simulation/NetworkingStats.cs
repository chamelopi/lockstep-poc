using System.Collections.Generic;
using System.Linq;

namespace Server
{
    public class NetworkingStats
    {
        public uint packetsSent;
        public uint packetsReceived;
        public uint bytesSent;
        public uint bytesReceived;
        public Dictionary<uint, uint> roundTripTime = new();

        public override string ToString() {
            return $"Packets sent: {packetsSent}\nPackets received: {packetsReceived}\nBytes sent: {bytesSent}\nBytes received: {bytesReceived}\n"
                + "Round trip times: "
                + roundTripTime.Select(kv => $"\n  Peer {kv.Key}: {kv.Value}").Aggregate((a,b) => a + b);
        }
    }
}
