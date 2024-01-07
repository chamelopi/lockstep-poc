using System.Dynamic;
using System.Xml.Serialization;
using Simulation;
using System.Numerics;
using Raylib_cs;

namespace Server
{
    class Server
    {
        static readonly float FixedPointRes = 10000f;
        static readonly int InitialTurnSpeedMs = 100;
        static readonly int TurnSpeedIncrement = 10;
        static readonly int PlayerCount = 2;
        static readonly int MaxTurns = 100;

        private static List<Command> GetPresetCommands() {
            return new List<Command>(){
                new() { PlayerId = 0, TargetTurn = 2, TargetX = 100000, TargetY = 100000 },
                new() { PlayerId = 1, TargetTurn = 10, TargetX = -50000, TargetY = 50000 },
                new() { PlayerId = 1, TargetTurn = 30, TargetX = -50000, TargetY = -50000 },
                new() { PlayerId = 1, TargetTurn = 50, TargetX = 50000, TargetY = -50000 },
                new() { PlayerId = 1, TargetTurn = 70, TargetX = 50000, TargetY = 50000 },
                new() { PlayerId = 0, TargetTurn = 50, TargetX = 50000, TargetY = 50000 }
            };
        }

        public static void Main(string[] args)
        {
            // TODO: Implement ENet Server after this example: https://github.com/nxrighthere/ENet-CSharp#net-environment

            // Init rendering
            Raylib.InitWindow(1280, 1024, "Simulation");
            var camera = new Camera3D(new Vector3(50.0f, 50.0f, 50.0f), Vector3.Zero, Vector3.UnitY, 45, CameraProjection.CAMERA_PERSPECTIVE);

            var sim = new Simulation.Simulation(InitialTurnSpeedMs, PlayerCount);

            if (args.Length == 0) {
                // These could come from the player, from the network, or from a file (replay)
                var commands = GetPresetCommands();
                sim.AddCommands(commands);
            } else if (args.Length == 1) {
                Console.WriteLine("Loading replay from " + args[0]);
                sim.LoadReplay(args[0]);
            }

            while (!Raylib.WindowShouldClose())
            {
                if (sim.currentTurn < MaxTurns)
                {
                    sim.RunSimulation();
                }
                else
                {
                    if (Raylib.IsKeyPressed(KeyboardKey.KEY_F))
                    {
                        Raylib.DrawText("Running full determinism check....", 10, 10, 30, Color.BLACK);
                        sim.CheckFullDeterminism();
                        Console.WriteLine("Simulation re-simulated successfully, we should be deterministic!");
                    }
                }

                // Allow control of simulation speed. Simulation speed goes UP when turn duration goes DOWN
                // (a bit counter-intuitive)
                if (Raylib.IsKeyPressed(KeyboardKey.KEY_PAGE_UP)) {
                    sim.turnSpeedMs -= TurnSpeedIncrement;
                }
                if (Raylib.IsKeyPressed(KeyboardKey.KEY_PAGE_DOWN)) {
                    sim.turnSpeedMs += TurnSpeedIncrement;
                }

                if (Raylib.IsKeyPressed(KeyboardKey.KEY_S)) {
                    sim.SaveReplay("replay-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".csv");
                }

                Render(sim, camera);
            }

            Raylib.CloseWindow();
        }
        static void Render(Simulation.Simulation sim, Camera3D camera)
        {
            float delta = sim.GetTimeSinceLastStep();

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.LIGHTGRAY);
            Raylib.BeginMode3D(camera);

            Raylib.DrawPlane(new Vector3(0, -1, 0), new Vector2(80, 80), Color.GREEN);

            var interpolatedState = sim.Interpolate(delta);
            int i = 0;
            foreach (var obj in interpolatedState)
            {
                var pos = new Vector3(((float)obj.X) / FixedPointRes, 0, ((float)obj.Y) / FixedPointRes);
                Raylib.DrawCube(pos, 1, 1, 1, GetColor(i));
                i++;
            }

            Raylib.EndMode3D();

            Raylib.DrawText($"Time since last sim step: ", 10, 10, 24, Color.BLACK);
            Raylib.DrawRectangle(360, 12, Math.Min((int)delta * 3, 500), 20, Color.DARKGREEN);
            Raylib.DrawText($"Current simulation step: {sim.currentTurn}/{MaxTurns}", 10, 40, 24, Color.BLACK);
            Raylib.DrawText($"Ms per simulation step: {sim.turnSpeedMs}", 10, 70, 24, Color.BLACK);

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
}
