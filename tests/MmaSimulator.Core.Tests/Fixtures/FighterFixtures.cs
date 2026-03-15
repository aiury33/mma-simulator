using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Models;
using MmaSimulator.Core.ValueObjects;

namespace MmaSimulator.Core.Tests.Fixtures;

public static class FighterFixtures
{
    public static Fighter CreateStriker(int accuracy = 85, int power = 80, int chin = 75, int age = 28) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = "Test",
        LastName = "Striker",
        Nickname = "The Puncher",
        Nationality = "USA",
        WeightClass = WeightClass.Welterweight,
        PrimaryStyle = FightingStyle.Striker,
        StyleProfiles =
        [
            new StyleProfile(
                FightingStyle.Striker,
                85,
                [StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks, StyleSpecialty.KarateDistance])
        ],
        Stance = Stance.Orthodox,
        Physical = new PhysicalStats(180, 170, 183, age),
        Striking = new StrikingStats(accuracy, power, 80, 70, chin, 70),
        Grappling = new GrapplingStats(50, 55, 45, 50, 48, 52),
        Athletics = new AthleticStats(80, 75, 80, 80, 75),
        Record = new FighterRecord(10, 2, 0)
    };

    public static Fighter CreateWrestler(int tdAccuracy = 88, int strength = 90, int age = 28) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = "Test",
        LastName = "Wrestler",
        Nickname = "The Grappler",
        Nationality = "USA",
        WeightClass = WeightClass.Welterweight,
        PrimaryStyle = FightingStyle.Wrestler,
        StyleProfiles =
        [
            new StyleProfile(
                FightingStyle.Wrestler,
                90,
                [StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl, StyleSpecialty.WrestlingTakedownDefense])
        ],
        Stance = Stance.Orthodox,
        Physical = new PhysicalStats(178, 170, 178, age),
        Striking = new StrikingStats(65, 68, 70, 65, 78, 78),
        Grappling = new GrapplingStats(tdAccuracy, 80, 72, 80, 88, 78),
        Athletics = new AthleticStats(88, strength, 78, 88, 82),
        Record = new FighterRecord(12, 1, 0)
    };

    public static Fighter CreateEliteFighter() => new()
    {
        Id = Guid.NewGuid(),
        FirstName = "Elite",
        LastName = "Champion",
        Nickname = "GOAT",
        Nationality = "USA",
        WeightClass = WeightClass.Welterweight,
        PrimaryStyle = FightingStyle.MMAFighter,
        StyleProfiles =
        [
            new StyleProfile(FightingStyle.Boxer, 88, [StyleSpecialty.BoxingCombinations, StyleSpecialty.BoxingCountering]),
            new StyleProfile(FightingStyle.Wrestler, 86, [StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingTakedownDefense]),
            new StyleProfile(FightingStyle.BJJPractitioner, 82, [StyleSpecialty.BjjControl, StyleSpecialty.BjjFinisher])
        ],
        Stance = Stance.Orthodox,
        Physical = new PhysicalStats(183, 170, 193, 30),
        Striking = new StrikingStats(92, 90, 92, 90, 90, 90),
        Grappling = new GrapplingStats(92, 90, 88, 88, 92, 88),
        Athletics = new AthleticStats(96, 92, 92, 96, 92),
        Record = new FighterRecord(25, 0, 0)
    };

    public static Fighter CreateNoviceFighter() => new()
    {
        Id = Guid.NewGuid(),
        FirstName = "Novice",
        LastName = "Fighter",
        Nickname = "The Beginner",
        Nationality = "USA",
        WeightClass = WeightClass.Welterweight,
        PrimaryStyle = FightingStyle.MMAFighter,
        StyleProfiles =
        [
            new StyleProfile(FightingStyle.Boxer, 45, [StyleSpecialty.BoxingCombinations]),
            new StyleProfile(FightingStyle.Wrestler, 42, [StyleSpecialty.WrestlingTakedowns]),
            new StyleProfile(FightingStyle.BJJPractitioner, 40, [StyleSpecialty.BjjControl])
        ],
        Stance = Stance.Orthodox,
        Physical = new PhysicalStats(175, 170, 175, 22),
        Striking = new StrikingStats(45, 50, 50, 42, 48, 48),
        Grappling = new GrapplingStats(42, 45, 40, 42, 44, 44),
        Athletics = new AthleticStats(55, 55, 55, 55, 50),
        Record = new FighterRecord(2, 3, 0)
    };
}
