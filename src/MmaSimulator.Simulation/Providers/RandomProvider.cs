using MmaSimulator.Core.Interfaces;

namespace MmaSimulator.Simulation.Providers;

public sealed class RandomProvider : IRandomProvider
{
    private readonly Random _random = new();

    /// <summary>
    /// Returns a random floating-point value in the half-open interval [0, 1).
    /// </summary>
    public double NextDouble() => _random.NextDouble();

    /// <summary>
    /// Returns a random integer within the requested half-open interval.
    /// </summary>
    public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);

    /// <summary>
    /// Returns whether an event with the given probability occurs.
    /// </summary>
    public bool Chance(double probability) => _random.NextDouble() < Math.Clamp(probability, 0.0, 1.0);

    /// <summary>
    /// Chooses a random item from a non-empty list.
    /// </summary>
    public T Choose<T>(IReadOnlyList<T> items)
    {
        if (items.Count == 0) throw new ArgumentException("Cannot choose from empty list.", nameof(items));
        return items[_random.Next(0, items.Count)];
    }
}
