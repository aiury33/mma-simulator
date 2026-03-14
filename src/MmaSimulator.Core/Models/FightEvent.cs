using MmaSimulator.Core.Enums;

namespace MmaSimulator.Core.Models;

public sealed class FightEvent
{
    public required TimeSpan Timestamp { get; init; }
    public required FightEventType Type { get; init; }
    public required Fighter Actor { get; init; }
    public Fighter? Target { get; init; }
    public required FightPosition PositionBefore { get; init; }
    public required FightPosition PositionAfter { get; init; }
    public StrikeType? StrikeType { get; init; }
    public GrappleAction? GrappleAction { get; init; }
    public double DamageDealt { get; init; }
    public bool IsSignificant { get; init; }
    public string NarrationText { get; init; } = string.Empty;
}
