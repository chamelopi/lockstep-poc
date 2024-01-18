using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ENet;

namespace Server;

public enum PacketType
{
    ServerGreeting,
    Hello,
    Command,
    StateChange,
}

// TODO: Can we ensure that 'PkgType' is always correctly filled on serialization?
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
        // FIXME: Ensure that correct PkgType is set at all times!
        var json = JsonSerializer.Serialize(networkPacket, options);
        var bytes = Encoding.UTF8.GetBytes(json);
        var packet = default(Packet);
        packet.Create(bytes);
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

public class StateChangePacket : NetworkPacket
{
    public int PlayerId { get; set; }
    public ClientState NewClientState { get; set; }
}

public class StartGamePacket
{
    // TODO: Implement

}

/**
 * Contains an input command. Embeds struct from Command.cs (?)
 *
 * This may be movement commands, building placement, unit creation, etc.
 */
public class CommandPacket
{
    // TODO: Implement
}

/**
 * Tells other players that this player finished sending inputs for their turn. Once every player
 * finished the turn, all players are allowed to advance their simulation by one step.
 */
public class EndOfTurnPacket
{
    // TODO: Implement
}

