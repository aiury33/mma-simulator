namespace MmaSimulator.Core.Models;

public sealed class Round
{
    public required int Number { get; init; }
    public required IReadOnlyList<FightEvent> Events { get; init; }
    public required RoundStats FighterAStats { get; init; }
    public required RoundStats FighterBStats { get; init; }
    public required IReadOnlyList<JudgeScorecard> Scorecards { get; init; }
}
