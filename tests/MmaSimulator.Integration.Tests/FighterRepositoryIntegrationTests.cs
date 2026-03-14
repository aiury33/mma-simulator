using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Interfaces;
using MmaSimulator.Simulation.DependencyInjection;

namespace MmaSimulator.Integration.Tests;

public sealed class FighterRepositoryIntegrationTests
{
    private static IFighterRepository BuildRepository() =>
        new ServiceCollection()
            .AddSimulationServices()
            .BuildServiceProvider()
            .GetRequiredService<IFighterRepository>();

    [Fact]
    public void GetAll_ReturnsAtLeastTenFighters()
    {
        var repo = BuildRepository();

        repo.GetAll().Count.Should().BeGreaterThanOrEqualTo(10);
    }

    [Theory]
    [InlineData(WeightClass.Lightweight)]
    [InlineData(WeightClass.Welterweight)]
    [InlineData(WeightClass.Middleweight)]
    [InlineData(WeightClass.Heavyweight)]
    public void GetByWeightClass_ReturnsAtLeastTwoFighters(WeightClass weightClass)
    {
        var repo = BuildRepository();

        repo.GetByWeightClass(weightClass).Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void GetByWeightClass_ReturnsOnlyMatchingClass()
    {
        var repo = BuildRepository();

        var lightweights = repo.GetByWeightClass(WeightClass.Lightweight);

        lightweights.Should().AllSatisfy(f => f.WeightClass.Should().Be(WeightClass.Lightweight));
    }

    [Fact]
    public void GetById_WithValidId_ReturnsFighter()
    {
        var repo = BuildRepository();
        var all = repo.GetAll();
        var target = all[0];

        var found = repo.GetById(target.Id);

        found.Should().NotBeNull();
        found!.Id.Should().Be(target.Id);
    }

    [Fact]
    public void GetById_WithInvalidId_ReturnsNull()
    {
        var repo = BuildRepository();

        var found = repo.GetById(Guid.NewGuid());

        found.Should().BeNull();
    }
}
