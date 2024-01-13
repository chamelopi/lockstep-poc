using System.Runtime.InteropServices;
using ENet;

namespace Server
{
    public interface NetworkPacket
    {
        public abstract static ushort getMagic();

        public static T deserialize<T>(Packet packet) where T : struct, NetworkPacket
        {
            var data = new byte[packet.Length];
            packet.CopyTo(data);
            // TODO: Assumes that the short is stored in little endian
            ushort magic = (ushort)(data[0] | data[1] << 8);

            if (T.getMagic() != magic)
            {
                throw new ArgumentException("Corrupt packet, expected magic " + T.getMagic().ToString("x") + " got " + magic.ToString("x"));
            }

            return MemoryMarshal.Cast<byte, T>(data)[0];
        }

        public static Packet Serialize<T>(T networkPacket) where T : struct, NetworkPacket
        {
            var bytes = new T[] { networkPacket };
            var packet = default(Packet);
            packet.Create(MemoryMarshal.Cast<T, byte>(bytes).ToArray());
            return packet;
        }
    }


    /**
     * Sent by server to assign player ID.
     */
    public struct ServerGreetingPacket : NetworkPacket
    {
        public ushort magic;
        public int assignedPlayerId;

        public static ushort getMagic()
        {
            return 0xcaf0;
        }
    }

    /**
     * Sent by server or client to indicate any kind of error
     */
    public struct ErrorPacket : NetworkPacket
    {
        public ushort magic;
        public byte playerId;
        public byte hostId;
        public ushort errorCode;

        public static ushort getMagic()
        {
            return 0xcaf1;
        }

        public string GetErrorMessage()
        {
            return errorCode switch
            {
                0 => "OK",
                1 => "Too many players on server already, cannot join!",
                ushort c => "Unknown error code: " + c,
            };
        }
    }

    public struct HelloPacket : NetworkPacket
    {
        public static ushort getMagic()
        {
            return 0xcaf2;
        }
        public ushort magic;
        public byte playerId;
        public byte clientState;
        // TODO: Can MemoryMarshal serialize this ootb or do we need char array?
        public string playerName;



        // TODO: Implement
    }

    public struct StateChangePacket : NetworkPacket
    {
        public ushort magic;
        public byte playerId;
        public byte newClientState;

        public static ushort getMagic()
        {
            return 0xcaf3;
        }
    }

    public struct StartGamePacket 
    {
        // TODO: Implement

    }

    /**
     * Contains an input command. Embeds struct from Command.cs (?)
     *
     * This may be movement commands, building placement, unit creation, etc.
     */
    public struct CommandPacket
    {
        // TODO: Implement
    }

    /**
     * Tells other players that this player finished sending inputs for their turn. Once every player
     * finished the turn, all players are allowed to advance their simulation by one step.
     */
    public struct EndOfTurnPacket
    {
        // TODO: Implement
    }

}