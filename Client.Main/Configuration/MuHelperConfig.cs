using System;
using System.Collections.Generic;
using System.Buffers.Binary;
using System.Text;

namespace Client.Main.Configuration
{
    /// <summary>
    /// Client-side representation of MuMain's MU Helper ConfigData and its
    /// 257-byte OpenMU wire representation.
    /// </summary>
    public sealed class MuHelperConfig
    {
        public int HuntingRange { get; set; } = 5;
        public bool LongRangeCounterAttack { get; set; }
        public bool ReturnToOriginalPosition { get; set; }
        public int MaxSecondsAway { get; set; } = 20;

        public ushort BasicSkillId { get; set; }
        public MuHelperSkillActivation ActivationSkill1 { get; set; } = new();
        public MuHelperSkillActivation ActivationSkill2 { get; set; } = new();
        public bool UseCombo { get; set; }
        public ushort[] ComboSkillIds { get; set; } = new ushort[3];
        public ushort[] BuffSkillIds { get; set; } = new ushort[3];
        public bool BuffDuration { get; set; }
        public bool BuffDurationParty { get; set; }
        public int BuffCastIntervalSeconds { get; set; }

        public bool AutoHeal { get; set; }
        public int HealThreshold { get; set; } = 50;
        public bool SupportParty { get; set; }
        public bool AutoHealParty { get; set; }
        public int HealPartyThreshold { get; set; } = 50;
        public bool UseHealPotion { get; set; }
        public int PotionThreshold { get; set; } = 50;
        /// <summary>Q/W/E healing-potion assignment to use; -1 selects the first assigned healing potion.</summary>
        public int PotionHotbarSlot { get; set; } = -1;
        public bool UseDrainLife { get; set; }
        public bool UseDarkRaven { get; set; }
        public int DarkRavenMode { get; set; }
        public bool RepairItem { get; set; }

        public int ObtainingRange { get; set; } = 5;
        public bool PickAllItems { get; set; }
        public bool PickSelectedItems { get; set; } = true;
        public bool PickJewel { get; set; } = true;
        public bool PickZen { get; set; } = true;
        public bool PickAncient { get; set; } = true;
        public bool PickExcellent { get; set; } = true;
        public bool PickExtraItems { get; set; }
        public List<string> ExtraItems { get; set; } = new();

        // Local-only Helper preferences. The reference client stores these outside
        // the server packet so existing Helper protocol layouts remain untouched.
        public bool UseSelfDefense { get; set; }
        public bool AutoAcceptFriend { get; set; }
        public bool AutoAcceptGuild { get; set; }
        public bool FallbackBasicAttack { get; set; } = true;
        public List<ushort> BlockedMapIds { get; set; } = new();

        private const int HelperDataLength = 257;
        private const int ExtraItemsOffset = 65;
        private const int ExtraItemSlotLength = 15;
        private const int ExtraItemSlotCount = 12;

        public byte[] ToHelperDataBytes()
        {
            MuHelperConfig config = Clone();
            config.Normalize();
            byte[] data = new byte[HelperDataLength];

            data[1] = (byte)((config.PickJewel ? 1 << 3 : 0) |
                             (config.PickAncient ? 1 << 4 : 0) |
                             (config.PickExcellent ? 1 << 5 : 0) |
                             (config.PickZen ? 1 << 6 : 0) |
                             (config.PickExtraItems ? 1 << 7 : 0));
            data[2] = (byte)((config.HuntingRange & 0x0F) | ((config.ObtainingRange & 0x0F) << 4));

            WriteHelperWord(data, 3, config.MaxSecondsAway);
            WriteHelperWord(data, 5, config.BasicSkillId);
            WriteHelperWord(data, 7, config.ActivationSkill1.SkillId);
            WriteHelperWord(data, 9, config.ActivationSkill1.DelaySeconds);
            WriteHelperWord(data, 11, config.ActivationSkill2.SkillId);
            WriteHelperWord(data, 13, config.ActivationSkill2.DelaySeconds);
            WriteHelperWord(data, 15, config.BuffCastIntervalSeconds);
            WriteHelperWord(data, 17, config.BuffSkillIds[0]);
            WriteHelperWord(data, 19, config.BuffSkillIds[1]);
            WriteHelperWord(data, 21, config.BuffSkillIds[2]);

            data[23] = (byte)(((config.PotionThreshold / 10) & 0x0F) |
                              (((config.HealThreshold / 10) & 0x0F) << 4));
            data[24] = (byte)(((config.HealPartyThreshold / 10) & 0x0F) |
                              (((config.HealThreshold / 10) & 0x0F) << 4));
            data[25] = (byte)((config.UseHealPotion ? 1 << 0 : 0) |
                              (config.AutoHeal ? 1 << 1 : 0) |
                              (config.UseDrainLife ? 1 << 2 : 0) |
                              (config.LongRangeCounterAttack ? 1 << 3 : 0) |
                              (config.ReturnToOriginalPosition ? 1 << 4 : 0) |
                              (config.UseCombo ? 1 << 5 : 0) |
                              (config.SupportParty ? 1 << 6 : 0) |
                              (config.AutoHealParty ? 1 << 7 : 0));

            int skill1SubCondition = Math.Clamp(config.ActivationSkill1.MinimumNearbyMonsters, 2, 5) - 2;
            data[26] = (byte)((config.BuffDurationParty ? 1 << 0 : 0) |
                              (config.UseDarkRaven ? 1 << 1 : 0) |
                              (config.BuffDuration ? 1 << 2 : 0) |
                              (config.ActivationSkill1.UseTimer ? 1 << 3 : 0) |
                              (config.ActivationSkill1.UseCondition ? 1 << 4 : 0) |
                              (config.ActivationSkill1.MobsAttacking ? 1 << 5 : 0) |
                              (skill1SubCondition << 6));

            int skill2SubCondition = Math.Clamp(config.ActivationSkill2.MinimumNearbyMonsters, 2, 5) - 2;
            data[27] = (byte)((config.ActivationSkill2.UseTimer ? 1 << 0 : 0) |
                              (config.ActivationSkill2.UseCondition ? 1 << 1 : 0) |
                              (config.ActivationSkill2.MobsAttacking ? 1 << 2 : 0) |
                              (skill2SubCondition << 3) |
                              (config.RepairItem ? 1 << 5 : 0) |
                              (config.PickAllItems ? 1 << 6 : 0) |
                              (config.PickSelectedItems ? 1 << 7 : 0));
            data[28] = (byte)Math.Clamp(config.DarkRavenMode, 0, 2);
            data[29] = (byte)((config.UseSelfDefense ? 1 << 0 : 0) |
                              (config.AutoAcceptFriend ? 1 << 1 : 0) |
                              (config.AutoAcceptGuild ? 1 << 2 : 0) |
                              (config.FallbackBasicAttack ? 1 << 3 : 0));

            for (int i = 0; i < Math.Min(config.ExtraItems.Count, ExtraItemSlotCount); i++)
            {
                string itemName = config.ExtraItems[i]?.Trim() ?? string.Empty;
                if (itemName.Length == 0 || itemName.Length >= ExtraItemSlotLength ||
                    itemName.Any(character => character > 0x7F))
                {
                    continue;
                }

                Encoding.ASCII.GetBytes(itemName, data.AsSpan(ExtraItemsOffset + i * ExtraItemSlotLength, ExtraItemSlotLength));
            }

            return data;
        }

        public static MuHelperConfig FromHelperDataBytes(byte[] data)
        {
            if (data == null || data.Length < HelperDataLength)
                throw new ArgumentException($"MU Helper data must contain at least {HelperDataLength} bytes.", nameof(data));

            byte[] blob = data.AsSpan(0, HelperDataLength).ToArray();
            byte behavior = blob[25];
            byte skill1 = blob[26];
            byte skill2 = blob[27];
            byte helper = blob[29];
            var config = new MuHelperConfig
            {
                HuntingRange = blob[2] & 0x0F,
                ObtainingRange = (blob[2] >> 4) & 0x0F,
                MaxSecondsAway = ReadHelperWord(blob, 3),
                BasicSkillId = (ushort)ReadHelperWord(blob, 5),
                LongRangeCounterAttack = (behavior & (1 << 3)) != 0,
                ReturnToOriginalPosition = (behavior & (1 << 4)) != 0,
                UseCombo = (behavior & (1 << 5)) != 0,
                ActivationSkill1 = new MuHelperSkillActivation
                {
                    SkillId = (ushort)ReadHelperWord(blob, 7),
                    DelaySeconds = ReadHelperWord(blob, 9),
                    UseTimer = (skill1 & (1 << 3)) != 0,
                    UseCondition = (skill1 & (1 << 4)) != 0,
                    MobsAttacking = (skill1 & (1 << 5)) != 0,
                    MinimumNearbyMonsters = ((skill1 >> 6) & 0x03) + 2
                },
                ActivationSkill2 = new MuHelperSkillActivation
                {
                    SkillId = (ushort)ReadHelperWord(blob, 11),
                    DelaySeconds = ReadHelperWord(blob, 13),
                    UseTimer = (skill2 & (1 << 0)) != 0,
                    UseCondition = (skill2 & (1 << 1)) != 0,
                    MobsAttacking = (skill2 & (1 << 2)) != 0,
                    MinimumNearbyMonsters = ((skill2 >> 3) & 0x03) + 2
                },
                BuffCastIntervalSeconds = ReadHelperWord(blob, 15),
                BuffSkillIds = new ushort[]
                {
                    (ushort)ReadHelperWord(blob, 17),
                    (ushort)ReadHelperWord(blob, 19),
                    (ushort)ReadHelperWord(blob, 21)
                },
                PotionThreshold = (blob[23] & 0x0F) * 10,
                HealThreshold = ((blob[23] >> 4) & 0x0F) * 10,
                HealPartyThreshold = (blob[24] & 0x0F) * 10,
                UseHealPotion = (behavior & (1 << 0)) != 0,
                AutoHeal = (behavior & (1 << 1)) != 0,
                UseDrainLife = (behavior & (1 << 2)) != 0,
                SupportParty = (behavior & (1 << 6)) != 0,
                AutoHealParty = (behavior & (1 << 7)) != 0,
                BuffDurationParty = (skill1 & (1 << 0)) != 0,
                UseDarkRaven = (skill1 & (1 << 1)) != 0,
                BuffDuration = (skill1 & (1 << 2)) != 0,
                RepairItem = (skill2 & (1 << 5)) != 0,
                PickAllItems = (skill2 & (1 << 6)) != 0,
                PickSelectedItems = (skill2 & (1 << 7)) != 0,
                PickJewel = (blob[1] & (1 << 3)) != 0,
                PickAncient = (blob[1] & (1 << 4)) != 0,
                PickExcellent = (blob[1] & (1 << 5)) != 0,
                PickZen = (blob[1] & (1 << 6)) != 0,
                PickExtraItems = (blob[1] & (1 << 7)) != 0,
                DarkRavenMode = blob[28],
                UseSelfDefense = (helper & (1 << 0)) != 0,
                AutoAcceptFriend = (helper & (1 << 1)) != 0,
                AutoAcceptGuild = (helper & (1 << 2)) != 0,
                FallbackBasicAttack = (helper & (1 << 3)) != 0
            };

            for (int i = 0; i < ExtraItemSlotCount; i++)
            {
                int slotStart = ExtraItemsOffset + i * ExtraItemSlotLength;
                int length = Array.IndexOf(blob, (byte)0, slotStart, ExtraItemSlotLength);
                if (length < 0)
                    length = ExtraItemSlotLength;
                else
                    length -= slotStart;

                if (length > 0)
                    config.ExtraItems.Add(Encoding.ASCII.GetString(blob, slotStart, length));
            }

            config.Normalize();
            return config;
        }

        private static void WriteHelperWord(byte[] data, int offset, int value) =>
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset, sizeof(ushort)),
                (ushort)Math.Clamp(value, 0, ushort.MaxValue));

        private static int ReadHelperWord(byte[] data, int offset) =>
            BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset, sizeof(ushort)));

        public void Normalize()
        {
            HuntingRange = Math.Clamp(HuntingRange, 0, 6);
            MaxSecondsAway = Math.Clamp(MaxSecondsAway, 0, 999);
            PotionThreshold = Math.Clamp(PotionThreshold, 0, 100);
            PotionHotbarSlot = Math.Clamp(PotionHotbarSlot, -1, 2);
            HealThreshold = Math.Clamp(HealThreshold, 0, 100);
            HealPartyThreshold = Math.Clamp(HealPartyThreshold, 0, 100);
            BuffCastIntervalSeconds = Math.Clamp(BuffCastIntervalSeconds, 0, 3600);
            ObtainingRange = Math.Clamp(ObtainingRange, 0, 8);
            DarkRavenMode = Math.Clamp(DarkRavenMode, 0, 2);
            ActivationSkill1 ??= new MuHelperSkillActivation();
            ActivationSkill2 ??= new MuHelperSkillActivation();
            ActivationSkill1.Normalize();
            ActivationSkill2.Normalize();
            if (BuffSkillIds == null || BuffSkillIds.Length != 3)
            {
                var normalized = new ushort[3];
                if (BuffSkillIds != null)
                    Array.Copy(BuffSkillIds, normalized, Math.Min(BuffSkillIds.Length, normalized.Length));
                BuffSkillIds = normalized;
            }
            ExtraItems ??= new List<string>();
            BlockedMapIds ??= new List<ushort>();
        }

        public MuHelperConfig Clone()
        {
            return new MuHelperConfig
            {
                HuntingRange = HuntingRange,
                LongRangeCounterAttack = LongRangeCounterAttack,
                ReturnToOriginalPosition = ReturnToOriginalPosition,
                MaxSecondsAway = MaxSecondsAway,
                BasicSkillId = BasicSkillId,
                ActivationSkill1 = ActivationSkill1?.Clone() ?? new MuHelperSkillActivation(),
                ActivationSkill2 = ActivationSkill2?.Clone() ?? new MuHelperSkillActivation(),
                UseCombo = UseCombo,
                ComboSkillIds = ComboSkillIds == null ? new ushort[3] : (ushort[])ComboSkillIds.Clone(),
                BuffSkillIds = BuffSkillIds == null ? new ushort[3] : (ushort[])BuffSkillIds.Clone(),
                BuffDuration = BuffDuration,
                BuffDurationParty = BuffDurationParty,
                BuffCastIntervalSeconds = BuffCastIntervalSeconds,
                AutoHeal = AutoHeal,
                HealThreshold = HealThreshold,
                SupportParty = SupportParty,
                AutoHealParty = AutoHealParty,
                HealPartyThreshold = HealPartyThreshold,
                UseHealPotion = UseHealPotion,
                PotionThreshold = PotionThreshold,
                PotionHotbarSlot = PotionHotbarSlot,
                UseDrainLife = UseDrainLife,
                UseDarkRaven = UseDarkRaven,
                DarkRavenMode = DarkRavenMode,
                RepairItem = RepairItem,
                ObtainingRange = ObtainingRange,
                PickAllItems = PickAllItems,
                PickSelectedItems = PickSelectedItems,
                PickJewel = PickJewel,
                PickZen = PickZen,
                PickAncient = PickAncient,
                PickExcellent = PickExcellent,
                PickExtraItems = PickExtraItems,
                ExtraItems = ExtraItems == null ? new List<string>() : new List<string>(ExtraItems),
                UseSelfDefense = UseSelfDefense,
                AutoAcceptFriend = AutoAcceptFriend,
                AutoAcceptGuild = AutoAcceptGuild,
                FallbackBasicAttack = FallbackBasicAttack,
                BlockedMapIds = BlockedMapIds == null ? new List<ushort>() : new List<ushort>(BlockedMapIds)
            };
        }
    }

    /// <summary>Activation options corresponding to the reference timer/condition skill slots.</summary>
    public sealed class MuHelperSkillActivation
    {
        public ushort SkillId { get; set; }
        public int DelaySeconds { get; set; }
        public bool UseTimer { get; set; } = true;
        public bool UseCondition { get; set; }
        public bool MobsAttacking { get; set; }
        public int MinimumNearbyMonsters { get; set; } = 2;

        public void Normalize()
        {
            DelaySeconds = Math.Clamp(DelaySeconds, 0, 3600);
            MinimumNearbyMonsters = Math.Clamp(MinimumNearbyMonsters, 1, 5);
        }

        public MuHelperSkillActivation Clone() => new()
        {
            SkillId = SkillId,
            DelaySeconds = DelaySeconds,
            UseTimer = UseTimer,
            UseCondition = UseCondition,
            MobsAttacking = MobsAttacking,
            MinimumNearbyMonsters = MinimumNearbyMonsters
        };
    }
}
