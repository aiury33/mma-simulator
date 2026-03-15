using FluentAssertions;
using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Interfaces;
using MmaSimulator.Core.Models;
using MmaSimulator.Core.ValueObjects;
using MmaSimulator.Simulation.DependencyInjection;
using MmaSimulator.Simulation.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace MmaSimulator.Simulation.Tests.Simulators;

public sealed class FightSimulatorTests
{
    private static IFightSimulator BuildSimulator(int? seed = null)
    {
        var services = new ServiceCollection()
            .AddSimulationServices(seed)
            .BuildServiceProvider();
        return services.GetRequiredService<IFightSimulator>();
    }

    private static Fight CreateFight(int rounds = 3) => new()
    {
        Id = Guid.NewGuid(),
        FighterA = FighterFixtures.CreateStriker(),
        FighterB = FighterFixtures.CreateWrestler(),
        NumberOfRounds = rounds,
        IsTitleFight = rounds == 5,
        WeightClass = WeightClass.Welterweight
    };

    [Fact]
    public void Simulate_AlwaysProducesNonNullResult()
    {
        var simulator = BuildSimulator(42);
        var result = simulator.Simulate(CreateFight(), new SimulationOptions());

        result.Should().NotBeNull();
        result.Winner.Should().NotBeNull();
    }

    [Fact]
    public void Simulate_TitleFight_HasAtMostFiveRounds()
    {
        var simulator = BuildSimulator(100);
        var fight = CreateFight(5);
        var result = simulator.Simulate(fight, new SimulationOptions());

        result.Rounds.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public void Simulate_ThreeRoundFight_HasAtMostThreeRounds()
    {
        var simulator = BuildSimulator(200);
        var result = simulator.Simulate(CreateFight(3), new SimulationOptions());

        result.Rounds.Count.Should().BeLessThanOrEqualTo(3);
    }

    [Fact]
    public void Simulate_WithSameSeed_ProducesSameWinner()
    {
        var fight = CreateFight();
        var options = new SimulationOptions(RandomSeed: 999);

        var r1 = BuildSimulator(999).Simulate(fight, options);
        var r2 = BuildSimulator(999).Simulate(fight, options);

        r1.Winner.Id.Should().Be(r2.Winner.Id);
        r1.Method.Should().Be(r2.Method);
        r1.FinishRound.Should().Be(r2.FinishRound);
    }

    [Fact]
    public void Simulate_StatsSummary_IsNonNegative()
    {
        var simulator = BuildSimulator(77);
        var result = simulator.Simulate(CreateFight(), new SimulationOptions());
        var s = result.StatsSummary;

        s.TotalSignificantStrikesA.Should().BeGreaterThanOrEqualTo(0);
        s.TotalSignificantStrikesB.Should().BeGreaterThanOrEqualTo(0);
        s.TakedownsA.Should().BeGreaterThanOrEqualTo(0);
        s.TakedownsB.Should().BeGreaterThanOrEqualTo(0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(500)]
    public void Simulate_ManySeeds_AlwaysProducesValidResult(int seed)
    {
        var fight = CreateFight();
        var options = new SimulationOptions(RandomSeed: seed);
        var result = BuildSimulator(seed).Simulate(fight, options);

        result.Winner.Should().NotBeNull();
        result.FinishRound.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(fight.NumberOfRounds);
        result.FinishTime.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public void Simulate_ExtremeSizeMismatch_FlyweightWinsOnlyRarely()
    {
        var flyweightGoat = CreateEliteFlyweight();
        var badHeavyweight = CreatePoorHeavyweight();
        var winsByFlyweight = 0;

        for (var seed = 1; seed <= 200; seed++)
        {
            var fight = new Fight
            {
                Id = Guid.NewGuid(),
                FighterA = flyweightGoat,
                FighterB = badHeavyweight,
                NumberOfRounds = 3,
                IsTitleFight = false,
                WeightClass = WeightClass.Heavyweight
            };

            var result = BuildSimulator(seed).Simulate(fight, new SimulationOptions(RandomSeed: seed));
            if (result.Winner.Id == flyweightGoat.Id)
                winsByFlyweight++;
        }

        winsByFlyweight.Should().BeLessThanOrEqualTo(3);
    }

    private static Fighter CreateEliteFlyweight() => new()
    {
        Id = Guid.NewGuid(),
        FirstName = "Mighty",
        LastName = "Mouse",
        Nickname = "GOAT",
        Nationality = "USA",
        WeightClass = WeightClass.Flyweight,
        PrimaryStyle = FightingStyle.MMAFighter,
        StyleProfiles =
        [
            new StyleProfile(FightingStyle.Boxer, 90, [StyleSpecialty.BoxingCombinations, StyleSpecialty.BoxingCountering, StyleSpecialty.BoxingPocketPressure]),
            new StyleProfile(FightingStyle.Wrestler, 94, [StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingTakedownDefense, StyleSpecialty.WrestlingControl]),
            new StyleProfile(FightingStyle.BJJPractitioner, 88, [StyleSpecialty.BjjFinisher, StyleSpecialty.BjjScrambles, StyleSpecialty.BjjControl])
        ],
        Stance = Stance.Orthodox,
        Physical = new PhysicalStats(160, 125, 168, 30),
        Striking = new StrikingStats(92, 74, 95, 90, 88, 86),
        Grappling = new GrapplingStats(94, 92, 90, 92, 92, 90),
        Athletics = new AthleticStats(98, 72, 96, 99, 92),
        Record = new FighterRecord(30, 3, 0)
    };

    private static Fighter CreatePoorHeavyweight() => new()
    {
        Id = Guid.NewGuid(),
        FirstName = "Sloppy",
        LastName = "Giant",
        Nickname = "Fringe",
        Nationality = "USA",
        WeightClass = WeightClass.Heavyweight,
        PrimaryStyle = FightingStyle.Striker,
        StyleProfiles =
        [
            new StyleProfile(FightingStyle.Boxer, 55, [StyleSpecialty.BoxingCombinations]),
            new StyleProfile(FightingStyle.Wrestler, 35, [StyleSpecialty.WrestlingTakedowns]),
            new StyleProfile(FightingStyle.BJJPractitioner, 28, [StyleSpecialty.BjjControl])
        ],
        Stance = Stance.Orthodox,
        Physical = new PhysicalStats(193, 245, 198, 34),
        Striking = new StrikingStats(58, 72, 54, 48, 58, 58),
        Grappling = new GrapplingStats(36, 42, 28, 36, 34, 38),
        Athletics = new AthleticStats(52, 74, 42, 48, 54),
        Record = new FighterRecord(5, 7, 0)
    };
}
