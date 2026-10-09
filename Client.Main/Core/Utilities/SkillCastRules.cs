using Client.Main.Models;
using Microsoft.Xna.Framework;

namespace Client.Main.Core.Utilities;

/// <summary>
/// Client cast eligibility from MuMain's SkillManager, SkillCast, ClassAttack and
/// ZzzInterface. These distances are not the server's damage/hit-test distances.
/// </summary>
internal static class SkillCastRules
{
    public static float GetRange(int skillId, float baseRange, bool darkHorse, bool bloodCastle, bool hasTarget)
    {
        float range = baseRange + (darkHorse ? 2f : 0f);

        // CastWarriorSkill: the Blood Castle override replaces even the mount bonus.
        if (hasTarget && IsWeaponSwing(skillId) && bloodCastle)
            return 1.8f;

        // SkillWarrior routes these skills through CastWarriorSkill when an enemy
        // is selected. Ground casts retain the unmultiplied ClassAttack distance.
        if (hasTarget && (IsWarriorTargetSkill(skillId) || IsRageFighterTargetSkill(skillId)))
            range *= 1.2f;

        return range;
    }

    public static bool IsInRange(Vector3 casterPosition, Vector2 targetTile, float range)
    {
        // MuMain CheckTile measures the rendered position to the target tile CENTER.
        // Zero is a real range, not an unlimited-range sentinel.
        float dx = casterPosition.X - ((int)targetTile.X + 0.5f) * Constants.TERRAIN_SCALE;
        float dy = casterPosition.Y - ((int)targetTile.Y + 0.5f) * Constants.TERRAIN_SCALE;
        float worldRange = range * Constants.TERRAIN_SCALE;
        return range >= 0f && dx * dx + dy * dy <= worldRange * worldRange;
    }

    private static bool IsWeaponSwing(int id) => id is >= 19 and <= 23 or 326 or 327 or 328 or 329 or 479;

    private static bool IsWarriorTargetSkill(int id) => IsWeaponSwing(id) ||
        id is 41 or 43 or 44 or 47 or 49 or 55 or 57 or 60 or 61 or 66 or 74
            or 330 or 332 or 336 or 481 or 490 or 508 or 509 or 514;

    private static bool IsRageFighterTargetSkill(int id) =>
        id is 260 or 261 or 262 or 264 or 265 or 269 or 270
            or 551 or 552 or 554 or 555 or 558 or 560;

    // These ClassAttack/AttackRagefighter branches execute before CheckTile.
    public static bool IgnoresTargetRange(int id, bool hasTarget) =>
        id is 10 or 14 or 388 or 381 or 486 or 263 or 559 ||
        (!hasTarget && id is 41 or 330 or 332 or 481);

    public static bool UsesCasterAreaPosition(int id) =>
        id is 8 or 9 or 10 or 12 or 14 or 24 or 41 or 52 or 56 or 65 or 78 or 235 or 236 or 238
            or 330 or 332 or 381 or 385 or 388 or 411 or 414 or 416 or 418 or 431
            or 481 or 482 or 486 or 487 or 518 or 523;

    // These server area skills require the selected victim in ExtraTargetId.
    public static bool RequiresExplicitAreaTarget(int id) =>
        id is 214 or 215 or 264 or 270 or 455 or 458 or 560;

    public static bool IsFriendlyTargetSkill(int id) =>
        id is 16 or 26 or 27 or 28 or 217 or 234 or 403 or 404 or 413 or 417 or 420 or 422 or 423;

    public static bool CanStartCast(PlayerAction action) =>
        action is >= PlayerAction.PlayerStopMale and <= PlayerAction.PlayerStopRideWeapon
            or PlayerAction.PlayerStopTwoHandSwordTwo or PlayerAction.PlayerSkillHellBegin
            or PlayerAction.PlayerDarklordStand or PlayerAction.PlayerStopRideHorse
            or >= PlayerAction.PlayerFenrirStand and <= PlayerAction.PlayerFenrirStandOneLeft
            or >= PlayerAction.PlayerRageFenrirStand and <= PlayerAction.PlayerRageFenrirStandOneLeft
            or PlayerAction.PlayerRageUniStopOneRight or PlayerAction.PlayerStopRagefighter;

    public static bool BlocksWalking(PlayerAction action)
    {
        // MuMain MoveHero explicitly permits these locomotion states, even though
        // several fall inside the legacy PLAYER_ATTACK_FIST..PLAYER_RIDE_SKILL range.
        if (action is >= PlayerAction.PlayerStopTwoHandSwordTwo and <= PlayerAction.PlayerRunTwoHandSwordTwo
            or >= PlayerAction.PlayerDarklordStand and <= PlayerAction.PlayerRunRideHorse
            or >= PlayerAction.PlayerFenrirRun and <= PlayerAction.PlayerFenrirWalkOneLeft
            or >= PlayerAction.PlayerRageFenrirWalk and <= PlayerAction.PlayerRageFenrirStandOneLeft)
            return false;

        return action is >= PlayerAction.PlayerAttackFist and <= PlayerAction.PlayerRideSkill
            or >= PlayerAction.PlayerSkillSleep and <= PlayerAction.PlayerSkillLightningShock
            or PlayerAction.PlayerRecoverSkill
            or >= PlayerAction.PlayerSkillThrust and <= PlayerAction.PlayerSkillHpUpOurforces;
    }
}
