using MmaSimulator.Core.Enums;
using MmaSimulator.Core.ValueObjects;

namespace MmaSimulator.Core.Models;

public sealed class Fighter
{
    public required Guid Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Nickname { get; init; }
    public required string Nationality { get; init; }
    public required WeightClass WeightClass { get; init; }
    public required FightingStyle PrimaryStyle { get; init; }
    public FightingStyle? SecondaryStyle { get; init; }
    public required Stance Stance { get; init; }
    public required PhysicalStats Physical { get; init; }
    public required StrikingStats Striking { get; init; }
    public required GrapplingStats Grappling { get; init; }
    public required AthleticStats Athletics { get; init; }
    public required FighterRecord Record { get; init; }

    public string FullName => string.IsNullOrWhiteSpace(Nickname)
        ? $"{FirstName} {LastName}"
        : $"{FirstName} \"{Nickname}\" {LastName}";
}
