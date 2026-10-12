using Client.Main.Models;

namespace Client.Main.Core.Utilities;

/// <summary>MuMain ZzzCharacter.cpp: equipped-weapon blur action selection.</summary>
public static class WeaponSkillVisualRules
{
    public static bool IsWeaponSkill(PlayerAction action) =>
        action is >= PlayerAction.PlayerAttackSkillSword1 and <= PlayerAction.PlayerAttackSkillSword5
        or PlayerAction.PlayerAttackOneFlash or PlayerAction.PlayerAttackRush or PlayerAction.PlayerAttackStrike
        or PlayerAction.PlayerSkillBlowOfDestruction
        or >= PlayerAction.PlayerSkillLightningOrb and <= PlayerAction.PlayerSkillLightningOrbFenrir;

    public static bool ShouldEmitTrail(PlayerAction action, byte itemGroup, bool rightHand, int frame)
    {
        // MuMain selects the left weapon only for the left-sword attack actions.
        bool leftAttack = action is PlayerAction.PlayerAttackSwordLeft1 or PlayerAction.PlayerAttackSwordLeft2;
        if (rightHand == leftAttack || itemGroup > 5 || itemGroup == 4)
            return false;
        if (action == PlayerAction.PlayerSkillBlowOfDestruction)
            return frame is >= 2 and <= 8;
        if (IsWeaponSkill(action))
            return true;
        if (itemGroup == 0)
            return action is >= PlayerAction.PlayerAttackSwordRight1 and <= PlayerAction.PlayerAttackTwoHandSword3
                or PlayerAction.PlayerAttackTwoHandSwordTwo;
        return itemGroup == 3 && action is >= PlayerAction.PlayerAttackSpear1 and <= PlayerAction.PlayerAttackScythe3;
    }
}
