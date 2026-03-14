namespace MmaSimulator.Core.Models;

public sealed record JudgeDecision(
    int JudgeIndex,
    int ScoreFighterA,
    int ScoreFighterB,
    Fighter PickedWinner);
