using MmaSimulator.Core.Interfaces;
using MmaSimulator.Core.Models;

namespace MmaSimulator.Simulation.Engines;

public sealed class JudgeScoringEngine : IJudgeScoringEngine
{
    private static readonly (double Striking, double Grappling, double Aggression, double Control)[] JudgeCriteriaWeights =
    [
        (0.40, 0.35, 0.15, 0.10),
        (0.45, 0.30, 0.10, 0.15),
        (0.35, 0.40, 0.15, 0.10)
    ];

    public JudgeScorecard ScoreRound(int judgeIndex, Round round, Fighter fighterA, Fighter fighterB)
    {
        var weights = JudgeCriteriaWeights[Math.Clamp(judgeIndex - 1, 0, 2)];
        var a = round.FighterAStats;
        var b = round.FighterBStats;

        var strikingDiff = (a.SignificantStrikesLanded - b.SignificantStrikesLanded)
            + (a.KnockdownsScored - b.KnockdownsScored) * 5;

        var grapplingDiff = (a.TakedownsLanded - b.TakedownsLanded) * 3
            + (a.SubmissionAttempts - b.SubmissionAttempts) * 2;

        var aggressionDiff = a.TotalStrikesLanded - b.TotalStrikesLanded;

        var controlDiff = a.TakedownsLanded - b.TakedownsLanded;

        var composite = weights.Striking * strikingDiff
            + weights.Grappling * grapplingDiff
            + weights.Aggression * aggressionDiff
            + weights.Control * controlDiff;

        int scoreA, scoreB;

        if (a.KnockdownsScored > 0 && composite > 3)
        {
            scoreA = 10;
            scoreB = 8;
        }
        else if (b.KnockdownsScored > 0 && composite < -3)
        {
            scoreA = 8;
            scoreB = 10;
        }
        else if (composite > 1.5)
        {
            scoreA = 10;
            scoreB = 9;
        }
        else if (composite < -1.5)
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

    public JudgeDecision TallyDecision(int judgeIndex, IReadOnlyList<JudgeScorecard> roundScores, Fighter fighterA, Fighter fighterB)
    {
        var totalA = roundScores.Where(s => s.JudgeIndex == judgeIndex).Sum(s => s.ScoreFighterA);
        var totalB = roundScores.Where(s => s.JudgeIndex == judgeIndex).Sum(s => s.ScoreFighterB);
        var winner = totalA >= totalB ? fighterA : fighterB;
        return new JudgeDecision(judgeIndex, totalA, totalB, winner);
    }

    private static string BuildRationale(RoundStats a, RoundStats b, int scoreA, int scoreB, Fighter fighterA, Fighter fighterB)
    {
        var winner = scoreA > scoreB ? fighterA.LastName : scoreB > scoreA ? fighterB.LastName : "Even";
        if (winner == "Even") return "Very close round, judge scores it 10-10.";
        return $"{winner} dominated with {Math.Max(a.SignificantStrikesLanded, b.SignificantStrikesLanded)} significant strikes.";
    }
}
