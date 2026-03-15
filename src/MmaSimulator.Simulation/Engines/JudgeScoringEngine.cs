using MmaSimulator.Core.Interfaces;
using MmaSimulator.Core.Models;

namespace MmaSimulator.Simulation.Engines;

public sealed class JudgeScoringEngine : IJudgeScoringEngine
{
    private static readonly (double Striking, double Grappling, double Aggression, double Control)[] JudgeCriteriaWeights =
    [
        (0.50, 0.25, 0.10, 0.15),
        (0.55, 0.20, 0.10, 0.15),
        (0.45, 0.30, 0.10, 0.15)
    ];

    /// <summary>
    /// Scores a round for one judge using weighted striking, grappling, aggression, and control criteria.
    /// </summary>
    public JudgeScorecard ScoreRound(int judgeIndex, Round round, Fighter fighterA, Fighter fighterB)
    {
        var weights = JudgeCriteriaWeights[Math.Clamp(judgeIndex - 1, 0, 2)];
        var a = round.FighterAStats;
        var b = round.FighterBStats;

        var strikingDiff = (a.SignificantStrikesLanded - b.SignificantStrikesLanded)
            + (a.KnockdownsScored - b.KnockdownsScored) * 8
            + (b.DamageTaken - a.DamageTaken) * 1.25;

        var grapplingDiff = (a.TakedownsLanded - b.TakedownsLanded) * 3
            + (a.SubmissionAttempts - b.SubmissionAttempts) * 2.5;

        var aggressionDiff = a.TotalStrikesLanded - b.TotalStrikesLanded;

        var controlDiff = (a.TakedownsLanded - b.TakedownsLanded)
            + (a.SubmissionAttempts - b.SubmissionAttempts) * 0.5;

        var composite = weights.Striking * strikingDiff
            + weights.Grappling * grapplingDiff
            + weights.Aggression * aggressionDiff
            + weights.Control * controlDiff;

        int scoreA, scoreB;

        if (a.KnockdownsScored > b.KnockdownsScored && composite > 5)
        {
            scoreA = 10;
            scoreB = 8;
        }
        else if (b.KnockdownsScored > a.KnockdownsScored && composite < -5)
        {
            scoreA = 8;
            scoreB = 10;
        }
        else if (composite > 2.0)
        {
            scoreA = 10;
            scoreB = 9;
        }
        else if (composite < -2.0)
        {
            scoreA = 9;
            scoreB = 10;
        }
        else
        {
            scoreA = 10;
            scoreB = 10;
        }

        var rationale = BuildRationale(a, b, scoreA, scoreB, fighterA, fighterB);
        return new JudgeScorecard(judgeIndex, scoreA, scoreB, rationale);
    }

    /// <summary>
    /// Totals the round scorecards for one judge and returns the final card.
    /// </summary>
    public JudgeDecision TallyDecision(int judgeIndex, IReadOnlyList<JudgeScorecard> roundScores, Fighter fighterA, Fighter fighterB)
    {
        var totalA = roundScores.Where(s => s.JudgeIndex == judgeIndex).Sum(s => s.ScoreFighterA);
        var totalB = roundScores.Where(s => s.JudgeIndex == judgeIndex).Sum(s => s.ScoreFighterB);
        var winner = totalA >= totalB ? fighterA : fighterB;
        return new JudgeDecision(judgeIndex, totalA, totalB, winner);
    }

    /// <summary>
    /// Builds a short textual explanation for the round score.
    /// </summary>
    private static string BuildRationale(RoundStats a, RoundStats b, int scoreA, int scoreB, Fighter fighterA, Fighter fighterB)
    {
        if (scoreA == scoreB)
            return "Very close round, judge scores it 10-10.";

        var winnerIsA = scoreA > scoreB;
        var winnerName = winnerIsA ? fighterA.LastName : fighterB.LastName;
        var winnerStats = winnerIsA ? a : b;
        var loserStats = winnerIsA ? b : a;

        if (winnerStats.KnockdownsScored > loserStats.KnockdownsScored)
            return $"{winnerName} won the round with the knockdown and cleaner impact.";

        if (winnerStats.TakedownsLanded > loserStats.TakedownsLanded || winnerStats.SubmissionAttempts > loserStats.SubmissionAttempts)
            return $"{winnerName} edged the round with grappling control and positional threat.";

        var sigStrikeGap = winnerStats.SignificantStrikesLanded - loserStats.SignificantStrikesLanded;
        if (sigStrikeGap > 0)
            return $"{winnerName} led the round on cleaner striking, landing {winnerStats.SignificantStrikesLanded} significant strikes.";

        return $"{winnerName} had the stronger moments and overall impact in the round.";
    }
}
