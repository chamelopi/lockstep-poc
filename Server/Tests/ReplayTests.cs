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
            CommandType = CommandType.Move,
            TargetTurn = 20,
            PlayerId = 1,
            TargetX = 125423056,
            TargetY = 130503525,
        });

        var filename = Path.GetTempFileName();

        sim.SaveReplay(filename);

        var expectedContents = @"[{""PlayerId"":1,""TargetTurn"":2,""CommandType"":""Select"",""TargetX"":0,""TargetY"":0,""BoxX"":0,""BoxY"":0},{""PlayerId"":1,""TargetTurn"":20,""CommandType"":""Move"",""TargetX"":125423056,""TargetY"":130503525,""BoxX"":0,""BoxY"":0}]";
        var actualContents = File.ReadAllText(filename);

        Assert.That(actualContents, Is.EqualTo(expectedContents));
    }
}