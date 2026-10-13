using System;
namespace Client.Main.Core.Utilities;

public static class ExperienceProgress
{
    // MuMain RenderExperience and OpenMU's default cumulative EXP table.
    public static ulong LevelStart(int level)
    {
        ulong previous = (ulong)Math.Max(0, level - 1);
        ulong total = (previous + 9) * previous * previous * 10;
        if (previous > 255)
        {
            ulong excess = previous - 255;
            total += (excess + 9) * excess * excess * 1000;
        }
        return total;
    }
    public static ulong MasterLevelStart(int level)
    {
        ulong n = (ulong)Math.Max(0, level);
        return 505 * n * n * n + 228045 * n * n + 35278500 * n;
    }
    public static double Ratio(ulong current, ulong next, ulong start) =>
        next > start ? Math.Clamp(((double)current - start) / (next - start), 0d, 1d) : 0d;
    public static double For(Client.CharacterState state) => state.Level >= 400
        ? Ratio(state.MasterExperience, state.MasterExperienceForNextLevel, MasterLevelStart(state.MasterLevel))
        : Ratio(state.Experience, state.ExperienceForNextLevel, LevelStart(state.Level));
    public static int Digit(double ratio) => Math.Clamp((int)(ratio * 10), 0, 9);
    public static double Segment(double ratio) => ratio >= 1 ? 1 : ratio * 10 - Digit(ratio);
}
