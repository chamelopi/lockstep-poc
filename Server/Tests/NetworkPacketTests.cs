using System.Reflection;
using System.Text;
using Server;

namespace Tests;

public class Tests
{

    [Test]
    public void TestServerGreetingPacket() {
        var pack = new ServerGreetingPacket {
            AssignedPlayerId = 12,
        };
        var serialized = NetworkPacket.Serialize(pack);
        var deserialized = NetworkPacket.Deserialize<ServerGreetingPacket>(serialized);
        
        Assert.That(deserialized.AssignedPlayerId, Is.EqualTo(12));
    }

    [Test]
    public void TestHelloPacket() {
        var pack = new HelloPacket{
            PlayerId = 1,
            ClientState = ClientState.Waiting,
            PlayerName = "bob",
        };
        var serialized = NetworkPacket.Serialize(pack);
        var data = new byte[serialized.Length];
        serialized.CopyTo(data);

        // Verify that string and enum de-serializes correctly
        var deserialized = NetworkPacket.Deserialize<HelloPacket>(serialized);
        Assert.That(deserialized.PlayerId, Is.EqualTo(1));
        Assert.That(deserialized.ClientState, Is.EqualTo(ClientState.Waiting));
        Assert.That(deserialized.PlayerName, Is.EqualTo("bob"));
    }

    [Test]
    public void TestDetectType() {
        var bytes = Encoding.UTF8.GetBytes(@"{""PkgType"": ""Command"", ""OtherContent"": 1337, ""SomeList"": [1, 2, 3] }");
        using var packet = default(ENet.Packet);
        packet.Create(bytes);

        var type = NetworkPacket.DetectType(packet);

        Assert.That(type, Is.EqualTo(PacketType.Command));
    }

    [Test]
    public void TestSerializationFail() {
        // TODO
    }
}