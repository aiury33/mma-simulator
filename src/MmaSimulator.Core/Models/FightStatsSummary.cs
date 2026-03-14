namespace MmaSimulator.Core.Models;

public sealed record FightStatsSummary(
    int TotalSignificantStrikesA,
    int TotalSignificantStrikesB,
    int TakedownsA,
    int TakedownsB,
    int SubmissionAttemptsA,
    int SubmissionAttemptsB,
    int KnockdownsA,
    int KnockdownsB,
    double TotalDamageA,
    double TotalDamageB);
