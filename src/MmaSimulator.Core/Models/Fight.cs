using MmaSimulator.Core.Enums;

namespace MmaSimulator.Core.Models;

public sealed class Fight
{
    public required Guid Id { get; init; }
    public required Fighter FighterA { get; init; }
    public required Fighter FighterB { get; init; }
    public required int NumberOfRounds { get; init; }
    public required bool IsTitleFight { get; init; }
    public required WeightClass WeightClass { get; init; }
    public DateTime SimulatedAt { get; init; } = DateTime.UtcNow;
}
