using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Interfaces;
using MmaSimulator.Core.Models;

namespace MmaSimulator.Simulation.Engines;

public sealed class StaminaEngine : IStaminaEngine
{
    private static readonly IReadOnlyDictionary<FightEventType, double> DrainTable = new Dictionary<FightEventType, double>
    {
        [FightEventType.StrikeLanded] = 0.003,
        [FightEventType.StrikeMissed] = 0.004,
        [FightEventType.StrikeBlocked] = 0.002,
        [FightEventType.TakedownLanded] = 0.018,
        [FightEventType.TakedownDefended] = 0.012,
        [FightEventType.SubmissionAttempted] = 0.020,
        [FightEventType.SubmissionEscaped] = 0.015,
        [FightEventType.PositionChange] = 0.010,
        [FightEventType.KnockdownScored] = 0.005
    };

    public double CalculateStaminaDrain(FighterState fighter, FightEvent fightEvent)
    {
        var isActor = fightEvent.Actor.Id == fighter.Fighter.Id;
        if (!isActor) return 0;

        if (!DrainTable.TryGetValue(fightEvent.Type, out var baseDrain))
            return 0;

        var cardioFactor = 1.0 - (fighter.Fighter.Athletics.Cardio / 100.0) * 0.6;
        var ageFactor = 1.0 + Math.Max(0, (fighter.Fighter.Physical.Age - 30) * 0.01);

        return baseDrain * cardioFactor * ageFactor;
    }

    public double CalculateRoundRecovery(FighterState fighter)
    {
        var baseRecovery = 0.25;
        var cardioBonus = fighter.Fighter.Athletics.Cardio / 100.0 * 0.15;
        var ageRecoveryPenalty = Math.Max(0, (fighter.Fighter.Physical.Age - 28) * 0.005);
        var bodyDamagePenalty = fighter.AccumulatedBodyDamage / 80.0;

        return (baseRecovery + cardioBonus - ageRecoveryPenalty) * (1.0 - bodyDamagePenalty);
    }
}
