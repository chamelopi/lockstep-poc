using System.Data.Common;
using System.Numerics;
using Raylib_cs;
using Simulation;
using static Simulation.FixedPointUtil;

namespace Server;

public class GameScene : Scene
{
    static readonly int GroundSize = 80;
    static readonly int TurnSpeedIncrement = 10;

    private Simulation.Simulation sim;
    private INetworkManager networkManager;
    private Camera3D camera;
    private int myPlayerId;

    public GameScene(Simulation.Simulation sim, INetworkManager networkManager, Camera3D camera)
    {
        this.sim = sim;
        this.networkManager = networkManager;
        myPlayerId = networkManager.GetLocalPlayer();
        this.camera = camera;
        var startTime = DateTime.Now;
        Console.WriteLine($"GAME START at {startTime.ToLongTimeString()}.{startTime.Millisecond}");
    }

    public Scene? Run()
    {
        networkManager.AddCallback(PacketType.Command, HandleRemoteCommand);

        while (!Raylib.WindowShouldClose())
        {
            RunSimulation();

            HandleInput();
            networkManager.PollEvents();

            RenderGameState();
        }

        networkManager.RemoveCallback(PacketType.Command);
        // next scene is null -> exit game
        return null;
    }

    void HandleInput()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_F))
        {
            Raylib.DrawText("Running full determinism check....", 10, 10, 30, Color.BLACK);
            sim.CheckFullDeterminism();
            Console.WriteLine("Simulation re-simulated successfully, we should be deterministic!");
        }

        // Allow control of simulation speed. Simulation speed goes UP when turn duration goes DOWN
        // (a bit counter-intuitive)
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_PAGE_UP))
        {
            sim.turnSpeedMs -= TurnSpeedIncrement;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_PAGE_DOWN))
        {
            sim.turnSpeedMs += TurnSpeedIncrement;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.KEY_S))
        {
            sim.SaveReplay("replay-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".json");
        }
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_P))
        {
            sim.TogglePause();
        }

        if (!sim.isPaused)
        {
            // Right click for move command
            if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_RIGHT))
            {
                RecordMoveCommand();
            }
            if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
            {
                RecordSelectCommand();
            }
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_E))
            {
                RecordSpawnCommand();
            }
        }
    }



    void HandleRemoteCommand(NetworkPacket packet)
    {
        var commandPacket = (CommandPacket)packet;
        Console.WriteLine($"Remote command received! {commandPacket.Command}");

        if (commandPacket.Command.TargetTurn < sim.currentTurn)
        {
            Console.WriteLine($"ERROR: Received command for past turn: {commandPacket.Command.TargetTurn}. Discarding it.");
            return;
        }

        if (commandPacket.PlayerId != commandPacket.PlayerId)
        {
            Console.WriteLine($"ERROR: Received command for player {commandPacket.PlayerId} from player {commandPacket.PlayerId}!");
            return;
        }

        // It is important here that we add the command to the local simulation, only!
        // Otherwise it would be sent to all players again, as if it was our own command!
        sim.AddCommand(commandPacket.Command);
    }

    private void RecordSelectCommand()
    {
        // Find point on ground
        var coll = CollideGround(camera);
        if (coll.Hit)
        {
            // Check for entities in the close proximity
            bool hit = false;
            foreach (var entity in sim.currentState.Entities.Values)
            {
                // Guard against selecting other player's entities.
                if (entity.OwningPlayer != networkManager.GetLocalPlayer())
                {
                    continue;
                }

                var dist = Distance(entity.X, entity.Y, coll.Point.X, coll.Point.Z);
                if (dist < FixedPointUtil.One * 2)
                {
                    var cmd = new Command
                    {
                        EntityId = entity.EntityId,
                        PlayerId = networkManager.GetLocalPlayer(),
                        CommandType = CommandType.Select,
                    };
                    AddCommand(cmd);
                    hit = true;

                    Console.WriteLine($"New command: selected entity {entity.EntityId}");
                    // Select only one entity this way
                    break;
                }
            }

            // If we hit nothing, deselect
            if (!hit)
            {
                var cmd = new Command
                {
                    PlayerId = networkManager.GetLocalPlayer(),
                    CommandType = CommandType.Deselect,
                };
                AddCommand(cmd);

                Console.WriteLine($"New command: deselected everything");
            }
        }
    }

    
    private void RecordSpawnCommand()
    {
         // Find point on ground
        var coll = CollideGround(camera);
        if (coll.Hit)
        {
            var spawnCommand = new Command() {
                CommandType = CommandType.Spawn,
                // This will later have to track the entity TYPE, too!
                PlayerId = myPlayerId,
                TargetX = ToFixed(coll.Point.X),
                TargetY = ToFixed(coll.Point.Z),
            };
            AddCommand(spawnCommand);

            Console.WriteLine($"New command: spawn entity");
        }
    }

    private void RecordMoveCommand()
    {
        var coll = CollideGround(camera);
        if (coll.Hit)
        {
            ToFixed(12f);

            var cmd = new Simulation.Command
            {
                PlayerId = networkManager.GetLocalPlayer(),
                CommandType = CommandType.Move,
                TargetX = ToFixed(coll.Point.X),
                TargetY = ToFixed(coll.Point.Z),
                // Queue up commands for two turns in the future!
                // This allows the netcode time to transmit commands between players
                TargetTurn = sim.currentTurn + 2,
            };
            AddCommand(cmd);
            Console.WriteLine($"New command: move to {cmd.TargetX}/{cmd.TargetY}");
        }
    }

    /**
     * Records a command, both for the local simulation as well as the network manager.
     *
     * Commands are always recorded 2 turns in the future, to compensate for packet travel times.
     */
    private void AddCommand(Command command)
    {
        // Queue up commands for two turns in the future!
        // This allows the netcode time to transmit commands between players
        command.TargetTurn =  sim.currentTurn + 2;
        
        sim.AddCommand(command);
        networkManager.QueuePacket(new CommandPacket() { PkgType = PacketType.Command, PlayerId = myPlayerId, Command = command });
    }

    private static RayCollision CollideGround(Camera3D cam)
    {
        return Raylib.GetRayCollisionQuad(Raylib.GetMouseRay(Raylib.GetMousePosition(), cam),
            new Vector3(-GroundSize / 2, -1, -GroundSize / 2),
            new Vector3(-GroundSize / 2, -1, GroundSize / 2),
            new Vector3(GroundSize / 2, -1, GroundSize / 2),
            new Vector3(GroundSize / 2, -1, -GroundSize / 2));
    }

    private void RunSimulation()
    {
        if (sim.isPaused)
        {
            return;
        }

        // Fixed time step
        var startFrame = Clock.GetTicks();
        var timeSinceLastStep = startFrame - sim.lastTurnTimestamp;

        if (timeSinceLastStep > sim.turnSpeedMs)
        {
            // Signal next turn to other players and advance once we are allowed
            networkManager.SignalNextTurn(sim.currentTurn);
            if (networkManager.CanAdvanceTurn())
            {
                sim.lastTurnTimestamp = Clock.GetTicks();

                sim.Step();

                // We might disable this for a release build
                sim.CheckDeterminism();
            }
        }
    }

    void RenderGameState()
    {
        float delta = sim.GetTimeSinceLastStep();

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.LIGHTGRAY);
        Raylib.BeginMode3D(camera);

        Raylib.DrawPlane(new Vector3(0, -1, 0), new Vector2(GroundSize, GroundSize), Color.GREEN);

        // TODO: This is not smooth, we should maybe store the delta when pausing?
        var interpolatedState = sim.isPaused ? sim.currentState : sim.Interpolate(delta);
        int i = 0;
        foreach (var (entityId, obj) in interpolatedState.Entities)
        {
            var pos = new Vector3(FromFixed(obj.X), 0, FromFixed(obj.Y));
            Raylib.DrawCube(pos, 1, 1, 1, GetColor(obj.OwningPlayer));
            if (interpolatedState.SelectedEntities.Contains(entityId))
            {
                Raylib.DrawCircle3D(pos, 1.5f, new Vector3(1, 0, 0), 90f, GetColor(obj.OwningPlayer));
            }
            i++;
        }

        Raylib.EndMode3D();

        Raylib.DrawText($"Time since last sim step: ", 10, 10, 24, Color.BLACK);
        Raylib.DrawRectangle(360, 12, Math.Min((int)delta * 3, 500), 20, Color.DARKGREEN);
        Raylib.DrawText($"Current simulation step: {sim.currentTurn}", 10, 40, 24, Color.BLACK);
        Raylib.DrawText($"Ms per simulation step: {sim.turnSpeedMs}", 10, 70, 24, Color.BLACK);

        Raylib.DrawText($"Local player: {networkManager.GetLocalPlayer()}", 800, 100, 24, Color.BLACK);
        var clients = networkManager.GetClients();
        foreach (var client in clients)
        {
            var height = 100 + client.PlayerId * 30;
            Raylib.DrawText($"Client {client.PlayerName} ({client.PlayerId}): State: {client.State} Turn done? {client.CurrentTurnDone}", 10, height, 24, Color.BLACK);
        }

        Raylib.EndDrawing();
    }

    static Color GetColor(int playerId)
    {
        return playerId switch
        {
            0 => Color.GRAY,
            1 => Color.RED,
            2 => Color.BLUE,
            _ => throw new ArgumentException("Color not defined for player " + playerId),
        };
    }

    public ClientState GetState()
    {
        return ClientState.InGame;
    }
}