using System.Buffers;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using ENet;

namespace Server
{
    public interface NetworkPacket
    {
        Packet serialize();

        public static NetworkPacket? deserialize(Packet packet)
        {
            var data = new byte[packet.Length];
            packet.CopyTo(data);
            ushort magic = (ushort)(data[0] << 8 | data[1]);

            return magic switch {
                ServerGreetingPacket.MAGIC => MemoryMarshal.Cast<byte, ServerGreetingPacket>(data)[0],
                ErrorPacket.MAGIC => MemoryMarshal.Cast<byte, ErrorPacket>(data)[0],
                // TODO: We probably don't want to crash the program because of this
                ushort other => throw new ArgumentException("Illegal packet, magic is " + other),
            };
        }
    }

    /**
     * Sent by server to assign player ID.
     */
    public struct ServerGreetingPacket : NetworkPacket
    {
        public const ushort MAGIC = 0xcaf0;

        public ushort magic;
        public int assignedPlayerId;

        public Packet serialize()
        {
            this.magic = MAGIC;
            var bytes = new ServerGreetingPacket[] { this };
            var packet = default(Packet);
            packet.Create(MemoryMarshal.Cast<ServerGreetingPacket, byte>(bytes).ToArray());
            return packet;
        }
    }

    /**
     * Sent by server or client to indicate any kind of error
     */
    public struct ErrorPacket : NetworkPacket
    {
        public const ushort MAGIC = 0xcaf1;

        public byte playerId;
        public byte hostId;
        public ushort magic;
        public ushort errorCode;

        public Packet serialize()
        {
            this.magic = MAGIC;
            var bytes = new ErrorPacket[] { this };
            var packet = default(Packet);
            packet.Create(MemoryMarshal.Cast<ErrorPacket, byte>(bytes).ToArray());
            return packet;
        }

        public string GetErrorMessage() {
            return errorCode switch {
                0 => "OK",
                1 => "Too many players on server already, cannot join!",
                ushort c => "Unknown error code: " + c,
            };
        }
    }

    public struct HelloPacket {
        public const ushort MAGIC = 0xcaf2;

        public byte playerId;
        public byte clientState;
        // TODO: Can MemoryMarshal serialize this ootb or do we need char array?
        public string playerName;

        // TODO: Implement
    }

    public struct StateChangePacket {
        public byte playerId;
        public byte newClientState;
    }

    public struct StartGamePacket {
        // TODO: Implement
    }

    /**
     * Contains an input command. Embeds struct from Command.cs (?)
     *
     * This may be movement commands, building placement, unit creation, etc.
     */
    public struct CommandPacket {
        // TODO: Implement
    }

    /**
     * Tells other players that this player finished sending inputs for their turn. Once every player
     * finished the turn, all players are allowed to advance their simulation by one step.
     */
    public struct EndOfTurnPacket {
        // TODO: Implement
    }

}