namespace Tests;

using Simulation;

public class SimulationTests
{

    private static long RandomPos()
    {
        return Random.Shared.NextInt64(-100000, 100000);
    }
    private static int RandomPlayer(int maxPlayers)
    {
        return (int)Random.Shared.NextInt64(1, maxPlayers + 1);
    }

    /**
     * Ensures that we can maintain our frame times (not respecting network or rendering)
     * 
     * Simultaneously also runs CheckDeterminism() on our simulation
     */
    [Test]
    public void BenchmarkEntityUpdates()
    {
        var numEntities = 40000;
        // Maximum update time to get stable 60 FPS, not respecting rendering 
        var maxTimeMs = 16;
        var steps = 100;
        var sim = new Simulation(0, 2);

        for (int i = 0; i < numEntities; i++)
        {
            sim.currentState.SpawnEntity(new Entity()
            {
                TargetX = RandomPos(),
                TargetY = RandomPos(),
                X = RandomPos(),
                Y = RandomPos(),
                Moving = true,
                VelocityX = RandomPos() / 50000,
                VelocityY = RandomPos() / 50000,
            }, RandomPlayer(2));
        }
        sim.lastState = new(sim.currentState);
        sim.twoStepsAgoState = new(sim.currentState);

        for (int i = 0; i < steps; i++)
        {
            var duration = Clock.TimeIt(() =>
            {
                sim.Step();
            });
            Console.WriteLine($"Simulation stepping {numEntities} took {duration}ms");
            if (duration > maxTimeMs)
            {
                Assert.Fail($"Simulation stepping {numEntities} took longer than {maxTimeMs}ms!");
            }
        }
        sim.CheckFullDeterminism();
    }

}