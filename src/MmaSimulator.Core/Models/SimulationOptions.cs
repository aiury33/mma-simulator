namespace MmaSimulator.Core.Models;

public sealed record SimulationOptions(
    int? RandomSeed = null,
    bool VerboseEvents = true,
    double RandomnessFactor = 0.15);
