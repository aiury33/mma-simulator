using MmaSimulator.Core.Enums;

namespace MmaSimulator.Core.Models;

public sealed class FighterState
{
    /// <summary>
    /// Creates a mutable in-fight state wrapper for the supplied fighter.
    /// </summary>
    public FighterState(Fighter fighter)
    {
        Fighter = fighter;
        CurrentStamina = 1.0;
        CurrentPosition = FightPosition.Standing;
    }

    public Fighter Fighter { get; }
    public double CurrentStamina { get; set; }
    public double AccumulatedHeadDamage { get; set; }
    public double AccumulatedBodyDamage { get; set; }
    public double AccumulatedLegDamage { get; set; }
    public bool IsStunned { get; set; }
    public int StunRecoveryTicksRemaining { get; set; }
    public FightPosition CurrentPosition { get; set; }
    public int KnockdownsThisFight { get; set; }
    public int GroundedTicksRemaining { get; set; }

    public double EffectiveStrikeAccuracy
    {
        get
        {
            var base_ = Fighter.Striking.Accuracy / 100.0;
            var staminaPenalty = CurrentStamina < 0.5
                ? 1.0 - (0.5 - CurrentStamina) * 0.6
                : 1.0;
            var legPenalty = Math.Clamp(1.0 - AccumulatedLegDamage / 110.0, 0.55, 1.0);
            var bodyPenalty = Math.Clamp(1.0 - AccumulatedBodyDamage / 140.0, 0.65, 1.0);
            var stunPenalty = IsStunned ? 0.72 : 1.0;
            return Math.Max(0.05, base_ * staminaPenalty * legPenalty * bodyPenalty * stunPenalty);
        }
    }

    public double EffectiveDamageMultiplier
    {
        get
        {
            var staminaFactor = 1.0 - (1.0 - CurrentStamina) * 0.4;
            var positionBonus = CurrentPosition switch
            {
                FightPosition.MountTop => 1.4,
                FightPosition.BackControlAttacker => 1.3,
                FightPosition.SideControlTop => 1.2,
                FightPosition.TurtleTop => 1.15,
                FightPosition.GroundAndPoundTop => 1.15,
                _ => 1.0
            };
            var bodyPenalty = Math.Clamp(1.0 - AccumulatedBodyDamage / 120.0, 0.60, 1.0);
            var legPenalty = Math.Clamp(1.0 - AccumulatedLegDamage / 140.0, 0.70, 1.0);
            return staminaFactor * positionBonus * bodyPenalty * legPenalty;
        }
    }

    public double EffectiveMovementMultiplier
    {
        get
        {
            var legPenalty = Math.Clamp(1.0 - AccumulatedLegDamage / 90.0, 0.40, 1.0);
            var bodyPenalty = Math.Clamp(1.0 - AccumulatedBodyDamage / 150.0, 0.70, 1.0);
            var stunPenalty = IsStunned ? 0.65 : 1.0;
            return legPenalty * bodyPenalty * stunPenalty;
        }
    }
}
