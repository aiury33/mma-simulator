using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Interfaces;
using MmaSimulator.Core.Models;
using MmaSimulator.Simulation.Narration;

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
            [StrikeType.Elbow]           = 1.15,
            [StrikeType.Knee]            = 1.00,
            [StrikeType.FrontKick]       = 0.65,
            [StrikeType.Roundhouse]      = 0.95,
            [StrikeType.HeadKick]        = 1.40,
            [StrikeType.SpinningBackKick]= 1.20
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
                               * Math.Max(0.3, defender.CurrentStamina);

        var blocked = defenseRoll < effectiveDefense * 0.5;
        var damage  = ComputeDamage(attacker, defender, strikeType, blocked);

        var knockdown = false;
        var stun      = false;

        if (!blocked && strikeType != StrikeType.BodyShot)
        {
            defender.AccumulatedHeadDamage += damage;
            (knockdown, stun) = CheckKoConditions(attacker, defender, damage);
        }
        else
        {
            // Body shots and blocked punches add only body damage
            defender.AccumulatedBodyDamage += damage * (strikeType == StrikeType.BodyShot ? 1.0 : 0.4);
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
        var base_        = attacker.Fighter.Striking.Accuracy / 100.0;
        var staminaMod   = Math.Max(0.5, attacker.CurrentStamina);
        var reachDiff    = attacker.Fighter.Physical.ReachCm - defender.Fighter.Physical.ReachCm;
        var reachMod     = 1.0 + Math.Clamp(reachDiff / 200.0, -0.1, 0.1);
        var stancePenalty = SameStancePenalty(attacker.Fighter.Stance, defender.Fighter.Stance);
        return Math.Clamp(base_ * staminaMod * reachMod * (1 - stancePenalty), 0.05, 0.95);
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
                         * attacker.EffectiveDamageMultiplier;

        if (blocked) baseDamage *= 0.4;

        return Math.Clamp(baseDamage * (1 + (_random.NextDouble() - 0.5) * 0.2), 0.1, 10.0);
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

        var dangerMult       = GetKoDangerMultiplier(attacker, defender);
        var normalizedDamage = defender.AccumulatedHeadDamage / (20.0 * Math.Max(0.3, chin));

        // Exponential growth: early strikes are relatively safe; later strikes are increasingly lethal
        var koThreshold = (1.0 - Math.Exp(-normalizedDamage * dangerMult)) * (1.0 - toughness * 0.3);
        koThreshold = Math.Clamp(koThreshold, 0.0, 0.95);

        if (_random.Chance(koThreshold * 0.35))
            return (true, false);

        if (_random.Chance(koThreshold * 0.55))
            return (false, true);

        return (false, false);
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
        var baseMultiplier = attacker.Fighter.Physical.WeightLbs switch
        {
            >= 235 => 2.0,   // Heavyweight
            >= 195 => 1.5,   // Light Heavyweight
            >= 180 => 1.2,   // Middleweight
            >= 165 => 1.0,   // Welterweight
            >= 150 => 0.85,  // Lightweight
            >= 140 => 0.75,  // Featherweight
            >= 130 => 0.65,  // Bantamweight
            _      => 0.55   // Flyweight
        };

        // Cross-division weight advantage makes the smaller fighter's KO danger skyrocket
        var weightDiff = attacker.Fighter.Physical.WeightLbs - defender.Fighter.Physical.WeightLbs;
        if (weightDiff > 0)
        {
            var diffBonus = Math.Clamp(weightDiff / 30.0, 0.0, 3.0);
            baseMultiplier *= 1.0 + diffBonus;
        }

        return baseMultiplier;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private double ApplyNoise(double value) =>
        Math.Clamp(value + (_random.NextDouble() - 0.5) * 2 * _randomnessFactor, 0.01, 0.99);

    private static double SameStancePenalty(Stance attacker, Stance defender)
    {
        if (attacker == Stance.Switch || defender == Stance.Switch) return 0;
        return attacker == defender ? 0.05 : 0;
    }

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
