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
            _ when attacker.Fighter.GetStyleProficiency(FightingStyle.Kickboxer) >= 80 => 0.0055,
            _ when attacker.Fighter.GetStyleProficiency(FightingStyle.MuayThai) >= 80 => 0.0052,
            _ when attacker.Fighter.GetStyleProficiency(FightingStyle.Boxer) >= 80 => 0.0050,
            _ when attacker.Fighter.HasAnySpecialty(StyleSpecialty.KarateDistance, StyleSpecialty.TaekwondoKicks) => 0.0052,
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

        multiplier *= GetExtremeMismatchOffensePenalty(attacker, defender, mildFloor: 0.55, severeFloor: 0.26, catastrophicFloor: 0.12);
        return Math.Clamp(multiplier, 0.12, 1.45);
    }

    /// <summary>
    /// Applies physical advantages that make defending strikes easier or harder.
    /// </summary>
    public static double StrikeDefenseMultiplier(FighterState defender, FighterState attacker)
    {
        var multiplier = BuildPhysicalEdge(defender, attacker, weightFactor: 0.0014, reachFactor: 0.0020, heightFactor: 0.0006, strengthFactor: 0.0010, min: 0.75, max: 1.25);
        multiplier *= GetExtremeMismatchDefenseBonus(defender, attacker);
        multiplier *= DefensiveStrikingReadMultiplier(defender, attacker);
        return Math.Clamp(multiplier, 0.75, 1.45);
    }

    /// <summary>
    /// Applies non-physical striking skill edge so elite technicians are not flattened by mere size and reach.
    /// </summary>
    public static double StandingTechnicalEdgeMultiplier(FighterState attacker, FighterState defender)
    {
        var attackerQuality =
            attacker.Fighter.Striking.Accuracy * 0.34 +
            attacker.Fighter.Striking.Defense * 0.08 +
            attacker.Fighter.Striking.Speed * 0.14 +
            attacker.Fighter.Striking.Power * 0.16 +
            attacker.Fighter.GetStyleProficiency(attacker.Fighter.DominantStrikingStyle()) * 0.18 +
            attacker.Fighter.FightIq * 0.10;

        var defenderQuality =
            defender.Fighter.Striking.Defense * 0.38 +
            defender.Fighter.Striking.ChinDurability * 0.18 +
            defender.Fighter.Striking.BodyDurability * 0.10 +
            defender.Fighter.Athletics.Agility * 0.12 +
            defender.Fighter.GetStyleProficiency(defender.Fighter.DominantStrikingStyle()) * 0.14 +
            defender.Fighter.FightIq * 0.12;

        var edge = attackerQuality - defenderQuality;
        var countering = defender.Fighter.HasSpecialty(StyleSpecialty.BoxingCountering) ? 0.06 : 0.0;
        return Math.Clamp(1.0 + edge / 180.0 - countering, 0.76, 1.24);
    }

    /// <summary>
    /// Applies defensive striking awareness so elite defenders and counter strikers are harder to hit cleanly.
    /// </summary>
    public static double DefensiveStrikingReadMultiplier(FighterState defender, FighterState attacker)
    {
        var defensiveRead =
            defender.Fighter.Striking.Defense * 0.42 +
            defender.Fighter.Athletics.Agility * 0.16 +
            defender.Fighter.FightIq * 0.14 +
            defender.Fighter.GetStyleProficiency(defender.Fighter.DominantStrikingStyle()) * 0.14 +
            (defender.Fighter.HasSpecialty(StyleSpecialty.BoxingCountering) ? 10.0 : 0.0) +
            (defender.Fighter.HasSpecialty(StyleSpecialty.KarateDistance) ? 6.0 : 0.0);

        var attackerRead =
            attacker.Fighter.Striking.Accuracy * 0.34 +
            attacker.Fighter.Striking.Speed * 0.18 +
            attacker.Fighter.FightIq * 0.12 +
            attacker.Fighter.GetStyleProficiency(attacker.Fighter.DominantStrikingStyle()) * 0.16;

        var edge = defensiveRead - attackerRead;
        return Math.Clamp(1.0 + edge / 240.0, 0.84, 1.20);
    }

    /// <summary>
    /// Reduces standing output when the opponent is a clearly better defensive reader and counter striker.
    /// </summary>
    public static double StandingOutputMultiplier(FighterState actor, FighterState opponent)
    {
        var actorPressure =
            actor.Fighter.Striking.Accuracy * 0.26 +
            actor.Fighter.Striking.Speed * 0.18 +
            actor.Fighter.FightIq * 0.14 +
            actor.Fighter.GetStyleProficiency(actor.Fighter.DominantStrikingStyle()) * 0.20 +
            actor.Fighter.Striking.Power * 0.10 +
            (actor.Fighter.HasSpecialty(StyleSpecialty.KickboxingPressure) ? 8.0 : 0.0);

        var opponentRead =
            opponent.Fighter.Striking.Defense * 0.34 +
            opponent.Fighter.Athletics.Agility * 0.14 +
            opponent.Fighter.FightIq * 0.16 +
            opponent.Fighter.GetStyleProficiency(opponent.Fighter.DominantStrikingStyle()) * 0.18 +
            (opponent.Fighter.HasSpecialty(StyleSpecialty.BoxingCountering) ? 12.0 : 0.0);

        var edge = actorPressure - opponentRead;
        var styleBias =
            (actor.Fighter.HasSpecialty(StyleSpecialty.KickboxingPressure) ? 0.08 : 0.0) +
            (actor.Fighter.HasSpecialty(StyleSpecialty.BoxingPocketPressure) ? 0.06 : 0.0) -
            (actor.Fighter.HasSpecialty(StyleSpecialty.KickboxingRange) ? 0.05 : 0.0) -
            (actor.Fighter.HasSpecialty(StyleSpecialty.BoxingCountering) ? 0.04 : 0.0);

        return Math.Clamp(1.0 + edge / 320.0 + styleBias, 0.72, 1.18);
    }

    /// <summary>
    /// Applies style-aware physical modifiers to strike damage.
    /// </summary>
    public static double StrikeDamageMultiplier(FighterState attacker, FighterState defender)
    {
        var styleReachFactor = attacker.Fighter.PrimaryStyle switch
        {
            _ when attacker.Fighter.GetStyleProficiency(FightingStyle.Kickboxer) >= 80 => 0.0018,
            _ when attacker.Fighter.GetStyleProficiency(FightingStyle.MuayThai) >= 80 => 0.0017,
            _ when attacker.Fighter.GetStyleProficiency(FightingStyle.Boxer) >= 80 => 0.0016,
            _ when attacker.Fighter.HasAnySpecialty(StyleSpecialty.KarateDistance, StyleSpecialty.TaekwondoKicks) => 0.0017,
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

        multiplier *= GetExtremeMismatchOffensePenalty(attacker, defender, mildFloor: 0.50, severeFloor: 0.18, catastrophicFloor: 0.08);
        return Math.Clamp(multiplier, 0.08, 1.85);
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
    {
        var multiplier = BuildPhysicalEdge(attacker, defender, weightFactor: 0.0042, reachFactor: 0.0, heightFactor: 0.0008, strengthFactor: 0.0024, min: 0.45, max: 1.90);
        multiplier *= GetExtremeMismatchOffensePenalty(attacker, defender, mildFloor: 0.45, severeFloor: 0.12, catastrophicFloor: 0.04);
        return Math.Clamp(multiplier, 0.04, 1.90);
    }

    /// <summary>
    /// Applies physical leverage and level-change modifiers to takedown success.
    /// </summary>
    public static double TakedownSuccessMultiplier(FighterState attacker, FighterState defender)
    {
        var baseMultiplier = BuildPhysicalEdge(attacker, defender, weightFactor: 0.0085, reachFactor: 0.0, heightFactor: 0.0, strengthFactor: 0.0060, min: 0.08, max: 4.00);
        var heightDiff = attacker.Fighter.Physical.HeightCm - defender.Fighter.Physical.HeightCm;
        var levelChangeBonus = Math.Clamp(-heightDiff * 0.010, -0.18, 0.25);
        var multiplier = baseMultiplier * (1.0 + levelChangeBonus);
        multiplier *= GetExtremeMismatchOffensePenalty(attacker, defender, mildFloor: 0.48, severeFloor: 0.14, catastrophicFloor: 0.06);
        return Math.Clamp(multiplier, 0.06, 4.25);
    }

    /// <summary>
    /// Applies physical leverage modifiers to top control on the mat.
    /// </summary>
    public static double TopControlMultiplier(FighterState attacker, FighterState defender)
    {
        var baseMultiplier = BuildPhysicalEdge(attacker, defender, weightFactor: 0.0080, reachFactor: 0.0, heightFactor: 0.0, strengthFactor: 0.0050, min: 0.10, max: 4.00);
        var heightDiff = attacker.Fighter.Physical.HeightCm - defender.Fighter.Physical.HeightCm;
        var leverageBonus = Math.Clamp(-heightDiff * 0.008, -0.15, 0.18);
        var multiplier = baseMultiplier * (1.0 + leverageBonus);
        multiplier *= GetExtremeMismatchOffensePenalty(attacker, defender, mildFloor: 0.55, severeFloor: 0.16, catastrophicFloor: 0.07);
        return Math.Clamp(multiplier, 0.07, 4.20);
    }

    /// <summary>
    /// Applies physical base and mobility modifiers to scrambles and escapes.
    /// </summary>
    public static double EscapeMultiplier(FighterState fighter, FighterState opponent)
    {
        var baseMultiplier = BuildPhysicalEdge(fighter, opponent, weightFactor: 0.0070, reachFactor: 0.0, heightFactor: 0.0, strengthFactor: 0.0040, min: 0.12, max: 2.20);
        var heightDiff = fighter.Fighter.Physical.HeightCm - opponent.Fighter.Physical.HeightCm;
        var lowBaseBonus = Math.Clamp(-heightDiff * 0.007, -0.12, 0.15);
        var multiplier = baseMultiplier * (1.0 + lowBaseBonus);
        multiplier *= GetExtremeMismatchOffensePenalty(fighter, opponent, mildFloor: 0.72, severeFloor: 0.40, catastrophicFloor: 0.22);
        return Math.Clamp(multiplier, 0.12, 2.30);
    }

    /// <summary>
    /// Applies physical modifiers to clinch entries and tie-up control.
    /// </summary>
    public static double ClinchEntryMultiplier(FighterState attacker, FighterState defender)
    {
        var multiplier = BuildPhysicalEdge(attacker, defender, weightFactor: 0.0075, reachFactor: 0.003, heightFactor: 0.0020, strengthFactor: 0.0045, min: 0.10, max: 2.80);
        multiplier *= GetExtremeMismatchOffensePenalty(attacker, defender, mildFloor: 0.58, severeFloor: 0.24, catastrophicFloor: 0.12);
        return Math.Clamp(multiplier, 0.10, 2.80);
    }

    /// <summary>
    /// Determines whether the matchup should be treated as an extreme physical mismatch.
    /// </summary>
    public static bool IsMassiveMismatch(FighterState fighterA, FighterState fighterB)
        => Math.Abs(fighterA.CurrentWeightLbs - fighterB.CurrentWeightLbs) >= 60;

    /// <summary>
    /// Builds a per-fighter initiative score from speed, agility, stamina, reach, and mass.
    /// </summary>
    private static double BuildInitiativeScore(FighterState actor, FighterState opponent)
    {
        var strikingSpeed = actor.Fighter.Striking.Speed / 100.0;
        var agility = actor.Fighter.Athletics.Agility / 100.0;
        var stamina = Math.Max(0.35, actor.CurrentStamina);
        var reachTerm = Math.Clamp((actor.Fighter.Physical.ReachCm - opponent.Fighter.Physical.ReachCm) / 220.0, -0.08, 0.08);
        var effectiveWeightDiff = GetEffectiveWeightDifference(actor, opponent);
        var physicalTerm = Math.Clamp(effectiveWeightDiff / 700.0, -0.07, 0.10);
        var pressureBias =
            (actor.Fighter.HasSpecialty(StyleSpecialty.KickboxingPressure) ? 0.10 : 0.0) +
            (actor.Fighter.HasSpecialty(StyleSpecialty.BoxingPocketPressure) ? 0.07 : 0.0) -
            (actor.Fighter.HasSpecialty(StyleSpecialty.KickboxingRange) ? 0.05 : 0.0) -
            (actor.Fighter.HasSpecialty(StyleSpecialty.BoxingCountering) ? 0.03 : 0.0);

        var score = (strikingSpeed + agility) * 0.5 * stamina * actor.EffectiveMovementMultiplier * (1.0 + reachTerm + physicalTerm + pressureBias);
        score *= StandingTechnicalEdgeMultiplier(actor, opponent);
        score *= GetExtremeMismatchOffensePenalty(actor, opponent, mildFloor: 0.80, severeFloor: 0.55, catastrophicFloor: 0.38);
        return Math.Max(0.06, score);
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
        var rawDiff = advantaged.CurrentWeightLbs - disadvantaged.CurrentWeightLbs;
        if (rawDiff == 0)
            return 0;

        var absDiff = Math.Abs(rawDiff);
        var bandGap = Math.Abs(GetWeightBand(advantaged.CurrentWeightLbs) - GetWeightBand(disadvantaged.CurrentWeightLbs));

        var scaledDiff = bandGap switch
        {
            // Same natural-size band: keep differences light even when the listed class differs.
            // This is where LHW vs HW and MW vs WW should still look competitive.
            0 => absDiff switch
            {
                <= 20 => absDiff * 0.03,
                <= 45 => 0.60 + (absDiff - 20) * 0.035,
                _ => 1.475 + (absDiff - 45) * 0.025
            },

            // Adjacent bands: visible advantage, but still far from deterministic.
            1 => absDiff switch
            {
                <= 20 => absDiff * 0.06,
                <= 45 => 1.20 + (absDiff - 20) * 0.060,
                _ => 2.70 + (absDiff - 45) * 0.050
            },

            // Two-band gaps: now the size edge becomes severe.
            2 => absDiff switch
            {
                <= 30 => absDiff * 0.16,
                <= 60 => 4.80 + (absDiff - 30) * 0.14,
                _ => 9.00 + (absDiff - 60) * 0.10
            },

            // Three-band gaps: preserve extreme mismatch behavior for flyweight vs heavyweight.
            _ => absDiff switch
            {
                <= 40 => absDiff * 0.26,
                <= 80 => 10.40 + (absDiff - 40) * 0.22,
                _ => 19.20 + (absDiff - 80) * 0.18
            }
        };

        return Math.Sign(rawDiff) * scaledDiff;
    }

    /// <summary>
    /// Groups divisions into nearby natural-size bands so adjacent classes stay competitively close.
    /// </summary>
    private static int GetWeightBand(double weightLbs) => weightLbs switch
    {
        <= 135 => 0, // Flyweight / Bantamweight
        <= 155 => 1, // Featherweight / Lightweight
        <= 185 => 2, // Welterweight / Middleweight
        _ => 3       // Light Heavyweight / Heavyweight
    };

    /// <summary>
    /// Applies a nonlinear penalty when the acting fighter is massively smaller than the opponent.
    /// </summary>
    private static double GetExtremeMismatchOffensePenalty(
        FighterState actor,
        FighterState opponent,
        double mildFloor,
        double severeFloor,
        double catastrophicFloor)
    {
        var weightDiff = actor.CurrentWeightLbs - opponent.CurrentWeightLbs;
        if (weightDiff >= 0)
            return 1.0;

        var absDiff = Math.Abs(weightDiff);
        var bandGap = Math.Abs(GetWeightBand(actor.CurrentWeightLbs) - GetWeightBand(opponent.CurrentWeightLbs));

        if (absDiff < 30)
            return 1.0;

        if (bandGap <= 1 && absDiff < 45)
            return Math.Clamp(1.0 - (absDiff - 30) * 0.012, mildFloor, 1.0);

        if (bandGap == 1)
            return Math.Clamp(0.82 - (absDiff - 45) * 0.010, mildFloor, 1.0);

        if (bandGap == 2)
            return Math.Clamp(0.58 - Math.Max(0, absDiff - 55) * 0.006, severeFloor, 1.0);

        return Math.Clamp(0.30 - Math.Max(0, absDiff - 85) * 0.004, catastrophicFloor, 1.0);
    }

    /// <summary>
    /// Applies a bonus when the acting fighter is massively larger than the opponent.
    /// </summary>
    private static double GetExtremeMismatchDefenseBonus(FighterState actor, FighterState opponent)
    {
        var weightDiff = actor.CurrentWeightLbs - opponent.CurrentWeightLbs;
        if (weightDiff <= 0)
            return 1.0;

        var absDiff = weightDiff;
        var bandGap = Math.Abs(GetWeightBand(actor.CurrentWeightLbs) - GetWeightBand(opponent.CurrentWeightLbs));

        if (absDiff < 45)
            return 1.0;

        var bonus = bandGap switch
        {
            1 => 1.04 + Math.Min(0.08, (absDiff - 45) * 0.002),
            2 => 1.12 + Math.Min(0.18, (absDiff - 55) * 0.003),
            _ => 1.28 + Math.Min(0.28, (absDiff - 85) * 0.004)
        };

        return Math.Clamp(bonus, 1.0, 1.56);
    }
}
