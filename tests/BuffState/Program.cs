using Client.Main.Core.Client;
using Microsoft.Extensions.Logging.Abstractions;

static void Check(bool result, string description) { if (!result) throw new Exception(description); }
var manager = new BuffManager(NullLoggerFactory.Instance);
var changes = new List<BuffStateChangedEventArgs>();
manager.BuffStateChanged += (_, change) => changes.Add(change);
var effects = new[] { BuffEffectId.ManaShield, BuffEffectId.CriticalDamage, BuffEffectId.Berserk, BuffEffectId.Reflection, BuffEffectId.SwellLife, BuffEffectId.SwellLifeProficiency, BuffEffectId.CriticalDamageMastery, BuffEffectId.IgnoreDefense, BuffEffectId.IncreaseHealth, BuffEffectId.IncreaseBlock };
Check((byte)BuffEffectId.Berserk == 81 && (byte)BuffEffectId.Reflection == 71, "Source status IDs");
foreach (var effect in effects) {
    manager.ProcessMagicEffectStatus(0x8123, (byte)effect, true);
    Check(manager.HasBuff(0x123, effect), "Success flag must not change the recipient");
    Check(manager.GetBuffState(0x123, effect)?.ExpiresAt == null, "Unknown server duration must not expire early");
    int before = changes.Count;
    manager.ProcessMagicEffectStatus(0x123, (byte)effect, true);
    Check(changes.Count == before, "Refresh must not create duplicate visuals");
}
manager.ProcessMagicEffectStatus(0x123, (byte)BuffEffectId.Reflection, false);
Check(!manager.HasBuff(0x123, BuffEffectId.Reflection) && manager.HasBuff(0x123, BuffEffectId.Berserk), "Independent removal of simultaneous buffs");
manager.ClearPlayerBuffs(0x8123);
Check(!manager.GetActiveBuffs(0x123).Any(), "Scope/death cleanup must remove every class buff");
Check(changes.Count == effects.Length * 2, "Exactly one activation and removal per buff");
Console.WriteLine("PASS: reference IDs, recipient masking, authoritative lifetime, refresh, independent removal and scope cleanup.");
