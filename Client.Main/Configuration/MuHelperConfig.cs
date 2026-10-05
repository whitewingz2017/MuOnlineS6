using System;
using System.Collections.Generic;

namespace Client.Main.Configuration
{
    /// <summary>
    /// Client-side representation of MuMain's MU Helper ConfigData. Network/server
    /// serialization is intentionally omitted: this client uses local settings only.
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
        public bool PickSelectedItems { get; set; }
        public bool PickJewel { get; set; }
        public bool PickZen { get; set; } = true;
        public bool PickAncient { get; set; }
        public bool PickExcellent { get; set; }
        public bool PickExtraItems { get; set; }
        public List<string> ExtraItems { get; set; } = new();

        // Local-only Helper preferences. The reference client stores these outside
        // the server packet so existing Helper protocol layouts remain untouched.
        public bool UseSelfDefense { get; set; }
        public bool AutoAcceptFriend { get; set; }
        public bool AutoAcceptGuild { get; set; }
        public bool FallbackBasicAttack { get; set; } = true;
        public List<ushort> BlockedMapIds { get; set; } = new();

        public void Normalize()
        {
            HuntingRange = Math.Clamp(HuntingRange, 0, 15);
            MaxSecondsAway = Math.Clamp(MaxSecondsAway, 0, 999);
            PotionThreshold = Math.Clamp(PotionThreshold, 0, 100);
            PotionHotbarSlot = Math.Clamp(PotionHotbarSlot, -1, 2);
            HealThreshold = Math.Clamp(HealThreshold, 0, 100);
            HealPartyThreshold = Math.Clamp(HealPartyThreshold, 0, 100);
            BuffCastIntervalSeconds = Math.Clamp(BuffCastIntervalSeconds, 0, 3600);
            ObtainingRange = Math.Clamp(ObtainingRange, 0, 15);
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
