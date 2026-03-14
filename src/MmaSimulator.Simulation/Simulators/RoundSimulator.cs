using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Interfaces;
using MmaSimulator.Core.Models;

namespace MmaSimulator.Simulation.Simulators;

/// <summary>
/// Simulates a single round via a 300-tick loop (1 tick = 1 second).
///
/// Calibration targets (per 5-round fight):
///   Elite striker  →  150–180 sig strikes,  0–1 TD,   0   sub attempts
///   Elite wrestler →   50–80  sig strikes,  6–12 TD,  0–2 sub attempts
///   BJJ specialist →   60–100 sig strikes,  3–8  TD,  3–8 sub attempts
///
/// Action probabilities are deliberately low because most ticks represent
/// movement, circling, feinting, and recovery — not continuous action.
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
    private const double GnpTopStrikeProb     = 0.06;
    private const double GnpTopPassProb        = 0.008;
    private const double GnpTopSubProb         = 0.003;

    private const double FullGuardTopStrikeProb = 0.03;
    private const double FullGuardTopPassProb   = 0.012;
    private const double FullGuardTopSubProb    = 0.002;

    private const double MountTopStrikeProb     = 0.05;
    private const double MountTopSubProb        = 0.010;

    private const double BackControlStrikeProb  = 0.025;
    private const double BackControlSubProb     = 0.018;

    private const double SideControlTopStrikeProb = 0.04;
    private const double SideControlTopPassProb   = 0.010;

    // ── Escape base probabilities ─────────────────────────────────────────
    // ~1 % per tick → average ~100 sec (≈ 1.5–2 min) on the ground — realistic
    private const double BottomEscapeBaseProb    = 0.010;
    private const double BackControlEscapeProb   = 0.004;

    // ── Strike pools ──────────────────────────────────────────────────────
    private static readonly StrikeType[] StandingStrikes =
    [
        StrikeType.Jab, StrikeType.Cross, StrikeType.Cross,
        StrikeType.Hook, StrikeType.Hook, StrikeType.Uppercut,
        StrikeType.Overhand, StrikeType.Roundhouse, StrikeType.HeadKick,
        StrikeType.BodyShot, StrikeType.FrontKick, StrikeType.SpinningBackKick
    ];

    private static readonly StrikeType[] ClinchStrikes =
    [
        StrikeType.Hook, StrikeType.Uppercut, StrikeType.Elbow,
        StrikeType.Knee, StrikeType.BodyShot
    ];

    private static readonly StrikeType[] GroundStrikes =
    [
        StrikeType.Hook, StrikeType.Cross, StrikeType.Uppercut,
        StrikeType.Elbow, StrikeType.Elbow
    ];

    private static readonly GrappleAction[] GroundSubmissions =
    [
        GrappleAction.SubmissionAttemptChoke,
        GrappleAction.SubmissionAttemptArmlock,
        GrappleAction.SubmissionAttemptLeglock
    ];

    private readonly IStrikingEngine _striking;
    private readonly IGrapplingEngine _grappling;
    private readonly IStaminaEngine _stamina;
    private readonly IJudgeScoringEngine _judgeScoring;
    private readonly IRandomProvider _random;

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

    public Round SimulateRound(int roundNumber, FighterState stateA, FighterState stateB, SimulationOptions options)
    {
        var events = new List<FightEvent>();
        var statsA = new RoundStatsBuilder();
        var statsB = new RoundStatsBuilder();

        events.Add(CreateMarker(FightEventType.RoundStart, stateA, TimeSpan.Zero));

        FightEvent? finishEvent = null;

        for (var tick = 0; tick < TicksPerRound; tick++)
        {
            var timestamp  = TimeSpan.FromSeconds(tick);
            var actorIsA   = _random.Chance(0.5);
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

            if (tickEvent.Type == FightEventType.KnockdownScored && CheckTkoAfterKnockdown(opponent))
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

    private FightEvent? ResolveAction(
        FighterState actor, FighterState opponent,
        RoundStatsBuilder actorStats, RoundStatsBuilder opponentStats,
        TimeSpan timestamp) => actor.CurrentPosition switch
    {
        FightPosition.Standing  => ResolveStanding(actor, opponent, actorStats, opponentStats, timestamp),
        FightPosition.Clinch    => ResolveClinch(actor, opponent, actorStats, opponentStats, timestamp),

        FightPosition.GroundAndPoundTop or
        FightPosition.SideControlTop    => ResolveTopGround(actor, opponent, actorStats, timestamp, actor.CurrentPosition),

        FightPosition.FullGuardTop or
        FightPosition.HalfGuardTop      => ResolveGuardTop(actor, opponent, actorStats, timestamp),

        FightPosition.MountTop          => ResolveMountTop(actor, opponent, actorStats, timestamp),
        FightPosition.BackControlAttacker => ResolveBackControl(actor, opponent, actorStats, timestamp),

        FightPosition.GroundAndPoundBottom or
        FightPosition.SideControlBottom or
        FightPosition.FullGuardBottom or
        FightPosition.HalfGuardBottom   => ResolveBottomEscape(actor, opponent, BottomEscapeBaseProb, timestamp),

        FightPosition.MountBottom       => ResolveBottomEscape(actor, opponent, BottomEscapeBaseProb * 0.8, timestamp),
        FightPosition.BackControlDefender => ResolveBottomEscape(actor, opponent, BackControlEscapeProb, timestamp),

        _ => null
    };

    // ── Standing & Clinch ─────────────────────────────────────────────────

    private FightEvent? ResolveStanding(
        FighterState actor, FighterState opponent,
        RoundStatsBuilder actorStats, RoundStatsBuilder opponentStats,
        TimeSpan timestamp)
    {
        var roll   = _random.NextDouble();
        var tdProb = GetTakedownProbability(actor);

        if (roll < StandingStrikeProb)
            return ExecuteStrike(actor, opponent, actorStats, opponentStats, StandingStrikes, timestamp);

        if (roll < StandingStrikeProb + tdProb)
            return ExecuteTakedown(actor, opponent, actorStats, timestamp);

        if (roll < StandingStrikeProb + tdProb + StandingClinchProb)
        {
            actor.CurrentPosition    = FightPosition.Clinch;
            opponent.CurrentPosition = FightPosition.Clinch;
        }

        return null;
    }

    private FightEvent? ResolveClinch(
        FighterState actor, FighterState opponent,
        RoundStatsBuilder actorStats, RoundStatsBuilder opponentStats,
        TimeSpan timestamp)
    {
        var roll = _random.NextDouble();

        if (roll < 0.12)
            return ExecuteStrike(actor, opponent, actorStats, opponentStats, ClinchStrikes, timestamp);

        // TDs from clinch are ~3x easier
        if (roll < 0.12 + GetTakedownProbability(actor) * 3.0)
            return ExecuteTakedown(actor, opponent, actorStats, timestamp);

        if (roll < 0.22) // break from clinch
        {
            actor.CurrentPosition    = FightPosition.Standing;
            opponent.CurrentPosition = FightPosition.Standing;
        }

        return null;
    }

    // ── Ground top positions ──────────────────────────────────────────────

    private FightEvent? ResolveTopGround(
        FighterState actor, FighterState opponent,
        RoundStatsBuilder actorStats, TimeSpan timestamp, FightPosition pos)
    {
        var (sProb, pProb) = pos == FightPosition.GroundAndPoundTop
            ? (GnpTopStrikeProb, GnpTopPassProb)
            : (SideControlTopStrikeProb, SideControlTopPassProb);

        var roll = _random.NextDouble();
        if (roll < sProb)
            return ExecuteGroundStrike(actor, opponent, actorStats, timestamp);
        if (roll < sProb + pProb)
            return TryGuardPass(actor, opponent, timestamp);
        if (roll < sProb + pProb + GnpTopSubProb)
            return TrySubmission(actor, opponent, actorStats, timestamp);
        return null;
    }

    private FightEvent? ResolveGuardTop(
        FighterState actor, FighterState opponent,
        RoundStatsBuilder actorStats, TimeSpan timestamp)
    {
        var roll = _random.NextDouble();
        if (roll < FullGuardTopStrikeProb)
            return ExecuteGroundStrike(actor, opponent, actorStats, timestamp);
        if (roll < FullGuardTopStrikeProb + FullGuardTopPassProb)
            return TryGuardPass(actor, opponent, timestamp);
        if (roll < FullGuardTopStrikeProb + FullGuardTopPassProb + FullGuardTopSubProb)
            return TrySubmission(actor, opponent, actorStats, timestamp);
        return null;
    }

    private FightEvent? ResolveMountTop(
        FighterState actor, FighterState opponent,
        RoundStatsBuilder actorStats, TimeSpan timestamp)
    {
        var roll = _random.NextDouble();
        if (roll < MountTopStrikeProb)
            return ExecuteGroundStrike(actor, opponent, actorStats, timestamp);
        if (roll < MountTopStrikeProb + MountTopSubProb * GetSubAttemptMultiplier(actor))
            return TrySubmission(actor, opponent, actorStats, timestamp);
        return null;
    }

    private FightEvent? ResolveBackControl(
        FighterState actor, FighterState opponent,
        RoundStatsBuilder actorStats, TimeSpan timestamp)
    {
        var roll = _random.NextDouble();
        if (roll < BackControlSubProb * GetSubAttemptMultiplier(actor))
            return TrySubmission(actor, opponent, actorStats, timestamp);
        if (roll < BackControlSubProb + BackControlStrikeProb)
            return ExecuteGroundStrike(actor, opponent, actorStats, timestamp);
        return null;
    }

    private FightEvent? ResolveBottomEscape(
        FighterState actor, FighterState opponent,
        double baseProb, TimeSpan timestamp)
    {
        var escapeProb = baseProb
            * (actor.Fighter.Grappling.TakedownDefense / 70.0)
            * actor.CurrentStamina;

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

    private FightEvent? ExecuteStrike(
        FighterState actor, FighterState opponent,
        RoundStatsBuilder actorStats, RoundStatsBuilder opponentStats,
        StrikeType[] pool, TimeSpan timestamp)
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

    private FightEvent? ExecuteGroundStrike(
        FighterState actor, FighterState opponent,
        RoundStatsBuilder actorStats, TimeSpan timestamp)
    {
        var opDummy = new RoundStatsBuilder();
        return ExecuteStrike(actor, opponent, actorStats, opDummy, GroundStrikes, timestamp);
    }

    private FightEvent? ExecuteTakedown(
        FighterState actor, FighterState opponent,
        RoundStatsBuilder actorStats, TimeSpan timestamp)
    {
        var outcome = _grappling.ResolveGrappleAction(actor, opponent, GrappleAction.TakedownAttempt);
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
            GrappleAction  = GrappleAction.TakedownAttempt,
            IsSignificant  = outcome.Succeeded,
            NarrationText  = outcome.NarrationText
        };
    }

    private FightEvent? TryGuardPass(FighterState actor, FighterState opponent, TimeSpan timestamp)
    {
        var outcome = _grappling.ResolveGrappleAction(actor, opponent, GrappleAction.GuardPass);
        if (!outcome.Succeeded) return null;

        return new FightEvent
        {
            Timestamp      = timestamp,
            Type           = FightEventType.PositionChange,
            Actor          = actor.Fighter,
            Target         = opponent.Fighter,
            PositionBefore = actor.CurrentPosition,
            PositionAfter  = outcome.NewPosition,
            IsSignificant  = true,
            NarrationText  = outcome.NarrationText
        };
    }

    private FightEvent? TrySubmission(
        FighterState actor, FighterState opponent,
        RoundStatsBuilder actorStats, TimeSpan timestamp)
    {
        var action  = _random.Choose(GroundSubmissions);
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
    private static double GetTakedownProbability(FighterState actor)
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

        return baseProb * (actor.Fighter.Grappling.TakedownAccuracy / 70.0) * actor.CurrentStamina;
    }

    /// <summary>
    /// Multiplier for submission attempt probability from dominant positions.
    /// BJJ specialists go for subs relentlessly; pure strikers rarely bother.
    /// </summary>
    private static double GetSubAttemptMultiplier(FighterState actor)
    {
        var styleMod = actor.Fighter.PrimaryStyle switch
        {
            FightingStyle.BJJPractitioner => 3.5,
            FightingStyle.Judoka          => 2.0,
            FightingStyle.Wrestler        => 1.4,
            FightingStyle.MMAFighter      => 1.2,
            _                            => 0.5
        };
        return Math.Clamp(styleMod * (actor.Fighter.Grappling.SubmissionOffense / 70.0), 0.3, 5.0);
    }

    private bool CheckTkoAfterKnockdown(FighterState downed)
    {
        var tkoProb = (downed.KnockdownsThisFight / 2.0)
            * (1.0 - downed.Fighter.Athletics.Toughness / 100.0)
            * (1.0 - downed.CurrentStamina * 0.4);

        return _random.Chance(Math.Clamp(tkoProb, 0, 0.80));
    }

    private static Round BuildRound(int number, List<FightEvent> events, RoundStats statsA, RoundStats statsB) =>
        new()
        {
            Number        = number,
            Events        = events,
            FighterAStats = statsA,
            FighterBStats = statsB,
            Scorecards    = []
        };

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
