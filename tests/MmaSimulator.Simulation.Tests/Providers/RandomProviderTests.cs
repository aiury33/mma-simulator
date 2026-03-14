using FluentAssertions;
using MmaSimulator.Simulation.Providers;

namespace MmaSimulator.Simulation.Tests.Providers;

public sealed class RandomProviderTests
{
    [Fact]
    public void Chance_WithZeroProbability_NeverReturnsTrue()
    {
        var provider = new SeededRandomProvider(42);

        for (var i = 0; i < 1000; i++)
            provider.Chance(0.0).Should().BeFalse();
    }

    [Fact]
    public void Chance_WithOneProbability_AlwaysReturnsTrue()
    {
        var provider = new SeededRandomProvider(42);

        for (var i = 0; i < 1000; i++)
            provider.Chance(1.0).Should().BeTrue();
    }

    [Fact]
    public void SeededProvider_WithSameSeed_ProducesSameSequence()
    {
        var p1 = new SeededRandomProvider(1234);
        var p2 = new SeededRandomProvider(1234);

        var seq1 = Enumerable.Range(0, 20).Select(_ => p1.NextDouble()).ToArray();
        var seq2 = Enumerable.Range(0, 20).Select(_ => p2.NextDouble()).ToArray();

        seq1.Should().BeEquivalentTo(seq2);
    }

    [Fact]
    public void SeededProvider_WithDifferentSeeds_ProduceDifferentSequences()
    {
        var p1 = new SeededRandomProvider(111);
        var p2 = new SeededRandomProvider(999);

        var seq1 = Enumerable.Range(0, 10).Select(_ => p1.NextDouble()).ToArray();
        var seq2 = Enumerable.Range(0, 10).Select(_ => p2.NextDouble()).ToArray();

        seq1.Should().NotBeEquivalentTo(seq2);
    }

    [Fact]
    public void Choose_FromSingleItemList_AlwaysReturnsThatItem()
    {
        var provider = new SeededRandomProvider(42);
        var list = new[] { "only" };

        for (var i = 0; i < 100; i++)
            provider.Choose(list).Should().Be("only");
    }

    [Fact]
    public void Choose_FromEmptyList_ThrowsArgumentException()
    {
        var provider = new SeededRandomProvider(42);

        var act = () => provider.Choose(Array.Empty<string>());

        act.Should().Throw<ArgumentException>();
    }
}
