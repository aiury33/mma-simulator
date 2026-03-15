using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Interfaces;
using MmaSimulator.Core.Models;
using MmaSimulator.Simulation.Narration;
using MmaSimulator.Simulation.Physics;
using MmaSimulator.Simulation.Styles;

namespace MmaSimulator.Simulation.Engines;

public sealed class GrapplingEngine : IGrapplingEngine
{
    private readonly IRandomProvider _random;
    private readonly NarrationBuilder _narration;

    /// <summary>
    /// Creates the grappling engine with random and narration services.
    /// </summary>
    public GrapplingEngine(IRandomProvider random, NarrationBuilder narration)
    {
        _random = random;
        _narration = narration;
    }

    /// <summary>
    /// Resolves a grappling action and returns the resulting position, damage, and narration.
    /// </summary>
    public GrappleOutcome ResolveGrappleAction(FighterState actor, FighterState opponent, GrappleAction action)
    {
        return action switch
        {
            GrappleAction.TakedownAttempt or
            GrappleAction.SingleLegTakedown or
            GrappleAction.DoubleLegTakedown or
            GrappleAction.BodyLockTakedown or
            GrappleAction.TripFromClinch or
            GrappleAction.OutsideTrip or
            GrappleAction.InsideTrip => ResolveTakedown(actor, opponent, action),
            GrappleAction.SubmissionAttemptRearNakedChoke or
            GrappleAction.SubmissionAttemptGuillotine or
            GrappleAction.SubmissionAttemptDarce or
            GrappleAction.SubmissionAttemptAnaconda or
            GrappleAction.SubmissionAttemptArmTriangle or
            GrappleAction.SubmissionAttemptTriangle or
            GrappleAction.SubmissionAttemptArmbar or
            GrappleAction.SubmissionAttemptKimura or
            GrappleAction.SubmissionAttemptHeelHook => ResolveSubmission(actor, opponent, action),
            GrappleAction.GuardPass or GrappleAction.KneeCutPass or GrappleAction.StackPass or GrappleAction.BackTake => ResolveGuardPass(actor, opponent, action),
            GrappleAction.Escape or GrappleAction.GetUp => ResolveEscape(actor, opponent),
            GrappleAction.Sweep or GrappleAction.ButterflySweep => ResolveSweep(actor, opponent, action),
            _ => new GrappleOutcome(false, actor.CurrentPosition, 0, false, $"{actor.Fighter.FullName} attempts a grappling move.")
        };
    }

    /// <summary>
    /// Resolves takedown attempts, including specialty- and physics-based leverage.
    /// </summary>
    private GrappleOutcome ResolveTakedown(FighterState actor, FighterState opponent, GrappleAction action)
    {
        var styleBonus = actor.Fighter.StyleFactor(FightingStyle.Wrestler) - 1.0;
        var takedownSpecialty = actor.Fighter.SpecialtyFactor(StyleSpecialty.WrestlingTakedowns);
        var defendSpecialty = opponent.Fighter.SpecialtyFactor(StyleSpecialty.WrestlingTakedownDefense);
        var actionSpecialty = action switch
        {
            GrappleAction.SingleLegTakedown => actor.Fighter.SpecialtyFactor(StyleSpecialty.WrestlingSingleLeg),
            GrappleAction.DoubleLegTakedown => actor.Fighter.SpecialtyFactor(StyleSpecialty.WrestlingDoubleLeg),
            GrappleAction.TripFromClinch or GrappleAction.OutsideTrip or GrappleAction.InsideTrip => Math.Max(actor.Fighter.SpecialtyFactor(StyleSpecialty.ClinchTrips), actor.Fighter.SpecialtyFactor(StyleSpecialty.JudoTripsThrows)),
            _ => 1.0
        };
        var attackTD = actor.Fighter.Grappling.TakedownAccuracy / 100.0 * (1 + styleBonus) * takedownSpecialty * actor.CurrentStamina;
        var defendTD = opponent.Fighter.Grappling.TakedownDefense / 100.0
            * (1 + opponent.Fighter.Athletics.Strength / 100.0 * 0.2)
            * defendSpecialty
            * PhysicalAdvantageModel.TopControlMultiplier(opponent, actor)
            * opponent.CurrentStamina;
        var physicalMultiplier = PhysicalAdvantageModel.TakedownSuccessMultiplier(actor, opponent);
        var successProb = Math.Clamp((attackTD * actionSpecialty * physicalMultiplier) / (defendTD + 0.01), 0.01, 0.92);

        if (_random.Chance(successProb))
        {
            var newPos = action switch
            {
                GrappleAction.TripFromClinch or GrappleAction.OutsideTrip or GrappleAction.InsideTrip => FightPosition.HalfGuardTop,
                GrappleAction.BodyLockTakedown => FightPosition.SideControlTop,
                _ => FightPosition.GroundAndPoundTop
            };
            actor.CurrentPosition = newPos;
            opponent.CurrentPosition = MirrorTopPosition(newPos);
            actor.GroundedTicksRemaining = 6;
            opponent.GroundedTicksRemaining = 6;
            var ev = CreateGrappleEvent(FightEventType.TakedownLanded, actor, opponent, action);
            return new GrappleOutcome(true, newPos, 0.5, false, _narration.BuildForGrapple(ev));
        }

        var ev2 = CreateGrappleEvent(FightEventType.TakedownDefended, actor, opponent, action);
        return new GrappleOutcome(false, actor.CurrentPosition, 0, false, _narration.BuildForGrapple(ev2));
    }

    /// <summary>
    /// Resolves submission attempts using positional, specialty, and escape factors.
    /// </summary>
    private GrappleOutcome ResolveSubmission(FighterState actor, FighterState opponent, GrappleAction action)
    {
        var posMultiplier = actor.CurrentPosition switch
        {
            FightPosition.BackControlAttacker => 1.5,
            FightPosition.MountTop => 1.3,
            FightPosition.SideControlTop => 1.1,
            FightPosition.FullGuardTop => 0.9,
            FightPosition.TurtleTop => 1.0,
            FightPosition.ButterflyGuardTop => 0.8,
            _ => 0.7
        };

        var finisherSpecialty = actor.Fighter.SpecialtyFactor(StyleSpecialty.BjjFinisher)
            * SubmissionActionFactor(actor, action)
            * SubmissionPositionFactor(actor.CurrentPosition, action);
        var controlSpecialty = actor.Fighter.SpecialtyFactor(StyleSpecialty.BjjControl);
        var scrambleSpecialty = opponent.Fighter.SpecialtyFactor(StyleSpecialty.BjjScrambles);
        var lockedProb = actor.Fighter.Grappling.SubmissionOffense / 100.0 * posMultiplier * finisherSpecialty * actor.CurrentStamina;
        var escapeProb = opponent.Fighter.Grappling.SubmissionDefense / 100.0
            * (1 - opponent.AccumulatedBodyDamage / 60.0)
            * scrambleSpecialty
            * PhysicalAdvantageModel.EscapeMultiplier(opponent, actor)
            * opponent.CurrentStamina;
        escapeProb = Math.Clamp(escapeProb, 0.05, 0.95);

        lockedProb *= PhysicalAdvantageModel.TopControlMultiplier(actor, opponent) * controlSpecialty;

        if (!_random.Chance(Math.Clamp(lockedProb, 0.01, 0.85)))
        {
            var attemptEv = CreateGrappleEvent(FightEventType.SubmissionAttempted, actor, opponent, action);
            return new GrappleOutcome(false, actor.CurrentPosition, 0, false, _narration.BuildForGrapple(attemptEv));
        }

        if (_random.Chance(1 - escapeProb))
        {
            var finishEv = CreateGrappleEvent(FightEventType.SubmissionLocked, actor, opponent, action);
            return new GrappleOutcome(true, actor.CurrentPosition, 0, true, _narration.BuildForGrapple(finishEv));
        }

        var escapeEv = CreateGrappleEvent(FightEventType.SubmissionEscaped, actor, opponent, action);
        return new GrappleOutcome(false, actor.CurrentPosition, 1.0, false, _narration.BuildForGrapple(escapeEv));
    }

    /// <summary>
    /// Resolves guard passing and back-take attempts from top position.
    /// </summary>
    private GrappleOutcome ResolveGuardPass(FighterState actor, FighterState opponent, GrappleAction action)
    {
        var passProb = actor.Fighter.Grappling.GroundControl / 100.0
            * 0.4
            * actor.Fighter.SpecialtyFactor(StyleSpecialty.BjjControl)
            * (action is GrappleAction.KneeCutPass or GrappleAction.StackPass ? actor.Fighter.SpecialtyFactor(StyleSpecialty.BjjGuardPassing) : 1.0)
            * actor.CurrentStamina
            * PhysicalAdvantageModel.TopControlMultiplier(actor, opponent);

        if (_random.Chance(passProb))
        {
            var newPos = action switch
            {
                GrappleAction.BackTake => FightPosition.BackControlAttacker,
                GrappleAction.KneeCutPass => FightPosition.HalfGuardTop,
                _ => FightPosition.SideControlTop
            };
            actor.CurrentPosition = newPos;
            opponent.CurrentPosition = MirrorTopPosition(newPos);
            var ev = CreateGrappleEvent(FightEventType.PositionChange, actor, opponent, action);
            return new GrappleOutcome(true, newPos, 0, false, _narration.BuildForGrapple(ev));
        }

        return new GrappleOutcome(false, actor.CurrentPosition, 0, false,
            $"{actor.Fighter.FullName} tries to pass the guard but {opponent.Fighter.FullName} maintains position.");
    }

    /// <summary>
    /// Resolves stand-up and scramble attempts from inferior positions.
    /// </summary>
    private GrappleOutcome ResolveEscape(FighterState actor, FighterState opponent)
    {
        var escapePenalty = actor.CurrentPosition == FightPosition.BackControlDefender ? 0.5 : 1.0;
        var escapeProb = actor.Fighter.Grappling.SubmissionDefense / 100.0 * actor.CurrentStamina
            * actor.Fighter.Athletics.Agility / 100.0
            * actor.Fighter.SpecialtyFactor(StyleSpecialty.BjjScrambles)
            * escapePenalty;
        escapeProb *= PhysicalAdvantageModel.EscapeMultiplier(actor, opponent);

        if (_random.Chance(Math.Clamp(escapeProb, 0.02, 0.7)))
        {
            actor.CurrentPosition = FightPosition.Standing;
            opponent.CurrentPosition = FightPosition.Standing;
            return new GrappleOutcome(true, FightPosition.Standing, 0, false,
                $"{actor.Fighter.FullName} works back to the feet!");
        }

        return new GrappleOutcome(false, actor.CurrentPosition, 0, false,
            $"{actor.Fighter.FullName} attempts to escape but {opponent.Fighter.FullName} maintains control.");
    }

    /// <summary>
    /// Resolves sweep attempts from bottom positions.
    /// </summary>
    private GrappleOutcome ResolveSweep(FighterState actor, FighterState opponent, GrappleAction action)
    {
        var sweepProb = actor.Fighter.Grappling.SubmissionOffense / 100.0
            * 0.5
            * actor.Fighter.SpecialtyFactor(StyleSpecialty.BjjScrambles)
            * actor.CurrentStamina
            * PhysicalAdvantageModel.TopControlMultiplier(actor, opponent);

        if (_random.Chance(sweepProb))
        {
            var newPos = action == GrappleAction.ButterflySweep ? FightPosition.HalfGuardTop : FightPosition.MountTop;
            actor.CurrentPosition = newPos;
            opponent.CurrentPosition = MirrorTopPosition(newPos);
            return new GrappleOutcome(true, newPos, 0, false,
                $"{actor.Fighter.FullName} sweeps {opponent.Fighter.FullName} and takes top position!");
        }

        return new GrappleOutcome(false, actor.CurrentPosition, 0, false,
            $"{actor.Fighter.FullName} attempts a sweep but {opponent.Fighter.FullName} bases out.");
    }

    /// <summary>
    /// Returns the specialty multiplier for the requested submission type.
    /// </summary>
    private static double SubmissionActionFactor(FighterState actor, GrappleAction action) => action switch
    {
        GrappleAction.SubmissionAttemptRearNakedChoke => actor.Fighter.SpecialtyFactor(StyleSpecialty.RearNakedChoke),
        GrappleAction.SubmissionAttemptGuillotine => actor.Fighter.SpecialtyFactor(StyleSpecialty.GuillotineChoke),
        GrappleAction.SubmissionAttemptDarce => actor.Fighter.SpecialtyFactor(StyleSpecialty.DarceChoke),
        GrappleAction.SubmissionAttemptAnaconda => actor.Fighter.SpecialtyFactor(StyleSpecialty.AnacondaChoke),
        GrappleAction.SubmissionAttemptArmTriangle => actor.Fighter.SpecialtyFactor(StyleSpecialty.ArmTriangleChoke),
        GrappleAction.SubmissionAttemptTriangle => actor.Fighter.SpecialtyFactor(StyleSpecialty.TriangleChoke),
        GrappleAction.SubmissionAttemptArmbar => actor.Fighter.SpecialtyFactor(StyleSpecialty.Armbar),
        GrappleAction.SubmissionAttemptKimura => actor.Fighter.SpecialtyFactor(StyleSpecialty.Kimura),
        GrappleAction.SubmissionAttemptHeelHook => actor.Fighter.SpecialtyFactor(StyleSpecialty.HeelHook),
        _ => 1.0
    };

    /// <summary>
    /// Returns how suitable the current position is for the requested submission.
    /// </summary>
    private static double SubmissionPositionFactor(FightPosition position, GrappleAction action) => action switch
    {
        GrappleAction.SubmissionAttemptRearNakedChoke => position == FightPosition.BackControlAttacker ? 1.5 : 0.45,
        GrappleAction.SubmissionAttemptArmTriangle => position is FightPosition.SideControlTop or FightPosition.MountTop ? 1.25 : 0.65,
        GrappleAction.SubmissionAttemptDarce or GrappleAction.SubmissionAttemptAnaconda => position is FightPosition.TurtleTop or FightPosition.SideControlTop ? 1.25 : 0.70,
        GrappleAction.SubmissionAttemptTriangle or GrappleAction.SubmissionAttemptArmbar or GrappleAction.SubmissionAttemptKimura => position is FightPosition.FullGuardTop or FightPosition.ButterflyGuardTop ? 1.10 : 0.80,
        GrappleAction.SubmissionAttemptHeelHook => position is FightPosition.HalfGuardTop or FightPosition.ButterflyGuardTop ? 1.10 : 0.75,
        _ => 1.0
    };

    /// <summary>
    /// Maps a top-position enum to its mirrored defender position.
    /// </summary>
    private static FightPosition MirrorTopPosition(FightPosition topPosition) => topPosition switch
    {
        FightPosition.GroundAndPoundTop => FightPosition.GroundAndPoundBottom,
        FightPosition.TurtleTop => FightPosition.TurtleBottom,
        FightPosition.FullGuardTop => FightPosition.FullGuardBottom,
        FightPosition.ButterflyGuardTop => FightPosition.ButterflyGuardBottom,
        FightPosition.HalfGuardTop => FightPosition.HalfGuardBottom,
        FightPosition.MountTop => FightPosition.MountBottom,
        FightPosition.BackControlAttacker => FightPosition.BackControlDefender,
        FightPosition.SideControlTop => FightPosition.SideControlBottom,
        _ => FightPosition.GroundAndPoundBottom
    };

    /// <summary>
    /// Creates a lightweight fight event for grappling narration and bookkeeping.
    /// </summary>
    private static FightEvent CreateGrappleEvent(FightEventType type, FighterState actor, FighterState opponent, GrappleAction? action = null) =>
        new()
        {
            Timestamp = TimeSpan.Zero,
            Type = type,
            Actor = actor.Fighter,
            Target = opponent.Fighter,
            PositionBefore = actor.CurrentPosition,
            PositionAfter = actor.CurrentPosition,
            GrappleAction = action,
            IsSignificant = type is FightEventType.TakedownLanded or FightEventType.SubmissionLocked or FightEventType.KnockdownScored
        };
}
