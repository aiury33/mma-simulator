using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Interfaces;
using MmaSimulator.Core.Models;
using MmaSimulator.Simulation.Narration;

namespace MmaSimulator.Simulation.Engines;

public sealed class GrapplingEngine : IGrapplingEngine
{
    private readonly IRandomProvider _random;
    private readonly NarrationBuilder _narration;

    public GrapplingEngine(IRandomProvider random, NarrationBuilder narration)
    {
        _random = random;
        _narration = narration;
    }

    public GrappleOutcome ResolveGrappleAction(FighterState actor, FighterState opponent, GrappleAction action)
    {
        return action switch
        {
            GrappleAction.TakedownAttempt => ResolveTakedown(actor, opponent),
            GrappleAction.SubmissionAttemptChoke or
            GrappleAction.SubmissionAttemptArmlock or
            GrappleAction.SubmissionAttemptLeglock => ResolveSubmission(actor, opponent, action),
            GrappleAction.GuardPass => ResolveGuardPass(actor, opponent),
            GrappleAction.Escape or GrappleAction.GetUp => ResolveEscape(actor, opponent),
            GrappleAction.Sweep => ResolveSweep(actor, opponent),
            _ => new GrappleOutcome(false, actor.CurrentPosition, 0, false, $"{actor.Fighter.FullName} attempts a grappling move.")
        };
    }

    private GrappleOutcome ResolveTakedown(FighterState actor, FighterState opponent)
    {
        var styleBonus = actor.Fighter.PrimaryStyle == FightingStyle.Wrestler ? 0.15 : 0.0;
        var attackTD = actor.Fighter.Grappling.TakedownAccuracy / 100.0 * (1 + styleBonus) * actor.CurrentStamina;
        var defendTD = opponent.Fighter.Grappling.TakedownDefense / 100.0
            * (1 + opponent.Fighter.Athletics.Strength / 100.0 * 0.2)
            * opponent.CurrentStamina;
        var successProb = Math.Clamp(attackTD / (defendTD + 0.01), 0.05, 0.85);

        if (_random.Chance(successProb))
        {
            var newPos = FightPosition.GroundAndPoundTop;
            actor.CurrentPosition = newPos;
            opponent.CurrentPosition = FightPosition.GroundAndPoundBottom;
            var ev = CreateGrappleEvent(FightEventType.TakedownLanded, actor, opponent);
            return new GrappleOutcome(true, newPos, 0.5, false, _narration.BuildForGrapple(ev));
        }

        var ev2 = CreateGrappleEvent(FightEventType.TakedownDefended, actor, opponent);
        return new GrappleOutcome(false, actor.CurrentPosition, 0, false, _narration.BuildForGrapple(ev2));
    }

    private GrappleOutcome ResolveSubmission(FighterState actor, FighterState opponent, GrappleAction action)
    {
        var posMultiplier = actor.CurrentPosition switch
        {
            FightPosition.BackControlAttacker => 1.5,
            FightPosition.MountTop => 1.3,
            FightPosition.SideControlTop => 1.1,
            FightPosition.FullGuardTop => 0.9,
            _ => 0.7
        };

        var lockedProb = actor.Fighter.Grappling.SubmissionOffense / 100.0 * posMultiplier * actor.CurrentStamina;
        var escapeProb = opponent.Fighter.Grappling.SubmissionDefense / 100.0
            * (1 - opponent.AccumulatedBodyDamage / 60.0)
            * opponent.CurrentStamina;
        escapeProb = Math.Clamp(escapeProb, 0.05, 0.95);

        if (!_random.Chance(Math.Clamp(lockedProb, 0.02, 0.70)))
        {
            var attemptEv = CreateGrappleEvent(FightEventType.SubmissionAttempted, actor, opponent);
            return new GrappleOutcome(false, actor.CurrentPosition, 0, false, _narration.BuildForGrapple(attemptEv));
        }

        if (_random.Chance(1 - escapeProb))
        {
            var finishEv = CreateGrappleEvent(FightEventType.SubmissionLocked, actor, opponent);
            return new GrappleOutcome(true, actor.CurrentPosition, 0, true, _narration.BuildForGrapple(finishEv));
        }

        var escapeEv = CreateGrappleEvent(FightEventType.SubmissionEscaped, actor, opponent);
        return new GrappleOutcome(false, actor.CurrentPosition, 1.0, false, _narration.BuildForGrapple(escapeEv));
    }

    private GrappleOutcome ResolveGuardPass(FighterState actor, FighterState opponent)
    {
        var passProb = actor.Fighter.Grappling.GroundControl / 100.0 * 0.4 * actor.CurrentStamina;

        if (_random.Chance(passProb))
        {
            var newPos = FightPosition.SideControlTop;
            actor.CurrentPosition = newPos;
            opponent.CurrentPosition = FightPosition.SideControlBottom;
            var ev = CreateGrappleEvent(FightEventType.PositionChange, actor, opponent);
            return new GrappleOutcome(true, newPos, 0, false, _narration.BuildForGrapple(ev));
        }

        return new GrappleOutcome(false, actor.CurrentPosition, 0, false,
            $"{actor.Fighter.FullName} tries to pass the guard but {opponent.Fighter.FullName} maintains position.");
    }

    private GrappleOutcome ResolveEscape(FighterState actor, FighterState opponent)
    {
        var escapePenalty = actor.CurrentPosition == FightPosition.BackControlDefender ? 0.5 : 1.0;
        var escapeProb = actor.Fighter.Grappling.SubmissionDefense / 100.0 * actor.CurrentStamina
            * actor.Fighter.Athletics.Agility / 100.0 * escapePenalty;

        if (_random.Chance(Math.Clamp(escapeProb, 0.05, 0.6)))
        {
            actor.CurrentPosition = FightPosition.Standing;
            opponent.CurrentPosition = FightPosition.Standing;
            return new GrappleOutcome(true, FightPosition.Standing, 0, false,
                $"{actor.Fighter.FullName} works back to the feet!");
        }

        return new GrappleOutcome(false, actor.CurrentPosition, 0, false,
            $"{actor.Fighter.FullName} attempts to escape but {opponent.Fighter.FullName} maintains control.");
    }

    private GrappleOutcome ResolveSweep(FighterState actor, FighterState opponent)
    {
        var sweepProb = actor.Fighter.Grappling.SubmissionOffense / 100.0 * 0.5 * actor.CurrentStamina;

        if (_random.Chance(sweepProb))
        {
            actor.CurrentPosition = FightPosition.MountTop;
            opponent.CurrentPosition = FightPosition.MountBottom;
            return new GrappleOutcome(true, FightPosition.MountTop, 0, false,
                $"{actor.Fighter.FullName} sweeps {opponent.Fighter.FullName} and takes mount!");
        }

        return new GrappleOutcome(false, actor.CurrentPosition, 0, false,
            $"{actor.Fighter.FullName} attempts a sweep but {opponent.Fighter.FullName} bases out.");
    }

    private static FightEvent CreateGrappleEvent(FightEventType type, FighterState actor, FighterState opponent) =>
        new()
        {
            Timestamp = TimeSpan.Zero,
            Type = type,
            Actor = actor.Fighter,
            Target = opponent.Fighter,
            PositionBefore = actor.CurrentPosition,
            PositionAfter = actor.CurrentPosition,
            GrappleAction = null,
            IsSignificant = type is FightEventType.TakedownLanded or FightEventType.SubmissionLocked or FightEventType.KnockdownScored
        };
}
