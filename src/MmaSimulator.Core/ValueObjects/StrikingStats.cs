namespace MmaSimulator.Core.ValueObjects;

public sealed record StrikingStats(
    int Accuracy,
    int Power,
    int Speed,
    int Defense,
    int ChinDurability,
    int BodyDurability);
