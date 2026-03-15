using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Models;

namespace MmaSimulator.Simulation.Styles;

internal static class StyleProfileExtensions
{
    /// <summary>
    /// Converts a style proficiency into a normalized multiplier around the given baseline.
    /// </summary>
    public static double StyleFactor(this Fighter fighter, FightingStyle style, double baseline = 70.0) =>
        Math.Clamp(fighter.GetStyleProficiency(style) / baseline, 0.25, 1.60);

    /// <summary>
    /// Converts a specialty proficiency into a normalized multiplier around the given baseline.
    /// </summary>
    public static double SpecialtyFactor(this Fighter fighter, StyleSpecialty specialty, double baseline = 75.0)
    {
        var proficiency = fighter.GetSpecialtyProficiency(specialty);
        if (proficiency == 0) return 1.0;
        return Math.Clamp(proficiency / baseline, 0.75, 1.60);
    }

    /// <summary>
    /// Returns whether the fighter has at least one of the requested specialties.
    /// </summary>
    public static bool HasAnySpecialty(this Fighter fighter, params StyleSpecialty[] specialties) =>
        specialties.Any(fighter.HasSpecialty);

    /// <summary>
    /// Returns the fighter's strongest striking-oriented style profile.
    /// </summary>
    public static FightingStyle DominantStrikingStyle(this Fighter fighter)
    {
        var styles = new[]
        {
            FightingStyle.Boxer,
            FightingStyle.Kickboxer,
            FightingStyle.MuayThai,
            FightingStyle.Striker,
            FightingStyle.MMAFighter
        };

        return styles
            .OrderByDescending(fighter.GetStyleProficiency)
            .First();
    }
}
