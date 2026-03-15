using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Interfaces;
using MmaSimulator.Core.Models;
using MmaSimulator.Simulation.Narration;
using MmaSimulator.Simulation.Physics;
using MmaSimulator.Simulation.Styles;

namespace MmaSimulator.Simulation.Engines;

/// <summary>
/// Resolves individual strike attempts, computing accuracy, damage, and finish probability.
///
/// <para><b>Damage model:</b>
/// Raw damage = (Power / 100) × strikePowerMultiplier × positionMultiplier.
/// A blocked strike deals 40% of its computed damage.
/// Body shots only accumulate body damage and never trigger head KO checks.
/// </para>
///
/// <para><b>KO / stun model (exponential accumulation):</b>
/// KO danger grows exponentially with accumulated head damage, scaled by a
/// <i>danger multiplier</i> that accounts for the attacker's weight class and
/// any cross-division weight difference. This means:
/// <list type="bullet">
///   <item>A heavyweight's shots are roughly twice as threatening as a flyweight's.</item>
///   <item>A 115-lb difference (e.g., Jones vs a flyweight) can produce a near-certain
///       KO within 5–8 clean strikes.</item>
///   <item>Two elite LHW knockout artists (Pereira vs Prochazka) rarely survive into
///       the third round.</item>
/// </list>
/// </para>
/// </summary>
public sealed class StrikingEngine : IStrikingEngine
{
    // ── Strike power table ────────────────────────────────────────────────
    // Multipliers applied to the attacker's base Power stat.
    // Head kicks and elbows carry the most concussive force; jabs are probing tools.
    private static readonly IReadOnlyDictionary<StrikeType, double> PowerMultipliers =
        new Dictionary<StrikeType, double>
        {
            [StrikeType.Jab]             = 0.40,
            [StrikeType.Cross]           = 0.90,
            [StrikeType.Hook]            = 1.00,
            [StrikeType.Uppercut]        = 0.85,
            [StrikeType.BodyShot]        = 0.70,
            [StrikeType.Overhand]        = 1.10,
            [StrikeType.ElbowHorizontal] = 1.15,
            [StrikeType.ElbowUpward]     = 1.10,
            [StrikeType.SpinningElbow]   = 1.30,
            [StrikeType.KneeBody]        = 1.00,
            [StrikeType.KneeHead]        = 1.25,
            [StrikeType.FrontKick]       = 0.50,
            [StrikeType.Teep]            = 0.46,
            [StrikeType.BodyKick]        = 0.82,
            [StrikeType.LowKick]         = 0.75,
            [StrikeType.CalfKick]        = 0.70,
            [StrikeType.ObliqueKick]     = 0.55,
            [StrikeType.Roundhouse]      = 0.95,
            [StrikeType.HeadKick]        = 1.40,
            [StrikeType.SpinningBackKick]= 1.20,
            [StrikeType.Stomp]           = 0.65,
            [StrikeType.GroundPunch]     = 1.12,
            [StrikeType.GroundElbow]     = 1.45,
            [StrikeType.Hammerfist]      = 1.18
        };

    private readonly IRandomProvider _random;
    private readonly NarrationBuilder _narration;
    private readonly double _randomnessFactor;

    /// <summary>Initialises the engine with a random provider, narration builder, and optional noise factor.</summary>
    /// <param name="random">Seeded or live random source used for all probability rolls.</param>
    /// <param name="narration">Converts raw fight-event data into human-readable narration strings.</param>
    /// <param name="randomnessFactor">Noise applied to accuracy and defense rolls (default 0.15).</param>
    public StrikingEngine(IRandomProvider random, NarrationBuilder narration, double randomnessFactor = 0.15)
    {
        _random          = random;
        _narration       = narration;
        _randomnessFactor = randomnessFactor;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Resolution pipeline:
    /// <list type="number">
    ///   <item>Accuracy roll — miss returns <see cref="StrikeOutcome"/> with <c>Landed = false</c>.</item>
    ///   <item>Defense roll — blocked strikes deal 40% damage and skip KO checks.</item>
    ///   <item>Damage computation — power × strike-type multiplier × position bonus, plus ±10% jitter.</item>
    ///   <item>KO / stun check — only for unblocked head strikes; uses exponential danger model.</item>
    /// </list>
    /// </remarks>
    public StrikeOutcome ResolveStrike(FighterState attacker, FighterState defender, StrikeType strikeType)
    {
        var effectiveAccuracy = ComputeEffectiveAccuracy(attacker, defender);
        var roll              = ApplyNoise(_random.NextDouble());

        if (roll >= effectiveAccuracy)
        {
            var ev = CreateEvent(FightEventType.StrikeMissed, attacker, defender, strikeType, 0);
            return new StrikeOutcome(false, false, 0, false, false, _narration.BuildForStrike(ev));
        }

        var defenseRoll      = ApplyNoise(_random.NextDouble());
        var effectiveDefense = defender.Fighter.Striking.Defense / 100.0
                               * PhysicalAdvantageModel.StrikeDefenseMultiplier(defender, attacker)
                               * defender.Fighter.SpecialtyFactor(StyleSpecialty.BoxingCountering)
                               * Math.Max(0.3, defender.CurrentStamina);

        var blocked = defenseRoll < effectiveDefense * 0.5;
        var damage  = ComputeDamage(attacker, defender, strikeType, blocked);

        var knockdown = false;
        var stun      = false;

        if (!blocked && IsHeadStrike(strikeType))
        {
            defender.AccumulatedHeadDamage += damage;
            knockdown = CheckFlashKo(attacker, defender, strikeType, damage);
            var accumulatedResult = CheckKoConditions(attacker, defender, damage);
            knockdown = knockdown || accumulatedResult.knockdown;
            stun = accumulatedResult.stun;
        }
        else if (!blocked && IsLegStrike(strikeType))
        {
            defender.AccumulatedLegDamage += damage;
            defender.CurrentStamina = Math.Max(0.05, defender.CurrentStamina - Math.Clamp(damage / 40.0, 0.01, 0.12));
            (knockdown, stun) = CheckLegFinishConditions(attacker, defender, strikeType, damage);
        }
        else
        {
            defender.AccumulatedBodyDamage += damage * (IsBodyStrike(strikeType) ? 1.0 : 0.4);

            if (!blocked && IsBodyStrike(strikeType))
            {
                defender.CurrentStamina = Math.Max(0.05, defender.CurrentStamina - Math.Clamp(damage / 45.0, 0.01, 0.10));
                (knockdown, stun) = CheckBodyFinishConditions(attacker, defender, strikeType, damage);
            }
        }

        if (stun)
        {
            defender.IsStunned = true;
            defender.StunRecoveryTicksRemaining = Math.Max(defender.StunRecoveryTicksRemaining, 4);
        }

        var eventType  = knockdown   ? FightEventType.KnockdownScored
                       : blocked     ? FightEventType.StrikeBlocked
                                     : FightEventType.StrikeLanded;

        var fightEvent = CreateEvent(eventType, attacker, defender, strikeType, damage);
        return new StrikeOutcome(true, blocked, damage, knockdown, stun, _narration.BuildForStrike(fightEvent));
    }

    // ── Accuracy ──────────────────────────────────────────────────────────

    /// <summary>
    /// Effective accuracy = base accuracy × stamina modifier × reach advantage × stance penalty.
    /// Stamina never drops accuracy below 50%. Reach advantage caps at ±10%.
    /// </summary>
    private double ComputeEffectiveAccuracy(FighterState attacker, FighterState defender)
    {
        var base_        = attacker.EffectiveStrikeAccuracy;
        var staminaMod   = Math.Max(0.5, attacker.CurrentStamina);
        var reachDiff    = attacker.Fighter.Physical.ReachCm - defender.Fighter.Physical.ReachCm;
        var reachMod     = 1.0 + Math.Clamp(reachDiff / 500.0, -0.04, 0.04);
        var physicalMod  = PhysicalAdvantageModel.StrikeAccuracyMultiplier(attacker, defender);
        var technicalMod = PhysicalAdvantageModel.StandingTechnicalEdgeMultiplier(attacker, defender);
        var stancePenalty = SameStancePenalty(attacker.Fighter.Stance, defender.Fighter.Stance);
        return Math.Clamp(base_ * staminaMod * reachMod * physicalMod * technicalMod * (1 - stancePenalty), 0.03, 0.98);
    }

    // ── Damage ────────────────────────────────────────────────────────────

    /// <summary>
    /// Damage = (Power / 100) × typeMultiplier × positionBonus, with ±10% random jitter.
    /// Blocked strikes deal 40% damage. Result is clamped to [0.1, 10.0].
    /// </summary>
    private double ComputeDamage(FighterState attacker, FighterState defender,
        StrikeType strikeType, bool blocked)
    {
        var baseDamage = attacker.Fighter.Striking.Power / 100.0
                         * PowerMultipliers.GetValueOrDefault(strikeType, 1.0)
                         * attacker.EffectiveDamageMultiplier
                         * PhysicalAdvantageModel.StrikeDamageMultiplier(attacker, defender);

        baseDamage *= GetImpactScale(attacker, strikeType);

        if (IsGroundStrike(strikeType) && IsDominantGroundPosition(attacker.CurrentPosition))
        {
            var specialtyBonus = strikeType switch
            {
                StrikeType.GroundElbow => attacker.Fighter.SpecialtyFactor(StyleSpecialty.GroundAndPoundElbows),
                StrikeType.GroundPunch or StrikeType.Hammerfist => attacker.Fighter.SpecialtyFactor(StyleSpecialty.GroundAndPoundPunches),
                _ => 1.0
            };

            var positionBonus = attacker.CurrentPosition switch
            {
                FightPosition.MountTop => 1.22,
                FightPosition.BackControlAttacker => 1.16,
                FightPosition.SideControlTop or FightPosition.TurtleTop => 1.13,
                _ => 1.08
            };

            baseDamage *= positionBonus * specialtyBonus;
        }

        var durability = IsBodyStrike(strikeType)
            ? defender.Fighter.Striking.BodyDurability / 100.0
            : IsLegStrike(strikeType)
                ? defender.Fighter.Striking.BodyDurability / 100.0
            : defender.Fighter.Striking.ChinDurability / 100.0;

        var resistance = IsBodyStrike(strikeType)
            ? PhysicalAdvantageModel.BodyDamageResistanceMultiplier(defender, attacker)
            : IsLegStrike(strikeType)
                ? PhysicalAdvantageModel.BodyDamageResistanceMultiplier(defender, attacker) * 0.9
            : PhysicalAdvantageModel.HeadDamageResistanceMultiplier(defender, attacker);

        baseDamage /= Math.Max(0.45, durability * resistance);

        if (blocked)
        {
            var blockedMultiplier = IsHeadStrike(strikeType) ? 0.14
                : IsBodyStrike(strikeType) ? 0.20
                : 0.24;
            baseDamage *= blockedMultiplier;
        }

        return Math.Clamp(baseDamage * (1 + (_random.NextDouble() - 0.5) * 0.2), 0.05, 14.5);
    }

    // ── KO / Stun check ───────────────────────────────────────────────────

    /// <summary>
    /// Determines whether the most recent unblocked head strike caused a knockdown or stun.
    ///
    /// <para>The KO threshold uses an exponential build-up formula:
    /// <c>koThreshold = (1 − e^(−normalizedDmg × dangerMult)) × (1 − toughness × 0.3)</c>
    /// where <c>normalizedDmg = accumulatedHeadDamage / (20 × chinDurability)</c>.</para>
    ///
    /// <para>This means early in a fight even a Pereira head kick carries only ~2–4% KO risk,
    /// but after 25–35 accumulated strikes (roughly one round) the danger becomes very high,
    /// reflecting how real knockouts build from accumulated punishment.</para>
    ///
    /// <para>The <see cref="GetKoDangerMultiplier"/> further scales the threshold by the
    /// attacker's weight class and any cross-division weight advantage, ensuring that
    /// heavyweight punches and super-fights behave realistically.</para>
    /// </summary>
    private (bool knockdown, bool stun) CheckKoConditions(
        FighterState attacker, FighterState defender, double lastDamage)
    {
        var chin      = defender.Fighter.Striking.ChinDurability / 100.0;
        var toughness = defender.Fighter.Athletics.Toughness    / 100.0;

        var dangerMult       = GetKoDangerMultiplier(attacker, defender)
                               * PhysicalAdvantageModel.KnockdownThreatMultiplier(attacker, defender);
        var normalizedDamage = defender.AccumulatedHeadDamage / (20.0 * Math.Max(0.3, chin));
        var damageSpike      = 1.0 + Math.Clamp(lastDamage / 8.0, 0.0, 0.6);

        // Exponential growth: early strikes are relatively safe; later strikes are increasingly lethal
        var koThreshold = (1.0 - Math.Exp(-normalizedDamage * dangerMult * damageSpike)) * (1.0 - toughness * 0.35);
        koThreshold = Math.Clamp(koThreshold, 0.0, 0.95);

        if (_random.Chance(koThreshold * 0.45))
            return (true, false);

        if (_random.Chance(koThreshold * 0.70))
            return (false, true);

        return (false, false);
    }

    /// <summary>
    /// Evaluates rare one-shot knockdowns or knockouts from perfectly landed head strikes.
    /// </summary>
    private bool CheckFlashKo(FighterState attacker, FighterState defender, StrikeType strikeType, double damage)
    {
        var flashMultiplier = strikeType switch
        {
            StrikeType.HeadKick => 1.55,
            StrikeType.SpinningElbow => 1.45,
            StrikeType.Overhand or StrikeType.Hook or StrikeType.KneeHead => 1.25,
            _ => 1.0
        };

        if (strikeType is StrikeType.Cross or StrikeType.Hook or StrikeType.Uppercut or StrikeType.Overhand)
        {
            flashMultiplier *= attacker.Fighter.Striking.Power switch
            {
                >= 97 => 1.30,
                >= 92 => 1.18,
                _ => 1.0
            };
        }

        var power = attacker.Fighter.Striking.Power / 100.0;
        var chin = defender.Fighter.Striking.ChinDurability / 100.0;
        var flashProb = Math.Max(0.0, damage - 7.5) / 11.0
            * flashMultiplier
            * power
            * GetKoDangerMultiplier(attacker, defender)
            * (1.0 - chin * 0.55);

        return _random.Chance(Math.Clamp(flashProb, 0.0, 0.09));
    }

    /// <summary>
    /// Evaluates liver-shot style knockdowns and body-shot stuns.
    /// </summary>
    private (bool knockdown, bool stun) CheckBodyFinishConditions(
        FighterState attacker, FighterState defender, StrikeType strikeType, double damage)
    {
        var bodyDamage = defender.AccumulatedBodyDamage;
        var bodyDurability = defender.Fighter.Striking.BodyDurability / 100.0;
        var staminaFactor = 1.0 - Math.Max(0.25, defender.CurrentStamina) * 0.55;

        var liverMultiplier = strikeType switch
        {
            StrikeType.BodyShot => 1.30,
            StrikeType.BodyKick => 1.40,
            StrikeType.KneeBody => 1.45,
            StrikeType.Teep or StrikeType.FrontKick => 0.85,
            _ => 1.0
        };

        var knockdownProb = Math.Clamp((bodyDamage / 32.0) * liverMultiplier * (damage / 8.0) * (1.0 - bodyDurability * 0.55) * (0.65 + staminaFactor), 0.0, 0.40);
        if (_random.Chance(knockdownProb))
            return (true, false);

        var stunProb = Math.Clamp((bodyDamage / 42.0) * liverMultiplier * (damage / 9.5) * (1.0 - bodyDurability * 0.45), 0.0, 0.52);
        return _random.Chance(stunProb) ? (false, true) : (false, false);
    }

    /// <summary>
    /// Evaluates leg-kick collapses and knee-line damage that can floor or badly stun a fighter.
    /// </summary>
    private (bool knockdown, bool stun) CheckLegFinishConditions(
        FighterState attacker, FighterState defender, StrikeType strikeType, double damage)
    {
        var legDamage = defender.AccumulatedLegDamage;
        var bodyDurability = defender.Fighter.Striking.BodyDurability / 100.0;
        var movementLoss = 1.0 - defender.EffectiveMovementMultiplier;

        var collapseMultiplier = strikeType switch
        {
            StrikeType.ObliqueKick or StrikeType.Stomp => 1.55,
            StrikeType.CalfKick => 1.18,
            StrikeType.LowKick or StrikeType.Roundhouse => 1.08,
            _ => 1.0
        };

        var knockdownProb = Math.Clamp((legDamage / 30.0) * collapseMultiplier * (damage / 9.0) * (0.45 + movementLoss) * (1.0 - bodyDurability * 0.35), 0.0, 0.28);
        if (_random.Chance(knockdownProb))
            return (true, false);

        var stunProb = Math.Clamp((legDamage / 38.0) * collapseMultiplier * (damage / 11.0) * (0.35 + movementLoss), 0.0, 0.45);
        return _random.Chance(stunProb) ? (false, true) : (false, false);
    }

    /// <summary>
    /// Returns the KO danger multiplier for a given attacker/defender pair.
    ///
    /// <para><b>Weight-class base multipliers:</b>
    /// Heavyweight = 2.0, Light Heavyweight = 1.5, Middleweight = 1.2, Welterweight = 1.0,
    /// Lightweight = 0.85, Featherweight = 0.75, Bantamweight = 0.65, Flyweight = 0.55.</para>
    ///
    /// <para><b>Cross-division bonus:</b> every 30 lbs of weight advantage adds 33% to the
    /// base multiplier, capped at 3× the base (i.e., Jones at 240 vs a 125-lb flyweight
    /// would hit at 2.0 × (1 + 3.0) = 8.0 — nearly insta-KO territory).</para>
    /// </summary>
    private static double GetKoDangerMultiplier(FighterState attacker, FighterState defender)
    {
        var attackerBand = GetKoWeightBand(attacker.CurrentWeightLbs);
        var defenderBand = GetKoWeightBand(defender.CurrentWeightLbs);

        var baseMultiplier = attackerBand switch
        {
            3 => 1.16, // Light heavyweight / heavyweight stay close to each other
            2 => 1.00, // Welterweight / middleweight
            1 => 0.86, // Featherweight / lightweight
            _ => 0.72  // Flyweight / bantamweight
        };

        var weightDiff = attacker.CurrentWeightLbs - defender.CurrentWeightLbs;
        if (weightDiff <= 0)
            return baseMultiplier;

        var bandGap = Math.Abs(attackerBand - defenderBand);
        var diffBonus = bandGap switch
        {
            0 => Math.Clamp(weightDiff / 220.0, 0.0, 0.08),
            1 => Math.Clamp(weightDiff / 150.0, 0.0, 0.18),
            2 => Math.Clamp(weightDiff / 90.0, 0.0, 0.45),
            _ => Math.Clamp(weightDiff / 60.0, 0.0, 0.90)
        };

        return baseMultiplier * (1.0 + diffBonus);
    }

    /// <summary>
    /// Groups nearby divisions for KO calibration so adjacent upper divisions remain competitively close.
    /// </summary>
    private static int GetKoWeightBand(double weightLbs) => weightLbs switch
    {
        <= 135 => 0,
        <= 155 => 1,
        <= 185 => 2,
        _ => 3
    };

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Applies bounded random noise to a probability roll or scalar value.
    /// </summary>
    private double ApplyNoise(double value) =>
        Math.Clamp(value + (_random.NextDouble() - 0.5) * 2 * _randomnessFactor, 0.01, 0.99);

    /// <summary>
    /// Applies a small penalty when both fighters share the same stance.
    /// </summary>
    private static double SameStancePenalty(Stance attacker, Stance defender)
    {
        if (attacker == Stance.Switch || defender == Stance.Switch) return 0;
        return attacker == defender ? 0.05 : 0;
    }

    /// <summary>
    /// Returns whether the strike should be treated as a head-targeting attack.
    /// </summary>
    private static bool IsHeadStrike(StrikeType strikeType) => strikeType is
        StrikeType.Jab or StrikeType.Cross or StrikeType.Hook or StrikeType.Uppercut or StrikeType.Overhand
        or StrikeType.ElbowHorizontal or StrikeType.ElbowUpward or StrikeType.SpinningElbow
        or StrikeType.KneeHead or StrikeType.HeadKick or StrikeType.GroundPunch
        or StrikeType.GroundElbow or StrikeType.Hammerfist;

    /// <summary>
    /// Returns whether the strike should be treated as a body-targeting attack.
    /// </summary>
    private static bool IsBodyStrike(StrikeType strikeType) => strikeType is
        StrikeType.BodyShot or StrikeType.KneeBody or StrikeType.FrontKick or StrikeType.Teep
        or StrikeType.BodyKick or StrikeType.SpinningBackKick;

    /// <summary>
    /// Returns whether the strike should be treated as a leg-targeting attack.
    /// </summary>
    private static bool IsLegStrike(StrikeType strikeType) => strikeType is
        StrikeType.LowKick or StrikeType.CalfKick or StrikeType.ObliqueKick or StrikeType.Roundhouse or StrikeType.Stomp;

    /// <summary>
    /// Adds extra impact for elite power strikers, especially in the upper weight classes.
    /// </summary>
    private static double GetImpactScale(FighterState attacker, StrikeType strikeType)
    {
        var weightBonus = attacker.CurrentWeightLbs switch
        {
            >= 205 => 1.10,
            >= 170 => 1.05,
            _ => 1.0
        };

        var powerBonus = attacker.Fighter.Striking.Power switch
        {
            >= 97 => 1.16,
            >= 92 => 1.10,
            >= 86 => 1.05,
            _ => 1.0
        };

        var techniqueBonus = strikeType switch
        {
            StrikeType.Cross or StrikeType.Hook or StrikeType.Uppercut or StrikeType.Overhand => 1.08,
            StrikeType.HeadKick or StrikeType.KneeHead or StrikeType.SpinningElbow => 1.12,
            StrikeType.BodyKick or StrikeType.KneeBody or StrikeType.BodyShot => 1.06,
            _ => 1.0
        };

        return Math.Clamp(weightBonus * powerBonus * techniqueBonus, 1.0, 1.38);
    }

    /// <summary>
    /// Returns whether the strike should be treated as a ground-and-pound attack.
    /// </summary>
    private static bool IsGroundStrike(StrikeType strikeType) => strikeType is
        StrikeType.GroundPunch or StrikeType.GroundElbow or StrikeType.Hammerfist;

    /// <summary>
    /// Returns whether the attacker is in a dominant top position that amplifies ground strikes.
    /// </summary>
    private static bool IsDominantGroundPosition(FightPosition position) => position is
        FightPosition.GroundAndPoundTop or
        FightPosition.TurtleTop or
        FightPosition.SideControlTop or
        FightPosition.MountTop or
        FightPosition.BackControlAttacker;

    /// <summary>
    /// Creates an internal strike event for narration and downstream bookkeeping.
    /// </summary>
    private static FightEvent CreateEvent(FightEventType type, FighterState attacker, FighterState defender,
        StrikeType strikeType, double damage) =>
        new()
        {
            Timestamp      = TimeSpan.Zero,
            Type           = type,
            Actor          = attacker.Fighter,
            Target         = defender.Fighter,
            PositionBefore = attacker.CurrentPosition,
            PositionAfter  = attacker.CurrentPosition,
            StrikeType     = strikeType,
            DamageDealt    = damage,
            // All clean (non-blocked) landed strikes count as significant, matching UFC methodology
            IsSignificant  = type is FightEventType.StrikeLanded or FightEventType.KnockdownScored
        };
}
