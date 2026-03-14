namespace MmaSimulator.Core.Interfaces;

/// <summary>
/// Abstraction over random number generation that enables fully deterministic,
/// seeded simulations and clean unit testing.
///
/// <para>Two implementations are provided:</para>
/// <list type="bullet">
///   <item><b>RandomProvider</b> — wraps <see cref="System.Random"/> with a new seed each run.</item>
///   <item><b>SeededRandomProvider</b> — accepts an explicit integer seed so that identical seeds
///       always produce identical fight outcomes. Used in integration tests and fight replays.</item>
/// </list>
///
/// <para>All simulation engines depend on this interface, never on concrete random types,
/// so swapping strategies requires no changes to business logic.</para>
/// </summary>
public interface IRandomProvider
{
    /// <summary>Returns a uniformly distributed <c>double</c> in [0.0, 1.0).</summary>
    double NextDouble();

    /// <summary>
    /// Returns a uniformly distributed integer in [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>).
    /// </summary>
    int Next(int minInclusive, int maxExclusive);

    /// <summary>
    /// Returns <c>true</c> with the given probability.
    /// </summary>
    /// <param name="probability">Value in [0.0, 1.0]. ≤ 0 always returns <c>false</c>;
    /// ≥ 1 always returns <c>true</c>.</param>
    bool Chance(double probability);

    /// <summary>
    /// Selects a uniformly random element from <paramref name="items"/>.
    /// </summary>
    /// <typeparam name="T">Element type.</typeparam>
    /// <param name="items">Non-empty list to pick from.</param>
    /// <returns>One element chosen uniformly at random.</returns>
    T Choose<T>(IReadOnlyList<T> items);
}
