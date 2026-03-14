using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Interfaces;
using MmaSimulator.Core.Models;
using MmaSimulator.Simulation.DependencyInjection;

namespace MmaSimulator.Integration.Tests;

public sealed class FightSimulationIntegrationTests
{
    private static IServiceProvider BuildServices(int? seed = null) =>
        new ServiceCollection()
            .AddSimulationServices(seed)
            .BuildServiceProvider();

    private static Fight CreateFight(IFighterRepository repo, WeightClass wc = WeightClass.Lightweight, bool title = false)
    {
        var fighters = repo.GetByWeightClass(wc);
        return new Fight
        {
            Id = Guid.NewGuid(),
            FighterA = fighters[0],
            FighterB = fighters[1],
            NumberOfRounds = title ? 5 : 3,
            IsTitleFight = title,
            WeightClass = wc
        };
    }

    [Fact]
    public void FullFight_WithRealFighters_ShouldComplete()
    {
        var sp = BuildServices(42);
        var repo = sp.GetRequiredService<IFighterRepository>();
        var simulator = sp.GetRequiredService<IFightSimulator>();

        var fight = CreateFight(repo);
        var result = simulator.Simulate(fight, new SimulationOptions());

        result.Should().NotBeNull();
        result.Winner.Should().NotBeNull();
        result.Rounds.Should().NotBeEmpty();
        result.StatsSummary.Should().NotBeNull();
    }

    [Fact]
    public void FullFight_SeededRandom_IsReproducible()
    {
        var fight = CreateFight(BuildServices(1).GetRequiredService<IFighterRepository>());
        var options = new SimulationOptions(RandomSeed: 1234);

        var r1 = BuildServices(1234).GetRequiredService<IFightSimulator>().Simulate(fight, options);
        var r2 = BuildServices(1234).GetRequiredService<IFightSimulator>().Simulate(fight, options);

        r1.Winner.Id.Should().Be(r2.Winner.Id);
        r1.Method.Should().Be(r2.Method);
        r1.FinishRound.Should().Be(r2.FinishRound);
    }

    [Fact]
    public void FullFight_TitleFight_HasAtMostFiveRoundScorecards()
    {
        var sp = BuildServices(77);
        var repo = sp.GetRequiredService<IFighterRepository>();
        var simulator = sp.GetRequiredService<IFightSimulator>();

        var fight = CreateFight(repo, title: true);
        var result = simulator.Simulate(fight, new SimulationOptions(RandomSeed: 77));

        result.Rounds.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public void FullFight_100Seeds_NeverProducesInvalidState()
    {
        for (var seed = 1; seed <= 100; seed++)
        {
            var sp = BuildServices(seed);
            var repo = sp.GetRequiredService<IFighterRepository>();
            var simulator = sp.GetRequiredService<IFightSimulator>();

            var fight = CreateFight(repo);
            var result = simulator.Simulate(fight, new SimulationOptions(RandomSeed: seed));

            result.Winner.Should().NotBeNull($"seed {seed} should always produce a winner");
            result.FinishRound.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(3, $"seed {seed}");
            result.StatsSummary.TotalDamageA.Should().BeGreaterThanOrEqualTo(0, $"seed {seed}");
            result.StatsSummary.TotalDamageB.Should().BeGreaterThanOrEqualTo(0, $"seed {seed}");
        }
    }

    [Theory]
    [InlineData(WeightClass.Lightweight)]
    [InlineData(WeightClass.Welterweight)]
    [InlineData(WeightClass.Middleweight)]
    [InlineData(WeightClass.LightHeavyweight)]
    [InlineData(WeightClass.Heavyweight)]
    [InlineData(WeightClass.Featherweight)]
    [InlineData(WeightClass.Bantamweight)]
    [InlineData(WeightClass.Flyweight)]
    public void FullFight_AllWeightClasses_CompleteWithoutException(WeightClass weightClass)
    {
        var sp = BuildServices(42);
        var repo = sp.GetRequiredService<IFighterRepository>();
        var simulator = sp.GetRequiredService<IFightSimulator>();

        var fighters = repo.GetByWeightClass(weightClass);
        if (fighters.Count < 2) return;

        var fight = CreateFight(repo, weightClass);
        var act = () => simulator.Simulate(fight, new SimulationOptions(RandomSeed: 42));

        act.Should().NotThrow();
    }
}
