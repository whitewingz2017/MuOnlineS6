from pathlib import Path
p=Path('tests/ItemAttributes/Program.cs');s=p.read_text();a=s.index('if (args.Length > 0)')
s=s[:a]+'''var state = (Client.Main.Core.Client.CharacterState)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Client.Main.Core.Client.CharacterState));
state.Class = CharacterClassNumber.BladeMaster; state.Level = 400;
state.Strength = 169; state.AddedStrength = 1; state.Agility = 140; state.Vitality = 80; state.Energy = 120; state.Leadership = 30;
Check(MuMainItemRules.MeetsRequirements(normal, state), "Equip check ignores added stats or promoted classes.");
state.AddedStrength = 0;
Check(!MuMainItemRules.MeetsRequirements(normal, state), "Equip check allows insufficient Strength."); state.AddedStrength = 1;
state.Vitality = 79;
Check(!MuMainItemRules.MeetsRequirements(normal, state), "Equip check allows insufficient Vitality."); state.Vitality = 80;
state.Energy = 119;
Check(!MuMainItemRules.MeetsRequirements(normal, state), "Equip check allows insufficient Energy."); state.Energy = 120;
state.Leadership = 29;
Check(!MuMainItemRules.MeetsRequirements(normal, state), "Equip check allows insufficient Command."); state.Leadership = 30;
state.Agility = 139;
Check(!MuMainItemRules.MeetsRequirements(normal, state), "Equip check allows insufficient Agility."); state.Agility = 140;
normal.Definition.RequiredLevel = 401;
Check(!MuMainItemRules.MeetsRequirements(normal, state), "Equip check allows insufficient Level."); normal.Definition.RequiredLevel = 0;
state.Class = CharacterClassNumber.HighElf;
Check(!MuMainItemRules.MeetsRequirements(normal, state), "Equip check allows the wrong character class.");
''' +s[a:];p.write_text(s)
