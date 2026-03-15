using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Models;
using MmaSimulator.Simulation.Styles;

namespace MmaSimulator.Simulation.Physics;

internal static class PhysicalAdvantageModel
{
    /// <summary>
    /// Calculates the share of initiative fighter A should have on a given tick.
    /// </summary>
    public static double InitiativeShare(FighterState fighterA, FighterState fighterB)
    {
        var aScore = BuildInitiativeScore(fighterA, fighterB);
        var bScore = BuildInitiativeScore(fighterB, fighterA);
        return aScore / (aScore + bScore);
    }

    /// <summary>
    /// Applies style-aware physical modifiers to striking accuracy.
    /// </summary>
    public static double StrikeAccuracyMultiplier(FighterState attacker, FighterState defender)
    {
        var styleReachFactor = attacker.Fighter.PrimaryStyle switch
        {
            _ when attacker.Fighter.GetStyleProficiency(FightingStyle.Kickboxer) >= 80 => 0.0080,
            _ when attacker.Fighter.GetStyleProficiency(FightingStyle.MuayThai) >= 80 => 0.0075,
            _ when attacker.Fighter.GetStyleProficiency(FightingStyle.Boxer) >= 80 => 0.0050,
            _ when attacker.Fighter.HasAnySpecialty(StyleSpecialty.KarateDistance, StyleSpecialty.TaekwondoKicks) => 0.0070,
            _ => 0.0035
        };

        var styleHeightFactor = attacker.Fighter.PrimaryStyle switch
        {
            FightingStyle.Boxer => 0.0004,
            FightingStyle.Kickboxer or FightingStyle.MuayThai or FightingStyle.Striker => 0.0006,
            _ => 0.0004
        };

        var multiplier = BuildPhysicalEdge(
            attacker,
            defender,
            weightFactor: 0.0018,
            reachFactor: styleReachFactor,
            heightFactor: styleHeightFactor,
            strengthFactor: 0.0014,
            min: 0.60,
            max: 1.45);

        if (attacker.Fighter.GetStyleProficiency(FightingStyle.Boxer) >= 75)
        {
            var shortReachCompensation = Math.Clamp((defender.Fighter.Physical.ReachCm - attacker.Fighter.Physical.ReachCm) / 100.0, 0.0, 0.10);
            multiplier += shortReachCompensation * 1.3;
        }

        return Math.Clamp(multiplier, 0.60, 1.45);
    }

    /// <summary>
    /// Applies physical advantages that make defending strikes easier or harder.
    /// </summary>
    public static double StrikeDefenseMultiplier(FighterState defender, FighterState attacker)
        => BuildPhysicalEdge(defender, attacker, weightFactor: 0.0014, reachFactor: 0.0020, heightFactor: 0.0006, strengthFactor: 0.0010, min: 0.75, max: 1.25);

    /// <summary>
    /// Applies style-aware physical modifiers to strike damage.
    /// </summary>
    public static double StrikeDamageMultiplier(FighterState attacker, FighterState defender)
    {
        var styleReachFactor = attacker.Fighter.PrimaryStyle switch
        {
            _ when attacker.Fighter.GetStyleProficiency(FightingStyle.Kickboxer) >= 80 => 0.0028,
            _ when attacker.Fighter.GetStyleProficiency(FightingStyle.MuayThai) >= 80 => 0.0026,
            _ when attacker.Fighter.GetStyleProficiency(FightingStyle.Boxer) >= 80 => 0.0016,
            _ when attacker.Fighter.HasAnySpecialty(StyleSpecialty.KarateDistance, StyleSpecialty.TaekwondoKicks) => 0.0024,
            _ => 0.0010
        };

        var multiplier = BuildPhysicalEdge(
            attacker,
            defender,
            weightFactor: 0.0030,
            reachFactor: styleReachFactor,
            heightFactor: 0.0006,
            strengthFactor: 0.0020,
            min: 0.55,
            max: 1.85);

        if (attacker.Fighter.GetStyleProficiency(FightingStyle.Boxer) >= 75)
        {
            var insideBonus = Math.Clamp((defender.Fighter.Physical.ReachCm - attacker.Fighter.Physical.ReachCm) / 80.0, 0.0, 0.12);
            multiplier += insideBonus * 1.35;
        }

        return Math.Clamp(multiplier, 0.55, 1.85);
    }

    /// <summary>
    /// Calculates how much the defender's physical profile reduces incoming head damage.
    /// </summary>
    public static double HeadDamageResistanceMultiplier(FighterState defender, FighterState attacker)
        => BuildPhysicalEdge(defender, attacker, weightFactor: 0.0020, reachFactor: 0.0, heightFactor: 0.0006, strengthFactor: 0.0018, min: 0.75, max: 1.45);

    /// <summary>
    /// Calculates how much the defender's physical profile reduces incoming body damage.
    /// </summary>
    public static double BodyDamageResistanceMultiplier(FighterState defender, FighterState attacker)
        => BuildPhysicalEdge(defender, attacker, weightFactor: 0.0022, reachFactor: 0.0, heightFactor: 0.0004, strengthFactor: 0.0018, min: 0.72, max: 1.50);

    /// <summary>
    /// Estimates the physical contribution to knockdown risk.
    /// </summary>
    public static double KnockdownThreatMultiplier(FighterState attacker, FighterState defender)
        => BuildPhysicalEdge(attacker, defender, weightFactor: 0.0042, reachFactor: 0.0, heightFactor: 0.0008, strengthFactor: 0.0024, min: 0.45, max: 1.90);

    /// <summary>
    /// Applies physical leverage and level-change modifiers to takedown success.
    /// </summary>
    public static double TakedownSuccessMultiplier(FighterState attacker, FighterState defender)
    {
        var baseMultiplier = BuildPhysicalEdge(attacker, defender, weightFactor: 0.0085, reachFactor: 0.0, heightFactor: 0.0, strengthFactor: 0.0060, min: 0.08, max: 4.00);
        var heightDiff = attacker.Fighter.Physical.HeightCm - defender.Fighter.Physical.HeightCm;
        var levelChangeBonus = Math.Clamp(-heightDiff * 0.010, -0.18, 0.25);
        return Math.Clamp(baseMultiplier * (1.0 + levelChangeBonus), 0.08, 4.25);
    }

    /// <summary>
    /// Applies physical leverage modifiers to top control on the mat.
    /// </summary>
    public static double TopControlMultiplier(FighterState attacker, FighterState defender)
    {
        var baseMultiplier = BuildPhysicalEdge(attacker, defender, weightFactor: 0.0080, reachFactor: 0.0, heightFactor: 0.0, strengthFactor: 0.0050, min: 0.10, max: 4.00);
        var heightDiff = attacker.Fighter.Physical.HeightCm - defender.Fighter.Physical.HeightCm;
        var leverageBonus = Math.Clamp(-heightDiff * 0.008, -0.15, 0.18);
        return Math.Clamp(baseMultiplier * (1.0 + leverageBonus), 0.10, 4.20);
    }

    /// <summary>
    /// Applies physical base and mobility modifiers to scrambles and escapes.
    /// </summary>
    public static double EscapeMultiplier(FighterState fighter, FighterState opponent)
    {
        var baseMultiplier = BuildPhysicalEdge(fighter, opponent, weightFactor: 0.0070, reachFactor: 0.0, heightFactor: 0.0, strengthFactor: 0.0040, min: 0.12, max: 2.20);
        var heightDiff = fighter.Fighter.Physical.HeightCm - opponent.Fighter.Physical.HeightCm;
        var lowBaseBonus = Math.Clamp(-heightDiff * 0.007, -0.12, 0.15);
        return Math.Clamp(baseMultiplier * (1.0 + lowBaseBonus), 0.12, 2.30);
    }

    /// <summary>
    /// Applies physical modifiers to clinch entries and tie-up control.
    /// </summary>
    public static double ClinchEntryMultiplier(FighterState attacker, FighterState defender)
        => BuildPhysicalEdge(attacker, defender, weightFactor: 0.0075, reachFactor: 0.003, heightFactor: 0.0020, strengthFactor: 0.0045, min: 0.10, max: 2.80);

    /// <summary>
    /// Determines whether the matchup should be treated as an extreme physical mismatch.
    /// </summary>
    public static bool IsMassiveMismatch(FighterState fighterA, FighterState fighterB)
        => Math.Abs(fighterA.Fighter.Physical.WeightLbs - fighterB.Fighter.Physical.WeightLbs) >= 60;

    /// <summary>
    /// Builds a per-fighter initiative score from speed, agility, stamina, reach, and mass.
    /// </summary>
    private static double BuildInitiativeScore(FighterState actor, FighterState opponent)
    {
        var strikingSpeed = actor.Fighter.Striking.Speed / 100.0;
        var agility = actor.Fighter.Athletics.Agility / 100.0;
        var stamina = Math.Max(0.35, actor.CurrentStamina);
        var reachTerm = Math.Clamp((actor.Fighter.Physical.ReachCm - opponent.Fighter.Physical.ReachCm) / 120.0, -0.20, 0.20);
        var effectiveWeightDiff = GetEffectiveWeightDifference(actor, opponent);
        var physicalTerm = Math.Clamp(effectiveWeightDiff / 450.0, -0.12, 0.18);
        return Math.Max(0.10, (strikingSpeed + agility) * 0.5 * stamina * actor.EffectiveMovementMultiplier * (1.0 + reachTerm + physicalTerm));
    }

    /// <summary>
    /// Builds a clamped multiplier from weight, reach, height, and strength differentials.
    /// </summary>
    private static double BuildPhysicalEdge(
        FighterState advantaged,
        FighterState disadvantaged,
        double weightFactor,
        double reachFactor,
        double heightFactor,
        double strengthFactor,
        double min,
        double max)
    {
        var weightDiff = GetEffectiveWeightDifference(advantaged, disadvantaged);
        var reachDiff = advantaged.Fighter.Physical.ReachCm - disadvantaged.Fighter.Physical.ReachCm;
        var heightDiff = advantaged.Fighter.Physical.HeightCm - disadvantaged.Fighter.Physical.HeightCm;
        var strengthDiff = advantaged.Fighter.Athletics.Strength - disadvantaged.Fighter.Athletics.Strength;

        var multiplier = 1.0
            + weightDiff * weightFactor
            + reachDiff * reachFactor
            + heightDiff * heightFactor
            + strengthDiff * strengthFactor;

        return Math.Clamp(multiplier, min, max);
    }

    /// <summary>
    /// Compresses weight differences for adjacent division groups and escalates them for distant groups.
    /// </summary>
    private static double GetEffectiveWeightDifference(FighterState advantaged, FighterState disadvantaged)
    {
        var rawDiff = advantaged.Fighter.Physical.WeightLbs - disadvantaged.Fighter.Physical.WeightLbs;
        if (rawDiff == 0)
            return 0;

        var absDiff = Math.Abs(rawDiff);
        var bandGap = Math.Abs(GetWeightBand(advantaged.Fighter.Physical.WeightLbs) - GetWeightBand(disadvantaged.Fighter.Physical.WeightLbs));

        var sameBandScaled = absDiff switch
        {
            <= 15 => absDiff * 0.18,
            <= 35 => 2.7 + (absDiff - 15) * 0.14,
            <= 60 => 5.5 + (absDiff - 35) * 0.10,
            _ => 8.0 + (absDiff - 60) * 0.08
        };

        var crossBandBoost = bandGap switch
        {
            0 => 1.00,
            1 => 1.45,
            2 => 2.25,
            _ => 3.35
        };

        return Math.Sign(rawDiff) * sameBandScaled * crossBandBoost;
    }

    /// <summary>
    /// Groups divisions into nearby natural-size bands so adjacent classes stay competitively close.
    /// </summary>
    private static int GetWeightBand(int weightLbs) => weightLbs switch
    {
        <= 135 => 0, // Flyweight / Bantamweight
        <= 155 => 1, // Featherweight / Lightweight
        <= 185 => 2, // Welterweight / Middleweight
        _ => 3       // Light Heavyweight / Heavyweight
    };
}
