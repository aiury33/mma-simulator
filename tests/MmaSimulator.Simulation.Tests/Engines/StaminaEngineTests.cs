using FluentAssertions;
using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Models;
using MmaSimulator.Simulation.Engines;
using MmaSimulator.Simulation.Tests.Fixtures;

namespace MmaSimulator.Simulation.Tests.Engines;

public sealed class StaminaEngineTests
{
    private readonly StaminaEngine _engine = new();

    [Fact]
    public void CalculateStaminaDrain_ForNonActor_ReturnsZero()
    {
        var fighter = FighterFixtures.CreateStriker();
        var otherFighter = FighterFixtures.CreateWrestler();
        var state = new FighterState(fighter);

        var ev = new FightEvent
        {
            Timestamp = TimeSpan.Zero,
            Type = FightEventType.StrikeLanded,
            Actor = otherFighter,
            PositionBefore = FightPosition.Standing,
            PositionAfter = FightPosition.Standing
        };

        var drain = _engine.CalculateStaminaDrain(state, ev);

        drain.Should().Be(0);
    }

    [Fact]
    public void CalculateStaminaDrain_MissedStrike_DrainMoreThanLandedStrike()
    {
        var fighter = FighterFixtures.CreateStriker();
        var state = new FighterState(fighter);

        var landedEvent = MakeEvent(FightEventType.StrikeLanded, fighter);
        var missedEvent = MakeEvent(FightEventType.StrikeMissed, fighter);

        var landedDrain = _engine.CalculateStaminaDrain(state, landedEvent);
        var missedDrain = _engine.CalculateStaminaDrain(state, missedEvent);

        missedDrain.Should().BeGreaterThan(landedDrain);
    }

    [Fact]
    public void CalculateStaminaDrain_TakedownAttempt_IsHigherThanStrike()
    {
        var fighter = FighterFixtures.CreateStriker();
        var state = new FighterState(fighter);

        var strikeDrain = _engine.CalculateStaminaDrain(state, MakeEvent(FightEventType.StrikeLanded, fighter));
        var tdDrain = _engine.CalculateStaminaDrain(state, MakeEvent(FightEventType.TakedownLanded, fighter));

        tdDrain.Should().BeGreaterThan(strikeDrain);
    }

    [Fact]
    public void CalculateRoundRecovery_HighCardioFighter_RecoverMoreThanLowCardio()
    {
        var highCardio = FighterFixtures.CreateStriker();
        var lowCardio = new MmaSimulator.Core.Models.Fighter
        {
            Id = Guid.NewGuid(), FirstName = "Low", LastName = "Cardio", Nickname = "tired",
            Nationality = "X",
            WeightClass = MmaSimulator.Core.Enums.WeightClass.Welterweight,
            PrimaryStyle = MmaSimulator.Core.Enums.FightingStyle.Striker,
            Stance = MmaSimulator.Core.Enums.Stance.Orthodox,
            Physical = new MmaSimulator.Core.ValueObjects.PhysicalStats(175, 170, 175, 28),
            Striking = new MmaSimulator.Core.ValueObjects.StrikingStats(70, 70, 70, 70, 70, 70),
            Grappling = new MmaSimulator.Core.ValueObjects.GrapplingStats(50, 50, 50, 50, 50, 50),
            Athletics = new MmaSimulator.Core.ValueObjects.AthleticStats(50, 50, 50, 30, 50),
            Record = new MmaSimulator.Core.Models.FighterRecord(5, 5, 0)
        };

        var stateHigh = new FighterState(highCardio) { CurrentStamina = 0.5 };
        var stateLow = new FighterState(lowCardio) { CurrentStamina = 0.5 };

        var recoveryHigh = _engine.CalculateRoundRecovery(stateHigh);
        var recoveryLow = _engine.CalculateRoundRecovery(stateLow);

        recoveryHigh.Should().BeGreaterThan(recoveryLow);
    }

    [Fact]
    public void CalculateRoundRecovery_WithBodyDamage_RecoverLess()
    {
        var fighter = FighterFixtures.CreateStriker();
        var cleanState = new FighterState(fighter) { AccumulatedBodyDamage = 0 };
        var damagedState = new FighterState(fighter) { AccumulatedBodyDamage = 40 };

        var cleanRecovery = _engine.CalculateRoundRecovery(cleanState);
        var damagedRecovery = _engine.CalculateRoundRecovery(damagedState);

        damagedRecovery.Should().BeLessThan(cleanRecovery);
    }

    private static FightEvent MakeEvent(FightEventType type, MmaSimulator.Core.Models.Fighter actor) =>
        new()
        {
            Timestamp = TimeSpan.Zero,
            Type = type,
            Actor = actor,
            PositionBefore = FightPosition.Standing,
            PositionAfter = FightPosition.Standing
        };
}
