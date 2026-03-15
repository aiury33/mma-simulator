using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Interfaces;
using MmaSimulator.Core.Models;

namespace MmaSimulator.Simulation.Simulators;

public sealed class FightSimulator : IFightSimulator
{
    private readonly IRoundSimulator _roundSimulator;
    private readonly IJudgeScoringEngine _judgeScoring;
    private readonly IStaminaEngine _stamina;

    /// <summary>
    /// Creates the fight simulator with its round, scoring, and stamina dependencies.
    /// </summary>
    public FightSimulator(
        IRoundSimulator roundSimulator,
        IJudgeScoringEngine judgeScoring,
        IStaminaEngine stamina)
    {
        _roundSimulator = roundSimulator;
        _judgeScoring   = judgeScoring;
        _stamina        = stamina;
    }

    /// <summary>
    /// Simulates a full fight until a finish or judges' decision.
    /// </summary>
    public FightResult Simulate(Fight fight, SimulationOptions options)
    {
        var stateA = new FighterState(fight.FighterA);
        var stateB = new FighterState(fight.FighterB);
        var rounds = new List<Round>();

        FightResultMethod? finishMethod = null;
        Fighter?  winner      = null;
        Fighter?  loser       = null;
        TimeSpan  finishTime  = TimeSpan.Zero;
        int       finishRound = fight.NumberOfRounds;

        for (var roundNum = 1; roundNum <= fight.NumberOfRounds; roundNum++)
        {
            var round = _roundSimulator.SimulateRound(roundNum, stateA, stateB, options);
            rounds.Add(round);

            var finishEvent = round.Events.FirstOrDefault(e => e.Type == FightEventType.FightEnded);

            if (finishEvent != null)
            {
                finishRound = roundNum;
                finishTime  = finishEvent.Timestamp;
                winner      = finishEvent.Actor;
                loser       = finishEvent.Target;

                var hasSubmission = round.Events.Any(e => e.Type == FightEventType.SubmissionLocked);
                if (hasSubmission)
                {
                    finishMethod = FightResultMethod.Submission;
                }
                else
                {
                    var winnerKnockdowns = winner.Id == fight.FighterA.Id
                        ? round.FighterAStats.KnockdownsScored
                        : round.FighterBStats.KnockdownsScored;
                    finishMethod = winnerKnockdowns > 0 ? FightResultMethod.KO : FightResultMethod.TKO;
                }
                break;
            }

            // Between-round stamina recovery
            stateA.CurrentStamina = Math.Min(1.0, stateA.CurrentStamina + _stamina.CalculateRoundRecovery(stateA));
            stateB.CurrentStamina = Math.Min(1.0, stateB.CurrentStamina + _stamina.CalculateRoundRecovery(stateB));
            ResetBetweenRounds(stateA, stateB);
        }

        if (finishMethod != null)
        {
            return new FightResult
            {
                Fight          = fight,
                Rounds         = rounds,
                Winner         = winner!,
                Loser          = loser,
                Method         = finishMethod.Value,
                FinishRound    = finishRound,
                FinishTime     = finishTime,
                JudgeDecisions = [],
                StatsSummary   = BuildSummary(rounds)
            };
        }

        // Decision — tally all round scorecards
        var allScorecards = rounds.SelectMany(r => r.Scorecards).ToList();
        var d1 = _judgeScoring.TallyDecision(1, allScorecards, fight.FighterA, fight.FighterB);
        var d2 = _judgeScoring.TallyDecision(2, allScorecards, fight.FighterA, fight.FighterB);
        var d3 = _judgeScoring.TallyDecision(3, allScorecards, fight.FighterA, fight.FighterB);
        var decisions = new[] { d1, d2, d3 };

        var votesA = decisions.Count(d => d.PickedWinner.Id == fight.FighterA.Id);
        var votesB = decisions.Count(d => d.PickedWinner.Id == fight.FighterB.Id);

        if (votesA == 3)       { finishMethod = FightResultMethod.DecisionUnanimous; winner = fight.FighterA; loser = fight.FighterB; }
        else if (votesB == 3)  { finishMethod = FightResultMethod.DecisionUnanimous; winner = fight.FighterB; loser = fight.FighterA; }
        else if (votesA == 2)  { finishMethod = FightResultMethod.DecisionSplit;     winner = fight.FighterA; loser = fight.FighterB; }
        else if (votesB == 2)  { finishMethod = FightResultMethod.DecisionSplit;     winner = fight.FighterB; loser = fight.FighterA; }
        else                   { finishMethod = FightResultMethod.Draw;              winner = fight.FighterA; }

        return new FightResult
        {
            Fight          = fight,
            Rounds         = rounds,
            Winner         = winner!,
            Loser          = loser,
            Method         = finishMethod.Value,
            FinishRound    = fight.NumberOfRounds,
            FinishTime     = TimeSpan.FromMinutes(5),
            JudgeDecisions = decisions,
            StatsSummary   = BuildSummary(rounds)
        };
    }

    /// <summary>
    /// Aggregates per-round stats into a fight-level summary.
    /// </summary>
    private static FightStatsSummary BuildSummary(IReadOnlyList<Round> rounds) => new(
        rounds.Sum(r => r.FighterAStats.SignificantStrikesLanded),
        rounds.Sum(r => r.FighterBStats.SignificantStrikesLanded),
        rounds.Sum(r => r.FighterAStats.TakedownsLanded),
        rounds.Sum(r => r.FighterBStats.TakedownsLanded),
        rounds.Sum(r => r.FighterAStats.SubmissionAttempts),
        rounds.Sum(r => r.FighterBStats.SubmissionAttempts),
        rounds.Sum(r => r.FighterAStats.KnockdownsScored),
        rounds.Sum(r => r.FighterBStats.KnockdownsScored),
        rounds.Sum(r => r.FighterAStats.DamageTaken),
        rounds.Sum(r => r.FighterBStats.DamageTaken));

    /// <summary>
    /// Resets transient positional state between rounds while preserving fight damage and stamina.
    /// </summary>
    private static void ResetBetweenRounds(FighterState stateA, FighterState stateB)
    {
        stateA.CurrentPosition = FightPosition.Standing;
        stateB.CurrentPosition = FightPosition.Standing;
        stateA.GroundedTicksRemaining = 0;
        stateB.GroundedTicksRemaining = 0;
        stateA.IsStunned = false;
        stateB.IsStunned = false;
        stateA.StunRecoveryTicksRemaining = 0;
        stateB.StunRecoveryTicksRemaining = 0;
    }
}
