using FluentAssertions;
using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Models;
using MmaSimulator.Core.Tests.Fixtures;

namespace MmaSimulator.Core.Tests.Models;

public sealed class FighterStateTests
{
    [Fact]
    public void EffectiveStrikeAccuracy_AtFullStamina_EqualsBaseAccuracy()
    {
        var fighter = FighterFixtures.CreateStriker(accuracy: 80);
        var state = new FighterState(fighter) { CurrentStamina = 1.0 };

        state.EffectiveStrikeAccuracy.Should().BeApproximately(0.80, 0.01);
    }

    [Fact]
    public void EffectiveStrikeAccuracy_AtLowStamina_IsLowerThanBase()
    {
        var fighter = FighterFixtures.CreateStriker(accuracy: 80);
        var fullState = new FighterState(fighter) { CurrentStamina = 1.0 };
        var tiredState = new FighterState(fighter) { CurrentStamina = 0.2 };

        tiredState.EffectiveStrikeAccuracy.Should().BeLessThan(fullState.EffectiveStrikeAccuracy);
    }

    [Fact]
    public void EffectiveDamageMultiplier_InMountPosition_IsHigherThanStanding()
    {
        var fighter = FighterFixtures.CreateStriker();
        var standingState = new FighterState(fighter) { CurrentPosition = FightPosition.Standing };
        var mountState = new FighterState(fighter) { CurrentPosition = FightPosition.MountTop };

        mountState.EffectiveDamageMultiplier.Should().BeGreaterThan(standingState.EffectiveDamageMultiplier);
    }

    [Fact]
    public void InitialStamina_IsOne()
    {
        var state = new FighterState(FighterFixtures.CreateStriker());

        state.CurrentStamina.Should().Be(1.0);
    }

    [Fact]
    public void InitialPosition_IsStanding()
    {
        var state = new FighterState(FighterFixtures.CreateStriker());

        state.CurrentPosition.Should().Be(FightPosition.Standing);
    }
}
