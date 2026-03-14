using MmaSimulator.Core.Enums;

namespace MmaSimulator.Core.Models;

public sealed class FightResult
{
    public required Fight Fight { get; init; }
    public required IReadOnlyList<Round> Rounds { get; init; }
    public required Fighter Winner { get; init; }
    public Fighter? Loser { get; init; }
    public required FightResultMethod Method { get; init; }
    public required int FinishRound { get; init; }
    public required TimeSpan FinishTime { get; init; }
    public required IReadOnlyList<JudgeDecision> JudgeDecisions { get; init; }
    public required FightStatsSummary StatsSummary { get; init; }
}
