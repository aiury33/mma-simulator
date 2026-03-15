using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Interfaces;
using MmaSimulator.Core.Models;
using MmaSimulator.Simulation.Physics;
using MmaSimulator.Simulation.Styles;

namespace MmaSimulator.Simulation.Simulators;

/// <summary>
/// Simulates a single round via a 300-tick loop (1 tick ≈ 1 second of fight time).
///
/// <para><b>Calibration targets (per 5-round fight):</b>
/// <list type="table">
///   <item><term>Elite striker (LW)</term><description>~100–130 sig strikes, 0–1 TDs, 0 sub attempts</description></item>
///   <item><term>Elite wrestler</term><description>~40–80 sig strikes, 6–12 TDs, 0–2 sub attempts</description></item>
///   <item><term>BJJ specialist</term><description>~50–90 sig strikes, 3–8 TDs, 3–8 sub attempts</description></item>
/// </list>
/// </para>
///
/// <para>Action probabilities are deliberately low because most ticks represent
/// movement, circling, feinting, and recovery between exchanges. Only meaningful
/// actions produce <see cref="FightEvent"/> entries in the round log.</para>
///
/// <para><b>Finish mechanisms:</b>
/// KO/stun probability grows exponentially with accumulated head damage (see
/// <see cref="StrikingEngine"/>). TKO after a knockdown uses a style-aware
/// finisher multiplier. Submissions use a multi-tick lock process.</para>
/// </summary>
public sealed class RoundSimulator : IRoundSimulator
{
    private const int TicksPerRound = 300;

    // ── Standing base probabilities (per actor-tick) ──────────────────────
    // ~17 % strike chance → ~25 sig strikes/round for an elite striker standing
    private const double StandingStrikeProb = 0.17;
    private const double StandingClinchProb = 0.012;

    // ── Ground probabilities (per actor-tick) ─────────────────────────────
    // Calibrated to real GnP / submission frequencies
    private const double GnpTopStrikeProb     = 0.10;
    private const double GnpTopPassProb        = 0.008;
    private const double GnpTopSubProb         = 0.003;

    private const double FullGuardTopStrikeProb = 0.045;
    private const double FullGuardTopPassProb   = 0.012;
    private const double FullGuardTopSubProb    = 0.002;

    private const double MountTopStrikeProb     = 0.09;
    private const double MountTopSubProb        = 0.010;

    private const double BackControlStrikeProb  = 0.04;
    private const double BackControlSubProb     = 0.018;

    private const double SideControlTopStrikeProb = 0.07;
    private const double SideControlTopPassProb   = 0.010;

    // ── Escape base probabilities ─────────────────────────────────────────
    // ~1 % per tick → average ~100 sec (≈ 1.5–2 min) on the ground — realistic
    private const double BottomEscapeBaseProb    = 0.010;
    private const double BackControlEscapeProb   = 0.004;
    private const double MaxStandingActionShare  = 0.46;

    // ── Strike pools ──────────────────────────────────────────────────────
    private static readonly StrikeType[] StandingStrikes =
    [
        StrikeType.Jab, StrikeType.Cross, StrikeType.Cross,
        StrikeType.Hook, StrikeType.Hook, StrikeType.Uppercut,
        StrikeType.Overhand, StrikeType.LowKick, StrikeType.BodyKick,
        StrikeType.Roundhouse, StrikeType.HeadKick,
        StrikeType.BodyShot, StrikeType.FrontKick, StrikeType.SpinningBackKick
    ];

    private static readonly StrikeType[] BoxingStandingStrikes =
    [
        StrikeType.Jab, StrikeType.Jab, StrikeType.Cross, StrikeType.Cross,
        StrikeType.Hook, StrikeType.Hook, StrikeType.Uppercut,
        StrikeType.Overhand, StrikeType.BodyShot, StrikeType.LowKick
    ];

    private static readonly StrikeType[] KickboxingStandingStrikes =
    [
        StrikeType.Jab, StrikeType.Jab, StrikeType.Cross,
        StrikeType.Hook, StrikeType.BodyKick,
        StrikeType.Roundhouse, StrikeType.CalfKick, StrikeType.BodyShot, StrikeType.Teep, StrikeType.Teep
    ];

    private static readonly StrikeType[] MuayThaiStandingStrikes =
    [
        StrikeType.Jab, StrikeType.Cross, StrikeType.Hook,
        StrikeType.KneeBody, StrikeType.BodyKick, StrikeType.Roundhouse,
        StrikeType.CalfKick, StrikeType.BodyShot, StrikeType.Teep
    ];

    private static readonly StrikeType[] GrapplerStandingStrikes =
    [
        StrikeType.Jab, StrikeType.Cross, StrikeType.Hook, StrikeType.Hook,
        StrikeType.Overhand, StrikeType.BodyShot, StrikeType.LowKick, StrikeType.FrontKick
    ];

    private static readonly StrikeType[] ClinchStrikes =
    [
        StrikeType.Hook, StrikeType.Uppercut, StrikeType.ElbowHorizontal,
        StrikeType.KneeBody, StrikeType.BodyShot
    ];

    private static readonly StrikeType[] MuayThaiClinchStrikes =
    [
        StrikeType.ElbowHorizontal, StrikeType.ElbowUpward, StrikeType.KneeBody, StrikeType.KneeHead, StrikeType.BodyShot
    ];

    private static readonly StrikeType[] BoxingClinchStrikes =
    [
        StrikeType.Hook, StrikeType.Hook, StrikeType.Uppercut, StrikeType.BodyShot
    ];

    private static readonly StrikeType[] GroundStrikes =
    [
        StrikeType.GroundPunch, StrikeType.GroundPunch, StrikeType.Hammerfist,
        StrikeType.GroundElbow, StrikeType.GroundElbow
    ];

    private static readonly GrappleAction[] GroundSubmissions =
    [
        GrappleAction.SubmissionAttemptRearNakedChoke,
        GrappleAction.SubmissionAttemptGuillotine,
        GrappleAction.SubmissionAttemptDarce,
        GrappleAction.SubmissionAttemptAnaconda,
        GrappleAction.SubmissionAttemptArmTriangle,
        GrappleAction.SubmissionAttemptTriangle,
        GrappleAction.SubmissionAttemptArmbar,
        GrappleAction.SubmissionAttemptKimura,
        GrappleAction.SubmissionAttemptHeelHook
    ];

    private static readonly GrappleAction[] TakedownActions =
    [
        GrappleAction.SingleLegTakedown,
        GrappleAction.DoubleLegTakedown,
        GrappleAction.BodyLockTakedown
    ];

    private static readonly GrappleAction[] ClinchTakedownActions =
    [
        GrappleAction.TripFromClinch,
        GrappleAction.OutsideTrip,
        GrappleAction.InsideTrip,
        GrappleAction.BodyLockTakedown
    ];

    private static readonly GrappleAction[] GuardPassActions =
    [
        GrappleAction.GuardPass,
        GrappleAction.KneeCutPass,
        GrappleAction.StackPass
    ];

    private readonly IStrikingEngine _striking;
    private readonly IGrapplingEngine _grappling;
    private readonly IStaminaEngine _stamina;
    private readonly IJudgeScoringEngine _judgeScoring;
    private readonly IRandomProvider _random;

    /// <summary>
    /// Creates the round simulator with striking, grappling, stamina, scoring, and randomness services.
    /// </summary>
    public RoundSimulator(
        IStrikingEngine striking,
        IGrapplingEngine grappling,
        IStaminaEngine stamina,
        IJudgeScoringEngine judgeScoring,
        IRandomProvider random)
    {
        _striking     = striking;
        _grappling    = grappling;
        _stamina      = stamina;
        _judgeScoring = judgeScoring;
        _random       = random;
    }

    /// <summary>
    /// Simulates one round, producing the event log, stats, and scorecards when the fight reaches the horn.
    /// </summary>
    public Round SimulateRound(int roundNumber, FighterState stateA, FighterState stateB, SimulationOptions options)
    {
        var events = new List<FightEvent>();
        var statsA = new RoundStatsBuilder();
        var statsB = new RoundStatsBuilder();

        events.Add(CreateMarker(FightEventType.RoundStart, stateA, TimeSpan.Zero));

        FightEvent? finishEvent = null;

        for (var tick = 0; tick < TicksPerRound; tick++)
        {
            UpdateRecoveryState(stateA);
            UpdateRecoveryState(stateB);

            var timestamp  = TimeSpan.FromSeconds(tick);
            var actorIsA   = _random.Chance(PhysicalAdvantageModel.InitiativeShare(stateA, stateB));
            var actor      = actorIsA ? stateA : stateB;
            var opponent   = actorIsA ? stateB : stateA;
            var actorStats    = actorIsA ? statsA : statsB;
            var opponentStats = actorIsA ? statsB : statsA;

            var tickEvent = ResolveAction(actor, opponent, actorStats, opponentStats, timestamp);
            if (tickEvent == null) continue;

            events.Add(tickEvent);
            actor.CurrentStamina = Math.Max(0.05, actor.CurrentStamina - _stamina.CalculateStaminaDrain(actor, tickEvent));

            if (tickEvent.Type == FightEventType.SubmissionLocked)
            {
                finishEvent = tickEvent;
                break;
            }

            if (CheckTkoFromGroundDamage(actor, opponent, tickEvent))
            {
                var groundTkoEvent = new FightEvent
                {
                    Timestamp = timestamp,
                    Type = FightEventType.FightEnded,
                    Actor = actor.Fighter,
                    Target = opponent.Fighter,
                    PositionBefore = actor.CurrentPosition,
                    PositionAfter = actor.CurrentPosition,
                    IsSignificant = true,
                    NarrationText = $"The referee jumps in after unanswered ground strikes! {actor.Fighter.FullName} wins by TKO!"
                };
                events.Add(groundTkoEvent);
                finishEvent = groundTkoEvent;
                break;
            }

            if (tickEvent.Type == FightEventType.KnockdownScored && CheckTkoAfterKnockdown(actor, opponent))
            {
                var tkoEvent = new FightEvent
                {
                    Timestamp     = timestamp,
                    Type          = FightEventType.FightEnded,
                    Actor         = actor.Fighter,
                    Target        = opponent.Fighter,
                    PositionBefore = actor.CurrentPosition,
                    PositionAfter  = actor.CurrentPosition,
                    IsSignificant  = true,
                    NarrationText  = $"The referee stops the fight! {actor.Fighter.FullName} wins by TKO!"
                };
                events.Add(tkoEvent);
                finishEvent = tkoEvent;
                break;
            }
        }

        events.Add(CreateMarker(
            finishEvent == null ? FightEventType.RoundEnd : FightEventType.FightEnded,
            stateA, TimeSpan.FromSeconds(TicksPerRound)));

        var roundStatsA = statsA.Build(stateA.CurrentStamina);
        var roundStatsB = statsB.Build(stateB.CurrentStamina);
        var tempRound   = BuildRound(roundNumber, events, roundStatsA, roundStatsB);

        var scorecards = finishEvent == null
            ? new[]
            {
                _judgeScoring.ScoreRound(1, tempRound, stateA.Fighter, stateB.Fighter),
                _judgeScoring.ScoreRound(2, tempRound, stateA.Fighter, stateB.Fighter),
                _judgeScoring.ScoreRound(3, tempRound, stateA.Fighter, stateB.Fighter)
            }
            : Array.Empty<JudgeScorecard>();

        return new Round
        {
            Number = roundNumber,
            Events = events,
            FighterAStats = roundStatsA,
            FighterBStats = roundStatsB,
            Scorecards = scorecards
        };
    }

    // ── Position dispatch ──────────────────────────────────────────────────

    /// <summary>
    /// Dispatches the actor's tick to the appropriate resolver for the current position.
    /// </summary>
    private FightEvent? ResolveAction(
        FighterState actor, FighterState opponent,
        RoundStatsBuilder actorStats, RoundStatsBuilder opponentStats,
        TimeSpan timestamp) => actor.CurrentPosition switch
    {
        FightPosition.Standing  => ResolveStanding(actor, opponent, actorStats, opponentStats, timestamp),
        FightPosition.Clinch    => ResolveClinch(actor, opponent, actorStats, opponentStats, timestamp),

        FightPosition.GroundAndPoundTop or
        FightPosition.TurtleTop or
        FightPosition.SideControlTop    => ResolveTopGround(actor, opponent, actorStats, opponentStats, timestamp, actor.CurrentPosition),

        FightPosition.FullGuardTop or
        FightPosition.ButterflyGuardTop or
        FightPosition.HalfGuardTop      => ResolveGuardTop(actor, opponent, actorStats, opponentStats, timestamp),

        FightPosition.MountTop          => ResolveMountTop(actor, opponent, actorStats, opponentStats, timestamp),
        FightPosition.BackControlAttacker => ResolveBackControl(actor, opponent, actorStats, opponentStats, timestamp),

        FightPosition.GroundAndPoundBottom or
        FightPosition.TurtleBottom or
        FightPosition.SideControlBottom or
        FightPosition.FullGuardBottom or
        FightPosition.ButterflyGuardBottom or
        FightPosition.HalfGuardBottom   => ResolveBottomEscape(actor, opponent, BottomEscapeBaseProb, timestamp),

        FightPosition.MountBottom       => ResolveBottomEscape(actor, opponent, BottomEscapeBaseProb * 0.8, timestamp),
        FightPosition.BackControlDefender => ResolveBottomEscape(actor, opponent, BackControlEscapeProb, timestamp),

        _ => null
    };

    // ── Standing & Clinch ─────────────────────────────────────────────────

    /// <summary>
    /// Resolves a standing tick by choosing between striking, takedown entries, and clinch engagement.
    /// </summary>
    private FightEvent? ResolveStanding(
        FighterState actor, FighterState opponent,
        RoundStatsBuilder actorStats, RoundStatsBuilder opponentStats,
        TimeSpan timestamp)
    {
        var roll = _random.NextDouble();
        var strategy = EvaluateStandingStrategy(actor, opponent);
        var tdProb = GetTakedownProbability(actor, opponent) * strategy.TakedownMultiplier;
        var strikeProb = GetStandingStrikeProbability(actor, opponent) * strategy.StrikeMultiplier;
        var clinchProb = GetClinchProbability(actor, opponent) * strategy.ClinchMultiplier;

        ScaleStandingProbabilities(ref strikeProb, ref tdProb, ref clinchProb);

        if (roll < strikeProb)
            return ExecuteStrike(actor, opponent, actorStats, opponentStats, GetStandingStrikePool(actor), timestamp);

        if (roll < strikeProb + tdProb)
            return ExecuteTakedown(actor, opponent, actorStats, timestamp, ChooseTakedownAction(actor, standing: true));

        if (roll < strikeProb + tdProb + clinchProb)
        {
            actor.CurrentPosition    = FightPosition.Clinch;
            opponent.CurrentPosition = FightPosition.Clinch;
        }

        return null;
    }

    /// <summary>
    /// Resolves a clinch tick by choosing between short strikes, trips, and disengagement.
    /// </summary>
    private FightEvent? ResolveClinch(
        FighterState actor, FighterState opponent,
        RoundStatsBuilder actorStats, RoundStatsBuilder opponentStats,
        TimeSpan timestamp)
    {
        var roll = _random.NextDouble();
        var strategy = EvaluateStandingStrategy(actor, opponent);
        var clinchStrikeProb = GetClinchStrikeProbability(actor, opponent) * Math.Clamp(strategy.StrikeMultiplier, 0.70, 1.20);
        var clinchTakedownProb = GetTakedownProbability(actor, opponent)
            * 2.2
            * PhysicalAdvantageModel.ClinchEntryMultiplier(actor, opponent)
            * Math.Clamp(strategy.TakedownMultiplier, 0.80, 1.80);

        if (roll < clinchStrikeProb)
            return ExecuteStrike(actor, opponent, actorStats, opponentStats, GetClinchStrikePool(actor), timestamp);

        if (roll < clinchStrikeProb + clinchTakedownProb)
            return ExecuteTakedown(actor, opponent, actorStats, timestamp, ChooseTakedownAction(actor, standing: false));

        if (roll < clinchStrikeProb + clinchTakedownProb + 0.10)
        {
            actor.CurrentPosition    = FightPosition.Standing;
            opponent.CurrentPosition = FightPosition.Standing;
        }

        return null;
    }

    // ── Ground top positions ──────────────────────────────────────────────

    /// <summary>
    /// Resolves top-position ground ticks for side control, turtle, and generic ground-and-pound.
    /// </summary>
    private FightEvent? ResolveTopGround(
        FighterState actor, FighterState opponent,
        RoundStatsBuilder actorStats, RoundStatsBuilder opponentStats, TimeSpan timestamp, FightPosition pos)
    {
        var (sProb, pProb) = pos == FightPosition.GroundAndPoundTop
            ? (GnpTopStrikeProb, GnpTopPassProb)
            : (SideControlTopStrikeProb, SideControlTopPassProb);

        var roll = _random.NextDouble();
        if (roll < sProb)
            return ExecuteGroundStrike(actor, opponent, actorStats, opponentStats, timestamp);
        if (roll < sProb + pProb)
            return TryGuardPass(actor, opponent, timestamp);
        if (roll < sProb + pProb + GnpTopSubProb)
            return TrySubmission(actor, opponent, actorStats, timestamp);
        return null;
    }

    /// <summary>
    /// Resolves top-position ticks from guard and half-guard.
    /// </summary>
    private FightEvent? ResolveGuardTop(
        FighterState actor, FighterState opponent,
        RoundStatsBuilder actorStats, RoundStatsBuilder opponentStats, TimeSpan timestamp)
    {
        var roll = _random.NextDouble();
        if (roll < FullGuardTopStrikeProb)
            return ExecuteGroundStrike(actor, opponent, actorStats, opponentStats, timestamp);
        if (roll < FullGuardTopStrikeProb + FullGuardTopPassProb)
            return TryGuardPass(actor, opponent, timestamp);
        if (roll < FullGuardTopStrikeProb + FullGuardTopPassProb + FullGuardTopSubProb)
            return TrySubmission(actor, opponent, actorStats, timestamp);
        return null;
    }

    /// <summary>
    /// Resolves mount ticks, favoring damage and submission pressure.
    /// </summary>
    private FightEvent? ResolveMountTop(
        FighterState actor, FighterState opponent,
        RoundStatsBuilder actorStats, RoundStatsBuilder opponentStats, TimeSpan timestamp)
    {
        var roll = _random.NextDouble();
        if (roll < MountTopStrikeProb)
            return ExecuteGroundStrike(actor, opponent, actorStats, opponentStats, timestamp);
        if (roll < MountTopStrikeProb + MountTopSubProb * GetSubAttemptMultiplier(actor))
            return TrySubmission(actor, opponent, actorStats, timestamp);
        return null;
    }

    /// <summary>
    /// Resolves back-control ticks, favoring submissions over strikes.
    /// </summary>
    private FightEvent? ResolveBackControl(
        FighterState actor, FighterState opponent,
        RoundStatsBuilder actorStats, RoundStatsBuilder opponentStats, TimeSpan timestamp)
    {
        var roll = _random.NextDouble();
        if (roll < BackControlSubProb * GetSubAttemptMultiplier(actor))
            return TrySubmission(actor, opponent, actorStats, timestamp);
        if (roll < BackControlSubProb + BackControlStrikeProb)
            return ExecuteGroundStrike(actor, opponent, actorStats, opponentStats, timestamp);
        return null;
    }

    /// <summary>
    /// Resolves escape attempts from bottom positions back to standing.
    /// </summary>
    private FightEvent? ResolveBottomEscape(
        FighterState actor, FighterState opponent,
        double baseProb, TimeSpan timestamp)
    {
        var escapeProb = baseProb
            * (actor.Fighter.Grappling.TakedownDefense / 70.0)
            * PhysicalAdvantageModel.EscapeMultiplier(actor, opponent)
            * actor.CurrentStamina;

        if (actor.GroundedTicksRemaining > 0 || opponent.GroundedTicksRemaining > 0)
            escapeProb *= 0.10;

        if (!_random.Chance(Math.Clamp(escapeProb, 0.001, 0.05))) return null;

        var prevPos = actor.CurrentPosition;
        actor.CurrentPosition    = FightPosition.Standing;
        opponent.CurrentPosition = FightPosition.Standing;

        return new FightEvent
        {
            Timestamp      = timestamp,
            Type           = FightEventType.PositionChange,
            Actor          = actor.Fighter,
            Target         = opponent.Fighter,
            PositionBefore = prevPos,
            PositionAfter  = FightPosition.Standing,
            IsSignificant  = false,
            NarrationText  = $"{actor.Fighter.FullName} works back to the feet!"
        };
    }

    // ── Atomic executors ──────────────────────────────────────────────────

    /// <summary>
    /// Executes a standing or clinch strike and records the resulting event and stats.
    /// </summary>
    private FightEvent? ExecuteStrike(
        FighterState actor, FighterState opponent,
        RoundStatsBuilder actorStats, RoundStatsBuilder opponentStats,
        IReadOnlyList<StrikeType> pool, TimeSpan timestamp)
    {
        var strikeType = _random.Choose(pool);
        var outcome    = _striking.ResolveStrike(actor, opponent, strikeType);

        if (!outcome.Landed) return null; // missed — skip narration for cleanliness

        var eventType = outcome.CausedKnockdown ? FightEventType.KnockdownScored
            : outcome.Blocked ? FightEventType.StrikeBlocked
            : FightEventType.StrikeLanded;

        var isSignificant = eventType is FightEventType.StrikeLanded or FightEventType.KnockdownScored;

        actorStats.TotalStrikesLanded++;
        if (isSignificant) actorStats.SignificantStrikesLanded++;
        actorStats.SignificantStrikesAttempted++;
        opponentStats.DamageTaken += outcome.DamageDealt;
        if (outcome.CausedKnockdown)
        {
            actorStats.KnockdownsScored++;
            opponent.KnockdownsThisFight++;
        }

        return new FightEvent
        {
            Timestamp      = timestamp,
            Type           = eventType,
            Actor          = actor.Fighter,
            Target         = opponent.Fighter,
            PositionBefore = actor.CurrentPosition,
            PositionAfter  = actor.CurrentPosition,
            StrikeType     = strikeType,
            DamageDealt    = outcome.DamageDealt,
            IsSignificant  = isSignificant,
            NarrationText  = outcome.NarrationText
        };
    }

    /// <summary>
    /// Executes a ground strike using the ground-specific strike pool.
    /// </summary>
    private FightEvent? ExecuteGroundStrike(
        FighterState actor, FighterState opponent,
        RoundStatsBuilder actorStats, RoundStatsBuilder opponentStats, TimeSpan timestamp)
    {
        return ExecuteStrike(actor, opponent, actorStats, opponentStats, GetGroundStrikePool(actor), timestamp);
    }

    /// <summary>
    /// Executes a takedown attempt and records the resulting event.
    /// </summary>
    private FightEvent? ExecuteTakedown(
        FighterState actor, FighterState opponent,
        RoundStatsBuilder actorStats, TimeSpan timestamp, GrappleAction action)
    {
        var outcome = _grappling.ResolveGrappleAction(actor, opponent, action);
        actorStats.TakedownsAttempted++;
        if (outcome.Succeeded) actorStats.TakedownsLanded++;

        return new FightEvent
        {
            Timestamp      = timestamp,
            Type           = outcome.Succeeded ? FightEventType.TakedownLanded : FightEventType.TakedownDefended,
            Actor          = actor.Fighter,
            Target         = opponent.Fighter,
            PositionBefore = actor.CurrentPosition,
            PositionAfter  = outcome.NewPosition,
            GrappleAction  = action,
            IsSignificant  = outcome.Succeeded,
            NarrationText  = outcome.NarrationText
        };
    }

    /// <summary>
    /// Attempts a guard pass or back take from top position.
    /// </summary>
    private FightEvent? TryGuardPass(FighterState actor, FighterState opponent, TimeSpan timestamp)
    {
        var action = ChooseGuardPassAction(actor);
        var outcome = _grappling.ResolveGrappleAction(actor, opponent, action);
        if (!outcome.Succeeded) return null;

        return new FightEvent
        {
            Timestamp      = timestamp,
            Type           = FightEventType.PositionChange,
            Actor          = actor.Fighter,
            Target         = opponent.Fighter,
            PositionBefore = actor.CurrentPosition,
            PositionAfter  = outcome.NewPosition,
            GrappleAction  = action,
            IsSignificant  = true,
            NarrationText  = outcome.NarrationText
        };
    }

    /// <summary>
    /// Attempts a submission from the current grappling position.
    /// </summary>
    private FightEvent? TrySubmission(
        FighterState actor, FighterState opponent,
        RoundStatsBuilder actorStats, TimeSpan timestamp)
    {
        var action  = ChooseSubmissionAction(actor);
        var outcome = _grappling.ResolveGrappleAction(actor, opponent, action);

        var eventType = outcome.SubmissionFinish ? FightEventType.SubmissionLocked
            : outcome.Succeeded                  ? FightEventType.SubmissionAttempted
            :                                      FightEventType.SubmissionEscaped;

        if (eventType is FightEventType.SubmissionAttempted or FightEventType.SubmissionLocked)
            actorStats.SubmissionAttempts++;

        return new FightEvent
        {
            Timestamp      = timestamp,
            Type           = eventType,
            Actor          = actor.Fighter,
            Target         = opponent.Fighter,
            PositionBefore = actor.CurrentPosition,
            PositionAfter  = outcome.NewPosition,
            GrappleAction  = action,
            DamageDealt    = outcome.DamageDealt,
            IsSignificant  = outcome.SubmissionFinish,
            NarrationText  = outcome.NarrationText
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Takedown attempt probability per actor-tick when standing.
    /// Elite wrestlers (Khabib/Islam) → ~6–8 attempts/round
    /// Pure strikers (Adesanya/Topuria) → ~0–1 attempts/round
    /// </summary>
    private static double GetTakedownProbability(FighterState actor, FighterState opponent)
    {
        var baseProb = actor.Fighter.PrimaryStyle switch
        {
            FightingStyle.Wrestler        => 0.022,
            FightingStyle.Judoka          => 0.018,
            FightingStyle.MMAFighter      => 0.012,
            FightingStyle.BJJPractitioner => 0.010,
            FightingStyle.MuayThai        => 0.005,
            FightingStyle.Kickboxer       => 0.004,
            FightingStyle.Boxer           => 0.003,
            FightingStyle.Striker         => 0.003,
            _                            => 0.008
        };

        return baseProb
            * (actor.Fighter.Grappling.TakedownAccuracy / 70.0)
            * actor.Fighter.StyleFactor(FightingStyle.Wrestler)
            * actor.Fighter.SpecialtyFactor(StyleSpecialty.WrestlingTakedowns)
            * actor.CurrentStamina
            * PhysicalAdvantageModel.TakedownSuccessMultiplier(actor, opponent);
    }

    /// <summary>
    /// Multiplier for submission attempt probability from dominant positions.
    /// BJJ specialists go for subs relentlessly; pure strikers rarely bother.
    /// </summary>
    private static double GetSubAttemptMultiplier(FighterState actor)
    {
        var styleMod = actor.Fighter.StyleFactor(FightingStyle.BJJPractitioner) switch
        {
            >= 1.3 => 3.5,
            >= 1.0 => 2.4,
            _ when actor.Fighter.StyleFactor(FightingStyle.Judoka) >= 1.0 => 2.0,
            _ when actor.Fighter.StyleFactor(FightingStyle.Wrestler) >= 1.0 => 1.4,
            _ when actor.Fighter.StyleFactor(FightingStyle.MMAFighter) >= 1.0 => 1.2,
            _ => 0.5
        };
        return Math.Clamp(
            styleMod
            * actor.Fighter.SpecialtyFactor(StyleSpecialty.BjjFinisher)
            * (actor.Fighter.Grappling.SubmissionOffense / 70.0),
            0.3,
            5.5);
    }

    /// <summary>
    /// Determines whether the referee stops the fight immediately after a knockdown (TKO).
    ///
    /// <para>Factors that raise TKO probability:
    /// <list type="bullet">
    ///   <item>Each additional knockdown in the same fight significantly increases stoppage odds.</item>
    ///   <item>Finisher-style attackers (Kickboxer, Striker, MuayThai) get a 1.5× multiplier
    ///       because they follow up with aggressive ground-and-pound.</item>
    ///   <item>Low toughness and drained stamina both reduce the fighter's ability to recover.</item>
    /// </list>
    /// </para>
    ///
    /// <para>Calibration targets (first knockdown):
    /// ~20–35% TKO for a finisher vs an average chin; ~10–15% vs a very tough fighter.</para>
    /// </summary>
    private bool CheckTkoAfterKnockdown(FighterState attacker, FighterState downed)
    {
        var toughness     = downed.Fighter.Athletics.Toughness / 100.0;
        var staminaFactor = Math.Max(0.3, downed.CurrentStamina);

        // Finisher multiplier: aggressive strikers/kickboxers follow up ruthlessly
        var finisherMult = attacker.Fighter.DominantStrikingStyle() switch
        {
            FightingStyle.Kickboxer or FightingStyle.Striker or FightingStyle.MuayThai => 1.5,
            FightingStyle.Boxer or FightingStyle.MMAFighter => 1.2,
            _ => 1.0
        };

        var tkoProb = finisherMult
            * (0.25 + downed.KnockdownsThisFight * 0.35)
            * PhysicalAdvantageModel.KnockdownThreatMultiplier(attacker, downed)
            * (1.0 - toughness * 0.65)
            * (1.0 - staminaFactor * 0.3);

        return _random.Chance(Math.Clamp(tkoProb, 0.01, 0.98));
    }

    /// <summary>
    /// Determines whether sustained dominant ground-and-pound causes an immediate referee stoppage.
    /// </summary>
    private bool CheckTkoFromGroundDamage(FighterState attacker, FighterState defender, FightEvent tickEvent)
    {
        if (tickEvent.Type is not FightEventType.StrikeLanded and not FightEventType.KnockdownScored)
            return false;

        if (tickEvent.StrikeType is not (StrikeType.GroundPunch or StrikeType.GroundElbow or StrikeType.Hammerfist))
            return false;

        if (!IsDominantGroundPosition(attacker.CurrentPosition))
            return false;

        var toughness = defender.Fighter.Athletics.Toughness / 100.0;
        var stamina = Math.Max(0.2, defender.CurrentStamina);
        var headDamage = defender.AccumulatedHeadDamage;
        var bodyDamage = defender.AccumulatedBodyDamage;
        var strikeDamage = tickEvent.DamageDealt;
        var specialty = tickEvent.StrikeType == StrikeType.GroundElbow
            ? attacker.Fighter.SpecialtyFactor(StyleSpecialty.GroundAndPoundElbows)
            : attacker.Fighter.SpecialtyFactor(StyleSpecialty.GroundAndPoundPunches);
        var positionMultiplier = attacker.CurrentPosition switch
        {
            FightPosition.MountTop => 1.35,
            FightPosition.BackControlAttacker => 1.18,
            FightPosition.SideControlTop or FightPosition.TurtleTop => 1.22,
            _ => 1.10
        };
        var elbowMultiplier = tickEvent.StrikeType == StrikeType.GroundElbow ? 1.28 : 1.0;
        var damagePressure = (headDamage / 22.0) + (bodyDamage / 55.0) + (strikeDamage / 7.0);

        var tkoProb =
            damagePressure *
            positionMultiplier *
            elbowMultiplier *
            specialty *
            PhysicalAdvantageModel.KnockdownThreatMultiplier(attacker, defender) *
            (1.0 - toughness * 0.52) *
            (1.18 - stamina * 0.55);

        return _random.Chance(Math.Clamp(tkoProb * 0.11, 0.0, 0.92));
    }

    /// <summary>
    /// Returns whether the fighter is in a dominant top position that enables unanswered ground-and-pound.
    /// </summary>
    private static bool IsDominantGroundPosition(FightPosition position) => position is
        FightPosition.GroundAndPoundTop or
        FightPosition.TurtleTop or
        FightPosition.SideControlTop or
        FightPosition.MountTop or
        FightPosition.BackControlAttacker;

    /// <summary>
    /// Returns the per-tick probability of throwing a standing strike.
    /// </summary>
    private static double GetStandingStrikeProbability(FighterState actor, FighterState opponent)
    {
        var speed = actor.Fighter.Striking.Speed / 100.0;
        var physical = PhysicalAdvantageModel.StrikeAccuracyMultiplier(actor, opponent);
        var technical = PhysicalAdvantageModel.StandingTechnicalEdgeMultiplier(actor, opponent);
        var output = PhysicalAdvantageModel.StandingOutputMultiplier(actor, opponent);
        var tempoPhysical = 1.0 + (Math.Clamp(physical, 0.80, 1.20) - 1.0) * 0.30;
        return Math.Clamp(StandingStrikeProb * (0.8 + speed * 0.35) * actor.EffectiveMovementMultiplier * tempoPhysical * technical * output, 0.04, 0.28);
    }

    /// <summary>
    /// Returns the per-tick probability of entering or initiating the clinch.
    /// </summary>
    private static double GetClinchProbability(FighterState actor, FighterState opponent)
    {
        var styleFactor = actor.Fighter.StyleFactor(FightingStyle.Wrestler) >= 1.0 || actor.Fighter.StyleFactor(FightingStyle.Judoka) >= 1.0
            ? 1.5
            : actor.Fighter.StyleFactor(FightingStyle.MMAFighter) >= 1.0 || actor.Fighter.StyleFactor(FightingStyle.BJJPractitioner) >= 1.0
                ? 1.1
                : 0.8;

        return Math.Clamp(
            StandingClinchProb
            * styleFactor
            * actor.EffectiveMovementMultiplier
            * actor.Fighter.SpecialtyFactor(StyleSpecialty.MuayThaiClinch)
            * PhysicalAdvantageModel.ClinchEntryMultiplier(actor, opponent),
            0.001,
            0.045);
    }

    /// <summary>
    /// Returns the per-tick probability of throwing a strike once in the clinch.
    /// </summary>
    private static double GetClinchStrikeProbability(FighterState actor, FighterState opponent)
    {
        var styleFactor = actor.Fighter.StyleFactor(FightingStyle.MuayThai) >= 1.0
            ? 1.3
            : actor.Fighter.StyleFactor(FightingStyle.Wrestler) >= 1.0 || actor.Fighter.StyleFactor(FightingStyle.Judoka) >= 1.0
                ? 0.8
                : 1.0;

        return Math.Clamp(0.10 * styleFactor * actor.EffectiveMovementMultiplier * Math.Clamp(PhysicalAdvantageModel.ClinchEntryMultiplier(actor, opponent), 0.6, 1.4), 0.03, 0.20);
    }

    /// <summary>
    /// Builds a matchup-aware standing strategy so high-IQ fighters can target major vulnerabilities.
    /// </summary>
    private static StandingStrategy EvaluateStandingStrategy(FighterState actor, FighterState opponent)
    {
        var iq = actor.Fighter.FightIq / 100.0;
        var strikingGap = CalculateStrikingGap(actor, opponent);
        var grapplingGap = CalculateGrapplingGap(actor, opponent);

        var strikeMultiplier = 1.0;
        var takedownMultiplier = 1.0;
        var clinchMultiplier = 1.0;

        if (grapplingGap >= 14 && grapplingGap >= strikingGap + 8)
        {
            var exploitStrength = (grapplingGap - 14) / 18.0;
            takedownMultiplier += exploitStrength * (1.35 + iq * 1.55);
            clinchMultiplier += exploitStrength * (0.75 + iq * 1.10);
            strikeMultiplier -= exploitStrength * (0.30 + iq * 0.30);

            if (grapplingGap >= 24 && iq >= 0.88)
            {
                takedownMultiplier += 0.90;
                clinchMultiplier += 0.35;
                strikeMultiplier -= 0.18;
            }
        }
        else if (strikingGap >= 10 && strikingGap >= grapplingGap + 6)
        {
            var exploitStrength = (strikingGap - 10) / 20.0;
            strikeMultiplier += exploitStrength * (0.35 + iq * 0.45);
            takedownMultiplier -= exploitStrength * 0.18;
            clinchMultiplier -= exploitStrength * 0.10;
        }
        else if (strikingGap <= -8)
        {
            var respectStrength = Math.Min(1.0, Math.Abs(strikingGap + 8) / 18.0);
            strikeMultiplier -= respectStrength * (0.12 + iq * 0.10);
        }

        return new StandingStrategy(
            Math.Clamp(strikeMultiplier, 0.45, 1.45),
            Math.Clamp(takedownMultiplier, 0.70, 4.20),
            Math.Clamp(clinchMultiplier, 0.75, 2.40));
    }

    /// <summary>
    /// Estimates the actor's technical striking edge over the opponent.
    /// </summary>
    private static double CalculateStrikingGap(FighterState actor, FighterState opponent)
    {
        var actorOffense =
            actor.Fighter.Striking.Accuracy * 0.34 +
            actor.Fighter.Striking.Power * 0.28 +
            actor.Fighter.Striking.Speed * 0.22 +
            actor.Fighter.GetStyleProficiency(actor.Fighter.DominantStrikingStyle()) * 0.16;

        var opponentDefense =
            opponent.Fighter.Striking.Defense * 0.42 +
            opponent.Fighter.Striking.ChinDurability * 0.24 +
            opponent.Fighter.Striking.BodyDurability * 0.18 +
            opponent.Fighter.Athletics.Agility * 0.16;

        return actorOffense - opponentDefense;
    }

    /// <summary>
    /// Estimates the actor's ability to exploit the opponent's grappling vulnerabilities.
    /// </summary>
    private static double CalculateGrapplingGap(FighterState actor, FighterState opponent)
    {
        var actorPressure =
            actor.Fighter.Grappling.TakedownAccuracy * 0.34 +
            actor.Fighter.Grappling.GroundControl * 0.24 +
            actor.Fighter.Grappling.SubmissionOffense * 0.18 +
            actor.Fighter.Grappling.Clinchwork * 0.10 +
            actor.Fighter.GetStyleProficiency(FightingStyle.Wrestler) * 0.14;

        var opponentResistance =
            opponent.Fighter.Grappling.TakedownDefense * 0.42 +
            opponent.Fighter.Grappling.SubmissionDefense * 0.22 +
            opponent.Fighter.Athletics.Strength * 0.16 +
            opponent.Fighter.Athletics.Agility * 0.10 +
            opponent.Fighter.Grappling.Clinchwork * 0.10;

        return actorPressure - opponentResistance;
    }

    /// <summary>
    /// Keeps standing action probabilities within the intended per-tick action budget.
    /// </summary>
    private static void ScaleStandingProbabilities(ref double strikeProb, ref double tdProb, ref double clinchProb)
    {
        var total = strikeProb + tdProb + clinchProb;
        if (total <= MaxStandingActionShare)
            return;

        var scale = MaxStandingActionShare / total;
        strikeProb *= scale;
        tdProb *= scale;
        clinchProb *= scale;
    }

    /// <summary>
    /// Builds the standing strike pool from the fighter's style profiles and specialties.
    /// </summary>
    private static IReadOnlyList<StrikeType> GetStandingStrikePool(FighterState actor)
    {
        var pool = new List<StrikeType>();
        var fighter = actor.Fighter;

        AddRepeated(pool, BoxingStandingStrikes, fighter.GetStyleProficiency(FightingStyle.Boxer));
        AddRepeated(pool, KickboxingStandingStrikes, fighter.GetStyleProficiency(FightingStyle.Kickboxer));
        AddRepeated(pool, MuayThaiStandingStrikes, fighter.GetStyleProficiency(FightingStyle.MuayThai));
        AddRepeated(pool, GrapplerStandingStrikes, Math.Max(fighter.GetStyleProficiency(FightingStyle.Wrestler), fighter.GetStyleProficiency(FightingStyle.BJJPractitioner)));
        AddRepeated(pool, StandingStrikes, Math.Max(30, fighter.GetStyleProficiency(fighter.PrimaryStyle)));

        if (fighter.HasSpecialty(StyleSpecialty.KickboxingKicks) || fighter.HasSpecialty(StyleSpecialty.TaekwondoKicks))
        {
            pool.Add(StrikeType.Roundhouse);
            pool.Add(StrikeType.HeadKick);
            pool.Add(StrikeType.BodyKick);
            pool.Add(StrikeType.CalfKick);
        }

        if (fighter.HasSpecialty(StyleSpecialty.KickboxingRange))
        {
            pool.Add(StrikeType.Jab);
            pool.Add(StrikeType.Jab);
            pool.Add(StrikeType.Teep);
            pool.Add(StrikeType.CalfKick);
        }

        if (fighter.HasSpecialty(StyleSpecialty.KickboxingPressure))
        {
            pool.Add(StrikeType.Cross);
            pool.Add(StrikeType.Hook);
            pool.Add(StrikeType.Overhand);
            pool.Add(StrikeType.BodyShot);
        }

        if (fighter.HasSpecialty(StyleSpecialty.KarateDistance) || fighter.HasSpecialty(StyleSpecialty.MuayThaiTeeps))
        {
            pool.Add(StrikeType.Jab);
            pool.Add(StrikeType.Teep);
            pool.Add(StrikeType.FrontKick);
        }

        if (fighter.HasSpecialty(StyleSpecialty.MuayThaiElbows))
        {
            pool.Add(StrikeType.ElbowHorizontal);
            pool.Add(StrikeType.ElbowUpward);
        }

        if (fighter.HasSpecialty(StyleSpecialty.MuayThaiKnees))
        {
            pool.Add(StrikeType.KneeBody);
            pool.Add(StrikeType.KneeHead);
        }

        if (fighter.HasSpecialty(StyleSpecialty.BoxingPocketPressure))
        {
            pool.Add(StrikeType.Hook);
            pool.Add(StrikeType.Uppercut);
            pool.Add(StrikeType.BodyShot);
        }

        if (fighter.HasSpecialty(StyleSpecialty.ObliqueKicks))
        {
            pool.Add(StrikeType.ObliqueKick);
            pool.Add(StrikeType.Stomp);
        }

        return pool;
    }

    /// <summary>
    /// Builds the clinch strike pool from Muay Thai, boxing, and specialty signals.
    /// </summary>
    private static IReadOnlyList<StrikeType> GetClinchStrikePool(FighterState actor)
    {
        var pool = new List<StrikeType>(ClinchStrikes);
        var fighter = actor.Fighter;

        AddRepeated(pool, MuayThaiClinchStrikes, fighter.GetStyleProficiency(FightingStyle.MuayThai));
        AddRepeated(pool, BoxingClinchStrikes, fighter.GetStyleProficiency(FightingStyle.Boxer));

        if (fighter.HasSpecialty(StyleSpecialty.MuayThaiElbows))
        {
            pool.Add(StrikeType.ElbowHorizontal);
            pool.Add(StrikeType.ElbowUpward);
        }

        if (fighter.HasSpecialty(StyleSpecialty.MuayThaiKnees))
        {
            pool.Add(StrikeType.KneeBody);
            pool.Add(StrikeType.KneeHead);
        }

        return pool;
    }

    /// <summary>
    /// Builds the ground-and-pound strike pool from the fighter's specialties.
    /// </summary>
    private static IReadOnlyList<StrikeType> GetGroundStrikePool(FighterState actor)
    {
        var pool = new List<StrikeType>(GroundStrikes);
        var fighter = actor.Fighter;

        if (fighter.HasSpecialty(StyleSpecialty.GroundAndPoundPunches))
        {
            pool.Add(StrikeType.GroundPunch);
            pool.Add(StrikeType.Hammerfist);
            pool.Add(StrikeType.GroundPunch);
        }

        if (fighter.HasSpecialty(StyleSpecialty.GroundAndPoundElbows))
        {
            pool.Add(StrikeType.GroundElbow);
            pool.Add(StrikeType.GroundElbow);
        }

        return pool;
    }

    /// <summary>
    /// Adds repeated copies of a strike subset according to style proficiency.
    /// </summary>
    private static void AddRepeated(List<StrikeType> target, IReadOnlyList<StrikeType> source, int proficiency)
    {
        var copies = proficiency switch
        {
            >= 90 => 3,
            >= 75 => 2,
            >= 55 => 1,
            _ => 0
        };

        for (var i = 0; i < copies; i++)
            target.AddRange(source);
    }

    /// <summary>
    /// Chooses a takedown action weighted by wrestling and clinch specialties.
    /// </summary>
    private GrappleAction ChooseTakedownAction(FighterState actor, bool standing)
    {
        var pool = new List<GrappleAction>(standing ? TakedownActions : ClinchTakedownActions);
        if (actor.Fighter.HasSpecialty(StyleSpecialty.WrestlingSingleLeg))
            pool.Add(GrappleAction.SingleLegTakedown);
        if (actor.Fighter.HasSpecialty(StyleSpecialty.WrestlingDoubleLeg))
            pool.Add(GrappleAction.DoubleLegTakedown);
        if (actor.Fighter.HasSpecialty(StyleSpecialty.ClinchTrips) || actor.Fighter.HasSpecialty(StyleSpecialty.JudoTripsThrows))
        {
            pool.Add(GrappleAction.OutsideTrip);
            pool.Add(GrappleAction.InsideTrip);
        }

        return _random.Choose(pool);
    }

    /// <summary>
    /// Chooses a guard-passing action weighted by guard-passing specialties.
    /// </summary>
    private GrappleAction ChooseGuardPassAction(FighterState actor)
    {
        var pool = new List<GrappleAction>(GuardPassActions);
        if (actor.Fighter.HasSpecialty(StyleSpecialty.BjjGuardPassing))
        {
            pool.Add(GrappleAction.KneeCutPass);
            pool.Add(GrappleAction.StackPass);
        }

        return _random.Choose(pool);
    }

    /// <summary>
    /// Chooses a submission action weighted by the fighter's specialty profile.
    /// </summary>
    private GrappleAction ChooseSubmissionAction(FighterState actor)
    {
        var pool = new List<GrappleAction>(GroundSubmissions);

        AddSubmissionWeight(pool, actor, StyleSpecialty.RearNakedChoke, GrappleAction.SubmissionAttemptRearNakedChoke);
        AddSubmissionWeight(pool, actor, StyleSpecialty.GuillotineChoke, GrappleAction.SubmissionAttemptGuillotine);
        AddSubmissionWeight(pool, actor, StyleSpecialty.DarceChoke, GrappleAction.SubmissionAttemptDarce);
        AddSubmissionWeight(pool, actor, StyleSpecialty.AnacondaChoke, GrappleAction.SubmissionAttemptAnaconda);
        AddSubmissionWeight(pool, actor, StyleSpecialty.ArmTriangleChoke, GrappleAction.SubmissionAttemptArmTriangle);
        AddSubmissionWeight(pool, actor, StyleSpecialty.TriangleChoke, GrappleAction.SubmissionAttemptTriangle);
        AddSubmissionWeight(pool, actor, StyleSpecialty.Armbar, GrappleAction.SubmissionAttemptArmbar);
        AddSubmissionWeight(pool, actor, StyleSpecialty.Kimura, GrappleAction.SubmissionAttemptKimura);
        AddSubmissionWeight(pool, actor, StyleSpecialty.HeelHook, GrappleAction.SubmissionAttemptHeelHook);

        return _random.Choose(pool);
    }

    /// <summary>
    /// Adds extra weight for a submission when the fighter specializes in it.
    /// </summary>
    private static void AddSubmissionWeight(List<GrappleAction> pool, FighterState actor, StyleSpecialty specialty, GrappleAction action)
    {
        if (!actor.Fighter.HasSpecialty(specialty)) return;

        pool.Add(action);
        pool.Add(action);
    }

    /// <summary>
    /// Advances temporary recovery states such as stun duration.
    /// </summary>
    private static void UpdateRecoveryState(FighterState fighter)
    {
        fighter.GroundedTicksRemaining = Math.Max(0, fighter.GroundedTicksRemaining - 1);

        if (!fighter.IsStunned)
            return;

        fighter.StunRecoveryTicksRemaining = Math.Max(0, fighter.StunRecoveryTicksRemaining - 1);
        if (fighter.StunRecoveryTicksRemaining == 0)
            fighter.IsStunned = false;
    }

    /// <summary>
    /// Creates the immutable round model from the event log and round stats.
    /// </summary>
    private static Round BuildRound(int number, List<FightEvent> events, RoundStats statsA, RoundStats statsB) =>
        new()
        {
            Number        = number,
            Events        = events,
            FighterAStats = statsA,
            FighterBStats = statsB,
            Scorecards    = []
        };

    /// <summary>
    /// Creates non-action marker events such as round start and round end.
    /// </summary>
    private static FightEvent CreateMarker(FightEventType type, FighterState stateA, TimeSpan timestamp) =>
        new()
        {
            Timestamp      = timestamp,
            Type           = type,
            Actor          = stateA.Fighter,
            PositionBefore = stateA.CurrentPosition,
            PositionAfter  = stateA.CurrentPosition,
            IsSignificant  = false,
            NarrationText  = string.Empty
        };

    private readonly record struct StandingStrategy(double StrikeMultiplier, double TakedownMultiplier, double ClinchMultiplier);
}

internal sealed class RoundStatsBuilder
{
    public int    SignificantStrikesLanded    { get; set; }
    public int    SignificantStrikesAttempted { get; set; }
    public int    TotalStrikesLanded          { get; set; }
    public int    TakedownsLanded             { get; set; }
    public int    TakedownsAttempted          { get; set; }
    public int    SubmissionAttempts          { get; set; }
    public int    KnockdownsScored            { get; set; }
    public double DamageTaken                 { get; set; }

    /// <summary>
    /// Converts the mutable counters into the immutable round statistics model.
    /// </summary>
    public RoundStats Build(double staminaRemaining) => new(
        SignificantStrikesLanded,
        SignificantStrikesAttempted,
        TotalStrikesLanded,
        TakedownsLanded,
        TakedownsAttempted,
        SubmissionAttempts,
        KnockdownsScored,
        DamageTaken,
        staminaRemaining);
}
