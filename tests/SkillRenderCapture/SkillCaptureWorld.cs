using Client.Main.Controls;
using Client.Main.Objects.Player;
using Microsoft.Xna.Framework;

public sealed class SkillCaptureWorld : WalkableWorldControl
{
    public PlayerObject Caster { get; }
    public PlayerObject Victim { get; }
    public static short MapIndex { get; set; } = 1;
    public static Client.Main.Models.PlayerClass CasterClass { get; set; } = Client.Main.Models.PlayerClass.DarkWizard;
    public static Vector2 CasterTile { get; set; } = new(35, 26);
    public static Vector2 TargetTile { get; set; } = new(38, 26);
    public static List<(int Slot, string Name, int Level)> ReferenceWeapons { get; } = new();
    public static bool SelfTarget { get; set; }
    public static bool CaptureChainHops { get; set; }
    public static bool CaptureDarkSideTargets { get; set; }
    public PlayerObject[] ChainTargets { get; }

    public SkillCaptureWorld() : base(MapIndex)
    {
        Caster = new PlayerObject { NetworkId = 1, Location = CasterTile };
        if (!Enum.TryParse<MUnique.OpenMU.Network.Packets.CharacterClassNumber>(CasterClass.ToString(), out var networkClass))
            throw new InvalidOperationException($"Unsupported capture class: {CasterClass}");
        Caster.CharacterClass = networkClass;
        Victim = new PlayerObject { NetworkId = 2, Location = TargetTile };
        Walker = Caster;
        Objects.Add(Caster);
        Objects.Add(Victim);
        ChainTargets = CaptureDarkSideTargets
            ? new[] { Victim, new PlayerObject { NetworkId=3,Location=TargetTile+new Vector2(2,1) },new PlayerObject { NetworkId=4,Location=TargetTile+new Vector2(3,-1) },new PlayerObject { NetworkId=5,Location=TargetTile+new Vector2(-2,1) },new PlayerObject { NetworkId=6,Location=TargetTile+new Vector2(-1,-2) } }
            : CaptureChainHops
            ? new[] { Victim, new PlayerObject { NetworkId = 3, Location = TargetTile + new Vector2(2f, 1f) }, new PlayerObject { NetworkId = 4, Location = TargetTile + new Vector2(3f, -1f) } }
            : new[] { Victim };
        for (int i = 1; i < ChainTargets.Length; i++) Objects.Add(ChainTargets[i]);
    }
}
