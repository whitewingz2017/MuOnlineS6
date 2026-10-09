using System.Buffers.Binary;
using System.Reflection;
using System.Runtime.CompilerServices;
using Client.Main.Core.Client;
using Client.Main.Networking;
using Client.Main.Networking.PacketHandling.Handlers;
using Microsoft.Extensions.Logging.Abstractions;

try
{
    int assertions = 0;
    foreach (var (version, length) in new[] {
        (TargetProtocolVersion.Season6, 28), (TargetProtocolVersion.Season6, 36),
        (TargetProtocolVersion.Version097, 22), (TargetProtocolVersion.Version075, 20) })
    {
        var state = new CharacterState(NullLoggerFactory.Instance) { MaximumShield = 999 };
        // Only scene navigation is omitted; exercise the real handler and state updates.
        var network = (NetworkManager)RuntimeHelpers.GetUninitializedObject(typeof(NetworkManager));
        typeof(NetworkManager).GetField("_logger", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(network, NullLogger<NetworkManager>.Instance);
        typeof(NetworkManager).GetField("_characterState", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(network, state);
        var handler = new CharacterDataHandler(NullLoggerFactory.Instance, state, network, version, null!);
        byte[] packet = new byte[length];
        packet[0] = 0xC3; packet[1] = (byte)length; packet[2] = 0xF3; packet[3] = 0x04;
        packet[4] = 131; packet[5] = 116; packet[6] = 2; packet[7] = 3;
        ulong exp = version == TargetProtocolVersion.Season6 ? 0x1234567803A38F4EUL : 123456;
        uint hp = length == 36 ? 90000u : 1000u;
        if (length == 36)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(8), hp);
            BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(12), 80000);
            BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(16), 70000);
            BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(20), 60000);
            BinaryPrimitives.WriteUInt64LittleEndian(packet.AsSpan(24), exp);
        }
        else
        {
            BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(8), (ushort)hp);
            BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(10), 800);
            if (length == 28)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(12), 700);
                BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(14), 600);
                BinaryPrimitives.WriteUInt64BigEndian(packet.AsSpan(16), exp);
            }
            else
            {
                if (length == 22) BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(12), 600);
                BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(length - 8), (uint)exp);
            }
        }
        BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(length - 4), 10092544);
        await handler.HandleRespawnAfterDeathAsync(packet);
        if (state.InventoryZen != 10092544 || state.Experience != exp || state.CurrentHealth != hp ||
            state.MapId != 2 || state.PositionX != 131 || state.PositionY != 116 ||
            state.CurrentShield != (length == 36 ? 70000u : length == 28 ? 700u : 999u) ||
            state.CurrentMana != (length == 36 ? 80000u : 800u) ||
            state.CurrentAbility != (length == 36 ? 60000u : length == 20 ? 0u : 600u))
            throw new Exception($"Incorrect revival state for {version}, {length} bytes.");
        assertions += 9;
        if (version == TargetProtocolVersion.Season6)
        {
            await handler.HandleRespawnAfterDeathAsync(packet.AsMemory(0, 22));
            if (state.InventoryZen != 10092544 || state.Experience != exp)
                throw new Exception("Truncated Season 6 packet corrupted character state.");
            assertions += 2;
        }
    }
    Console.WriteLine($"PASS: {assertions} revival assertions across Season 6 standard/extended and legacy layouts.");
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception);
    Environment.Exit(1);
}
