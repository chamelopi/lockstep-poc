﻿using System.Dynamic;
using System.Xml.Serialization;
using Simulation;
using System.Numerics;
using Raylib_cs;

namespace Server
{
    class Server
    {
        static readonly float FixedPointRes = 10000f;
        static readonly int TurnSpeedMs = 100;
        static readonly int PlayerCount = 2;


        static long lastFrame = 0;

        public static long GetTicks()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public static void Main(string[] args)
        {
            // TODO: Implement ENet Server after this example: https://github.com/nxrighthere/ENet-CSharp#net-environment

            // Init rendering
            Raylib.InitWindow(1280, 1024, "Simulation");
            var camera = new Camera3D(new Vector3(50.0f, 50.0f, 50.0f), Vector3.Zero, Vector3.UnitY, 45, CameraProjection.CAMERA_PERSPECTIVE);

            var sim = new Simulation.Simulation(TurnSpeedMs, PlayerCount);
            // These could come from the player, from the network, or from a file (replay)
            var commands = new List<Command>(){
                new() { PlayerId = 0, TargetTurn = 2, TargetX = 100000, TargetY = 100000 },
                new() { PlayerId = 1, TargetTurn = 10, TargetX = -50000, TargetY = 50000 },
                new() { PlayerId = 1, TargetTurn = 30, TargetX = -50000, TargetY = -50000 },
                new() { PlayerId = 1, TargetTurn = 50, TargetX = 50000, TargetY = -50000 },
                new() { PlayerId = 1, TargetTurn = 70, TargetX = 50000, TargetY = 50000 },
                new() { PlayerId = 0, TargetTurn = 50, TargetX = 50000, TargetY = 50000 }
            };
            sim.AddCommands(commands);

            const int MaxTurns = 100;
            lastFrame = GetTicks();

            while (!Raylib.WindowShouldClose())
            {
                if (sim.currentTurn < MaxTurns)
                {
                    RunSimulation(sim);
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

                Render(sim, camera);
            }

            Raylib.CloseWindow();
        }

        static float RunSimulation(Simulation.Simulation sim)
        {
            // TODO: Build a clock abstraction for this?
            long timeSinceLastStep = 0;

            // Fixed time step
            var startFrame = GetTicks();
            timeSinceLastStep += startFrame - lastFrame;

            while (timeSinceLastStep > sim.turnSpeedMs)
            {
                timeSinceLastStep -= sim.turnSpeedMs;
                lastFrame += sim.turnSpeedMs;
                sim.Step();
            }

            // We might disable this for a release build
            // FIXME: Does not check correctly
            //sim.CheckDeterminism();

            // How far into the new frame are we?
            var dt = GetTicks() - lastFrame;
            return dt;
        }

        static void Render(Simulation.Simulation sim, Camera3D camera)
        {
            float delta = GetTicks() - lastFrame;

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.WHITE);
            Raylib.BeginMode3D(camera);

            var interpolatedState = sim.Interpolate(delta);
            int i = 0;
            foreach (var obj in interpolatedState)
            {
                var pos = new Vector3(((float)obj.X) / FixedPointRes, 0, ((float)obj.Y) / FixedPointRes);
                Raylib.DrawCube(pos, 1, 1, 1, GetColor(i));
                i++;
            }

            Raylib.EndMode3D();

            Raylib.DrawText($"Time since last step: ", 10, 10, 24, Color.BLACK);
            Raylib.DrawRectangle(360, 8, Math.Min((int)delta * 3, 500), 20, Color.DARKGREEN);
            Raylib.DrawText($"Current simulation step: {sim.currentTurn}", 10, 40, 24, Color.BLACK);
            Raylib.DrawText($"Ms per simulation step: {sim.turnSpeedMs}", 10, 70, 24, Color.BLACK);

            Raylib.EndDrawing();
        }

        static Color GetColor(int playerId)
        {
            switch (playerId)
            {
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
