using Simulation;

namespace Server {
    class Server {
        public static void Main(string[] args) {
            Console.WriteLine("hello world!");
            // TODO: Implement ENet Server after this example: https://github.com/nxrighthere/ENet-CSharp#net-environment


            var sim = new Simulation.Simulation(turnSpeedMs: 200, playerCount: 2);
            
            sim.AddCommand(new Command { PlayerId = 0, TargetTurn = 2, TargetX = 10000, TargetY = 10000 });
            sim.AddCommand(new Command { PlayerId = 1, TargetTurn = 10, TargetX = -5000, TargetY = 10000 });
            sim.AddCommand(new Command { PlayerId = 0, TargetTurn = 50, TargetX = 5000, TargetY = 5000 });

            const int MaxTurns = 100;
            for (var i = 0; i < MaxTurns; i++) {
                sim.Step();
                
                Console.WriteLine("Turn " + sim.currentTurn);
                Console.WriteLine(sim.currentState[0].ToString());
                Console.WriteLine(sim.currentState[1].ToString());
                Console.WriteLine();
            }
        }
    }
}