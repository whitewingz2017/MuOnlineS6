namespace Client.Main.Objects.Effects
{
    /// <summary>AttackStage and MoveJoint rules shared with the headless regression checks.</summary>
    internal static class GreaterFortitudeRules
    {
        internal static bool ShouldEmit(bool vitality, float attackTime) =>
            (vitality && (int)attackTime > 9) || (int)attackTime >= 15;

        // MoveJoint performs generic flight using the old speed, then adds five.
        internal static float TakeVelocity(ref float velocity)
        {
            float flightVelocity = velocity;
            velocity += 5f;
            return flightVelocity;
        }

        internal static void FadeTrail(int life, ref float brightness)
        {
            if (life < 10) brightness /= 1.2f;
        }
    }
}
