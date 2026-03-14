namespace MmaSimulator.Core.Models;

public sealed record RoundStats(
    int SignificantStrikesLanded,
    int SignificantStrikesAttempted,
    int TotalStrikesLanded,
    int TakedownsLanded,
    int TakedownsAttempted,
    int SubmissionAttempts,
    int KnockdownsScored,
    double DamageTaken,
    double StaminaRemaining);
