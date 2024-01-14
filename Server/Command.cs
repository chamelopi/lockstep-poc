using System.Text.Json;
using Server;

namespace Simulation;

public enum CommandType
{
    Select,
    Deselect,
    BoxSelect,
    MoveCommand,
}

public struct Command
{
    public int PlayerId;
    public int TargetTurn;
    public CommandType CommandType;

    public long TargetX;
    public long TargetY;
    public long BoxX;
    public long BoxY;


    public override string ToString()
    {
        return $"Command: id={PlayerId}, turn={TargetTurn}, type={CommandType}, tx={TargetX}, ty={TargetY}, boxX={BoxX}, boxY={BoxY}";
    }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this, NetworkPacket.options);
    }

    public static Command Deserialize(string input)
    {
        return JsonSerializer.Deserialize<Command>(input, NetworkPacket.options);
    }
}
