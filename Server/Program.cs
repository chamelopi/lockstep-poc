using System.Dynamic;
using System.Xml.Serialization;
using Simulation;
using System.Numerics;
using Raylib_cs;
using UnityEngine.Diagnostics;

namespace Server {
    class Server {
        static readonly float FixedPointRes = 1000f;
        static readonly int TurnSpeedMs = 200;
        static readonly int PlayerCount = 2;

        public static long GetTicks() {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public static void Main(string[] args) {
            // TODO: Implement ENet Server after this example: https://github.com/nxrighthere/ENet-CSharp#net-environment

            // Init rendering
            Raylib.InitWindow(1280, 1024, "Simulation");
            var camera = new Camera3D(new Vector3(50.0f, 50.0f, 50.0f), Vector3.Zero, Vector3.UnitY, 45, CameraProjection.CAMERA_PERSPECTIVE);

            var sim = new Simulation.Simulation(TurnSpeedMs, PlayerCount);
            // These could come from the player, from the network, or from a file (replay)
            var commands = new List<Command>(){
                new Command { PlayerId = 0, TargetTurn = 2, TargetX = 10000, TargetY = 10000 },
                new Command { PlayerId = 1, TargetTurn = 10, TargetX = -5000, TargetY = 10000 },
                new Command { PlayerId = 0, TargetTurn = 50, TargetX = 5000, TargetY = 5000 }
            };
            sim.AddCommands(commands);

            while(!Raylib.WindowShouldClose()) {
                RunSimulation(sim, camera);
                Render(sim, camera, 0);
            }

            Raylib.CloseWindow();
        }

        static void RunSimulation(Simulation.Simulation sim, Camera3D camera) {
            // TODO: Build a clock abstraction for this
            const int MaxTurns = 100;
            long beginOfSim = GetTicks();
            long lastFrame = GetTicks();
            long timeSinceLastStep;
            // TODO: Refactor this so that we have an actual game loop!
            while (sim.currentTurn < MaxTurns && !Raylib.WindowShouldClose()) {
                // Fixed time step
                var startFrame = GetTicks();
                timeSinceLastStep = startFrame - lastFrame;
                while (timeSinceLastStep > sim.turnSpeedMs) {
                    timeSinceLastStep -= sim.turnSpeedMs;
                    lastFrame += sim.turnSpeedMs;
                    sim.Step();
                }

                var dt = Math.Abs(lastFrame - timeSinceLastStep);

                Render(sim, camera, dt);

                Thread.Sleep(16);
            }

            var endOfSim = GetTicks();
            Console.WriteLine($"Simulating {MaxTurns} turns with speed {sim.turnSpeedMs}ms per turn took {endOfSim - beginOfSim}ms");

            sim.CheckDeterminism();
            Console.WriteLine("If you can read this, we're good!");
        }

        static void Render(Simulation.Simulation sim, Camera3D camera, float dt) {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.WHITE);
            Raylib.BeginMode3D(camera);

            // FIXME: Interpolation is broken
            //var interpolatedState = sim.Interpolate(dt);
            var interpolatedState = sim.currentState;
            int i = 0;
            foreach(var obj in interpolatedState) {
                var pos = new Vector3(((float)obj.X) / FixedPointRes, 0, ((float)obj.Y) / FixedPointRes);
                Raylib.DrawCube(pos, 1, 1, 1, GetColor(i));
                i++;
            }

            Raylib.EndMode3D();
            Raylib.EndDrawing();
        }

        static Color GetColor(int playerId) {
            switch(playerId) {
                case 0:
                    return Color.RED;
                case 1:
                    return Color.BLUE;
                default:
                    throw new ArgumentException("Color not defined for player " + playerId);
            }
        }
    }
}
