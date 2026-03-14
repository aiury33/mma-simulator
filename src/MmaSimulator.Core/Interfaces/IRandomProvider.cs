namespace MmaSimulator.Core.Interfaces;

public interface IRandomProvider
{
    double NextDouble();
    int Next(int minInclusive, int maxExclusive);
    bool Chance(double probability);
    T Choose<T>(IReadOnlyList<T> items);
}
