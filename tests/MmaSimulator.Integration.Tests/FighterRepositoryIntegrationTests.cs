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

    [Fact]
    public void GetAll_AllFightersHaveCompositeStyleProfiles()
    {
        var repo = BuildRepository();

        repo.GetAll().Should().AllSatisfy(fighter =>
        {
            fighter.StyleProfiles.Should().NotBeEmpty();
            fighter.GetStyleProficiency(fighter.PrimaryStyle).Should().BeGreaterThan(0);
        });
    }

    [Fact]
    public void KeyFighters_HaveExpectedStyleSubstyleProfiles()
    {
        var repo = BuildRepository();
        var all = repo.GetAll();

        var jonJones = all.Single(f => f.FirstName == "Jon" && f.LastName == "Jones");
        jonJones.GetStyleProficiency(FightingStyle.MuayThai).Should().BeGreaterThanOrEqualTo(90);
        jonJones.GetStyleProficiency(FightingStyle.Kickboxer).Should().BeGreaterThanOrEqualTo(85);
        jonJones.GetStyleProficiency(FightingStyle.Wrestler).Should().BeGreaterThanOrEqualTo(85);
        jonJones.HasSpecialty(StyleSpecialty.MuayThaiElbows).Should().BeTrue();
        jonJones.HasSpecialty(StyleSpecialty.MuayThaiKnees).Should().BeTrue();
        jonJones.HasSpecialty(StyleSpecialty.ObliqueKicks).Should().BeTrue();
        jonJones.HasSpecialty(StyleSpecialty.WrestlingTakedownDefense).Should().BeTrue();

        var alexPereira = all.Single(f => f.FirstName == "Alex" && f.LastName == "Pereira");
        alexPereira.GetStyleProficiency(FightingStyle.Kickboxer).Should().BeGreaterThanOrEqualTo(95);
        alexPereira.HasSpecialty(StyleSpecialty.KickboxingRange).Should().BeTrue();
        alexPereira.HasSpecialty(StyleSpecialty.KickboxingKicks).Should().BeTrue();

        var charles = all.Single(f => f.FirstName == "Charles" && f.LastName == "Oliveira");
        charles.GetStyleProficiency(FightingStyle.BJJPractitioner).Should().BeGreaterThanOrEqualTo(95);
        charles.GetStyleProficiency(FightingStyle.MuayThai).Should().BeGreaterThanOrEqualTo(80);
        charles.HasSpecialty(StyleSpecialty.BjjFinisher).Should().BeTrue();
        charles.HasSpecialty(StyleSpecialty.Armbar).Should().BeTrue();

        var khamzat = all.Single(f => f.FirstName == "Khamzat" && f.LastName == "Chimaev");
        khamzat.GetStyleProficiency(FightingStyle.Wrestler).Should().BeGreaterThanOrEqualTo(95);
        khamzat.HasSpecialty(StyleSpecialty.WrestlingTakedowns).Should().BeTrue();
        khamzat.HasSpecialty(StyleSpecialty.BjjFinisher).Should().BeTrue();

        var islam = all.Single(f => f.FirstName == "Islam" && f.LastName == "Makhachev");
        islam.HasSpecialty(StyleSpecialty.DarceChoke).Should().BeTrue();
        islam.HasSpecialty(StyleSpecialty.BjjGuardPassing).Should().BeTrue();
    }

    [Theory]
    [InlineData(WeightClass.Flyweight)]
    [InlineData(WeightClass.Bantamweight)]
    [InlineData(WeightClass.Featherweight)]
    [InlineData(WeightClass.Lightweight)]
    [InlineData(WeightClass.Welterweight)]
    [InlineData(WeightClass.Middleweight)]
    [InlineData(WeightClass.LightHeavyweight)]
    [InlineData(WeightClass.Heavyweight)]
    public void MensDivisions_HaveAtLeastSixteenRankedFighters(WeightClass weightClass)
    {
        var repo = BuildRepository();

        repo.GetByWeightClass(weightClass).Count.Should().BeGreaterThanOrEqualTo(16);
    }
}
