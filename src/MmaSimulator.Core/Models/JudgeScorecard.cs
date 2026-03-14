namespace MmaSimulator.Core.Models;

public sealed record JudgeScorecard(
    int JudgeIndex,
    int ScoreFighterA,
    int ScoreFighterB,
    string Rationale);
