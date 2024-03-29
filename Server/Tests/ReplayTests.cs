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
            EntityId = 5,
        });
        sim.AddCommand(new Command {
            CommandType = CommandType.Move,
            TargetTurn = 20,
            PlayerId = 1,
            EntityId = 5,
            TargetX = 125423056,
            TargetY = 130503525,
        });

        var filename = Path.GetTempFileName();

        sim.SaveReplay(filename);

        var expectedContents = @"{""Commands"":[{""PlayerId"":1,""EntityId"":5,""TargetTurn"":2,""CommandType"":""Select"",""TargetX"":0,""TargetY"":0,""BoxX"":0,""BoxY"":0},{""PlayerId"":1,""EntityId"":5,""TargetTurn"":20,""CommandType"":""Move"",""TargetX"":125423056,""TargetY"":130503525,""BoxX"":0,""BoxY"":0}]}";
        var actualContents = File.ReadAllText(filename);

        Assert.That(actualContents, Is.EqualTo(expectedContents));
    }

    [Test]
    public void TestLoading() {
        var fileContents = @"{""Commands"":[{""PlayerId"":1,""EntityId"":5,""TargetTurn"":2,""CommandType"":""Select"",""TargetX"":0,""TargetY"":0,""BoxX"":0,""BoxY"":0},{""PlayerId"":1,""EntityId"":5,""TargetTurn"":20,""CommandType"":""Move"",""TargetX"":125423056,""TargetY"":130503525,""BoxX"":0,""BoxY"":0}]}";
        var filename = Path.GetTempFileName();

        File.WriteAllText(filename, fileContents);

        var sim = new Simulation(100, 2);
        sim.LoadReplay(filename);

        Assert.That(sim.allCommands.Count, Is.EqualTo(2));
        var firstCommand = sim.allCommands[0];
        Assert.That(firstCommand.CommandType, Is.EqualTo(CommandType.Select));
        Assert.That(firstCommand.TargetTurn, Is.EqualTo(2));
        Assert.That(firstCommand.PlayerId, Is.EqualTo(1));
        Assert.That(firstCommand.EntityId, Is.EqualTo(5));
        var secondCommand = sim.allCommands[1];
        Assert.That(secondCommand.CommandType, Is.EqualTo(CommandType.Move));
        Assert.That(secondCommand.TargetTurn, Is.EqualTo(20));
        Assert.That(secondCommand.PlayerId, Is.EqualTo(1));
        Assert.That(secondCommand.EntityId, Is.EqualTo(5));
        Assert.That(secondCommand.TargetX, Is.EqualTo(125423056));
        Assert.That(secondCommand.TargetY, Is.EqualTo(130503525));
    }
}