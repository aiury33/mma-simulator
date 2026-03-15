using MmaSimulator.Core.Enums;
using MmaSimulator.Core.Models;
using MmaSimulator.Core.ValueObjects;

namespace MmaSimulator.Simulation.Data;

// Roster: UFC top 15 per weight class as of March 2026
// Stats guide:  StrikingStats(accuracy, power, speed, defense, chinDurability, bodyDurability)
//               GrapplingStats(takedownAcc, takedownDef, subOffense, subDefense, groundControl, clinchwork)
//               AthleticStats(stamina, strength, agility, cardio, toughness)
internal static class FighterData
{
    /// <summary>
    /// Creates a read-only list of style profiles for a fighter definition.
    /// </summary>
    private static IReadOnlyList<StyleProfile> Styles(params StyleProfile[] profiles) => profiles;

    /// <summary>
    /// Creates a style profile with its proficiency and specialties.
    /// </summary>
    private static StyleProfile Profile(FightingStyle style, int proficiency, params StyleSpecialty[] specialties) =>
        new(style, proficiency, specialties);

    /// <summary>
    /// Builds a ranked fighter entry and derives attribute groups from physical data, overall level, and style profiles.
    /// </summary>
    private static Fighter RankedFighter(
        string id,
        string firstName,
        string lastName,
        string nickname,
        string nationality,
        WeightClass weightClass,
        FightingStyle primaryStyle,
        Stance stance,
        int heightCm,
        int weightLbs,
        int reachCm,
        int age,
        FighterRecord record,
        int overall,
        params StyleProfile[] profiles)
    {
        var styleProfiles = Styles(profiles);
        var strikingFocus = MaxStyle(styleProfiles, FightingStyle.Boxer, FightingStyle.Kickboxer, FightingStyle.MuayThai, FightingStyle.Striker, FightingStyle.MMAFighter);
        var grapplingFocus = MaxStyle(styleProfiles, FightingStyle.Wrestler, FightingStyle.BJJPractitioner, FightingStyle.Judoka, FightingStyle.MMAFighter);
        var lightweightBonus = weightLbs <= 155 ? 5 : weightLbs <= 170 ? 3 : weightLbs <= 185 ? 1 : -2;
        var heavyweightPenalty = weightLbs >= 235 ? 4 : 0;

        var strikingAccuracy = Clamp(
            overall
            + strikingFocus / 10
            + MaxSpecialty(styleProfiles, StyleSpecialty.BoxingCombinations, StyleSpecialty.BoxingCountering, StyleSpecialty.KickboxingRange, StyleSpecialty.KarateDistance) / 18
            - 4,
            60, 96);

        var strikingPower = Clamp(
            overall
            + strikingFocus / 11
            + MaxSpecialty(styleProfiles, StyleSpecialty.BoxingPocketPressure, StyleSpecialty.KickboxingPressure, StyleSpecialty.KickboxingKicks, StyleSpecialty.MuayThaiElbows, StyleSpecialty.MuayThaiKnees) / 15
            + (weightLbs - 145) / 12,
            58, 98);

        var strikingSpeed = Clamp(
            overall
            + strikingFocus / 10
            + lightweightBonus
            - heavyweightPenalty
            + MaxSpecialty(styleProfiles, StyleSpecialty.KickboxingRange, StyleSpecialty.KarateDistance, StyleSpecialty.TaekwondoKicks) / 18
            - 6,
            60, 98);

        var strikingDefense = Clamp(
            overall
            + strikingFocus / 12
            + MaxSpecialty(styleProfiles, StyleSpecialty.BoxingCountering, StyleSpecialty.WrestlingTakedownDefense, StyleSpecialty.KarateDistance) / 18
            - 4,
            58, 96);

        var chin = Clamp(overall + (weightLbs - 145) / 10 - 2, 62, 94);
        var body = Clamp(overall + (weightLbs - 145) / 12 - 1, 62, 94);

        var takedownAcc = Clamp(
            overall
            + grapplingFocus / 10
            + MaxSpecialty(styleProfiles, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.JudoTripsThrows) / 14
            - 10,
            45, 96);

        var takedownDef = Clamp(
            overall
            + grapplingFocus / 11
            + MaxSpecialty(styleProfiles, StyleSpecialty.WrestlingTakedownDefense, StyleSpecialty.WrestlingControl) / 14
            - 8,
            48, 96);

        var subOffense = Clamp(
            overall
            + MaxStyle(styleProfiles, FightingStyle.BJJPractitioner, FightingStyle.MMAFighter) / 11
            + MaxSpecialty(styleProfiles, StyleSpecialty.BjjFinisher, StyleSpecialty.BjjControl) / 14
            - 10,
            42, 98);

        var subDefense = Clamp(
            overall
            + grapplingFocus / 12
            + MaxSpecialty(styleProfiles, StyleSpecialty.BjjScrambles, StyleSpecialty.WrestlingTakedownDefense) / 15
            - 8,
            45, 96);

        var groundControl = Clamp(
            overall
            + grapplingFocus / 10
            + MaxSpecialty(styleProfiles, StyleSpecialty.WrestlingControl, StyleSpecialty.BjjControl) / 14
            - 8,
            45, 96);

        var clinchwork = Clamp(
            overall
            + MaxStyle(styleProfiles, FightingStyle.MuayThai, FightingStyle.Judoka, FightingStyle.Wrestler) / 11
            + MaxSpecialty(styleProfiles, StyleSpecialty.MuayThaiClinch, StyleSpecialty.JudoTripsThrows) / 14
            - 8,
            42, 94);

        var stamina = Clamp(overall + lightweightBonus + 2, 64, 98);
        var strength = Clamp(overall + (weightLbs - 145) / 8 + grapplingFocus / 16 - 2, 60, 98);
        var agility = Clamp(overall + lightweightBonus + strikingFocus / 18 - 4, 58, 98);
        var cardio = Clamp(overall + lightweightBonus + 2, 62, 98);
        var toughness = Clamp(overall + (weightLbs - 145) / 12, 62, 96);

        return new Fighter
        {
            Id = new Guid(id),
            FirstName = firstName,
            LastName = lastName,
            Nickname = nickname,
            Nationality = nationality,
            WeightClass = weightClass,
            PrimaryStyle = primaryStyle,
            StyleProfiles = styleProfiles,
            Stance = stance,
            Physical = new PhysicalStats(heightCm, weightLbs, reachCm, age),
            Striking = new StrikingStats(strikingAccuracy, strikingPower, strikingSpeed, strikingDefense, chin, body),
            Grappling = new GrapplingStats(takedownAcc, takedownDef, subOffense, subDefense, groundControl, clinchwork),
            Athletics = new AthleticStats(stamina, strength, agility, cardio, toughness),
            Record = record
        };
    }

    /// <summary>
    /// Returns the highest proficiency among the requested styles.
    /// </summary>
    private static int MaxStyle(IReadOnlyList<StyleProfile> profiles, params FightingStyle[] styles) =>
        profiles.Where(profile => styles.Contains(profile.Style)).Select(profile => profile.Proficiency).DefaultIfEmpty(0).Max();

    /// <summary>
    /// Returns the highest proficiency among profiles that contain any of the requested specialties.
    /// </summary>
    private static int MaxSpecialty(IReadOnlyList<StyleProfile> profiles, params StyleSpecialty[] specialties) =>
        profiles.Where(profile => profile.Specialties.Any(specialties.Contains)).Select(profile => profile.Proficiency).DefaultIfEmpty(0).Max();

    /// <summary>
    /// Clamps a derived attribute value into the allowed roster range.
    /// </summary>
    private static int Clamp(int value, int min, int max) => Math.Clamp(value, min, max);

    internal static readonly IReadOnlyList<Fighter> All =
    [
        // ──────────────── FLYWEIGHT (125 lbs) ────────────────
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000001"),
            FirstName = "Alexandre", LastName = "Pantoja", Nickname = "The Cannibal",
            Nationality = "Brazil", WeightClass = WeightClass.Flyweight,
            PrimaryStyle = FightingStyle.BJJPractitioner, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.BJJPractitioner, 95, StyleSpecialty.BjjFinisher, StyleSpecialty.BjjControl, StyleSpecialty.BjjScrambles),
                Profile(FightingStyle.Wrestler, 82, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl, StyleSpecialty.WrestlingTakedownDefense),
                Profile(FightingStyle.Boxer, 74, StyleSpecialty.BoxingPocketPressure, StyleSpecialty.BoxingCombinations)),
            Physical = new PhysicalStats(168, 125, 168, 33),
            Striking  = new StrikingStats(76, 72, 84, 74, 94, 83),
            Grappling = new GrapplingStats(78, 76, 92, 84, 86, 78),
            Athletics = new AthleticStats(88, 74, 86, 90, 82),
            Record = new FighterRecord(27, 5, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000002"),
            FirstName = "Brandon", LastName = "Royval", Nickname = "Raw Dawg",
            Nationality = "USA", WeightClass = WeightClass.Flyweight,
            PrimaryStyle = FightingStyle.BJJPractitioner, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.BJJPractitioner, 90, StyleSpecialty.BjjFinisher, StyleSpecialty.BjjScrambles, StyleSpecialty.BjjControl),
                Profile(FightingStyle.MuayThai, 76, StyleSpecialty.MuayThaiKnees, StyleSpecialty.MuayThaiElbows, StyleSpecialty.MuayThaiClinch),
                Profile(FightingStyle.Boxer, 70, StyleSpecialty.BoxingCombinations)),
            Physical = new PhysicalStats(175, 125, 178, 31),
            Striking  = new StrikingStats(74, 76, 86, 68, 72, 74),
            Grappling = new GrapplingStats(76, 72, 88, 80, 80, 74),
            Athletics = new AthleticStats(88, 72, 88, 88, 76),
            Record = new FighterRecord(16, 6, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000003"),
            FirstName = "Steve", LastName = "Erceg", Nickname = "The Hammer",
            Nationality = "Australia", WeightClass = WeightClass.Flyweight,
            PrimaryStyle = FightingStyle.MMAFighter, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.Boxer, 78, StyleSpecialty.BoxingCombinations, StyleSpecialty.BoxingCountering),
                Profile(FightingStyle.Wrestler, 72, StyleSpecialty.WrestlingTakedownDefense, StyleSpecialty.WrestlingControl),
                Profile(FightingStyle.BJJPractitioner, 68, StyleSpecialty.BjjControl)),
            Physical = new PhysicalStats(170, 125, 170, 29),
            Striking  = new StrikingStats(74, 72, 80, 72, 76, 74),
            Grappling = new GrapplingStats(74, 72, 76, 74, 74, 72),
            Athletics = new AthleticStats(84, 72, 80, 84, 80),
            Record = new FighterRecord(11, 2, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000004"),
            FirstName = "Amir", LastName = "Albazi", Nickname = "The Prince",
            Nationality = "Sweden", WeightClass = WeightClass.Flyweight,
            PrimaryStyle = FightingStyle.BJJPractitioner, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.BJJPractitioner, 88, StyleSpecialty.BjjFinisher, StyleSpecialty.BjjControl),
                Profile(FightingStyle.Wrestler, 80, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl, StyleSpecialty.WrestlingTakedownDefense)),
            Physical = new PhysicalStats(170, 125, 175, 30),
            Striking  = new StrikingStats(72, 68, 78, 74, 76, 74),
            Grappling = new GrapplingStats(80, 78, 86, 82, 82, 76),
            Athletics = new AthleticStats(86, 74, 84, 86, 80),
            Record = new FighterRecord(17, 1, 0)
        },

        // ──────────────── BANTAMWEIGHT (135 lbs) ────────────────
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000005"),
            FirstName = "Merab", LastName = "Dvalishvili", Nickname = "The Machine",
            Nationality = "Georgia", WeightClass = WeightClass.Bantamweight,
            PrimaryStyle = FightingStyle.Wrestler, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.Wrestler, 96, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl, StyleSpecialty.WrestlingTakedownDefense),
                Profile(FightingStyle.Boxer, 62, StyleSpecialty.BoxingCombinations)),
            Physical = new PhysicalStats(175, 135, 173, 33),
            Striking  = new StrikingStats(68, 65, 80, 70, 80, 72),
            Grappling = new GrapplingStats(92, 84, 72, 82, 90, 84),
            Athletics = new AthleticStats(98, 80, 86, 100, 86),
            Record = new FighterRecord(18, 4, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000006"),
            FirstName = "Sean", LastName = "O'Malley", Nickname = "Sugar",
            Nationality = "USA", WeightClass = WeightClass.Bantamweight,
            PrimaryStyle = FightingStyle.Striker, Stance = Stance.Southpaw,
            StyleProfiles = Styles(
                Profile(FightingStyle.Kickboxer, 88, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks),
                Profile(FightingStyle.Boxer, 84, StyleSpecialty.BoxingCountering, StyleSpecialty.BoxingCombinations),
                Profile(FightingStyle.Striker, 86, StyleSpecialty.KarateDistance, StyleSpecialty.TaekwondoKicks)),
            Physical = new PhysicalStats(178, 135, 180, 29),
            Striking  = new StrikingStats(86, 82, 88, 76, 70, 72),
            Grappling = new GrapplingStats(54, 62, 52, 60, 54, 58),
            Athletics = new AthleticStats(82, 68, 92, 84, 72),
            Record = new FighterRecord(17, 2, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000007"),
            FirstName = "Umar", LastName = "Nurmagomedov", Nickname = "The Predator",
            Nationality = "Russia", WeightClass = WeightClass.Bantamweight,
            PrimaryStyle = FightingStyle.Wrestler, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.Wrestler, 94, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingTakedownDefense, StyleSpecialty.WrestlingControl),
                Profile(FightingStyle.Kickboxer, 74, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks),
                Profile(FightingStyle.BJJPractitioner, 72, StyleSpecialty.BjjControl)),
            Physical = new PhysicalStats(173, 135, 175, 27),
            Striking  = new StrikingStats(74, 72, 78, 76, 86, 82),
            Grappling = new GrapplingStats(90, 88, 80, 86, 90, 84),
            Athletics = new AthleticStats(92, 88, 82, 92, 88),
            Record = new FighterRecord(18, 0, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000008"),
            FirstName = "Cory", LastName = "Sandhagen", Nickname = "The Sandman",
            Nationality = "USA", WeightClass = WeightClass.Bantamweight,
            PrimaryStyle = FightingStyle.MMAFighter, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.Kickboxer, 82, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks),
                Profile(FightingStyle.Striker, 84, StyleSpecialty.KarateDistance, StyleSpecialty.TaekwondoKicks),
                Profile(FightingStyle.Wrestler, 66, StyleSpecialty.WrestlingTakedownDefense)),
            Physical = new PhysicalStats(183, 135, 183, 32),
            Striking  = new StrikingStats(82, 74, 86, 76, 74, 76),
            Grappling = new GrapplingStats(68, 70, 64, 68, 66, 66),
            Athletics = new AthleticStats(88, 74, 88, 90, 80),
            Record = new FighterRecord(17, 4, 0)
        },

        // ──────────────── FEATHERWEIGHT (145 lbs) ────────────────
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000009"),
            FirstName = "Ilia", LastName = "Topuria", Nickname = "El Matador",
            Nationality = "Georgia", WeightClass = WeightClass.Lightweight,
            PrimaryStyle = FightingStyle.Boxer, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.Boxer, 96, StyleSpecialty.BoxingPocketPressure, StyleSpecialty.BoxingCombinations, StyleSpecialty.BoxingCountering),
                Profile(FightingStyle.BJJPractitioner, 76, StyleSpecialty.BjjControl, StyleSpecialty.BjjFinisher),
                Profile(FightingStyle.Wrestler, 72, StyleSpecialty.WrestlingTakedownDefense)),
            Physical = new PhysicalStats(170, 145, 170, 27),
            Striking  = new StrikingStats(90, 94, 86, 85, 84, 82),
            Grappling = new GrapplingStats(77, 85, 74, 76, 72, 76),
            Athletics = new AthleticStats(88, 86, 88, 88, 88),
            Record = new FighterRecord(16, 0, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000010"),
            FirstName = "Max", LastName = "Holloway", Nickname = "Blessed",
            Nationality = "USA", WeightClass = WeightClass.Lightweight,
            PrimaryStyle = FightingStyle.Boxer, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.Boxer, 92, StyleSpecialty.BoxingCombinations, StyleSpecialty.BoxingPocketPressure),
                Profile(FightingStyle.Kickboxer, 72, StyleSpecialty.KickboxingPressure, StyleSpecialty.KickboxingKicks)),
            Physical = new PhysicalStats(180, 145, 175, 33),
            Striking  = new StrikingStats(82, 72, 90, 74, 76, 78),
            Grappling = new GrapplingStats(68, 74, 64, 68, 66, 70),
            Athletics = new AthleticStats(94, 72, 88, 96, 84),
            Record = new FighterRecord(25, 7, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000011"),
            FirstName = "Brian", LastName = "Ortega", Nickname = "T-City",
            Nationality = "USA", WeightClass = WeightClass.Featherweight,
            PrimaryStyle = FightingStyle.BJJPractitioner, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.BJJPractitioner, 96, StyleSpecialty.BjjFinisher, StyleSpecialty.BjjControl, StyleSpecialty.BjjScrambles),
                Profile(FightingStyle.Boxer, 72, StyleSpecialty.BoxingCountering),
                Profile(FightingStyle.MuayThai, 70, StyleSpecialty.MuayThaiKnees, StyleSpecialty.MuayThaiClinch)),
            Physical = new PhysicalStats(175, 145, 180, 33),
            Striking  = new StrikingStats(76, 78, 74, 72, 84, 76),
            Grappling = new GrapplingStats(76, 72, 94, 86, 86, 78),
            Athletics = new AthleticStats(82, 76, 76, 82, 86),
            Record = new FighterRecord(16, 2, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000012"),
            FirstName = "Alexander", LastName = "Volkanovski", Nickname = "The Great",
            Nationality = "Australia", WeightClass = WeightClass.Featherweight,
            PrimaryStyle = FightingStyle.MMAFighter, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.Boxer, 86, StyleSpecialty.BoxingCombinations, StyleSpecialty.BoxingCountering),
                Profile(FightingStyle.Wrestler, 84, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingTakedownDefense, StyleSpecialty.WrestlingControl),
                Profile(FightingStyle.Kickboxer, 80, StyleSpecialty.KickboxingKicks, StyleSpecialty.KickboxingPressure)),
            Physical = new PhysicalStats(168, 145, 182, 36),
            Striking  = new StrikingStats(84, 78, 84, 80, 82, 82),
            Grappling = new GrapplingStats(84, 92, 72, 78, 80, 80),
            Athletics = new AthleticStats(92, 84, 86, 90, 88),
            Record = new FighterRecord(26, 4, 0)
        },

        // ──────────────── LIGHTWEIGHT (155 lbs) ────────────────
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000013"),
            FirstName = "Islam", LastName = "Makhachev", Nickname = "The Eagle's Heir",
            Nationality = "Russia", WeightClass = WeightClass.Welterweight,
            PrimaryStyle = FightingStyle.Wrestler, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.Wrestler, 96, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingDoubleLeg, StyleSpecialty.WrestlingControl, StyleSpecialty.WrestlingTakedownDefense, StyleSpecialty.GroundAndPoundPunches),
                Profile(FightingStyle.BJJPractitioner, 86, StyleSpecialty.BjjControl, StyleSpecialty.BjjFinisher, StyleSpecialty.BjjGuardPassing, StyleSpecialty.DarceChoke, StyleSpecialty.ArmTriangleChoke),
                Profile(FightingStyle.Boxer, 68, StyleSpecialty.BoxingCountering)),
            Physical = new PhysicalStats(178, 155, 178, 33),
            Striking  = new StrikingStats(74, 74, 74, 76, 84, 82),
            Grappling = new GrapplingStats(94, 90, 84, 88, 93, 88),
            Athletics = new AthleticStats(94, 92, 82, 94, 90),
            Record = new FighterRecord(26, 1, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000014"),
            FirstName = "Charles", LastName = "Oliveira", Nickname = "Do Bronx",
            Nationality = "Brazil", WeightClass = WeightClass.Lightweight,
            PrimaryStyle = FightingStyle.BJJPractitioner, Stance = Stance.Orthodox,
            SecondaryStyle = FightingStyle.MuayThai,
            StyleProfiles = Styles(
                Profile(FightingStyle.BJJPractitioner, 98, StyleSpecialty.BjjFinisher, StyleSpecialty.BjjControl, StyleSpecialty.BjjScrambles, StyleSpecialty.RearNakedChoke, StyleSpecialty.Armbar, StyleSpecialty.TriangleChoke),
                Profile(FightingStyle.MuayThai, 82, StyleSpecialty.MuayThaiKnees, StyleSpecialty.MuayThaiElbows, StyleSpecialty.MuayThaiClinch)),
            Physical = new PhysicalStats(178, 155, 185, 35),
            Striking  = new StrikingStats(78, 83, 78, 68, 70, 70),
            Grappling = new GrapplingStats(82, 76, 96, 90, 88, 82),
            Athletics = new AthleticStats(86, 78, 82, 88, 78),
            Record = new FighterRecord(34, 9, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000015"),
            FirstName = "Arman", LastName = "Tsarukyan", Nickname = "Ahalkalakets",
            Nationality = "Armenia", WeightClass = WeightClass.Lightweight,
            PrimaryStyle = FightingStyle.Wrestler, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.Wrestler, 90, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl, StyleSpecialty.WrestlingTakedownDefense),
                Profile(FightingStyle.Boxer, 76, StyleSpecialty.BoxingCombinations),
                Profile(FightingStyle.Kickboxer, 72, StyleSpecialty.KickboxingKicks)),
            Physical = new PhysicalStats(175, 155, 178, 28),
            Striking  = new StrikingStats(76, 76, 80, 74, 82, 80),
            Grappling = new GrapplingStats(86, 82, 74, 80, 84, 80),
            Athletics = new AthleticStats(90, 84, 82, 92, 84),
            Record = new FighterRecord(22, 3, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000016"),
            FirstName = "Dustin", LastName = "Poirier", Nickname = "The Diamond",
            Nationality = "USA", WeightClass = WeightClass.Lightweight,
            PrimaryStyle = FightingStyle.MMAFighter, Stance = Stance.Southpaw,
            StyleProfiles = Styles(
                Profile(FightingStyle.Boxer, 88, StyleSpecialty.BoxingPocketPressure, StyleSpecialty.BoxingCombinations, StyleSpecialty.BoxingCountering),
                Profile(FightingStyle.BJJPractitioner, 72, StyleSpecialty.BjjFinisher),
                Profile(FightingStyle.Kickboxer, 72, StyleSpecialty.KickboxingKicks)),
            Physical = new PhysicalStats(175, 155, 182, 36),
            Striking  = new StrikingStats(82, 84, 80, 72, 74, 80),
            Grappling = new GrapplingStats(72, 70, 74, 76, 72, 74),
            Athletics = new AthleticStats(86, 80, 82, 86, 86),
            Record = new FighterRecord(30, 8, 0)
        },

        // ──────────────── WELTERWEIGHT (170 lbs) ────────────────
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000017"),
            FirstName = "Belal", LastName = "Muhammad", Nickname = "Remember The Name",
            Nationality = "USA", WeightClass = WeightClass.Welterweight,
            PrimaryStyle = FightingStyle.Wrestler, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.Wrestler, 90, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl, StyleSpecialty.WrestlingTakedownDefense),
                Profile(FightingStyle.Boxer, 74, StyleSpecialty.BoxingCombinations, StyleSpecialty.BoxingCountering)),
            Physical = new PhysicalStats(178, 170, 183, 36),
            Striking  = new StrikingStats(70, 68, 74, 76, 82, 80),
            Grappling = new GrapplingStats(88, 82, 70, 78, 86, 82),
            Athletics = new AthleticStats(94, 82, 78, 96, 84),
            Record = new FighterRecord(24, 3, 1)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000018"),
            FirstName = "Leon", LastName = "Edwards", Nickname = "Rocky",
            Nationality = "UK", WeightClass = WeightClass.Welterweight,
            PrimaryStyle = FightingStyle.Striker, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.Kickboxer, 82, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks),
                Profile(FightingStyle.MuayThai, 80, StyleSpecialty.MuayThaiKnees, StyleSpecialty.MuayThaiElbows, StyleSpecialty.MuayThaiClinch),
                Profile(FightingStyle.Wrestler, 70, StyleSpecialty.WrestlingTakedownDefense)),
            Physical = new PhysicalStats(183, 170, 185, 32),
            Striking  = new StrikingStats(80, 76, 80, 78, 80, 76),
            Grappling = new GrapplingStats(72, 76, 65, 72, 68, 74),
            Athletics = new AthleticStats(88, 78, 86, 90, 82),
            Record = new FighterRecord(22, 3, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000019"),
            FirstName = "Shavkat", LastName = "Rakhmonov", Nickname = "Nomad",
            Nationality = "Kazakhstan", WeightClass = WeightClass.Welterweight,
            PrimaryStyle = FightingStyle.MMAFighter, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.MuayThai, 84, StyleSpecialty.MuayThaiKnees, StyleSpecialty.MuayThaiClinch),
                Profile(FightingStyle.Wrestler, 84, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl),
                Profile(FightingStyle.BJJPractitioner, 82, StyleSpecialty.BjjFinisher, StyleSpecialty.BjjControl)),
            Physical = new PhysicalStats(183, 170, 185, 29),
            Striking  = new StrikingStats(80, 88, 82, 74, 84, 80),
            Grappling = new GrapplingStats(82, 80, 84, 80, 82, 80),
            Athletics = new AthleticStats(90, 88, 84, 90, 88),
            Record = new FighterRecord(18, 0, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000020"),
            FirstName = "Jack", LastName = "Della Maddalena", Nickname = "The Highlight",
            Nationality = "Australia", WeightClass = WeightClass.Welterweight,
            PrimaryStyle = FightingStyle.Boxer, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.Boxer, 92, StyleSpecialty.BoxingPocketPressure, StyleSpecialty.BoxingCombinations, StyleSpecialty.BoxingCountering),
                Profile(FightingStyle.Kickboxer, 68, StyleSpecialty.KickboxingKicks)),
            Physical = new PhysicalStats(183, 170, 185, 27),
            Striking  = new StrikingStats(82, 90, 82, 72, 80, 76),
            Grappling = new GrapplingStats(64, 68, 58, 64, 60, 62),
            Athletics = new AthleticStats(86, 84, 82, 86, 80),
            Record = new FighterRecord(16, 2, 0)
        },

        // ──────────────── MIDDLEWEIGHT (185 lbs) ────────────────
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000021"),
            FirstName = "Dricus", LastName = "du Plessis", Nickname = "Stillknocks",
            Nationality = "South Africa", WeightClass = WeightClass.Middleweight,
            PrimaryStyle = FightingStyle.MMAFighter, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.Kickboxer, 80, StyleSpecialty.KickboxingPressure, StyleSpecialty.KickboxingKicks),
                Profile(FightingStyle.Wrestler, 78, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl),
                Profile(FightingStyle.BJJPractitioner, 74, StyleSpecialty.BjjControl)),
            Physical = new PhysicalStats(183, 185, 186, 30),
            Striking  = new StrikingStats(78, 84, 80, 76, 88, 86),
            Grappling = new GrapplingStats(76, 78, 78, 76, 78, 80),
            Athletics = new AthleticStats(90, 86, 82, 90, 92),
            Record = new FighterRecord(22, 2, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000022"),
            FirstName = "Israel", LastName = "Adesanya", Nickname = "The Last Stylebender",
            Nationality = "Nigeria", WeightClass = WeightClass.Middleweight,
            PrimaryStyle = FightingStyle.Kickboxer, Stance = Stance.Switch,
            StyleProfiles = Styles(
                Profile(FightingStyle.Kickboxer, 96, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks, StyleSpecialty.KickboxingPressure),
                Profile(FightingStyle.Striker, 90, StyleSpecialty.KarateDistance, StyleSpecialty.TaekwondoKicks)),
            Physical = new PhysicalStats(193, 185, 203, 34),
            Striking  = new StrikingStats(90, 78, 92, 86, 74, 70),
            Grappling = new GrapplingStats(52, 68, 58, 62, 56, 60),
            Athletics = new AthleticStats(82, 70, 94, 84, 76),
            Record = new FighterRecord(24, 4, 0),
            FightIq = 90
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000023"),
            FirstName = "Khamzat", LastName = "Chimaev", Nickname = "Borz",
            Nationality = "Sweden", WeightClass = WeightClass.Middleweight,
            PrimaryStyle = FightingStyle.Wrestler, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.Wrestler, 96, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingDoubleLeg, StyleSpecialty.WrestlingControl, StyleSpecialty.WrestlingTakedownDefense, StyleSpecialty.GroundAndPoundPunches),
                Profile(FightingStyle.BJJPractitioner, 84, StyleSpecialty.BjjFinisher, StyleSpecialty.BjjControl, StyleSpecialty.GuillotineChoke),
                Profile(FightingStyle.Boxer, 72, StyleSpecialty.BoxingCombinations)),
            Physical = new PhysicalStats(188, 185, 190, 30),
            Striking  = new StrikingStats(80, 83, 82, 78, 88, 86),
            Grappling = new GrapplingStats(97, 89, 85, 78, 90, 86),
            Athletics = new AthleticStats(90, 94, 82, 90, 92),
            Record = new FighterRecord(14, 0, 0),
            FightIq = 92
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000024"),
            FirstName = "Sean", LastName = "Strickland", Nickname = "Tarzan",
            Nationality = "USA", WeightClass = WeightClass.Middleweight,
            PrimaryStyle = FightingStyle.Boxer, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.Boxer, 84, StyleSpecialty.BoxingCombinations, StyleSpecialty.BoxingCountering, StyleSpecialty.BoxingPocketPressure),
                Profile(FightingStyle.Wrestler, 60, StyleSpecialty.WrestlingTakedownDefense)),
            Physical = new PhysicalStats(185, 185, 193, 33),
            Striking  = new StrikingStats(78, 72, 86, 93, 78, 76),
            Grappling = new GrapplingStats(62, 83, 58, 66, 60, 64),
            Athletics = new AthleticStats(90, 76, 84, 92, 88),
            Record = new FighterRecord(29, 6, 0)
        },

        // ──────────────── LIGHT HEAVYWEIGHT (205 lbs) ────────────────
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000025"),
            FirstName = "Alex", LastName = "Pereira", Nickname = "Poatan",
            Nationality = "Brazil", WeightClass = WeightClass.LightHeavyweight,
            PrimaryStyle = FightingStyle.Kickboxer, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.Kickboxer, 99, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks, StyleSpecialty.KickboxingPressure),
                Profile(FightingStyle.Boxer, 89, StyleSpecialty.BoxingCountering, StyleSpecialty.BoxingCombinations)),
            Physical = new PhysicalStats(193, 205, 198, 37),
            Striking  = new StrikingStats(95, 99, 88, 90, 88, 86),
            Grappling = new GrapplingStats(60, 70, 58, 66, 60, 72),
            Athletics = new AthleticStats(88, 92, 88, 88, 92),
            Record = new FighterRecord(12, 2, 0),
            FightIq = 88
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000026"),
            FirstName = "Jiri", LastName = "Prochazka", Nickname = "Denisa",
            Nationality = "Czech Republic", WeightClass = WeightClass.LightHeavyweight,
            PrimaryStyle = FightingStyle.Striker, Stance = Stance.Southpaw,
            StyleProfiles = Styles(
                Profile(FightingStyle.Striker, 90, StyleSpecialty.KarateDistance, StyleSpecialty.TaekwondoKicks),
                Profile(FightingStyle.Kickboxer, 84, StyleSpecialty.KickboxingPressure, StyleSpecialty.KickboxingKicks),
                Profile(FightingStyle.MuayThai, 76, StyleSpecialty.MuayThaiElbows)),
            Physical = new PhysicalStats(191, 205, 201, 32),
            Striking  = new StrikingStats(76, 90, 93, 68, 74, 72),
            Grappling = new GrapplingStats(62, 66, 62, 64, 60, 64),
            Athletics = new AthleticStats(88, 82, 90, 86, 84),
            Record = new FighterRecord(30, 4, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000027"),
            FirstName = "Magomed", LastName = "Ankalaev", Nickname = "Iron",
            Nationality = "Russia", WeightClass = WeightClass.LightHeavyweight,
            PrimaryStyle = FightingStyle.Wrestler, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.Wrestler, 88, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl, StyleSpecialty.WrestlingTakedownDefense),
                Profile(FightingStyle.Kickboxer, 78, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks),
                Profile(FightingStyle.Boxer, 72, StyleSpecialty.BoxingCountering)),
            Physical = new PhysicalStats(188, 205, 193, 31),
            Striking  = new StrikingStats(78, 80, 76, 78, 84, 80),
            Grappling = new GrapplingStats(84, 80, 72, 76, 80, 76),
            Athletics = new AthleticStats(88, 84, 80, 88, 86),
            Record = new FighterRecord(20, 1, 1)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000028"),
            FirstName = "Khalil", LastName = "Rountree Jr.", Nickname = "The War Horse",
            Nationality = "USA", WeightClass = WeightClass.LightHeavyweight,
            PrimaryStyle = FightingStyle.MuayThai, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.Kickboxer, 94, StyleSpecialty.KickboxingKicks, StyleSpecialty.KickboxingPressure),
                Profile(FightingStyle.MuayThai, 78, StyleSpecialty.MuayThaiKnees, StyleSpecialty.MuayThaiClinch)),
            Physical = new PhysicalStats(185, 205, 188, 34),
            Striking  = new StrikingStats(74, 96, 78, 68, 76, 74),
            Grappling = new GrapplingStats(60, 62, 54, 58, 56, 58),
            Athletics = new AthleticStats(82, 88, 78, 80, 80),
            Record = new FighterRecord(13, 5, 1)
        },

        // ──────────────── HEAVYWEIGHT (265 lbs) ────────────────
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000029"),
            FirstName = "Jon", LastName = "Jones", Nickname = "Bones",
            Nationality = "USA", WeightClass = WeightClass.Heavyweight,
            PrimaryStyle = FightingStyle.MMAFighter, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.MuayThai, 94, StyleSpecialty.MuayThaiElbows, StyleSpecialty.MuayThaiKnees, StyleSpecialty.MuayThaiTeeps, StyleSpecialty.MuayThaiClinch, StyleSpecialty.ObliqueKicks),
                Profile(FightingStyle.Kickboxer, 90, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks, StyleSpecialty.ObliqueKicks),
                Profile(FightingStyle.Wrestler, 88, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.ClinchTrips, StyleSpecialty.WrestlingTakedownDefense, StyleSpecialty.WrestlingControl, StyleSpecialty.GroundAndPoundElbows)),
            Physical = new PhysicalStats(193, 240, 215, 38),
            Striking  = new StrikingStats(84, 81, 78, 95, 86, 84),
            Grappling = new GrapplingStats(86, 97, 88, 85, 88, 84),
            Athletics = new AthleticStats(95, 90, 84, 94, 90),
            Record = new FighterRecord(27, 1, 0),
            FightIq = 98
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000030"),
            FirstName = "Tom", LastName = "Aspinall", Nickname = "The Machine",
            Nationality = "UK", WeightClass = WeightClass.Heavyweight,
            PrimaryStyle = FightingStyle.MMAFighter, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.Boxer, 84, StyleSpecialty.BoxingCombinations, StyleSpecialty.BoxingCountering),
                Profile(FightingStyle.Wrestler, 78, StyleSpecialty.WrestlingTakedownDefense, StyleSpecialty.WrestlingDoubleLeg, StyleSpecialty.WrestlingTakedowns),
                Profile(FightingStyle.BJJPractitioner, 74, StyleSpecialty.BjjFinisher, StyleSpecialty.BjjControl, StyleSpecialty.Armbar)),
            Physical = new PhysicalStats(193, 245, 201, 31),
            Striking  = new StrikingStats(82, 90, 90, 76, 78, 76),
            Grappling = new GrapplingStats(78, 74, 76, 74, 74, 76),
            Athletics = new AthleticStats(88, 96, 84, 88, 80),
            Record = new FighterRecord(15, 3, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000031"),
            FirstName = "Ciryl", LastName = "Gane", Nickname = "Bon Gamin",
            Nationality = "France", WeightClass = WeightClass.Heavyweight,
            PrimaryStyle = FightingStyle.Kickboxer, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.Kickboxer, 88, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks),
                Profile(FightingStyle.MuayThai, 78, StyleSpecialty.MuayThaiKnees, StyleSpecialty.MuayThaiTeeps)),
            Physical = new PhysicalStats(196, 250, 211, 34),
            Striking  = new StrikingStats(78, 77, 84, 80, 78, 76),
            Grappling = new GrapplingStats(66, 62, 60, 64, 62, 66),
            Athletics = new AthleticStats(82, 78, 82, 84, 78),
            Record = new FighterRecord(12, 2, 0),
            FightIq = 80
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000032"),
            FirstName = "Sergei", LastName = "Pavlovich", Nickname = "The Bulldozer",
            Nationality = "Russia", WeightClass = WeightClass.Heavyweight,
            PrimaryStyle = FightingStyle.Striker, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.Boxer, 88, StyleSpecialty.BoxingPocketPressure, StyleSpecialty.BoxingCombinations),
                Profile(FightingStyle.Striker, 82, StyleSpecialty.KickboxingPressure)),
            Physical = new PhysicalStats(191, 243, 196, 31),
            Striking  = new StrikingStats(72, 96, 80, 64, 74, 70),
            Grappling = new GrapplingStats(64, 66, 56, 62, 60, 60),
            Athletics = new AthleticStats(76, 88, 76, 76, 78),
            Record = new FighterRecord(18, 2, 0)
        },

        // Additional current men's top-15 roster entries (March 2026)
        RankedFighter("00000000-0000-0000-0000-000000000039", "Joshua", "Van", "The Fearless", "Myanmar", WeightClass.Flyweight, FightingStyle.Boxer, Stance.Orthodox, 165, 125, 165, 24, new FighterRecord(15, 2, 0), 88,
            Profile(FightingStyle.Boxer, 90, StyleSpecialty.BoxingCombinations, StyleSpecialty.BoxingPocketPressure, StyleSpecialty.BoxingCountering),
            Profile(FightingStyle.Wrestler, 64, StyleSpecialty.WrestlingTakedownDefense)),
        RankedFighter("00000000-0000-0000-0000-000000000040", "Manel", "Kape", "Starboy", "Angola", WeightClass.Flyweight, FightingStyle.Kickboxer, Stance.Southpaw, 165, 125, 173, 32, new FighterRecord(21, 7, 0), 85,
            Profile(FightingStyle.Kickboxer, 90, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks, StyleSpecialty.KickboxingPressure),
            Profile(FightingStyle.Boxer, 80, StyleSpecialty.BoxingCountering)),
        RankedFighter("00000000-0000-0000-0000-000000000041", "Tatsuro", "Taira", "", "Japan", WeightClass.Flyweight, FightingStyle.BJJPractitioner, Stance.Orthodox, 170, 125, 178, 25, new FighterRecord(17, 1, 0), 84,
            Profile(FightingStyle.BJJPractitioner, 92, StyleSpecialty.BjjFinisher, StyleSpecialty.BjjControl, StyleSpecialty.BjjScrambles),
            Profile(FightingStyle.Wrestler, 82, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl)),
        RankedFighter("00000000-0000-0000-0000-000000000042", "Kyoji", "Horiguchi", "The Typhoon", "Japan", WeightClass.Flyweight, FightingStyle.Striker, Stance.Orthodox, 165, 125, 168, 35, new FighterRecord(34, 5, 0), 83,
            Profile(FightingStyle.Striker, 88, StyleSpecialty.KarateDistance, StyleSpecialty.TaekwondoKicks),
            Profile(FightingStyle.Wrestler, 74, StyleSpecialty.WrestlingTakedownDefense)),
        RankedFighter("00000000-0000-0000-0000-000000000043", "Kai", "Kara-France", "Don't Blink", "New Zealand", WeightClass.Flyweight, FightingStyle.Kickboxer, Stance.Orthodox, 163, 125, 175, 33, new FighterRecord(25, 12, 0), 82,
            Profile(FightingStyle.Kickboxer, 86, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks),
            Profile(FightingStyle.Boxer, 78, StyleSpecialty.BoxingCombinations)),
        RankedFighter("00000000-0000-0000-0000-000000000044", "Brandon", "Moreno", "The Assassin Baby", "Mexico", WeightClass.Flyweight, FightingStyle.MMAFighter, Stance.Orthodox, 170, 125, 178, 32, new FighterRecord(23, 9, 2), 84,
            Profile(FightingStyle.Boxer, 84, StyleSpecialty.BoxingCombinations, StyleSpecialty.BoxingCountering),
            Profile(FightingStyle.BJJPractitioner, 82, StyleSpecialty.BjjControl, StyleSpecialty.BjjFinisher)),
        RankedFighter("00000000-0000-0000-0000-000000000045", "Alex", "Perez", "", "USA", WeightClass.Flyweight, FightingStyle.Wrestler, Stance.Orthodox, 168, 125, 166, 33, new FighterRecord(25, 9, 0), 79,
            Profile(FightingStyle.Wrestler, 84, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl),
            Profile(FightingStyle.Boxer, 70, StyleSpecialty.BoxingPocketPressure)),
        RankedFighter("00000000-0000-0000-0000-000000000046", "Tim", "Elliott", "", "USA", WeightClass.Flyweight, FightingStyle.Wrestler, Stance.Orthodox, 170, 125, 170, 39, new FighterRecord(21, 14, 1), 77,
            Profile(FightingStyle.Wrestler, 82, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl),
            Profile(FightingStyle.BJJPractitioner, 76, StyleSpecialty.BjjScrambles)),
        RankedFighter("00000000-0000-0000-0000-000000000047", "Tagir", "Ulanbekov", "", "Russia", WeightClass.Flyweight, FightingStyle.Wrestler, Stance.Orthodox, 170, 125, 178, 33, new FighterRecord(17, 3, 0), 79,
            Profile(FightingStyle.Wrestler, 86, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingTakedownDefense, StyleSpecialty.WrestlingControl),
            Profile(FightingStyle.BJJPractitioner, 72, StyleSpecialty.BjjControl)),
        RankedFighter("00000000-0000-0000-0000-000000000048", "Asu", "Almabayev", "", "Kazakhstan", WeightClass.Flyweight, FightingStyle.Wrestler, Stance.Orthodox, 163, 125, 165, 31, new FighterRecord(21, 3, 0), 78,
            Profile(FightingStyle.Wrestler, 84, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl),
            Profile(FightingStyle.BJJPractitioner, 68, StyleSpecialty.BjjControl)),
        RankedFighter("00000000-0000-0000-0000-000000000049", "Charles", "Johnson", "InnerG", "USA", WeightClass.Flyweight, FightingStyle.Boxer, Stance.Orthodox, 175, 125, 178, 34, new FighterRecord(17, 6, 0), 77,
            Profile(FightingStyle.Boxer, 82, StyleSpecialty.BoxingCombinations, StyleSpecialty.BoxingCountering),
            Profile(FightingStyle.Wrestler, 66, StyleSpecialty.WrestlingTakedownDefense)),
        RankedFighter("00000000-0000-0000-0000-000000000050", "Bruno", "Silva", "", "Brazil", WeightClass.Flyweight, FightingStyle.Boxer, Stance.Orthodox, 163, 125, 165, 35, new FighterRecord(14, 6, 2), 76,
            Profile(FightingStyle.Boxer, 80, StyleSpecialty.BoxingPocketPressure, StyleSpecialty.BoxingCombinations),
            Profile(FightingStyle.MuayThai, 72, StyleSpecialty.MuayThaiKnees)),

        RankedFighter("00000000-0000-0000-0000-000000000051", "Petr", "Yan", "No Mercy", "Russia", WeightClass.Bantamweight, FightingStyle.Boxer, Stance.Switch, 170, 135, 171, 33, new FighterRecord(19, 5, 0), 90,
            Profile(FightingStyle.Boxer, 94, StyleSpecialty.BoxingCombinations, StyleSpecialty.BoxingCountering, StyleSpecialty.BoxingPocketPressure),
            Profile(FightingStyle.Wrestler, 78, StyleSpecialty.WrestlingTakedownDefense)),
        RankedFighter("00000000-0000-0000-0000-000000000052", "Song", "Yadong", "", "China", WeightClass.Bantamweight, FightingStyle.Boxer, Stance.Orthodox, 173, 135, 173, 28, new FighterRecord(22, 9, 1), 82,
            Profile(FightingStyle.Boxer, 86, StyleSpecialty.BoxingPocketPressure, StyleSpecialty.BoxingCombinations),
            Profile(FightingStyle.Kickboxer, 74, StyleSpecialty.KickboxingKicks)),
        RankedFighter("00000000-0000-0000-0000-000000000053", "Aiemann", "Zahabi", "", "Canada", WeightClass.Bantamweight, FightingStyle.Boxer, Stance.Orthodox, 173, 135, 173, 37, new FighterRecord(13, 2, 0), 79,
            Profile(FightingStyle.Boxer, 82, StyleSpecialty.BoxingCountering, StyleSpecialty.BoxingCombinations),
            Profile(FightingStyle.Kickboxer, 70, StyleSpecialty.KickboxingRange)),
        RankedFighter("00000000-0000-0000-0000-000000000054", "Deiveson", "Figueiredo", "Deus da Guerra", "Brazil", WeightClass.Bantamweight, FightingStyle.MMAFighter, Stance.Orthodox, 165, 135, 173, 38, new FighterRecord(24, 5, 1), 81,
            Profile(FightingStyle.Boxer, 84, StyleSpecialty.BoxingPocketPressure, StyleSpecialty.BoxingCountering),
            Profile(FightingStyle.BJJPractitioner, 80, StyleSpecialty.BjjFinisher, StyleSpecialty.BjjControl)),
        RankedFighter("00000000-0000-0000-0000-000000000055", "Mario", "Bautista", "", "USA", WeightClass.Bantamweight, FightingStyle.Wrestler, Stance.Orthodox, 175, 135, 175, 31, new FighterRecord(16, 2, 0), 80,
            Profile(FightingStyle.Wrestler, 84, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl, StyleSpecialty.WrestlingTakedownDefense),
            Profile(FightingStyle.Boxer, 72, StyleSpecialty.BoxingCombinations)),
        RankedFighter("00000000-0000-0000-0000-000000000056", "David", "Martinez", "", "Mexico", WeightClass.Bantamweight, FightingStyle.Kickboxer, Stance.Orthodox, 173, 135, 178, 27, new FighterRecord(12, 1, 0), 78,
            Profile(FightingStyle.Kickboxer, 84, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks),
            Profile(FightingStyle.Boxer, 74, StyleSpecialty.BoxingCombinations)),
        RankedFighter("00000000-0000-0000-0000-000000000057", "Marlon", "Vera", "Chito", "Ecuador", WeightClass.Bantamweight, FightingStyle.MuayThai, Stance.Orthodox, 173, 135, 179, 33, new FighterRecord(22, 10, 1), 78,
            Profile(FightingStyle.MuayThai, 84, StyleSpecialty.MuayThaiKnees, StyleSpecialty.MuayThaiElbows, StyleSpecialty.MuayThaiClinch),
            Profile(FightingStyle.BJJPractitioner, 70, StyleSpecialty.BjjControl)),
        RankedFighter("00000000-0000-0000-0000-000000000058", "Payton", "Talbott", "", "USA", WeightClass.Bantamweight, FightingStyle.Kickboxer, Stance.Switch, 178, 135, 178, 26, new FighterRecord(11, 1, 0), 79,
            Profile(FightingStyle.Kickboxer, 86, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks),
            Profile(FightingStyle.Striker, 82, StyleSpecialty.KarateDistance)),
        RankedFighter("00000000-0000-0000-0000-000000000059", "Vinicius", "Oliveira", "", "Brazil", WeightClass.Bantamweight, FightingStyle.Striker, Stance.Orthodox, 175, 135, 178, 30, new FighterRecord(22, 4, 0), 77,
            Profile(FightingStyle.Striker, 82, StyleSpecialty.KickboxingPressure, StyleSpecialty.KickboxingKicks),
            Profile(FightingStyle.Boxer, 74, StyleSpecialty.BoxingPocketPressure)),
        RankedFighter("00000000-0000-0000-0000-000000000060", "Raul", "Rosas Jr.", "El Niño Problema", "USA", WeightClass.Bantamweight, FightingStyle.Wrestler, Stance.Orthodox, 175, 135, 170, 21, new FighterRecord(11, 1, 0), 77,
            Profile(FightingStyle.Wrestler, 84, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl),
            Profile(FightingStyle.BJJPractitioner, 80, StyleSpecialty.BjjFinisher, StyleSpecialty.BjjScrambles)),
        RankedFighter("00000000-0000-0000-0000-000000000061", "Montel", "Jackson", "", "USA", WeightClass.Bantamweight, FightingStyle.Boxer, Stance.Southpaw, 178, 135, 191, 33, new FighterRecord(15, 2, 0), 78,
            Profile(FightingStyle.Boxer, 82, StyleSpecialty.BoxingCountering, StyleSpecialty.BoxingCombinations),
            Profile(FightingStyle.Kickboxer, 78, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks)),
        RankedFighter("00000000-0000-0000-0000-000000000062", "Farid", "Basharat", "", "Afghanistan", WeightClass.Bantamweight, FightingStyle.MMAFighter, Stance.Orthodox, 175, 135, 178, 27, new FighterRecord(13, 1, 0), 77,
            Profile(FightingStyle.Wrestler, 80, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingTakedownDefense),
            Profile(FightingStyle.Boxer, 76, StyleSpecialty.BoxingCombinations),
            Profile(FightingStyle.BJJPractitioner, 72, StyleSpecialty.BjjControl)),

        RankedFighter("00000000-0000-0000-0000-000000000063", "Movsar", "Evloev", "", "Russia", WeightClass.Featherweight, FightingStyle.Wrestler, Stance.Orthodox, 170, 145, 184, 31, new FighterRecord(20, 0, 0), 88,
            Profile(FightingStyle.Wrestler, 92, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl, StyleSpecialty.WrestlingTakedownDefense),
            Profile(FightingStyle.Boxer, 72, StyleSpecialty.BoxingCombinations)),
        RankedFighter("00000000-0000-0000-0000-000000000064", "Diego", "Lopes", "", "Brazil", WeightClass.Featherweight, FightingStyle.BJJPractitioner, Stance.Orthodox, 180, 145, 183, 31, new FighterRecord(27, 7, 0), 86,
            Profile(FightingStyle.BJJPractitioner, 92, StyleSpecialty.BjjFinisher, StyleSpecialty.BjjControl, StyleSpecialty.BjjScrambles),
            Profile(FightingStyle.Boxer, 78, StyleSpecialty.BoxingPocketPressure)),
        RankedFighter("00000000-0000-0000-0000-000000000065", "Lerone", "Murphy", "", "UK", WeightClass.Featherweight, FightingStyle.Boxer, Stance.Orthodox, 175, 145, 185, 34, new FighterRecord(16, 0, 1), 84,
            Profile(FightingStyle.Boxer, 88, StyleSpecialty.BoxingCountering, StyleSpecialty.BoxingCombinations),
            Profile(FightingStyle.Kickboxer, 74, StyleSpecialty.KickboxingRange)),
        RankedFighter("00000000-0000-0000-0000-000000000066", "Yair", "Rodriguez", "El Pantera", "Mexico", WeightClass.Featherweight, FightingStyle.Striker, Stance.Orthodox, 180, 145, 180, 33, new FighterRecord(20, 5, 0), 83,
            Profile(FightingStyle.Striker, 90, StyleSpecialty.KarateDistance, StyleSpecialty.TaekwondoKicks),
            Profile(FightingStyle.MuayThai, 80, StyleSpecialty.MuayThaiKnees, StyleSpecialty.MuayThaiElbows)),
        RankedFighter("00000000-0000-0000-0000-000000000067", "Aljamain", "Sterling", "Funk Master", "USA", WeightClass.Featherweight, FightingStyle.Wrestler, Stance.Orthodox, 170, 145, 180, 36, new FighterRecord(24, 5, 0), 82,
            Profile(FightingStyle.Wrestler, 88, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl, StyleSpecialty.WrestlingTakedownDefense),
            Profile(FightingStyle.BJJPractitioner, 80, StyleSpecialty.BjjControl, StyleSpecialty.BjjFinisher)),
        RankedFighter("00000000-0000-0000-0000-000000000068", "Jean", "Silva", "", "Brazil", WeightClass.Featherweight, FightingStyle.MuayThai, Stance.Orthodox, 170, 145, 175, 28, new FighterRecord(16, 2, 0), 81,
            Profile(FightingStyle.MuayThai, 86, StyleSpecialty.MuayThaiElbows, StyleSpecialty.MuayThaiKnees, StyleSpecialty.MuayThaiClinch),
            Profile(FightingStyle.Boxer, 76, StyleSpecialty.BoxingPocketPressure)),
        RankedFighter("00000000-0000-0000-0000-000000000069", "Youssef", "Zalal", "", "Morocco", WeightClass.Featherweight, FightingStyle.MMAFighter, Stance.Orthodox, 178, 145, 180, 28, new FighterRecord(17, 5, 1), 80,
            Profile(FightingStyle.Kickboxer, 80, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks),
            Profile(FightingStyle.Wrestler, 76, StyleSpecialty.WrestlingTakedownDefense)),
        RankedFighter("00000000-0000-0000-0000-000000000070", "Arnold", "Allen", "", "UK", WeightClass.Featherweight, FightingStyle.Boxer, Stance.Orthodox, 173, 145, 178, 31, new FighterRecord(21, 4, 0), 81,
            Profile(FightingStyle.Boxer, 84, StyleSpecialty.BoxingCombinations, StyleSpecialty.BoxingCountering),
            Profile(FightingStyle.Kickboxer, 74, StyleSpecialty.KickboxingKicks)),
        RankedFighter("00000000-0000-0000-0000-000000000071", "Steve", "Garcia", "Mean Machine", "USA", WeightClass.Featherweight, FightingStyle.Boxer, Stance.Orthodox, 183, 145, 191, 33, new FighterRecord(18, 6, 0), 79,
            Profile(FightingStyle.Boxer, 82, StyleSpecialty.BoxingPocketPressure, StyleSpecialty.BoxingCombinations),
            Profile(FightingStyle.Kickboxer, 74, StyleSpecialty.KickboxingPressure)),
        RankedFighter("00000000-0000-0000-0000-000000000072", "Josh", "Emmett", "", "USA", WeightClass.Featherweight, FightingStyle.Boxer, Stance.Orthodox, 168, 145, 178, 40, new FighterRecord(19, 5, 0), 80,
            Profile(FightingStyle.Boxer, 84, StyleSpecialty.BoxingPocketPressure, StyleSpecialty.BoxingCountering),
            Profile(FightingStyle.Wrestler, 70, StyleSpecialty.WrestlingTakedownDefense)),
        RankedFighter("00000000-0000-0000-0000-000000000073", "Melquizael", "Costa", "", "Brazil", WeightClass.Featherweight, FightingStyle.MuayThai, Stance.Orthodox, 178, 145, 180, 28, new FighterRecord(22, 7, 0), 78,
            Profile(FightingStyle.MuayThai, 82, StyleSpecialty.MuayThaiKnees, StyleSpecialty.MuayThaiElbows),
            Profile(FightingStyle.BJJPractitioner, 74, StyleSpecialty.BjjControl)),
        RankedFighter("00000000-0000-0000-0000-000000000074", "Patricio", "Pitbull", "", "Brazil", WeightClass.Featherweight, FightingStyle.Boxer, Stance.Orthodox, 168, 145, 170, 38, new FighterRecord(36, 8, 0), 79,
            Profile(FightingStyle.Boxer, 84, StyleSpecialty.BoxingPocketPressure, StyleSpecialty.BoxingCombinations),
            Profile(FightingStyle.Wrestler, 72, StyleSpecialty.WrestlingTakedowns)),
        RankedFighter("00000000-0000-0000-0000-000000000075", "Kevin", "Vallejos", "", "Argentina", WeightClass.Featherweight, FightingStyle.Kickboxer, Stance.Orthodox, 170, 145, 175, 23, new FighterRecord(15, 1, 0), 77,
            Profile(FightingStyle.Kickboxer, 82, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks),
            Profile(FightingStyle.Boxer, 72, StyleSpecialty.BoxingCombinations)),
        RankedFighter("00000000-0000-0000-0000-000000000076", "David", "Onama", "Silent Assassin", "Uganda", WeightClass.Featherweight, FightingStyle.MMAFighter, Stance.Orthodox, 180, 145, 188, 31, new FighterRecord(15, 3, 0), 77,
            Profile(FightingStyle.Boxer, 78, StyleSpecialty.BoxingPocketPressure),
            Profile(FightingStyle.Wrestler, 72, StyleSpecialty.WrestlingTakedownDefense),
            Profile(FightingStyle.BJJPractitioner, 70, StyleSpecialty.BjjControl)),

        RankedFighter("00000000-0000-0000-0000-000000000077", "Justin", "Gaethje", "The Highlight", "USA", WeightClass.Lightweight, FightingStyle.Boxer, Stance.Orthodox, 180, 155, 178, 37, new FighterRecord(27, 5, 0), 88,
            Profile(FightingStyle.Boxer, 90, StyleSpecialty.BoxingPocketPressure, StyleSpecialty.BoxingCombinations),
            Profile(FightingStyle.Wrestler, 78, StyleSpecialty.WrestlingTakedownDefense)),
        RankedFighter("00000000-0000-0000-0000-000000000078", "Benoit", "Saint Denis", "God of War", "France", WeightClass.Lightweight, FightingStyle.MMAFighter, Stance.Orthodox, 180, 155, 185, 29, new FighterRecord(14, 3, 0), 82,
            Profile(FightingStyle.Wrestler, 82, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl),
            Profile(FightingStyle.BJJPractitioner, 80, StyleSpecialty.BjjFinisher, StyleSpecialty.BjjScrambles),
            Profile(FightingStyle.MuayThai, 76, StyleSpecialty.MuayThaiKnees)),
        RankedFighter("00000000-0000-0000-0000-000000000079", "Paddy", "Pimblett", "", "UK", WeightClass.Lightweight, FightingStyle.BJJPractitioner, Stance.Orthodox, 178, 155, 185, 31, new FighterRecord(23, 3, 0), 81,
            Profile(FightingStyle.BJJPractitioner, 86, StyleSpecialty.BjjFinisher, StyleSpecialty.BjjControl),
            Profile(FightingStyle.Boxer, 72, StyleSpecialty.BoxingCombinations)),
        RankedFighter("00000000-0000-0000-0000-000000000080", "Dan", "Hooker", "The Hangman", "New Zealand", WeightClass.Lightweight, FightingStyle.Kickboxer, Stance.Orthodox, 183, 155, 191, 35, new FighterRecord(25, 13, 0), 81,
            Profile(FightingStyle.Kickboxer, 86, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks),
            Profile(FightingStyle.Boxer, 80, StyleSpecialty.BoxingCountering)),
        RankedFighter("00000000-0000-0000-0000-000000000081", "Mateusz", "Gamrot", "", "Poland", WeightClass.Lightweight, FightingStyle.Wrestler, Stance.Southpaw, 178, 155, 178, 35, new FighterRecord(25, 4, 0), 81,
            Profile(FightingStyle.Wrestler, 88, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl, StyleSpecialty.WrestlingTakedownDefense),
            Profile(FightingStyle.BJJPractitioner, 74, StyleSpecialty.BjjControl)),
        RankedFighter("00000000-0000-0000-0000-000000000082", "Mauricio", "Ruffy", "", "Brazil", WeightClass.Lightweight, FightingStyle.Kickboxer, Stance.Orthodox, 180, 155, 185, 29, new FighterRecord(12, 1, 0), 79,
            Profile(FightingStyle.Kickboxer, 86, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks, StyleSpecialty.KickboxingPressure),
            Profile(FightingStyle.Boxer, 76, StyleSpecialty.BoxingCountering)),
        RankedFighter("00000000-0000-0000-0000-000000000083", "Rafael", "Fiziev", "Ataman", "Azerbaijan", WeightClass.Lightweight, FightingStyle.MuayThai, Stance.Switch, 173, 155, 180, 33, new FighterRecord(12, 4, 0), 80,
            Profile(FightingStyle.MuayThai, 90, StyleSpecialty.MuayThaiKnees, StyleSpecialty.MuayThaiElbows, StyleSpecialty.MuayThaiClinch),
            Profile(FightingStyle.Kickboxer, 80, StyleSpecialty.KickboxingKicks)),
        RankedFighter("00000000-0000-0000-0000-000000000084", "Renato", "Moicano", "", "Brazil", WeightClass.Lightweight, FightingStyle.BJJPractitioner, Stance.Orthodox, 180, 155, 183, 36, new FighterRecord(20, 6, 1), 80,
            Profile(FightingStyle.BJJPractitioner, 88, StyleSpecialty.BjjFinisher, StyleSpecialty.BjjControl),
            Profile(FightingStyle.Boxer, 72, StyleSpecialty.BoxingCombinations)),
        RankedFighter("00000000-0000-0000-0000-000000000085", "Beneil", "Dariush", "", "USA", WeightClass.Lightweight, FightingStyle.BJJPractitioner, Stance.Southpaw, 178, 155, 183, 37, new FighterRecord(22, 7, 1), 79,
            Profile(FightingStyle.BJJPractitioner, 86, StyleSpecialty.BjjControl, StyleSpecialty.BjjFinisher),
            Profile(FightingStyle.MuayThai, 74, StyleSpecialty.MuayThaiKnees)),
        RankedFighter("00000000-0000-0000-0000-000000000086", "Michael", "Chandler", "Iron", "USA", WeightClass.Lightweight, FightingStyle.Wrestler, Stance.Orthodox, 173, 155, 180, 40, new FighterRecord(24, 10, 0), 79,
            Profile(FightingStyle.Wrestler, 84, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl),
            Profile(FightingStyle.Boxer, 82, StyleSpecialty.BoxingPocketPressure)),
        RankedFighter("00000000-0000-0000-0000-000000000087", "Manuel", "Torres", "El Loco", "Mexico", WeightClass.Lightweight, FightingStyle.Boxer, Stance.Orthodox, 178, 155, 185, 30, new FighterRecord(15, 3, 0), 77,
            Profile(FightingStyle.Boxer, 82, StyleSpecialty.BoxingPocketPressure, StyleSpecialty.BoxingCombinations),
            Profile(FightingStyle.BJJPractitioner, 68, StyleSpecialty.BjjControl)),
        RankedFighter("00000000-0000-0000-0000-000000000088", "Fares", "Ziam", "", "France", WeightClass.Lightweight, FightingStyle.Kickboxer, Stance.Orthodox, 185, 155, 191, 28, new FighterRecord(17, 4, 0), 77,
            Profile(FightingStyle.Kickboxer, 82, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks),
            Profile(FightingStyle.Boxer, 72, StyleSpecialty.BoxingCountering)),

        RankedFighter("00000000-0000-0000-0000-000000000089", "Ian", "Machado Garry", "The Future", "Ireland", WeightClass.Welterweight, FightingStyle.Kickboxer, Stance.Orthodox, 190, 170, 188, 28, new FighterRecord(16, 1, 0), 86,
            Profile(FightingStyle.Kickboxer, 88, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks),
            Profile(FightingStyle.Boxer, 82, StyleSpecialty.BoxingCountering)),
        RankedFighter("00000000-0000-0000-0000-000000000090", "Michael", "Morales", "", "Ecuador", WeightClass.Welterweight, FightingStyle.Boxer, Stance.Orthodox, 183, 170, 201, 25, new FighterRecord(18, 0, 0), 85,
            Profile(FightingStyle.Boxer, 88, StyleSpecialty.BoxingCombinations, StyleSpecialty.BoxingCountering),
            Profile(FightingStyle.Kickboxer, 76, StyleSpecialty.KickboxingRange)),
        RankedFighter("00000000-0000-0000-0000-000000000091", "Carlos", "Prates", "", "Brazil", WeightClass.Welterweight, FightingStyle.MuayThai, Stance.Southpaw, 185, 170, 198, 31, new FighterRecord(22, 6, 0), 84,
            Profile(FightingStyle.MuayThai, 95, StyleSpecialty.MuayThaiKnees, StyleSpecialty.MuayThaiElbows, StyleSpecialty.MuayThaiClinch, StyleSpecialty.MuayThaiTeeps),
            Profile(FightingStyle.Boxer, 78, StyleSpecialty.BoxingCountering)),
        RankedFighter("00000000-0000-0000-0000-000000000092", "Sean", "Brady", "", "USA", WeightClass.Welterweight, FightingStyle.Wrestler, Stance.Southpaw, 178, 170, 183, 33, new FighterRecord(18, 2, 0), 84,
            Profile(FightingStyle.Wrestler, 90, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl, StyleSpecialty.WrestlingTakedownDefense),
            Profile(FightingStyle.BJJPractitioner, 82, StyleSpecialty.BjjControl, StyleSpecialty.BjjFinisher)),
        RankedFighter("00000000-0000-0000-0000-000000000093", "Kamaru", "Usman", "The Nigerian Nightmare", "Nigeria", WeightClass.Welterweight, FightingStyle.Wrestler, Stance.Switch, 183, 170, 193, 38, new FighterRecord(21, 5, 0), 82,
            Profile(FightingStyle.Wrestler, 88, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl, StyleSpecialty.WrestlingTakedownDefense),
            Profile(FightingStyle.Boxer, 74, StyleSpecialty.BoxingPocketPressure)),
        RankedFighter("00000000-0000-0000-0000-000000000094", "Joaquin", "Buckley", "New Mansa", "USA", WeightClass.Welterweight, FightingStyle.Striker, Stance.Southpaw, 173, 170, 193, 31, new FighterRecord(22, 6, 0), 81,
            Profile(FightingStyle.Striker, 88, StyleSpecialty.KickboxingPressure, StyleSpecialty.KickboxingKicks),
            Profile(FightingStyle.Boxer, 80, StyleSpecialty.BoxingPocketPressure)),
        RankedFighter("00000000-0000-0000-0000-000000000095", "Gabriel", "Bonfim", "Marretinha", "Brazil", WeightClass.Welterweight, FightingStyle.BJJPractitioner, Stance.Orthodox, 183, 170, 183, 28, new FighterRecord(17, 2, 0), 80,
            Profile(FightingStyle.BJJPractitioner, 86, StyleSpecialty.BjjFinisher, StyleSpecialty.BjjControl),
            Profile(FightingStyle.Wrestler, 76, StyleSpecialty.WrestlingTakedowns)),
        RankedFighter("00000000-0000-0000-0000-000000000096", "Gilbert", "Burns", "Durinho", "Brazil", WeightClass.Welterweight, FightingStyle.BJJPractitioner, Stance.Orthodox, 178, 170, 180, 39, new FighterRecord(22, 9, 0), 80,
            Profile(FightingStyle.BJJPractitioner, 90, StyleSpecialty.BjjFinisher, StyleSpecialty.BjjControl),
            Profile(FightingStyle.Boxer, 78, StyleSpecialty.BoxingPocketPressure)),
        RankedFighter("00000000-0000-0000-0000-000000000097", "Uros", "Medic", "", "Serbia", WeightClass.Welterweight, FightingStyle.Boxer, Stance.Southpaw, 185, 170, 185, 32, new FighterRecord(11, 3, 0), 78,
            Profile(FightingStyle.Boxer, 82, StyleSpecialty.BoxingPocketPressure, StyleSpecialty.BoxingCombinations),
            Profile(FightingStyle.Kickboxer, 72, StyleSpecialty.KickboxingKicks)),
        RankedFighter("00000000-0000-0000-0000-000000000098", "Michael", "Page", "Venom", "UK", WeightClass.Welterweight, FightingStyle.Striker, Stance.Switch, 191, 170, 201, 39, new FighterRecord(23, 4, 0), 79,
            Profile(FightingStyle.Striker, 88, StyleSpecialty.KarateDistance, StyleSpecialty.TaekwondoKicks),
            Profile(FightingStyle.Boxer, 76, StyleSpecialty.BoxingCountering)),
        RankedFighter("00000000-0000-0000-0000-000000000099", "Colby", "Covington", "Chaos", "USA", WeightClass.Welterweight, FightingStyle.Wrestler, Stance.Orthodox, 180, 170, 183, 38, new FighterRecord(19, 5, 0), 78,
            Profile(FightingStyle.Wrestler, 86, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl, StyleSpecialty.WrestlingTakedownDefense),
            Profile(FightingStyle.Boxer, 68, StyleSpecialty.BoxingCombinations)),
        RankedFighter("00000000-0000-0000-0000-000000000100", "Daniel", "Rodriguez", "", "USA", WeightClass.Welterweight, FightingStyle.Boxer, Stance.Southpaw, 185, 170, 188, 38, new FighterRecord(19, 5, 0), 77,
            Profile(FightingStyle.Boxer, 82, StyleSpecialty.BoxingCombinations, StyleSpecialty.BoxingPocketPressure),
            Profile(FightingStyle.BJJPractitioner, 66, StyleSpecialty.BjjControl)),

        RankedFighter("00000000-0000-0000-0000-000000000101", "Nassourdine", "Imavov", "", "France", WeightClass.Middleweight, FightingStyle.Boxer, Stance.Orthodox, 191, 185, 191, 31, new FighterRecord(16, 4, 0), 85,
            Profile(FightingStyle.Boxer, 88, StyleSpecialty.BoxingCountering, StyleSpecialty.BoxingCombinations),
            Profile(FightingStyle.Kickboxer, 82, StyleSpecialty.KickboxingRange)),
        RankedFighter("00000000-0000-0000-0000-000000000102", "Caio", "Borralho", "", "Brazil", WeightClass.Middleweight, FightingStyle.MMAFighter, Stance.Southpaw, 185, 185, 191, 32, new FighterRecord(17, 1, 0), 84,
            Profile(FightingStyle.Wrestler, 82, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl),
            Profile(FightingStyle.BJJPractitioner, 84, StyleSpecialty.BjjControl, StyleSpecialty.BjjFinisher),
            Profile(FightingStyle.Boxer, 74, StyleSpecialty.BoxingCountering)),
        RankedFighter("00000000-0000-0000-0000-000000000103", "Brendan", "Allen", "All In", "USA", WeightClass.Middleweight, FightingStyle.BJJPractitioner, Stance.Orthodox, 188, 185, 191, 29, new FighterRecord(25, 7, 0), 82,
            Profile(FightingStyle.BJJPractitioner, 88, StyleSpecialty.BjjControl, StyleSpecialty.BjjFinisher),
            Profile(FightingStyle.Wrestler, 78, StyleSpecialty.WrestlingControl)),
        RankedFighter("00000000-0000-0000-0000-000000000104", "Anthony", "Hernandez", "Fluffy", "USA", WeightClass.Middleweight, FightingStyle.Wrestler, Stance.Switch, 183, 185, 191, 32, new FighterRecord(15, 2, 0), 82,
            Profile(FightingStyle.Wrestler, 86, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl),
            Profile(FightingStyle.BJJPractitioner, 80, StyleSpecialty.BjjControl, StyleSpecialty.BjjScrambles)),
        RankedFighter("00000000-0000-0000-0000-000000000105", "Reinier", "de Ridder", "", "Netherlands", WeightClass.Middleweight, FightingStyle.BJJPractitioner, Stance.Orthodox, 193, 185, 198, 35, new FighterRecord(20, 2, 0), 82,
            Profile(FightingStyle.BJJPractitioner, 90, StyleSpecialty.BjjFinisher, StyleSpecialty.BjjControl),
            Profile(FightingStyle.Judoka, 76, StyleSpecialty.JudoTripsThrows)),
        RankedFighter("00000000-0000-0000-0000-000000000106", "Robert", "Whittaker", "The Reaper", "Australia", WeightClass.Middleweight, FightingStyle.Boxer, Stance.Switch, 183, 185, 185, 35, new FighterRecord(27, 8, 0), 82,
            Profile(FightingStyle.Boxer, 86, StyleSpecialty.BoxingCountering, StyleSpecialty.BoxingCombinations),
            Profile(FightingStyle.Striker, 84, StyleSpecialty.KarateDistance, StyleSpecialty.TaekwondoKicks)),
        RankedFighter("00000000-0000-0000-0000-000000000107", "Jared", "Cannonier", "The Killa Gorilla", "USA", WeightClass.Middleweight, FightingStyle.Boxer, Stance.Orthodox, 180, 185, 196, 41, new FighterRecord(18, 8, 0), 80,
            Profile(FightingStyle.Boxer, 84, StyleSpecialty.BoxingPocketPressure, StyleSpecialty.BoxingCombinations),
            Profile(FightingStyle.Kickboxer, 76, StyleSpecialty.KickboxingKicks)),
        RankedFighter("00000000-0000-0000-0000-000000000108", "Roman", "Dolidze", "", "Georgia", WeightClass.Middleweight, FightingStyle.Wrestler, Stance.Orthodox, 188, 185, 193, 37, new FighterRecord(15, 4, 0), 79,
            Profile(FightingStyle.Wrestler, 82, StyleSpecialty.WrestlingControl, StyleSpecialty.WrestlingTakedowns),
            Profile(FightingStyle.BJJPractitioner, 76, StyleSpecialty.BjjControl)),
        RankedFighter("00000000-0000-0000-0000-000000000109", "Gregory", "Rodrigues", "Robocop", "Brazil", WeightClass.Middleweight, FightingStyle.MMAFighter, Stance.Orthodox, 191, 185, 191, 33, new FighterRecord(17, 6, 0), 79,
            Profile(FightingStyle.Boxer, 80, StyleSpecialty.BoxingPocketPressure, StyleSpecialty.BoxingCombinations),
            Profile(FightingStyle.BJJPractitioner, 78, StyleSpecialty.BjjControl),
            Profile(FightingStyle.Wrestler, 74, StyleSpecialty.WrestlingTakedownDefense)),
        RankedFighter("00000000-0000-0000-0000-000000000110", "Paulo", "Costa", "The Eraser", "Brazil", WeightClass.Middleweight, FightingStyle.Boxer, Stance.Orthodox, 185, 185, 183, 34, new FighterRecord(15, 4, 0), 79,
            Profile(FightingStyle.Boxer, 84, StyleSpecialty.BoxingPocketPressure, StyleSpecialty.BoxingCombinations),
            Profile(FightingStyle.Kickboxer, 76, StyleSpecialty.KickboxingPressure)),
        RankedFighter("00000000-0000-0000-0000-000000000111", "Joe", "Pyfer", "Bodybagz", "USA", WeightClass.Middleweight, FightingStyle.Boxer, Stance.Orthodox, 188, 185, 191, 29, new FighterRecord(14, 3, 0), 78,
            Profile(FightingStyle.Boxer, 82, StyleSpecialty.BoxingPocketPressure, StyleSpecialty.BoxingCountering),
            Profile(FightingStyle.Wrestler, 68, StyleSpecialty.WrestlingTakedownDefense)),
        RankedFighter("00000000-0000-0000-0000-000000000112", "Brunno", "Ferreira", "The Hulk", "Brazil", WeightClass.Middleweight, FightingStyle.BJJPractitioner, Stance.Orthodox, 178, 185, 183, 32, new FighterRecord(13, 2, 0), 77,
            Profile(FightingStyle.BJJPractitioner, 82, StyleSpecialty.BjjFinisher, StyleSpecialty.BjjControl),
            Profile(FightingStyle.Boxer, 74, StyleSpecialty.BoxingPocketPressure)),

        RankedFighter("00000000-0000-0000-0000-000000000113", "Carlos", "Ulberg", "Black Jag", "New Zealand", WeightClass.LightHeavyweight, FightingStyle.Kickboxer, Stance.Orthodox, 193, 205, 195, 35, new FighterRecord(13, 1, 0), 84,
            Profile(FightingStyle.Kickboxer, 90, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks),
            Profile(FightingStyle.Boxer, 80, StyleSpecialty.BoxingCountering)),
        RankedFighter("00000000-0000-0000-0000-000000000114", "Jan", "Blachowicz", "", "Poland", WeightClass.LightHeavyweight, FightingStyle.Kickboxer, Stance.Orthodox, 188, 205, 198, 42, new FighterRecord(29, 10, 1), 81,
            Profile(FightingStyle.Kickboxer, 84, StyleSpecialty.KickboxingPressure, StyleSpecialty.KickboxingKicks),
            Profile(FightingStyle.Wrestler, 76, StyleSpecialty.WrestlingTakedownDefense)),
        RankedFighter("00000000-0000-0000-0000-000000000115", "Azamat", "Murzakanov", "", "Russia", WeightClass.LightHeavyweight, FightingStyle.Boxer, Stance.Southpaw, 178, 205, 180, 36, new FighterRecord(15, 0, 0), 80,
            Profile(FightingStyle.Boxer, 84, StyleSpecialty.BoxingPocketPressure, StyleSpecialty.BoxingCountering),
            Profile(FightingStyle.Wrestler, 72, StyleSpecialty.WrestlingTakedownDefense)),
        RankedFighter("00000000-0000-0000-0000-000000000116", "Jamahal", "Hill", "Sweet Dreams", "USA", WeightClass.LightHeavyweight, FightingStyle.Boxer, Stance.Southpaw, 193, 205, 201, 34, new FighterRecord(13, 3, 1), 80,
            Profile(FightingStyle.Boxer, 84, StyleSpecialty.BoxingCombinations, StyleSpecialty.BoxingPocketPressure),
            Profile(FightingStyle.Kickboxer, 74, StyleSpecialty.KickboxingKicks)),
        RankedFighter("00000000-0000-0000-0000-000000000117", "Bogdan", "Guskov", "", "Uzbekistan", WeightClass.LightHeavyweight, FightingStyle.Boxer, Stance.Orthodox, 190, 205, 193, 32, new FighterRecord(18, 3, 0), 78,
            Profile(FightingStyle.Boxer, 82, StyleSpecialty.BoxingPocketPressure),
            Profile(FightingStyle.Kickboxer, 74, StyleSpecialty.KickboxingPressure)),
        RankedFighter("00000000-0000-0000-0000-000000000118", "Volkan", "Oezdemir", "No Time", "Switzerland", WeightClass.LightHeavyweight, FightingStyle.Kickboxer, Stance.Orthodox, 188, 205, 191, 36, new FighterRecord(20, 8, 0), 79,
            Profile(FightingStyle.Kickboxer, 84, StyleSpecialty.KickboxingPressure, StyleSpecialty.KickboxingKicks),
            Profile(FightingStyle.Boxer, 76, StyleSpecialty.BoxingCountering)),
        RankedFighter("00000000-0000-0000-0000-000000000119", "Dominick", "Reyes", "", "USA", WeightClass.LightHeavyweight, FightingStyle.Striker, Stance.Southpaw, 193, 205, 196, 36, new FighterRecord(15, 5, 0), 79,
            Profile(FightingStyle.Striker, 84, StyleSpecialty.KarateDistance, StyleSpecialty.KickboxingKicks),
            Profile(FightingStyle.Boxer, 78, StyleSpecialty.BoxingCountering)),
        RankedFighter("00000000-0000-0000-0000-000000000120", "Aleksandar", "Rakic", "", "Austria", WeightClass.LightHeavyweight, FightingStyle.Kickboxer, Stance.Orthodox, 193, 205, 198, 33, new FighterRecord(15, 5, 0), 78,
            Profile(FightingStyle.Kickboxer, 82, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks),
            Profile(FightingStyle.Wrestler, 70, StyleSpecialty.WrestlingTakedownDefense)),
        RankedFighter("00000000-0000-0000-0000-000000000121", "Johnny", "Walker", "", "Brazil", WeightClass.LightHeavyweight, FightingStyle.Striker, Stance.Orthodox, 198, 205, 208, 34, new FighterRecord(22, 8, 0), 78,
            Profile(FightingStyle.Striker, 84, StyleSpecialty.KarateDistance, StyleSpecialty.TaekwondoKicks),
            Profile(FightingStyle.MuayThai, 76, StyleSpecialty.MuayThaiKnees)),
        RankedFighter("00000000-0000-0000-0000-000000000122", "Nikita", "Krylov", "The Miner", "Ukraine", WeightClass.LightHeavyweight, FightingStyle.MMAFighter, Stance.Orthodox, 191, 205, 196, 33, new FighterRecord(31, 10, 0), 78,
            Profile(FightingStyle.Wrestler, 80, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl),
            Profile(FightingStyle.BJJPractitioner, 80, StyleSpecialty.BjjFinisher, StyleSpecialty.BjjControl),
            Profile(FightingStyle.Kickboxer, 72, StyleSpecialty.KickboxingKicks)),
        RankedFighter("00000000-0000-0000-0000-000000000123", "Dustin", "Jacoby", "The Hanyak", "USA", WeightClass.LightHeavyweight, FightingStyle.Kickboxer, Stance.Orthodox, 193, 205, 193, 37, new FighterRecord(21, 9, 1), 77,
            Profile(FightingStyle.Kickboxer, 82, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingPressure),
            Profile(FightingStyle.Boxer, 74, StyleSpecialty.BoxingCountering)),
        RankedFighter("00000000-0000-0000-0000-000000000124", "Zhang", "Mingyang", "", "China", WeightClass.LightHeavyweight, FightingStyle.Kickboxer, Stance.Orthodox, 185, 205, 191, 26, new FighterRecord(19, 6, 0), 77,
            Profile(FightingStyle.Kickboxer, 82, StyleSpecialty.KickboxingPressure, StyleSpecialty.KickboxingKicks),
            Profile(FightingStyle.Boxer, 74, StyleSpecialty.BoxingPocketPressure)),

        RankedFighter("00000000-0000-0000-0000-000000000125", "Alexander", "Volkov", "Drago", "Russia", WeightClass.Heavyweight, FightingStyle.Kickboxer, Stance.Orthodox, 201, 247, 203, 37, new FighterRecord(39, 11, 0), 83,
            Profile(FightingStyle.Kickboxer, 88, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks),
            Profile(FightingStyle.Boxer, 80, StyleSpecialty.BoxingCountering)),
        RankedFighter("00000000-0000-0000-0000-000000000126", "Curtis", "Blaydes", "", "USA", WeightClass.Heavyweight, FightingStyle.Wrestler, Stance.Orthodox, 193, 245, 203, 35, new FighterRecord(19, 5, 0), 82,
            Profile(FightingStyle.Wrestler, 90, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl, StyleSpecialty.WrestlingTakedownDefense),
            Profile(FightingStyle.Boxer, 72, StyleSpecialty.BoxingPocketPressure)),
        RankedFighter("00000000-0000-0000-0000-000000000127", "Waldo", "Cortes-Acosta", "Salsa Boy", "Dominican Republic", WeightClass.Heavyweight, FightingStyle.Boxer, Stance.Orthodox, 193, 260, 198, 33, new FighterRecord(14, 1, 0), 80,
            Profile(FightingStyle.Boxer, 84, StyleSpecialty.BoxingCombinations, StyleSpecialty.BoxingPocketPressure),
            Profile(FightingStyle.Wrestler, 68, StyleSpecialty.WrestlingTakedownDefense)),
        RankedFighter("00000000-0000-0000-0000-000000000128", "Serghei", "Spivac", "", "Moldova", WeightClass.Heavyweight, FightingStyle.Wrestler, Stance.Orthodox, 191, 260, 198, 31, new FighterRecord(18, 5, 0), 79,
            Profile(FightingStyle.Wrestler, 86, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl),
            Profile(FightingStyle.BJJPractitioner, 76, StyleSpecialty.BjjControl)),
        RankedFighter("00000000-0000-0000-0000-000000000129", "Rizvan", "Kuniev", "", "Russia", WeightClass.Heavyweight, FightingStyle.Wrestler, Stance.Orthodox, 193, 245, 196, 33, new FighterRecord(13, 2, 1), 78,
            Profile(FightingStyle.Wrestler, 84, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl, StyleSpecialty.WrestlingTakedownDefense),
            Profile(FightingStyle.Boxer, 70, StyleSpecialty.BoxingPocketPressure)),
        RankedFighter("00000000-0000-0000-0000-000000000130", "Marcin", "Tybura", "", "Poland", WeightClass.Heavyweight, FightingStyle.Wrestler, Stance.Orthodox, 191, 245, 198, 39, new FighterRecord(27, 10, 0), 78,
            Profile(FightingStyle.Wrestler, 84, StyleSpecialty.WrestlingControl, StyleSpecialty.WrestlingTakedowns),
            Profile(FightingStyle.Kickboxer, 70, StyleSpecialty.KickboxingPressure)),
        RankedFighter("00000000-0000-0000-0000-000000000131", "Derrick", "Lewis", "The Black Beast", "USA", WeightClass.Heavyweight, FightingStyle.Boxer, Stance.Orthodox, 191, 260, 201, 41, new FighterRecord(28, 12, 0), 79,
            Profile(FightingStyle.Boxer, 86, StyleSpecialty.BoxingPocketPressure, StyleSpecialty.BoxingCombinations),
            Profile(FightingStyle.Wrestler, 66, StyleSpecialty.WrestlingTakedownDefense)),
        RankedFighter("00000000-0000-0000-0000-000000000132", "Ante", "Delija", "", "Croatia", WeightClass.Heavyweight, FightingStyle.Kickboxer, Stance.Orthodox, 193, 245, 198, 34, new FighterRecord(25, 6, 0), 77,
            Profile(FightingStyle.Kickboxer, 80, StyleSpecialty.KickboxingPressure, StyleSpecialty.KickboxingKicks),
            Profile(FightingStyle.Wrestler, 70, StyleSpecialty.WrestlingTakedownDefense)),
        RankedFighter("00000000-0000-0000-0000-000000000133", "Tallison", "Teixeira", "", "Brazil", WeightClass.Heavyweight, FightingStyle.Kickboxer, Stance.Orthodox, 201, 245, 208, 25, new FighterRecord(9, 0, 0), 78,
            Profile(FightingStyle.Kickboxer, 82, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks),
            Profile(FightingStyle.Boxer, 72, StyleSpecialty.BoxingPocketPressure)),
        RankedFighter("00000000-0000-0000-0000-000000000134", "Mick", "Parkin", "", "UK", WeightClass.Heavyweight, FightingStyle.MMAFighter, Stance.Orthodox, 193, 245, 198, 30, new FighterRecord(11, 0, 0), 77,
            Profile(FightingStyle.Boxer, 76, StyleSpecialty.BoxingCombinations),
            Profile(FightingStyle.Wrestler, 76, StyleSpecialty.WrestlingTakedownDefense, StyleSpecialty.WrestlingControl),
            Profile(FightingStyle.BJJPractitioner, 70, StyleSpecialty.BjjControl)),
        RankedFighter("00000000-0000-0000-0000-000000000135", "Shamil", "Gaziev", "", "Bahrain", WeightClass.Heavyweight, FightingStyle.Boxer, Stance.Orthodox, 193, 245, 196, 35, new FighterRecord(13, 2, 0), 76,
            Profile(FightingStyle.Boxer, 80, StyleSpecialty.BoxingPocketPressure, StyleSpecialty.BoxingCombinations),
            Profile(FightingStyle.Wrestler, 68, StyleSpecialty.WrestlingControl)),
        RankedFighter("00000000-0000-0000-0000-000000000136", "Valter", "Walker", "", "Brazil", WeightClass.Heavyweight, FightingStyle.Wrestler, Stance.Orthodox, 198, 245, 203, 27, new FighterRecord(13, 1, 0), 76,
            Profile(FightingStyle.Wrestler, 82, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl),
            Profile(FightingStyle.BJJPractitioner, 72, StyleSpecialty.BjjControl)),
        RankedFighter("00000000-0000-0000-0000-000000000137", "Tai", "Tuivasa", "Bam Bam", "Australia", WeightClass.Heavyweight, FightingStyle.Boxer, Stance.Orthodox, 188, 265, 191, 33, new FighterRecord(15, 8, 0), 76,
            Profile(FightingStyle.Boxer, 82, StyleSpecialty.BoxingPocketPressure, StyleSpecialty.BoxingCombinations),
            Profile(FightingStyle.Kickboxer, 72, StyleSpecialty.KickboxingPressure)),

        // ──────────────── WOMEN'S STRAWWEIGHT (115 lbs) ────────────────
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000033"),
            FirstName = "Zhang", LastName = "Weili", Nickname = "Magnum",
            Nationality = "China", WeightClass = WeightClass.WomensStrawweight,
            PrimaryStyle = FightingStyle.Kickboxer, Stance = Stance.Southpaw,
            StyleProfiles = Styles(
                Profile(FightingStyle.Kickboxer, 88, StyleSpecialty.KickboxingPressure, StyleSpecialty.KickboxingKicks),
                Profile(FightingStyle.Wrestler, 78, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl),
                Profile(FightingStyle.Boxer, 80, StyleSpecialty.BoxingCombinations)),
            Physical = new PhysicalStats(163, 115, 163, 35),
            Striking  = new StrikingStats(84, 80, 88, 80, 82, 80),
            Grappling = new GrapplingStats(80, 76, 72, 74, 76, 78),
            Athletics = new AthleticStats(92, 78, 90, 92, 86),
            Record = new FighterRecord(24, 3, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000034"),
            FirstName = "Tatiana", LastName = "Suarez", Nickname = "The Lioness",
            Nationality = "USA", WeightClass = WeightClass.WomensStrawweight,
            PrimaryStyle = FightingStyle.Wrestler, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.Wrestler, 95, StyleSpecialty.WrestlingTakedowns, StyleSpecialty.WrestlingControl, StyleSpecialty.WrestlingTakedownDefense),
                Profile(FightingStyle.BJJPractitioner, 74, StyleSpecialty.BjjControl)),
            Physical = new PhysicalStats(163, 115, 165, 32),
            Striking  = new StrikingStats(68, 64, 72, 70, 78, 74),
            Grappling = new GrapplingStats(92, 80, 76, 78, 88, 78),
            Athletics = new AthleticStats(90, 76, 82, 90, 84),
            Record = new FighterRecord(11, 1, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000035"),
            FirstName = "Marina", LastName = "Rodriguez", Nickname = "Xena",
            Nationality = "Brazil", WeightClass = WeightClass.WomensStrawweight,
            PrimaryStyle = FightingStyle.MuayThai, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.MuayThai, 90, StyleSpecialty.MuayThaiKnees, StyleSpecialty.MuayThaiElbows, StyleSpecialty.MuayThaiClinch),
                Profile(FightingStyle.Kickboxer, 78, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks)),
            Physical = new PhysicalStats(168, 115, 170, 33),
            Striking  = new StrikingStats(80, 76, 84, 76, 78, 76),
            Grappling = new GrapplingStats(64, 66, 60, 62, 60, 62),
            Athletics = new AthleticStats(86, 70, 82, 88, 80),
            Record = new FighterRecord(17, 4, 0)
        },

        // ──────────────── WOMEN'S FLYWEIGHT (125 lbs) ────────────────
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000036"),
            FirstName = "Alexa", LastName = "Grasso", Nickname = "The Mexican Queen",
            Nationality = "Mexico", WeightClass = WeightClass.WomensFlyweight,
            PrimaryStyle = FightingStyle.BJJPractitioner, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.Boxer, 82, StyleSpecialty.BoxingCombinations, StyleSpecialty.BoxingCountering),
                Profile(FightingStyle.BJJPractitioner, 84, StyleSpecialty.BjjFinisher, StyleSpecialty.BjjControl)),
            Physical = new PhysicalStats(160, 125, 163, 31),
            Striking  = new StrikingStats(80, 72, 82, 76, 80, 78),
            Grappling = new GrapplingStats(72, 70, 84, 78, 76, 74),
            Athletics = new AthleticStats(86, 68, 86, 88, 82),
            Record = new FighterRecord(16, 3, 1)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000037"),
            FirstName = "Valentina", LastName = "Shevchenko", Nickname = "Bullet",
            Nationality = "Kyrgyzstan", WeightClass = WeightClass.WomensFlyweight,
            PrimaryStyle = FightingStyle.MuayThai, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.MuayThai, 94, StyleSpecialty.MuayThaiKnees, StyleSpecialty.MuayThaiElbows, StyleSpecialty.MuayThaiTeeps, StyleSpecialty.MuayThaiClinch),
                Profile(FightingStyle.Kickboxer, 88, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks),
                Profile(FightingStyle.Judoka, 72, StyleSpecialty.JudoTripsThrows)),
            Physical = new PhysicalStats(165, 125, 168, 36),
            Striking  = new StrikingStats(88, 76, 86, 86, 82, 82),
            Grappling = new GrapplingStats(74, 82, 74, 76, 74, 76),
            Athletics = new AthleticStats(90, 72, 90, 92, 88),
            Record = new FighterRecord(24, 4, 0)
        },
        new Fighter
        {
            Id = new Guid("00000000-0000-0000-0000-000000000038"),
            FirstName = "Manon", LastName = "Fiorot", Nickname = "La Lionne",
            Nationality = "France", WeightClass = WeightClass.WomensFlyweight,
            PrimaryStyle = FightingStyle.MMAFighter, Stance = Stance.Orthodox,
            StyleProfiles = Styles(
                Profile(FightingStyle.Kickboxer, 82, StyleSpecialty.KickboxingRange, StyleSpecialty.KickboxingKicks),
                Profile(FightingStyle.Boxer, 76, StyleSpecialty.BoxingCombinations),
                Profile(FightingStyle.Wrestler, 68, StyleSpecialty.WrestlingTakedownDefense)),
            Physical = new PhysicalStats(163, 125, 163, 31),
            Striking  = new StrikingStats(78, 74, 82, 74, 78, 76),
            Grappling = new GrapplingStats(74, 72, 68, 70, 68, 68),
            Athletics = new AthleticStats(88, 70, 82, 88, 80),
            Record = new FighterRecord(11, 1, 0)
        }
    ];
}
