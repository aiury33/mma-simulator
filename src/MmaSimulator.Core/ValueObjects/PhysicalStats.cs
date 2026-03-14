namespace MmaSimulator.Core.ValueObjects;

public sealed record PhysicalStats(
    int HeightCm,
    int WeightLbs,
    int ReachCm,
    int Age)
{
    public double LegLengthRatio => HeightCm > 0 ? (HeightCm * 0.47) / HeightCm : 0.47;
}
