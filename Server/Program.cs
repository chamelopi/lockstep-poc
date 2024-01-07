﻿using System.Dynamic;
using System.Xml.Serialization;
using Simulation;

namespace Server {
    class Server {
        public static long GetTicks() {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public static void Main(string[] args) {
            Console.WriteLine("hello world!");
            // TODO: Implement ENet Server after this example: https://github.com/nxrighthere/ENet-CSharp#net-environment


            var sim = new Simulation.Simulation(turnSpeedMs: 200, playerCount: 2);
            // These could come from the player, from the network, or from a file (replay)
            var commands = new List<Command>(){
                new Command { PlayerId = 0, TargetTurn = 2, TargetX = 10000, TargetY = 10000 },
                new Command { PlayerId = 1, TargetTurn = 10, TargetX = -5000, TargetY = 10000 },
                new Command { PlayerId = 0, TargetTurn = 50, TargetX = 5000, TargetY = 5000 }
            };
            sim.AddCommands(commands);

            // Run the simulation a second time to compare results later
            var determinismCheck = new Simulation.Simulation(turnSpeedMs: 200, playerCount: 2);
            determinismCheck.AddCommands(commands);

            const int MaxTurns = 100;
            long beginOfSim = GetTicks();
            long lastFrame = GetTicks();
            long timeSinceLastStep;
            while (sim.currentTurn < MaxTurns) {
                // Fixed time step
                var startFrame = GetTicks();
                timeSinceLastStep = startFrame - lastFrame;
                while (timeSinceLastStep > sim.turnSpeedMs) {
                    timeSinceLastStep -= sim.turnSpeedMs;
                    lastFrame += sim.turnSpeedMs;
                    sim.Step();
                }

                // Render
                // TODO: Interpolate!
                Console.WriteLine("Turn " + sim.currentTurn + " current timestamp " + lastFrame);
                Console.WriteLine(sim.currentState[0].ToString());
                Console.WriteLine(sim.currentState[1].ToString());
                Console.WriteLine();

                Thread.Sleep(16);
            }

            var endOfSim = GetTicks();
            Console.WriteLine($"Simulating {MaxTurns} turns with speed {sim.turnSpeedMs}ms per turn took {endOfSim - beginOfSim}ms");

            sim.CheckDeterminism();
            Console.WriteLine("If you can read this, we're good!");
        }
    }
}