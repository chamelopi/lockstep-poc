namespace Tests;

using Simulation;

public class ReplayTests {
    [Test]
    public void TestSaving() {
        var sim = new Simulation(100, 2);
        sim.AddCommand(new Command {
            CommandType = CommandType.Select,
            TargetTurn = 2,
            PlayerId = 1,
        });
        sim.AddCommand(new Command {
            CommandType = CommandType.MoveCommand,
            TargetTurn = 20,
            PlayerId = 1,
            TargetX = 125423056,
            TargetY = 130503525,
        });

        var filename = Path.GetTempFileName();

        sim.SaveReplay(filename);

        // TODO: Assert file contents
    }
}