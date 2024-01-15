using System.Numerics;
using Raylib_cs;
using Simulation;
using static Simulation.FixedPointUtil;

namespace Server;

public class Game
{
    static readonly int GroundSize = 80;
    static readonly int TurnSpeedIncrement = 10;


    private Simulation.Simulation sim;
    private INetworkManager networkManager;
    private Camera3D camera;

    public Game(Simulation.Simulation sim, INetworkManager networkManager, Camera3D camera)
    {
        this.sim = sim;
        this.networkManager = networkManager;
        this.camera = camera;
    }

    public void Run()
    {
        while (!Raylib.WindowShouldClose())
        {
            sim.RunSimulation();

            HandleInput();
            networkManager.PollEvents();

            RenderGameState();
        }
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
        }
    }

    private void RecordSelectCommand()
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
                if ((i + 1) != networkManager.GetLocalPlayer())
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
                        PlayerId = networkManager.GetLocalPlayer(),
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
                    PlayerId = networkManager.GetLocalPlayer(),
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

    private static RayCollision CollideGround(Camera3D cam)
    {
        return Raylib.GetRayCollisionQuad(Raylib.GetMouseRay(Raylib.GetMousePosition(), cam),
            new Vector3(-GroundSize / 2, -1, -GroundSize / 2),
            new Vector3(-GroundSize / 2, -1, GroundSize / 2),
            new Vector3(GroundSize / 2, -1, GroundSize / 2),
            new Vector3(GroundSize / 2, -1, -GroundSize / 2));
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
            sim.AddCommand(cmd);
            Console.WriteLine($"New command: move to {cmd.TargetX}/{cmd.TargetY}");
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
        Raylib.DrawText($"Current simulation step: {sim.currentTurn}", 10, 40, 24, Color.BLACK);
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