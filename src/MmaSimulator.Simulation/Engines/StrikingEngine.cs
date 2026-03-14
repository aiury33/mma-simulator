using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Interfaces;
using MmaSimulator.Core.Models;
using MmaSimulator.Simulation.Narration;

namespace MmaSimulator.Simulation.Engines;

public sealed class StrikingEngine : IStrikingEngine
{
    private static readonly IReadOnlyDictionary<StrikeType, double> PowerMultipliers = new Dictionary<StrikeType, double>
    {
        [StrikeType.Jab] = 0.4,
        [StrikeType.Cross] = 0.9,
        [StrikeType.Hook] = 1.0,
        [StrikeType.Uppercut] = 0.85,
        [StrikeType.BodyShot] = 0.7,
        [StrikeType.Overhand] = 1.1,
        [StrikeType.Elbow] = 1.15,
        [StrikeType.Knee] = 1.0,
        [StrikeType.FrontKick] = 0.65,
        [StrikeType.Roundhouse] = 0.95,
        [StrikeType.HeadKick] = 1.4,
        [StrikeType.SpinningBackKick] = 1.2
    };

    private readonly IRandomProvider _random;
    private readonly NarrationBuilder _narration;
    private readonly double _randomnessFactor;

    public StrikingEngine(IRandomProvider random, NarrationBuilder narration, double randomnessFactor = 0.15)
    {
        _random = random;
        _narration = narration;
        _randomnessFactor = randomnessFactor;
    }

    public StrikeOutcome ResolveStrike(FighterState attacker, FighterState defender, StrikeType strikeType)
    {
        var effectiveAccuracy = ComputeEffectiveAccuracy(attacker, defender);
        var roll = ApplyNoise(_random.NextDouble());

        if (roll >= effectiveAccuracy)
        {
            var ev = CreateEvent(FightEventType.StrikeMissed, attacker, defender, strikeType, 0);
            return new StrikeOutcome(false, false, 0, false, false, _narration.BuildForStrike(ev));
        }

        var defenseRoll = ApplyNoise(_random.NextDouble());
        var effectiveDefense = defender.Fighter.Striking.Defense / 100.0 * Math.Max(0.3, defender.CurrentStamina);

        var blocked = defenseRoll < effectiveDefense * 0.5;
        var damage = ComputeDamage(attacker, defender, strikeType, blocked);

        var knockdown = false;
        var stun = false;

        if (!blocked && strikeType != StrikeType.BodyShot)
        {
            defender.AccumulatedHeadDamage += damage;
            (knockdown, stun) = CheckKoConditions(defender);
        }
        else
        {
            defender.AccumulatedBodyDamage += damage * (strikeType == StrikeType.BodyShot ? 1.0 : 0.4);
        }

        var eventType = knockdown ? FightEventType.KnockdownScored
            : blocked ? FightEventType.StrikeBlocked
            : FightEventType.StrikeLanded;

        var fightEvent = CreateEvent(eventType, attacker, defender, strikeType, damage);
        return new StrikeOutcome(true, blocked, damage, knockdown, stun, _narration.BuildForStrike(fightEvent));
    }

    private double ComputeEffectiveAccuracy(FighterState attacker, FighterState defender)
    {
        var base_ = attacker.Fighter.Striking.Accuracy / 100.0;
        var staminaMod = Math.Max(0.5, attacker.CurrentStamina);
        var reachDiff = attacker.Fighter.Physical.ReachCm - defender.Fighter.Physical.ReachCm;
        var reachMod = 1.0 + Math.Clamp(reachDiff / 200.0, -0.1, 0.1);
        var stancePenalty = SameStancePenalty(attacker.Fighter.Stance, defender.Fighter.Stance);
        return Math.Clamp(base_ * staminaMod * reachMod * (1 - stancePenalty), 0.05, 0.95);
    }

    private double ComputeDamage(FighterState attacker, FighterState defender, StrikeType strikeType, bool blocked)
    {
        var baseDamage = attacker.Fighter.Striking.Power / 100.0
            * PowerMultipliers.GetValueOrDefault(strikeType, 1.0)
            * attacker.EffectiveDamageMultiplier;

        if (blocked) baseDamage *= 0.4;

        return Math.Clamp(baseDamage * (1 + (_random.NextDouble() - 0.5) * 0.2), 0.1, 10.0);
    }

    private (bool knockdown, bool stun) CheckKoConditions(FighterState defender)
    {
        var chin = defender.Fighter.Striking.ChinDurability / 100.0;
        var toughness = defender.Fighter.Athletics.Toughness / 100.0;
        var koThreshold = (defender.AccumulatedHeadDamage / 50.0) * (1 - chin) * (1 - toughness * 0.5);
        koThreshold = Math.Clamp(koThreshold, 0, 0.95);

        if (_random.Chance(koThreshold * 0.3))
            return (true, false);

        if (_random.Chance(koThreshold * 0.5))
            return (false, true);

        return (false, false);
    }

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
            Timestamp = TimeSpan.Zero,
            Type = type,
            Actor = attacker.Fighter,
            Target = defender.Fighter,
            PositionBefore = attacker.CurrentPosition,
            PositionAfter = attacker.CurrentPosition,
            StrikeType = strikeType,
            DamageDealt = damage,
            // Any clean (non-blocked) landed strike counts as significant, matching UFC stat methodology
            IsSignificant = (type == FightEventType.StrikeLanded || type == FightEventType.KnockdownScored)
        };
}
