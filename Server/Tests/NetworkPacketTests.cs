using System.Text;
using Server;
using Simulation;

namespace Tests;

public class NetworkPacketTests
{

    [Test]
    public void TestServerGreetingPacket()
    {
        var pack = new ServerGreetingPacket
        {
            PkgType = PacketType.ServerGreeting,
            AssignedPlayerId = 12,
        };
        var serialized = NetworkPacket.Serialize(pack);
        var deserialized = NetworkPacket.Deserialize<ServerGreetingPacket>(serialized);

        Assert.That(deserialized.AssignedPlayerId, Is.EqualTo(12));
    }

    [Test]
    public void TestHelloPacket()
    {
        var pack = new HelloPacket
        {
            PkgType = PacketType.Hello,
            PlayerId = 1,
            ClientState = ClientState.Waiting,
            PlayerName = "bob",
            CurrentTurnDone = false,
        };
        var serialized = NetworkPacket.Serialize(pack);
        var data = new byte[serialized.Length];
        serialized.CopyTo(data);

        // Verify that string and enum de-serializes correctly
        var deserialized = NetworkPacket.Deserialize<HelloPacket>(serialized);
        Assert.That(deserialized.PlayerId, Is.EqualTo(1));
        Assert.That(deserialized.ClientState, Is.EqualTo(ClientState.Waiting));
        Assert.That(deserialized.PlayerName, Is.EqualTo("bob"));
        Assert.That(deserialized.CurrentTurnDone, Is.EqualTo(false));
    }

    [Test]
    public void TestDetectType()
    {
        var bytes = Encoding.UTF8.GetBytes(@"{""PkgType"": ""Command"", ""OtherContent"": 1337, ""SomeList"": [1, 2, 3] }");
        using var packet = default(ENet.Packet);
        packet.Create(bytes);

        var type = NetworkPacket.DetectType(packet);

        Assert.That(type, Is.EqualTo(PacketType.Command));
    }

    [Test]
    public void TestPacketWithDefaultType()
    {
        var pack = new StateChangePacket()
        {
            NewClientState = ClientState.Disconnected,
            PlayerId = 13,
        };

        // Should not serialize when type is unset
        Assert.Catch(() => NetworkPacket.Serialize(pack));
    }

    [Test]
    [Ignore("FIXME: how do we want to handle deserialization with the wrong type/format?")]
    public void TestDeserializationFail()
    {
        var bytes = Encoding.UTF8.GetBytes(@"{""PkgType"": ""Command"", ""OtherContent"": 1337, ""SomeList"": [1, 2, 3] }");
        using var packet = default(ENet.Packet);
        packet.Create(bytes);

        Assert.Catch(() =>
        {
            var p = NetworkPacket.Deserialize<HelloPacket>(packet);
        });
    }

    [Test]
    public void TestPerf()
    {
        var packet = new HelloPacket()
        {
            PkgType = PacketType.Hello,
            ClientState = ClientState.Ready,
            PlayerId = 1,
            PlayerName = "verylongnamethatmighttakelongertoserialize",
            CurrentTurnDone = false,
        };

        var numPackets = 10000;
        var maxTime = 250;
        long bytes = 0;

        var diff = Clock.TimeIt(() =>
        {
            for (int i = 0; i < numPackets; i++)
            {
                var serialized = NetworkPacket.Serialize(packet);
                bytes += serialized.Length;
                var deserialized = NetworkPacket.Deserialize<HelloPacket>(serialized);
                Assert.That(deserialized.PlayerId, Is.EqualTo(1));
            }
        });

        Debug.Log($"Serializing {numPackets} packets ({bytes} bytes) took {diff}ms!");

        Assert.That(diff, Is.LessThan(maxTime), $"Serialization of {numPackets} took longer than {maxTime}ms!");
    }
}