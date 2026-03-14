using MmaSimulator.Core.Interfaces;

namespace MmaSimulator.Simulation.Providers;

public sealed class SeededRandomProvider : IRandomProvider
{
    private readonly Random _random;

    public SeededRandomProvider(int seed)
    {
        _random = new Random(seed);
    }

    public double NextDouble() => _random.NextDouble();

    public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);

    public bool Chance(double probability) => _random.NextDouble() < Math.Clamp(probability, 0.0, 1.0);

    public T Choose<T>(IReadOnlyList<T> items)
    {
        if (items.Count == 0) throw new ArgumentException("Cannot choose from empty list.", nameof(items));
        return items[_random.Next(0, items.Count)];
    }
}
