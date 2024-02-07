using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ENet;
using Simulation;

namespace Server
{



    public enum PacketType
    {
        ServerGreeting = 1,
        Hello,
        Command,
        StateChange,
        StartGame,
        EndOfTurn,

    }

    public class NetworkPacket
    {
        public static JsonSerializerOptions options = new()
        {
            MaxDepth = 10,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            // Because I will just keep forgetting that only properties are serialized otherwise...
            IncludeFields = true,
        };
        static NetworkPacket()
        {
            options.Converters.Add(new JsonStringEnumConverter());
        }

        public PacketType PkgType { get; set; }

        public static Packet Serialize<T>(T networkPacket) where T : NetworkPacket
        {
            // TODO: Autodetect type from class?
            if (networkPacket.PkgType == 0)
            {
                throw new ArgumentException("PkgType must be set!");
            }
            var json = JsonSerializer.Serialize(networkPacket, options);
            var bytes = Encoding.UTF8.GetBytes(json);
            var packet = default(Packet);
            // We always want reliable packets. We don't need the extra speedup of using
            // unreliable packets because sending just the input over the network does not create
            // much traffic.
            packet.Create(bytes, PacketFlags.Reliable);
            return packet;
        }

        public static PacketType? DetectType(Packet packet)
        {
            var data = new byte[packet.Length];
            packet.CopyTo(data);
            var str = Encoding.UTF8.GetString(data);
            var obj = JsonSerializer.Deserialize<NetworkPacket>(str, options);
            return obj?.PkgType;
        }

        public static T Deserialize<T>(Packet packet) where T : NetworkPacket
        {
            var data = new byte[packet.Length];
            packet.CopyTo(data);
            var str = Encoding.UTF8.GetString(data);
            var obj = JsonSerializer.Deserialize<T>(str, options);
            return obj!;
        }
    }


    /**
     * Sent by server to assign player ID.
     */
    public class ServerGreetingPacket : NetworkPacket
    {
        public int AssignedPlayerId { get; set; }
    }

    /**
     * Sent by server or client to indicate any kind of error
     */
    public class ErrorPacket : NetworkPacket
    {
        public byte PlayerId { get; set; }
        public byte HostId { get; set; }
        public ushort ErrorCode { get; set; }

        public string GetErrorMessage()
        {
            return ErrorCode switch
            {
                0 => "OK",
                1 => "Too many players on server already, cannot join!",
                ushort c => "Unknown error code: " + c,
            };
        }
    }

    public enum ClientState
    {
        Disconnected,
        Waiting,
        Ready,
        InGame,
    }

    /**
     * Sent by all clients on every new connect to notify them about their current state.
     */
    public class HelloPacket : NetworkPacket
    {
        public int PlayerId { get; set; }
        public ClientState ClientState { get; set; }
        public string PlayerName { get; set; }
        public bool CurrentTurnDone { get; set; }
    }

    /**
     * Notifies all network peers of a local state change
     */
    public class StateChangePacket : NetworkPacket
    {
        public int PlayerId { get; set; }
        public ClientState NewClientState { get; set; }
    }

    /**
     * Triggers the start of the game. No additional data required.
     */
    public class StartGamePacket : NetworkPacket
    {
    }

    /**
     * Contains an input command. Embeds struct from Command.cs (?)
     *
     * This may be movement commands, building placement, unit creation, etc.
     */
    public class CommandPacket : NetworkPacket
    {
        public int PlayerId { get; set; }
        public Command Command { get; set; }
    }

    /**
     * Tells other players that this player finished sending inputs for their turn. Once every player
     * finished the turn, all players are allowed to advance their simulation by one step.
     */
    public class EndOfTurnPacket : NetworkPacket
    {
        public int PlayerId { get; set; }
        // The turn this player just ended (i.e. sim.currentTurn *before* incrementing)
        public int CurrentTurn { get; set; }
    }

}