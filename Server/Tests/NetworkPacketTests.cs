using System.Reflection;
using Server;

namespace Tests;

public class Tests
{

    [Test]
    public void VerifyPacketCorrectness()
    {
        var baseClass = typeof(NetworkPacket);
        var subtypes = baseClass.Assembly.GetTypes();

        foreach (var subtype in subtypes)
        {
            if (subtype.GetInterfaces().Contains(baseClass))
            {
                Assert.That(subtype.IsValueType, $"{subtype} should be value types");

                Assert.That(subtype.GetMember("magic"), Has.Length.AtLeast(1), $"{subtype} should have a member `magic`");
                var magicField = subtype.GetMember("magic")[0];
                Assert.NotNull(magicField, $"{subtype}: NetworkPacket subclasses should have a field `magic`");
                Assert.AreEqual(MemberTypes.Field, magicField.MemberType, $"{subtype}: `magic` should be a field");
                Assert.AreEqual(typeof(ushort), ((FieldInfo)magicField).FieldType, $"{subtype}: `magic` should be a ushort");
                Assert.AreEqual(magicField, subtype.GetMembers().Where(m => m.MemberType == MemberTypes.Field).First(), $"{subtype}: The first field should be `magic`");
            }

        }
    }

    // TODO: Add more tests for serializer/deserializer
    [Test]
    public void TestServerGreetingPacket() {
        var pack = new ServerGreetingPacket {
            magic = ServerGreetingPacket.getMagic(),
            assignedPlayerId = 12,
        };
        var serialized = NetworkPacket.Serialize(pack);
        var data = new byte[serialized.Length];
        serialized.CopyTo(data);
        
        // length is 8 with padding
        Assert.That(data, Is.EquivalentTo(new byte[]{ 0xf0, 0xca, 0x00, 0x00, 12, 0x00, 0x00, 0x00 }));

    }
}