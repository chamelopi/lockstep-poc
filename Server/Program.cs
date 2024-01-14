using Simulation;
using System.Numerics;
using Raylib_cs;
using System.CommandLine;

using static Simulation.FixedPointUtil;

namespace Server;

class Server
{
    static readonly ushort Port = 1337;
    static readonly int InitialTurnSpeedMs = 100;
    static readonly int TurnSpeedIncrement = 10;
    static readonly int PlayerCount = 2;
    static readonly int MaxTurns = 100;

    static readonly int GroundSize = 80;

    // Server or local player is always player 1
    static int uiPlayerID = 1;


    private static RayCollision CollideGround(Camera3D cam)
    {
        return Raylib.GetRayCollisionQuad(Raylib.GetMouseRay(Raylib.GetMousePosition(), cam),
            new Vector3(-GroundSize / 2, -1, -GroundSize / 2),
            new Vector3(-GroundSize / 2, -1, GroundSize / 2),
            new Vector3(GroundSize / 2, -1, GroundSize / 2),
            new Vector3(GroundSize / 2, -1, -GroundSize / 2));
    }

    // Entrypoint, using System.CommandLine package for arg parsing
    static async Task<int> Main(string[] args)
    {
        var replayOption = new Option<string?>("--replay", "Replay file to load on startup, if desired");
        var hostOption = new Option<string?>("--host", "Acts as a network client and connects to the specified host");
        hostOption.AddAlias("--connect");
        var serverOption = new Option<bool>("--server", () => false, "Acts as a network server");
        var rootCommand = new RootCommand("Run lockstep simulation");
        rootCommand.AddOption(replayOption);
        rootCommand.AddOption(hostOption);
        rootCommand.AddOption(serverOption);
        rootCommand.SetHandler((replay, host, server) => Run(replay, server, host), replayOption, hostOption, serverOption);

        return await rootCommand.InvokeAsync(args);
    }

    public static void Run(string? replay, bool server, string? host)
    {
        // Init rendering
        Raylib.InitWindow(1280, 1024, "Simulation");
        var camera = new Camera3D(new Vector3(50.0f, 50.0f, 50.0f), Vector3.Zero, Vector3.UnitY, 45, CameraProjection.CAMERA_PERSPECTIVE);

        INetworkManager networkManager;
        var sim = new Simulation.Simulation(InitialTurnSpeedMs, PlayerCount);

        // Argument parsing
        if (replay != null)
        {
            Console.WriteLine("Loading replay from " + replay);
            sim.LoadReplay(replay);
        }
        if (server)
        {
            networkManager = ENetNetworkManager.NewServer(Port);
            Console.WriteLine("Started server!");
        }
        else if (host != null)
        {
            networkManager = ENetNetworkManager.NewClient(host, Port);
            Console.WriteLine("Started client & connected to " + host);
        }
        else
        {
            networkManager = new NoopNetworkManager();
            Console.WriteLine("Started without network!");
        }
        // Listen for our player id and set it accordingly
        networkManager.AddCallback(PacketType.ServerGreeting, (packet) =>
        {
            uiPlayerID = ((ServerGreetingPacket)packet).AssignedPlayerId;
            Console.WriteLine("We are player " + uiPlayerID);
        });

        while (!Raylib.WindowShouldClose())
        {
            sim.RunSimulation();

            HandleInput(sim, camera);
            networkManager.PollEvents();

            Render(sim, camera, networkManager);
        }

        Raylib.CloseWindow();
        networkManager.Dispose();
    }

    static void HandleInput(Simulation.Simulation sim, Camera3D camera)
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
                RecordMoveCommand(sim, camera);
            }
            if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
            {
                RecordSelectCommand(sim, camera);
            }
        }
    }

    private static void RecordSelectCommand(Simulation.Simulation sim, Camera3D camera)
    {
        // Find point on ground
        var coll = CollideGround(camera);
        if (coll.Hit)
        {
            // Check for entities in the close proximity
            bool hit = false;
            for (int i = 0; i < sim.currentState.Entities.Count; i++)
            {
                // Guard against selecting other player's entities.
                // TODO: Replace this with ownership check once we have more than once entity per player
                if ((i + 1) != uiPlayerID)
                {
                    continue;
                }

                var entity = sim.currentState.Entities[i];

                var dist = Distance(entity.X, entity.Y, coll.Point.X, coll.Point.Z);
                if (dist < FixedPointUtil.One * 2)
                {
                    var cmd = new Simulation.Command
                    {
                        // TODO: We will have to add an entity id later!
                        PlayerId = uiPlayerID,
                        CommandType = CommandType.Select,
                        // Queue up commands for two turns in the future!
                        // This allows the netcode time to transmit commands between players
                        TargetTurn = sim.currentTurn + 2,
                    };
                    sim.AddCommand(cmd);
                    hit = true;

                    Console.WriteLine($"New command: selected entity {i}");
                    break;
                }
            }

            // If we hit nothing, deselect
            if (!hit)
            {
                var cmd = new Simulation.Command
                {
                    PlayerId = uiPlayerID,
                    CommandType = CommandType.Deselect,
                    // Queue up commands for two turns in the future!
                    // This allows the netcode time to transmit commands between players
                    TargetTurn = sim.currentTurn + 2,
                };
                sim.AddCommand(cmd);

                Console.WriteLine($"New command: deselected everything");
            }
        }
    }

    private static void RecordMoveCommand(Simulation.Simulation sim, Camera3D camera)
    {
        var coll = CollideGround(camera);
        if (coll.Hit)
        {
            ToFixed(12f);

            var cmd = new Simulation.Command
            {
                PlayerId = uiPlayerID,
                CommandType = CommandType.MoveCommand,
                TargetX = ToFixed(coll.Point.X),
                TargetY = ToFixed(coll.Point.Z),
                // Queue up commands for two turns in the future!
                // This allows the netcode time to transmit commands between players
                TargetTurn = sim.currentTurn + 2,
            };
            sim.AddCommand(cmd);
            Console.WriteLine($"New command: move to {cmd.TargetX}/{cmd.TargetY}");
        }
    }

    static void Render(Simulation.Simulation sim, Camera3D camera, INetworkManager networkManager)
    {
        float delta = sim.GetTimeSinceLastStep();

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.LIGHTGRAY);
        Raylib.BeginMode3D(camera);

        Raylib.DrawPlane(new Vector3(0, -1, 0), new Vector2(GroundSize, GroundSize), Color.GREEN);

        // TODO: This is not smooth, we should maybe store the delta when pausing?
        var interpolatedState = sim.isPaused ? sim.currentState : sim.Interpolate(delta);
        int i = 0;
        foreach (var obj in interpolatedState.Entities)
        {
            var pos = new Vector3(FromFixed(obj.X), 0, FromFixed(obj.Y));
            Raylib.DrawCube(pos, 1, 1, 1, GetColor(i));
            if (interpolatedState.SelectedEntities.Contains(i))
            {
                Raylib.DrawCircle3D(pos, 1.5f, new Vector3(1, 0, 0), 90f, GetColor(i));
            }
            i++;
        }

        Raylib.EndMode3D();

        Raylib.DrawText($"Time since last sim step: ", 10, 10, 24, Color.BLACK);
        Raylib.DrawRectangle(360, 12, Math.Min((int)delta * 3, 500), 20, Color.DARKGREEN);
        Raylib.DrawText($"Current simulation step: {sim.currentTurn}/{MaxTurns}", 10, 40, 24, Color.BLACK);
        Raylib.DrawText($"Ms per simulation step: {sim.turnSpeedMs}", 10, 70, 24, Color.BLACK);

        var clients = networkManager.GetConnectedClients();
        if (clients.Any())
        {
            Raylib.DrawText($"Remote clients: " + clients.Order().Aggregate("", (a, c) => a + c + ","), 10, 100, 24, Color.BLACK);
        }

        Raylib.EndDrawing();
    }

    static Color GetColor(int playerId)
    {
        return playerId switch
        {
            0 => Color.RED,
            1 => Color.BLUE,
            _ => throw new ArgumentException("Color not defined for player " + playerId),
        };
    }
}

